using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace ZoomableImageViewer
{
    public class HScaleBar : IOverlayArtwork
    {
        public HScaleBar(float scale, string text, Color color)
        {
            Scale = scale;
            Text = text;
            Color = color;
            MinWidth = 100;
            MaxWidth = 200;
            Height = 20;
            Location = new Point(30, -40);
        }

        public bool findHandle(PointF point, Func<PointF, PointF> abs2scr, out int index, out PointF clickOffset)
        {
            index = -1;
            clickOffset = new PointF();
            return false;
        }

        public Cursor getCursor(int index)
        {
            return null;
        }

        public void moveHandle(int index, PointF newLocation)
        {            
        }

        public virtual void Paint(PaintEventArgs e, Func<PointF, PointF> abs2scr)
        {
            float number;
            float width;
            getScalebarSize(abs2scr, out number, out width);

            float px1, px2, py;
            if (Location.X >= 0) {
                px1 = Location.X;
                px2 = px1 + width;
            }
            else {
                px1 = e.Graphics.ClipBounds.Width + Location.X - width;
                px2 = px1 + width;
            }

            if (Location.Y >= 0) {
                py = Location.Y;
            } else {
                py = e.Graphics.ClipBounds.Height + Location.Y;
            }

            float prefNumber;
            string prefix;
            addPrefix(number, out prefNumber, out prefix);

            Pen p = new Pen(Color, 2);
            e.Graphics.DrawLine(p, px1, py, px2, py);
            e.Graphics.DrawLine(p, px1, py - Height / 2, px1, py + Height / 2);
            e.Graphics.DrawLine(p, px2, py - Height / 2, px2, py + Height / 2);
            StringFormat drawFormat = new StringFormat();
            drawFormat.Alignment = StringAlignment.Center;
            drawFormat.LineAlignment = StringAlignment.Near;
            e.Graphics.DrawString(string.Format(Text, prefNumber, prefix), SystemFonts.DialogFont, new SolidBrush(Color), (px1 + px2) / 2, py + 2, drawFormat);
        }

        protected virtual void getScalebarSize(Func<PointF, PointF> abs2scr, out float number, out float width)
        {
            PointF p0 = abs2scr(new PointF(0, 0));
            PointF p1 = abs2scr(new PointF(1, 0));
            float s1 = p1.X - p0.X;
            float decade = (float)Math.Pow(10,Math.Floor(Math.Log10(MaxWidth / (m_numbers[0] / Scale * s1))));
            float targWidth = (MaxWidth+MinWidth)/ 2;
            float prefMin = 0;
            float minDist = float.MaxValue;
            width = 0;
            number = 0;
            foreach (float x in m_numbers) {
                float dist = Math.Abs(targWidth - decade * x / Scale * s1);
                if (dist < minDist)
                {
                    prefMin = x;
                    minDist = dist;
                    number = decade * x;
                    width = number / Scale * s1;
                }
            }
        }

        protected void addPrefix(float x, out float y, out string prefix)
        {
            y = x;
            prefix = "";
            if (x < m_decades[0] || x>m_decades.Last())
                 return;
            for(int i=0; i<m_decades.Length-1; i++)
            {
                if(m_decades[i] <= x && x <m_decades[i+1])
                {
                    y = x / m_decades[i];
                    prefix = m_prefixes[i];
                }
            }
        }

        /// <summary>
        /// relative location w.r.t. the window size
        /// Use negative numbers to place it relative to the bottom-right corner, 
        /// or positive numbers to place it relative to the top-left corner of the image.
        /// </summary>
        public Point Location { get; set; }

        /// <summary>
        /// the number displayed on the scale bar is the Scale multiplied by the number of pixels.
        /// </summary>
        public float Scale { get; set; }
        public Color Color { get; set; }
        public string Text { get; set; }

        /// <summary>
        /// minimum with of the scale bar in pixels
        /// </summary>
        public int MinWidth { get; set; }
        public int MaxWidth { get; set; }
        public int Height { get; set; }

        protected float[] m_numbers = new float[] { 1, 2, 3, 4, 5 };
        protected float[] m_decades = new float[] { 1e-12f, 1e-9f, 1e-6f, 1e-3f, 1, 1e3f, 1e6f, 1e9f, 1e12f };
        protected string[] m_prefixes = new string[] { "p", "n", "µ", "m", "", "k", "M", "G", "T" };
    }

    public class VScaleBar : HScaleBar
    {
        public VScaleBar(float scale, string text, Color color) : base(scale, text, color)
        {
            Location = new Point(-30, -30);
        }

        protected override void getScalebarSize(Func<PointF, PointF> abs2scr, out float number, out float width)
        {
            Func<PointF, PointF> f = (p) =>
            {
                PointF p1 = new PointF(p.Y, p.X);
                PointF p2 = abs2scr(p1);
                return new PointF(p2.Y, p2.X);
            };

            base.getScalebarSize(f, out number, out width);
        }

        public override void Paint(PaintEventArgs e, Func<PointF, PointF> abs2scr)
        {
            float number;
            float width;
            getScalebarSize(abs2scr, out number, out width);

            float py1, py2, px;
            if (Location.X >= 0)
            {
                px = Location.X;
            }
            else
            {
                px = e.Graphics.ClipBounds.Width + Location.X;
            }

            if (Location.Y >= 0)
            {
                py1 = Location.Y;
                py2 = py1 + width;
            }
            else
            {
                py1 = e.Graphics.ClipBounds.Height + Location.Y - width;
                py2 = py1 + width;
            }

            float prefNumber;
            string prefix;
            addPrefix(number, out prefNumber, out prefix);

            Pen p = new Pen(Color, 2);
            e.Graphics.DrawLine(p, px, py1, px, py2);
            e.Graphics.DrawLine(p, px-Height / 2, py1, px + Height / 2, py1);
            e.Graphics.DrawLine(p, px - Height / 2, py2, px + Height / 2, py2);
            StringFormat drawFormat = new StringFormat();
            drawFormat.Alignment = StringAlignment.Center;
            drawFormat.LineAlignment = StringAlignment.Near;
            var safeTrafo = e.Graphics.Transform;
            e.Graphics.TranslateTransform(px + 2, (py1 + py2) / 2);
            e.Graphics.RotateTransform(-90);
            e.Graphics.DrawString(string.Format(Text, prefNumber, prefix), SystemFonts.DialogFont, new SolidBrush(Color), 0, 0,drawFormat);
            e.Graphics.Transform = safeTrafo;
        }

    }
}
