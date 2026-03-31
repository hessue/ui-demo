#if UNITY_EDITOR

using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.AddressableAssets;
using UnityEngine;

namespace BlockAndDagger.Editor
{
    public static class BuildUtils
    {
        [MenuItem("Build/Refresh Level Count File")]
        public static void CreateFile()
        {
            var path = Path.Combine(Application.dataPath, Constants.LevelFolderPath);
            var info = new DirectoryInfo(path);
            var fileInfo = info.GetFiles("*.json", SearchOption.AllDirectories);
            File.WriteAllText(path  + "level_count.txt", "count=" + fileInfo.Length);
            AssetDatabase.Refresh();
            Debug.Log($"Level count file created successfully. Found {fileInfo.Length} levels and predefined blueprint files");
        }

        [MenuItem("Build/Create New Level")]
        public static void CreateNewReadOnlyLevel()
        {
            GameManager.Instance.LevelMaker.m_activeLevel.RefreshLevelDataForSaving();
            var distictTileTypes = GameManager.Instance.LevelMaker.m_activeLevel.GetUsedTileTypes();

            var folderPath = "Assets/Prefabs/Blocks";
            var distinctManifestEntries = new List<ManifestEntry>();
            var levelName = GameManager.Instance.LevelMaker.m_activeLevel.LevelData.LevelName;

            var settings = AddressableAssetSettingsDefaultObject.Settings;
            foreach (var tile in distictTileTypes)
            {
                var name = tile.ToString();
                var guids = AssetDatabase.FindAssets(name + " t:Prefab", new[] { folderPath });
                if (guids != null && guids.Length > 0)
                {
                    var guid = guids[0];
                    var entry = settings?.FindAssetEntry(guid);
                    var groupName = entry?.parentGroup?.name ?? string.Empty;
                    distinctManifestEntries.Add(new ManifestEntry
                    {
                        guid = guid,
                        tileType = name,
                        group = groupName
                    });
                }
            }

            GameManager.Instance.LevelMaker.m_activeLevel.LevelData.SetManifestEntries(distinctManifestEntries.ToArray());

            Debug.LogWarning("Remember to add these warning steps below:");
            Debug.LogWarning("1) Unique BlockType enum");
            Debug.LogWarning("2) Level selection image to Assets/Resources/StoredLevels/Images/");
            Debug.LogWarning("3) Move the file from persistentDataPath to Assets/Resources/StoredLevels folder");
            Debug.LogWarning("4) Add new blocks to one of the predetermined addressable groups");

            DataPersistenceManager.SaveReadOnlyLevel(GameManager.Instance.LevelMaker.m_activeLevel.LevelData);
        }
        
        [MenuItem("Build/Create Predefined Blueprint")]
        public static void CreateNewPredefinedBlueprint()
        {
            GameManager.Instance.LevelMaker.m_activeLevel.RefreshLevelDataForSaving(true);
            DataPersistenceManager.SaveReadOnlyLevel(GameManager.Instance.LevelMaker.m_activeLevel.LevelData);
            Debug.LogWarning("Remember to add these warning steps below:");
            Debug.LogWarning("1) Replace blueprint file ending with unique ID:");
            Debug.LogWarning("2) Move the file from persistentDataPath to Assets/Resources/StoredLevels/PredefinedBlueprints folder");
        }
        
        
    }
}

#endif