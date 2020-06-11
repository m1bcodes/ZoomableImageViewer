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
using System.Xml.Schema;

namespace ZoomableImageViewer
{    

    public class RectangleOverlayArtwork : IOverlayArtwork
    {
        #region Properties
        private bool m_allowResize = true;
        public bool AllowResize
        {
            get { return m_allowResize; }
            set
            {
                m_allowResize = value;
                updateHandleVisibility();
            }
        }

        bool m_Square = false;
        public bool Square
        {
            get { return m_Square; }
            set
            {
                m_Square = value;
                if(m_Square)
                {
                    m_height = m_width;
                    rearrangeHandles(-1);
                }
            }
        }      

        private bool m_ShowRotateHandle = true;
        public bool ShowRotateHandle
        {
            get { return m_ShowRotateHandle; }
            set
            {
                m_ShowRotateHandle = value;
                updateHandleVisibility();
            }
        }

        private bool m_ShowCenterHandle = true;
        public bool ShowCenterHandle
        {
            get { return m_ShowCenterHandle; }
            set
            {
                m_ShowCenterHandle = value;
                updateHandleVisibility();
            }
        }

        private bool m_ShowSideHandles = true;
        public bool ShowSideHandles
        {
            get { return m_ShowSideHandles; }
            set
            {
                m_ShowSideHandles = value;
                updateHandleVisibility();
            }
        }

        public float HandleSize
        {
            get
            {
                return m_dragHandles[0].Size;
            }
            set
            {
                foreach(DragHandle dh in m_dragHandles)
                {
                    dh.Size = value;
                }
            }
        }

        private Color m_color = Color.Yellow;
        public Color Color
        {
            get
            {
                return m_color;
            }
            set
            {
                m_color = value;
                foreach (DragHandle dh in m_dragHandles)
                {
                    dh.Color = m_color;
                }
            }
        }

        public float RotationHandleLength { get; private set; } = 50f;
        #endregion

        #region rectangle access
        public RectangleF Rectangle {
            get { return new RectangleF(m_xc - m_width / 2, m_yc - m_height / 2, m_width, m_height); }
            set
            {
                m_xc = value.X + value.Width / 2;
                m_yc = value.Y + value.Height / 2;
                m_width = value.Width;
                m_height = value.Height;
                rearrangeHandles(-1);
            }
        }

        public float XC
        {
            get { return m_xc; }
            set { 
                m_xc = value;
                rearrangeHandles(-1);
            }
        }

        public float YC
        {
            get { return m_yc; }
            set
            {
                m_yc = value;
                rearrangeHandles(-1);
            }
        }

        public float Width
        {
            get { return m_width; }
            set
            {
                m_width = value;
                rearrangeHandles(-1);
            }
        }

        public float Height
        {
            get { return m_height; }
            set
            {
                m_height = value;
                rearrangeHandles(-1);
            }
        }

        public float Rotation
        {
            get { return m_angle; }
            set
            {
                m_angle = value;
                rearrangeHandles(-1);
            }
        }
#endregion
        // Handle indices
        const int hiTopLeft = 0;
        const int hiTop = 1;
        const int hiTopRight = 2;
        const int hiRight = 3;
        const int hiBottomRight = 4;
        const int hiBottom = 5;
        const int hiBottomLeft = 6;
        const int hiLeft = 7;
        const int hiCenter = 8;
        const int hiRotate = 9;
        const int hiCount = 10; // == number of handles. 

        public RectangleOverlayArtwork(Rectangle rect, float angle, Color color)
        {
            m_xc = rect.X + rect.Width / 2;
            m_yc = rect.Y + rect.Height / 2;
            m_width = rect.Width;
            m_height = rect.Height;
            m_angle = angle;
            m_dragHandles = new DragHandle[hiCount];

            m_dragHandles[hiTopLeft] = m_topleft = new DragHandle(0, 0, color);
            m_dragHandles[hiTop] = m_top = new DragHandle(0, 0, color);
            m_dragHandles[hiTopRight] = m_topright = new DragHandle(0, 0, color);
            m_dragHandles[hiRight] = m_right = new DragHandle(0, 0, color);
            m_dragHandles[hiBottomRight] = m_bottomright = new DragHandle(0, 0, color);
            m_dragHandles[hiBottom] = m_bottom = new DragHandle(0, 0, color);
            m_dragHandles[hiBottomLeft] = m_bottomleft = new DragHandle(0, 0, color);
            m_dragHandles[hiLeft] = m_left = new DragHandle(0, 0, color);
            m_dragHandles[hiCenter] = m_center = new DragHandle(0, 0, color);
            m_dragHandles[hiRotate] = m_rotate = new DragHandle(0, 0, color);
            m_color = color;
            rearrangeHandles(-1);
        }

        public void moveHandle(int index, PointF newLocation)
        {
            m_dragHandles[index].Location = newLocation;
            rearrangeHandles(index);
        }

        public void Paint(PaintEventArgs e, Func<PointF, PointF> abs2scr, bool selected)
        {
            Pen pen = new Pen(m_color, selected ? 3 : 1);

            e.Graphics.DrawLine(pen, abs2scr(m_topleft.Location), abs2scr(m_topright.Location));
            e.Graphics.DrawLine(pen, abs2scr(m_topright.Location), abs2scr(m_bottomright.Location));
            e.Graphics.DrawLine(pen, abs2scr(m_bottomright.Location), abs2scr(m_bottomleft.Location));
            e.Graphics.DrawLine(pen, abs2scr(m_bottomleft.Location), abs2scr(m_topleft.Location));

            m_topleft.Draw(e.Graphics, abs2scr);
            m_topright.Draw(e.Graphics, abs2scr);
            m_bottomright.Draw(e.Graphics, abs2scr);
            m_bottomleft.Draw(e.Graphics, abs2scr);
            if(ShowSideHandles)
            {
                m_top.Draw(e.Graphics, abs2scr);
                m_right.Draw(e.Graphics, abs2scr);
                m_bottom.Draw(e.Graphics, abs2scr);
                m_left.Draw(e.Graphics, abs2scr);
            }
            if(ShowCenterHandle)
            {
                m_center.Draw(e.Graphics, abs2scr);
            }
            if(ShowRotateHandle)
            {
                PointF pr = abs2scr(m_right.Location);
                PointF prot = calcLocationOfRotateHandle(abs2scr);
                e.Graphics.DrawLine(pen, pr, prot);
                m_rotate.DrawAtScreenPosition(e.Graphics, prot, abs2scr);
            }

        }

        private PointF calcLocationOfRotateHandle(Func<PointF, PointF> abs2scr)
        {
            PointF pr = abs2scr(m_right.Location);
            PointF pcen = abs2scr(m_center.Location);
            float rad = Distance(pr, pcen);
            PointF prot = new PointF(pr.X + RotationHandleLength * (pr.X - pcen.X) / rad, pr.Y + RotationHandleLength * (pr.Y - pcen.Y) / rad); //RotatePoint(new PointF(pr.X + RotationHandleLength, pr.Y), pcen, m_angle);
            return prot;
        }

         public bool findHandle(PointF point, Func<PointF, PointF> abs2scr, out int index, out PointF clickOffset)
        {
            for (int i = 0; i < m_dragHandles.Length; i++)
            {
                if (i == hiRotate && ShowRotateHandle)
                {
                    PointF p1 = abs2scr(point);
                    PointF p2 = calcLocationOfRotateHandle(abs2scr);
                    if (Math.Abs(p1.X - p2.X) <= m_rotate.Size / 2 && Math.Abs(p1.Y - p2.Y) <= m_rotate.Size / 2)
                    {
                        index = i;
                        clickOffset = new PointF(p2.X - p1.X, p2.Y - p1.Y);
                        return true;
                    }
                }
                else if (m_dragHandles[i].Visible && m_dragHandles[i].Test(point, abs2scr))
                {
                    index = i;
                    PointF p1 = abs2scr(point);
                    PointF p2 = abs2scr(m_dragHandles[i].Location);
                    clickOffset = new PointF(p2.X - p1.X, p2.Y - p1.Y);
                    return true;
                }
            }
            index = -1;
            clickOffset = new PointF();
            return false;
        }

        public Cursor getCursor(int index)
        {
            return m_dragHandles[index].Cursor;
        }

        private void updateHandleVisibility()
        {
            m_topleft.Visible = m_allowResize;
            m_top.Visible = m_ShowSideHandles & m_allowResize;
            m_topright.Visible = m_allowResize;
            m_right.Visible = m_ShowSideHandles & m_allowResize;
            m_bottomright.Visible = m_allowResize;
            m_bottom.Visible = m_ShowSideHandles & m_allowResize;
            m_bottomleft.Visible = m_allowResize;
            m_left.Visible = m_ShowSideHandles & m_allowResize;
            m_center.Visible = m_ShowCenterHandle;
            m_rotate.Visible = m_ShowRotateHandle;
        }

        private void rearrangeHandles(int index)
        {
            float dummy = 0.0f;
            switch (index)
            {
                case hiRotate:
                    m_angle = (float)Math.Atan2((float)(m_rotate.Y - m_yc), (float)(m_rotate.X - m_xc));
                    break;
                case hiCenter:
                    m_xc = m_center.X;
                    m_yc = m_center.Y;
                    break;

                case hiTopLeft:
                    adjustSize(m_topleft.Location, m_bottomright.Location, m_center.Location, m_angle, AdjustSizeRestriction.None, Square, -1.0f, -1.0f, ref m_xc, ref m_yc, ref m_width, ref m_height);
                    break;
                case hiTopRight:
                    adjustSize(m_topright.Location, m_bottomleft.Location, m_center.Location, m_angle, AdjustSizeRestriction.None, Square, +1.0f, -1.0f, ref m_xc, ref m_yc, ref m_width, ref m_height);
                    break;
                case hiBottomRight:
                    adjustSize(m_bottomright.Location, m_topleft.Location, m_center.Location, m_angle, AdjustSizeRestriction.None, Square, +1.0f, +1.0f, ref m_xc, ref m_yc, ref m_width, ref m_height);
                    break;
                case hiBottomLeft:
                    adjustSize(m_bottomleft.Location, m_topright.Location, m_center.Location, m_angle, AdjustSizeRestriction.None, Square, -1.0f, +1.0f, ref m_xc, ref m_yc, ref m_width, ref m_height);
                    break;

                case hiTop:
                    adjustSize(m_top.Location, m_bottom.Location, m_center.Location, m_angle, AdjustSizeRestriction.Height, Square, 0.0f, -1.0f, ref m_xc, ref m_yc, ref m_width, ref m_height);                    
                    break;
                case hiRight:
                    adjustSize(m_right.Location, m_left.Location, m_center.Location, m_angle, AdjustSizeRestriction.Width, Square, +1.0f, 0.0f, ref m_xc, ref m_yc, ref m_width, ref m_height);
                    break;
                case hiBottom:
                    adjustSize(m_bottom.Location, m_top.Location, m_center.Location, m_angle, AdjustSizeRestriction.Height, Square, 0.0f, +1.0f, ref m_xc, ref m_yc, ref m_width, ref m_height);
                    break;
                case hiLeft:
                    adjustSize(m_left.Location, m_right.Location, m_center.Location, m_angle, AdjustSizeRestriction.Width, Square, -1.0f, 0.0f, ref m_xc, ref m_yc, ref m_width, ref m_height);
                    break;
            }

            m_center.Location       = new PointF(m_xc, m_yc);
            m_topleft.Location      = RotatePoint(new PointF(m_xc - m_width / 2, m_yc - m_height / 2), m_center.Location, m_angle);
            m_top.Location          = RotatePoint(new PointF(m_xc, m_yc - m_height / 2), m_center.Location, m_angle);
            m_topright.Location     = RotatePoint(new PointF(m_xc + m_width / 2, m_yc - m_height / 2), m_center.Location, m_angle);
            m_right.Location        = RotatePoint(new PointF(m_xc + m_width / 2, m_yc), m_center.Location, m_angle);
            m_bottomright.Location  = RotatePoint(new PointF(m_xc + m_width / 2, m_yc + m_height / 2), m_center.Location, m_angle);
            m_bottom.Location       = RotatePoint(new PointF(m_xc, m_yc + m_height / 2), m_center.Location, m_angle);
            m_bottomleft.Location   = RotatePoint(new PointF(m_xc - m_width / 2, m_yc + m_height / 2), m_center.Location, m_angle);
            m_left.Location         = RotatePoint(new PointF(m_xc - m_width / 2, m_yc), m_center.Location, m_angle);
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
        /// <param name="wsign">tells the "direction" of the width w.r.t to the handle that is dragged.</param>
        /// <param name="hsign">...same for the height</param>
        /// <param name="m_xc"></param>
        /// <param name="m_yc"></param>
        /// <param name="m_width"></param>
        /// <param name="m_height"></param>
        private static void adjustSize(PointF moveLoc, PointF fixLoc, PointF centerLoc, float angle, 
            AdjustSizeRestriction restrictPos, bool fixAspect, float wsign, float hsign, ref float m_xc, ref float m_yc, ref float m_width, ref float m_height)
        {
            bool keepSymmetry = (Control.ModifierKeys & Keys.Control) != Keys.None;
            bool keepAspect = fixAspect || (Control.ModifierKeys & Keys.Shift) != Keys.None;

            float oldWidth = m_width;
            float oldHeight = m_height;

            double theta = Math.Atan2(moveLoc.Y - fixLoc.Y, moveLoc.X - fixLoc.X);
            double beta = theta - angle - Math.PI / 2;

            if (keepSymmetry)
            {
                float d = Distance(moveLoc, centerLoc);

                m_height = 2.0f * (float)Math.Abs(d * Math.Cos(beta));
                m_width = 2.0f * (float)Math.Abs(d * Math.Sin(beta));

                if (keepAspect)
                {
                    m_width = Math.Max(m_width, m_height);
                    m_height = m_width;
                }
                else
                {
                    if (restrictPos == AdjustSizeRestriction.Height)
                        m_width = oldWidth;
                    if (restrictPos == AdjustSizeRestriction.Width)
                        m_height = oldHeight;
                }
            }
            else
            {
                float d = Distance(moveLoc, fixLoc);
                switch (restrictPos)
                {
                    case AdjustSizeRestriction.Width:
                        m_width = (float)Math.Abs(d * Math.Sin(beta));
                        if (keepAspect) m_height = m_width;
                        break;
                    case AdjustSizeRestriction.Height:
                        m_height = (float)Math.Abs(d * Math.Cos(beta));
                        if (keepAspect) m_width = m_height;
                        break;
                    case AdjustSizeRestriction.None:
                        m_width = (float)Math.Abs(d * Math.Sin(beta));
                        m_height = (float)Math.Abs(d * Math.Cos(beta));
                        if (keepAspect)
                        {
                            m_width = Math.Max(m_width, m_height);
                            m_height = m_width;
                        }
                        break;
                }
                m_xc = (float)(fixLoc.X + wsign * m_width / 2 * Math.Cos(angle) - hsign * m_height / 2 * Math.Sin(angle));
                m_yc = (float)(fixLoc.Y + wsign * m_width / 2 * Math.Sin(angle) + hsign * m_height / 2 * Math.Cos(angle));
            }
        }

        private DragHandle[] m_dragHandles;
        private DragHandle m_topleft, m_top, m_topright, m_right, m_bottomright, m_bottom, m_bottomleft, m_left, m_center, m_rotate;

        private float m_xc, m_yc, m_width, m_height, m_angle;

        private static float Distance(PointF pr, PointF pcen)
        {
            return (float)Math.Sqrt((double)((pr.X - pcen.X) * (pr.X - pcen.X) + (pr.Y - pcen.Y) * (pr.Y - pcen.Y)));
        }

        /// <summary>
        /// Rotates one point around another
        /// </summary>
        /// <param name="pointToRotate">The point to rotate.</param>
        /// <param name="centerPoint">The center point of rotation.</param>
        /// <param name="angleInDegrees">The rotation angle in degrees.</param>
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
    }
}
