/*
MIT License

Copyright (c) 2017 m1bcodes

Permission is hereby granted, free of charge, to any person obtaining a copy
of this software and associated documentation files (the "Software"), to deal
in the Software without restriction, including without limitation the rights
to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
copies of the Software, and to permit persons to whom the Software is
furnished to do so, subject to the following conditions:

The above copyright notice and this permission notice shall be included in all
copies or substantial portions of the Software.

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
FITNESS FOR A PARTICULAR PURPOSE AND NON-INFRINGEMENT. IN NO EVENT SHALL THE
AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
SOFTWARE.

Fork me on GitHub:
https://github.com/mibcoder/ZoomableImageViewer.git
*/

using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;

// ReSharper disable once CheckNamespace
namespace ZoomImageViewer
{
    public class OverlayGrayScale : IImageLayer
    {
        public OverlayGrayScale(int width, int height, byte[] data)
        {
            this.width = width;
            this.height = height;
            this.data = data;
            this.Position = new Point(0, 0);
            this.Alpha = 1;
            this.Scale = 1;
        }

        public Point Position { get; set; }

        public double Alpha { get; set; }

        public double Scale { get; set; }

        private readonly byte[] data;
        private readonly int width, height;

        public Image CreateImage()
        {
            Bitmap bmp = new Bitmap(this.width, this.height, PixelFormat.Format8bppIndexed);
            ColorPalette ncp = bmp.Palette;
            for (int i = 0; i < 256; i++)
            {
                ncp.Entries[i] = Color.FromArgb(255, i, i, i);
            }

            bmp.Palette = ncp;

            var boundsRect = new Rectangle(0, 0, this.width, this.height);
            BitmapData bmpData = bmp.LockBits(boundsRect,
                                            ImageLockMode.WriteOnly,
                                            bmp.PixelFormat);

            IntPtr ptr = bmpData.Scan0;

            for (int i = 0; i < bmp.Height; i++)
            {
                // copy line wise, because bmpData has "stride" line length, while data[] has "width" line length
                Marshal.Copy(this.data, i * this.width, ptr + i * bmpData.Stride, bmpData.Width);
            }
            bmp.UnlockBits(bmpData);
            return bmp;
        }

        public virtual object GetValueUnderCursor(PointF loc)
        {
            if (this.data == null || (int)loc.X >= this.width || (int)loc.Y >= this.height || loc.X < 0 || loc.Y < 0)
            {
                return null;
            }
            return this.data[(int)loc.X + this.width * (int)loc.Y];
        }
    }
}
