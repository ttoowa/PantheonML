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
using SharpDX;
using SharpDX.Direct3D11;
using SharpDX.DXGI;
using GKitForWPF;
using PantheonEye.ML;
using PantheonEye.Recorder;
using PantheonEye.ImageProcess;

using Screen = System.Windows.Forms.Screen;
using Graphics = System.Drawing.Graphics;
using SystemBitmap = System.Drawing.Bitmap;
using SystemRectangle = System.Drawing.Rectangle;
using SystemSize = System.Drawing.Size;
using System.Runtime.InteropServices;
using System.IO;
using System.Diagnostics;
using System.Drawing.Imaging;

namespace PantheonEye {
	
	public partial class MainWindow : Window {
		public static MainWindow Instance {
			get; private set;
		}

		

		// Global setting
		public static bool onRecordMovingHistory = false;
		public static bool IsTrainMode = false;

		// Image processing setting
		private int findPixelInterval = 5;
		private int levelBoxWidth = 26;
		private int levelBoxTolerance1 = 20;
		private int levelBoxTolerance2 = 20;
		private int cameraBoxTolerance = 10;
		private byte[] levelBoxColorBytes = new byte[] { 2, 7, 66 };
		private byte[] levelBoxStripeBytes = new byte[] {
			//90, 93, 90, 255,
			//8, 8, 8, 255,
			//2, 7, 64, 255,

			82,85,82,255,
			82,85,82,255,
			8,8,8,255,
			8,8,8,255,
			0,3,48,255,
			0,3,48,255,
			0,3,48,255,
			0,3,48,255,
			0,3,48,255,
			0,3,48,255,
			0,3,48,255,
			0,3,48,255,
			0,3,48,255,
			0,3,48,255,
			0,3,48,255,
			0,3,48,255,
			0,3,48,255,
			0,3,48,255,
			0,3,48,255,
			0,3,48,255,
			0,3,48,255,
			8,8,8,255,
			38,37,38,255,
			68,72,68,255,
		};
		private byte[] cameraBoxBytes = Enumerable.Repeat((byte)255, 4 * 50).ToArray();
		private ChampIndicatorWindow champIndicatorWindow;
		private int cameraPosition;


		public MainWindow() {
			Instance = this;
			this.RegisterLoadedOnce(MainWindow_Loaded);

			InitializeComponent();
		}

		private void MainWindow_Loaded(object sender, RoutedEventArgs e) {

			PantheonBrain.GlobalConfig();

			//Task.Run(() => {
				PantheonBrain brain = new PantheonBrain("PantheonBrainNet");
				brain.LoadOrCreateModel();

				if(IsTrainMode) {
					brain.TrainMovingData();
					//brain.TrainXOR();
				}
			//});

			if(!IsTrainMode) {
				DXLoop();
			}
		}

		private void ShowChampIndicatorWindow() {
			champIndicatorWindow = new ChampIndicatorWindow();
			champIndicatorWindow.Show();
			champIndicatorWindow.Left = 0d;
			champIndicatorWindow.Top = 0d;
		}

		// [ Event ]
		protected override void OnClosed(EventArgs e) {
			base.OnClosed(e);

			Application.Current.Shutdown();
		}

		// []
		// Loop
		private async void GDILoop() {
			GDIScreenRecorder recorder = new GDIScreenRecorder();

			for (; ; ) {
				await Task.Delay(1);

				await Task.Run(recorder.Capture);

				//AnaylizeBitmap(recorder.systemBitmap);

				recorder.DisposeSnapshot();
			}
		}
		private void DXLoop() {
			ShowChampIndicatorWindow();

			DXScreenRecorder recorder = new DXScreenRecorder();

			Task.Run(() => {
				for (; ; ) {
					Stopwatch watch = new Stopwatch();
					watch.Start();

					if (!recorder.Capture())
						continue;

					AnaylizeBitmap(recorder.bitmapIntPtr, recorder.size);

					recorder.DisposeSnapshot();

					watch.Stop();
					Debug.WriteLine(watch.GetElapsedMilliseconds());
				}
			});
		}
		private async void TopMostLoop() {
			await Task.Delay(30000);

			champIndicatorWindow.Topmost = false;
			champIndicatorWindow.Topmost = true;
		}

		
		public void AnaylizeBitmap(IntPtr bitmapIntPtr, Vector2Int size) {
			
			StringBuilder builder = new StringBuilder();
			List<PixelPoint> levelBGPointList = new List<PixelPoint>();

			FindEnemyPosition();

			unsafe void FindEnemyPosition() {
				byte* bitmapPtr = (byte*)bitmapIntPtr;
				byte* bitmapPtrMax = bitmapPtr + size.x * size.y * 4;

				// Step 0. 화면이 이동중인지 탐색
				bool onCameraMoving = false;
				GRectInt minimapArea = new GRectInt(size.x - LolConfig.MinimapWidth, size.y - LolConfig.MinimapWidth, size.x - 50, size.y);
				for(int y=minimapArea.yMin; y<minimapArea.yMax; ++y) {
					for(int x=minimapArea.xMin; x<minimapArea.xMax; ++x) {
						int index = (y * size.x + x) * 4;
						if(ImageProcessUtility.IsMatchColors(bitmapPtr + index, bitmapPtrMax, cameraBoxBytes, cameraBoxTolerance)) {
							onCameraMoving = cameraPosition != index;
							cameraPosition = index;

							builder.AppendLine($"Camera position: ({x}, {y})");
							goto FoundCameraPosition;
						}
					}
				}
				FoundCameraPosition:;
				if (onCameraMoving) {
					Dispatcher.Invoke(new Action(() => {
						for (int i = 0; i < LolConfig.TeamPlayerCount; ++i) {
							champIndicatorWindow.indicators[i].ClearPositionHistory();
							champIndicatorWindow.SetIndicatorVisible(i, false);
						}
						DebugTextBox.Text = builder.ToString();
					}));
					return;
				}


				// Step 1. 레벨상자 픽셀 탐색
				int loopMaxX = size.x;
				int loopMaxY = Mathf.Max(0, size.y - LolConfig.BottomUIMargin);
				for (int y = 0; y < loopMaxY; y += findPixelInterval) {
					if(y > size.y-LolConfig.MinimapWidth) {
						loopMaxX = size.x - LolConfig.MinimapWidth;
					}
					for (int x = 0; x < loopMaxX; x += findPixelInterval) {
						int index = (y * size.x + x) * 4;
						if (ImageProcessUtility.IsMatchColors(bitmapPtr + index, bitmapPtrMax, levelBoxColorBytes, levelBoxTolerance1)) {
							levelBGPointList.Add(new PixelPoint(new Vector2Int(x, y), index));
						}
						index += 4;
					}

				}

				// Step 2. 근처에 있는 포인트 제거 (같은 챔프의 레벨상자임)
				StartSameChampFilter:
				PixelPoint[] compareTarget = levelBGPointList.ToArray();
				foreach (PixelPoint point in compareTarget) {
					foreach (PixelPoint otherPoint in compareTarget) {
						if (point == otherPoint)
							continue;

						if (Mathf.Abs(point.position.x - otherPoint.position.x) < levelBoxWidth &&
							Mathf.Abs(point.position.y - otherPoint.position.y) < levelBoxWidth) {
							if (point.index < otherPoint.index) {
								levelBGPointList.Remove(otherPoint);
							} else {
								levelBGPointList.Remove(point);
							}
						}
					}
				}

				// Step 3. 레벨상자가 맞는지 필터링
				List<PixelPoint> champLevelPointList = new List<PixelPoint>();
				int margin = (int)(levelBoxWidth * 0.7f);
				foreach (PixelPoint point in levelBGPointList) {
					GRectInt targetRect = new GRectInt( 
						Mathf.Max(0, point.position.x - levelBoxWidth), 
						Mathf.Max(0, point.position.y - levelBoxWidth),
						point.position.x + levelBoxWidth, point.position.y + levelBoxWidth);
					

					for (int y = targetRect.yMin; y < targetRect.yMax; ++y) {
						for (int x = targetRect.xMin; x < targetRect.xMax; ++x) {
							int index = (y * size.x + x) * 4;

							if (ImageProcessUtility.IsMatchColors(bitmapPtr + index, bitmapPtrMax, levelBoxStripeBytes, levelBoxTolerance2)) {
								champLevelPointList.Add(new PixelPoint(new Vector2Int(x, y), index));
								goto FoundChamp;
							}
						}
					}
				FoundChamp:;
				}

				builder.AppendLine($"챔프 수 : {champLevelPointList.Count}");

				Dispatcher.BeginInvoke(new Action(() => {
					//DebugTextBox.Text = builder.ToString();

					int champLevelPointCount = Mathf.Min(LolConfig.TeamPlayerCount, champLevelPointList.Count);

					List<int> updatedIndicatorList = new List<int>();
					List<PixelPoint> newPointList = new List<PixelPoint>();
					// 기존 포인트 이동
					foreach (PixelPoint point in champLevelPointList) {
						int index = champIndicatorWindow.GetNearChampIndex(point.position, updatedIndicatorList);
						if(index < 0) {
							newPointList.Add(point);
							continue;
						}

						updatedIndicatorList.Add(index);

						champIndicatorWindow.SetIndicatorVisible(index, true);
						champIndicatorWindow.SetIndicatorPosition(index, point.position);
					}
					// 새로운 포인트 이동
					foreach(PixelPoint point in newPointList) {
						for(int i=0; i<LolConfig.TeamPlayerCount; ++i) {
							if (updatedIndicatorList.Contains(i))
								continue;

							updatedIndicatorList.Add(i);

							champIndicatorWindow.SetIndicatorVisible(i, true);
							champIndicatorWindow.SetIndicatorPosition(i, point.position);
						}
					}
					// 미사용 인디케이터 클리어 / 저장
					for(int i=0; i<LolConfig.TeamPlayerCount; ++i) {
						if (updatedIndicatorList.Contains(i))
							continue;

						champIndicatorWindow.indicators[i].ClearPositionHistory();
						champIndicatorWindow.SetIndicatorVisible(i, false);
					}
				}));
				
				
			}
		}
	}
	
	
}
