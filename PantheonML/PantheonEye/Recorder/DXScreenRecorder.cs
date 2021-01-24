using GKitForWPF;
using SharpDX;
using SharpDX.Direct3D11;
using SharpDX.DXGI;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing.Imaging;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media.Imaging;
using SystemBitmap = System.Drawing.Bitmap;

namespace PantheonEye.Recorder {
	public class DXScreenRecorder {
		public Vector2Int size;
		private int timeoutMillisec = 8;

		// Recorder resource
		private Factory1 factory;
		private Adapter1 adapter;
		private SharpDX.Direct3D11.Device device;
		private Output output;
		private Output1 output1;
		private Texture2DDescription textureDesc;
		private Texture2D screenTexture;
		private OutputDuplication duplicatedOutput;

		// Snapshot resource
		public IntPtr bitmapIntPtr;
		private SharpDX.DXGI.Resource screenResource;
		//public SystemBitmap systemBitmap;
		//private IntPtr hBitmap;
		//public WriteableBitmap writeableBitmap;

		[DllImport("gdi32.dll")]
		public static extern bool DeleteObject(IntPtr hObject);

		public DXScreenRecorder() {
			Initialize();
		}
		private void Initialize() {
			factory = new Factory1();
			//Get first adapter
			adapter = factory.GetAdapter1(0);
			//Get device from adapter
			device = new SharpDX.Direct3D11.Device(adapter);
			//Get front buffer of the adapter
			output = adapter.GetOutput(0);
			output1 = output.QueryInterface<Output1>();

			// Width/Height of desktop to capture
			int width = output.Description.DesktopBounds.Right;
			int height = output.Description.DesktopBounds.Bottom;
			size = new Vector2Int(width, height);

			// Create Staging texture CPU-accessible
			textureDesc = new Texture2DDescription {
				CpuAccessFlags = CpuAccessFlags.Read,
				BindFlags = BindFlags.None,
				Format = Format.B8G8R8A8_UNorm,
				Width = width,
				Height = height,
				OptionFlags = ResourceOptionFlags.None,
				MipLevels = 1,
				ArraySize = 1,
				SampleDescription = { Count = 1, Quality = 0 },
				Usage = ResourceUsage.Staging
			};
			screenTexture = new Texture2D(device, textureDesc);
			duplicatedOutput = output1.DuplicateOutput(device);
		}
		public void Dispose() {
			duplicatedOutput.Dispose();
			screenTexture.Dispose();
			output1.Dispose();
			output.Dispose();
			device.Dispose();
			adapter.Dispose();
			factory.Dispose();
		}
		public void DisposeSnapshot() {
			//systemBitmap.Dispose();
			//DeleteObject(hBitmap);
			screenResource.Dispose();
			duplicatedOutput.ReleaseFrame();
			device.ImmediateContext.UnmapSubresource(screenTexture, 0);
		}

		public bool Capture() {
			try {
				OutputDuplicateFrameInformation duplicateFrameInformation;

				duplicatedOutput.AcquireNextFrame(0, out duplicateFrameInformation, out screenResource);
				if (screenResource == null)
					return false;

				using (Texture2D screenTexture2D = screenResource.QueryInterface<Texture2D>()) {
					device.ImmediateContext.CopyResource(screenTexture2D, screenTexture);
				}

				DataBox mapSource = device.ImmediateContext.MapSubresource(screenTexture, 0, MapMode.Read, SharpDX.Direct3D11.MapFlags.None);
				bitmapIntPtr = mapSource.DataPointer;

				return true;
			} catch (SharpDXException e) {
				if (e.ResultCode.Code != SharpDX.DXGI.ResultCode.WaitTimeout.Result.Code) {
					Debug.WriteLine(e.Message);
					Debug.WriteLine(e.StackTrace);
				}
				return false;
			}
		}
		//public void CreateWriteableBitmap() {
		//	hBitmap = systemBitmap.GetHbitmap();
		//	BitmapSource bitmapSource = System.Windows.Interop.Imaging.CreateBitmapSourceFromHBitmap(
		//		hBitmap, IntPtr.Zero, Int32Rect.Empty, BitmapSizeOptions.FromEmptyOptions());

		//	writeableBitmap = new WriteableBitmap(bitmapSource);
		//}
	}
}
