using GKitForWPF;
using GKitForWPF.Network;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PantheonEye.ML {
	public class MovingDataset {
		public const int InputVectorCount = 100;
		public static int InputFloatCount => InputVectorCount * 2;
		public const int OutputVectorCount = 1;
		public static int OutputFloatCount => OutputVectorCount * 2;
		public const int OutputOffset = 30;
		public const float VectorScaleForTrain = 0.05f;

		public List<Vector2> motionVecList;

		public static Vector2 GetTrainVector(Vector2 originVector) {
			originVector *= VectorScaleForTrain * 0.5f;
			originVector += new Vector2(0.5f, 0.5f);
			originVector = GMath.Clamp(originVector, 0, 1);

			return originVector;
		}
		public static Vector2 GetOriginVector(Vector2 trainVector) {
			trainVector -= new Vector2(0.5f, 0.5f);
			trainVector /= VectorScaleForTrain * 2f;

			return trainVector;
		}
		public static float[] GetInputData(Vector2[] vectors) {
			float[] inputs = new float[InputFloatCount];

			int index = 0;

			for(int i=1; i<vectors.Length; ++i) {
				Vector2 motionVector = vectors[i] - vectors[i - 1];
				Vector2 trainVector = GetTrainVector(motionVector);

				inputs[index++] = trainVector.x;
				inputs[index++] = trainVector.y;
			}

			return inputs;
		}

		public MovingDataset() {
			motionVecList = new List<Vector2>();
		}

		public void AddBytesData(byte[] data) {
			for (int i = 0; i < data.Length; i += (sizeof(float) * 2)) {
				float x = EndianConverter.ToLocalFloat(data, i);
				float y = EndianConverter.ToLocalFloat(data, i + sizeof(float));

				motionVecList.Add(new Vector2(x, y));
			}
		}

		public TrainData GetTrainData() {
			int loopMax = motionVecList.Count - OutputOffset;
			
			TrainData trainData = new TrainData();

			List<Vector2[]> inputVectorsList = new List<Vector2[]>();
			List<Vector2> outputVectorList = new List<Vector2>();

			for (int i=InputVectorCount; i< loopMax; ++i) {
				Vector2[] inputVectors = motionVecList.Skip(i - InputVectorCount).Take(InputVectorCount).ToArray();

				Vector2 afterVector = new Vector2();
				for (int afterI = i; afterI < i + OutputOffset; ++afterI) {
					afterVector += motionVecList[afterI];
				}
				for(int inputVecI=0; inputVecI<inputVectors.Length; ++inputVecI) {
					inputVectors[inputVecI] = GetTrainVector(inputVectors[inputVecI]);
				}
				afterVector = GetTrainVector(afterVector);

				inputVectorsList.Add(inputVectors);
				outputVectorList.Add(afterVector);
			}

			trainData.inputs = new float[inputVectorsList.Count, InputFloatCount];
			trainData.outputs = new float[outputVectorList.Count, 2];

			for(int i=0; i<inputVectorsList.Count; ++i) {
				for(int inputVecI=0; inputVecI<InputVectorCount; ++inputVecI) {
					int index = inputVecI * 2;
					Vector2 inputVec = inputVectorsList[i][inputVecI];
					trainData.inputs[i, index] = inputVec.x;
					trainData.inputs[i, index+1] = inputVec.y;
				}
			}
			for(int i=0; i<outputVectorList.Count; ++i) {
				trainData.outputs[i, 0] = outputVectorList[i].x;
				trainData.outputs[i, 1] = outputVectorList[i].y;
			}

			return trainData;
		}
	}
}
