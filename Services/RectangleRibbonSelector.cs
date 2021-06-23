using System;
using System.Drawing;
using System.Windows.Forms;

namespace Stories.Services
{
    public class RibbonSelectedEventArgs : EventArgs
    {
        public Rectangle RectangleSelected { get; set; }
    }

    public class RectangleRibbonSelector
    {
        private readonly Control _container;
        private readonly Pen _borderPen;
        private readonly Brush _fillBrush;
        private Point _mouseDownLocation = Point.Empty;
        private Rectangle _ribbonRect = new Rectangle();

        public event EventHandler<RibbonSelectedEventArgs> OnSelected;

        /// <summary>
        /// Конструктор с параметрами для перехвата и обработки событий
        /// </summary>
        /// <param name="container">визуальный компонент с поверхностью для рисования</param>
        /// <param name="borderPen">карандаш для рисования рамки</param>
        /// <param name="fillBrush">кисть для закрашивания рамки</param>
        public RectangleRibbonSelector(Control container, Pen borderPen = null, Brush fillBrush = null)
        {
            // запоминаем ссылку на контейнер для рисования
            _container = container;
            // по умолчанию рамка пунктирная, чёрного цвета
            _borderPen = borderPen ?? new Pen(Color.Black) { DashStyle = System.Drawing.Drawing2D.DashStyle.Dot };
            _fillBrush = fillBrush;
            // подключаемся к необходимым событиям на контейнере
            _container.MouseDown += Control_MouseDown;
            _container.MouseMove += _control_MouseMove;
            _container.MouseUp += Control_MouseUp;
            _container.Paint += _control_Paint;
        }

        /// <summary>
        /// Обработчик события рисования на поверхности контейнера
        /// </summary>
        /// <param name="sender">визуальный компонент с поверхностью для рисования</param>
        /// <param name="e">объект параметров события со свойством Graphics</param>
        private void _control_Paint(object sender, PaintEventArgs e)
        {
            // если прямоугольник выбора не пуст
            if (!_ribbonRect.IsEmpty)
            {
                // если указана кисть заливки
                if (_fillBrush != null)
                    e.Graphics.FillRectangle(_fillBrush, _ribbonRect); // то выполняем закрашивание
                // рисуем рамку прямоугольника выбора
                e.Graphics.DrawRectangle(_borderPen, _ribbonRect);
            }
        }

        /// <summary>
        /// Обработчик события нажатия кнопки мышки
        /// </summary>
        /// <param name="sender">визуальный компонент с поверхностью для рисования</param>
        /// <param name="e">объект параметров события со свойством Location</param>
        private void Control_MouseDown(object sender, MouseEventArgs e)
        {
            // обрабатываем событие, если была нажата левая кнопка мышки
            if (e.Button == MouseButtons.Left)
            {
                // обнуление прямоугольника выбора
                _ribbonRect = Rectangle.Empty;
                // запоминаем точку первую точку выбора начала рисования прямоугольника выбора
                _mouseDownLocation = e.Location;
            }
        }

        /// <summary>
        /// Обработчик события перемещения мышки
        /// </summary>
        /// <param name="sender">визуальный компонент с поверхностью для рисования</param>
        /// <param name="e">объект параметров события со свойством Location</param>
        private void _control_MouseMove(object sender, MouseEventArgs e)
        {
            // обрабатываем событие, если была нажата левая кнопка мышки
            if (e.Button == MouseButtons.Left)
            {
                // нормализация параметров для прямоугольника выбора
                // в случае, если мы "растягиваем" прямоугольник не только по "главной" диагонали
                _ribbonRect.X = Math.Min(_mouseDownLocation.X, e.Location.X);
                _ribbonRect.Y = Math.Min(_mouseDownLocation.Y, e.Location.Y);
                // размеры должны быть всегда положительные числа
                _ribbonRect.Width = Math.Abs(_mouseDownLocation.X - e.Location.X);
                _ribbonRect.Height = Math.Abs(_mouseDownLocation.Y - e.Location.Y);
                // запрашиваем контейнер для рисования, чтобы обновился
                _container.Invalidate();
            }
        }

        /// <summary>
        /// Обработчик события отпускания кнопки мышки
        /// </summary>
        /// <param name="sender">визуальный компонент с поверхностью для рисования</param>
        /// <param name="e">объект параметров события</param>
        private void Control_MouseUp(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left)
            {
                // инициация события при окончании выбора прямоугольником
                if (!_ribbonRect.IsEmpty)
                {
                    // создаём объект аргумента для возбуждения события
                    RibbonSelectedEventArgs args = new RibbonSelectedEventArgs
                    {
                        // и передаём выбранный прямоугольник
                        RectangleSelected = _ribbonRect
                    };
                    // возбуждаем событие окончания выбора
                    OnRibbonSelected(args);
                }
                // обнуление прямоугольника выбора
                _ribbonRect = Rectangle.Empty;
                // запрашиваем контейнер для рисования, чтобы обновился
                _container.Invalidate();
            }
        }

        /// <summary>
        /// Метод инициации события по окончании процесса выбора прямоугольником
        /// </summary>
        /// <param name="e">объект параметров события со свойством RectangleSelected</param>
        protected virtual void OnRibbonSelected(RibbonSelectedEventArgs e)
        {
            // если на событие подписались, то вызываем его
            OnSelected?.Invoke(this, e);
        }
    }
}
