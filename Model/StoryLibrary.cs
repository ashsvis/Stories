using System;
using System.Collections.Generic;
using System.Windows.Forms;

namespace Stories.Model
{
    public static class StoryLibrary
    {
        private static Type[] controls;

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

        public static IEnumerable<Type> GetControlTypes()
        {
            return controls;
        }
    }
}
