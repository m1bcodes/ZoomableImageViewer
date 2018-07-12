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
using System.Windows.Forms;

namespace ZoomableImageViewer
{
    public class DragHandle
    {
        public DragHandle(float x, float y, Color color, bool visible = true, Cursor cursor = null)
        {
            Location = new PointF(x, y);
            Size = 13;
            Visible = visible;
            Color = color;
            if (cursor == null)
                Cursor = Cursors.Hand;
        }

        public void Draw(Graphics g, Func<PointF, PointF> abs2scr)
        {
            if (Visible)
            {
                PointF loc = abs2scr(Location);
                DrawAtScreenPosition(g, loc, abs2scr);
            }
        }

        public void DrawAtScreenPosition(Graphics g, PointF loc, Func<PointF, PointF> abs2scr)
        {
            g.FillRectangle(m_fillBrush, loc.X - Size / 2, loc.Y - Size / 2, Size, Size);
        }

        public bool Test(PointF testLoc, Func<PointF, PointF> abs2scr)
        {
            PointF p1 = abs2scr(Location);
            PointF p2 = abs2scr(testLoc);
            return (Math.Abs(p1.X - p2.X) <= Size / 2 && Math.Abs(p1.Y - p2.Y) <= Size / 2);
        }

        public PointF Location { get; set; }
        public float X { get { return Location.X; } }
        public float Y { get { return Location.Y; } }
        public float Size { get; set; }

        public bool Visible { get; set; }

        public Color Color
        {
            get { return m_color; }
            set {
                m_color = value;
                m_fillBrush = new SolidBrush(value);
            }
        }
        public Cursor Cursor { get; set; }

        private Color m_color;
        private Brush m_fillBrush;
    }
}
