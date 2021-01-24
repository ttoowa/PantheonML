using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using GKitForWPF;

namespace PantheonEye.ImageProcess {
	public class PixelPoint {
		public Vector2Int position;
		public int index;

		public PixelPoint(Vector2Int position, int index) {
			this.position = position;
			this.index = index;
		}
	}
}
