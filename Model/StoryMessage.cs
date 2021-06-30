using System;
using System.ComponentModel;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace Stories.Model
{
    [Serializable]
    public partial class StoryMessage : StoryElement
    {
        RectangleF rect;

        private void TuningControl()
        {
            Size = new Size(160, 40);
            TuningSize();
        }

        private void TuningSize()
        {
            rect = new RectangleF(0, 0, Size.Width - 1, Size.Height - 1);
        }

        public StoryMessage()
        {
            InitializeComponent();
            TuningControl();
        }

        public StoryMessage(IContainer container)
        {
            container.Add(this);

            InitializeComponent();
            TuningControl();
        }

        protected override void OnResize(EventArgs e)
        {
            base.OnResize(e);
            TuningSize();
        }

        private GraphicsPath GetAreaPath()
        {
            var path = new GraphicsPath();
            path.AddPath(RoundedRect(rect, Math.Min(Width / 2, Height / 2)), true);
            return path;
        }

        public static GraphicsPath RoundedRect(RectangleF bounds, int radius)
        {
            int diameter = radius * 2;
            Size size = new(diameter, diameter);
            RectangleF arc = new(bounds.Location, size);
            GraphicsPath path = new();

            if (radius == 0)
            {
                path.AddRectangle(bounds);
                return path;
            }

            // top left arc  
            path.AddArc(arc, 180, 90);

            // top right arc  
            arc.X = bounds.Right - diameter;
            path.AddArc(arc, 270, 90);

            // bottom right arc  
            arc.Y = bounds.Bottom - diameter;
            path.AddArc(arc, 0, 90);

            // bottom left arc 
            arc.X = bounds.Left;
            path.AddArc(arc, 90, 90);

            path.CloseFigure();
            return path;
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);
            var gr = e.Graphics;
            using (var path = GetAreaPath())
            {
                var rect = path.GetBounds();
                var point1 = rect.Location;
                var point2 = rect.Location;
                using (var brush = new LinearGradientBrush(PointF.Add(point1, new SizeF(rect.Width / 2, 0)),
                                       PointF.Add(point2, new SizeF(rect.Width / 2, rect.Height)),
                                       SystemColors.ControlDark,
                                       SystemColors.ControlLightLight))
                    gr.FillPath(brush, path);
                using (var pen = new Pen(base.Enabled ? SystemColors.ControlDarkDark : SystemColors.InactiveCaption))
                    gr.DrawPath(pen, path);
                rect.Inflate(-3, -3);
                using (var sf = new StringFormat())
                {
                    sf.Alignment = StringAlignment.Center;
                    sf.LineAlignment = StringAlignment.Center;
                    gr.DrawString(Text, Font, Brushes.Black, rect, sf);
                }
            }
        }

        protected override void OnTextChanged(EventArgs e)
        {
            base.OnTextChanged(e);
            CalculateHeight();
        }

        protected override void OnSizeChanged(EventArgs e)
        {
            base.OnSizeChanged(e);
            CalculateHeight();
        }
    }
}