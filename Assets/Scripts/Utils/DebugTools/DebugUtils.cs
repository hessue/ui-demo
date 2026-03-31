using UnityEngine;

namespace BlockAndDagger.DebugTools
{
    public static class DebugUtils
    {
        private static readonly int ColorId = Shader.PropertyToID("_Color");
        private static readonly int EmissionId = Shader.PropertyToID("_EmissionColor");

        /// <summary>
        /// Apply a Unity-like purple tint to all MeshRenderers on the provided GameObject and its children
        /// when the GameObject is marked as static at runtime.
        /// </summary>
        public static void ApplyPurpleTint(GameObject go)
        {
            if (go == null)
                return;

            if (!go.isStatic)
                return;

            var renderers = go.GetComponentsInChildren<MeshRenderer>();
            if (renderers == null || renderers.Length == 0)
                return;

            Color unityPurple = new Color(0.48f, 0.22f, 0.76f, 1f);

            foreach (var r in renderers)
            {
                if (r == null)
                    continue;

                var baseMats = r.sharedMaterials ?? new Material[0];
                var newMats = new Material[baseMats.Length];

                for (int i = 0; i < baseMats.Length; i++)
                {
                    var baseMat = baseMats[i];
                    if (baseMat == null)
                    {
                        var m = new Material(Shader.Find("Standard"));
                        m.color = unityPurple;
                        newMats[i] = m;
                        continue;
                    }

                    var inst = new Material(baseMat);
                    if (inst.HasProperty(ColorId))
                    {
                        inst.color = unityPurple;
                    }
                    else if (inst.HasProperty(EmissionId))
                    {
                        inst.EnableKeyword("_EMISSION");
                        inst.SetColor(EmissionId, unityPurple * 0.6f);
                    }

                    newMats[i] = inst;
                }

                r.materials = newMats;
            }
        }
    }
}
