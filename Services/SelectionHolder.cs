using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Drawing;
using System.Windows.Forms;

namespace Stories.Services
{
    public class SelectionHolder
    {
        private readonly Control _container;
        private readonly List<Control> _selections = new List<Control>();

        public SelectionHolder(Control container)
        {
            // запоминаем ссылку на контейнер для рисования
            _container = container;
            // подключаемся к необходимым событиям на контейнере
            _container.MouseDown += Control_MouseDown;
            _container.MouseMove += Control_MouseMove;
            _container.MouseUp += Control_MouseUp;
            _container.Paint += Control_Paint;
        }

        public void Add(Control control)
        {
            if (_selections.Contains(control)) return;
            _selections.Add(control);
            _container.Invalidate();
        }

        public void Remove(Control control)
        {
            if (!_selections.Contains(control)) return;
            _selections.Remove(control);
            _container.Invalidate();
        }

        public void Clear()
        {
            _selections.Clear();
            _container.Invalidate();
        }

        public object[] GetSelected()
        {
            return _selections.Count > 0 ? _selections.Cast<object>().ToArray() : new object[] { };
        }

        private void Control_MouseDown(object sender, MouseEventArgs e)
        {
            Clear();
        }

        private void Control_MouseMove(object sender, MouseEventArgs e)
        {
            //throw new NotImplementedException();
        }

        private void Control_MouseUp(object sender, MouseEventArgs e)
        {
            //throw new NotImplementedException();
        }

        private void Control_Paint(object sender, PaintEventArgs e)
        {
            foreach (var control in _selections)
            {
                var rect = control.Bounds;
                rect.Width -= 1;
                rect.Height -= 1;
                rect.Inflate(1, 1);
                e.Graphics.DrawRectangle(Pens.Fuchsia, rect);
            }
        }
    }
}
