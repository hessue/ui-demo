using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using UnityEngine;
using Utils.JsonHelpers;

namespace BlockAndDagger
{
    [Serializable]
    public class JsonLevelData
    {
        public string levelName;
        public int? blueprint;
        public bool isPredefinedBlueprint;
        public string description;
        public JsonBlock[] groundThree;
        public JsonBlock[] groundTwo;
        public JsonBlock[] groundOne;
        public JsonBlock[] groundZero;
        
        public JsonBlock[] staticMainStructures;
        public JsonBlock[] staticWalkingPlatform;

        public int tileCount;
        public LevelEvents m_events;
        public ChallengeInfo challengeInfo;
    }

    public class JsonFilePersistence
    {
        private ILogger _logger;
        public JsonFilePersistence(ILogger logger)
        {
            _logger = logger;
        }

        private const string FileExtension = ".json";

        public void Save(LevelData data, bool useEditedVersion)
        {
            if (string.IsNullOrWhiteSpace(data?.LevelName))
            {
                throw new ArgumentException("data.LevelName is empty");
            }

            if (data.LevelName.EndsWith('.')) //TODO: regex validation
            {
                throw new ArgumentException("Level name cannot end with a '.' char");
            }
            
            if (data.ChallengeInfo.ChallengeType == ChallengeType.NOT_DEFINED)
            {
                throw new ArgumentException("Error, ChallengeType NOT_DEFINED");
            }

            //TODO: create cleaner implementation
            var conversion = data.ToJsonLevelData();
            var json = JsonConvert.SerializeObject(conversion,
                new JsonSerializerSettings()
                    {NullValueHandling = NullValueHandling.Ignore, Formatting = Formatting.Indented});

            var fileName = data.LevelName;
            if (useEditedVersion)
            {
                fileName += "_edited_" + data.Blueprint;
            }
            else if (data.IsPredefinedBlueprint)
            {
                fileName += "_predefined_GIVE_UNIQUE_ID"; //+ data.Blueprint;
            }

            WriteToFile(fileName, json);
            _logger.Log("Save success");
        }

        private void WriteToFile(string fileName, string json)
        {
            string path = GetFilePath(fileName + FileExtension);

            FileStream fileStream = new FileStream(path, FileMode.Create);

            using (StreamWriter writer = new StreamWriter(fileStream))
            {
                writer.Write(json);
            }

            _logger.Log(path);
        }

        public string ReadFromPersistentDataPath(string fileName)
        {
            var path = GetFilePath(fileName + FileExtension);
            if (File.Exists(path))
            {
                using (StreamReader reader = new StreamReader(path))
                {
                    string json = reader.ReadToEnd();
                    return json;
                }
            }
            else
            {
                Debug.LogWarning("File not found");
            }

            throw new DirectoryNotFoundException($"Error, File not found {fileName}");
        }

        public string ReadFromReadOnlyFolder(string fileName, string subfolder = "")
        {
            var folder = "StoredLevels/" + subfolder;
            var name = fileName.Replace(FileExtension, "");
            var targetFile = (TextAsset) Resources.Load(folder + name, typeof(TextAsset));
            if (targetFile is null)
            {
                Debug.LogWarning($"File not found with fileName: {fileName}");
            }

            return targetFile.text;
        }

        private string GetFilePath(string fileName)
        {
            return Application.persistentDataPath + "/" + fileName;
        }
    }

    public class KeysJsonConverter : JsonConverter
    {
        private readonly Type[] _types;

        public KeysJsonConverter(params Type[] types)
        {
            _types = types;
        }

        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
        {
            JToken t = JToken.FromObject(value);

            if (t.Type != JTokenType.Object)
            {
                t.WriteTo(writer);
            }
            else
            {
                JObject o = (JObject) t;
                IList<string> propertyNames = o.Properties().Select(p => p.Name).ToList();

                o.AddFirst(new JProperty("Keys", new JArray(propertyNames)));
                o.WriteTo(writer);
            }
        }

        public override object ReadJson(JsonReader reader, Type objectType, object existingValue,
            JsonSerializer serializer)
        {
            throw new NotImplementedException(
                "Unnecessary because CanRead is false. The type will skip the converter.");
        }

        public override bool CanRead
        {
            get { return false; }
        }

        public override bool CanConvert(Type objectType)
        {
            return _types.Any(t => t == objectType);
        }
    }
}