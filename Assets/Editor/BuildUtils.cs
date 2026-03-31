using System.IO;
using UnityEditor;
using UnityEngine;

namespace BlockAndDagger.Utils
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
            DataPersistenceManager.SaveReadOnlyLevel(GameManager.Instance.LevelMaker.m_activeLevel.LevelData);
            Debug.LogWarning("Remember to add these warning steps below:");
            Debug.LogWarning("1) Unique BlockType enum");
            Debug.LogWarning("2) Level selection image to Assets/Resources/StoredLevels/Images/");
            Debug.LogWarning("3) Move the file from persistentDataPath to Assets/Resources/StoredLevels folder");
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