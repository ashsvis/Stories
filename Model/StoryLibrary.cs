using System;
using System.Collections.Generic;
using System.Windows.Forms;

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
                typeof(Label),
                typeof(Button),
                typeof(TextBox),
                typeof(CheckBox),
                typeof(RadioButton)
            };
        }

        /// <summary>
        /// Функция публикует доступные типы из библиотеки
        /// </summary>
        /// <returns></returns>
        public static IEnumerable<Type> GetControlTypes()
        {
            return controls;
        }
    }
}
