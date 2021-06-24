﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;

namespace Stories.Model
{
    public partial class StoryPad : Panel
    {
        private Rectangle ribbonRect;
        private Point mouseDownLocation;
        private readonly List<Control> elements = new();
        private readonly List<Control> selected = new();
        private bool dragMode = false;
        private readonly List<Rectangle> dragRects = new();

        public event EventHandler<RibbonSelectedEventArgs> OnSelected;
        public event EventHandler<EventArgs> OnChanged;

        private void Init()
        {
            DoubleBuffered = true;
            BackColor = SystemColors.Control;
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
            foreach (var control in Elements.Where(item => item.Visible))
            {
                var bounds = control.Bounds;
                using var bmp = new Bitmap(bounds.Width, bounds.Height);
                control.DrawToBitmap(bmp, control.ClientRectangle);
                e.Graphics.DrawImage(bmp, bounds);
                if (control is PictureBox pBox)
                {
                    if (pBox.Image == null)
                        using (var pen = new Pen(Color.Black) { DashStyle = System.Drawing.Drawing2D.DashStyle.Dot })
                            e.Graphics.DrawRectangle(pen, CorrectRect(bounds));
                }
                if (control is Label label)
                {
                    if (string.IsNullOrWhiteSpace(label.Text) && label.BorderStyle == BorderStyle.None)
                        using (var pen = new Pen(Color.Black) { DashStyle = System.Drawing.Drawing2D.DashStyle.Dot })
                            e.Graphics.DrawRectangle(pen, CorrectRect(bounds));
                }
            }
            if (!dragMode)
            {
                // рисуем рамки для выбранных элементов
                foreach (var control in selected)
                {
                    Rectangle rect = CorrectRect(control);
                    rect.Inflate(1, 1);
                    e.Graphics.DrawRectangle(Pens.Fuchsia, rect);
                }
            }
            // если прямоугольник выбора не пуст
            if (!ribbonRect.IsEmpty && !dragMode)
            {
                // рисуем рамку прямоугольника выбора
                using var pen = new Pen(Color.Fuchsia) { DashStyle = System.Drawing.Drawing2D.DashStyle.Dot };
                e.Graphics.DrawRectangle(pen, ribbonRect);
            }
            // в режиме перетаскивания рисуем прямоугольники со смещением delta
            if (dragMode)
            {
                foreach (var rect in dragRects)
                {
                    var r = rect;
                    r.Offset(delta);
                    e.Graphics.DrawRectangle(Pens.Fuchsia, r);
                }
            }
        }

        private static Rectangle CorrectRect(Control control)
        {
            var rect = control.Bounds;
            rect.Width -= 1;
            rect.Height -= 1;
            return rect;
        }

        private static Rectangle CorrectRect(Rectangle bounds)
        {
            var rect = bounds;
            rect.Width -= 1;
            rect.Height -= 1;
            return rect;
        }

        protected override void OnMouseDown(MouseEventArgs e)
        {
            base.OnMouseDown(e);
            dragMode = false;
            dragRects.Clear();
            // обрабатываем событие, если была нажата левая кнопка мышки
            if (e.Button == MouseButtons.Left)
            {
                // обнуление прямоугольника выбора
                ribbonRect = Rectangle.Empty;
                // запоминаем точку первую точку выбора начала рисования прямоугольника выбора
                mouseDownLocation = e.Location;

                // проверка, если есть под курсором уже выбранные, тогда добавляем прямоугольники в список для перетаскивания
                foreach (var control in elements)
                {
                    if (control.Bounds.Contains(e.Location) && selected.Contains(control))
                    {
                        // под курсором есть выбранные элементы
                        dragMode = true;
                        dragRects.AddRange(selected.Select(item => item.Bounds));
                        return;
                    }
                }

                selected.Clear();
                // запрашиваем, чтобы обновился
                Invalidate();
            }
        }

        private Point delta = new();

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

                delta = new Point(e.X - mouseDownLocation.X, e.Y - mouseDownLocation.Y);

                // запрашиваем, чтобы обновился
                Invalidate();
            }
        }

        protected override void OnMouseUp(MouseEventArgs e)
        {
            base.OnMouseUp(e);
            if (e.Button == MouseButtons.Left)
            {
                if (!dragMode)
                {
                    selected.Clear();
                    // инициация события при окончании выбора прямоугольником
                    if (!ribbonRect.IsEmpty)
                    {
                        foreach (var control in elements)
                        {
                            if (Rectangle.Intersect(control.Bounds, ribbonRect).IsEmpty) continue;
                            selected.Add(control);
                        }
                        // возбуждаем событие окончания выбора
                        OnRibbonSelected(new(ribbonRect, selected));
                        Invalidate();
                        // обнуление прямоугольника выбора
                        ribbonRect = Rectangle.Empty;
                        return;
                    }
                    else
                    {
                        foreach (var control in elements)
                        {
                            if (control.Bounds.Contains(e.Location))
                            {
                                selected.Add(control);
                                // возбуждаем событие окончания выбора
                                OnRibbonSelected(new(ribbonRect, selected));
                                Invalidate();
                                return;
                            }
                        }
                    }
                    OnRibbonSelected(new(ribbonRect, selected));
                }
                else
                {
                    // был режим перетаскивания
                    for (var i = 0; i < dragRects.Count; i++)
                    {
                        var element = elements.FirstOrDefault(item => item == selected[i]);
                        if (element == null) continue;
                        var pt = dragRects[i].Location;
                        pt.Offset(delta);
                        element.Location = pt;
                    }
                    ribbonRect = Rectangle.Empty;
                    dragMode = false;
                    dragRects.Clear();
                    OnElementsChanged();
                }
                Invalidate();
            }
        }

        protected virtual void OnRibbonSelected(RibbonSelectedEventArgs e)
        {
            // если на событие подписались, то вызываем его
            OnSelected?.Invoke(this, e);
        }

        protected virtual void OnElementsChanged(EventArgs e = null)
        {
            // если на событие подписались, то вызываем его
            OnChanged?.Invoke(this, e ?? new());
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

        public void Select(Control element)
        {
            if (selected.Contains(element)) return;
            selected.Clear();
            selected.AddRange(elements.Where(item => item == element));
            Invalidate();
        }

        public void ClearSelection()
        {
            selected.Clear();
            Invalidate();
        }
    }

    public class RibbonSelectedEventArgs : EventArgs
    {
        public RibbonSelectedEventArgs(Rectangle rectangleSelected, IEnumerable<object> selected)
        {
            RectangleSelected = rectangleSelected;
            Selected = selected;
        }

        public Rectangle RectangleSelected { get; private set; }
        public IEnumerable<object> Selected { get; private set; }
    }
}
