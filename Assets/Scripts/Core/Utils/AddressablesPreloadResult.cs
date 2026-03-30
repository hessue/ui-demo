using System.Collections.Generic;
using UnityEngine;

namespace BlockAndDagger.Core
{
    // Result returned by preload/release operations
    public class AddressablesPreloadResult
    {
        public IList<GameObject> PreloadedGroup { get; set; }
        public HashSet<string> PreloadedGroupKeys { get; set; }
    }
}
