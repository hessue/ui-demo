using System.Linq;
using BlockAndDagger;

namespace Utils.JsonHelpers
{
    public static class JsonLevelDataExtensions
    {
        public static JsonLevelData ToJsonLevelData(this LevelData levelData)
        {
            return new JsonLevelData()
            {
                blueprint = levelData.Blueprint,
                isPredefinedBlueprint = levelData.IsPredefinedBlueprint,
                description = levelData.Description,
                groundThree = levelData.GroundThree.TileToJsonTile(),
                groundTwo = levelData.GroundTwo.TileToJsonTile(),
                groundOne = levelData.GroundOne.TileToJsonTile(),
                groundZero = levelData.GroundZero.TileToJsonTile(),
                
                staticMainStructures = levelData.StaticMainStructures.TileToJsonTile(),
                staticWalkingPlatform = levelData.StaticWalkingPlatform.TileToJsonTile(),
                
                levelName = levelData.LevelName,
                tileCount = levelData.TileCount,
                m_events = levelData.LevelEvents,
                challengeInfo = levelData.ChallengeInfo
            };
        }

        private static JsonBlock[] TileToJsonTile(this Block[] tiles)
        {
            return tiles.Select(x => new JsonBlock()
            {
                hp = x.Data.hp,
                x = x.Data.x,
                y = x.Data.y,
                z = x.Data.z,
                type = x.Data.type,
                isBluePrintBlock = x.Data.isBluePrintBlock,
                isStaticGameObject = x.Data.isStaticGameObject,
                rotationY = x.Data.rotationY
            }).ToArray();
        }
    }
}
