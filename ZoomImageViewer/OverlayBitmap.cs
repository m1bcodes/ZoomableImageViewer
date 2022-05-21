/*
MIT License

Copyright (c) 2017,2022 m1bcodes

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

using System.Drawing;

// ReSharper disable once CheckNamespace
namespace ZoomImageViewer
{
    public class OverlayBitmap : IImageLayer
    {
        public OverlayBitmap(Image image)
        {
            this.image = image;
            this.Position = new Point(0, 0);
            this.Scale = 1;
            this.Alpha = 1;
        }

        // public Point Position => m_position;

        public double Alpha { get; set; }

        public double Scale { get; set; }

        public Point Position { get; set; }

        private readonly Image image;

        public Image CreateImage()
        {
            return this.image;
        }

        public virtual object GetValueUnderCursor(PointF loc)
        {
            if(this.image is Bitmap bm)
            {
                int lx = Point.Round(loc).X - this.Position.X;
                int ly = Point.Round(loc).Y - this.Position.Y;
                if(lx >= 0 && ly >= 0 && lx < bm.Width && ly < bm.Height)
                {
                    return bm.GetPixel(lx,ly);
                }
            }
            return null;
        }
    }
}
