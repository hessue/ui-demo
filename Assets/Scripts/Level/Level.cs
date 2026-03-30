using System;
using System.Collections.Generic;
using ZLinq;
using BlockAndDagger.Core;
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

        public TileDataTracker TileDataTracker;
        private readonly System.Collections.Generic.List<Block> _tilesBuffer = new System.Collections.Generic.List<Block>(1024);

        [SerializeField] public TilemapManager m_staticTilemaps; //TODO: should not be public

        [SerializeField] private Transform m_waterPlane;
        public Transform WaterPlane => m_waterPlane;
        [SerializeField] private Transform m_greenPlane;
        public Transform GreenPlane => m_greenPlane;

        public int blueprintCount;

        //Extra stuff to post calculate from data
        public Vector3 StartPosition { get; private set; }
        [field:SerializeField] public Block StartPositionBlock { get; private set; }
        public NavMeshSurface StartPositionNavMeshSurface { get; private set; }
        public Block[] SpawnPositions { get; private set; }

        [SerializeField] public Transform[] m_borders;

        private ILogger _logger;
        private IAddressablesManager _addressablesManager;

        [Inject]
        public void Construct(ILogger log, IAddressablesManager addressablesManager, TileDataTracker tileDataTracker)
        {
            _logger = log;
            _addressablesManager = addressablesManager;
            TileDataTracker = tileDataTracker;
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

        public void CleanLevel(bool cleanOnlyUserBlocks = true)
        {
            if (!cleanOnlyUserBlocks)
            {
                //TODO: all grounds and tilemaps need to be cleaned, should loop each ground go and not FindObjectsByType
                var allBlocks = UnityEngine.Object.FindObjectsByType<Block>(UnityEngine.FindObjectsSortMode.None);
                foreach (var block in allBlocks)
                {
                    if (block == null || block.gameObject == null)
                    {
                        continue;
                    }

                    _addressablesManager?.ReleaseInstance(block.gameObject);
                }

                if (TileDataTracker != null)
                {
                    TileDataTracker.TileDataList.Clear();
                }
            }
            else
            {
                ReleaseInstantiatedBlocks(true);
            }

            if (m_staticTilemaps != null)
            {
                m_staticTilemaps.ClearAll();
            }

            _levelData.EmptyLevelData();
            StartPosition = default;
            StartPositionBlock = null;
            if (StartPositionNavMeshSurface != null)
            {
                StartPositionNavMeshSurface.RemoveData();
            }

            SpawnPositions = default;
        }

        /// <summary>
        /// Release instantiated Block objects that live under the level ground parents.
        /// If targetUserBlocksOnly is true, only blocks marked as IsBluePrintBlock will be released.
        /// Default (false) preserves previous behavior and releases all instances.
        /// </summary>
        public void ReleaseInstantiatedBlocks(bool targetUserBlocksOnly = false)
        {
            TryReleaseBlocksUnderParent(m_groundThree, targetUserBlocksOnly);
            TryReleaseBlocksUnderParent(m_groundTwo, targetUserBlocksOnly);
            TryReleaseBlocksUnderParent(m_groundOne, targetUserBlocksOnly);
            TryReleaseBlocksUnderParent(m_groundZero, targetUserBlocksOnly);
        }

        // Helper to release all Block children under a parent transform safely
        private void TryReleaseBlocksUnderParent(Transform parent, bool targetUserBlocksOnly)
        {
            if (parent == null)
            {
                return;
            }

            _tilesBuffer.Clear();
            parent.GetComponentsInChildren(true, _tilesBuffer);
            foreach (var block in _tilesBuffer)
            {
                if (block == null || block.gameObject == null)
                {
                    continue;
                }

                // If requested, skip blocks that are not user/blueprint created
                if (targetUserBlocksOnly && !block.IsBluePrintBlock)
                {
                    continue;
                }

                _addressablesManager?.ReleaseInstance(block.gameObject);
            }
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

            SpawnPositions = twos.AsValueEnumerable().Where(x => x.Data.type == TileType.EnemySpawner).ToArray();

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
                _levelData.ChallengeInfo,
                _levelData.IsPredefinedBlueprint,
                _addressablesManager
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


#if UNITY_EDITOR
        private List<TileType> _tileTypesForAddressableCreation = new();
        public List<TileType> GetUsedTileTypes()
        { 
            return _tileTypesForAddressableCreation.AsValueEnumerable().Distinct().ToList();
        }
#endif
        
        
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
                threes = threes.AsValueEnumerable().Where(x => x.IsBluePrintBlock).ToArray();
                twos = twos.AsValueEnumerable().Where(x => x.IsBluePrintBlock).ToArray();
                ones = ones.AsValueEnumerable().Where(x => x.IsBluePrintBlock).ToArray();
                zeros = zeros.AsValueEnumerable().Where(x => x.IsBluePrintBlock).ToArray();
            }

            var staticMainStructures = GetTiles(staticMainStructuresTransform);
            var staticWalkingPlatform = GetTiles(staticWalkingPlatformTransform);
            
#if UNITY_EDITOR
            _tileTypesForAddressableCreation.Clear();
#endif
            PopulateListJsonData(staticMainStructures);
            PopulateListJsonData(staticWalkingPlatform);
            PopulateListJsonData(threes);
            PopulateListJsonData(twos);
            PopulateListJsonData(ones);
            PopulateListJsonData(zeros);

            SpawnPositions = twos.AsValueEnumerable().Where(x => x.Data.type == TileType.EnemySpawner).ToArray();

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
                storeOnlyBlueprints,
                _addressablesManager
            );

            if (storeOnlyBlueprints)
            {
                return;
            }

            StartPositionBlock = GetStartPos(twos, ones); //TODO: needs add NavMeshSurface
            var start = StartPositionBlock.transform.position;
            StartPosition = new Vector3(start.x, start.y + Constants.SpawningPosOffsetY, start.z);

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
#if UNITY_EDITOR
                if (!_tileTypesForAddressableCreation.Contains(block.TileType))
                {
                    _tileTypesForAddressableCreation.Add(block.TileType);
                }
#endif
                
                if (block.TileType == TileType.ERROR_NOT_DEFINED)
                {
                    throw new InvalidOperationException("Tiletype not set!");
                }

                block.PopulateJsonData();
            }
        }

        private Vector3 FindNorthestBlock(Block[] groundTwo)
        {
            var northest = groundTwo.AsValueEnumerable().OrderBy(x => x.Data.x).First();
            return new Vector3(northest.Data.x, northest.Data.y, northest.Data.z);
        }

        private Vector3 FindSouthestBlock(Block[] groundTwo)
        {
            var southest = groundTwo.AsValueEnumerable().OrderByDescending(x => x.Data.x).First();
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

        private const float WaterPlaneScale = 6f;
        //Very temporary implementation!
        // Defensive checks: when Level is preserved across scene loads some serialized Transforms
        // may have been destroyed by the scene unload. Guard against that and skip plane setup
        // when required references are missing.
        private void SetPlane()
        {
            var menuControls = GameManager.Instance?.MenuManager?.MenuControls;
            
            //TODO: remove this crap
            if (m_waterPlane == null && m_greenPlane == null)
            {
                var rebind = TryRebindSceneReferences();
                if (!rebind)
                {
                    Debug.LogWarning("Rebind failed");
                    return;
                }
            }

            bool hasWaterPlane = m_waterPlane != null;
            bool hasGreenPlane = m_greenPlane != null;

            if (_levelData.LevelName == nameof(LevelName.Level_1) || _levelData.LevelName == nameof(LevelName.Level_2))
            {
                if (hasWaterPlane)
                {
                    m_waterPlane.gameObject.SetActive(true);
                    // Use green plane's Y scale if available, otherwise keep current Y scale.
                    var greenY = hasGreenPlane ? m_greenPlane.localScale.y : m_waterPlane.localScale.y;
                    m_waterPlane.localScale = new Vector3(WaterPlaneScale, greenY, WaterPlaneScale);
                }

                if (hasGreenPlane)
                {
                    m_greenPlane.gameObject.SetActive(false);
                }

                menuControls?.SetPreviewPanLimiter(this, 1, 10, -5);
            }
            else if (_levelData.LevelName == nameof(LevelName.Level_3) ||
                     _levelData.LevelName == nameof(LevelName.Level_5))
            {
                if (hasWaterPlane)
                {
                    m_waterPlane.gameObject.SetActive(true);
                    var greenY = hasGreenPlane ? m_greenPlane.localScale.y : m_waterPlane.localScale.y;
                    m_waterPlane.localScale = new Vector3(10, greenY, 10);
                }

                if (hasGreenPlane)
                {
                    m_greenPlane.gameObject.SetActive(false);
                }

                menuControls?.SetPreviewPanLimiter(this, 1, 10, -5);
            }
            else if (_levelData.LevelName == nameof(LevelName.Level_4))
            {
                if (hasWaterPlane)
                {
                    m_waterPlane.gameObject.SetActive(false);
                }

                if (hasGreenPlane)
                {
                    m_greenPlane.gameObject.SetActive(true);
                    m_greenPlane.localScale = new Vector3(17, m_greenPlane.localScale.y, 10);
                }
            }
            else
            {
                if (hasWaterPlane)
                {
                    m_waterPlane.gameObject.SetActive(false);
                }

                if (hasGreenPlane)
                {
                    m_greenPlane.gameObject.SetActive(false);
                }
            }
        }

        private void InitLevel()
        {
            foreach (var block in _levelData.GroundThree)
            {
                if (m_groundThree != null)
                {
                    block.gameObject.transform.SetParent(m_groundThree);
                    // Clear preserved metadata now that block is parented back into the scene
                    block.SetPreservedOriginalParentName(null);
                }
                else
                {
                    // If the ground parent was destroyed (Level persisted across scenes), keep blocks at root
                    block.gameObject.transform.SetParent(null);
                }
            }

            foreach (var block in _levelData.GroundTwo)
            {
                if (m_groundTwo != null)
                {
                    block.gameObject.transform.SetParent(m_groundTwo);
                    block.SetPreservedOriginalParentName(null);
                }
                else
                {
                    block.gameObject.transform.SetParent(null);
                }
            }

            foreach (var block in _levelData.GroundOne)
            {
                if (m_groundOne != null)
                {
                    block.gameObject.transform.SetParent(m_groundOne);
                    block.SetPreservedOriginalParentName(null);
                }
                else
                {
                    block.gameObject.transform.SetParent(null);
                }
            }

            foreach (var block in _levelData.GroundZero)
            {
                if (m_groundZero != null)
                {
                    block.gameObject.transform.SetParent(m_groundZero);
                    block.SetPreservedOriginalParentName(null);
                }
                else
                {
                    block.gameObject.transform.SetParent(null);
                }
            }
            
            StartPositionBlock = GetStartPos(_levelData.GroundTwo, _levelData.GroundOne);
            var start = StartPositionBlock.transform.position;
            StartPosition = new Vector3(start.x, start.y + Constants.SpawningPosOffsetY, start.z);

            if (_levelData.ChallengeInfo == null)
            {
                Debug.LogError("ChallengeInfo missing!");
            }
            else
            {
                SetChallengeSettings(_levelData.ChallengeInfo);
            }
            
            SpawnPositions = _levelData.GroundTwo.AsValueEnumerable().Where(x => x.Data.type == TileType.EnemySpawner).ToArray();

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
            StartPositionNavMeshSurface = StartPositionBlock.GetComponent<NavMeshSurface>();
            if(StartPositionNavMeshSurface == null)
            {
                Debug.LogWarning("StartPositionBlock has no NavMeshSurface component. Adding one");
                StartPositionBlock.gameObject.AddComponent<NavMeshSurface>();
                StartPositionNavMeshSurface = StartPositionBlock.GetComponent<NavMeshSurface>();
            }

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

        public void BuildDesignBlocks()
        {
            BuildIfUserBlueprint(_levelData?.GroundThree);
            BuildIfUserBlueprint(_levelData?.GroundTwo);
            BuildIfUserBlueprint(_levelData?.GroundOne);
            BuildIfUserBlueprint(_levelData?.GroundZero);
            BuildIfUserBlueprint(_levelData?.StaticWalkingPlatform);
            BuildIfUserBlueprint(_levelData?.StaticMainStructures);
            
            void BuildIfUserBlueprint(Block[] blocks)
            {
                if (blocks == null)
                {
                    return;
                }

                foreach (var block in blocks)
                {
                    if (block == null)
                    {
                        continue;
                    }

                    if (block.IsBluePrintBlock)
                    {
                        if (block.gameObject != null && !block.gameObject.activeSelf)
                        {
                            block.gameObject.SetActive(true);
                        }
                        //TODO: check when time: suspicious, might add active navmesh and other flags not needed for LevelBuilder mode.
                        block.SetBuildState(true);
                    }
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
                
                //TODO: continue
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
                _addressablesManager?.ReleaseInstance(block.GetComponent<Block>());

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
                            _addressablesManager?.ReleaseInstance(blockUnderneath.gameObject);
                        }

                        //TODO: lastly
                        /*if (blockUnderneath.gameObject is not null)
                        {
                            //TODO:script not needed, until something interactable is implemented
                            _addressablesManager?.ReleaseInstance(blockUnderneath.GetComponent<Block>());
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
                _addressablesManager?.ReleaseInstance(navMeshObstacle);
            }

            var sphereCollider = blockUnderneath.GetComponent<SphereCollider>();
            if (sphereCollider != null)
            {
                _addressablesManager?.ReleaseInstance(sphereCollider);
            }

            var boxCollider = blockUnderneath.GetComponent<BoxCollider>();
            if (boxCollider != null)
            {
                _addressablesManager?.ReleaseInstance(boxCollider);
            }
        }
        
        private Block GetStartPos(Block[] groundTwo, Block[] groundOne = null)
        {
            if(TryToFindStartPos(groundTwo, out Block startPos))
            {
                return startPos;
            }
            
            if(TryToFindStartPos(groundOne, out Block startPos2))
            {
                return startPos2;
            }
       
            throw new ArgumentNullException("Start position not found in GroundTwo and GroundOne. Please mark a block with " + Constants.StartPositionSymbol + " symbol or add metadata " + MetadataLabel.PlayerStartPosition);
        }

        private bool TryToFindStartPos(Block[] targetGround, out Block startPos)
        {
            var found = false;
            startPos = default;
        
            if (targetGround == null || targetGround.Length == 0)
            {
                return false;
            }

            //If developer forgets to mark metadata in case of one same symbol //remove this at some point
            var startSymbol = Constants.StartPositionSymbol.ToString();
            Block single = null;
            int matchCount = 0;
            string parentName = null;
            foreach (var b in targetGround)
            {
                if (b == null) continue;

                if (parentName == null && b.gameObject != null && b.gameObject.transform != null && b.gameObject.transform.parent != null)
                {
                    parentName = b.gameObject.transform.parent.name;
                }

                if (b.name == startSymbol)
                {
                    matchCount++;
                    if (matchCount == 1) single = b;
                    else break;
                }
            }

            if (matchCount == 1)
            {
                startPos = single;
                found = true;
                return found; //break
            }
            else if (matchCount > 1)
            {
                Debug.Log("More than one block with " + Constants.StartPositionSymbol + " symbol found in " + (parentName ?? "unknown") + ", will try to find by metadata");
            }
            
            //fallback any block that has metadata PlayerStartPosition
            foreach (var b in targetGround)
            {
                if (b == null || b.Data == null || b.Data.metadata == null) continue;
                if (b.Data.metadata.AsValueEnumerable().Contains(MetadataLabel.PlayerStartPosition))
                {
                    startPos = b;
                    found = true;
                    break;
                }
            }

            return found;
        }

        /// <summary>
        /// Supports east or west side. More later if needed
        /// Note! Returns the block below the target, so block in the WalkingPlatform
        /// </summary>
        /// <param name="preferToTryEastFirst"></param>
        /// <returns></returns>
        public IBlock GetBlockNextToStartPosition(bool preferToTryEastFirst)
        {
            //TODO: rewrite after startblock changes, only supported groudTwo before, but now also other Grounds
            if (StartPositionBlock  == null)
            {
                Debug.LogWarning("Start position block not found in GroundTwo.");
                return null;
            }
            //TODO: the code is too forgiving(design/bug), should also check startBlock.NeighbourData.EastUnder.NeighbourData.Above != null
            var extended = StartPositionBlock.NeighbourData as ExtendedNeighbourData;
            return preferToTryEastFirst ? (extended?.EastUnder ?? extended?.WestUnder) : (extended?.WestUnder ?? extended?.EastUnder);
        }

        private Block[] GetTiles(Transform transform)
        {
            if (transform == null)
            {
                return Array.Empty<Block>();
            }

            _tilesBuffer.Clear();
            transform.GetComponentsInChildren(_tilesBuffer);
            if (_tilesBuffer.Count == 0)
            {
                return Array.Empty<Block>();
            }

            return _tilesBuffer.ToArray();
        }

        //TODO: nuke when time....This is used when the Level singleton was preserved across a scene load and must be re-bound to the
        // new scene's ActiveLevel visuals/parents.
        public bool TryRebindSceneReferences()
        {
            var activeLevelGo = GameManager.Instance.LevelMaker.m_activeLevel;

            Transform FindChildRecursive(Transform parent, string name)
            {
                foreach (Transform child in parent)
                {
                    if (child.name == name) return child;
                    var res = FindChildRecursive(child, name);
                    if (res != null) return res;
                }
                return null;
            }

            m_groundThree = FindChildRecursive(activeLevelGo.transform, "UpperlevelStructures") ?? FindChildRecursive(activeLevelGo.transform, "UpperLevelStructures");
            m_groundTwo = FindChildRecursive(activeLevelGo.transform, "MainStructures") ?? FindChildRecursive(activeLevelGo.transform, "MainStructure");
            m_groundOne = FindChildRecursive(activeLevelGo.transform, "WalkingPlatform") ?? FindChildRecursive(activeLevelGo.transform, "WalkingPlatform");
            m_groundZero = FindChildRecursive(activeLevelGo.transform, "Water") ?? FindChildRecursive(activeLevelGo.transform, "WaterPlane");
            m_waterPlane = FindChildRecursive(activeLevelGo.transform, "WaterPlane");
            m_greenPlane = FindChildRecursive(activeLevelGo.transform, "GreenPlane");
 
            var bordersParent = FindChildRecursive(activeLevelGo.transform, "InvisibleBorders(Obsolete)") ?? FindChildRecursive(activeLevelGo.transform, "Borders");
            if (bordersParent != null)
            {
                var list = new List<Transform>();
                for (int i = 0, n = bordersParent.childCount; i < n; i++)
                {
                    list.Add(bordersParent.GetChild(i));
                }
                m_borders = list.ToArray();
            }

            return true;
        }
    }
}

