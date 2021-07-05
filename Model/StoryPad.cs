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
            Resize,
            LinkFromOutput,
            LinkFromInput,
        }

        enum SizeMarkerKind
        {
            None = -1,
            TopLeft = 0,
            Top = 1,
            TopRight = 2,
            Right = 3,
            BottomRight = 4,
            Bottom = 5,
            BottomLeft = 6,
            Left = 7
        }

        private Rectangle ribbonRect;
        private Point[] ribbonLine = new Point[2];
        private Point mouseDownLocation;
        private readonly List<StoryElement> elements = new();
        private readonly List<StoryElement> selected = new();
        private WorkMode workMode = WorkMode.Default;
        private readonly List<Rectangle> dragRects = new();

        private SizeMarkerKind resizedMarker = SizeMarkerKind.None;

        private StoryElement sourceElement = null;
        private StoryElement targetElement = null;
        private LinkMarkerKind linkedInputMarker = LinkMarkerKind.None;
        private LinkMarkerKind linkedOutputMarker = LinkMarkerKind.None;

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

        public IEnumerable<StoryElement> Elements => elements;

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);

            // рисование связей между элементами
            foreach (var element in elements.Where(item => item.Visible))
            {
                element.DrawEdgeLinks(e.Graphics);
            }

            // рисование элементов, размещенных на поверхности
            foreach (var element in elements.Where(item => item.Visible))
            {
                var bounds = element.Bounds;
                using var bmp = new Bitmap(bounds.Width, bounds.Height);
                element.DrawToBitmap(bmp, element.ClientRectangle);
                e.Graphics.DrawImage(bmp, bounds);     
            }

            // рисование маркеров размеров у выбранных элементов
            foreach (var control in elements.Where(item => item.Visible && 
                (workMode == WorkMode.Default || workMode == WorkMode.Drag && !selected.Contains(item))))
            {
                if (selected.Contains(control) && workMode == WorkMode.Default)
                    DrawMarkers(e.Graphics, control);
            }

            // рисуем рамки прямоугольников размера в режиме изменения размера
            using var pen = new Pen(Color.Black) { DashStyle = System.Drawing.Drawing2D.DashStyle.Dot };
            foreach (var rect in dragRects.Where(item => workMode == WorkMode.Resize))
            {
                e.Graphics.DrawRectangle(pen, rect);
            }

            // если прямоугольник выбора не пуст
            if (!ribbonRect.IsEmpty && workMode == WorkMode.Default)
            {
                // рисуем рамку прямоугольника выбора
                e.Graphics.DrawRectangle(pen, ribbonRect);
            }
            // в режиме перетаскивания рисуем со смещением delta
            if (workMode == WorkMode.Drag)
            {
                using (var grayPen = new Pen(Color.Gray, 2f))
                foreach (var control in selected)
                {
                    var r = control.Bounds;
                    r.Offset(delta);
                    using var bmp = new Bitmap(r.Width, r.Height);
                    control.DrawToBitmap(bmp, control.ClientRectangle);
                    e.Graphics.DrawImage(bmp, r);
                    e.Graphics.DrawRectangle(grayPen, r);
                }
            }
            // рисование резиновой линии связи в режиме определения связей
            if (workMode == WorkMode.LinkFromInput || workMode == WorkMode.LinkFromOutput)
            {
                //using var linePen = new Pen(Color.Gray, 2) { DashStyle = System.Drawing.Drawing2D.DashStyle.Dash };
                var linePen = Pens.Black;
                e.Graphics.DrawLines(linePen, ribbonLine);
                var pt0 = workMode == WorkMode.LinkFromOutput ? ribbonLine[0]: ribbonLine[1];
                var pt1 = workMode == WorkMode.LinkFromOutput ? ribbonLine[1] : ribbonLine[0];
                StoryElement.DrawArrow(e.Graphics, pt0, pt1, linePen);
            }
        }

        private static void DrawMarkers(Graphics graphics, StoryElement element)
        {
            Rectangle rect = CorrectRect(element);
            rect.Inflate(3, 3);
            using var pen = new Pen(Color.Black) { DashStyle = System.Drawing.Drawing2D.DashStyle.Dot };
            graphics.DrawRectangle(pen, rect);
            // маркеры размерные
            var sizesList = element.GetSizeMarkerRectangles();
            graphics.FillRectangles(Brushes.White, sizesList);
            graphics.DrawRectangles(Pens.Black, sizesList);
            // маркеры выходных связей
            var linkOutList = element.GetOutputLinkMarkerRectangles();
            graphics.FillRectangles(Brushes.Pink, linkOutList);
            graphics.DrawRectangles(Pens.Black, linkOutList);
            // маркеры входных связей
            var linkInpList = element.GetInputLinkMarkerRectangles();
            graphics.FillRectangles(Brushes.Lime, linkInpList);
            graphics.DrawRectangles(Pens.Black, linkInpList);
        }

        protected override void OnMouseDown(MouseEventArgs e)
        {
            base.OnMouseDown(e);
            workMode = WorkMode.Default;
            dragRects.Clear();

            sourceElement = null;
            targetElement = null;
            linkedInputMarker = LinkMarkerKind.None;
            linkedOutputMarker = LinkMarkerKind.None;

            resizedMarker = SizeMarkerKind.None;
            // обрабатываем событие, если была нажата левая кнопка мышки
            if (e.Button == MouseButtons.Left)
            {
                // обнуление прямоугольника выбора
                ribbonRect = Rectangle.Empty;
                // запоминаем точку первую точку выбора начала рисования прямоугольника выбора
                mouseDownLocation = e.Location;

                OnElementMouseClick(e);

                // проверка, что под курсором есть маркер подключения связи
                foreach (var element in elements.ToArray().Reverse())
                {
                    var list = element.GetOutputLinkMarkerRectangles();
                    for (var i = 0; i < list.Length; i++)
                    {
                        if (list[i].Contains(e.Location))
                        {
                            linkedOutputMarker = (LinkMarkerKind)i;
                            if (element.IsOutputBusy(linkedOutputMarker))
                                return;
                            workMode = WorkMode.LinkFromOutput;
                            var pt = list[i].Location;
                            pt.Offset(list[i].Size.Width / 2, list[i].Size.Height / 2);
                            mouseDownLocation = pt;
                            sourceElement = element;
                            return;
                        }
                    }
                    list = element.GetInputLinkMarkerRectangles();
                    for (var i = 0; i < list.Length; i++)
                    {
                        if (list[i].Contains(e.Location))
                        {
                            linkedInputMarker = (LinkMarkerKind)i;
                            if (element.IsInputBusy(linkedInputMarker))
                                return;
                            workMode = WorkMode.LinkFromInput;
                            var pt = list[i].Location;
                            pt.Offset(list[i].Size.Width / 2, list[i].Size.Height / 2);
                            mouseDownLocation = pt;
                            targetElement = element;
                            return;
                        }
                    }
                }

                // проверка, что под курсором есть маркер изменения размера
                foreach (var element in selected.ToArray().Reverse())
                {
                    var list = element.GetSizeMarkerRectangles();
                    for (var i = 0; i < list.Length; i++)
                    {
                        if (list[i].Contains(e.Location))
                        {
                            workMode = WorkMode.Resize;
                            dragRects.AddRange(selected.Select(item => item.Bounds));
                            resizedMarker = (SizeMarkerKind)i;
                            return;
                        }
                    }
                }

                // проверка, если есть под курсором уже выбранные, тогда добавляем прямоугольники в список для перетаскивания
                foreach (var control in elements.ToArray().Reverse().Where(item => item.Bounds.Contains(e.Location) && selected.Contains(item)))
                {
                    // под курсором есть выбранные элементы
                    workMode = WorkMode.Drag;
                    dragRects.AddRange(selected.ToArray().Reverse().Select(item => item.Bounds));
                    return;
                }

                // проверка, если есть под курсором нет выбранных, тогда очищаем список выбранных и добавляем
                // в список выбора и прямоугольники в список для перетаскивания
                foreach (var control in elements.ToArray().Reverse().Where(item => item.Bounds.Contains(e.Location)))
                {
                    selected.Clear();
                    selected.Add(control);
                    workMode = WorkMode.Drag;
                    dragRects.AddRange(selected.ToArray().Reverse().Select(item => item.Bounds));
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
                delta = new Point(e.X - mouseDownLocation.X, e.Y - mouseDownLocation.Y);
                
                if (workMode == WorkMode.LinkFromInput || workMode == WorkMode.LinkFromOutput)
                {
                    
                    ribbonLine = new Point[] { mouseDownLocation, e.Location };

                    // проверка, что под курсором есть маркер подключения связи
                    foreach (var element in elements.ToArray().Reverse())
                    {
                        var list = workMode == WorkMode.LinkFromInput 
                            ? element.GetOutputLinkMarkerRectangles()
                            : element.GetInputLinkMarkerRectangles();
                        for (var i = 0; i < list.Length; i++)
                        {
                            if (list[i].Contains(e.Location))
                            {
                                if (workMode == WorkMode.LinkFromInput)
                                {
                                    linkedInputMarker = (LinkMarkerKind)i;
                                    sourceElement = element;
                                }
                                else
                                    targetElement = element;
                                if (sourceElement != targetElement)
                                    Cursor = Cursors.Cross;
                                else
                                    Cursor = Cursors.No;
                                Invalidate();
                                return;
                            }
                        }
                    }
                    Cursor = Cursors.Arrow;
                }

                if (workMode == WorkMode.Default || workMode == WorkMode.Drag)
                {
                    // нормализация параметров для прямоугольника выбора
                    // в случае, если мы "растягиваем" прямоугольник не только по "главной" диагонали
                    ribbonRect.X = Math.Min(mouseDownLocation.X, e.Location.X);
                    ribbonRect.Y = Math.Min(mouseDownLocation.Y, e.Location.Y);
                    // размеры должны быть всегда положительные числа
                    ribbonRect.Width = Math.Abs(mouseDownLocation.X - e.Location.X);
                    ribbonRect.Height = Math.Abs(mouseDownLocation.Y - e.Location.Y);

                    // защиты по перемещению (корректируется размер delta в зависимости от ограничений на перемещение)
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
                }
                if (workMode == WorkMode.Resize)
                {
                    DoResizeModeWhenMove();
                }
                // запрашиваем, чтобы обновился
                Invalidate();
            }
        }

        private void DoResizeModeWhenMove()
        {
            dragRects.Clear();
            IEnumerable<Rectangle> rects;
            const int minWidth = 5;
            const int minHeight = 5;
            var dw = selected.Min(item => item.Width) - minWidth;
            var dh = selected.Min(item => item.Height) - minHeight;
            switch (resizedMarker)
            {
                case SizeMarkerKind.TopLeft:
                    rects = selected.Select(item => new Rectangle(item.Left + delta.X, item.Top + delta.Y, item.Width - delta.X, item.Height - delta.Y));
                    if (rects.All(r => r.Height > minHeight && r.Width > minWidth))
                        dragRects.AddRange(rects);
                    else if (rects.All(r => r.Width > minWidth) && rects.Any(r => r.Height <= minHeight))
                        dragRects.AddRange(selected.Select(item => new Rectangle(item.Left + delta.X, item.Top + dh, item.Width - delta.X, item.Height - dh)));
                    else if (rects.All(r => r.Height > minHeight) && rects.Any(r => r.Width <= minWidth))
                        dragRects.AddRange(selected.Select(item => new Rectangle(item.Left + dw, item.Top + delta.Y, item.Width - dw, item.Height - delta.Y)));
                    else
                        dragRects.AddRange(selected.Select(item => new Rectangle(item.Left + dw, item.Top + dh, item.Width - dw, item.Height - dh)));
                    Cursor = Cursors.SizeNWSE;
                    break;
                case SizeMarkerKind.Top:
                    rects = selected.Select(item => new Rectangle(item.Left, item.Top + delta.Y, item.Width, item.Height - delta.Y));
                    dragRects.AddRange(rects.All(r => r.Height > minHeight) ? rects
                        : selected.Select(item => new Rectangle(item.Left, item.Top + dh, item.Width, item.Height - dh)));
                    Cursor = Cursors.SizeNS;
                    break;
                case SizeMarkerKind.TopRight:
                    rects = selected.Select(item => new Rectangle(item.Left, item.Top + delta.Y, item.Width + delta.X, item.Height - delta.Y));
                    if (rects.All(r => r.Height > minHeight && r.Width > minWidth))
                        dragRects.AddRange(rects);
                    else if (rects.All(r => r.Width > minWidth) && rects.Any(r => r.Height <= minHeight))
                        dragRects.AddRange(selected.Select(item => new Rectangle(item.Left, item.Top + dh, item.Width + delta.X, item.Height - dh)));
                    else if (rects.All(r => r.Height > minHeight) && rects.Any(r => r.Width <= minWidth))
                        dragRects.AddRange(selected.Select(item => new Rectangle(item.Left, item.Top + delta.Y, item.Width - dw, item.Height - delta.Y)));
                    else
                        dragRects.AddRange(selected.Select(item => new Rectangle(item.Left, item.Top + dh, item.Width - dw, item.Height - dh)));
                    Cursor = Cursors.SizeNESW;
                    break;
                case SizeMarkerKind.Right:
                    rects = selected.Select(item => new Rectangle(item.Left, item.Top, item.Width + delta.X, item.Height));
                    dragRects.AddRange(rects.All(r => r.Width > minWidth) ? rects
                        : selected.Select(item => new Rectangle(item.Left, item.Top, item.Width - dw, item.Height)));
                    Cursor = Cursors.SizeWE;
                    break;
                case SizeMarkerKind.BottomRight:
                    rects = selected.Select(item => new Rectangle(item.Left, item.Top, item.Width + delta.X, item.Height + delta.Y));
                    if (rects.All(r => r.Height > minHeight && r.Width > minWidth))
                        dragRects.AddRange(rects);
                    else if (rects.All(r => r.Width > minWidth) && rects.Any(r => r.Height <= minHeight))
                        dragRects.AddRange(selected.Select(item => new Rectangle(item.Left, item.Top, item.Width + delta.X, item.Height - dh)));
                    else if (rects.All(r => r.Height > minHeight) && rects.Any(r => r.Width <= minWidth))
                        dragRects.AddRange(selected.Select(item => new Rectangle(item.Left, item.Top, item.Width - dw, item.Height + delta.Y)));
                    else
                        dragRects.AddRange(selected.Select(item => new Rectangle(item.Left, item.Top, item.Width - dw, item.Height - dh)));
                    Cursor = Cursors.SizeNWSE;
                    break;
                case SizeMarkerKind.Bottom:
                    rects = selected.Select(item => new Rectangle(item.Left, item.Top, item.Width, item.Height + delta.Y));
                    dragRects.AddRange(rects.All(r => r.Height > minHeight) ? rects
                        : selected.Select(item => new Rectangle(item.Left, item.Top, item.Width, item.Height - dh)));
                    Cursor = Cursors.SizeNS;
                    break;
                case SizeMarkerKind.BottomLeft:
                    rects = selected.Select(item => new Rectangle(item.Left + delta.X, item.Top, item.Width - delta.X, item.Height + delta.Y));
                    if (rects.All(r => r.Height > minHeight && r.Width > minWidth))
                        dragRects.AddRange(rects);
                    else if (rects.All(r => r.Width > minWidth) && rects.Any(r => r.Height <= minHeight))
                        dragRects.AddRange(selected.Select(item => new Rectangle(item.Left + delta.X, item.Top, item.Width - delta.X, item.Height - dh)));
                    else if (rects.All(r => r.Height > minHeight) && rects.Any(r => r.Width <= minWidth))
                        dragRects.AddRange(selected.Select(item => new Rectangle(item.Left + dw, item.Top, item.Width - dw, item.Height + delta.Y)));
                    else
                        dragRects.AddRange(selected.Select(item => new Rectangle(item.Left + dw, item.Top, item.Width - dw, item.Height - dh)));
                    Cursor = Cursors.SizeNESW;
                    break;
                case SizeMarkerKind.Left:
                    rects = selected.Select(item => new Rectangle(item.Left + delta.X, item.Top, item.Width - delta.X, item.Height));
                    dragRects.AddRange(rects.All(r => r.Width > minWidth) ? rects
                        : selected.Select(item => new Rectangle(item.Left + dw, item.Top, item.Width - dw, item.Height)));
                    Cursor = Cursors.SizeWE;
                    break;
            }
        }

        private static Rectangle CorrectRect(StoryElement control)
        {
            var rect = control.Bounds;
            rect.Width -= 1;
            rect.Height -= 1;
            return rect;
        }

        private void MakeCursorAtMarkers(Point location)
        {
            // проверка на попадание на маркер связи
            foreach (var element in elements)
            {
                var list = new List<Rectangle>();
                list.AddRange(element.GetInputLinkMarkerRectangles());
                list.AddRange(element.GetOutputLinkMarkerRectangles());
                foreach (var r in list)
                {
                    if (r.Contains(location))
                    {
                        Cursor = Cursors.Hand;
                        return;
                    }
                }
            }
            // проверка на попадание на маркер размера
            foreach (var element in selected)
            {
                var sizedMarkers = element.GetSizeMarkerRectangles();
                if (sizedMarkers[0].Contains(location) || sizedMarkers[4].Contains(location))
                {
                    Cursor = Cursors.SizeNWSE;
                    return;
                }
                else if (sizedMarkers[1].Contains(location) || sizedMarkers[5].Contains(location))
                {
                    Cursor = Cursors.SizeNS;
                    return;
                }
                else if (sizedMarkers[2].Contains(location) || sizedMarkers[6].Contains(location))
                {
                    Cursor = Cursors.SizeNESW;
                    return;
                }
                else if (sizedMarkers[3].Contains(location) || sizedMarkers[7].Contains(location))
                {
                    Cursor = Cursors.SizeWE;
                    return;
                }
                else if (element.Bounds.Contains(location))
                {
                    Cursor = Cursors.SizeAll;
                    return;
                }
            }
            // проверка на попадание в тело фигуры
            foreach (var element in elements)
            {
                Rectangle rect = CorrectRect(element);
                if (rect.Contains(location))
                {
                    Cursor = Cursors.SizeAll;
                    return;
                }
            }
            Cursor = Cursors.Default;
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
                else if (workMode == WorkMode.Resize)
                {
                    // был режим изменения размеров
                    for (var i = 0; i < dragRects.Count; i++)
                    {
                        var element = elements.FirstOrDefault(item => item == selected[i]);
                        if (element == null) continue;
                        var pt = dragRects[i].Location;
                        var sz = dragRects[i].Size;
                        element.Location = pt;
                        element.Size = sz;
                    }
                    workMode = WorkMode.Default;
                    dragRects.Clear();
                    OnElementsChanged();
                }
                else if (workMode == WorkMode.Drag)
                {
                    // был режим перетаскивания
                    dragRects.Reverse();
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
                else if (workMode == WorkMode.LinkFromOutput || 
                         workMode == WorkMode.LinkFromInput)
                {
                    // проверка, что под курсором есть маркер подключения связи
                    foreach (var element in elements.ToArray().Reverse())
                    {
                        var list = workMode == WorkMode.LinkFromInput
                            ? element.GetOutputLinkMarkerRectangles()
                            : element.GetInputLinkMarkerRectangles();
                        for (var i = 0; i < list.Length; i++)
                        {
                            if (list[i].Contains(e.Location))
                            {
                                if (workMode == WorkMode.LinkFromInput)
                                {
                                    linkedInputMarker = (LinkMarkerKind)i;
                                    sourceElement = element;
                                }
                                else
                                    targetElement = element;
                                if (sourceElement != targetElement)
                                {
                                    if (workMode == WorkMode.LinkFromOutput)
                                        sourceElement?.DefineTargetLinkTo(targetElement, linkedOutputMarker);
                                    if (workMode == WorkMode.LinkFromInput)
                                        sourceElement?.DefineTargetLinkTo(targetElement, linkedInputMarker);

                                    workMode = WorkMode.Default;
                                    OnElementsChanged();
                                    goto exit;
                                }
                                return;
                            }
                        }
                    }
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

        public void KeyDownExecute(Keys keyCode, Keys modifiers)
        {
            if (keyCode == Keys.Delete)
            {
                var list = new List<StoryElement>(selected);
                var changed = false;
                foreach (var item in list)
                {
                    Remove(item);
                    changed = true;
                }
                if (changed)
                    OnElementsChanged();
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

        public void LoadData(IEnumerable<StoryElement> controls)
        {
            elements.Clear();
            elements.AddRange(controls);
            Invalidate();
        }

        public void Add(StoryElement element)
        {
            elements.Add(element);
            Invalidate();
        }

        public void Remove(StoryElement element)
        {
            elements.Remove(element);
            Invalidate();
        }

        public void Select(StoryElement element)
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
