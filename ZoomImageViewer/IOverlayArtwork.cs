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
using System.Windows.Forms;

// ReSharper disable once CheckNamespace
namespace ZoomImageViewer
{
    [Flags]
    public enum Status
    {
        None = 0,
        Selected = 1,
        Enabled = 2,
        Visible = 4,
        All = 7
    }

    public interface IOverlayArtwork
    {

        /// <summary>
        /// paints the artwork using the supplied transformation function fAbs2Scr.
        /// </summary>
        /// <param name="e">Normal OnPaint arguments</param>
        /// <param name="fAbs2Scr">Transformation function, which transforms absolute to screen coordinates.</param>
        void Paint(PaintEventArgs e, Func<PointF, PointF> fAbs2Scr);

        /// <summary>
        /// finds which handle the mouse if over (if any). Returns -1 if no handle was found.
        /// </summary>
        /// <param name="point">Mouse coordinate (in absolute coordinates)</param>
        /// <param name="fAbs2Scr">Transformation function</param>
        /// <param name="index">a handle to the drag handle (returns -1 if no handle was found).</param>
        /// <param name="clickOffset">the difference between the position of the drag handle and 
        /// the position of the mouse clock point (in screen coordinates)</param>
        /// <returns></returns>
        bool FindHandle(PointF point, Func<PointF, PointF> fAbs2Scr, out int index, out PointF clickOffset);

        /// <summary>
        /// returns the cursor for the specified handle
        /// </summary>
        /// <param name="index"></param>
        /// <returns></returns>
        Cursor GetCursor(int index);

        /// <summary>
        /// The user drags handle[index] around and moves it to the new location.
        /// </summary>
        /// <param name="index"></param>
        /// <param name="newLocation"></param>
        void MoveHandle(int index, PointF newLocation);

        Status Status { get; set; }
    }

}
// Flags
// selected, enabled, visible