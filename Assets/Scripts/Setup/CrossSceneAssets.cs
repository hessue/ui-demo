using BlockAndDagger.Utils;
using UnityEngine;

namespace BlockAndDagger
{
    //Use as parent GameObject or Pinned Addressable Group. Used mainly for LevelSelection and Game scenes to prevent unexpected asset unloading
    public class CrossSceneAssets : SingletonMono<CrossSceneAssets>
    {
        [Header("Cross-scene shared assets")]
        [SerializeField] private Material m_unbuildMaterial;
        public Material UnbuildMaterial => m_unbuildMaterial;
   
        
        public new void Awake()
        {
            base.Awake();
        }

        // Destroy owned runtime copies and clear references when explicitly requested.
        public void ReleaseAssets()
        {
            if (m_unbuildMaterial != null)
            {
                Object.Destroy(m_unbuildMaterial);
                m_unbuildMaterial = null;
            }
            Debug.Log("CrossSceneAssets.ReleaseAssets: released cross-scene assets");
        }
    }
}
