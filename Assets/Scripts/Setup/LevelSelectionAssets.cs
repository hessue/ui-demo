using BlockAndDagger.Utils;
using UnityEngine;

namespace BlockAndDagger
{
    public class LevelSelectionAssets : SingletonMono<LevelSelectionAssets>
    {
        [SerializeField] private Material m_highlighMaterial;
        [SerializeField] private GameObject m_levelIconItemPrefab;

        public GameObject LevelIconItemPrefab => m_levelIconItemPrefab;
        public Material HighlighMaterial => m_highlighMaterial;

        private new void Awake()
        {
            base.Awake();
        }
    }
}
