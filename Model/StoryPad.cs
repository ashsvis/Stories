using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;

namespace Stories.Model
{
    public partial class StoryPad : Panel
    {
        enum WorkMode
        {
            Default,
            Drag,
            Resize
        }

        private Rectangle ribbonRect;
        private Point mouseDownLocation;
        private readonly List<Control> elements = new();
        private readonly List<Control> selected = new();
        private WorkMode workMode = WorkMode.Default;
        private readonly List<Rectangle> dragRects = new();

        public event EventHandler<RibbonSelectedEventArgs> OnSelected;
        public event EventHandler<EventArgs> OnChanged;

        public event EventHandler<MouseEventArgs> OnElementClick;


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
            // рисование элементов, размещенных на поверхности
            foreach (var control in elements.Where(item => item.Visible && 
                (workMode == WorkMode.Default || workMode == WorkMode.Drag && !selected.Contains(item))).Reverse())
            {
                var bounds = control.Bounds;
                using var bmp = new Bitmap(bounds.Width, bounds.Height);
                control.DrawToBitmap(bmp, control.ClientRectangle);
                e.Graphics.DrawImage(bmp, bounds);
                DrawSpecifics(e, control, bounds);
            }

            // рисование маркеров размеров у выбранных элементов
            foreach (var control in elements.Where(item => item.Visible && 
                (workMode == WorkMode.Default || workMode == WorkMode.Drag && !selected.Contains(item))).Reverse())
            {
                if (selected.Contains(control) && workMode == WorkMode.Default)
                    DrawMarkers(e.Graphics, control);
            }

            // если прямоугольник выбора не пуст
            if (!ribbonRect.IsEmpty && workMode == WorkMode.Default)
            {
                // рисуем рамку прямоугольника выбора
                using var pen = new Pen(Color.Black) { DashStyle = System.Drawing.Drawing2D.DashStyle.Dot };
                e.Graphics.DrawRectangle(pen, ribbonRect);
            }
            // в режиме перетаскивания рисуем со смещением delta
            if (workMode == WorkMode.Drag)
            {
                foreach (var control in selected)
                {
                    var r = control.Bounds;
                    r.Offset(delta);
                    using var bmp = new Bitmap(r.Width, r.Height);
                    control.DrawToBitmap(bmp, control.ClientRectangle);
                    e.Graphics.DrawImage(bmp, r);
                    DrawSpecifics(e, control, r);
                    using (var pen = new Pen(Color.Gray, 2f))
                        e.Graphics.DrawRectangle(pen, r);
                }
            }
        }

        private void DrawMarkers(Graphics graphics, Control control)
        {
            Rectangle rect = CorrectRect(control);
            rect.Inflate(3, 3);
            using var pen = new Pen(Color.Black) { DashStyle = System.Drawing.Drawing2D.DashStyle.Dot };
            graphics.DrawRectangle(pen, rect);
            rect.Inflate(3, 3);
            var list = GetMarkerRectangles(rect);
            graphics.FillRectangles(Brushes.White, list);
            graphics.DrawRectangles(Pens.Black, list);
        }

        private static void DrawSpecifics(PaintEventArgs e, Control control, Rectangle bounds)
        {
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
            workMode = WorkMode.Default;
            dragRects.Clear();
            // обрабатываем событие, если была нажата левая кнопка мышки
            if (e.Button == MouseButtons.Left)
            {
                // обнуление прямоугольника выбора
                ribbonRect = Rectangle.Empty;
                // запоминаем точку первую точку выбора начала рисования прямоугольника выбора
                mouseDownLocation = e.Location;

                OnElementMouseClick(e);

                // проверка, если есть под курсором уже выбранные, тогда добавляем прямоугольники в список для перетаскивания
                foreach (var control in elements.Where(item => item.Bounds.Contains(e.Location) && selected.Contains(item)))
                {
                    // под курсором есть выбранные элементы
                    workMode = WorkMode.Drag;
                    dragRects.AddRange(selected.Select(item => item.Bounds));
                    return;
                }

                // проверка, если есть под курсором нет выбранных, тогдаочищаем список выбранных и добавляем в список выбора и прямоугольники в список для перетаскивания
                foreach (var control in elements.Where(item => item.Bounds.Contains(e.Location)))
                {
                    selected.Clear();
                    selected.Add(control);
                    workMode = WorkMode.Drag;
                    dragRects.AddRange(selected.Select(item => item.Bounds));
                    return;
                }

                // если вообще пусто под курсором
                selected.Clear();
                // запрашиваем, чтобы обновился
                Invalidate();
            }
        }

        private Point delta = new();

        protected override void OnMouseMove(MouseEventArgs e)
        {
            base.OnMouseMove(e);
            
            MakeCursorAtMarkers(e.Location);

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

                // защиты по перемещению (корректируется размер delta взависимости от ограничений на перемещение)
                if (!delta.IsEmpty && selected.Count > 0)
                {
                    Cursor = Cursors.SizeAll;
                    var rect = selected.First().Bounds;
                    selected.ForEach(control => { rect = Rectangle.Union(rect, control.Bounds); });
                    rect.Offset(delta);
                    // защита по левой и верхней сторонам
                    if (rect.Left < 0) delta.X += -rect.Left;
                    if (rect.Top < 0) delta.Y += -rect.Top;
                    // защита по правой и нижней сторонам
                    if (rect.Left + rect.Width > this.Width) delta.X -= rect.Left + rect.Width - this.Width;
                    if (rect.Top + rect.Height > this.Height) delta.Y -= rect.Top + rect.Height - this.Height;
                }

                // запрашиваем, чтобы обновился
                Invalidate();
            }
        }

        private void MakeCursorAtMarkers(Point location)
        {
            foreach (var control in selected)
            {
                Rectangle rect = CorrectRect(control);
                rect.Inflate(6, 6);
                var list = GetMarkerRectangles(rect);
                if (list[0].Contains(location) || list[4].Contains(location))
                {
                    Cursor = Cursors.SizeNWSE;
                    return;
                }
                else if (list[1].Contains(location) || list[5].Contains(location))
                {
                    Cursor = Cursors.SizeNS;
                    return;
                }
                else if (list[2].Contains(location) || list[6].Contains(location))
                {
                    Cursor = Cursors.SizeNESW;
                    return;
                }
                else if (list[3].Contains(location) || list[7].Contains(location))
                {
                    Cursor = Cursors.SizeWE;
                    return;
                }
                else if (rect.Contains(location))
                {
                    Cursor = Cursors.SizeAll;
                    return;
                }
            }
            foreach (var control in elements)
            {
                Rectangle rect = CorrectRect(control);
                if (rect.Contains(location))
                {
                    Cursor = Cursors.SizeAll;
                    return;
                }
            }
            Cursor = Cursors.Default;
        }

        private Rectangle[] GetMarkerRectangles(Rectangle rect)
        {
            var size = new Size(6, 6);
            var list = new Rectangle[]
            {
                new Rectangle(rect.Location,size),
                new Rectangle(new Point(rect.X + (rect.Width - size.Width) / 2, rect.Y), size),
                new Rectangle(new Point(rect.X + rect.Width - size.Width, rect.Y), size),
                new Rectangle(new Point(rect.X + rect.Width - size.Width, rect.Y + (rect.Height - size.Height) / 2), size),
                new Rectangle(new Point(rect.X + rect.Width - size.Width, rect.Y + rect.Height - size.Height), size),
                new Rectangle(new Point(rect.X + (rect.Width - size.Width) / 2, rect.Y + rect.Height - size.Height), size),
                new Rectangle(new Point(rect.X, rect.Y + rect.Height - size.Height), size),
                new Rectangle(new Point(rect.X, rect.Y + (rect.Height - size.Height) / 2), size),
            };
            return list;
        }

        protected override void OnMouseUp(MouseEventArgs e)
        {
            base.OnMouseUp(e);
            if (e.Button == MouseButtons.Left)
            {
                if (workMode == WorkMode.Default)
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
                        goto exit;
                    }
                    else
                    {
                        foreach (var control in elements.Where(item => item.Bounds.Contains(e.Location)))
                        {
                            selected.Add(control);
                            // возбуждаем событие окончания выбора
                            OnRibbonSelected(new(ribbonRect, selected));
                            goto exit;
                        }
                    }
                    OnRibbonSelected(new(ribbonRect, selected));
                }
                else if (delta.IsEmpty)
                    OnRibbonSelected(new(ribbonRect, selected));
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
                    workMode = WorkMode.Default;
                    dragRects.Clear();
                    OnElementsChanged();
                }
                exit:
                workMode = WorkMode.Default;
                delta = Point.Empty;
                // обнуление прямоугольника выбора
                ribbonRect = Rectangle.Empty;
                Cursor = Cursors.Default;
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

        protected virtual void OnElementMouseClick(MouseEventArgs e)
        {
            OnElementClick?.Invoke(this, e);
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

        public void SelectedAlignLefts()
        {
            if (selected.Count < 2) return;
            var left = selected.First().Left;
            foreach (var control in selected)
                control.Left = left;
            Invalidate();
            OnElementsChanged();
        }

        public void SelectedAlignCenters()
        {
            if (selected.Count < 2) return;
            var first = selected.First();
            var center = first.Left + first.Width / 2;
            foreach (var control in selected)
                control.Left = center - control.Width / 2;
            Invalidate();
            OnElementsChanged();
        }

        public void SelectedAlignRights()
        {
            if (selected.Count < 2) return;
            var first = selected.First();
            var right = first.Left + first.Width;
            foreach (var control in selected)
                control.Left = right - control.Width;
            Invalidate();
            OnElementsChanged();
        }

        public void SelectedAlignTops()
        {
            if (selected.Count < 2) return;
            var top = selected.First().Top;
            foreach (var control in selected)
                control.Top = top;
            Invalidate();
            OnElementsChanged();
        }

        public void SelectedAlignMiddles()
        {
            if (selected.Count < 2) return;
            var first = selected.First();
            var middle = first.Top + first.Height / 2;
            foreach (var control in selected)
                control.Top = middle - control.Height / 2;
            Invalidate();
            OnElementsChanged();
        }

        public void SelectedAlignBottoms()
        {
            if (selected.Count < 2) return;
            var first = selected.First();
            var bottom = first.Top + first.Height;
            foreach (var control in selected)
                control.Top = bottom - control.Height;
            Invalidate();
            OnElementsChanged();
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
