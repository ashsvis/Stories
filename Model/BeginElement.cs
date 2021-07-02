﻿using System;
using System.ComponentModel;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Linq;
using System.Windows.Forms;

namespace Stories.Model
{
    [Serializable]
    public partial class BeginElement : StoryElement
    {
        protected virtual void TuningControl()
        {
            Size = new Size(70, 25);
            Text = "Начало";
        }

        public BeginElement()
        {
            InitializeComponent();
            TuningControl();
        }

        public BeginElement(IContainer container)
        {
            container.Add(this);

            InitializeComponent();
            TuningControl();
        }

        protected override void OnResize(EventArgs e)
        {
            base.OnResize(e);
        }

        private GraphicsPath GetAreaPath()
        {
            var path = new GraphicsPath();
            var rect = new RectangleF(0, 0, ClientRectangle.Width - 1, ClientRectangle.Height - 1);
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
                // рисование градиентной заливки
                var point1 = rect.Location;
                var point2 = rect.Location;
                using (var brush = new LinearGradientBrush(PointF.Add(point1, new SizeF(rect.Width / 2, 0)),
                                       PointF.Add(point2, new SizeF(rect.Width / 2, rect.Height)),
                                       SystemColors.ControlDark,
                                       SystemColors.ControlLightLight))
                    gr.FillPath(brush, path);
                // рисование рамки
                using (var pen = new Pen(base.Enabled ? SystemColors.ControlDarkDark : SystemColors.ControlDark))
                    gr.DrawPath(pen, path);
                rect.Inflate(-3, -3);
                // рисование текста
                using (var sf = new StringFormat())
                {
                    sf.Alignment = StringAlignment.Center;
                    sf.LineAlignment = StringAlignment.Center;
                    using (var brush = new SolidBrush(base.Enabled ? SystemColors.WindowText : SystemColors.GrayText))
                        gr.DrawString(Text, Font, brush, rect, sf);
                }
            }
        }

        public bool IsEnd { get; set; }

        /// <summary>
        /// Запрет маркеров входящих связей
        /// </summary>
        /// <param name="rect"></param>
        /// <returns></returns>
        public override Rectangle[] GetInputLinkMarkerRectangles()
        {
            var size = MarkersSize;
            var rect = CorrectBounds();
            var list = new Rectangle[]
            {
                new Rectangle(new Point(rect.X + (rect.Width - size.Width) / 2, rect.Y + size.Height), Size.Empty)
            };
            return list;
        }

    }
}