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
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ZoomableImageViewer
{
    public class OverlayBitmap : IImageLayer
    {
        public OverlayBitmap(Image image)
        {
            m_image = image;
            Position = new Point(0, 0);
            Scale = 1;
            Alpha = 1;
        }

        // public Point Position => m_position;

        public double Alpha { get; set; }

        public double Scale { get; set; }

        public Point Position { get; set; }

        private Image m_image;

        public Image createImage()
        {
            return m_image;
        }

        public virtual object getValueUnderCursor(PointF loc)
        {
            if(m_image is Bitmap)
            {
                int lx = Point.Round(loc).X - Position.X;
                int ly = Point.Round(loc).Y - Position.Y;
                Bitmap bm = m_image as Bitmap;
                if(lx >= 0 && ly >= 0 && lx < bm.Width && ly < bm.Height)
                {
                    return bm.GetPixel(lx,ly);
                }
            }
            return null;
        }
    }
}
