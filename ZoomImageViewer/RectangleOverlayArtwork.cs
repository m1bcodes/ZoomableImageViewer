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

    public class RectangleOverlayArtwork : IOverlayArtwork
    {
        #region Properties
        private bool allowResize = true;
        public bool AllowResize
        {
            get => this.allowResize;
            set
            {
                this.allowResize = value;
                UpdateHandleVisibility();
            }
        }

        private bool square;
        public bool Square
        {
            get => this.square;
            set
            {
                this.square = value;
                if(this.square)
                {
                    this.height = this.width;
                    RearrangeHandles(-1);
                }
            }
        }      

        private bool showRotateHandle = true;
        public bool ShowRotateHandle
        {
            get => this.showRotateHandle;
            set
            {
                this.showRotateHandle = value;
                UpdateHandleVisibility();
            }
        }

        private bool showCenterHandle = true;
        public bool ShowCenterHandle
        {
            get => this.showCenterHandle;
            set
            {
                this.showCenterHandle = value;
                UpdateHandleVisibility();
            }
        }

        private bool showSideHandles = true;
        public bool ShowSideHandles
        {
            get => this.showSideHandles;
            set
            {
                this.showSideHandles = value;
                UpdateHandleVisibility();
            }
        }

        public float HandleSize
        {
            get => this.dragHandles[0].Size;
            set
            {
                foreach(DragHandle dh in this.dragHandles)
                {
                    dh.Size = value;
                }
            }
        }

        private Color color;
        public Color Color
        {
            get => this.color;
            set
            {
                this.color = value;
                foreach (DragHandle dh in this.dragHandles)
                {
                    dh.Color = this.color;
                }
            }
        }

        public float RotationHandleLength { get; set; } = 50f;

        public Status Status { get; set; }

        public Font Font { get; set; } = SystemFonts.DefaultFont;
        public string Caption { get; set; }
        #endregion

        #region rectangle access
        public RectangleF Rectangle {
            get => new RectangleF(this.centerX - this.width / 2, this.centerY - this.height / 2, this.width, this.height);
            set
            {
                this.centerX = value.X + value.Width / 2;
                this.centerY = value.Y + value.Height / 2;
                this.width = value.Width;
                this.height = value.Height;
                RearrangeHandles(-1);
            }
        }

        public float CenterX
        {
            get => this.centerX;
            set {
                this.centerX = value;
                RearrangeHandles(-1);
            }
        }

        public float CenterY
        {
            get => this.centerY;
            set
            {
                this.centerY = value;
                RearrangeHandles(-1);
            }
        }

        public float Width
        {
            get => this.width;
            set
            {
                this.width = value;
                RearrangeHandles(-1);
            }
        }

        public float Height
        {
            get => this.height;
            set
            {
                this.height = value;
                RearrangeHandles(-1);
            }
        }

        public float Rotation
        {
            get => this.angle;
            set
            {
                this.angle = value;
                RearrangeHandles(-1);
            }
        }
        
        #endregion
        // Handle indices
        private const int hiTopLeft = 0;
        private const int hiTop = 1;
        private const int hiTopRight = 2;
        private const int hiRight = 3;
        private const int hiBottomRight = 4;
        private const int hiBottom = 5;
        private const int hiBottomLeft = 6;
        private const int hiLeft = 7;
        private const int hiCenter = 8;
        private const int hiRotate = 9;
        private const int hiCount = 10; // == number of handles. 

        public RectangleOverlayArtwork(Rectangle rect, float angle, Color color)
        {
            this.Status = Status.Enabled | Status.Visible;

            this.centerX = rect.X + rect.Width / 2;
            this.centerY = rect.Y + rect.Height / 2;
            this.width = rect.Width;
            this.height = rect.Height;
            this.angle = angle;
            this.dragHandles = new DragHandle[hiCount];

            this.dragHandles[hiTopLeft] = this.dhTopLeft = new DragHandle(0, 0, color);
            this.dragHandles[hiTop] = this.dhTop = new DragHandle(0, 0, color);
            this.dragHandles[hiTopRight] = this.dhTopRight = new DragHandle(0, 0, color);
            this.dragHandles[hiRight] = this.dhRight = new DragHandle(0, 0, color);
            this.dragHandles[hiBottomRight] = this.dhBottomRight = new DragHandle(0, 0, color);
            this.dragHandles[hiBottom] = this.dhBottom = new DragHandle(0, 0, color);
            this.dragHandles[hiBottomLeft] = this.dhBottomLeft = new DragHandle(0, 0, color);
            this.dragHandles[hiLeft] = this.dhLeft = new DragHandle(0, 0, color);
            this.dragHandles[hiCenter] = this.dhCenter = new DragHandle(0, 0, color);
            this.dragHandles[hiRotate] = this.dhRotate = new DragHandle(0, 0, color);
            this.color = color;
            RearrangeHandles(-1);
        }

        public void MoveHandle(int index, PointF newLocation)
        {
            this.dragHandles[index].Location = newLocation;
            RearrangeHandles(index);
        }

        public void Paint(PaintEventArgs e, Func<PointF, PointF> fAbs2Scr)
        {
            Pen pen = new Pen(this.color, this.Status.HasFlag(Status.Selected) ? 3 : 1);

            e.Graphics.DrawLine(pen, fAbs2Scr(this.dhTopLeft.Location), fAbs2Scr(this.dhTopRight.Location));
            e.Graphics.DrawLine(pen, fAbs2Scr(this.dhTopRight.Location), fAbs2Scr(this.dhBottomRight.Location));
            e.Graphics.DrawLine(pen, fAbs2Scr(this.dhBottomRight.Location), fAbs2Scr(this.dhBottomLeft.Location));
            e.Graphics.DrawLine(pen, fAbs2Scr(this.dhBottomLeft.Location), fAbs2Scr(this.dhTopLeft.Location));

            if (!string.IsNullOrEmpty(this.Caption))
            {
                Graphics g = e.Graphics;
                var savedTransform = g.Transform;
                g.TranslateTransform(fAbs2Scr(dhTopLeft.Location).X, fAbs2Scr(dhTopLeft.Location).Y);
                g.RotateTransform((float)(Rotation / Math.PI * 180.0));
                g.DrawString(Caption, this.Font, new SolidBrush(Color), 0, -this.Font.Height - HandleSize / 2 - 1);
                g.Transform = savedTransform;
            }

            // don't draw the handles, if artwork is disabled.
            if (this.Status.HasFlag(Status.Enabled))
            {
                this.dhTopLeft.Draw(e.Graphics, fAbs2Scr);
                this.dhTopRight.Draw(e.Graphics, fAbs2Scr);
                this.dhBottomRight.Draw(e.Graphics, fAbs2Scr);
                this.dhBottomLeft.Draw(e.Graphics, fAbs2Scr);
                if (this.ShowSideHandles)
                {
                    this.dhTop.Draw(e.Graphics, fAbs2Scr);
                    this.dhRight.Draw(e.Graphics, fAbs2Scr);
                    this.dhBottom.Draw(e.Graphics, fAbs2Scr);
                    this.dhLeft.Draw(e.Graphics, fAbs2Scr);
                }
                if (this.ShowCenterHandle)
                {
                    this.dhCenter.Draw(e.Graphics, fAbs2Scr);
                }
                if (this.ShowRotateHandle)
                {
                    PointF pr = fAbs2Scr(this.dhRight.Location);
                    PointF pRot = CalcLocationOfRotateHandle(fAbs2Scr);
                    e.Graphics.DrawLine(pen, pr, pRot);
                    this.dhRotate.DrawAtScreenPosition(e.Graphics, pRot, fAbs2Scr);
                }
            }
        }

        private PointF CalcLocationOfRotateHandle(Func<PointF, PointF> fAbs2Scr)
        {
            PointF pr = fAbs2Scr(this.dhRight.Location);
            PointF pCen = fAbs2Scr(this.dhCenter.Location);
            float rad = Distance(pr, pCen);
            PointF pRot = new PointF(pr.X + this.RotationHandleLength * (pr.X - pCen.X) / rad, pr.Y + this.RotationHandleLength * (pr.Y - pCen.Y) / rad); //RotatePoint(new PointF(pr.X + RotationHandleLength, pr.Y), pCen, angle);
            return pRot;
        }

         public bool FindHandle(PointF point, Func<PointF, PointF> fAbs2Scr, out int index, out PointF clickOffset)
        {
            for (int i = 0; i < this.dragHandles.Length; i++)
            {
                if (i == hiRotate && this.ShowRotateHandle)
                {
                    PointF p1 = fAbs2Scr(point);
                    PointF p2 = CalcLocationOfRotateHandle(fAbs2Scr);
                    if (Math.Abs(p1.X - p2.X) <= this.dhRotate.Size / 2 && Math.Abs(p1.Y - p2.Y) <= this.dhRotate.Size / 2)
                    {
                        index = i;
                        clickOffset = new PointF(p2.X - p1.X, p2.Y - p1.Y);
                        return true;
                    }
                }
                else if (this.dragHandles[i].Visible && this.dragHandles[i].Test(point, fAbs2Scr))
                {
                    index = i;
                    PointF p1 = fAbs2Scr(point);
                    PointF p2 = fAbs2Scr(this.dragHandles[i].Location);
                    clickOffset = new PointF(p2.X - p1.X, p2.Y - p1.Y);
                    return true;
                }
            }
            index = -1;
            clickOffset = new PointF();
            return false;
        }

        public Cursor GetCursor(int index)
        {
            return this.dragHandles[index].Cursor;
        }

        private void UpdateHandleVisibility()
        {
            this.dhTopLeft.Visible = this.allowResize;
            this.dhTop.Visible = this.showSideHandles & this.allowResize;
            this.dhTopRight.Visible = this.allowResize;
            this.dhRight.Visible = this.showSideHandles & this.allowResize;
            this.dhBottomRight.Visible = this.allowResize;
            this.dhBottom.Visible = this.showSideHandles & this.allowResize;
            this.dhBottomLeft.Visible = this.allowResize;
            this.dhLeft.Visible = this.showSideHandles & this.allowResize;
            this.dhCenter.Visible = this.showCenterHandle;
            this.dhRotate.Visible = this.showRotateHandle;
        }

        private void RearrangeHandles(int index)
        {
            switch (index)
            {
                case hiRotate:
                    this.angle = (float)Math.Atan2(this.dhRotate.Y - this.centerY, this.dhRotate.X - this.centerX);
                    break;
                case hiCenter:
                    this.centerX = this.dhCenter.X;
                    this.centerY = this.dhCenter.Y;
                    break;

                case hiTopLeft:
                    AdjustSize(this.dhTopLeft.Location, this.dhBottomRight.Location, this.dhCenter.Location, this.angle, AdjustSizeRestriction.None, this.Square, -1.0f, -1.0f, ref this.centerX, ref this.centerY, ref this.width, ref this.height);
                    break;
                case hiTopRight:
                    AdjustSize(this.dhTopRight.Location, this.dhBottomLeft.Location, this.dhCenter.Location, this.angle, AdjustSizeRestriction.None, this.Square, +1.0f, -1.0f, ref this.centerX, ref this.centerY, ref this.width, ref this.height);
                    break;
                case hiBottomRight:
                    AdjustSize(this.dhBottomRight.Location, this.dhTopLeft.Location, this.dhCenter.Location, this.angle, AdjustSizeRestriction.None, this.Square, +1.0f, +1.0f, ref this.centerX, ref this.centerY, ref this.width, ref this.height);
                    break;
                case hiBottomLeft:
                    AdjustSize(this.dhBottomLeft.Location, this.dhTopRight.Location, this.dhCenter.Location, this.angle, AdjustSizeRestriction.None, this.Square, -1.0f, +1.0f, ref this.centerX, ref this.centerY, ref this.width, ref this.height);
                    break;

                case hiTop:
                    AdjustSize(this.dhTop.Location, this.dhBottom.Location, this.dhCenter.Location, this.angle, AdjustSizeRestriction.Height, this.Square, 0.0f, -1.0f, ref this.centerX, ref this.centerY, ref this.width, ref this.height);                    
                    break;
                case hiRight:
                    AdjustSize(this.dhRight.Location, this.dhLeft.Location, this.dhCenter.Location, this.angle, AdjustSizeRestriction.Width, this.Square, +1.0f, 0.0f, ref this.centerX, ref this.centerY, ref this.width, ref this.height);
                    break;
                case hiBottom:
                    AdjustSize(this.dhBottom.Location, this.dhTop.Location, this.dhCenter.Location, this.angle, AdjustSizeRestriction.Height, this.Square, 0.0f, +1.0f, ref this.centerX, ref this.centerY, ref this.width, ref this.height);
                    break;
                case hiLeft:
                    AdjustSize(this.dhLeft.Location, this.dhRight.Location, this.dhCenter.Location, this.angle, AdjustSizeRestriction.Width, this.Square, -1.0f, 0.0f, ref this.centerX, ref this.centerY, ref this.width, ref this.height);
                    break;
            }

            this.dhCenter.Location       = new PointF(this.centerX, this.centerY);
            this.dhTopLeft.Location      = RotatePoint(new PointF(this.centerX - this.width / 2, this.centerY - this.height / 2), this.dhCenter.Location, this.angle);
            this.dhTop.Location          = RotatePoint(new PointF(this.centerX, this.centerY - this.height / 2), this.dhCenter.Location, this.angle);
            this.dhTopRight.Location     = RotatePoint(new PointF(this.centerX + this.width / 2, this.centerY - this.height / 2), this.dhCenter.Location, this.angle);
            this.dhRight.Location        = RotatePoint(new PointF(this.centerX + this.width / 2, this.centerY), this.dhCenter.Location, this.angle);
            this.dhBottomRight.Location  = RotatePoint(new PointF(this.centerX + this.width / 2, this.centerY + this.height / 2), this.dhCenter.Location, this.angle);
            this.dhBottom.Location       = RotatePoint(new PointF(this.centerX, this.centerY + this.height / 2), this.dhCenter.Location, this.angle);
            this.dhBottomLeft.Location   = RotatePoint(new PointF(this.centerX - this.width / 2, this.centerY + this.height / 2), this.dhCenter.Location, this.angle);
            this.dhLeft.Location         = RotatePoint(new PointF(this.centerX - this.width / 2, this.centerY), this.dhCenter.Location, this.angle);
        }

        enum AdjustSizeRestriction
        {
            None,
            Width,
            Height
        };

        /// <summary>
        /// adjust the size of the rectangle, based on the handle that has been dragged.
        /// </summary>
        /// <param name="moveLoc">location of the handle, that is currently dragged.</param>
        /// <param name="fixLoc">location of the opposite handle.</param>
        /// <param name="centerLoc">location of the center of the rectangle</param>
        /// <param name="angle">the rotation angle of the rectangle</param>
        /// <param name="restrictPos">tells if resize is currently restricted to x,y or not restricted</param>
        /// <param name="fixAspect">the aspect ratio is fixed</param>
        /// <param name="wSign">tells the "direction" of the width w.r.t to the handle that is dragged.</param>
        /// <param name="hSign">...same for the height</param>
        /// <param name="xc"></param>
        /// <param name="yc"></param>
        /// <param name="width"></param>
        /// <param name="height"></param>
        private static void AdjustSize(PointF moveLoc, PointF fixLoc, PointF centerLoc, float angle, 
                                       AdjustSizeRestriction restrictPos, bool fixAspect, float wSign, float hSign, ref float xc, ref float yc, ref float width, ref float height)
        {
            bool keepSymmetry = (Control.ModifierKeys & Keys.Control) != Keys.None;
            bool keepAspect = fixAspect || (Control.ModifierKeys & Keys.Shift) != Keys.None;

            float oldWidth = width;
            float oldHeight = height;

            double theta = Math.Atan2(moveLoc.Y - fixLoc.Y, moveLoc.X - fixLoc.X);
            double beta = theta - angle - Math.PI / 2;

            if (keepSymmetry)
            {
                float d = Distance(moveLoc, centerLoc);

                height = 2.0f * (float)Math.Abs(d * Math.Cos(beta));
                width = 2.0f * (float)Math.Abs(d * Math.Sin(beta));

                if (keepAspect)
                {
                    width = Math.Max(width, height);
                    height = width;
                }
                else
                {
                    if (restrictPos == AdjustSizeRestriction.Height)
                    {
                        width = oldWidth;
                    }

                    if (restrictPos == AdjustSizeRestriction.Width)
                    {
                        height = oldHeight;
                    }
                }
            }
            else
            {
                float d = Distance(moveLoc, fixLoc);
                switch (restrictPos)
                {
                    case AdjustSizeRestriction.Width:
                        width = (float)Math.Abs(d * Math.Sin(beta));
                        if (keepAspect)
                        {
                            height = width;
                        }
                        break;
                    case AdjustSizeRestriction.Height:
                        height = (float)Math.Abs(d * Math.Cos(beta));
                        if (keepAspect)
                        {
                            width = height;
                        }
                        break;
                    case AdjustSizeRestriction.None:
                        width = (float)Math.Abs(d * Math.Sin(beta));
                        height = (float)Math.Abs(d * Math.Cos(beta));
                        if (keepAspect)
                        {
                            width = Math.Max(width, height);
                            height = width;
                        }
                        break;
                }
                xc = (float)(fixLoc.X + wSign * width / 2 * Math.Cos(angle) - hSign * height / 2 * Math.Sin(angle));
                yc = (float)(fixLoc.Y + wSign * width / 2 * Math.Sin(angle) + hSign * height / 2 * Math.Cos(angle));
            }
        }

        private readonly DragHandle[] dragHandles;
        private readonly DragHandle dhTopLeft, dhTop, dhTopRight, dhRight, dhBottomRight, dhBottom, dhBottomLeft, dhLeft, dhCenter, dhRotate;

        private float centerX, centerY, width, height, angle;

        private static float Distance(PointF pr, PointF pCen)
        {
            return (float)Math.Sqrt((pr.X - pCen.X) * (pr.X - pCen.X) + (pr.Y - pCen.Y) * (pr.Y - pCen.Y));
        }

        /// <summary>
        /// Rotates one point around another
        /// </summary>
        /// <param name="pointToRotate">The point to rotate.</param>
        /// <param name="centerPoint">The center point of rotation.</param>
        /// <param name="angleInRadians">The rotation angle in degrees.</param>
        /// <returns>Rotated point</returns>
        static PointF RotatePoint(PointF pointToRotate, PointF centerPoint, float angleInRadians)
        {
            double cosTheta = Math.Cos(angleInRadians);
            double sinTheta = Math.Sin(angleInRadians);
            return new PointF
            {
                X =
                    (float)
                    (cosTheta * (pointToRotate.X - centerPoint.X) -
                    sinTheta * (pointToRotate.Y - centerPoint.Y) + centerPoint.X),
                Y =
                    (float)
                    (sinTheta * (pointToRotate.X - centerPoint.X) +
                    cosTheta * (pointToRotate.Y - centerPoint.Y) + centerPoint.Y)
            };
        }

        /// <summary>
        /// default implementation for the ZoomImageViewer.OverlayArtworkCreator
        /// </summary>
        public static RectangleOverlayArtwork RectangleCreator(PointF startPoint, out int handleIndex)
        {
            RectangleOverlayArtwork ra = new RectangleOverlayArtwork(new Rectangle((int)startPoint.X, (int)startPoint.Y, 0, 0), 0, Color.Red);
            handleIndex = RectangleOverlayArtwork.hiBottomRight;
            return ra;
        }
}
}
