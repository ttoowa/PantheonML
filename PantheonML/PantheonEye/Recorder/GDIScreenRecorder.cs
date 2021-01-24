using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Forms;
using System.Windows.Media.Imaging;
using SystemBitmap = System.Drawing.Bitmap;
using SystemSize = System.Drawing.Size;

namespace PantheonEye.Recorder {
	public class GDIScreenRecorder {
		public SystemBitmap systemBitmap;
		public Graphics graphics;
		public WriteableBitmap writeableBitmap;
		public IntPtr hBitmap;

		[DllImport("gdi32.dll")]
		public static extern bool DeleteObject(IntPtr hObject);

		public GDIScreenRecorder() {
		}
		public void DisposeSnapshot() {
			graphics.Dispose();
			systemBitmap.Dispose();
			DeleteObject(hBitmap);
		}

		public void Capture() {
			Screen screen = Screen.PrimaryScreen;
			int width = screen.WorkingArea.Width;
			int height = screen.WorkingArea.Height;

			SystemSize size = new SystemSize(width, height);
			systemBitmap = new SystemBitmap(width, height);
			graphics = Graphics.FromImage(systemBitmap);
			graphics.CopyFromScreen(0, 0, 0, 0, size);
		}
		public void CreateWriteableBitmap() {
			hBitmap = systemBitmap.GetHbitmap();
			BitmapSource bitmapSource = System.Windows.Interop.Imaging.CreateBitmapSourceFromHBitmap(
				hBitmap, IntPtr.Zero, Int32Rect.Empty, BitmapSizeOptions.FromEmptyOptions());

			writeableBitmap = new WriteableBitmap(bitmapSource);
		}
	}
}
