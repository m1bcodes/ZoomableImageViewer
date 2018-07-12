/*
MIT License

Copyright (c) 2017 mibcoder

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
FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
SOFTWARE.

Fork me on GitHub:
https://github.com/mibcoder/ZoomableImageViewer.git
*/

using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace ZoomableImageViewer
{
    public class OverlayGrayscale : IImageLayer
    {
        public OverlayGrayscale(int width, int height, byte[] data)
        {
            m_width = width;
            m_height = height;
            m_data = data;
            Position = new Point(0, 0);
            Alpha = 1;
            Scale = 1;
        }

        public Point Position { get; set; }

        public double Alpha { get; set; }

        public double Scale { get; set; }

        private byte[] m_data;
        private int m_width, m_height;

        public Image createImage()
        {
            Bitmap bmp = new Bitmap(m_width, m_height, PixelFormat.Format8bppIndexed);
            ColorPalette ncp = bmp.Palette;
            for (int i = 0; i < 256; i++)
                ncp.Entries[i] = Color.FromArgb(255, i, i, i);
            bmp.Palette = ncp;

            var BoundsRect = new Rectangle(0, 0, m_width, m_height);
            BitmapData bmpData = bmp.LockBits(BoundsRect,
                                            ImageLockMode.WriteOnly,
                                            bmp.PixelFormat);

            IntPtr ptr = bmpData.Scan0;

            int bytes = bmpData.Stride * bmp.Height;
            for (int i = 0; i < bmp.Height; i++)
            {
                // copy line wise, because bmpData has "stride" line length, while data[] has "width" line length
                Marshal.Copy(m_data, i * m_width, ptr + i * bmpData.Stride, bmpData.Width);
            }
            bmp.UnlockBits(bmpData);
            return bmp;
        }

        public virtual object getValueUnderCursor(PointF loc)
        {
            if (m_data == null || (int)loc.X >= m_width || (int)loc.Y >= m_height || loc.X < 0 || loc.Y < 0)
                return null;
            return m_data[(int)loc.X + m_width * (int)loc.Y];
        }
    }
}
