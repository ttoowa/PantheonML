using GKitForWPF;
using PantheonHand.Utility;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media.Imaging;

namespace PantheonHand.Brain {
	public class ImageData {
		public static string DataImageDir => $@"..\..\Dataset";

		public const int Width = 128;
		public const int PixelCount = Width * Width;
		public const int SubpixelCount = PixelCount * 4;

		public const int ManualInputShape = 3;
		public const int TotalInputShape = ManualInputShape + NoiseCount;
		public const int NoiseCount = 50;
		public const int OutputShape = 128 * 128;

		public float[] input;
		public float[] output;
		public float[] manualInput;

		public static ImageData[] GetSampleDatas() {
			List<ImageData> datasetList = new List<ImageData>();

			string[] imageFilenames = Directory.GetFiles(DataImageDir, "*.png");

			//int i = 0;
			//foreach (string imageFile in imageFilenames) {
			//	ImageData imageData = ImageData.FromFile(imageFile, new float[] { (float)i / (imageFilenames.Length - 1) });

			//	datasetList.Add(imageData);
			//	++i;
			//}

			// Stick
			// 1. 앉음 - 서있음
			// 2. 왼쪽 - 오른쪽
			// 3. 주먹 - 손으로 V
			//AddWithNoise("01.png", new float[] { 1f, 0f, 0f });
			//AddWithNoise("02.png", new float[] { 1f, 1f, 0f });
			//AddWithNoise("03.png", new float[] { 1f, 0f, 1f });
			//AddWithNoise("04.png", new float[] { 1f, 1f, 1f });
			//AddWithNoise("05.png", new float[] { 0f, 0f, 0f });
			//AddWithNoise("06.png", new float[] { 0f, 1f, 0f });
			//AddWithNoise("07.png", new float[] { 0f, 0f, 1f });
			//AddWithNoise("08.png", new float[] { 0f, 1f, 1f });
			//AddWithNoise("09.png", new float[] { 0.5f, 0.5f, 0.5f });
			//AddWithNoise("10.png", new float[] { 0.5f, 0f, 0.5f });
			//AddWithNoise("11.png", new float[] { 0.5f, 1f, 0.5f });

			// Character
			// 1. 여캐 - 남캐
			// 2. 모자안씀 - 모자씀
			// 3. 단발 - 포니테일 - 장발
			AddWithNoise("00.png", new float[] { 0f, 1f, 0.5f });
			AddWithNoise("01.png", new float[] { 0f, 0f, 1f });
			AddWithNoise("02.png", new float[] { 0f, 0f, 0f });
			AddWithNoise("03.png", new float[] { 0f, 1f, 0f });
			AddWithNoise("04.png", new float[] { 0f, 0f, 1f });
			AddWithNoise("05.png", new float[] { 0f, 0f, 0.5f });
			AddWithNoise("06.png", new float[] { 0f, 0f, 0.5f });
			AddWithNoise("07.png", new float[] { 0f, 0f, 1f });
			AddWithNoise("08.png", new float[] { 0f, 1f, 0f });
			AddWithNoise("09.png", new float[] { 0f, 1f, 0f });
			AddWithNoise("10.png", new float[] { 0f, 1f, 1f });
			AddWithNoise("11.png", new float[] { 1f, 1f, 0f });
			AddWithNoise("12.png", new float[] { 1f, 1f, 0f });
			AddWithNoise("13.png", new float[] { 1f, 1f, 0f });
			AddWithNoise("14.png", new float[] { 1f, 0f, 0f });
			AddWithNoise("15.png", new float[] { 1f, 0f, 1f });
			AddWithNoise("16.png", new float[] { 1f, 0f, 1f });
			AddWithNoise("17.png", new float[] { 1f, 1f, 1f });

			return datasetList.ToArray();

			void AddWithNoise(string filename, float[] input) {
				for(int i=0;i<3; ++i) {
					datasetList.Add(FromFile(filename, DatasetUtility.GetNoiseArray(NoiseCount).Concat(input).ToArray(), input));
				}
			}
		}
		public unsafe static ImageData FromFile(string filename, float[] input, float[]  manualInput) {
			ImageData data = new ImageData();
			data.input = input;
			data.manualInput = manualInput;

			BitmapImage image = new BitmapImage(new Uri(Path.Combine(DataImageDir, filename), UriKind.Relative));
			WriteableBitmap bitmap = new WriteableBitmap(image);

			bitmap.Lock();
			byte* pixelBytePtr = (byte*)bitmap.BackBuffer;

			data.output = new float[PixelCount];

			int pixelI = 0;
			for(int subPixelI=0; subPixelI< SubpixelCount;) {
				data.output[pixelI] = pixelBytePtr[subPixelI] / 255f;	

				subPixelI += 4;
				pixelI++;
			}

			bitmap.Unlock();

			return data;
		}
		public unsafe static WriteableBitmap CreateImageFromOutput(float[] output) {
			WriteableBitmap bitmap = new WriteableBitmap(128, 128, 96, 96, System.Windows.Media.PixelFormats.Bgra32, null);

			bitmap.Lock();
			byte* pixelBytePtr = (byte*)bitmap.BackBuffer;

			int pixelI = 0;
			for (int subPixelI = 0; subPixelI < SubpixelCount;) {
				pixelBytePtr[subPixelI++] = (byte)(output[pixelI] * 255f);
				pixelBytePtr[subPixelI++] = (byte)(output[pixelI] * 255f);
				pixelBytePtr[subPixelI++] = (byte)(output[pixelI] * 255f);
				pixelBytePtr[subPixelI++] = (byte)255;

				pixelI++;
			}

			bitmap.Unlock();
			bitmap.Freeze();

			return bitmap;
		}

		public ImageData() {

		}
	}
}
