using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using UnityEngine;

namespace BlockAndDagger
{
    public sealed class LevelLoader : MonoBehaviour
    {
        private DataPersistenceManager _dataPersistenceManager;

        public void Init(DataPersistenceManager dataPersistenceManager)
        {
            _dataPersistenceManager = dataPersistenceManager;
        }

        public LevelData LoadEmptyLevel(LevelName levelName)
        {
            var jsonLevelData = _dataPersistenceManager.LoadEmptyLevel(levelName);
            return new LevelData(jsonLevelData);
        }

        public LevelData LoadEditedLevel(LevelAndBlueprint levelAndBlueprint)
        {
            return _dataPersistenceManager.LoadEditedLevel(levelAndBlueprint);
        }

        public LevelData LoadPredefinedBlueprint(LevelAndBlueprint levelAndBlueprint)
        {
            var levelBaseData = _dataPersistenceManager.LoadEmptyLevel(levelAndBlueprint.Level);
            var predefinedBlueprintData = _dataPersistenceManager.LoadPredefinedBlueprint(levelAndBlueprint);
            return new LevelData(levelBaseData, predefinedBlueprintData);
        }

        public List<(LevelName, int blueprintCount)> GetLevelAndBlueprintFigures()
        {
            var info = new DirectoryInfo(Application.persistentDataPath);
            var fileInfo = info.GetFiles("*.json", SearchOption.AllDirectories);

            var levels = fileInfo.GroupBy(x => x.Name.Split("_edited_")[0]).ToList();
            List<(LevelName, int blueprintCount)> list = new();

            foreach (var grouping in levels)
            {
                var levelName = Enum.Parse<LevelName>(grouping.Key, true);
                var count = levels.GroupBy(x => x.Key).Count();
                list.Add((levelName, count));
            }

            /*
           File.WriteAllText(path  + "level_count.txt", "count=" + fileInfo.Length);
           var json = _jsonFilePersistence.ReadFromPersistentDataPath(levelAndBluePrint.ToString());
           JsonLevelData jsonLevelData =
           JsonConvert.DeserializeObject<JsonLevelData>(json, new KeysJsonConverter(typeof(JsonLevelData)));*/

            return list;
        }

        public string[] GetAllAvailablePredefinedBlueprints(LevelName forLevelName)
        {
            var info = new DirectoryInfo(Path.Combine(Application.dataPath, Constants.PredefinedBlueprintFolderPath));
            var fileInfo = info.GetFiles(forLevelName + "_" + "*.json", SearchOption.AllDirectories);
            if (!fileInfo.Any())
            {
                return Array.Empty<string>();
            }

            var blueprintPart = fileInfo
                .Select(x => x.Name.Replace(".json", "").Remove(0, forLevelName.ToString().Length + 1))
                .ToArray(); //+1 underscore
            return blueprintPart;
        }

        public LevelAndBlueprint[] GetAllBlueprints()
        {
            var info = new DirectoryInfo(Path.Combine(Application.dataPath, Constants.LevelFolderPath));
            var files = info.GetFiles("Level_*.json", SearchOption.TopDirectoryOnly);
            var baseLevels = GetAllLevels(files, true);

            //TODO: filter unlocked

            info = new DirectoryInfo(Application.persistentDataPath);
            files = info.GetFiles("Level_*.json", SearchOption.TopDirectoryOnly);
            var blueprints = GetAllLevels(files, false);
            var list = baseLevels.Concat(blueprints);
            return list.OrderBy(x => x.Level).ToArray();
        }

        //TODO: plenty of refactoring 
        public LevelAndBlueprint[] GetAllLevels(FileInfo[] fileInfo, bool onlyBaseLevels,
            LevelName[] unlockedLevels = null)
        {
            List<LevelAndBlueprint> list = new();
            if (!fileInfo.Any())
            {
                return list.ToArray();
            }

            var pattern = @"^(Level_[0-9]+)";

            foreach (var fileName in fileInfo)
            {
                Match m = Regex.Match(fileName.Name, pattern, RegexOptions.IgnoreCase);
                if (m.Success)
                {
                    var str = m.Value;
                    if (!Enum.TryParse<LevelName>(str, true, out var levelName))
                    {
                        Debug.LogWarning($"Failed to parse LevelName from '{str}' in file {fileName.Name}. Skipping.");
                        continue;
                    }
                    
                    // var levelName = Enum.Parse<LevelName>(str, true);
                    string blueprint = "";
                    if (!onlyBaseLevels)
                    {
                        blueprint = fileName.Name.Split(m.Value)[1].Remove(0, 1);
                        Debug.Assert(!string.IsNullOrWhiteSpace(blueprint), "blueprint string cannot be empty");
                    }

                    var levelBaseData = _dataPersistenceManager.LoadEmptyLevel(levelName);
                    if (unlockedLevels != null && !unlockedLevels.Contains(levelName))
                    {
                        continue;
                    }

                    list.Add(new LevelAndBlueprint(levelName, blueprint.Replace(".json", ""), levelBaseData.description,
                        true));
                }
            }
            //TODO: Get all non-blueprints as well

            return list.ToArray();
        }
    }
}
