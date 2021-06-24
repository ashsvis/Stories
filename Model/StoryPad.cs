using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Windows.Forms;

namespace Stories.Model
{
    public partial class StoryPad : Panel
    {
        private Rectangle ribbonRect;
        private Point mouseDownLocation;
        private readonly List<Control> elements = new();
        private readonly List<Control> selected = new();

        public event EventHandler<RibbonSelectedEventArgs> OnSelected;

        private void Init()
        {
            DoubleBuffered = true;
        }

        public StoryPad()
        {
            InitializeComponent();
            Init();
        }

        public StoryPad(IContainer container)
        {
            container.Add(this);
            InitializeComponent();
            Init();
        }

        public IEnumerable<Control> Elements => elements;

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);
            foreach (var control in Elements)
            {
                var bounds = control.Bounds;
                using (var bmp = new Bitmap(bounds.Width, bounds.Height))
                {
                    control.DrawToBitmap(bmp, control.ClientRectangle);
                    e.Graphics.DrawImage(bmp, bounds);
                }
            }
            // рисуем рамки для выбранных элементов
            foreach (var control in selected)
            {
                var rect = control.Bounds;
                rect.Width -= 1;
                rect.Height -= 1;
                rect.Inflate(1, 1);
                e.Graphics.DrawRectangle(Pens.Fuchsia, rect);
            }
            // если прямоугольник выбора не пуст
            if (!ribbonRect.IsEmpty)
            {
                // рисуем рамку прямоугольника выбора
                using var pen = new Pen(Color.Fuchsia) { DashStyle = System.Drawing.Drawing2D.DashStyle.Dot };
                e.Graphics.DrawRectangle(pen, ribbonRect);
            }
        }

        protected override void OnMouseDown(MouseEventArgs e)
        {
            base.OnMouseDown(e);
            // обрабатываем событие, если была нажата левая кнопка мышки
            if (e.Button == MouseButtons.Left)
            {
                // обнуление прямоугольника выбора
                ribbonRect = Rectangle.Empty;
                // запоминаем точку первую точку выбора начала рисования прямоугольника выбора
                mouseDownLocation = e.Location;

                selected.Clear();
                Invalidate();
            }
        }

        protected override void OnMouseMove(MouseEventArgs e)
        {
            base.OnMouseMove(e);
            // обрабатываем событие, если была нажата левая кнопка мышки
            if (e.Button == MouseButtons.Left)
            {
                // нормализация параметров для прямоугольника выбора
                // в случае, если мы "растягиваем" прямоугольник не только по "главной" диагонали
                ribbonRect.X = Math.Min(mouseDownLocation.X, e.Location.X);
                ribbonRect.Y = Math.Min(mouseDownLocation.Y, e.Location.Y);
                // размеры должны быть всегда положительные числа
                ribbonRect.Width = Math.Abs(mouseDownLocation.X - e.Location.X);
                ribbonRect.Height = Math.Abs(mouseDownLocation.Y - e.Location.Y);
                // запрашиваем, чтобы обновился
                Invalidate();
            }
        }

        protected override void OnMouseUp(MouseEventArgs e)
        {
            base.OnMouseUp(e);
            if (e.Button == MouseButtons.Left)
            {
                selected.Clear();
                // инициация события при окончании выбора прямоугольником
                if (!ribbonRect.IsEmpty)
                {
                    foreach (var control in elements)
                    {
                        //if (Rectangle.Intersect(control.Bounds, ribbonRect).IsEmpty) continue;
                        if (Rectangle.Intersect(control.Bounds, ribbonRect) != control.Bounds) continue;
                        selected.Add(control);
                    }

                    // создаём объект аргумента для возбуждения события
                    RibbonSelectedEventArgs args = new()
                    {
                        // и передаём выбранный прямоугольник
                        RectangleSelected = ribbonRect
                    };
                    // возбуждаем событие окончания выбора
                    OnRibbonSelected(args);
                    // обнуление прямоугольника выбора
                    ribbonRect = Rectangle.Empty;
                }
                else
                {
                    foreach (var control in elements)
                    {
                        if (control.Bounds.Contains(e.Location))
                        {
                            selected.Add(control);
                            break;
                        }
                    }
                }
                // запрашиваем, чтобы обновился
                Invalidate();
            }
        }

        protected virtual void OnRibbonSelected(RibbonSelectedEventArgs e)
        {
            // если на событие подписались, то вызываем его
            OnSelected?.Invoke(this, e);
        }

        public void LoadData(IEnumerable<Control> controls)
        {
            elements.Clear();
            elements.AddRange(controls);
            Invalidate();
        }

        public void Add(Control element)
        {
            elements.Add(element);
            Invalidate();
        }

        public void Remove(Control element)
        {
            elements.Remove(element);
            Invalidate();
        }
    }

    public class RibbonSelectedEventArgs : EventArgs
    {
        public Rectangle RectangleSelected { get; set; }
    }
}
