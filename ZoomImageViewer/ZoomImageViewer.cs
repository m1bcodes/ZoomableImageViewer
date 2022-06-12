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
using System.Drawing.Drawing2D;
using System.Runtime.InteropServices;
using System.Collections.Generic;
using System.Drawing.Imaging;
using System.Collections.ObjectModel;

// ReSharper disable once CheckNamespace
namespace ZoomImageViewer
{

    public delegate void MousePositionChangedEventHandler(object sender, PointF location, object valueUnderCursor);
    public delegate void OverArtworkSelectedEventHandler(IOverlayArtwork sender);
    public delegate void ArtworkChanged(IOverlayArtwork sender);

    /// <summary>
    /// Called  to create a new overlay element, if the user clicks on the drawing area and does not hit another drag handle.
    /// Then it is assumed, a new artwork is to be created via the supplied delegate to OverlayArtworkCreator.
    /// </summary>
    /// <param name="startPoint">location of the mouse click.</param>
    /// <param name="dragHandleIndex">with the drag handle, which is now dragged. For example to create a rectangle, 
    /// the upper left corner is set to startPoint and the handle at the lower right corner is returned</param>
    /// <returns>the new object of type IOverlayArtork.</returns>
    public delegate IOverlayArtwork OverlayArtworkCreator(PointF startPoint, out int dragHandleIndex);

    public class ZoomImageViewer : ScrollableControl
    {
        #region private declarations
        private Image image;
        private readonly List<IImageLayer> imageList = new List<IImageLayer>();
        private readonly ObservableCollection<IOverlayArtwork> overlayList = new ObservableCollection<IOverlayArtwork>();

        private float scaleX = 1;
        private float scaleY = 1;

        private bool fit = true;

        // drag related stuff
        private enum DragMode
        {
            None,
            Pan,
            WindowZoom,
            DragHandle
        };
        private DragMode dragActive = DragMode.None;
        private Point dragStartPoint;
        private Point dragStartScrollPosition;
        private PointF dragHandleMouseOffset;
        private Rectangle dragWindowRectangle ;
        private IOverlayArtwork dragOa;
        private int dragIndex = -1;
        private ToolStripButton btnZoomWindow;

        // image cache
        Image imgCache;
        RectangleF cachedSrcRect;
        RectangleF cachedDstRect;
        #endregion

        public event MousePositionChangedEventHandler MousePositionChanged;
        public event OverArtworkSelectedEventHandler OverlayArtworkSelectionChanged;
        public event ArtworkChanged OverlayArtworkChanged;

        public ZoomImageViewer()
        {
            this.ZoomWindowEnabled = false;
            base.DoubleBuffered = true;

            SetStyle(ControlStyles.Selectable, true);
            this.TabStop = true;

            this.overlayList.CollectionChanged += OverlayListCollectionChanged;
        }

        private void OverlayListCollectionChanged(object sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
        {
            Invalidate();
        }

        [DllImport("user32.dll")]
        // ReSharper disable twice BuiltInTypeReferenceStyle
        public static extern int SendMessage(IntPtr hWnd, Int32 wMsg, bool wParam, Int32 lParam);

        private const int wmSetRedraw = 11;

        public static void SuspendDrawing(Control parent)
        {
            SendMessage(parent.Handle, wmSetRedraw, false, 0);
        }

        public static void ResumeDrawing(Control parent)
        {
            SendMessage(parent.Handle, wmSetRedraw, true, 0);
            parent.Refresh();
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            if (this.image != null)
            {
                Console.WriteLine("OnPaint");
                float srcX = ((-this.AutoScrollPosition.X + e.ClipRectangle.X) / this.scaleX);
                float srcY = ((-this.AutoScrollPosition.Y + e.ClipRectangle.Y) / this.scaleY);
                float srcWidth = (e.ClipRectangle.Width / this.scaleX);
                float srcHeight = (e.ClipRectangle.Height / this.scaleY);

                // image cache mechanism
                RectangleF srcRect = new RectangleF(srcX, srcY, srcWidth, srcHeight);
                Rectangle dstRect = e.ClipRectangle;
                Console.WriteLine("Clip = " + dstRect.ToString());
                if(srcRect != this.cachedSrcRect || dstRect != this.cachedDstRect)
                {
                    if (this.imgCache?.Size != dstRect.Size)
                    {
                        this.imgCache?.Dispose();
                        this.imgCache = new Bitmap(dstRect.Width, dstRect.Height, e.Graphics);
                    }                     
                    using (Graphics g1 = Graphics.FromImage(this.imgCache))
                    {
                        g1.Clear(Color.LightGray);
                        g1.CompositingMode = CompositingMode.SourceOver;
                        g1.CompositingQuality = CompositingQuality.HighSpeed;
                        if (this.scaleX < 1 || this.scaleY < 1)
                        {
                            g1.InterpolationMode = InterpolationMode.Bilinear;
                        }
                        else
                        {
                            g1.InterpolationMode = InterpolationMode.NearestNeighbor;
                        }
                        g1.SmoothingMode = SmoothingMode.HighSpeed;
                        g1.PixelOffsetMode = PixelOffsetMode.HighSpeed;
                        g1.DrawImage(this.image, new Rectangle(0,0, this.imgCache.Width, this.imgCache.Height), srcX, srcY, srcWidth, srcHeight, GraphicsUnit.Pixel);
                        Console.WriteLine("Update image cache" + srcRect.ToString() + ", img cache size=" + this.imgCache.Size) ;
                    }
                }

                this.cachedDstRect = dstRect;
                this.cachedSrcRect = srcRect;

                e.Graphics.DrawImage(this.imgCache, dstRect.Location);

                RectangleF newClipRect = e.Graphics.ClipBounds;
                newClipRect.Width = Math.Min(this.image.Width * this.scaleX, newClipRect.Width);
                newClipRect.Height = Math.Min(this.image.Height * this.scaleY, newClipRect.Height);
                e.Graphics.Clip = new Region(newClipRect);

                Console.WriteLine("Clip rect = " + e.Graphics.ClipBounds.ToString());
                foreach (IOverlayArtwork oa in this.overlayList)
                {
                    if (oa.Status.HasFlag(Status.Visible)) {
                        oa.Paint(e, Abs2Scr);
                    }
                }

                if(this.Focused)
                {
                    Rectangle rc = new Rectangle((int)newClipRect.X, (int)newClipRect.Y, (int)newClipRect.Width, (int)newClipRect.Height) ;
                    rc.Inflate(-2, -2);
                    ControlPaint.DrawFocusRectangle(e.Graphics, rc);
                }
            }
        }

        private bool FindHandle(PointF location, out IOverlayArtwork oa, out int index, out PointF clickOffset)
        {
            foreach (IOverlayArtwork ioa in this.overlayList)
            {
                if (ioa.Status.HasFlag(Status.Enabled | Status.Visible) && ioa.FindHandle(location, Abs2Scr, out index, out clickOffset))
                {
                    oa = ioa;
                    return true;
                }
            }
            index = -1;
            oa = null;
            clickOffset = new PointF();
            return false;
        }

        #region mouse event handlers
        protected override void OnMouseWheel(MouseEventArgs e)
        {
            SuspendDrawing(this);
            Point pClient = e.Location;

            float absX = AbsX(pClient.X);
            float absY = AbsY(pClient.Y);
            if (e.Delta > 0)
            {
                this.scaleX *= this.ZoomStep;
                this.scaleY *= this.ZoomStep;
            }
            if (e.Delta < 0)
            {
                this.scaleX /= this.ZoomStep;
                this.scaleY /= this.ZoomStep;
            }

            this.fit = false;

            UpdateImageParameters();
            this.AutoScrollPosition = new Point(
                (int)(absX * this.scaleX - pClient.X),
                (int)(absY * this.scaleY - pClient.Y));
            Invalidate();
            ResumeDrawing(this);
        }

        protected override void OnMouseMove(MouseEventArgs e)
        {
            PointF loc = new PointF(AbsX(e.Location.X), AbsY(e.Location.Y));
            if (this.image==null || loc.X < 0 || loc.Y < 0 || loc.X >= this.image.Width || loc.Y >= this.image.Height)
            {
                base.OnMouseMove(e);
                return;
            }

            object value = null;
            for(int i= this.imageList.Count-1; i>=0; i--)
            {
                value = this.imageList[i].GetValueUnderCursor(loc);
                if (value != null)
                {
                    break;
                }
            }
            if (this.MousePositionChanged != null)
            {
                this.MousePositionChanged(this, loc, value);
            }

            if (this.dragActive == DragMode.DragHandle)
            {
                this.dragOa.MoveHandle(this.dragIndex, 
                                         new PointF(AbsX(e.Location.X + this.dragHandleMouseOffset.X), AbsY(e.Location.Y + this.dragHandleMouseOffset.Y)));
                Invalidate();
            }
            else if (this.dragActive == DragMode.Pan)
            {
                Point cpos = e.Location;
                this.AutoScrollPosition = new Point(
                    (-this.dragStartScrollPosition.X - (cpos.X - this.dragStartPoint.X) ),
                    (-this.dragStartScrollPosition.Y - (cpos.Y - this.dragStartPoint.Y) ));
                Invalidate();
            }
            else if(this.dragActive== DragMode.WindowZoom)
            {
                ControlPaint.DrawReversibleFrame(this.dragWindowRectangle, Color.Black, FrameStyle.Dashed);
                Point p1 = PointToScreen(this.dragStartPoint);
                Point p2 = PointToScreen(e.Location);
                this.dragWindowRectangle = new Rectangle(p1.X, p1.Y, p2.X - p1.X, p2.Y - p1.Y);
                ControlPaint.DrawReversibleFrame(this.dragWindowRectangle, Color.Black, FrameStyle.Dashed);
            }
            else if(!this.ZoomWindowEnabled)
            {
                this.Cursor = FindHandle(loc, out var oa, out var index, out _) 
                    ? oa.GetCursor(index) 
                    : Cursors.Arrow;
            }
            base.OnMouseMove(e);
        }

        protected override void OnMouseDown(MouseEventArgs e)
        {
            PointF loc = new PointF(AbsX(e.Location.X), AbsY(e.Location.Y));
            if (this.image == null || loc.X < 0 || loc.Y < 0 || loc.X >= this.image.Width || loc.Y >= this.image.Height)
            {
                base.OnMouseMove(e);
                return;
            }
            if (e.Button == MouseButtons.Left)
            {
                if (this.ZoomWindowEnabled)
                {
                    this.dragActive = DragMode.WindowZoom;
                    this.dragStartPoint = e.Location;
                }
                else
                {
                    IOverlayArtwork oa = null;
                    int dragHandleIndex;
                    if (OverlayArtworkCreator != null)
                    {
                        // create new overlay
                        oa = OverlayArtworkCreator(loc, out dragHandleIndex);
                        if (oa != null)
                        {
                            this.Overlays.Add(oa);
                        }
                    } else
                    {
                        // modify existing overlay
                        FindHandle(loc, out oa, out dragHandleIndex, out this.dragHandleMouseOffset);
                    }
                    if (oa != null)
                    {
                        this.dragActive = DragMode.DragHandle;
                        this.dragOa = oa;
                        this.dragIndex = dragHandleIndex;
                        this.OverlayArtworkSelectionChanged?.Invoke(oa);
                        SelectOverlay(oa);
                        Invalidate();
                    }
                }
            }
            else if (e.Button == MouseButtons.Middle)
            {
                this.dragActive = DragMode.Pan;
                this.dragStartPoint = e.Location;
                this.dragStartScrollPosition = this.AutoScrollPosition;
            }
            base.OnMouseDown(e);
            Focus();
        }

        protected override void OnMouseUp(MouseEventArgs e)
        {
            if(this.dragActive == DragMode.WindowZoom)
            {
                ControlPaint.DrawReversibleFrame(this.dragWindowRectangle, Color.Black, FrameStyle.Dashed);
                Rectangle r  = RectangleToClient(this.dragWindowRectangle);
                float x1 = AbsX(r.Left);
                float y1 = AbsX(r.Top);
                float w = AbsX(r.Right) - AbsX(r.Left);
                float h = AbsY(r.Bottom) - AbsY(r.Top);
                this.Fit = false;
                if (this.Square)
                {
                    this.DisplayScale = Math.Min(this.Width / w, this.Height / h);
                }
                else
                {
                    this.DisplayScaleAnisotropic = new PointF(this.Width / w, this.Height / h);
                }
                PointF asPos = Abs2Scr(new PointF(x1, y1));
                this.AutoScrollPosition = new Point((int)asPos.X, (int)asPos.Y);
                this.dragWindowRectangle = new Rectangle() ;
            }
            if(this.dragActive == DragMode.DragHandle)
            {
                if (this.dragOa != null && this.OverlayArtworkChanged != null)
                {
                    this.OverlayArtworkChanged(this.dragOa);
                }
            }

            this.dragActive = DragMode.None;
            base.OnMouseUp(e);
        }
        #endregion

        #region coordinate transformations
        PointF Abs2Scr(PointF point)
        {
            return new PointF(point.X * this.scaleX + this.AutoScrollPosition.X, point.Y * this.scaleY + this.AutoScrollPosition.Y);
        }
        float AbsX(float x)
        {
            return (x - this.AutoScrollPosition.X) / this.scaleX;
        }

        float AbsY(float y)
        {
            return (y - this.AutoScrollPosition.Y) / this.scaleY;
        }
        #endregion

        #region control overrides
        protected override void OnResize(EventArgs e)
        {
            UpdateImageParameters();
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
        #endregion

        private void UpdateImageParameters()
        {
            if (this.image != null)
            {
                if (this.fit)
                {
                    if (this.Square)
                    {
                        this.scaleX = Math.Min((float)this.Width / this.image.Width, (float)this.Height / this.image.Height);
                        this.scaleY = this.scaleX;
                    }
                    else
                    {
                        this.scaleX = (float)this.Width / this.image.Width;
                        this.scaleY = (float)this.Height / this.image.Height; 
                    }
                }

                this.AutoScrollMinSize = new Size((int)(this.image.Size.Width * this.scaleX), (int)(this.image.Height * this.scaleY));
                this.cachedSrcRect = new RectangleF();
                Invalidate();
            }
        }

        public Image Image
        {
            get => this.image;
            set
            {
                this.image = value;
                UpdateImageParameters();
            }
        }

        public List<IImageLayer> Images => this.imageList;

        public ICollection<IOverlayArtwork> Overlays => this.overlayList;

        #region list access

        /// <summary>
        /// deselect all currently selected artworks and select the specified one.
        /// </summary>
        /// <param name="selectedOa">artwork to select. To select none, supply null</param>
        public void SelectOverlay(IOverlayArtwork selectedOa)
        {
            foreach (var oa in this.overlayList)
            {
                oa.Status &= ~Status.Selected;
            }
            if(selectedOa!=null)
            {
                selectedOa.Status |= Status.Selected;
            }
        }

        #endregion

        public float DisplayScale
        {
            get
            {
                if (!this.Square)
                {
                    throw new Exception("Can't use DisplayScale in non-Square mode");
                }
                return this.scaleX;
            }
            set
            {
                this.scaleX = value;
                this.scaleY = value;
                UpdateImageParameters();
            }
        }

        public PointF DisplayScaleAnisotropic
        {
            get => new PointF(this.scaleX, this.scaleY);
            set {
                if (this.Square)
                {
                    this.scaleX = value.X;
                    this.scaleY = this.scaleX;
                } else
                {
                    this.scaleX = value.X;
                    this.scaleY = value.Y;
                }
                UpdateImageParameters();
            }
        }

        public float ZoomStep { get; set; } = 1.1f;

        public bool Square { get; set; } = true;

        public bool Fit
        {
            get => this.fit;
            set
            {
                this.fit = value;
                UpdateImageParameters();
            }
        }

        public bool ZoomWindowEnabled { get; set; }

        public OverlayArtworkCreator OverlayArtworkCreator { get; set; }

        public void UpdateImageList()
        {
            if (this.imageList?.Count > 0)
            {
                Image img = this.imageList[0].CreateImage();
                this.image = new Bitmap(img.Width, img.Height);
                using (Graphics g = Graphics.FromImage(this.image))
                {
                    foreach (IImageLayer il in this.imageList)
                    {
                        ColorMatrix cmFade = new ColorMatrix
                                             {
                                                 Matrix33 = (float)il.Alpha
                                             };
                        ImageAttributes aFade = new ImageAttributes();
                        aFade.SetColorMatrix(cmFade, ColorMatrixFlag.Default, ColorAdjustType.Bitmap);

                        img = il.CreateImage();
                        g.DrawImage(img,
                                    new Rectangle(il.Position, img.Size),
                                    0, 0, img.Width, img.Height,
                                    GraphicsUnit.Pixel, aFade);
                    }
                }
            }
            else
            {
                this.image = new Bitmap(100, 100);
            }
            UpdateImageParameters();
        }

        #region toolstrip
        public void AppendToolStrip(ToolStrip toolStrip)
        {
            toolStrip.Items.Add("Fit", null, OnToolStripFit);
            toolStrip.Items.Add("1:1", null, OnToolStrip1to1);
            toolStrip.Items.Add("Z+", null, OnToolStripZoomIn);
            toolStrip.Items.Add("Z-", null, OnToolStripZoomOut);
            this.btnZoomWindow = new ToolStripButton("Window", null, OnToolStripZoomWindow);
            toolStrip.Items.Add(this.btnZoomWindow);
        }

        private void OnToolStripZoomWindow(object sender, EventArgs e)
        {
            this.btnZoomWindow.Checked = !this.btnZoomWindow.Checked;
            this.ZoomWindowEnabled = this.btnZoomWindow.Checked;
        }

        private void OnToolStripZoomOut(object sender, EventArgs e)
        {
            this.Fit = false;
            this.DisplayScaleAnisotropic = new PointF(this.DisplayScaleAnisotropic.X / this.ZoomStep, this.DisplayScaleAnisotropic.Y / this.ZoomStep);
        }

        private void OnToolStripZoomIn(object sender, EventArgs e)
        {
            this.Fit = false;
            this.DisplayScaleAnisotropic = new PointF(this.DisplayScaleAnisotropic.X * this.ZoomStep, this.DisplayScaleAnisotropic.Y * this.ZoomStep);
        }

        private void OnToolStrip1to1(object sender, EventArgs e)
        {
            this.Fit = false;
            this.DisplayScale = 1;
        }

        private void OnToolStripFit(object sender, EventArgs e)
        {
            this.Fit = true;
        }
        #endregion

    }
}
