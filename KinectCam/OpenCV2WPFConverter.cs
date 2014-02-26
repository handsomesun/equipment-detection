using System;
using System.Windows.Media.Imaging;
using System.IO;
using System.Runtime.InteropServices;
using System.Windows;
using System.Drawing;
using System.Drawing.Imaging;
using Emgu.CV;
using Emgu.Util;
using Emgu.CV.CvEnum;
using Emgu.CV.Structure;
using Emgu.CV.UI;
using Microsoft.Kinect;
namespace KinectCam
{
    /// <summary>
    /// Summary description for Class1
    /// </summary>
    public static class OpenCV2WPFConverter
    {

        public static System.Drawing.Bitmap ToBitmap(this BitmapSource bitmapsource)
        {
            System.Drawing.Bitmap bitmap;
            using (var outStream = new MemoryStream())
            {
                // from System.Media.BitmapImage to System.Drawing.Bitmap
                BitmapEncoder enc = new BmpBitmapEncoder();
                enc.Frames.Add(BitmapFrame.Create(bitmapsource));
                enc.Save(outStream);
                bitmap = new System.Drawing.Bitmap(outStream);
                return bitmap;
            }
        }


        [DllImport("gdi32")]
        private static extern int DeleteObject(IntPtr o);

        /// <summary>
        /// Convert an IImage to a WPF BitmapSource. The result can be used in the Set Property of Image.Source
        /// </summary>
        /// <param name="image">The Emgu CV Image</param>
        /// <returns>The equivalent BitmapSource</returns>
        public static BitmapSource ToBitmapSource(IImage image)
        {
            using (System.Drawing.Bitmap source = image.Bitmap)
            {
                IntPtr ptr = source.GetHbitmap(); //obtain the Hbitmap

                BitmapSource bs = System.Windows.Interop.Imaging.CreateBitmapSourceFromHBitmap(
                    ptr,
                    IntPtr.Zero,
                    Int32Rect.Empty,
                    System.Windows.Media.Imaging.BitmapSizeOptions.FromEmptyOptions());

                DeleteObject(ptr); //release the HBitmap
                return bs;
            }
        }

        public static Bitmap ColorImageFrameToBitmap(ColorImageFrame colorFrame)
        {
            byte[] pixelBuffer = new byte[colorFrame.PixelDataLength];
            colorFrame.CopyPixelDataTo(pixelBuffer);

            System.Drawing.Imaging.PixelFormat pixelFormat = System.Drawing.Imaging.PixelFormat.Format32bppRgb;

            if (colorFrame.Format == ColorImageFormat.InfraredResolution640x480Fps30)
            {
                pixelFormat = System.Drawing.Imaging.PixelFormat.Format16bppRgb565;
            }

            Bitmap bitmapFrame = new Bitmap(colorFrame.Width, colorFrame.Height, pixelFormat);
            System.Drawing.Rectangle rect = new System.Drawing.Rectangle(0, 0, bitmapFrame.Width, bitmapFrame.Height);
            BitmapData bitmapData = bitmapFrame.LockBits(rect, ImageLockMode.WriteOnly, bitmapFrame.PixelFormat);

            IntPtr intPointer = bitmapData.Scan0;
            Marshal.Copy(pixelBuffer, 0, intPointer, colorFrame.PixelDataLength);

            bitmapFrame.UnlockBits(bitmapData);

            bitmapData = null;
            pixelBuffer = null;

            return bitmapFrame;
        }

    }
}