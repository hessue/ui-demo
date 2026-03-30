using System;

namespace BlockAndDagger
{
    [Serializable]
    public class ManifestEntry
    {
        public string guid; //not good idea to rely upon if there are any changes made later
        public string tileType;
        public string group;
    }
}