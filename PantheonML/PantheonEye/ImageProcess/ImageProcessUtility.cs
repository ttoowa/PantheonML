using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PantheonEye.ImageProcess {
	public class ImageProcessUtility {
		public static unsafe bool IsMatchColors(byte* srcPtr, byte* srcPtrMax, byte[] dstBytes, int tolerance) {
			for (int i = 0; i < dstBytes.Length; ++i) {
				if (srcPtr + i >= srcPtrMax)
					return false;

				if (Math.Abs(srcPtr[i] - dstBytes[i]) > tolerance) {
					return false;
				}
			}
			return true;
		}
	}
}
