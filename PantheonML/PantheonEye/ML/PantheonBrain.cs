using Accord.MachineLearning.VectorMachines.Learning;
using Accord.Math.Optimization.Losses;
using Accord.Neuro;
using Accord.Neuro.Learning;
using Accord.Statistics.Kernels;
using Accord.Statistics.Models.Regression.Fitting;
using Accord.Statistics.Models.Regression.Linear;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Keras;
using Keras.Models;
using Numpy;
using Keras.Layers;
using Keras.Optimizers;
using GKitForWPF;
using GKitForWPF.Network;

namespace PantheonEye.ML {
	public class PantheonBrain {
		public static PantheonBrain Instance {
			get; private set;
		}

		public static string MovingVectorDirectoryName => $@"..\MovingVectors";
		public string ModelSettingFilename => $@"..\{modelFilename}.json";
		public string ModelWeightFilename => $@"..\{modelFilename}.h5";
		public string modelFilename;
		private BaseModel model;

		private float[,] inputDatas;

		public static GJobManager JobManager;

		public PantheonBrain(string modelFilename) {
			Instance = this;

			this.modelFilename = modelFilename;
			inputDatas = new float[1, MovingDataset.InputFloatCount];
			JobManager = new GJobManager();

			MLLoop();
		}

		public static void GlobalConfig() {
			Environment.SetEnvironmentVariable("CUDA_VISIBLE_DEVICES", "-1");
#if !DEBUG
			Keras.Keras.DisablePySysConsoleLog = true;
#endif
		}

		public async void MLLoop() {
			for(; ;) {
				JobManager.ExecuteJob();

				await Task.Delay(1);
			}
		}

		public void SaveModel() {
			string json = model.ToJson();
			File.WriteAllText(ModelSettingFilename, json);
			model.SaveWeight(ModelWeightFilename);
		}
		public void LoadOrCreateModel() {
			if (!LoadModel()) {
				// Build sequential model
				Sequential seqModel = new Sequential();
				seqModel.Add(new Dense(128, activation: "relu", input_shape: new Shape(MovingDataset.InputFloatCount)));
				seqModel.Add(new Dense(128, activation: "relu"));
				seqModel.Add(new Dense(64, activation: "relu"));
				seqModel.Add(new Dense(32, activation: "relu"));
				seqModel.Add(new Dense(2, activation: "sigmoid"));

				model = seqModel;
			}
		}
		public bool LoadModel() {
			if (File.Exists(ModelSettingFilename)) {
				model = Sequential.ModelFromJson(File.ReadAllText(ModelSettingFilename));

				if (File.Exists(ModelWeightFilename)) {
					model.LoadWeight(ModelWeightFilename);
				}
				return true;
			} else {
				return false;
			}
		}	
		
		public unsafe Vector2 PredictMoving(ChampIndicator indicator) {
			float[] outputDataF;
			try {
				float[] inputDataF = MovingDataset.GetInputData(indicator.positionHistoryList
					.Skip(indicator.positionHistoryList.Count - MovingDataset.InputVectorCount - 1)
					.Take(MovingDataset.InputVectorCount + 1).ToArray());
				for (int i = 0; i < inputDataF.Length; ++i) {
					inputDatas[0, i] = inputDataF[i];
				}

				var outputData = model.PredictOnBatch(inputDatas);
				outputDataF = outputData.GetData<float>();
			} catch {
				outputDataF = new float[2];
			}

			return new Vector2(outputDataF[0], outputDataF[1]);
		}
		private MovingDataset[] LoadMovingDatasets() {
			List<MovingDataset> datasetList = new List<MovingDataset>();

			string[] movingVecFilenames = Directory.GetFiles(MovingVectorDirectoryName, "*.vec");
			foreach(string movingVecFilename in movingVecFilenames) {
				byte[] data = File.ReadAllBytes(movingVecFilename);

				MovingDataset dataset = new MovingDataset();
				dataset.AddBytesData(data);

				datasetList.Add(dataset);
			}

			return datasetList.ToArray();
		}

		public async void TrainMovingData() {
			try {
				// Compile
				var optimizer = new Adam(lr: 0.001f);
				model.Compile(optimizer: optimizer, loss: "mse", metrics: new string[] { "accuracy" });

				// Train
				MovingDataset[] movingTrainDatasets = LoadMovingDatasets();
				int dataCount = 0;

				List<float[,]> inputList = new List<float[,]>();
				List<float[,]> outputList = new List<float[,]>();
				for(int i=0; i< movingTrainDatasets.Length; ++i) {
					MovingDataset datasetPart = movingTrainDatasets[i];

					TrainData trainData = datasetPart.GetTrainData();

					inputList.Add(trainData.inputs);
					outputList.Add(trainData.outputs);
					dataCount += trainData.inputs.GetLength(0);
				}
				float[,] inputs = new float[dataCount, MovingDataset.InputFloatCount];
				float[,] outputs = new float[dataCount, MovingDataset.OutputFloatCount];

				int inputIndex = 0;
				foreach(float[,] input in inputList) {
					for(int i=0; i<input.GetLength(0); ++i) {
						for(int f=0; f<MovingDataset.InputFloatCount; ++f) {
							inputs[inputIndex, f] = input[i, f];
						}

						++inputIndex;
					}
				}

				int outputIndex = 0;
				foreach (float[,] output in outputList) {
					for (int i = 0; i < output.GetLength(0); ++i) {
						for (int f = 0; f < MovingDataset.OutputFloatCount; ++f) {
							outputs[outputIndex, f] = output[i, f];
						}

						++outputIndex;
					}
				}

				for (; ;) {
					model.Fit(inputs, outputs, batch_size: 100, epochs: 100, verbose: 1);

					SaveModel();

					await Task.Delay(10);
				}



				//float[] predicts = model.Predict(x).GetData<float>();

				//Stopwatch watch = new Stopwatch();
				//watch.Start();
				//for (int i = 0; i < 1; ++i) {
				//	predicts = model.Predict(x, verbose: 0).GetData<float>();

				//	//Debug.WriteLine($"Result: ({string.Join(",", predicts)})");
				//}
				//watch.Stop();

				//MainWindow.Instance.Dispatcher.BeginInvoke(new Action(() => {
				//	MainWindow.Instance.DebugTextBox.Text = watch.GetElapsedMilliseconds().ToString();
				//}));
			} catch (Exception ex) {
				MainWindow.Instance.Dispatcher.BeginInvoke(new Action(() => {
					MainWindow.Instance.DebugTextBox.Text = ex.ToString();
				}));
			}
		}
		public void TrainXOR() {
			try {

				//Load train data
				float[,] testX = new float[,] { { 0, 1 }, };
				float[,] x = new float[,] { { 0, 0 }, { 0, 1 }, { 1, 0 }, { 1, 1 } };
				float[] y = new float[] { 0, 1, 1, 0 };

				//Build sequential model
				var model = new Sequential();
				model.Add(new Dense(32, activation: "relu", input_shape: new Shape(2)));
				model.Add(new Dense(32, activation: "relu"));
				model.Add(new Dropout(0.1d));
				model.Add(new Dense(1, activation: "sigmoid"));

				//Compile and train
				var optimizer = new Adam();
				model.Compile(optimizer: optimizer, loss: "mse", metrics: new string[] { "accuracy" });
				model.Fit(x, y, batch_size: 2, epochs: 1000, verbose: 1);

				float[] predicts;
				predicts = model.Predict(x).GetData<float>();
				predicts = model.PredictOnBatch(x).GetData<float>();
				predicts = model.Predict(x).GetData<float>();
				predicts = model.PredictOnBatch(x).GetData<float>();
				predicts = model.Predict(x).GetData<float>();
				predicts = model.PredictOnBatch(x).GetData<float>();

				Stopwatch watch = new Stopwatch();
				watch.Restart();
				for (int i = 0; i < 5; ++i) {
					predicts = model.PredictOnBatch(testX).GetData<float>();
				}
				watch.Stop();
				string batchMs = watch.GetElapsedMilliseconds().ToString();
				watch.Restart();
				for (int i = 0; i < 5; ++i) {
					predicts = model.Predict(testX).GetData<float>();
				}
				watch.Stop();

				MainWindow.Instance.Dispatcher.BeginInvoke(new Action(() => {
					MainWindow.Instance.DebugTextBox.Text = batchMs + " / " + watch.GetElapsedMilliseconds().ToString();
				}));
			} catch (Exception ex) {
				MainWindow.Instance.Dispatcher.BeginInvoke(new Action(() => {
					MainWindow.Instance.DebugTextBox.Text = ex.ToString();
				}));
			}
		}
	}

}
