using GKitForWPF;
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
using System.IO;
using PantheonEye.ML;

namespace PantheonEye {
	/// <summary>
	/// ChampIndicator.xaml에 대한 상호 작용 논리
	/// </summary>
	public partial class ChampIndicator : UserControl {
		public int ID;
		public Vector2 position;
		public List<Vector2> positionHistoryList;

		private int SaveableHistoryCountMin = 200;
		private int MaxSamePositionCount = 100;

		private TranslateTransform predictorTranslateTransform;
		private ScaleTransform predictorScaleTransform;
		private RotateTransform predictorRotateTransform;
		private float predictorAngleDegree;
		private Vector2 predictorPosition;

		private Vector2 predictVector;
		private int predictTick = 4;
		private int predictFrame;

		public ChampIndicator() {
			InitializeComponent();

			positionHistoryList = new List<Vector2>();

			TransformGroup predictorTransformGroup = new TransformGroup();
			predictorTranslateTransform = new TranslateTransform();
			predictorScaleTransform = new ScaleTransform(1d, 1d);
			predictorRotateTransform = new RotateTransform(0d);

			predictorTransformGroup.Children.Add(predictorTranslateTransform);
			predictorTransformGroup.Children.Add(predictorScaleTransform);
			predictorTransformGroup.Children.Add(predictorRotateTransform);

			MovingPredictor.RenderTransform = predictorTransformGroup;
		}
		
		public void AddPositionHistory(Vector2 position) {
			if (IsSamePositionHistory(position))
				return;
			positionHistoryList.Add(position);

			UpdateMovingPredictor();
		}
		public async void ClearPositionHistory() {
			if(MainWindow.onRecordMovingHistory &&
				positionHistoryList.Count >= SaveableHistoryCountMin) {
				await Task.Run(() => {
					string movingHistory = Guid.NewGuid().ToString();
					string filePath = $@"{PantheonBrain.MovingVectorDirectoryName}\{movingHistory}.vec";

					Directory.CreateDirectory(System.IO.Path.GetDirectoryName(filePath));
					File.WriteAllBytes(filePath, GetMovingHistoryData());
				});
			}

			positionHistoryList.Clear();
			position = new Vector2();
		}

		public void UpdateMovingPredictor() {
			float positionFactor = 6f;

			if (positionHistoryList.Count < MovingDataset.InputVectorCount + 1) {
				//MovingPredictor.Visibility = Visibility.Hidden;
				return;
			}

			//PantheonBrain.JobManager.AddJob(() => {
				if(predictFrame < predictTick) {
					++predictFrame;
				} else {
					predictFrame = 0;

					predictVector = PantheonBrain.Instance.PredictMoving(this);
					predictVector = MovingDataset.GetOriginVector(predictVector);
				}

				//MainWindow.Instance.Dispatcher.Invoke(() => {
					//MovingPredictor.Visibility = Visibility.Visible;

					// Update position
					predictorPosition += (predictVector - predictorPosition) * 0.1f;
					predictorTranslateTransform.X = predictorPosition.x * positionFactor;
					predictorTranslateTransform.Y = predictorPosition.y * -positionFactor;


					MainWindow.Instance.DebugTextBox.Text = predictVector.ToString();
				//});
			//});


			//Vector2 diff = position - positionHistoryList[positionHistoryList.Count-2];

			

			//if(diff.sqrMagnitude > float.Epsilon) {
			//	targetAngleDegree = Mathf.Atan2(diff.y, diff.x) * Mathf.Rad2Deg;
			//}
			//float angleDelta = (targetAngleDegree - predictorAngleDegree);
			//if(angleDelta > 180) {
			//	angleDelta -= 360;
			//} else if(angleDelta < -180) {
			//	angleDelta += 360;
			//}
			//predictorAngleDegree += angleDelta * 0.1f;
			//predictorRotateTransform.Angle = predictorAngleDegree;

			//float distance = diff.magnitude;
			//predictorScaleTransform.ScaleX = 1d + (distance * 0.01d);
		}

		private bool IsSamePositionHistory(Vector2 position) {
			int samePositionCount = 0;
			int loopMin = Mathf.Max(0, positionHistoryList.Count - MaxSamePositionCount - 1);
			for (int i = positionHistoryList.Count - 1; i >= loopMin; --i) {
				if (positionHistoryList[i] == position) {
					++samePositionCount;
				} else {
					return false;
				}
			}
			if (samePositionCount >= MaxSamePositionCount) {
				return true;
			}
			return false;
		}

		private byte[] GetMovingHistoryData() {
			Vector2[] positionHistories = positionHistoryList.ToArray();
			List<byte> byteList = new List<byte>(positionHistories.Length * (sizeof(float) * 2));

			for(int i=1; i<positionHistories.Length; ++i) {
				Vector2 motionVector = positionHistories[i] - positionHistories[i - 1];

				byteList.AddRange(GKitForWPF.Network.EndianConverter.ToNetBytes(motionVector.x));
				byteList.AddRange(GKitForWPF.Network.EndianConverter.ToNetBytes(motionVector.y));
			}
			return byteList.ToArray();
		}
	}
}
