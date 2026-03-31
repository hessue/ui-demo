using System;
using System.Collections;
using System.Linq;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.InputSystem;
using BlockAndDagger.Utils;

namespace BlockAndDagger
{
    public sealed class LevelMaker : MonoBehaviour
    {
        [SerializeField] private LevelLoader m_levelLoader;
        public LevelLoader LevelLoader => m_levelLoader; //TODO: remove

        [SerializeField] public Level m_activeLevel;
        [SerializeField] private float m_cameraPanSpeed;
        [SerializeField] private float m_cameraRotateSpeed;

        private TileDataTracker _tileDataTracker;
        private LevelData[] _levels;
        private int _currentLevelIndex;
        private DataPersistenceManager _dataPersistenceManager;
        private MenuInputActions _menuInputActions;
        private InputAction _moveAction;
        private InputAction _rotate_action;
        private Transform _playerFocusTransform;
        private LevelMakerUI _levelMakerUI;
        private int _currentBlueprintIndex = -1;
        public LevelData LevelData => m_activeLevel.LevelData;
        private LegacyLookups _legacyLookups = new();
        private LevelBlockPointing _levelBlockPointing;
        public LevelBlockPointing LevelBlockPointing => _levelBlockPointing;

        private void Awake()
        {
            _levelBlockPointing = GetComponent<LevelBlockPointing>();
        }

        private void Start()
        {
            if (m_cameraPanSpeed <= 0)
            {
                Debug.Log("Camera pan speed is zero");
            }

            if (m_cameraRotateSpeed <= 0)
            {
                Debug.LogWarning("Camera rotation speed is zero");
            }

            _menuInputActions = GameManager.Instance.MenuInputActions;
        }

        public Task PreloadCompleted => AddressablesManager.PreloadCompleted;

        public void Init(DataPersistenceManager dataPersistenceManager, LevelMakerUI levelMakerUI)
        {
            _dataPersistenceManager = dataPersistenceManager;
            _levelMakerUI = levelMakerUI;
            _tileDataTracker = m_activeLevel.TileDataTracker;
            m_levelLoader.Init(_dataPersistenceManager);
            _currentLevelIndex = 0;
            _levels = new LevelData[3]; //TODO: leveldata loading
            _levels[_currentLevelIndex] = m_activeLevel.LevelData;
        }

        public async Task LoadLevelToLevelMaker(LevelAndBlueprint levelAndBlueprint)
        {
            m_activeLevel.CleanLevel();

            // TODO: key by levelname
            await AddressablesManager.StartPreloadGroupAssets("blocks_folder");
            // StartPreloadGroupAssets loads all blocks used in the level
            // TODO: append with blocks that are available for player

            if (levelAndBlueprint.IsPredefinedBlueprint)
            {
                m_activeLevel.SetLevelByCombiningBaseToPredefinedBlueprint(levelAndBlueprint, m_levelLoader);
                levelAndBlueprint.IsPredefinedBlueprint = true;
            }
            else
            {
                _currentBlueprintIndex = -1;
                m_activeLevel.SetLevel(levelAndBlueprint, m_levelLoader);
            }

            GameManager.CurrentLevelAndBlueprint = levelAndBlueprint;
            m_activeLevel.SetLevelMakerSettings();
        }
        
        /// <summary>
        /// Places NavMesh Links to edges
        /// </summary>
        private void FinalizeDropSpotsForAI()
        {
           // m_activeLevel.LevelData.GroundTwo
        }

        public bool HasLevelAnyPredefinedBlueprintsAvailable()
        {
            string[] availablePredefinedBlueprints = m_levelLoader.GetAllAvailablePredefinedBlueprints(GameManager.CurrentLevelAndBlueprint.Level);
            if (availablePredefinedBlueprints.Any())
            {
                return true;
            }

            return false;
        }

        //TODO: there are more elegant ways
        public void LoadRandomPredefinedBlueprint()
        {
            //string format in array "_predefined_1234"
            string[] availablePredefinedBlueprints = m_levelLoader.GetAllAvailablePredefinedBlueprints(GameManager.CurrentLevelAndBlueprint.Level);
            if (availablePredefinedBlueprints.Length == 0)
            {
                throw new InvalidOperationException("Method should not be called before checking level availablity");
            }
            m_activeLevel.CleanLevel();
            
            _currentBlueprintIndex = GiveNewRandomBlueprint(availablePredefinedBlueprints);
            var levelAndBlueprint = new LevelAndBlueprint(GameManager.CurrentLevelAndBlueprint.Level, availablePredefinedBlueprints[_currentBlueprintIndex], "", true, true);
            m_activeLevel.SetLevelByCombiningBaseToPredefinedBlueprint(levelAndBlueprint, m_levelLoader);
            GameManager.CurrentLevelAndBlueprint = levelAndBlueprint;
        }

        private int GiveNewRandomBlueprint(string[] availablePredefinedlBlueprints)
        {
            if (availablePredefinedlBlueprints.Length == 1)
            {
                return 0;
            }

            int randomBlueprint =  UnityEngine.Random.Range(0, availablePredefinedlBlueprints.Length); 
            if (_currentBlueprintIndex == randomBlueprint)
            {
                GiveNewRandomBlueprint(availablePredefinedlBlueprints);
            }

            return randomBlueprint;
        }
        
        public void SaveAsEditedLevel(int currentBlueprintCount = 0)
        {
            int? blueprint;
            if (m_activeLevel.LevelData.IsPredefinedBlueprint)
            {
                //force to create new file
                 blueprint = currentBlueprintCount + 1;
            }
            else
            {
                //overwrite exiting file
                blueprint = m_activeLevel.LevelData.Blueprint ?? currentBlueprintCount + 1;
            }
            
            Enum.TryParse(m_activeLevel.LevelData.LevelName, out LevelName levelName);
            var originalIsPredefined = m_activeLevel.LevelData.IsPredefinedBlueprint;
            
            m_activeLevel.RefreshLevelDataForSaving();
            
            //TODO: refactor and make safe: Save always as non predefined, but if saving fails, the make sure the IsPredefinedBlueprint
            m_activeLevel.LevelData.SetBlueprint((int)blueprint);
            _dataPersistenceManager.SaveLevel(m_activeLevel.LevelData);

            GameManager.CurrentLevelAndBlueprint = new LevelAndBlueprint(
                levelName,
                m_activeLevel.LevelData.Blueprint.ToString(),
                m_activeLevel.LevelData.Description, true);
        }

        /// <summary>
        /// Player can build on LevelDepth level:
        /// * MainStructures = y2,   (== gameobject called Ground2)
        /// * UpperLevelStructures = y3 (== gameobject called Ground3 )
        /// </summary>
        public void AddBlueprintBlock(IFocusableBlock player, TileType blockType) //, bool showDisabled)
        {
            if (player.FocusedBlock.IsEmptyNew)
            {
                AddToEmptyPosition(player, blockType);
            }
            else
            {
                ReplaceExistingBlock(player, blockType);
            }
        }

        private void AddToEmptyPosition(IFocusableBlock player, TileType blockType)
        {
            var parent = GameManager.Instance.LevelMaker.m_activeLevel.GetGroundFloorTransform(player.FocusedBlock.GetGridFloor());
            // Prefer to use the ghost preview rotation if available so the concrete block matches the preview
            Quaternion rotation = Quaternion.identity;
     
            if (_levelBlockPointing?.PreviewBlock is Component ghostComp && ghostComp.transform != null)
            {
                rotation = ghostComp.transform.rotation;
            }
            else if (_levelBlockPointing != null && _levelBlockPointing.GetCachedGhostRotation().HasValue)
            {
                rotation = _levelBlockPointing.GetCachedGhostRotation().Value;
            }
            else if (player.FocusedBlock is Component focusedComp && focusedComp.transform != null)
            {
                rotation = focusedComp.transform.rotation;
            }

            var newObj = AddressablesManager.FindFromCacheAndInstantiatePrefab(blockType.ToString(), player.FocusedBlock.GetPos(), parent, rotation);
            if (newObj == null)
            {
                Debug.LogError($"Prefab '{blockType}' not found in preload cache. Cannot create block.");
                return;
            }

            FinishNewBlock(newObj);
        }

        private void FinishNewBlock(GameObject newObj)
        {
            var newBlock = newObj.GetComponent<Block>();
            newBlock.FinalizeFreshBlockOnLevelEditor();
            newBlock.Data.isBluePrintBlock = true;
            newBlock.InitBlock(newBlock.Data);
            _tileDataTracker.AddOrReplaceTile(newBlock.transform.position, newBlock);
            _levelBlockPointing.RefocusAfterBlockChange(newBlock);
        }

        private void ReplaceExistingBlock(IFocusableBlock blockInfo, TileType newBlockType)
        {
            var floor = (BlockDepth)blockInfo.FocusedBlock.GetGridFloor();
            if (floor != BlockDepth.MainStructures && floor != BlockDepth.UpperlevelStructures)
            {
                Debug.Log($"One cannot remove block from grid level {floor}");
                return;
            }

            var targetToReplace = blockInfo.FocusedBlock;
            Quaternion replaceRotation = DetermineReplaceRotation(blockInfo);

            // If editing the upper floor while focused on a MainStructures block, operate on the cell above
            if (_levelMakerUI != null && _levelMakerUI.UpperFloor && floor == BlockDepth.MainStructures)
            {
                var handled = HandleReplaceOnUpperFloor(targetToReplace, newBlockType, replaceRotation);
                if (handled)
                {
                    return;
                }
            }

            // Default: replace on the same floor
            ReplaceOnSameFloor(targetToReplace, newBlockType, replaceRotation, floor);
        }

        // Determine which rotation to use for the replacement block (preview ghost -> cached -> target transform)
        private Quaternion DetermineReplaceRotation(IFocusableBlock blockInfo)
        {
            var targetToReplace = blockInfo.FocusedBlock;
            Quaternion replaceRotation = Quaternion.identity;
            if (targetToReplace != null && targetToReplace.transform != null)
            {
                replaceRotation = targetToReplace.transform.rotation;
            }

            if (_levelBlockPointing?.PreviewBlock is Component ghostComp && ghostComp.transform != null)
            {
                replaceRotation = ghostComp.transform.rotation;
            }
            else if (_levelBlockPointing != null && _levelBlockPointing.GetCachedGhostRotation().HasValue)
            {
                replaceRotation = _levelBlockPointing.GetCachedGhostRotation().Value;
            }

            return replaceRotation;
        }

        // Handles replacing or creating a block on the upper floor when user is editing upper-floor mode
        // Returns true if the operation was handled (either replaced or created); false if not handled.
        private bool HandleReplaceOnUpperFloor(IBlock targetToReplace, TileType newBlockType, Quaternion replaceRotation)
        {
            var aboveDepth = BlockDepth.UpperlevelStructures;
            var upperBlocks = m_activeLevel.LevelData.GetGroundBlocks(aboveDepth);

            var higherY = targetToReplace.Data.y + 1f;
            Block foundAbove = _legacyLookups.FindMatchingBlockInArray(upperBlocks, targetToReplace, higherY);

            if (foundAbove != null)
            {
                // Replace existing block above
                var parent = foundAbove.transform.parent;
                GameObject newObj = AddressablesManager.FindFromCacheAndInstantiatePrefab(newBlockType.ToString(),
                    new Vector3(foundAbove.Data.x, foundAbove.Data.y, foundAbove.Data.z), parent, replaceRotation);

                if (newObj == null)
                {
                    Debug.LogError($"Prefab '{newBlockType}' not found in preload cache. Cannot replace block above.");
                    return true;
                }

                var newBlock = newObj.GetComponent<Block>();
                newBlock.FinalizeFreshBlockOnLevelEditor();
                newBlock.Data.isBluePrintBlock = true;

                var upperBlocksRef = m_activeLevel.LevelData.GetGroundBlocks(aboveDepth);
                if (upperBlocksRef != null)
                {
                    for (int i = 0; i < upperBlocksRef.Length; i++)
                    {
                        if (upperBlocksRef[i] == foundAbove)
                        {
                            upperBlocksRef[i] = newBlock;
                            break;
                        }
                    }
                }

                Destroy(foundAbove.transform.gameObject);
                _tileDataTracker.AddOrReplaceTile(newBlock.transform.position, newBlock);
                _levelBlockPointing.RefocusAfterBlockChange(newBlock);
                return true;
            }

            // No block above: create a new one above the focused main-structure block
            var parentForNew = m_activeLevel.GetGroundFloorTransform(aboveDepth);
            GameObject createdObj = AddressablesManager.FindFromCacheAndInstantiatePrefab(newBlockType.ToString(),
                new Vector3(targetToReplace.Data.x, targetToReplace.Data.y + 1f, targetToReplace.Data.z), parentForNew, replaceRotation);

            if (createdObj == null)
            {
                Debug.LogError($"Prefab '{newBlockType}' not found in preload cache. Cannot create block above.");
                return true;
            }

            var createdBlock = createdObj.GetComponent<Block>();
            createdBlock.FinalizeFreshBlockOnLevelEditor();
            createdBlock.Data.isBluePrintBlock = true;
            createdBlock.InitBlock(createdBlock.Data);
            _tileDataTracker.AddOrReplaceTile(createdBlock.transform.position, createdBlock);

            // Do not destroy the original main-structure block when placing above
            _levelBlockPointing.RefocusAfterBlockChange(createdBlock);
            return true;
        }

        private void ReplaceOnSameFloor(IBlock targetToReplace, TileType newBlockType, Quaternion replaceRotation, BlockDepth floor)
        {
            GameObject replaceObj = AddressablesManager.FindFromCacheAndInstantiatePrefab(newBlockType.ToString(),
                new Vector3(targetToReplace.Data.x, targetToReplace.Data.y, targetToReplace.Data.z),
                (targetToReplace is Component compTarget ? compTarget.transform.parent : null), replaceRotation); //TODO: test parent

            if (replaceObj == null)
            {
                Debug.LogError($"Prefab '{newBlockType}' not found in preload cache. Cannot replace block.");
                return;
            }

            var replacedBlock = replaceObj.GetComponent<Block>();
            replacedBlock.FinalizeFreshBlockOnLevelEditor();
            replacedBlock.Data.isBluePrintBlock = true;

            var groundArr = m_activeLevel.LevelData.GetGroundBlocks(floor);
            if (groundArr != null)
            {
                for (int i = 0; i < groundArr.Length; i++)
                {
                    if (groundArr[i] == (targetToReplace as Block))
                    {
                        groundArr[i] = replacedBlock;
                        break;
                    }
                }
            }

            if (targetToReplace is Component oldComp)
            {
                Destroy(oldComp.gameObject);
            }

            _tileDataTracker.AddOrReplaceTile(targetToReplace.GetPos(), replacedBlock);

            _levelBlockPointing.RefocusAfterBlockChange(replacedBlock);
        }

        public void RemoveBlock(IFocusableBlock blockInfo)
        {
            if (blockInfo.FocusedBlock.IsEmptyNew)
            {
                throw new ArgumentException("Cannot remove empty block");
            }

            if (!blockInfo.FocusedBlock.Data.isBluePrintBlock)
            {
                Debug.Log($"Only isBluePrintBlock marked blocks can be modified");
            }

            var floor = blockInfo.FocusedBlock.GetGridFloor();
            if (floor != BlockDepth.MainStructures && floor != BlockDepth.UpperlevelStructures)
            {
                Debug.Log($"One cannot modify blocks on grid floor: {floor}");
                return;
            }

            //TODO: wont work until DataTracker gets fixed
            //_tileDataTracker.RemoveTile(blockInfo.FocusedBlock);
            GameObject.Destroy(blockInfo.FocusedBlock.transform.gameObject);
            
            var emptyBlock = new EmptyBlock(floor, blockInfo.FocusedBlock.transform.position); 
            _levelBlockPointing.RefocusAfterBlockChange(emptyBlock);
        }

        public void RotateCurrentlyFocusedBlock(IFocusableBlock player)
        {
            if (GameManager.Instance == null)
            {
                Debug.LogError("RotateCurrentlyFocusedBlock: GameManager.Instance is null.");
                return;
            }

            var ghostBlockRot = _levelBlockPointing?.PreviewBlock;
            var focused = player?.FocusedBlock;

            Transform focusedTransform = (focused is Component fc) ? fc.transform : null;

            Transform ghostTransform = null;
            if (ghostBlockRot is Block ghostAsBlock)
            {
                ghostTransform = ghostAsBlock.transform;
            }
            else if (ghostBlockRot is Component gc)
            {
                ghostTransform = gc.transform;
            }

            if (ghostBlockRot != null && ghostTransform == null)
            {
                Debug.LogWarning($"RotateCurrentlyFocusedBlock: ghostBlock exists but no transform found. ghostBlock type={ghostBlockRot.GetType()}");
            }

            Debug.Log($"RotateCurrentlyFocusedBlock invoked. _isBlockRotating={_isBlockRotating}, focusedTransform={(focusedTransform!=null)}, ghostTransform={(ghostTransform!=null)}");

            if (focused?.Data != null)
            {
                focused.Data.rotationY += 90f;
            }
            if (ghostBlockRot?.Data != null)
            {
                ghostBlockRot.Data.rotationY += 90f;
            }

            if (focusedTransform != null)
            {
                _playerFocusTransform = focusedTransform;
                if (!_isBlockRotating)
                {
                    Debug.Log("Rotate: starting coroutine to rotate focused transform and sync ghost (if present)");
                    StartCoroutine(RotateSlowly(focusedTransform, 90f, ghostTransform));
                }
                else
                {
                    // Immediate fallback rotation to ensure responsiveness
                    Debug.Log("Rotate: coroutine already running, applying immediate rotation to focused and ghost transforms");
                    focusedTransform.rotation *= Quaternion.Euler(0, 90f, 0);
                    if (ghostTransform != null)
                    {
                        ghostTransform.rotation = focusedTransform.rotation;
                    }
                    if (_levelBlockPointing != null)
                    {
                        _levelBlockPointing.CacheGhostRotation(focusedTransform.rotation);
                    }
                }

                return;
            }

            if (ghostTransform != null)
            {
                if (!_isBlockRotating)
                {
                    StartCoroutine(RotateSlowly(ghostTransform, 90f, null));
                }
                else
                {
                    Debug.Log("Rotate: coroutine already running, applying immediate rotation to ghost transform");
                    ghostTransform.rotation *= Quaternion.Euler(0, 90f, 0);
                    if (_levelBlockPointing != null)
                    {
                        _levelBlockPointing.CacheGhostRotation(ghostTransform.rotation);
                    }
                }

                return;
            }

            if (ghostBlockRot != null)
            {
                GameObject ghostGo = null;
                if (ghostBlockRot is Component ghostComp)
                {
                    ghostGo = ghostComp.gameObject;
                }

                if (ghostGo != null)
                {
                    if (!_isBlockRotating)
                    {
                        StartCoroutine(RotateSlowly(ghostGo.transform, 90f, null));
                    }
                    else
                    {
                        Debug.Log("Rotate: coroutine already running, applying immediate rotation to ghost GameObject (fallback)");
                        var newRot = ghostGo.transform.rotation * Quaternion.Euler(0, 90f, 0);
                        ghostGo.transform.rotation = newRot;
                        if (_levelBlockPointing != null)
                        {
                            _levelBlockPointing.CacheGhostRotation(newRot);
                        }
                    }

                    // Only a single GameObject needs rotation; cache the current rotation
                    if (_levelBlockPointing != null)
                    {
                        _levelBlockPointing.CacheGhostRotation(ghostGo.transform.rotation);
                    }

                    return;
                }
            }

            Debug.Log("RotateCurrentlyFocusedBlock: nothing to rotate (no focused transform or ghost transform).");
        }

        public void RotatePreviewBlockOnly(Block ghost, bool animate = true)
        {
            if (ghost.Data != null)
            {
                ghost.Data.rotationY += 90f;
            }

            var gt = ghost.transform;
            if (gt == null)
            {
                Debug.LogWarning("RotateGhostOnly: ghost has no transform");
                return;
            }

            if (!animate)
            {
                var newRot = gt.rotation * Quaternion.Euler(0, 90f, 0);
                gt.rotation = newRot;
                if (_levelBlockPointing != null)
                {
                    _levelBlockPointing.CacheGhostRotation(newRot);
                }
            }
            else
            {
                if (!_isBlockRotating)
                {
                    StartCoroutine(RotateSlowly(gt, 90f, null));
                }
                else
                {
                    var newRot = gt.rotation * Quaternion.Euler(0, 90f, 0);
                    gt.rotation = newRot;
                    if (_levelBlockPointing != null)
                    {
                        _levelBlockPointing.CacheGhostRotation(newRot);
                    }
                }
            }

            Debug.Log($"RotateGhostOnly: applied rotation to ghost '{ghost.name}' newRot={ghost.transform.rotation.eulerAngles}");
        }

        private void Update()
        {
            if (_moveAction == null) //TODO: fix setup
            {
                return;
            }
            
            PanCamera();
            RotateCamera();
        }
        
        float _blockLerpDuration = 0.2f;
        bool _isBlockRotating;
        IEnumerator RotateSlowly(Transform target, float angle, Transform ghostBlock = null)
        {
            _isBlockRotating = true;
            Debug.Log($"RotateSlowly: starting rotation. target={target?.name}, ghostBlock={(ghostBlock!=null?ghostBlock.name:"null")}, angle={angle}");
            float timeElapsed = 0;
            Quaternion startRotation = target.rotation;
            Quaternion targetRotation = target.rotation * Quaternion.Euler(0, angle, 0);
            while (timeElapsed < _blockLerpDuration)
            {
                target.rotation = Quaternion.Slerp(startRotation, targetRotation, timeElapsed / _blockLerpDuration);
                timeElapsed += Time.deltaTime;
                if (ghostBlock != null)
                {
                    ghostBlock.rotation = target.rotation;
                }

                yield return null;
            }
            
            target.rotation = targetRotation;
            if (ghostBlock != null)
            { ghostBlock.rotation = targetRotation; }
            Debug.Log($"RotateSlowly: finished rotation. target={target?.name}, finalRot={target.rotation.eulerAngles}");

            if (_levelBlockPointing != null)
            {
                _levelBlockPointing.CacheGhostRotation(target.rotation);
            }

            _isBlockRotating = false;
        }

        private void PanCamera() //TODO: correct pan movement
        {
            if (!_moveAction.IsPressed())
            {
                return;
            }

            var inputDir = _moveAction.ReadValue<Vector2>();
            Vector3 movement = new Vector3(inputDir.x, 0f, inputDir.y);
            Camera.main!.transform.position += movement * m_cameraPanSpeed * Time.deltaTime;
        }

        private void RotateCamera()
        {
            if (!_rotate_action.IsPressed())
            {
                return;
            }

            var rotateDir = _rotate_action.ReadValue<float>();
            Camera.main!.transform.eulerAngles += new Vector3(0, rotateDir * m_cameraRotateSpeed * Time.deltaTime, 0);
        }
    }
}
