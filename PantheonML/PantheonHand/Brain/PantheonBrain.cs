using GKitForWPF;
using Keras;
using Keras.Layers;
using Keras.Models;
using Keras.Optimizers;
using Python.Runtime;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media.Imaging;
using PantheonHand.Utility;

namespace PantheonHand.Brain {
	public class PantheonBrain {
		public static PantheonBrain Instance {
			get; private set;
		}


		public string ModelSettingFilename => $@"..\{modelFilename}.json";
		public string ModelWeightFilename => $@"..\{modelFilename}.h5";
		public string modelFilename;
		private BaseModel generatorModel;
		private BaseModel discriminatorModel;
		private BaseModel ganModel;

		private float[,] inputDatas;

		public static void GlobalConfig() {
			Environment.SetEnvironmentVariable("CUDA_VISIBLE_DEVICES", "-1");
			
#if !DEBUG
			Keras.Keras.DisablePySysConsoleLog = true;
#endif
		}
		static PantheonBrain() {
			GlobalConfig();
		}
		public PantheonBrain(string modelFilename) {
			Instance = this;

			this.modelFilename = modelFilename;
			inputDatas = new float[1, ImageData.TotalInputShape];
		}


		public void SaveModel() {
			File.WriteAllText($"ImageGenerator_{ModelSettingFilename}", generatorModel.ToJson());
			generatorModel.SaveWeight($"ImageGenerator_{ModelSettingFilename}");

			File.WriteAllText($"ImageDiscriminator_{ModelSettingFilename}", discriminatorModel.ToJson());
			discriminatorModel.SaveWeight($"ImageDiscriminator_{ModelSettingFilename}");
		}
		public void LoadOrCreateModel() {
			if (!LoadModel()) {
				CreateModels();
			}
		}
		public void CreateModels() {
			var discriminatorOptimizer = new Adam(lr: 0.01f);
			var ganOptimizer = new Adam(lr: 0.005f);

			// Image generator
			Sequential generatorSeq = new Sequential();
			Input generatorInput = new Input(new Shape(ImageData.TotalInputShape));
			generatorSeq.Add(generatorInput);
			generatorSeq.Add(new Dense(128, activation: "relu"));
			generatorSeq.Add(new Dropout(0.3f));
			generatorSeq.Add(new BatchNormalization(momentum:0.9f));
			generatorSeq.Add(new Dense(256, activation: "relu"));
			generatorSeq.Add(new BatchNormalization(momentum:0.9f));
			generatorSeq.Add(new Dense(512, activation: "relu"));
			generatorSeq.Add(new BatchNormalization(momentum:0.9f));
			generatorSeq.Add(new Dense(2048, activation: "relu"));
			generatorSeq.Add(new BatchNormalization(momentum:0.9f));
			generatorSeq.Add(new Dense(ImageData.OutputShape + ImageData.ManualInputShape, activation: "sigmoid"));

			generatorModel = generatorSeq;
			generatorModel.Compile(optimizer: ganOptimizer, loss: "mse", metrics: new string[] { "accuracy" });

			// Image discriminator
			Sequential discriminatorSeq = new Sequential();
			discriminatorSeq.Add(new Dense(2048, activation: "relu", input_shape: new Shape(ImageData.OutputShape + ImageData.ManualInputShape)));
			discriminatorSeq.Add(new Dropout(0.4f));
			discriminatorSeq.Add(new Dense(512, activation: "relu"));
			discriminatorSeq.Add(new Dropout(0.4f));
			discriminatorSeq.Add(new Dense(128, activation: "relu"));
			discriminatorSeq.Add(new Dropout(0.4f));
			discriminatorSeq.Add(new Dense(32, activation: "relu"));
			discriminatorSeq.Add(new Dropout(0.4f));
			discriminatorSeq.Add(new Dense(1, activation: "sigmoid"));

			discriminatorModel = discriminatorSeq;
			discriminatorModel.Compile(optimizer: discriminatorOptimizer, loss: "binary_crossentropy", metrics: new string[] { "accuracy" });

			// AdversarialModel
			Sequential ganSeq = new Sequential();
			//ganSeq.Add(new Concatenate(generatorModel.ToLayer(), generatorInput));
			ganSeq.Add(generatorModel.ToLayer());
			ganSeq.Add(discriminatorModel.ToLayer());

			ganModel = ganSeq;
			ganModel.Compile(optimizer: ganOptimizer, loss: "binary_crossentropy", metrics: new string[] { "accuracy" });

		}
		public bool LoadModel() {
			if (File.Exists(ModelSettingFilename)) {
				generatorModel = Sequential.ModelFromJson(File.ReadAllText(ModelSettingFilename));

				if (File.Exists(ModelWeightFilename)) {
					generatorModel.LoadWeight(ModelWeightFilename);
				}
				return true;
			} else {
				return false;
			}
		}

		public WriteableBitmap PredictImage(float[] input) {
			try {
				float[] outputDataF = PredictPixels(input);
				return ImageData.CreateImageFromOutput(outputDataF);
			} catch {
				return null;
			}
		}
		public float[] PredictPixels(float[] input, bool clipBitmapPixels = true) {
			for (int i = 0; i < input.Length; ++i) {
				inputDatas[0, i] = input[i];
			}

			var outputData = generatorModel.PredictOnBatch(inputDatas);

			if(clipBitmapPixels) {
				return outputData.GetData<float>().Take(ImageData.OutputShape).ToArray();
			} else {
				return outputData.GetData<float>();
			}
		}

		public async void TrainImageData(int epochs, int GanBatchSize = 1, int localEpochs = 1) {
			// Make Discriminator dataset
			ImageData[] sampleDatas = ImageData.GetSampleDatas();

			for (int epochI = 0; epochI < epochs; ++epochI) {
				await Task.Delay(50);
				if(MainWindow.Instance.PauseTrainCheckBox.IsChecked.Value) {
					continue;
				}

				Console.WriteLine($"Epoch {epochI} / {epochs}.");

				if(epochI % 10 == 0) {
					WriteableBitmap checkpointBitmap = PredictImage(MainWindow.Instance.constantNoise.Concat(new float[] {
						0f, 0f, 0f,
					}).ToArray());

					try {
						string filename = $"PredictImage/Predict_{epochI}.png";
						Directory.CreateDirectory(Path.GetDirectoryName(filename));
						using (FileStream stream = new FileStream(filename, FileMode.Create)) {
							PngBitmapEncoder encoder = new PngBitmapEncoder();
							encoder.Frames.Add(BitmapFrame.Create(checkpointBitmap));
							encoder.Save(stream);
						}
					} catch (Exception ex) {
						Console.WriteLine($"Failed to save predict bitmap. {ex}");
					}
				}

				// Add real data
				List<float[]> inputList = new List<float[]>();
				List<float[]> outputList = new List<float[]>();
				for (int i = 0; i < sampleDatas.Length; ++i) {
					ImageData sampleData = sampleDatas[i];

					inputList.Add(sampleData.output.Concat(sampleData.manualInput).ToArray());
					outputList.Add(new float[] { 1f });
				}

				// Add fake data
				for (int i = 0; i < 20; ++i) {
					float[] fakePixels = PredictPixels(DatasetUtility.GetNoiseArray(ImageData.TotalInputShape), false);

					inputList.Add(fakePixels);
					outputList.Add(new float[] { 0f });
				}

				float[,] inputs = inputList.GetMultiDim();
				float[,] outputs = outputList.GetMultiDim();

				// Train Discriminator
				discriminatorModel.SetTrainable(true);
				discriminatorModel.Fit(inputs, outputs, batch_size: inputs.GetLength(0), epochs: localEpochs, verbose: 1);


				// Make GAN dataset
				inputList.Clear();
				outputList.Clear();

				for (int batchI = 0; batchI < GanBatchSize; ++batchI) {
					inputList.Add(DatasetUtility.GetNoiseArray(ImageData.TotalInputShape));
					outputList.Add(new float[] { 1f });
				}
				inputs = inputList.GetMultiDim();
				outputs = outputList.GetMultiDim();

				// Train GAN
				discriminatorModel.SetTrainable(false);
				ganModel.Fit(inputs, outputs, batch_size: inputs.GetLength(0), epochs: localEpochs, verbose: 1);

				//SaveModel();
			}

			Console.WriteLine($"Complete Train.");

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

				//MainWindow.Instance.Dispatcher.BeginInvoke(new Action(() => {
				//	MainWindow.Instance.DebugTextBox.Text = batchMs + " / " + watch.GetElapsedMilliseconds().ToString();
				//}));
			} catch (Exception ex) {
				//MainWindow.Instance.Dispatcher.BeginInvoke(new Action(() => {
				//	MainWindow.Instance.DebugTextBox.Text = ex.ToString();
				//}));
			}
		}


	}
}
