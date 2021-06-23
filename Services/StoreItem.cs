using System;

namespace Stories.Services
{
    [Serializable]
    public class StoreItem
    {
        public string Type { get; set; }
        public StoreProp[] Props { get; set; } = new StoreProp[] { };
    }

    [Serializable]
    public class StoreProp
    {
        public string Name { get; set; }
        public object Value { get; set; }
    }
}
