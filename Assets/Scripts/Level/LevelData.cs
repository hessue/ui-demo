using System;
using System.Collections.Generic;
using System.Linq;
using BlockAndDagger.Utils;
using UnityEngine;

namespace BlockAndDagger
{
    [Serializable]
    public sealed class LevelData
    {
        [field: SerializeField] public string LevelName { get; private set; }

        /// <summary>
        /// 0 is not acceptable currently
        /// </summary>
        [field: SerializeField]
        public int? Blueprint { get; private set; } //TODO: show on inspector, currently not showing

        //Constructor should only init this, the SetBlueprint method is just rare exception case
        public bool IsPredefinedBlueprint { get; set; }

        [field: SerializeField] public string Description { get; private set; }

        //Grounds are ideally just readonly from inspector. Example [ReadOnly] [SerializeField] 
        [field: SerializeField] public Block[] GroundThree { get; private set; }
        [field: SerializeField] public Block[] GroundTwo { get; private set; }
        [field: SerializeField] public Block[] GroundOne { get; private set; }
        [field: SerializeField] public Block[] GroundZero { get; private set; }


        //TODO: start using list
        [field: SerializeField] public Block[] StaticWalkingPlatform { get; private set; }
        [field: SerializeField] public Block[] StaticMainStructures { get; private set; }

        ///Excluding static grid game objects
        public int TileCount { get; private set; }

        //TODO: needs custom CreateInspectorGUI, exclusive radio button thing
        [field: SerializeField] public ChallengeInfo ChallengeInfo { get; private set; }

        [field: SerializeField] public LevelEvents LevelEvents { get; private set; } //TODO: probably a dedicated editor/window?
        
        [field: SerializeField] public BattleStateChange[] BattleStateChanges { get; private set; } //TODO create editor

        private IList<GameObject> m_preloadedBlockPrefabs;

        public void SetBlueprint(int currentBlueprintCount, bool isPredefined = false)
        {
            Blueprint = currentBlueprintCount;
            IsPredefinedBlueprint = isPredefined;
        }
        
        private void PosHalfStepWorkaround(JsonLevelData levelData)
        {
            SyncGridLocationBasedOnWorldPos(levelData.groundThree);
            SyncGridLocationBasedOnWorldPos(levelData.groundTwo);
            SyncGridLocationBasedOnWorldPos(levelData.groundOne);
            SyncGridLocationBasedOnWorldPos(levelData.groundZero);
            
            //TODO: remember to remove the Planes go 0.5f offset workaround
            
            
            bool PassesAsInteger(float myFloat)
            {
                return Mathf.Approximately(myFloat, Mathf.RoundToInt(myFloat));
            }

            void SyncGridLocationBasedOnWorldPos(JsonBlock[] groundBlocks)
            {
                for (int i = 0; i < groundBlocks.Length; i++)
                {
                    if (!PassesAsInteger(groundBlocks[i].x))
                    {
                        groundBlocks[i].x += 0.5f;
                    }
                    
                    if (!PassesAsInteger(groundBlocks[i].y))
                    {
                        groundBlocks[i].y += 0.5f;
                    }
                    
                    if (!PassesAsInteger(groundBlocks[i].z))
                    {
                        groundBlocks[i].z += 0.5f;
                    }
                }
            }
        }
        
        public LevelData(JsonLevelData levelData)
        {
            //TODO: 1) NO 1, fix the grid offset before doing more levels!
            //      2) Remove this workaround oneday! Can cause lots of harm if code uses hardcoding/expected values

            if (Enum.TryParse<LevelName>(levelData.levelName, out var _))
            {
                PosHalfStepWorkaround(levelData);
            }

            ChallengeInfo = levelData.challengeInfo;
            LevelName = levelData.levelName;
            Blueprint = levelData.blueprint;
            Description = levelData.description;
            GroundThree = new Block[levelData.groundThree.Length];
            GroundTwo = new Block[levelData.groundTwo.Length];
            GroundOne = new Block[levelData.groundOne.Length];
            GroundZero = new Block[levelData.groundZero.Length];

            StaticWalkingPlatform = levelData.staticWalkingPlatform == null
                ? Array.Empty<Block>()
                : new Block[levelData.staticWalkingPlatform.Length];
            StaticMainStructures = levelData.staticMainStructures == null
                ? Array.Empty<Block>()
                : new Block[levelData.staticMainStructures.Length];

            m_preloadedBlockPrefabs = AddressablesManager.GetPreloadedGroup();

            if (levelData.staticWalkingPlatform != null)
            {
                //TODO: inject parent
                var parent = GameManager.Instance.LevelMaker.m_activeLevel.m_staticTilemaps.GetTileMap("Static_" +
                    BlockDepth.WalkingPlatform);

                CreateStaticBlocks(levelData.staticWalkingPlatform, StaticWalkingPlatform, parent);
            }

            if (levelData.staticMainStructures != null)
            {
                //TODO: inject parent
                var parent = GameManager.Instance.LevelMaker.m_activeLevel.m_staticTilemaps.GetTileMap("Static_" +
                    BlockDepth.MainStructures);

                CreateStaticBlocks(levelData.staticMainStructures, StaticMainStructures, parent);
            }

            CreateBlocks(levelData.groundThree, GroundThree);
            CreateBlocks(levelData.groundTwo, GroundTwo);
            CreateBlocks(levelData.groundOne, GroundOne);
            CreateBlocks(levelData.groundZero, GroundZero);
            TileCount = levelData.tileCount;
            LevelEvents = levelData.m_events;
            ChallengeInfo = levelData.challengeInfo ?? new ChallengeInfo(ChallengeType.NOT_DEFINED);

            var groundList = new Dictionary<int, Block[]>()
            {
                {3, GroundThree },
                {2, GroundTwo },
                {1, GroundOne },
                {0, GroundZero }
            };
            
            //REMEMBER this only works with Vector3Int (pos should must not have decimals)
            PopulateNeighbourData(groundList);
            
#if UNITY_EDITOR 
            ChallengeInfo.ValidateInfo();
#endif
        }

        ///Use before a map get saved 
        public LevelData(string levelName, int? blueprint, string description, Block[] groundThree, Block[] groundTwo,
            Block[] groundOne, Block[] groundZero, LevelEvents levelEvents,
            Block[] staticMainStructures, Block[] staticWalkingPlatform, ChallengeInfo challengeInfo, bool isPredefinedBlueprint = false)
        {
            LevelName = levelName;
            Blueprint = blueprint;
            IsPredefinedBlueprint = isPredefinedBlueprint;
            Description = description;
            GroundThree = groundThree;
            GroundTwo = groundTwo;
            GroundOne = groundOne;
            GroundZero = groundZero;
            StaticMainStructures = staticMainStructures;
            StaticWalkingPlatform = staticWalkingPlatform;
            TileCount = groundThree.Length + groundTwo.Length + groundOne.Length + groundZero.Length;
            LevelEvents = levelEvents;
            ChallengeInfo = challengeInfo ?? new ChallengeInfo(ChallengeType.NOT_DEFINED);
#if UNITY_EDITOR 
            ChallengeInfo.ValidateInfo();
#endif
        }

        /// <summary>
        /// Extend base level with blueprint blocks
        /// </summary>
        public LevelData(JsonLevelData levelData, JsonLevelData predefinedBlueprintData)
        {
            if (levelData.levelName != predefinedBlueprintData.levelName)
            {
                //It might be possible to mix these later on, but it needs some extra logic when blocks overlap
                throw new InvalidOperationException(
                    "LevelName must be same on both JsonLevelData and predefinedBlueprintJsonLevelData to able to do merging properly");
            }

            LevelName = levelData.levelName;
            Description = levelData.description;
            ChallengeInfo = levelData.challengeInfo;
            //important ones
            Blueprint = predefinedBlueprintData.blueprint;
            IsPredefinedBlueprint = true;

            var appendedGroundTree = levelData.groundThree.Concat(predefinedBlueprintData.groundThree).ToArray();
            var appendedGroundTwo = levelData.groundTwo.Concat(predefinedBlueprintData.groundTwo).ToArray();
            var appendedGroundOne = levelData.groundOne.Concat(predefinedBlueprintData.groundOne).ToArray();
            var appendedGroundZero = levelData.groundZero.Concat(predefinedBlueprintData.groundZero).ToArray();

            GroundThree = new Block[appendedGroundTree.Length];
            GroundTwo = new Block[appendedGroundTwo.Length];
            GroundOne = new Block[appendedGroundOne.Length];
            GroundZero = new Block[appendedGroundZero.Length];

            CreateBlocks(appendedGroundTree, GroundThree);
            CreateBlocks(appendedGroundTwo, GroundTwo);
            CreateBlocks(appendedGroundOne, GroundOne);
            CreateBlocks(appendedGroundZero, GroundZero);
            TileCount = levelData.tileCount;
            LevelEvents = levelData.m_events;
        }

        public Block[] GetGroundBlocks(BlockDepth blockDepth)
        {
            switch (blockDepth)
            {
                case BlockDepth.UpperlevelStructures:
                    return GroundThree;
                case BlockDepth.MainStructures:
                    return GroundTwo;
                case BlockDepth.WalkingPlatform:
                    return GroundOne;
                case BlockDepth.Water:
                    throw new NotImplementedException("Zero/Water ground not supported");
            }

            throw new ArgumentOutOfRangeException(nameof(blockDepth), blockDepth, "BlockDepth not yet supported");
        }

        /// <summary>
        /// Try to get rid of
        /// </summary>
        /// <param name="blockDepth"></param>
        public Block[] GetGroundByHeightY(BlockDepth blockDepth)
        {
            switch (blockDepth)
            {
                case BlockDepth.UpperlevelStructures:
                    return GroundThree;
                case BlockDepth.MainStructures:
                    return GroundTwo;
                case BlockDepth.WalkingPlatform:
                    return GroundOne;
                case BlockDepth.Water:
                    throw new NotImplementedException("Zero/Water ground not supported");
            }

            throw new ArgumentOutOfRangeException(nameof(blockDepth), blockDepth, "BlockDepth not yet supported");
        }

        /// <summary>
        /// Note! Currently not removing static blocks
        /// </summary>
        public void EmptyLevelData()
        {
            LevelName = "";
            Blueprint = null;
            Description = "";
            TileCount = 0;
            ChallengeInfo = new ChallengeInfo(ChallengeType.NOT_DEFINED);

            DestroyGroundBlocks(GroundThree);
            DestroyGroundBlocks(GroundTwo);
            DestroyGroundBlocks(GroundOne);
            DestroyGroundBlocks(GroundZero);
        }

        private void DestroyGroundBlocks(Block[] ground)
        {
            if (ground is not null)
            {
                var tracker = GameManager.Instance.LevelMaker.m_activeLevel.TileDataTracker;
                foreach (var block in ground)
                {
                    tracker.RemoveTile(block); //json data vs transform.position?
                }
            }
        }

        private void CreateBlocks(JsonBlock[] groundData, Block[] targetList)
        {
            var tracker = GameManager.Instance.LevelMaker.m_activeLevel.TileDataTracker;
            for (int i = 0; i < groundData.Length; i++)
            {
                GameObject newObj = null;
                var blockData = groundData[i];
                
                if (m_preloadedBlockPrefabs != null)
                {
                   newObj = AddressablesManager.FindFromCacheAndInstantiatePrefab(blockData.type.ToString(),
                      new Vector3(blockData.x, blockData.y, blockData.z));
                }
                else
                {
                    //TODO: maybe scenearios to do normal instantiate as fallback
                }

                var block = newObj.GetComponent<Block>();
                block.InitBlock(blockData);

                //TODO: more elegant implementation needed for all goal types
                if(ChallengeInfo != null && ChallengeInfo.challengeType == ChallengeType.GetToTarget)
                {
                    if (block.TileType == ChallengeInfo.TouchTileType)
                    {
                        block.IsGoal = true;
                    }
                }

                MarkAsStatic(block);
                
#if UNITY_EDITOR
                //TODO:This line is a temporary solution. TODO: Decide which approach is better staticGrid or mark static by other logic
                //DebugUtils.ApplyPurpleTint(obj);
#endif
                
                targetList[i] = block;

                tracker.AddOrReplaceTile(new Vector3Int((int) blockData.x, (int) blockData.y, (int) blockData.z), block);
            }
        }

        private void MarkAsStatic(Block block)
        {
            switch (block.Data.type)
            {
                case TileType.TreePine:
                case TileType.Flowers:
                case TileType.EnemySpawner:
                case TileType.Rocks:
                case TileType.Plant:
                case TileType.InvisibleWall:
                    block.gameObject.isStatic = true;
                    break;
                case TileType.Block:
                case TileType.BlockDirt:
                case TileType.Slope:
                {
                    if (!block.IsBluePrintBlock)
                    {
                        block.gameObject.isStatic = true;
                    }
                }
                    break;
            }
        }

        private void PopulateNeighbourData(Dictionary<int, Block[]> targetList)
        {
            foreach (var targetFloor in targetList)
            {
                var groundLevel = (BlockDepth)targetFloor.Key;
                var blocks = targetFloor.Value;

                foreach (var currentBlock in blocks)
                {
                    IBlock south = null;
                    IBlock north = null;
                    IBlock west = null;
                    IBlock westUnder = null;
                    IBlock east = null;
                    IBlock eastUnder = null;
                    IBlock above = null;
                    IBlock under = null;

                    if (groundLevel < BlockDepth.UpperlevelStructures)
                    {
                        above = targetList[targetFloor.Key + 1]
                            .FirstOrDefault(x =>
                                x.GetXYZGridLocation() == currentBlock.GetXYZGridLocation() + new Vector3Int(0, 1, 0));
                    }

                    if (groundLevel > BlockDepth.Water)
                    {
                        under = targetList[targetFloor.Key - 1]
                            .FirstOrDefault(x =>
                                x.GetXYZGridLocation() == currentBlock.GetXYZGridLocation() + new Vector3Int(0, -1, 0));

                        eastUnder = targetList[targetFloor.Key - 1]
                            .FirstOrDefault(x =>
                                x.GetXYZGridLocation() == currentBlock.GetXYZGridLocation() + new Vector3Int(1, -1, 0));

                        westUnder = targetList[targetFloor.Key - 1]
                            .FirstOrDefault(x =>
                                x.GetXYZGridLocation() == currentBlock.GetXYZGridLocation() + new Vector3Int(-1, -1, 0));
                    }

                    north = targetList[targetFloor.Key]
                        .FirstOrDefault(x =>
                            x.GetXYZGridLocation() == currentBlock.GetXYZGridLocation() + new Vector3Int(0, 0, 1));

                    south = targetList[targetFloor.Key]
                        .FirstOrDefault(x =>
                            x.GetXYZGridLocation() == currentBlock.GetXYZGridLocation() + new Vector3Int(0, 0, -1));

                    east = targetList[targetFloor.Key]
                        .FirstOrDefault(x =>
                            x.GetXYZGridLocation() == currentBlock.GetXYZGridLocation() + new Vector3Int(1, 0, 0));

                    west = targetList[targetFloor.Key]
                        .FirstOrDefault(x =>
                            x.GetXYZGridLocation() == currentBlock.GetXYZGridLocation() + new Vector3Int(-1, 0, 0));

                    //TODO ExtendedNeighbour should be default type, use metadata to use ExtendedNeighbourData when more info needed

                    if (currentBlock.name == "Flowers")
                    {
                        //Debugger.Break();
                    }
                    
                    currentBlock.SetNeighbours(north, east, south, west, above, under, eastUnder, westUnder);
                }
            }
        }

        private void CreateStaticBlocks(JsonBlock[] groundData, Block[] targetList, Transform parent)
        {
            for (int i = 0; i < groundData.Length; i++)
            {
                var blockData = groundData[i];
                GameObject newObj = AddressablesManager.FindFromCacheAndInstantiatePrefab(blockData.type.ToString(),
                        new Vector3(blockData.x, blockData.y, blockData.z));

                var tile = newObj.GetComponent<Block>();
                tile.InitStaticBlock(blockData, parent);
                targetList[i] = tile;
            }
        }
    }
}