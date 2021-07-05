using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace Stories.Model
{
    public partial class IfElement : StoryElement
    {
        private void TuningControl()
        {
            Size = new Size(150, 25);
            Text = "Верно ли это?";
        }

        public IfElement()
        {
            InitializeComponent();
            TuningControl();
        }

        public IfElement(IContainer container)
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

        [DefaultValue(typeof(Size), "150, 52")]
        public new Size Size
        {
            get => base.Size;
            set => base.Size = value;
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
                if (NextYes == null)
                    DrawYesText(gr, new Point((int)(rect.X + rect.Width - 15), (int)(rect.Y + rect.Height / 2 - 3)));
            }
        }

        /// <summary>
        /// Переопределение маркеров исходящих связей
        /// </summary>
        /// <param name="rect"></param>
        /// <returns></returns>
        public override Rectangle[] GetOutputLinkMarkerRectangles()
        {
            var size = MarkersSize;
            var rect = CorrectBounds();
            var list = new Rectangle[]
            {
                new Rectangle(new Point(rect.X + rect.Width - size.Width * 2, rect.Y + (rect.Height - size.Height) / 2), size), // маркер 0 - ДА-ветка
                new Rectangle(new Point(rect.X + (rect.Width - size.Width) / 2, rect.Y + rect.Height - size.Height * 2), size)  // маркер 1 - НЕТ-ветка
            };
            return list;
        }

        private Point GetNextYesLinkPoints()
        {
            var rect = this.Bounds;
            return new Point(rect.X + rect.Width, rect.Y + rect.Height / 2);
        }

        private Point GetNextNoLinkPoints()
        {
            var rect = this.Bounds;
            return new Point(rect.X + rect.Width / 2, rect.Y + rect.Height);
        }

        private StoryElement NextYes { get; set; }

        private StoryElement NextNo { get; set; }

        public override void DefineTargetLinkTo(StoryElement target, LinkMarkerKind linkedOutputMarker)
        {
            if (target == null) return;
            if (linkedOutputMarker == LinkMarkerKind.YesPath)
            {
                if (NextYes != null) return;
                NextYes = target;
                target.DefineSourceLinkFrom(this, linkedOutputMarker);
            }
            else if (linkedOutputMarker == LinkMarkerKind.NoPath)
            {
                if (NextNo != null) return;
                NextNo = target;
                target.DefineSourceLinkFrom(this, linkedOutputMarker);
            }
        }

        public override bool IsOutputBusy(LinkMarkerKind linkedOutputMarker)
        {
            if (linkedOutputMarker == LinkMarkerKind.YesPath)
                return NextYes != null;
            else if (linkedOutputMarker == LinkMarkerKind.NoPath)
                return NextNo != null;
            return false;
        }

        public override void DrawEdgeLinks(Graphics graphics)
        {
            if (NextNo != null)
            {
                var inputLinkPoint = NextNo.GetInputLinkPoint();
                DrawDownEdgeLink(graphics, this.GetNextNoLinkPoints(), inputLinkPoint);
            }
            if (NextYes != null)
            {
                var inputLinkPoint = NextYes.GetInputLinkPoint();
                DrawRightEdgeLink(graphics, this.GetNextYesLinkPoints(), inputLinkPoint);
            }
        }

        private void DrawDownEdgeLink(Graphics graphics, Point srcPoint, Point tarPoint)
        {
            var points = new List<Point> { srcPoint };
            if (srcPoint.X != tarPoint.X)
            {
                var rect = new Rectangle(Math.Min(srcPoint.X, tarPoint.X), Math.Min(srcPoint.Y, tarPoint.Y),
                                         Math.Abs(srcPoint.X - tarPoint.X), Math.Abs(srcPoint.Y - tarPoint.Y));
                var y = rect.Y + rect.Height / 2;
                if (srcPoint.X < tarPoint.X)
                {
                    points.Add(new Point(rect.X, y));
                    points.Add(new Point(rect.X + rect.Width, y));
                }
                else
                {
                    points.Add(new Point(rect.X + rect.Width, y));
                    points.Add(new Point(rect.X, y));
                }
            }
            points.Add(tarPoint);
            graphics.DrawLines(Pens.Black, points.ToArray());
            DrawArrow(graphics, points[^2], points[^1]);
            DrawNoText(graphics, srcPoint);
        }

        private void DrawRightEdgeLink(Graphics graphics, Point srcPoint, Point tarPoint)
        {
            var points = new List<Point> { srcPoint };
            if (srcPoint.X != tarPoint.X)
            {
                var rect = new Rectangle(Math.Min(srcPoint.X, tarPoint.X), Math.Min(srcPoint.Y, tarPoint.Y),
                                         Math.Abs(srcPoint.X - tarPoint.X), Math.Abs(srcPoint.Y - tarPoint.Y));
                if (srcPoint.X < tarPoint.X)
                    points.Add(new Point(rect.X + rect.Width, srcPoint.Y));
                else
                {
                    var y = rect.Y + rect.Height / 2;
                    points.Add(new Point(rect.X + rect.Width, y));
                    points.Add(new Point(rect.X, y));
                }
            }
            points.Add(tarPoint);
            graphics.DrawLines(Pens.Black, points.ToArray());
            DrawArrow(graphics, points[^2], points[^1]);
            DrawYesText(graphics, srcPoint);
        }

        [DefaultValue("Да")]
        public string TextForYes { get; set; } = "Да";

        private void DrawYesText(Graphics graphics, Point srcPoint)
        {
            var text = TextForYes;
            var size = graphics.MeasureString(text, Font);
            var pt = srcPoint;
            pt.Offset(0, -(int)size.Height);
            using (var brush = new SolidBrush(base.Enabled ? SystemColors.WindowText : SystemColors.GrayText))
                graphics.DrawString(text, Font, brush, pt);
        }

        [DefaultValue("Нет")]
        public string TextForNo { get; set; } = "Нет";

        private void DrawNoText(Graphics graphics, Point srcPoint)
        {
            var text = TextForNo;
            using (var brush = new SolidBrush(base.Enabled ? SystemColors.WindowText : SystemColors.GrayText))
                graphics.DrawString(text, Font, brush, srcPoint);
        }
    }
}
