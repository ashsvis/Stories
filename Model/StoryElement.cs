using System;
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

        protected Size MarkersSize => new(6, 6);

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
            var list = new Rectangle[]
            {
                new Rectangle(new Point(rect.X + (rect.Width - size.Width) / 2, rect.Y + rect.Height - size.Height * 2), size)
            };
            return list;
        }

        public virtual Point[] GetOutputLinkPoints()
        {
            var rect = this.Bounds;
            var list = new Point[]
            {
                new Point(rect.X + rect.Width / 2, rect.Y + rect.Height)
            };
            return list;
        }

        public virtual Rectangle[] GetInputLinkMarkerRectangles()
        {
            var size = MarkersSize;
            var rect = CorrectBounds();
            var list = new Rectangle[]
            {
                new Rectangle(new Point(rect.X + (rect.Width - size.Width) / 2, rect.Y + size.Height), size)
            };
            return list;
        }

        public virtual Point[] GetInputLinkPoints()
        {
            var rect = this.Bounds;
            var list = new Point[]
            {
                new Point(rect.X + rect.Width / 2, rect.Y)
            };
            return list;
        }

        public StoryElement Prev { get; set; }

        public StoryElement[] Nexts { get; set; }

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