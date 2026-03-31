
namespace BlockAndDagger
{
    public static class Constants
    {
        public const string PredefinedBlueprintFolderPath = "Resources/StoredLevels/PredefinedBlueprints/";
        
        public const string LevelFolderPath = "Resources/StoredLevels/";
        
        public const string LevelImagesPath = "StoredLevels/Images/";

        public const string BlockIconPath = "Prefabs/UI/Images/";
        
        public const string LevelCountFilename = "level_count.txt";
        public const string LevelCountFilPath =  "Resources/" + LevelCountFilename;
        
        public const float SpawningPosOffsetY = 3f;
        
        /*private const int BlockWidthAndDepth = 1;
        public Vector2 StartingPointAllowedBuildArea = new(BlockWidthAndDepth, BlockWidthAndDepth); */

        /// <summary>
        /// Remember this is Abs
        /// </summary>
        public const int StartingPointAllowedBuildAreaX = 3;
        /// <summary>
        /// Remember this is Abs
        /// </summary>
        public const int StartingPointAllowedBuildAreaZ = 3;

        public const TileType StartPositionSymbol = TileType.Flowers;
        
        public const int CameraFieldOfViewDefault = 20;
        
        public const string MainMenuSceneName = "MainMenu";
        public const string LevelSelectionSceneName = "LevelSelection";
        public const string GameSceneName = "Game";
    }
}
