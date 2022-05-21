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

using System;
using System.Drawing;
using System.Windows.Forms;

// ReSharper disable once CheckNamespace
namespace ZoomImageViewer
{
    public class DragHandle
    {
        public DragHandle(float x, float y, Color color, bool visible = true, Cursor cursor = null)
        {
            this.Location = new PointF(x, y);
            this.Size = 13;
            this.Visible = visible;
            this.Color = color;
            if (cursor == null)
            {
                this.Cursor = Cursors.Hand;
            }
        }

        public void Draw(Graphics g, Func<PointF, PointF> fAbs2Scr)
        {
            if (this.Visible)
            {
                PointF loc = fAbs2Scr(this.Location);
                DrawAtScreenPosition(g, loc, fAbs2Scr);
            }
        }

        public void DrawAtScreenPosition(Graphics g, PointF loc, Func<PointF, PointF> fAbs2Scr)
        {
            g.FillRectangle(this.fillBrush, loc.X - this.Size / 2, loc.Y - this.Size / 2, this.Size, this.Size);
        }

        public bool Test(PointF testLoc, Func<PointF, PointF> fAbs2Scr)
        {
            PointF p1 = fAbs2Scr(this.Location);
            PointF p2 = fAbs2Scr(testLoc);
            return (Math.Abs(p1.X - p2.X) <= this.Size / 2 && Math.Abs(p1.Y - p2.Y) <= this.Size / 2);
        }

        public PointF Location { get; set; }
        public float X => this.Location.X;
        public float Y => this.Location.Y;
        public float Size { get; set; }

        public bool Visible { get; set; }

        public Color Color
        {
            get => this.color;
            set {
                this.color = value;
                this.fillBrush = new SolidBrush(value);
            }
        }
        public Cursor Cursor { get; set; }

        private Color color;
        private Brush fillBrush;
    }
}
