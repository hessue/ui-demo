using UnityEngine;

namespace BlockAndDagger
{
    public interface IPrefabManager
    {
        LevelSelectionAssets LevelSelectionAssets { get; set; }
        GameAssets GameAssets { get; set; }
        CrossSceneAssets CrossSceneAssets { get; set; }
        
        Player CreateNewPlayer();
        GameObject CreateNewObject(LevelObjectType type);
        void ResetInstantiatedPlayerCount();
        
        public void InitCrossSceneAssets();
    }
}
