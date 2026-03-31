using System.Collections.Generic;
using System.Threading.Tasks;
using BlockAndDagger.Core;
using UnityEngine;
#if UNITY_EDITOR

namespace BlockAndDagger.Editor
{
    public static class AddressablesValidator
    {
        public static async Task ValidateAddresses(IEnumerable<string> addresses)
        {
            if (addresses == null)
            {
                Debug.LogWarning("ValidateAddresses: addresses is null");
                return;
            }

            foreach (var a in addresses)
            {
                bool ok = await AddressablesManager.HasResourceLocationsAsync(a);
                Debug.Log(a + ": " + (ok ? "FOUND" : "MISSING"));
            }
        }

        public static async Task ValidateGroupsExist(IEnumerable<string> addresses)
        {
            if (addresses == null)
            {
                Debug.LogWarning("ValidateGroupsExist: addresses is null");
                return;
            }

            var missing = new List<string>();
            foreach (var a in addresses)
            {
                var ok = await AddressablesManager.HasResourceLocationsAsync(a);
                if (!ok)
                {
                    missing.Add(a);
                }
            }

            if (missing.Count > 0)
            {
                Debug.LogError($"Addressable groups missing: {string.Join(", ", missing)}");
            }
            else
            {
                Debug.Log("All addressable groups exist");
            }
        }
    }
}
#endif

