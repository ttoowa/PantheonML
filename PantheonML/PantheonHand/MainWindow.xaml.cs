using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using PantheonHand.Brain;
using GKitForWPF;
using PantheonHand.Utility;

namespace PantheonHand {
	/// <summary>
	/// MainWindow.xaml에 대한 상호 작용 논리
	/// </summary>
	public partial class MainWindow : Window {
		public static MainWindow Instance {
			get; private set;
		}

		private PantheonBrain brain;
		private bool requestImageDraw;
		public float[] constantNoise;

		// Constructor
		public MainWindow() {
			Instance = this;
			this.RegisterLoadedOnce(OnLoaded);

			InitializeComponent();
		}

		// Event - Window
		private void OnLoaded(object sender, RoutedEventArgs e) {
			constantNoise = DatasetUtility.GetNoiseArray(ImageData.NoiseCount);

			brain = new PantheonBrain("ImageDrawer");
			brain.CreateModels();

			brain.TrainImageData(1000000);

			// RegisterEvents
			NumberEditor_input0.ValueChanged += OnInputChanged;
			NumberEditor_input1.ValueChanged += OnInputChanged;
			NumberEditor_input2.ValueChanged += OnInputChanged;

			// Start loop
			Loop();
		}

		// Event - Editor
		private void OnInputChanged() {
			requestImageDraw = true;
		}

		// Loop
		private async void Loop() {
			for (; ; ) {

				//if(requestImageDraw) {
				requestImageDraw = false;

				float[] noise;
				if (FixNoiseCheckBox.IsChecked.Value) {
					noise = constantNoise;
				} else {
					noise = DatasetUtility.GetNoiseArray(ImageData.NoiseCount);
				}

				WriteableBitmap bitmap = brain.PredictImage(noise.Concat(new float[] {
						NumberEditor_input0.Value,
						NumberEditor_input1.Value,
						NumberEditor_input2.Value,
					}).ToArray());
				GeneratedImageView.Source = bitmap;

				GeneratedImageView.InvalidateVisual();

				//Console.WriteLine("Image updated");
				//}

				await Task.Delay(Mathf.Clamp(NumberEditor_refreshRate.IntValue, 10, 100000));
			}
		}
	}
}
