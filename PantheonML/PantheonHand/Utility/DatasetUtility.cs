using GKitForWPF;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PantheonHand.Utility {
	public static class DatasetUtility {
		public static float[,] GetMultiDim(this IEnumerable<float[]> dataCollection) {
			int shape = dataCollection.First().Length;
			float[,] result = new float[dataCollection.Count(), shape];

			int index = 0;
			foreach (float[] data in dataCollection) {
				for (int f = 0; f < shape; ++f) {
					result[index, f] = data[f];
				}

				++index;
			}
			return result;
		}
		public static float[] GetNoiseArray(int shape) {
			List<float> noiseList = new List<float>();
			for(int i=0; i<shape; ++i) {
				noiseList.Add(GRandom.Value * 2f - 1f);
			}
			return noiseList.ToArray();
		}
		public static float[] GetConstantArray(float value, int shape) {
			List<float> noiseList = new List<float>();
			for (int i = 0; i < shape; ++i) {
				noiseList.Add(shape);
			}
			return noiseList.ToArray();
		}
	}
}
