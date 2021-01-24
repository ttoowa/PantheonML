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
using System.Windows.Shapes;
using GKitForWPF;

namespace PantheonEye {
	/// <summary>
	/// ChampIndicator.xaml에 대한 상호 작용 논리
	/// </summary>
	public partial class ChampIndicatorWindow : Window {
		private Vector2 indicatorOffset = new Vector2(-30, 15);

		public ChampIndicator[] indicators;


		public ChampIndicatorWindow() {
			this.RegisterLoadedOnce(ChampIndicatorWindow_Loaded);

			InitializeComponent();
			CreateIndicators();
		}
		private void CreateIndicators() {
			indicators = new ChampIndicator[LolConfig.TeamPlayerCount];
			for (int i = 0; i < indicators.Length; ++i) { 
				ChampIndicator indicator = indicators[i] = new ChampIndicator();
				indicator.ID = i;
				indicator.HorizontalAlignment = HorizontalAlignment.Left;
				indicator.VerticalAlignment = VerticalAlignment.Top;

				IndicatorGrid.Children.Add(indicator);
			}

		}

		// [ Event ]
		private void ChampIndicatorWindow_Loaded(object sender, RoutedEventArgs e) {
			this.SetIgnoreWindow();
		}

		public void SetIndicatorVisible(int index, bool visible) {
			ChampIndicator indicator = indicators[index];

			indicator.Visibility = visible ? Visibility.Visible : Visibility.Hidden;
		}
		public void SetIndicatorPosition(int index, Vector2 position) {
			ChampIndicator indicator = indicators[index];

			indicator.Margin = new Thickness(position.x + indicatorOffset.x, position.y + indicatorOffset.y, 0, 0);
			indicator.position = position;
			indicator.AddPositionHistory(position);
		}

		// []
		public int GetNearChampIndex(Vector2 position, List<int> expectIndexList) {
			int nearIndex = -1;
			float nearDistance = 0f;
			for(int i=0; i<LolConfig.TeamPlayerCount; ++i) {
				ChampIndicator indicator = indicators[i];

				if (indicator.position == new Vector2())
					continue;

				if (expectIndexList.Contains(i))
					continue;

				float distance = (indicator.position - position).magnitude;
				if (nearIndex < 0 || distance < nearDistance) {
					nearIndex = i;
					nearDistance = distance;
				}
			}
			return nearIndex;
		}
	}
}
