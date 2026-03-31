using System;
using System.Collections.Generic;
using System.Linq;
using BlockAndDagger.Utils;
using Unity.AI.Navigation;
using UnityEngine;
using UnityEngine.AI;
using VContainer;

namespace BlockAndDagger
{
    public sealed class Level : SingletonMono<Level>
    {
        [Tooltip("Upper level platform")] [SerializeField]
        private Transform m_groundThree;

        [Tooltip("Main building level, trees, rocks etc")] [SerializeField]
        private Transform m_groundTwo;

        [Tooltip("Walking platform and pathfinding is created, if nothing then entity can fall")] [SerializeField]
        private Transform m_groundOne;

        [Tooltip("Water and other non-collider underground visuals")] [SerializeField]
        private Transform m_groundZero;

        [SerializeField] private LevelData _levelData;
        public LevelData LevelData => _levelData;

        public TileDataTracker TileDataTracker = new(); //TODO: switch to TileDataTrackerVector3Int

        [SerializeField] public TilemapManager m_staticTilemaps; //TODO: should not be public

        [SerializeField] private Transform m_waterPlane;
        [SerializeField] private Transform m_greenPlane;

        public int blueprintCount;

        //Extra stuff to post calculate from data
        public Vector3 StartPosition { get; private set; }
        public Block StartPositionBlock { get; private set; }
        public NavMeshSurface StartPositionNavMeshSurface { get; private set; }
        public Block[] SpawnPositions { get; private set; }

        [SerializeField] public Transform[] m_borders;

        private ILogger _logger;

        [Inject]
        public void Construct(ILogger log)
        {
            _logger = log;
        }

        private new void Awake()
        {
            base.Awake();
        }

        public Transform GetGroundFloorTransform(BlockDepth blockDepth)
        {
            switch (blockDepth)
            {
                case BlockDepth.UpperlevelStructures:
                    return m_groundThree;
                case BlockDepth.MainStructures:
                    return m_groundTwo;
                case BlockDepth.WalkingPlatform:
                    return m_groundZero;
                case BlockDepth.Water:
                    throw new NotImplementedException("Zero/Water ground not supported");
            }

            throw new ArgumentOutOfRangeException(nameof(blockDepth), blockDepth, "BlockDepth not yet supported");
        }

        public void BuildNavMeshSurface()
        {
            Debug.Log($"Building NavMesh from {LevelData.TileCount} blocks");

            //StartPositionNavMeshSurface.GetBuildSettings().ledgeDropHeight = 30f;
            StartPositionNavMeshSurface.BuildNavMesh();
        }

        public void CleanLevel()
        {
            m_staticTilemaps.ClearAll();
            _levelData.EmptyLevelData();
            StartPosition = default;
            StartPositionBlock = null;
            if (StartPositionNavMeshSurface != null)
            {
                StartPositionNavMeshSurface.RemoveData();
            }

            SpawnPositions = default;
            //TileDataTracker = new(); //TODO: rethink ... currently nothing added
            AddressablesManager.ReleasePreloadedGroup(); //TODO release map specific assets, block and music etc
        }

        ///Use case is when player does not want to save the level.
        ///Ideally update only new block changes
        public void RefreshLevelDataAfterEdit()
        {
            var threes = GetTiles(m_groundThree);
            var twos = GetTiles(m_groundTwo);
            var ones = GetTiles(m_groundOne);
            var zeros = GetTiles(m_groundZero);

            foreach (var block in twos)
            {
                block.PopulateJsonData();
            }

            foreach (var block in ones)
            {
                block.PopulateJsonData();
            }

            foreach (var block in zeros)
            {
                block.PopulateJsonData();
            }

            (Block[] staticMainstructures, Block[] staticWalkingPlatform) = PopulateStaticTilemaps();

            SpawnPositions = twos.Where(x => x.Data.type == TileType.EnemySpawner).ToArray();

            _levelData = new LevelData(_levelData.LevelName,
                _levelData.Blueprint,
                _levelData.Description,
                threes,
                twos,
                ones,
                zeros,
                _levelData.LevelEvents, //TODO: //new LevelScenarioData(GetSpawnerComponents(SpawnPositions)), 
                staticMainstructures,
                staticWalkingPlatform,
                _levelData.ChallengeInfo
            );
        }

        private SpawnerBlock[] GetSpawnerComponents(Block[] spawnPositions)
        {
            List<SpawnerBlock> spawnerBlocks = new();
            foreach (var spawn in spawnPositions)
            {
                spawnerBlocks.Add(spawn.GetComponent<SpawnerBlock>());
            }

            return spawnerBlocks.ToArray();
        }

        private (Block[] staticMainstructures, Block[] staticWalkingPlatform) PopulateStaticTilemaps()
        {
            var staticWalkingPlatform = m_staticTilemaps.GetTileMap(StaticGridPrefix + BlockDepth.WalkingPlatform);
            var staticMainStructures = m_staticTilemaps.GetTileMap(StaticGridPrefix + BlockDepth.MainStructures);
            var walking = GetTiles(staticWalkingPlatform);
            var structures = GetTiles(staticMainStructures);

            foreach (var block in walking)
            {
                block.PopulateJsonData();
            }

            foreach (var block in structures)
            {
                block.PopulateJsonData();
            }

            return (structures, walking);
        }

        private const string StaticGridPrefix = "Static_";

        public void RefreshLevelDataForSaving(bool storeOnlyBlueprints = false)
        {
            var threes = GetTiles(m_groundThree);
            var twos = GetTiles(m_groundTwo);
            var ones = GetTiles(m_groundOne);
            var zeros = GetTiles(m_groundZero);
            var staticWalkingPlatformTransform =
                m_staticTilemaps.GetTileMap(StaticGridPrefix + BlockDepth.WalkingPlatform);
            var staticMainStructuresTransform =
                m_staticTilemaps.GetTileMap(StaticGridPrefix + BlockDepth.MainStructures);

            if (storeOnlyBlueprints)
            {
                threes = threes.Where(x => x.IsBluePrintBlock).ToArray();
                twos = twos.Where(x => x.IsBluePrintBlock).ToArray();
                ones = ones.Where(x => x.IsBluePrintBlock).ToArray();
                zeros = zeros.Where(x => x.IsBluePrintBlock).ToArray();
            }

            var staticMainStructures = GetTiles(staticMainStructuresTransform);
            var staticWalkingPlatform = GetTiles(staticWalkingPlatformTransform);
            PopulateListJsonData(staticMainStructures);
            PopulateListJsonData(staticWalkingPlatform);
            PopulateListJsonData(threes);
            PopulateListJsonData(twos);
            PopulateListJsonData(ones);
            PopulateListJsonData(zeros);

            SpawnPositions = twos.Where(x => x.Data.type == TileType.EnemySpawner).ToArray();

            _levelData = new LevelData(_levelData.LevelName,
                _levelData.Blueprint,
                _levelData.Description,
                threes,
                twos,
                ones,
                zeros,
                _levelData.LevelEvents,
                staticMainStructures,
                staticWalkingPlatform,
                _levelData.ChallengeInfo,
                storeOnlyBlueprints
            );

            if (storeOnlyBlueprints)
            {
                return;
            }

            StartPosition = GetStartPos(twos);

            if (_levelData.GroundTwo.Length == 0 && _levelData.GroundOne.Length == 0 &&
                _levelData.GroundZero.Length == 0)
            {
                throw new ArgumentNullException("Zero ground prefabs found");
            }
        }

        private void PopulateListJsonData(Block[] list)
        {
            foreach (var block in list)
            {
                if (block.TileType == TileType.ERROR_NOT_DEFINED)
                {
                    throw new InvalidOperationException("Tiletype not set!");
                }

                block.PopulateJsonData();
            }
        }

        private Vector3 FindNorthestBlock(Block[] groundTwo)
        {
            var northest = groundTwo.OrderBy(x => x.Data.x).ToList().First();
            return new Vector3(northest.Data.x, northest.Data.y, northest.Data.z);
        }

        private Vector3 FindSouthestBlock(Block[] groundTwo)
        {
            var southest = groundTwo.OrderByDescending(x => x.Data.x).ToList().First();
            return new Vector3(southest.Data.x, southest.Data.y, southest.Data.z);
        }

        public void SetLevelByCombiningBaseToPredefinedBlueprint(LevelAndBlueprint levelAndBlueprint,
            LevelLoader levelLoader)
        {
            _levelData = levelLoader.LoadPredefinedBlueprint(levelAndBlueprint);

            //TODO: sort this out, blueprint needs to have a value but once you press save it should force to create new(clean the blueprint name)
            if (!_levelData.Blueprint.HasValue && !_levelData.IsPredefinedBlueprint)
            {
                throw new InvalidOperationException("predefined blueprint load");
            }

            InitLevel();
        }

        public void SetLevel(LevelAndBlueprint levelAndBlueprint, LevelLoader levelLoader)
        {
            if (string.IsNullOrWhiteSpace(levelAndBlueprint.BlueprintName))
            {
                _levelData = levelLoader.LoadEmptyLevel(levelAndBlueprint.Level);
            }
            else
            {
                _levelData = levelLoader.LoadEditedLevel(levelAndBlueprint);
                if (!_levelData.Blueprint.HasValue)
                {
                    throw new InvalidOperationException("Game loaded edited level which has no blueprint value?");
                }
            }

            InitLevel();
            SetPlane();
        }

        //Very temporary implementation
        private void SetPlane()
        {
            if (_levelData.LevelName == LevelName.Level_1.ToString())
            {
                m_waterPlane.gameObject.SetActive(true);
                m_greenPlane.gameObject.SetActive(false);
                m_waterPlane.localScale = new Vector3(2, m_greenPlane.localScale.y, 2);
            }
            else if ((_levelData.LevelName == LevelName.Level_2.ToString()) ||
                     (_levelData.LevelName == LevelName.Level_3.ToString()) ||
                     _levelData.LevelName == LevelName.Level_5.ToString())
            {
                m_waterPlane.gameObject.SetActive(true);
                m_greenPlane.gameObject.SetActive(false);
                m_waterPlane.localScale = new Vector3(10, m_greenPlane.localScale.y, 10);
            }
            else if (_levelData.LevelName == LevelName.Level_4.ToString())
            {
                m_waterPlane.gameObject.SetActive(false);
                m_greenPlane.gameObject.SetActive(true);
                m_greenPlane.localScale = new Vector3(17, m_greenPlane.localScale.y, 10);
            }
            else
            {
                m_waterPlane.gameObject.SetActive(false);
                m_greenPlane.gameObject.SetActive(false);
            }
        }

        private void InitLevel()
        {
            foreach (var block in _levelData.GroundThree)
            {
                block.gameObject.transform.SetParent(m_groundThree);
            }

            foreach (var block in _levelData.GroundTwo)
            {
                block.gameObject.transform.SetParent(m_groundTwo);
            }

            foreach (var block in _levelData.GroundOne)
            {
                block.gameObject.transform.SetParent(m_groundOne);
            }

            foreach (var block in _levelData.GroundZero)
            {
                block.gameObject.transform.SetParent(m_groundZero);
            }

            StartPosition = GetStartPos(_levelData.GroundTwo);

            if (_levelData.ChallengeInfo == null)
            {
                Debug.LogError("ChallengeInfo missing!");
            }
            else
            {
                SetChallengeSettings(_levelData.ChallengeInfo);
            }
            
            SpawnPositions = _levelData.GroundTwo.Where(x => x.Data.type == TileType.EnemySpawner).ToArray();

            TileDataTracker.AddBlocksToList(_levelData.GroundThree);
            TileDataTracker.AddBlocksToList(_levelData.GroundTwo);
            TileDataTracker.AddBlocksToList(_levelData.GroundOne);
            TileDataTracker.AddBlocksToList(_levelData.GroundZero);

#if UNITY_EDITOR
            //Gives ids to show spawner gizmo
            for (var i = 0; i < SpawnPositions.Length; i++)
            {
                SpawnPositions[i].GetComponent<SpawnerBlock>().InitGizmo(i);
            }
#endif
        }

        private void SetChallengeSettings(ChallengeInfo challengeInfo)
        {
            StartPositionBlock = GetStartPositionBlock(_levelData.GroundTwo);
            StartPositionNavMeshSurface = StartPositionBlock.GetComponent<NavMeshSurface>();

            ChallengeType challengeType = challengeInfo.ChallengeType;
            switch (challengeType)
            {
                case ChallengeType.NOT_DEFINED:
                    //_logger.LogWarning("TypeOfChallenge is NOT_DEFINED, level has no goal");
                    return;
                case ChallengeType.AgainstTime:
                    //TODO: set timer

                    break;
                case ChallengeType.GetToTarget:
                   
                    break;
                case ChallengeType.Collect:
                    //count if type of

                    break;
                case ChallengeType.SlayEnemies:
                    break;
                //kill count
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        //TODO: not all visuals like flowers and small plants should activate colliders
        public void ActivatePlayModeSettings()
        {
            RefreshLevelDataAfterEdit();
            UnbuildPreviouslyBuildBlocks();

            foreach (var block in _levelData.GroundThree)
            {
                block.SetPlayModeSettings();
            }

            foreach (var block in _levelData.GroundTwo)
            {
                block.SetPlayModeSettings();
            }

            foreach (var block in _levelData.GroundOne)
            {
                block.SetPlayModeSettings();
            }


            OptimizeWalkingPlatform_TEMP_FIX(_levelData.GroundOne, _levelData.GroundTwo);

            foreach (var block in _levelData.GroundZero)
            {
                block.SetPlayModeSettings();
            }
            
            FillAIDrops();

            RemoveUselessComponents(_levelData.StaticWalkingPlatform);
            RemoveUselessComponents(_levelData.StaticMainStructures);
        }
        
        //Workaround thingi. TODO refactoring:Probably best to call at Play Again button
        private void UnbuildPreviouslyBuildBlocks()
        {
            ResetBuiltFlags(_levelData.GroundThree);
            ResetBuiltFlags(_levelData.GroundTwo);
            ResetBuiltFlags(_levelData.GroundOne);
            ResetBuiltFlags(_levelData.GroundZero);
            ResetBuiltFlags(_levelData.StaticWalkingPlatform);
            ResetBuiltFlags(_levelData.StaticMainStructures);
            void ResetBuiltFlags(Block[] blocks)
            {
                if (blocks == null)
                {
                    return;
                }
                
                foreach (var block in blocks)
                {
                    //TODO: remove from Navmesh also 
                    if (block == null)
                    {
                        continue;
                    }
                    var naveMesh = block.gameObject.GetComponent<NavMeshObstacle>();
                    if (naveMesh != null)
                    {
                        naveMesh.enabled = false;
                    }
                    block.IsBuild = false;
                }
            }
        }

        //Removes renderer (purple material) and fills same size drops
        private void FillAIDrops()
        {
            foreach (var block in _levelData.GroundTwo)
            {
                if (block.TileType != TileType.AIDropDown)
                {
                    return;
                }
                
                block.GetComponent<MeshRenderer>().enabled = false;
                
                //TODO:
            }
        }

        public void SetLevelMakerSettings()
        {
            foreach (var block in _levelData.GroundThree)
            {
                block.SetLevelBuilderModeSettings();
            }

            foreach (var block in _levelData.GroundTwo)
            {
                block.SetLevelBuilderModeSettings();
            }

            foreach (var block in _levelData.GroundOne)
            {
                block.SetLevelBuilderModeSettings();
            }

            foreach (var block in _levelData.GroundZero)
            {
                block.SetLevelBuilderModeSettings();
            }
        }


        private void RemoveUselessComponents(Block[] staticBlocks)
        {
            //TODO: add yellow shade material at LevelBuilder mode to ease level making
            foreach (var block in staticBlocks)
            {
                //Not needed when in-game
                Destroy(block.GetComponent<Block>());

                if (block.TileType == TileType.InvisibleWall)
                {
                    block.GetComponent<MeshRenderer>().enabled = false;
                }
            }
        }

        //Removes useless blocks or colliders which have an indestructible block above
        public void OptimizeWalkingPlatform_TEMP_FIX(Block[] walkingPlatform, Block[] mainStructures)
        {
            foreach (var mainBlock in mainStructures)
            {
                Block blockUnderneath = null;
                foreach (var walk in walkingPlatform)
                {
                    if (Math.Abs(walk.Data.x - mainBlock.Data.x) <= TileDataTracker.VectorComparisonTolerance &&
                        Math.Abs(walk.Data.z - mainBlock.Data.z) <= TileDataTracker.VectorComparisonTolerance)
                    {
                        blockUnderneath = walk;
                    }

                    if (blockUnderneath is null)
                    {
                        continue;
                    }

                    if (!mainBlock.IsBluePrintBlock && (mainBlock.Data.type == TileType.Block ||
                                                   mainBlock.Data.type == TileType.TreePine ||
                                                   mainBlock.Data.type == TileType.Rocks))
                    {
                        if (LevelData.LevelName == LevelName.Level_1.ToString() ||
                            LevelData.LevelName == LevelName.Level_2.ToString() ||
                            LevelData.LevelName == LevelName.Level_5.ToString())
                        {
                            OptimizeLevelsWhichDoNotUtilizeVisualPlane(blockUnderneath);
                        }
                        else
                        {
                            //This will create a hole but the visual plane will cover this
                            Destroy(blockUnderneath.gameObject);
                        }

                        //TODO: lastly
                        /*if (blockUnderneath.gameObject is not null)
                        {
                            //TODO:script not needed, until something interactable is implemented
                            Destroy(blockUnderneath.GetComponent<Block>());
                        }*/
                    }
                    
                    break;
                }
            }
        }

        private void OptimizeLevelsWhichDoNotUtilizeVisualPlane(Block blockUnderneath)
        {
            var navMeshObstacle = blockUnderneath.GetComponent<NavMeshObstacle>();
            if (navMeshObstacle != null)
            {
                Destroy(navMeshObstacle);
            }

            var sphereCollider = blockUnderneath.GetComponent<SphereCollider>();
            if (sphereCollider != null)
            {
                Destroy(sphereCollider);
            }

            var boxCollider = blockUnderneath.GetComponent<BoxCollider>();
            if (boxCollider != null)
            {
                Destroy(boxCollider);
            }
        }

        private Vector3 GetStartPos(Block[] groundTwo)
        {
            var flag = groundTwo.First(x => x.name == Constants.StartPositionSymbol.ToString()).transform.position;
            return new Vector3(flag.x, flag.y + Constants.SpawningPosOffsetY, flag.z);
        }

        /// <summary>
        /// Supports east or west side. More later if needed
        /// Note! Returns the block below the target, so block in the WalkingPlatform
        /// </summary>
        /// <param name="preferToTryEastFirst"></param>
        /// <returns></returns>
        public IBlock GetBlockNextToStarPosition(bool preferToTryEastFirst)
        {
            var startName = Constants.StartPositionSymbol.ToString();
            var startBlock = _levelData.GroundTwo.FirstOrDefault(x => x.name == startName);
            if (startBlock == null)
            {
                Debug.LogWarning("Start position block not found in GroundTwo.");
                return null;
            }
            //TODO: the code is too forgiving(design/bug), should also check startBlock.NeighbourData.EastUnder.NeighbourData.Above != null
            var extended = startBlock.NeighbourData as ExtendedNeighbourData;
            return preferToTryEastFirst ? (extended?.EastUnder ?? extended?.WestUnder) : (extended?.WestUnder ?? extended?.EastUnder);
        }

        /// <summary>
        /// Useful when there are map is being rotated, means the position will taken from start pos gameobject vs Block data
        /// </summary>
        private Block GetStartPositionBlock(Block[] groundTwo)
        {
            return groundTwo.First(x => x.name == Constants.StartPositionSymbol.ToString());
        }

        private Block[] GetTiles(Transform transform)
        {
            var tiles = transform.GetComponentsInChildren<Block>();
            return tiles ?? Array.Empty<Block>();
        }
    }
}