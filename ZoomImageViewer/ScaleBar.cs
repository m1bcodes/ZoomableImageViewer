using System;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;

// ReSharper disable once CheckNamespace
namespace ZoomImageViewer
{
    public class HScaleBar : IOverlayArtwork
    {
        public HScaleBar(float scale, string text, Color color)
        {
            this.Status = Status.Visible | Status.Enabled;
            this.Scale = scale;
            this.Text = text;
            this.Color = color;
            this.MinWidth = 100;
            this.MaxWidth = 200;
            this.Height = 20;
            this.Location = new Point(30, -40);
        }

        public bool FindHandle(PointF point, Func<PointF, PointF> fAbs2Scr, out int index, out PointF clickOffset)
        {
            index = -1;
            clickOffset = new PointF();
            return false;
        }

        public Cursor GetCursor(int index)
        {
            return null;
        }

        public void MoveHandle(int index, PointF newLocation)
        {            
        }

        public virtual void Paint(PaintEventArgs e, Func<PointF, PointF> fAbs2Scr)
        {
            GetScaleBarSize(fAbs2Scr, out float number, out float width);

            float px1, px2, py;
            if (this.Location.X >= 0) {
                px1 = this.Location.X;
                px2 = px1 + width;
            }
            else {
                px1 = e.Graphics.ClipBounds.Width + this.Location.X - width;
                px2 = px1 + width;
            }

            if (this.Location.Y >= 0) {
                py = this.Location.Y;
            } else {
                py = e.Graphics.ClipBounds.Height + this.Location.Y;
            }

            AddPrefix(number, out float prefNumber, out string prefix);

            Pen p = new Pen(this.Color, 2);
            e.Graphics.DrawLine(p, px1, py, px2, py);
            e.Graphics.DrawLine(p, px1, py - this.Height / 2.0f, px1, py + this.Height / 2.0f);
            e.Graphics.DrawLine(p, px2, py - this.Height / 2.0f, px2, py + this.Height / 2.0f);
            StringFormat drawFormat = new StringFormat();
            drawFormat.Alignment = StringAlignment.Center;
            drawFormat.LineAlignment = StringAlignment.Near;
            e.Graphics.DrawString(string.Format(this.Text, prefNumber, prefix), SystemFonts.DialogFont, new SolidBrush(this.Color), (px1 + px2) / 2, py + 2, drawFormat);
        }

        protected virtual void GetScaleBarSize(Func<PointF, PointF> fAbs2Scr, out float number, out float width)
        {
            PointF p0 = fAbs2Scr(new PointF(0, 0));
            PointF p1 = fAbs2Scr(new PointF(1, 0));
            float s1 = p1.X - p0.X;
            float decade = (float)Math.Pow(10,Math.Floor(Math.Log10(this.MaxWidth / (this.Numbers[0] / this.Scale * s1))));
            float targetWidth = (this.MaxWidth+ this.MinWidth)/ 2.0f;
            float minDist = float.MaxValue;
            width = 0;
            number = 0;
            foreach (float x in this.Numbers) {
                float dist = Math.Abs(targetWidth - decade * x / this.Scale * s1);
                if (dist < minDist)
                {
                    minDist = dist;
                    number = decade * x;
                    width = number / this.Scale * s1;
                }
            }
        }

        protected void AddPrefix(float x, out float y, out string prefix)
        {
            y = x;
            prefix = "";
            if (x < this.Decades[0] || x> this.Decades.Last())
            {
                return;
            }
            for(int i=0; i< this.Decades.Length-1; i++)
            {
                if(this.Decades[i] <= x && x < this.Decades[i+1])
                {
                    y = x / this.Decades[i];
                    prefix = this.Prefixes[i];
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
        public Status Status { get; set; }

        protected float[] Numbers = new float[] { 1, 2, 3, 4, 5 };
        protected float[] Decades = new float[] { 1e-12f, 1e-9f, 1e-6f, 1e-3f, 1, 1e3f, 1e6f, 1e9f, 1e12f };
        protected string[] Prefixes = new string[] { "p", "n", "µ", "m", "", "k", "M", "G", "T" };
    }

    public class VScaleBar : HScaleBar
    {
        public VScaleBar(float scale, string text, Color color) : base(scale, text, color)
        {
            this.Location = new Point(-30, -30);
        }

        protected override void GetScaleBarSize(Func<PointF, PointF> fAbs2Scr, out float number, out float width)
        {
            Func<PointF, PointF> f = (p) =>
            {
                PointF p1 = new PointF(p.Y, p.X);
                PointF p2 = fAbs2Scr(p1);
                return new PointF(p2.Y, p2.X);
            };

            base.GetScaleBarSize(f, out number, out width);
        }

        public override void Paint(PaintEventArgs e, Func<PointF, PointF> fAbs2Scr)
        {
            GetScaleBarSize(fAbs2Scr, out var number, out var width);

            float py1, py2, px;
            if (this.Location.X >= 0)
            {
                px = this.Location.X;
            }
            else
            {
                px = e.Graphics.ClipBounds.Width + this.Location.X;
            }

            if (this.Location.Y >= 0)
            {
                py1 = this.Location.Y;
                py2 = py1 + width;
            }
            else
            {
                py1 = e.Graphics.ClipBounds.Height + this.Location.Y - width;
                py2 = py1 + width;
            }

            AddPrefix(number, out var prefNumber, out var prefix);

            Pen p = new Pen(this.Color, 2);
            e.Graphics.DrawLine(p, px, py1, px, py2);
            e.Graphics.DrawLine(p, px- this.Height / 2f, py1, px + this.Height / 2f, py1);
            e.Graphics.DrawLine(p, px - this.Height / 2f, py2, px + this.Height / 2f, py2);
            StringFormat drawFormat = new StringFormat();
            drawFormat.Alignment = StringAlignment.Center;
            drawFormat.LineAlignment = StringAlignment.Near;
            var safeTransformation = e.Graphics.Transform;
            e.Graphics.TranslateTransform(px + 2, (py1 + py2) / 2);
            e.Graphics.RotateTransform(-90);
            e.Graphics.DrawString(string.Format(this.Text, prefNumber, prefix), SystemFonts.DialogFont, new SolidBrush(this.Color), 0, 0,drawFormat);
            e.Graphics.Transform = safeTransformation;
        }

    }
}
