using System;
using System.Linq;
using System.Collections.Generic;
using System.Windows.Forms;
using System.Drawing;

namespace Stories.Model
{
    /// <summary>
    /// Статический класс библиотеки типов, содержит список поддерживаемых типов
    /// </summary>
    public static class StoryLibrary
    {
        private static Type[] controls;

        /// <summary>
        /// Инициализация внутреннего массива
        /// </summary>
        public static void Init()
        {
            controls = new Type[] 
            { 
                typeof(BeginOperator),
                typeof(EndOperator),
            };
        }

        public static void DrawSpecifics(Graphics graphics, Control control, Rectangle bounds)
        {
            if (control is PictureBox pBox)
            {
                if (pBox.Image == null)
                    using (var pen = new Pen(Color.Black) { DashStyle = System.Drawing.Drawing2D.DashStyle.Dot })
                        graphics.DrawRectangle(pen, CorrectRect(bounds));
            }
            if (control is Label label)
            {
                if (string.IsNullOrWhiteSpace(label.Text) && label.BorderStyle == BorderStyle.None)
                    using (var pen = new Pen(Color.Black) { DashStyle = System.Drawing.Drawing2D.DashStyle.Dot })
                        graphics.DrawRectangle(pen, CorrectRect(bounds));
            }
            if (control is Panel panel)
            {
                if (panel.BorderStyle == BorderStyle.None)
                    using (var pen = new Pen(Color.Black) { DashStyle = System.Drawing.Drawing2D.DashStyle.Dot })
                        graphics.DrawRectangle(pen, CorrectRect(bounds));
            }
        }

        private static Rectangle CorrectRect(Rectangle bounds)
        {
            var rect = bounds;
            rect.Width -= 1;
            rect.Height -= 1;
            return rect;
        }

        /// <summary>
        /// Функция публикует доступные типы из библиотеки
        /// </summary>
        /// <returns></returns>
        public static IEnumerable<Type> GetControlTypes()
        {
            return controls;
        }

        public static Type GetTypeByFullName(string fullName)
        {
            return controls.FirstOrDefault(item => item.ToString() == fullName);
        }
    }
}
