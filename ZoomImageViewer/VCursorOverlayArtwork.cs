﻿/*
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
using System.Windows.Forms;

// ReSharper disable once CheckNamespace
namespace ZoomImageViewer
{
    public class VCursorOverlayArtwork : IOverlayArtwork
    {
        public VCursorOverlayArtwork(float position)
        {
            this.Status = Status.Visible | Status.Enabled;
            this.Position = position;
        }

        public bool FindHandle(PointF point, Func<PointF, PointF> fAbs2Scr, out int index, out PointF clickOffset)
        {
            PointF p1 = fAbs2Scr(point);
            PointF p2 = fAbs2Scr(new PointF(this.Position, 0));
            if(Math.Abs(p1.X-p2.X) < 10)
            {
                index = 1;
                clickOffset = new PointF(p2.X - p1.X, p2.Y - p1.Y);
                return true;
            } else
            {
                index = -1;
                clickOffset = new PointF();
                return false;
            }
        }

        public Cursor GetCursor(int index)
        {
            return Cursors.SizeWE;
        }

        public void MoveHandle(int index, PointF newLocation)
        {
            this.Position = newLocation.X;
        }

        public void Paint(PaintEventArgs e, Func<PointF, PointF> fAbs2Scr)
        {
            float pos = fAbs2Scr(new PointF(this.Position, 0)).X;
            e.Graphics.DrawLine(Pens.Yellow, pos, e.Graphics.ClipBounds.Top, pos, e.Graphics.ClipBounds.Bottom);
        }

        public float Position { get; set; }
        public Status Status { get; set; }
    }
}
