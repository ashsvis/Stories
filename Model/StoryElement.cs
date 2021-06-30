using System;
using System.ComponentModel;
using System.Windows.Forms;

namespace Stories.Model
{
    [Serializable]
    public class StoryElement : Control
    {
        public StoryElement()
        {
            SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer | ControlStyles.UserPaint | ControlStyles.ResizeRedraw, true);
            base.AutoSize = true;
        }

        protected void CalculateHeight()
        {
            if (!AutoSize) return;
            var text = string.IsNullOrWhiteSpace(Text) ? "Strory Message" : Text;
            using (var graphics = this.CreateGraphics())
            {
                var size = graphics.MeasureString(text, Font, Width);
                Height = (int)(size.Height * 2);
            }
        }

        [Browsable(true), DefaultValue(true)]
        public new bool AutoSize
        {
            get { return base.AutoSize; }
            set
            {
                if (base.AutoSize == value) return;
                base.AutoSize = value;
                if (value)
                    CalculateHeight();
            }
        }

        [Browsable(false)]
        public override AnchorStyles Anchor { get => base.Anchor; set => base.Anchor = value; }

        [Browsable(false)]
        public override Cursor Cursor { get => base.Cursor; set => base.Cursor = value; }

        [Browsable(false)]
        public override DockStyle Dock { get => base.Dock; set => base.Dock = value; }
    }
}