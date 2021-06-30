using Stories.Model;
using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Windows.Forms;
using System.IO.Compression;
using System.Runtime.Serialization.Formatters.Binary;

namespace Stories.Services
{
    public class FileIOService
    {
        private readonly string PATH;

        public FileIOService(string path)
        {
            PATH = path;
        }

        public List<StoryElement> LoadData()
        {
            bool fileExists = File.Exists(PATH);
            if (!fileExists)
                return new List<StoryElement>();
            using (var fs = File.OpenRead(PATH))
            using (var zip = new GZipStream(fs, CompressionMode.Decompress))
            {
                var formatter = new BinaryFormatter();
                var items = (List<StoreItem>)formatter.Deserialize(zip);

                var list = new List<StoryElement>();
                foreach (var item in items)
                {
                    var type = StoryLibrary.GetTypeByFullName(item.Type);
                    if (type == null) continue;
                    var control = (StoryElement)Activator.CreateInstance(type);
                    list.Add(control);
                    foreach (var aprop in item.Props)
                    {
                        var prop = type.GetProperty(aprop.Name);
                        prop.SetValue(control, aprop.Value);
                    }
                }
                return list;
            }
        }

        public void SaveData(IEnumerable<StoryElement> controls)
        {
            var samples = new Dictionary<Type, object>();
            var content = new List<StoreItem>();
            foreach (var control in controls)
            {
                var type = control.GetType();
                var storeItem = new StoreItem() { Type = $"{type}" };
                content.Add(storeItem);
                var storeProps = new List<StoreProp>();
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
                    var propType = prop.PropertyType;

                    if (!prop.CanWrite || !prop.CanRead || !propType.IsSerializable) continue;

                    var propName = prop.Name;
                    var propValue = prop.GetValue(control);
                    var propValueSample = prop.GetValue(sample);
                    if ($"{propValue}" == $"{propValueSample}") continue;
                    storeProps.Add(new StoreProp() { Name = propName, Value = propValue });
                }
                storeItem.Props = storeProps;
            }

            using (var fs = File.Create(PATH))
            using (var zip = new GZipStream(fs, CompressionMode.Compress))
            {
                var formatter = new BinaryFormatter();
                formatter.Serialize(zip, content);
            }
        }
    }
}
