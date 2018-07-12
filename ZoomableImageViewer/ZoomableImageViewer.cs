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
using System.Drawing;
using System.Windows.Forms;
using System.Drawing.Drawing2D;
using System.Runtime.InteropServices;
using System.Collections.Generic;
using System.Drawing.Imaging;

namespace ZoomableImageViewer
{

    public delegate void MousePositionChangedEventHandler(object sender, PointF location, object valueUnderCursor);
    public partial class ZoomableImageViewer : ScrollableControl
    {

        Image m_image;
        List<IImageLayer> m_imageList;
        List<IOverlayArtwork> m_overlayList;

        float m_scalex = 1;
        float m_scaley = 1;

        bool m_fit = true;

        // drag related stuff
        enum DragMode
        {
            dmNone,
            dmPan,
            dmWindowZoom,
            dmDragHandle
        };
        DragMode m_dragActive = DragMode.dmNone;
        Point m_dragStartPoint;
        Point m_dragStartScrollPosition;
        Rectangle m_dragWindowRectangle = new Rectangle();
        IOverlayArtwork m_dragOa = null;
        int m_dragIndex = -1;
        ToolStripButton btnZoomWindow = null;

        // image cache
        Image m_imgCache;
        RectangleF m_cachedSrcRect;
        RectangleF m_cachedDstRect;

        public event MousePositionChangedEventHandler e_MousePositionChanged;

        public ZoomableImageViewer()
        {
            // base.InitializeComponent();
            m_imageList = new List<IImageLayer>();
            m_overlayList = new List<IOverlayArtwork>();
            ZoomWindowEnabled = false;
            DoubleBuffered = true;


            SetStyle(ControlStyles.Selectable, true);
            TabStop = true;
        }

        [DllImport("user32.dll")]
        public static extern int SendMessage(IntPtr hWnd, Int32 wMsg, bool wParam, Int32 lParam);

        private const int WM_SETREDRAW = 11;

        public static void SuspendDrawing(Control parent)
        {
            SendMessage(parent.Handle, WM_SETREDRAW, false, 0);
        }

        public static void ResumeDrawing(Control parent)
        {
            SendMessage(parent.Handle, WM_SETREDRAW, true, 0);
            parent.Refresh();
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            if (m_image != null)
            {
                Console.WriteLine("OnPaint");
                float srcX = (float)((-AutoScrollPosition.X + e.ClipRectangle.X) / m_scalex);
                float srcY = (float)((-AutoScrollPosition.Y + e.ClipRectangle.Y) / m_scaley);
                float srcWidth = (float)(e.ClipRectangle.Width / m_scalex);
                float srcHeight = (float)(e.ClipRectangle.Height / m_scaley);

                // image cache mechanism
                RectangleF srcRect = new RectangleF(srcX, srcY, srcWidth, srcHeight);
                Rectangle dstRect = e.ClipRectangle;
                Console.WriteLine("Clip = " + dstRect.ToString());
                if(srcRect != m_cachedSrcRect || dstRect != m_cachedDstRect)
                {
                    if (m_imgCache?.Size != dstRect.Size)
                    {
                        m_imgCache?.Dispose();
                        m_imgCache = new Bitmap(dstRect.Width, dstRect.Height, e.Graphics);
                    }                     
                    using (Graphics g1 = Graphics.FromImage(m_imgCache))
                    {
                        g1.Clear(Color.LightGray);
                        g1.CompositingMode = CompositingMode.SourceOver;
                        g1.CompositingQuality = CompositingQuality.HighSpeed;
                        if (m_scalex < 1 || m_scaley < 1)
                            g1.InterpolationMode = InterpolationMode.Bilinear;
                        else
                            g1.InterpolationMode = InterpolationMode.NearestNeighbor;
                        g1.SmoothingMode = SmoothingMode.HighSpeed;
                        g1.PixelOffsetMode = PixelOffsetMode.HighSpeed;
                        g1.DrawImage(m_image, new Rectangle(0,0,m_imgCache.Width, m_imgCache.Height), srcX, srcY, srcWidth, srcHeight, GraphicsUnit.Pixel);
                        Console.WriteLine("Update image cache" + srcRect.ToString() + ", img cache size=" + m_imgCache.Size) ;
                    }
                }
                m_cachedDstRect = dstRect;
                m_cachedSrcRect = srcRect;

                //e.Graphics.CompositingMode = CompositingMode.SourceOver;
                //e.Graphics.CompositingQuality = CompositingQuality.HighSpeed;
                //if (m_scalex < 1 || m_scaley < 1)
                //    e.Graphics.InterpolationMode = InterpolationMode.Bilinear;
                //else
                //    e.Graphics.InterpolationMode = InterpolationMode.NearestNeighbor;
                //e.Graphics.SmoothingMode = SmoothingMode.HighSpeed;
                //e.Graphics.PixelOffsetMode = PixelOffsetMode.HighSpeed;
                //e.Graphics.DrawImage(m_image, e.ClipRectangle, srcX, srcY, srcWidth, srcHeight, GraphicsUnit.Pixel);
                
                e.Graphics.DrawImage(m_imgCache, dstRect.Location);

                RectangleF newClipRect = e.Graphics.ClipBounds;
                newClipRect.Width = Math.Min(m_image.Width * m_scalex, newClipRect.Width);
                newClipRect.Height = Math.Min(m_image.Height * m_scaley, newClipRect.Height);
                e.Graphics.Clip = new Region(newClipRect);

                Console.WriteLine("Clip rect = " + e.Graphics.ClipBounds.ToString());
                foreach (IOverlayArtwork oa in m_overlayList)
                {
                    oa.Paint(e, abs2scr);
                }

                if(Focused)
                {
                    Rectangle rc = new Rectangle((int)newClipRect.X, (int)newClipRect.Y, (int)newClipRect.Width, (int)newClipRect.Height) ;
                    rc.Inflate(-2, -2);
                    ControlPaint.DrawFocusRectangle(e.Graphics, rc);
                }
            }
        }

        private bool findHandle(PointF location, out IOverlayArtwork oa, out int index)
        {
            foreach (IOverlayArtwork ioa in m_overlayList)
            {
                if (ioa.findHandle(location, abs2scr, out index))
                {
                    oa = ioa;
                    return true;
                }
            }
            index = -1;
            oa = null;
            return false;
        }

        protected override void OnMouseWheel(MouseEventArgs e)
        {
            SuspendDrawing(this);
            Point pClient = e.Location;

            float absX = absx(pClient.X);
            float absY = absy(pClient.Y);
            if (e.Delta > 0)
            {
                m_scalex *= ZoomStep;
                m_scaley *= ZoomStep;
            }
            if (e.Delta < 0)
            {
                m_scalex /= ZoomStep;
                m_scaley /= ZoomStep;
            }
            m_fit = false;

            updateImageParameters();
            this.AutoScrollPosition = new Point(
                (int)(absX * m_scalex - pClient.X),
                (int)(absY * m_scaley - pClient.Y));
            Invalidate();
            ResumeDrawing(this);
        }

        protected override void OnMouseMove(MouseEventArgs e)
        {
            PointF loc = new PointF(absx(e.Location.X), absy(e.Location.Y));
            if (m_image==null || loc.X < 0 || loc.Y < 0 || loc.X >= m_image.Width || loc.Y >= m_image.Height)
            {
                base.OnMouseMove(e);
                return;
            }

            object value = null;
            for(int i=m_imageList.Count-1; i>=0; i--)
            {
                value = m_imageList[i].getValueUnderCursor(loc);
                if (value != null)
                    break;
            }
            if (e_MousePositionChanged != null) e_MousePositionChanged(this, loc, value);

            if (m_dragActive == DragMode.dmDragHandle)
            {
                m_dragOa.moveHandle(m_dragIndex, loc);
                Invalidate();
            }
            else if (m_dragActive == DragMode.dmPan)
            {
                Point cpos = e.Location;
                this.AutoScrollPosition = new Point(
                    (int)(-m_dragStartScrollPosition.X - (cpos.X - m_dragStartPoint.X) ),
                    (int)(-m_dragStartScrollPosition.Y - (cpos.Y - m_dragStartPoint.Y) ));
                Invalidate();
            }
            else if(m_dragActive== DragMode.dmWindowZoom)
            {
                ControlPaint.DrawReversibleFrame(m_dragWindowRectangle, Color.Black, FrameStyle.Dashed);
                Point p1 = PointToScreen(m_dragStartPoint);
                Point p2 = PointToScreen(e.Location);
                m_dragWindowRectangle = new Rectangle(p1.X, p1.Y, p2.X - p1.X, p2.Y - p1.Y);
                ControlPaint.DrawReversibleFrame(m_dragWindowRectangle, Color.Black, FrameStyle.Dashed);
            }
            else if(!ZoomWindowEnabled)
            {
                IOverlayArtwork oa;
                int index;
                if (findHandle(loc, out oa, out index))
                {
                    this.Cursor = oa.getCursor(index);
                }
                else
                {
                    this.Cursor = Cursors.Arrow;
                }
            }
            base.OnMouseMove(e);
        }

        protected override void OnMouseDown(MouseEventArgs e)
        {
            PointF loc = new PointF(absx(e.Location.X), absy(e.Location.Y));
            if (m_image == null || loc.X < 0 || loc.Y < 0 || loc.X >= m_image.Width || loc.Y >= m_image.Height)
            {
                base.OnMouseMove(e);
                return;
            }
            if (e.Button == MouseButtons.Left)
            {
                if (ZoomWindowEnabled)
                {
                    m_dragActive = DragMode.dmWindowZoom;
                    m_dragStartPoint = e.Location;
                }
                else
                {
                    IOverlayArtwork oa;
                    int index;
                    if (findHandle(loc, out oa, out index))
                    {
                        m_dragActive = DragMode.dmDragHandle;
                        m_dragOa = oa;
                        m_dragIndex = index;
                    }
                }
            }
            else if (e.Button == MouseButtons.Middle)
            {
                m_dragActive = DragMode.dmPan;
                m_dragStartPoint = e.Location;
                m_dragStartScrollPosition = this.AutoScrollPosition;
            }
            base.OnMouseDown(e);
            Focus();
        }

        protected override void OnMouseUp(MouseEventArgs e)
        {
            if(m_dragActive == DragMode.dmWindowZoom)
            {
                ControlPaint.DrawReversibleFrame(m_dragWindowRectangle, Color.Black, FrameStyle.Dashed);
                Rectangle r  = RectangleToClient(m_dragWindowRectangle);
                float x1 = absx(r.Left);
                float y1 = absx(r.Top);
                float w = absx(r.Right) - absx(r.Left);
                float h = absy(r.Bottom) - absy(r.Top);
                Fit = false;
                if (Square)
                    DisplayScale = Math.Min((float)this.Width / w, (float)this.Height / h);
                else
                    DisplayScaleXY = new PointF((float)this.Width / w, (float)this.Height / h);

                PointF asPos = abs2scr(new PointF(x1, y1));
                AutoScrollPosition = new Point((int)asPos.X, (int)asPos.Y); 
                m_dragWindowRectangle = new Rectangle() ;
            }
            m_dragActive = DragMode.dmNone;
            base.OnMouseUp(e);
        }

        PointF abs2scr(PointF point)
        {
            return new PointF(point.X * (float)m_scalex + this.AutoScrollPosition.X, point.Y * (float)m_scaley + this.AutoScrollPosition.Y);
        }
        float absx(float x)
        {
            return (x - this.AutoScrollPosition.X) / (float)m_scalex;
        }

        float absy(float y)
        {
            return (y - this.AutoScrollPosition.Y) / (float)m_scaley;
        }

        protected override void OnResize(EventArgs e)
        {
            updateImageParameters();
            Invalidate();
            base.OnResize(e);
        }

        protected override void OnScroll(ScrollEventArgs se)
        {
            Invalidate();
            base.OnScroll(se);
        }

        protected override void OnEnter(EventArgs e)
        {
            Console.WriteLine("Entered");
            base.OnEnter(e);
            Invalidate();
        }

        protected override void OnLeave(EventArgs e)
        {
            Console.WriteLine("Leave");
            base.OnLeave(e);
            Invalidate();
        }
 
        private void updateImageParameters()
        {
            if (m_image != null)
            {
                if (m_fit)
                {
                    if (Square)
                    {
                        m_scalex = Math.Min((float)this.Width / m_image.Width, (float)this.Height / m_image.Height);
                        m_scaley = m_scalex;
                    }
                    else
                    {
                        m_scalex = (float)this.Width / m_image.Width;
                        m_scaley = (float)this.Height / m_image.Height; 
                    }
                }
                AutoScrollMinSize = new Size((int)(m_image.Size.Width * m_scalex), (int)(m_image.Height * m_scaley));
                m_cachedSrcRect = new RectangleF();
                Invalidate();
            }
        }

        public Image Image
        {
            get { return m_image; }
            set
            {
                m_image = value;
                updateImageParameters();
            }
        }

        public List<IImageLayer> Images => m_imageList;

        public List<IOverlayArtwork> Overlays => m_overlayList;

        public float DisplayScale
        {
            get
            {
                if (!Square) throw new Exception("Can't use DisplayScale in non-Square mode");
                return m_scalex;
            }
            set
            {                
                m_scalex = value;
                m_scaley = value;
                updateImageParameters();
            }
        }

        public PointF DisplayScaleXY
        {
            get { return new PointF(m_scalex, m_scaley); }
            set {
                if (Square)
                {
                    m_scalex = value.X;
                    m_scaley = m_scalex;
                } else
                {
                    m_scalex = value.X;
                    m_scaley = value.Y;
                }
                updateImageParameters();
            }
        }

        public float ZoomStep { get; set; } = 1.1f;

        public bool Square { get; set; } = true;

        public bool Fit
        {
            get { return m_fit; }
            set
            {
                m_fit = value;
                updateImageParameters();
            }
        }

        public bool ZoomWindowEnabled { get; set; }

        public void updateImageList()
        {
            if (m_imageList?.Count > 0)
            {
                Image img = m_imageList[0].createImage();
                m_image = new Bitmap(img.Width, img.Height);
                using (Graphics g = Graphics.FromImage(m_image))
                {
                    for (int i = 0; i < m_imageList.Count; i++)
                    {
                        IImageLayer il = m_imageList[i];

                        ColorMatrix CMFade = new ColorMatrix();
                        CMFade.Matrix33 = (float)il.Alpha;
                        ImageAttributes AFade = new ImageAttributes();
                        AFade.SetColorMatrix(CMFade, ColorMatrixFlag.Default, ColorAdjustType.Bitmap);

                        img = il.createImage();
                        g.DrawImage(img,
                             new Rectangle(il.Position, img.Size),
                             0, 0, img.Width, img.Height,
                             GraphicsUnit.Pixel, AFade);
                    }
                }
            }
            else
                m_image = new Bitmap(100, 100);
            updateImageParameters();
        }

        public void AppendToolStrip(ToolStrip toolStrip)
        {
            toolStrip.Items.Add("Fit", null, OnToolstripFit);
            toolStrip.Items.Add("1:1", null, OnToolstrip1to1);
            toolStrip.Items.Add("Z+", null, OnToolstripZoomIn);
            toolStrip.Items.Add("Z-", null, OnToolstripZoomOut);
            btnZoomWindow = new ToolStripButton("Window", null, OnToolstripZoomWindow);
            toolStrip.Items.Add(btnZoomWindow);
        }

        private void OnToolstripZoomWindow(object sender, EventArgs e)
        {
            btnZoomWindow.Checked = !btnZoomWindow.Checked;
            ZoomWindowEnabled = btnZoomWindow.Checked;
        }

        private void OnToolstripZoomOut(object sender, EventArgs e)
        {
            this.Fit = false;
            this.DisplayScaleXY = new PointF(DisplayScaleXY.X / ZoomStep, DisplayScaleXY.Y / ZoomStep);
        }

        private void OnToolstripZoomIn(object sender, EventArgs e)
        {
            this.Fit = false;
            this.DisplayScaleXY = new PointF(DisplayScaleXY.X * ZoomStep, DisplayScaleXY.Y * ZoomStep);
        }

        private void OnToolstrip1to1(object sender, EventArgs e)
        {
            this.Fit = false;
            this.DisplayScale = 1;
        }

        private void OnToolstripFit(object sender, EventArgs e)
        {
            this.Fit = true;
        }
    }
}
