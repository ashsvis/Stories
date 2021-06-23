using Newtonsoft.Json;
using System.Collections.Generic;
using System.IO;
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
                return JsonConvert.DeserializeObject<List<Control>>(input);
            }
        }

        public void SaveData(object storyContent)
        {
            using (StreamWriter writer = File.CreateText(PATH))
            {
                string output = JsonConvert.SerializeObject(storyContent);
                writer.Write(output);
            }
        }
    }
}
