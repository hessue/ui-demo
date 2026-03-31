using System.Linq;
using BlockAndDagger;

namespace BlockAndDagger.Utils.JsonHelpers
{
    public static class JsonLevelDataExtensions
    {
        public static JsonLevelData ToJsonLevelData(this LevelData levelData)
        {
            var groundThree = levelData.GroundThree.TileToJsonTile();
            var groundTwo = levelData.GroundTwo.TileToJsonTile();
            var groundOne = levelData.GroundOne.TileToJsonTile();
            var groundZero = levelData.GroundZero.TileToJsonTile();
            var staticMain = levelData.StaticMainStructures.TileToJsonTile();
            var staticWalk = levelData.StaticWalkingPlatform.TileToJsonTile();

            var used = groundThree.Concat(groundTwo).Concat(groundOne).Concat(groundZero).Concat(staticMain).Concat(staticWalk)
                .Where(x => x != null)
                .Select(x => x.type)
                .Distinct()
                .ToArray();

            return new JsonLevelData()
            {
                blueprint = levelData.Blueprint,
                isPredefinedBlueprint = levelData.IsPredefinedBlueprint,
                description = levelData.Description,
                groundThree = groundThree,
                groundTwo = groundTwo,
                groundOne = groundOne,
                groundZero = groundZero,
                staticMainStructures = staticMain,
                staticWalkingPlatform = staticWalk,
                usedTileTypes = used,
                levelName = levelData.LevelName,
                tileCount = levelData.TileCount,
                m_events = levelData.LevelEvents,
                challengeInfo = levelData.ChallengeInfo,
                addressableAssets = levelData.ManifestEntries
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
