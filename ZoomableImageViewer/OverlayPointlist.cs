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
    public class OverlayPointList : IImageLayer
    {
        public OverlayPointList(short[] pointList, Color colour)
        {
            m_pointList = pointList;

            Position = new Point(0, 0);
            Alpha = 1;
            Scale = 1;
            Color = colour;
            ColorMap = null;
        }

        public OverlayPointList(short[] pointList, byte[] colorList, Color[] colourMap)
        {
            m_pointList = pointList;
            Color = Color.White;
            ColorMap = colourMap;
            m_colorList = colorList;
            Position = new Point(0, 0);
            Alpha = 1;
            Scale = 1;
        }

        public Point Position { get; set; }

        public double Alpha { get; set; }

        public double Scale { get; set; }

        public Color Color { get; set; }

        public Color[] ColorMap { get; set; }

        /// <summary>
        /// allows to set the color list also after the object has been constructed. The caller has to make sure to update the image viewer for the changes to take effect.
        /// </summary>
        public byte[] ColorList
        {
            set
            {
                m_colorList = value;
            }
        }

        private short[] m_pointList;
        private byte[] m_colorList;     // index into colourMap

        public Image createImage()
        {
            // 0. calculate extent of pointlist
            int x1 = int.MaxValue;
            int y1 = int.MaxValue;
            int x2 = int.MinValue;
            int y2 = int.MinValue;
            for (int i = 0; i < m_pointList.Length / 2; i++)
            {
                int x = m_pointList[2 * i];
                int y = m_pointList[2 * i + 1];
                if (x < x1) x1 = x;
                if (y < y1) y1 = y;
                if (x > x2) x2 = x;
                if (y > y2) y2 = y;
            }

            int bmpWidth = x2 - x1 + 1;
            int bmpHeight = y2 - y1 + 1;
            Position.Offset(x1, y1);

            Bitmap bmp = new Bitmap(bmpWidth, bmpHeight, PixelFormat.Format8bppIndexed);

            ColorPalette ncp = bmp.Palette;
            if (ColorMap != null)
            {
                for (int i = 0; i < ColorMap.Length; i++)
                    ncp.Entries[i] = ColorMap[i];
            }
            else
            {
                ncp.Entries[0] = Color.FromArgb(0, Color.White);
                ncp.Entries[1] = Color;
            }
            bmp.Palette = ncp;

            var BoundsRect = new Rectangle(0, 0, bmpWidth, bmpHeight);
            BitmapData bmpData = bmp.LockBits(BoundsRect,
                                            ImageLockMode.WriteOnly,
                                            bmp.PixelFormat);

            IntPtr ptr = bmpData.Scan0;

            if (m_colorList != null)
            {
                for (int i = 0; i < m_pointList.Length / 2; i++)
                {
                    short x = (short)(m_pointList[2 * i] - x1);
                    short y = (short)(m_pointList[2 * i + 1] - y1);

                    Marshal.WriteByte(ptr + y * bmpData.Stride + x, m_colorList[i]);
                }
            }
            else
            {
                for (int i = 0; i < m_pointList.Length / 2; i++)
                {
                    short x = (short)(m_pointList[2 * i] - x1);
                    short y = (short)(m_pointList[2 * i + 1] - y1);

                    Marshal.WriteByte(ptr + y * bmpData.Stride + x, 1);
                }
            }
            bmp.UnlockBits(bmpData);

            return bmp;
        }

        public virtual object getValueUnderCursor(PointF loc)
        {
            return null;
        }
    }
}
