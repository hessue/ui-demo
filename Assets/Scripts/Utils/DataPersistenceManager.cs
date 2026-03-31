using Newtonsoft.Json;

namespace BlockAndDagger
{
    public sealed class DataPersistenceManager
    {
        private static JsonFilePersistence _jsonFilePersistence;
        
        public DataPersistenceManager(JsonFilePersistence jsonFilePersistence)
        {
            _jsonFilePersistence = jsonFilePersistence;
        }

        public void SaveLevel(LevelData levelData)
        {
            _jsonFilePersistence.Save(levelData, true);
        }

        /// <summary>
        /// Base of new level, developers only
        /// </summary>
        public static void SaveReadOnlyLevel(LevelData levelData)
        {
            
#if UNITY_EDITOR
            //Note! a MenuItem use case
            _jsonFilePersistence = _jsonFilePersistence ?? new(UnityLogger.Default);
#endif
            _jsonFilePersistence.Save(levelData, false);
        }

        public JsonLevelData LoadEmptyLevel(LevelName levelName)
        {
            var json = _jsonFilePersistence.ReadFromReadOnlyFolder(levelName.ToString());
            JsonLevelData data =
                JsonConvert.DeserializeObject<JsonLevelData>(json, new KeysJsonConverter(typeof(JsonLevelData)));

            return data;
        }

        public JsonLevelData LoadPredefinedBlueprint(LevelAndBlueprint levelAndBlueprint)
        {
            var fileName =
                levelAndBlueprint.Level + "_" + levelAndBlueprint.BlueprintName; //toString is overriden already
            var predefinedJson = _jsonFilePersistence.ReadFromReadOnlyFolder(fileName, "PredefinedBlueprints/");
            JsonLevelData data =
                JsonConvert.DeserializeObject<JsonLevelData>(predefinedJson,
                    new KeysJsonConverter(typeof(JsonLevelData)));

            return data;
        }

        public LevelData LoadEditedLevel(LevelAndBlueprint levelAndBlueprint)
        {
            var json = _jsonFilePersistence.ReadFromPersistentDataPath(levelAndBlueprint.Level + "_" +
                                                                       levelAndBlueprint
                                                                           .BlueprintName); //levelAndBlueprint.ToString());
            JsonLevelData jsonLevelData =
                JsonConvert.DeserializeObject<JsonLevelData>(json, new KeysJsonConverter(typeof(JsonLevelData)));

            return new LevelData(jsonLevelData);
        }
    }
}