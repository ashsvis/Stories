using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Windows.Forms;

namespace Stories.Model
{
    [Serializable]
    public abstract class StoryElement : Control
    {
        public StoryElement()
        {
            SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer | ControlStyles.UserPaint | ControlStyles.ResizeRedraw, true);
            base.AutoSize = true;
        }

        protected virtual void CalculateHeight()
        {
            if (!AutoSize) return;
            var text = string.IsNullOrWhiteSpace(Text) ? "Strory Element" : Text;
            using (var graphics = this.CreateGraphics())
            {
                var size = graphics.MeasureString(text, Font, Width - 7);
                Height = (int)(size.Height * 2);
            }
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

        public virtual void DefineTargetLinkTo(StoryElement target, LinkMarkerKind _)
        {
            if (target == null) return;
            if (Next != null) return;
            Next = target;
        }

        [Browsable(true), DefaultValue(true)]
        public new bool AutoSize
        {
            get { return base.AutoSize; }
            set
            {
                if (base.AutoSize == value) return;
                base.AutoSize = value;
                CalculateHeight();
            }
        }

        protected override void OnFontChanged(EventArgs e)
        {
            base.OnFontChanged(e);
            CalculateHeight();
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

        [Browsable(false)]
        public override AnchorStyles Anchor { get => base.Anchor; set => base.Anchor = value; }

        [Browsable(false)]
        public override Cursor Cursor { get => base.Cursor; set => base.Cursor = value; }

        [Browsable(false)]
        public override DockStyle Dock { get => base.Dock; set => base.Dock = value; }

        [Browsable(false)]
        public override bool AllowDrop { get => base.AllowDrop; set => base.AllowDrop = value; }

        [Browsable(false)]
        public override ContextMenuStrip ContextMenuStrip { get => base.ContextMenuStrip; set => base.ContextMenuStrip = value; }
    }
}