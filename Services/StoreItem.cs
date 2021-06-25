using System;
using System.Collections.Generic;

namespace Stories.Services
{
    [Serializable]
    public class StoreItem
    {
        public string Type { get; set; }
        public List<StoreProp> Props { get; set; } = new();
    }

    [Serializable]
    public class StoreProp
    {
        public string Name { get; set; }
        public object Value { get; set; }

        public override string ToString()
        {
            return $"{Name}={Value}";
        }
    }
}
