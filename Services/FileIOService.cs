using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Windows.Forms;

namespace Stories.Services
{
    public class FileIOService
    {
        private readonly string PATH;

        public FileIOService(string path)
        {
            PATH = path;
        }

        public List<Control> LoadData()
        {
            bool fileExists = File.Exists(PATH);
            if (!fileExists)
            {
                File.CreateText(PATH).Dispose();
                return new List<Control>();
            }
            using (StreamReader reader = File.OpenText(PATH))
            {
                string input = reader.ReadToEnd();
                //var dict = JsonConvert.DeserializeObject<Dictionary<string, object>>(input);
                return new List<Control>();
            }
        }

        public void SaveData(List<Control> controls)
        {
            var samples = new Dictionary<Type, object>();
            var id = 0;
            using (StreamWriter writer = File.CreateText(PATH))
            {
                foreach (var control in controls)
                {
                    var controlId = id++;
                    writer.WriteLine($"[{controlId}]");
                    var type = control.GetType();
                    writer.WriteLine($"Type={type}");
                    MemberInfo[] m = type.GetProperties();
                    object sample;
                    if (samples.ContainsKey(type))
                        sample = samples[type];
                    else
                    {
                        sample = Activator.CreateInstance(type);
                        samples.Add(type, sample);
                    }
                    foreach (var info in m)
                    {
                        // получаем ссылку на свойство по его имени
                        var prop = type.GetProperty(info.Name);
                        if (!prop.CanWrite || !prop.CanRead) continue;
                        var propName = prop.Name;
                        if (propName == "Left" || propName == "Top") continue;
                        var propValue = prop.GetValue(control);
                        var propValueSample = prop.GetValue(sample);
                        if ($"{propValue}" == $"{propValueSample}") continue;
                        writer.WriteLine($"{propName}={propValue}");
                    }
                }
            }
        }
    }
}
