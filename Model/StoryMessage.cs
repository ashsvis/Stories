using System;
using System.ComponentModel;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace Stories.Model
{
    public partial class StoryMessage : Control
    {
        RectangleF rect;

        private void TuningControl()
        {
            SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer | ControlStyles.UserPaint | ControlStyles.ResizeRedraw, true);
            Size = new Size(40, 40);
            TuningSize();
        }

        private void TuningSize()
        {
            //var side = Size.Width > Size.Height ? Size.Height : Size.Width;
            //rect = new RectangleF(0, 0, side - 1, side - 1);
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
            //path.AddEllipse(rect);
            path.AddPath(RoundedRect(rect, 10), true);
            return path;
        }

        public static GraphicsPath RoundedRect(RectangleF bounds, int radius)
        {
            int diameter = radius * 2;
            Size size = new Size(diameter, diameter);
            RectangleF arc = new RectangleF(bounds.Location, size);
            GraphicsPath path = new GraphicsPath();

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
                    gr.DrawString("Story message", SystemFonts.MessageBoxFont, Brushes.Black, rect, sf);
                }
            }
        }
    }
}