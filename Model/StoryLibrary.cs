using System;
using System.Linq;
using System.Collections.Generic;

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
                typeof(BeginElement),
                typeof(IfElement),
                typeof(EndElement),
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

        public static Type GetTypeByFullName(string fullName)
        {
            return controls.FirstOrDefault(item => item.ToString() == fullName);
        }
    }
}
