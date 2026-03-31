
using UnityEngine;

namespace BlockAndDagger
{
    public struct LevelAndBlueprint
    {
        /// <summary>
        /// Edited file name format: Level_Number_edited_GeneratedNumberBluePrintNumber.json
        /// </summary>
        public LevelAndBlueprint(LevelName level, string blueprintName = "", string challengeDescription = "", bool unlocked = false, bool isPredefinedBlueprint = false, int fieldOfView = Constants.CameraFieldOfViewDefault, string musicTrack = "")
        {
            Level = level;
            BlueprintName = blueprintName;
            Unlocked = unlocked;
            ChallengeDescription = challengeDescription;
            IsPredefinedBlueprint = isPredefinedBlueprint;
            FieldOfView = fieldOfView;
            MusicTrack = musicTrack;
  
            //Might change case by case, how map wants to be presented/the layout is
            CameraPos = new Vector3(13, 32, -27);
            CameraRot = new Vector3(45f, -28f, 0f);
        }
        
        public bool Unlocked { get; }
        public LevelName Level { get; }
        public string BlueprintName { get; }
        
        //Set immediately after loading predefined blueprint
        public bool IsPredefinedBlueprint { get; set; }
        
        public string ChallengeDescription { get; }
        
        public int FieldOfView { get; }
        
        public string MusicTrack { get; }
        
        public Vector3 CameraPos { get; }
        public Vector3 CameraRot{ get; }
        
        public override string ToString()
        {
            if (string.IsNullOrWhiteSpace(BlueprintName))
            {
                return Level.ToString();
            }
            else
            {
               return $"{Level}_edited_{BlueprintName}";
            }
        }
    }
}
