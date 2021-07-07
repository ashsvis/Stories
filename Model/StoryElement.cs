using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;

namespace Stories.Model
{
    [Serializable]
    public abstract class StoryElement
    {
        public StoryElement()
        {
            AutoSize = true;
        }

        public string Text { get; set; }

        public int Width { get; set; }
        public int Height { get; set; }

        public Size Size
        {
            get { return new(Width, Height); }
            set 
            {
                Width = value.Width;
                Height = value.Height;
            }
        }

        public int Left { get; set; }
        public int Top { get; set; }

        public Point Location
        {
            get { return new(Left, Top); }
            set
            {
                Left = value.X;
                Top = value.Y;
            }
        }

        public bool Enabled { get; set; } = true;

        public bool Visible { get; set; } = true;

        public Font Font { get; set; } = SystemFonts.CaptionFont;

        public Rectangle Bounds { get => new(Left, Top, Width, Height); }

        public Rectangle ClientRectangle { get => new(0, 0, Width - 1, Height - 1); }

        protected virtual void CalculateHeight()
        {
            if (!AutoSize) return;
            var text = string.IsNullOrWhiteSpace(Text) ? "Strory Element" : Text;
            using var image = new Bitmap(100, 100);
            using var graphics = Graphics.FromImage(image);
            var size = graphics.MeasureString(text, Font, Width - 7);
            Height = (int)(size.Height * 2);
        }

        protected static Size MarkersSize => new(6, 6);

        protected Rectangle CorrectBounds()
        {
            var rect = this.Bounds;
            rect.Width -= 1;
            rect.Height -= 1;
            rect.Inflate(MarkersSize);
            return rect;
        }

        public Rectangle[] GetSizeMarkerRectangles()
        {
            var size = MarkersSize;
            var rect = CorrectBounds();
            var list = new Rectangle[]
            {
                new Rectangle(rect.Location, AutoSize ? Size.Empty : size),
                new Rectangle(new Point(rect.X + (rect.Width - size.Width) / 2, rect.Y), AutoSize ? Size.Empty : size),
                new Rectangle(new Point(rect.X + rect.Width - size.Width, rect.Y), AutoSize ? Size.Empty : size),
                new Rectangle(new Point(rect.X + rect.Width - size.Width, rect.Y + (rect.Height - size.Height) / 2), size),
                new Rectangle(new Point(rect.X + rect.Width - size.Width, rect.Y + rect.Height - size.Height), AutoSize ? Size.Empty : size),
                new Rectangle(new Point(rect.X + (rect.Width - size.Width) / 2, rect.Y + rect.Height - size.Height), AutoSize ? Size.Empty : size),
                new Rectangle(new Point(rect.X, rect.Y + rect.Height - size.Height), AutoSize ? Size.Empty : size),
                new Rectangle(new Point(rect.X, rect.Y + (rect.Height - size.Height) / 2), size),
            };
            return list;
        }

        public virtual Rectangle[] GetOutputLinkMarkerRectangles()
        {
            var size = MarkersSize;
            var rect = CorrectBounds();
            var list = new Rectangle[] { new Rectangle(new Point(rect.X + (rect.Width - size.Width) / 2, rect.Y + rect.Height - size.Height * 2), size) };
            return list;
        }

        public void DrawToBitmap(Bitmap image, Rectangle clientRectangle)
        {
            using var graphics = Graphics.FromImage(image);
            OnPaint(new PaintEventArgs(graphics, clientRectangle));
        }

        private Point[] GetOutputLinkPoints()
        {
            var rect = this.Bounds;
            var list = new Point[] { new Point(rect.X + rect.Width / 2, rect.Y + rect.Height) };
            return list;
        }

        public virtual void DrawEdgeLinks(Graphics graphics)
        {
            if (Next == null) return;
            foreach (var point in this.GetOutputLinkPoints())
                DrawEdgeLink(graphics, point, Next.GetInputLinkPoint());
        }

        private static void DrawEdgeLink(Graphics graphics, Point sourcePoint, Point targetPoint)
        {
            var points = new List<Point> { sourcePoint };
            if (sourcePoint.X != targetPoint.X)
            {
                var rect = new Rectangle(Math.Min(sourcePoint.X, targetPoint.X), Math.Min(sourcePoint.Y, targetPoint.Y),
                                         Math.Abs(sourcePoint.X - targetPoint.X), Math.Abs(sourcePoint.Y - targetPoint.Y));
                var y = rect.Y + rect.Height / 2;
                if (sourcePoint.X < targetPoint.X)
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
            points.Add(targetPoint);
            graphics.DrawLines(Pens.Black, points.ToArray());
            DrawArrow(graphics, points[^2], points[^1]);
        }

        public static void DrawArrow(Graphics graphics, PointF prevPoint, PointF endPoint, Pen linePen = null)
        {
            //расчёт точек стрелки
            var HeadWidth = 10f; // Длина ребер стрелки
            var HeadHeight = 3f; // Ширина между ребрами стрелки

            var theta = Math.Atan2(prevPoint.Y - endPoint.Y, prevPoint.X - endPoint.X);
            var sint = Math.Sin(theta);
            var cost = Math.Cos(theta);

            var pt1 = new PointF((float)(endPoint.X + (HeadWidth * cost - HeadHeight * sint)),
                                 (float)(endPoint.Y + (HeadWidth * sint + HeadHeight * cost)));
            var pt2 = new PointF((float)(endPoint.X + (HeadWidth * cost + HeadHeight * sint)),
                                 (float)(endPoint.Y - (HeadHeight * cost - HeadWidth * sint)));
            var arrow = new PointF[] { pt1, pt2, endPoint, pt1, pt2 };

            var color = linePen != null ? linePen.Color : Color.Black;
            using (var brush = new SolidBrush(color))
                graphics.FillPolygon(brush, arrow);
            graphics.DrawLines(linePen ?? Pens.Black, arrow);
        }

        public virtual Rectangle[] GetInputLinkMarkerRectangles()
        {
            var size = MarkersSize;
            var rect = CorrectBounds();
            var list = new Rectangle[] { new Rectangle(new Point(rect.X + (rect.Width - size.Width) / 2, rect.Y + size.Height), size) };
            return list;
        }

        public Point GetInputLinkPoint()
        {
            var rect = this.Bounds;
            return new Point(rect.X + rect.Width / 2, rect.Y);
        }

        private StoryElement Next { get; set; }

        private StoryElement Prev { get; set; }

        public virtual void DefineTargetLinkTo(StoryElement target, LinkMarkerKind _)
        {
            if (target == null) return;
            if (Next != null) return;
            Next = target;
            target.DefineSourceLinkFrom(this, _);
        }

        public virtual void DefineSourceLinkFrom(StoryElement source, LinkMarkerKind _)
        {
            if (source == null) return;
            if (Prev != null) return;
            Prev = source;
        }

        private bool autoSize;

        public bool AutoSize
        {
            get { return autoSize; }
            set 
            {
                if (autoSize == value) return;
                autoSize = value;
                CalculateHeight();
            }
        }

        protected abstract void OnPaint(PaintEventArgs e);

        public virtual bool IsOutputBusy(LinkMarkerKind _)
        {
            return Next != null;
        }

        public virtual bool IsInputBusy(LinkMarkerKind _)
        {
            return Prev != null;
        }
    }
}