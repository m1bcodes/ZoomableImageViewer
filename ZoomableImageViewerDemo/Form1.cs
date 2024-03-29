﻿/*
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
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using ZoomImageViewer;

namespace ZoomableImageViewerDemo
{
    public partial class Form1 : Form, IMessageFilter
    {
        Image m_img;
        Image m_img2;

        public Form1()
        {
            InitializeComponent();
            //m_img = Image.FromFile(@"c:\Users\mibu\Desktop\DSC02921.jpg");
            
            m_img = Image.FromFile(@"..\..\test\Saturn_from_Cassini_Orbiter_(2004-10-06).jpg");
            m_img2 = Image.FromFile(@"..\..\test\508px-Jupiter_by_Cassini-Huygens.jpg");

            // either use multiple overlays, or just one image:
            if (false)
            {
                zoomImageViewer1.Images.Add(new OverlayBitmap(m_img));

                // create grayscale bitmap
                int width = 200;
                int height = 200;
                byte[] data = new byte[width * height];
                for (int iy = 0; iy < height; iy++)
                    for (int ix = 0; ix < width; ix++)
                        data[ix + iy * width] = (byte)(ix + iy);

                IImageLayer il = new OverlayGrayScale(width, height, data);
                il.Position = new Point(300, 400);
                il.Alpha = 0.5;
                zoomImageViewer1.Images.Add(il);

                IImageLayer i2 = new OverlayBitmap(m_img2);
                i2.Position = new Point(1700,800);
                zoomImageViewer1.Images.Add(i2);

                // create overlay from pointlist
                short[] pl = new short[2*width * height];
                int count = 0;

                for (short iy = 0; iy < height; iy++)
                    for (short ix = 0; ix < width; ix++)
                    {
                        if ((ix+iy<width))
                        {
                            pl[count++] = ix;
                            pl[count++] = iy;
                        }
                    }
                Array.Resize(ref pl, count);

                il = new OverlayPointList(pl, Color.Yellow);
                il.Position = new Point(600, 200);
                il.Alpha = 0.7;
                zoomImageViewer1.Images.Add(il);

                zoomImageViewer1.UpdateImageList();
            }
            else
            {
                zoomImageViewer1.Image = m_img;
            }
            zoomImageViewer2.Image = m_img;
            zoomImageViewer1.FieldOfViewChanged += zoomImageViewer2.SetFieldOfView;
            zoomImageViewer2.FieldOfViewChanged += zoomImageViewer1.SetFieldOfView;

            // zoomImageViewer1.AspectRatio = 2;
            zoomImageViewer1.AspectFixed = false;

            // create overlay artwork
            RectangleOverlayArtwork oa = new RectangleOverlayArtwork(new Rectangle(0, 0, 200, 300), 30f / 180f * 3.14159f, Color.Yellow);
            propertyGrid1.SelectedObject = oa;
            zoomImageViewer1.Overlays.Add(oa);

            ZoomImageViewer.HScaleBar hs = new ZoomImageViewer.HScaleBar(1, "Hallo {0} {1}m", Color.Green);
            hs.Scale = 10e-9f;
            zoomImageViewer1.Overlays.Add(hs);

            ZoomImageViewer.HScaleBar vs = new ZoomImageViewer.VScaleBar(1, "Vallo {0} {1}m", Color.Green);
            vs.Scale = 10e-9f;
            zoomImageViewer1.Overlays.Add(vs);

            ZoomImageViewer.VCursorOverlayArtwork vc = new VCursorOverlayArtwork(0);
            zoomImageViewer1.Overlays.Add(vc);

            toolStrip1.Items.Add(new ToolStripSeparator());
            zoomImageViewer1.AppendToolStrip(toolStrip1);
            Application.AddMessageFilter(this);
        }

        #region Mouse wheel redirect
        public bool PreFilterMessage(ref Message m)
        {
            if (m.Msg == 0x20a)
            {
                Point pos = Cursor.Position; //  new Point(m.LParam.ToInt32() & 0xffff, m.LParam.ToInt32() >> 16);
                IntPtr hWnd = WindowFromPoint(pos);
                // if mouse is over the PixelGrid, send the envent to the handler of the main form.
                if (hWnd != IntPtr.Zero && Control.FromHandle(hWnd) == zoomImageViewer1)
                {
                    SendMessage(this.zoomImageViewer1.Handle, m.Msg, m.WParam, m.LParam);
                    return true;
                }
            }
            return false;
        }

        [DllImport("user32.dll")]
        private static extern IntPtr WindowFromPoint(Point pt);
        [DllImport("user32.dll")]
        private static extern IntPtr SendMessage(IntPtr hWnd, int msg, IntPtr wp, IntPtr lp);
        #endregion

        private void toolStripButton1_Click(object sender, EventArgs e)
        {
            //zoomImageViewer1.Fit = false;
            //zoomImageViewer1.DisplayScale = 1;
        }

        private void toolStripButton2_Click(object sender, EventArgs e)
        {
            zoomImageViewer1.Fit = true;
        }

        private void toolStripButton3_Click(object sender, EventArgs e)
        {
            //zoomImageViewer1.Fit = false;
            //zoomImageViewer1.DisplayScale *= 1.1f;
        }

        private void toolStripButton4_Click(object sender, EventArgs e)
        {
            //zoomImageViewer1.Fit = false;
            //zoomImageViewer1.DisplayScale /= 1.1f;
        }

        private void propertyGrid1_PropertyValueChanged(object s, PropertyValueChangedEventArgs e)
        {
            zoomImageViewer1.Invalidate();
            
        }

        private void toolStripAddOverlay_Click(object sender, EventArgs e)
        {
            RectangleOverlayArtwork oa = new RectangleOverlayArtwork(new Rectangle(400, 500, 200, 300), 0f / 180f * 3.14159f, Color.Red);
            zoomImageViewer1.Overlays.Add(oa);
        }

        private void toolStripRemoveOverlay_Click(object sender, EventArgs e)
        {
            foreach(var oa in zoomImageViewer1.Overlays.Where((oa) => oa.Status.HasFlag(Status.Selected)).ToList())
                zoomImageViewer1.Overlays.Remove(oa);
        }

        private void toolStripInvalidate_Click(object sender, EventArgs e)
        {
            zoomImageViewer1.Invalidate();
        }

        private void zoomImageViewer1_OverlayArtworkChanged(IOverlayArtwork sender)
        {
            Console.WriteLine("OA changed");
            propertyGrid1.Update();

        }

        private void zoomImageViewer1_OverlayArtworkSelectionChanged(IOverlayArtwork sender)
        {
            Console.WriteLine("OA selected");
            propertyGrid1.SelectedObject = sender;
        }

        private void zoomImageViewer1_MousePositionChanged(object sender, PointF location, object valueUnderCursor)
        {
            toolStripStatusLabel1.Text = string.Format("Mouse position: ({0},{1}) : {2}", location.X, location.Y, valueUnderCursor?.ToString());
        }

        private void btAllowAdd_Click(object sender, EventArgs e)
        {
            btAllowAdd.Checked = !btAllowAdd.Checked;
            if(btAllowAdd.Checked)
            {
                zoomImageViewer1.OverlayArtworkCreator = RectangleOverlayArtwork.RectangleCreator;

            } else
            {
                zoomImageViewer1.OverlayArtworkCreator = null;
            }
        }
    }
}
