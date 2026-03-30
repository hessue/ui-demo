using System;
using System.Collections.Generic;
using System.Linq;
using BlockAndDagger.Core;
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
        [field: SerializeField] public int? Blueprint { get; private set; } //TODO: show on inspector, currently not showing

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

        [field: SerializeField] public ManifestEntry[] ManifestEntries { get; private set; }

        private IList<GameObject> m_preloadedBlockPrefabs;
                private readonly IAddressablesManager _addressablesManager;

                public void SetBlueprint(int currentBlueprintCount, bool isPredefined = false)
                {
                    Blueprint = currentBlueprintCount;
                    IsPredefinedBlueprint = isPredefined;
                }

        public void SetManifestEntries(ManifestEntry[] entries)
        {
            ManifestEntries = entries;
        }
        
        public LevelData(JsonLevelData levelData, IAddressablesManager addressablesManager)
        {
                    _addressablesManager = addressablesManager;
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

            m_preloadedBlockPrefabs = _addressablesManager?.GetPreloadedGroup() ?? new System.Collections.Generic.List<GameObject>();

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
                    Block[] staticMainStructures, Block[] staticWalkingPlatform, ChallengeInfo challengeInfo, bool isPredefinedBlueprint = false, IAddressablesManager addressablesManager = null)
        {
                    _addressablesManager = addressablesManager;
                    LevelName = levelName;
                    Blueprint = blueprint;
                    IsPredefinedBlueprint = isPredefinedBlueprint;
                    Description = description;
                    m_preloadedBlockPrefabs = _addressablesManager?.GetPreloadedGroup() ?? new System.Collections.Generic.List<GameObject>();
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
        public LevelData(JsonLevelData levelData, JsonLevelData predefinedBlueprintData, IAddressablesManager addressablesManager = null)
        {
                    _addressablesManager = addressablesManager;
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
            m_preloadedBlockPrefabs = _addressablesManager?.GetPreloadedGroup() ?? new System.Collections.Generic.List<GameObject>();

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

            // Old logic: remove tiles from the tracker (keeps data cleanup separate)
            DestroyGroundBlocks(GroundThree);
            DestroyGroundBlocks(GroundTwo);
            DestroyGroundBlocks(GroundOne);
            DestroyGroundBlocks(GroundZero);

            // Release instantiated GameObjects (separated so callers can reuse/opt-out)
            ReleaseGroundInstances(GroundThree);
            ReleaseGroundInstances(GroundTwo);
            ReleaseGroundInstances(GroundOne);
            ReleaseGroundInstances(GroundZero);

            // Clear references so Level is put into a true empty/default state
            GroundThree = Array.Empty<Block>();
            GroundTwo = Array.Empty<Block>();
            GroundOne = Array.Empty<Block>();
            GroundZero = Array.Empty<Block>();
        }

        private void DestroyGroundBlocks(Block[] ground)
        {
            if (ground is not null)
            {
                var tracker = GameManager.Instance.LevelMaker.m_activeLevel.TileDataTracker;
                foreach (var block in ground)
                {
                    if (block == null)
                    {
                        continue;
                    }

                    tracker.RemoveTile(block); //json data vs transform.position?
                }
            }
        }

        /// <summary>
        /// Releases instantiated GameObjects for the provided ground array without touching the TileDataTracker.
        /// Separated from DestroyGroundBlocks so callers can choose which cleanup steps to run.
        /// </summary>
        public void ReleaseGroundInstances(Block[] ground)
        {
            if (ground is null)
            {
                return;
            }

            foreach (var block in ground)
            {
                if (block == null)
                {
                    continue;
                }

                _addressablesManager?.ReleaseInstance(block.gameObject);
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
                   newObj = _addressablesManager?.FindFromCacheAndInstantiatePrefab(blockData.type.ToString(),
                      new Vector3(blockData.x, blockData.y, blockData.z));
                }

                if (newObj == null)
                {
                    var prefabName = blockData.type.ToString();
                    var preloadedNames = m_preloadedBlockPrefabs != null ? string.Join(", ", m_preloadedBlockPrefabs.Select(g => g != null ? g.name : "(null)")) : "(none)";
                    Debug.LogError($"Prefab '{prefabName}' not found in preloaded cache. Preloaded prefabs: {preloadedNames}. Attempting direct instantiate by key...");
                    // Fallback: try to instantiate directly by addressable key (prefabName)
                    newObj = _addressablesManager?.InstantiatePrefab(prefabName, new Vector3(blockData.x, blockData.y, blockData.z), null);
                    if (newObj == null)
                    {
                        Debug.LogError($"Prefab '{prefabName}' could not be instantiated from Addressables. Check addressable manifest or level manifest entries for level '{LevelName}'.");
                        throw new InvalidOperationException($"Prefab '{prefabName}' not found or failed to instantiate for level '{LevelName}'.");
                    }
                }

                var block = newObj.GetComponent<Block>();
                if (block == null)
                {
                    Debug.LogError($"Instantiated object '{newObj?.name ?? "(null)"}' is missing Block component for prefab '{(newObj != null ? newObj.name : blockData.type.ToString())}'.");
                    throw new InvalidOperationException($"Prefab '{(newObj != null ? newObj.name : blockData.type.ToString())}' does not contain a Block component.");
                }

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

                tracker.AddOrReplaceTile(TileDataTracker.ToGridPosition(new Vector3(blockData.x, blockData.y, blockData.z)), block);
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
            // Build quick lookup dictionaries per floor to avoid repeated O(n) scans
            var lookup = new Dictionary<int, System.Collections.Generic.Dictionary<UnityEngine.Vector3Int, Block>>();
            foreach (var kv in targetList)
            {
                int floor = kv.Key;
                var blocks = kv.Value ?? System.Array.Empty<Block>();
                var dict = new System.Collections.Generic.Dictionary<UnityEngine.Vector3Int, Block>(blocks.Length);
                for (int i = 0; i < blocks.Length; i++)
                {
                    var b = blocks[i];
                    var p = TileDataTracker.ToGridPosition(new UnityEngine.Vector3(b.Data.x, b.Data.y, b.Data.z));
                    dict[p] = b;
                }

                lookup[floor] = dict;
            }

            foreach (var targetFloor in targetList)
            {
                int floorKey = targetFloor.Key;
                var blocks = targetFloor.Value ?? System.Array.Empty<Block>();
                var currentFloorDict = lookup[floorKey];

                lookup.TryGetValue(floorKey + 1, out var aboveDict);
                lookup.TryGetValue(floorKey - 1, out var belowDict);

                for (int i = 0; i < blocks.Length; i++)
                {
                    var currentBlock = blocks[i];
                    var pos = TileDataTracker.ToGridPosition(new UnityEngine.Vector3(currentBlock.Data.x, currentBlock.Data.y, currentBlock.Data.z));
                    var gridStep = TileDataTracker.GridStep;

                    IBlock north = null, south = null, east = null, west = null, above = null, under = null, eastUnder = null, westUnder = null;

                    if (aboveDict != null)
                    {
                        aboveDict.TryGetValue(pos + new UnityEngine.Vector3Int(0, gridStep, 0), out var aboveBlock);
                        above = aboveBlock;
                    }

                    if (belowDict != null)
                    {
                        belowDict.TryGetValue(pos + new UnityEngine.Vector3Int(0, -gridStep, 0), out var underBlock);
                        under = underBlock;

                        belowDict.TryGetValue(pos + new UnityEngine.Vector3Int(gridStep, -gridStep, 0), out var eUnder);
                        eastUnder = eUnder;

                        belowDict.TryGetValue(pos + new UnityEngine.Vector3Int(-gridStep, -gridStep, 0), out var wUnder);
                        westUnder = wUnder;
                    }

                    currentFloorDict.TryGetValue(pos + new UnityEngine.Vector3Int(0, 0, gridStep), out var northB);
                    north = northB;

                    currentFloorDict.TryGetValue(pos + new UnityEngine.Vector3Int(0, 0, -gridStep), out var southB);
                    south = southB;

                    currentFloorDict.TryGetValue(pos + new UnityEngine.Vector3Int(gridStep, 0, 0), out var eastB);
                    east = eastB;

                    currentFloorDict.TryGetValue(pos + new UnityEngine.Vector3Int(-gridStep, 0, 0), out var westB);
                    west = westB;

                    currentBlock.SetNeighbours(north, east, south, west, above, under, eastUnder, westUnder);
                }
            }
        }

        private void CreateStaticBlocks(JsonBlock[] groundData, Block[] targetList, Transform parent)
        {
            for (int i = 0; i < groundData.Length; i++)
            {
                var blockData = groundData[i];
                GameObject newObj = _addressablesManager?.FindFromCacheAndInstantiatePrefab(blockData.type.ToString(),
                        new Vector3(blockData.x, blockData.y, blockData.z));

                var tile = newObj.GetComponent<Block>();
                tile.InitStaticBlock(blockData, parent);
                targetList[i] = tile;
            }
        }
    }
}
