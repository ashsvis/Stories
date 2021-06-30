﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Stories.Model
{
    public partial class IfSelector : StoryElement
    {
        private void TuningControl()
        {
            Size = new Size(150, 25);
            Text = "Верно ли это?";
        }

        public IfSelector()
        {
            InitializeComponent();
            TuningControl();
        }

        public IfSelector(IContainer container)
        {
            container.Add(this);

            InitializeComponent();
            TuningControl();
        }

        private GraphicsPath GetAreaPath()
        {
            var path = new GraphicsPath();
            var rect = new RectangleF(0, 0, ClientRectangle.Width - 1, ClientRectangle.Height - 1);
            path.AddPolygon(new PointF[] 
            { 
                new PointF(rect.Left + rect.Width / 2, rect.Top),
                new PointF(rect.Left + rect.Width, rect.Top + rect.Height / 2),
                new PointF(rect.Left + rect.Width / 2, rect.Top + rect.Height),
                new PointF(rect.Left, rect.Top + rect.Height / 2)
            });
            return path;
        }

        protected override void CalculateHeight()
        {
            if (!AutoSize) return;
            var text = string.IsNullOrWhiteSpace(Text) ? "Верно ли это?" : Text;
            using (var graphics = this.CreateGraphics())
            {
                var size = graphics.MeasureString(text, Font, Width - 7);
                Height = (int)(size.Height * 3);
            }
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
                using (var pen = new Pen(base.Enabled ? SystemColors.ControlDarkDark : SystemColors.ControlDark))
                    gr.DrawPath(pen, path);
                rect.Inflate(-3, -3);
                using (var sf = new StringFormat())
                {
                    sf.Alignment = StringAlignment.Center;
                    sf.LineAlignment = StringAlignment.Center;
                    using (var brush = new SolidBrush(base.Enabled ? SystemColors.WindowText : SystemColors.GrayText))
                        gr.DrawString(Text, Font, brush, rect, sf);
                }
            }
        }
    }
}