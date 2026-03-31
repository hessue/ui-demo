using System;
using System.Linq;
using UnityEngine;
using System.Collections;
using BlockAndDagger.Core;
using BlockAndDagger.UI;
using BlockAndDagger.Utils;

namespace BlockAndDagger
{
    public sealed class LevelBlockPointing : MonoBehaviour
    {
        private const float RaycastMaxDistance = 100f;
        
        [field:SerializeField] public Block PreviewBlock { get; private set; }
        private float _previewBlockLerpDuration = 0.2f;
        private bool _isPreviewRotating;
        private Camera _cam;
        private bool _initialized;
        /// <summary>
        /// start from half way of the lowest block (Platform ground)
        /// </summary>
        private const float ProximityCheckGroundLevelY = 2.5f;
        private const float DecreaseBoundingBoxXandZ = 0.75f;
        /// <summary>
        /// Box size should include max 9 blocks on each ground level(3 ground level, lowest ground 'Water' not included).
        /// Start from half way of the lowest block and max extents should hit middle of furthest block(upper or side)
        /// </summary>
        private static readonly Vector3 NeighbourBlueprintProximityCheckSize =
            new(3 * DecreaseBoundingBoxXandZ, 2, 3 * DecreaseBoundingBoxXandZ);

        [SerializeField] private IBlock _playerOnefocusedBlockGizmoCache;
        private Block _currentlyInspectionBlock;
        private MenuInputActions _menuInputActions;
        private IFocusableBlock[] _players;

        private MultiscrollController _multiscrollController;
        private Quaternion? _cachedPreviewRotation;
        private int _mouseRaycastLayerMask;
        private IPointerInput _pointerInput;
        private Vector3 _debugPos;
        private bool _showDebugGizmo;
        private bool _colliderTestDone;
        private bool _allowColliderTestCheck;
        private LevelMaker _levelMaker;
        private MenuManager _menuManager;
       
        public LayerMask m_LayerMask;
        public static event Action<IBlock> RefreshButtonOptions;
        public static event Action<IBlock> AllowOptions;
        public static event Action BlinkUpperFloorImageBackground;

        void Awake()
        {
            _mouseRaycastLayerMask = ~0; // hit all layers by default
            var ignoreLayerIndex = LayerMask.NameToLayer("Ignore");
            if (ignoreLayerIndex >= 0)
            {
                _mouseRaycastLayerMask &= ~(1 << ignoreLayerIndex);
            }
            var uiLayerIndex = LayerMask.NameToLayer("UI");
            if (uiLayerIndex >= 0)
            {
                _mouseRaycastLayerMask &= ~(1 << uiLayerIndex);
            }
        }

        public void Init(IFocusableBlock[] focusables, MultiscrollController multiscrollController, LevelMaker levelMaker, MenuManager menuManager)
        {
            if (_initialized)
            {
                Debug.Log("LevelBlockPointing: Already initialized - skipping duplicate Init.");
                return;
            }

            _menuInputActions = GameManager.Instance?.MenuInputActions;

            _players = focusables;
            _multiscrollController = multiscrollController;
            SnapScrollItem.OnTileChanged += OnScrollItemChanged;
            _initialized = true;
            _cam = Camera.main ?? FindFirstObjectByType<Camera>();
            _multiscrollController.OnInitialized += PlacePreviewBlock;
            _levelMaker = levelMaker;
            _menuManager = menuManager;

#if UNITY_IOS || UNITY_ANDROID || UNITY_EDITOR
            {
                _pointerInput = new TouchPointerInputInternal();
            }
            #else
            {
                _pointerInput = new MousePointerInputInternal();
            }
#endif

            _pointerInput.Init(_menuInputActions, _cam, _mouseRaycastLayerMask);
        }

        private void PlacePreviewBlock()
        {
            _multiscrollController.SelectDefaultBlockType(TileType.Wall);
            var emptyBlockNextToStartBlock = _levelMaker.m_activeLevel.GetBlockNextToStarPosition(true);
            var (highlightedBlock, emptyBlock) = LevelMakerBlockDepthHandling(emptyBlockNextToStartBlock);
            if (highlightedBlock is null && emptyBlock != null)
            {
                SetPlayerFocusedBlock(emptyBlock);
            }
        }

        private void OnScrollItemChanged()
        {
            if (!TryGetFirstPlayer(out var player))
            {
                return;
            }
            if (player.FocusedBlock == null)
            {
                return;
            }

            Quaternion? preservedRotation = null;
            var selected = _multiscrollController?.SelectedBlock ?? TileType.ERROR_NOT_DEFINED;
            if (selected == TileType.ERROR_NOT_DEFINED)
            {
                return;
            }

            if (PreviewBlock != null)
            {
                preservedRotation = PreviewBlock.transform != null ? PreviewBlock.transform.rotation : (Quaternion?)null;
                GameObject.Destroy(PreviewBlock.gameObject);
                PreviewBlock = null;
            }

            ShowPreviewBlock(player, player.FocusedBlock, selected, preservedRotation);
        }

        private void OnDisable()
        {
            _pointerInput?.Dispose();

            SnapScrollItem.OnTileChanged -= OnScrollItemChanged;
            _initialized = false;

            if (PreviewBlock != null)
            {
                GameObject.Destroy(PreviewBlock.gameObject);
                PreviewBlock = null;
            }

            if (_multiscrollController != null)
            {
                _multiscrollController.OnInitialized -= PlacePreviewBlock;
            }
        }

        private void OnDestroy()
        {
            SnapScrollItem.OnTileChanged -= OnScrollItemChanged;
        }

        void Update()
        {
            if (_menuManager.CurrentState == MenuState.LevelMaker && _cam != null)
            {
                if (_pointerInput != null && _pointerInput.TryGetHit(out Block targetBlock, false))
                {
                    if (!targetBlock.CanBeBuildOn())
                    {
                        return;
                    }

                    ResetHighlightCheck(targetBlock);
                }
            }
        }

        private void ResetHighlightCheck(Block targetBlock)
        {
            _currentlyInspectionBlock = targetBlock;
            _colliderTestDone = false;
            _allowColliderTestCheck = true;
        }

        void FixedUpdate()
        {
            HandleHighlightSelection();
        }

        private void ShowPreviewBlock(IFocusableBlock player, IBlock block, TileType? selectedTileOverride = null,
            Quaternion? initialRotation = null)
        {
            var blockType = ChooseBlockType(player, block, selectedTileOverride);

            Quaternion? rotation = DeterminePreviewRotation(player, block, initialRotation);

            GameObject newObj = null;

            var upperModeActive = GameManager.Instance != null && _menuManager.LevelMakerUI.UpperFloor;

            if (upperModeActive && block.GetGridFloor() == BlockDepth.MainStructures)
            {
                newObj = InstantiatePreviewForUpperFloor(player, block, selectedTileOverride, rotation);
                if (newObj == null)
                {
                    return;
                }
            }
            else
            {
                newObj = InstantiatePreviewForSameFloor(block, blockType, rotation);
                if (newObj == null)
                {
                    return;
                }
            }

            FinalizeCreatedPreview(newObj, block, blockType);
        }

        private TileType ChooseBlockType(IFocusableBlock player, IBlock block, TileType? selectedTileOverride)
        {
            if (selectedTileOverride.HasValue && selectedTileOverride.Value != TileType.ERROR_NOT_DEFINED)
            {
                return selectedTileOverride.Value;
            }

            if (block != null && block?.TileType != TileType.ERROR_NOT_DEFINED)
            {
                return block.TileType;
            }

            if (_multiscrollController != null && _multiscrollController.SelectedBlock != TileType.ERROR_NOT_DEFINED)
            {
                return _multiscrollController.SelectedBlock;
            }

            return TileType.Wall;
        }

        private Quaternion? DeterminePreviewRotation(IFocusableBlock player, IBlock block, Quaternion? initialRotation)
        {
            Quaternion? rotation = null;

            if (player?.FocusedBlock is Component focusedComponent)
            {
                if (player.FocusedBlock.Data != null && player.FocusedBlock.Data.type != TileType.ERROR_NOT_DEFINED)
                {
                    rotation = focusedComponent.transform.rotation;
                }
                else
                {
                    if (initialRotation.HasValue)
                    {
                        rotation = initialRotation.Value;
                    }
                    else if (_cachedPreviewRotation.HasValue)
                    {
                        rotation = _cachedPreviewRotation.Value;
                    }
                    else
                    {
                        rotation = focusedComponent.transform.rotation;
                    }
                }
            }
            else
            {
                if (initialRotation.HasValue)
                {
                    rotation = initialRotation.Value;
                }
                else if (_cachedPreviewRotation.HasValue)
                {
                    rotation = _cachedPreviewRotation.Value;
                }
            }

            return rotation;
        }

        private GameObject InstantiatePreviewForUpperFloor(IFocusableBlock player, IBlock block, TileType? selectedTileOverride, Quaternion? rotation)
        {
            if (block.GetGridFloor() != BlockDepth.MainStructures)
            {
                RefreshButtonOptions?.Invoke(_playerOnefocusedBlockGizmoCache);
                Debug.Log("No block under, selection ignored");
                BlinkUpperFloorImageBackground?.Invoke();
                return null;
            }

            bool hasSelectedTile = (selectedTileOverride.HasValue && selectedTileOverride.Value != TileType.ERROR_NOT_DEFINED)
                                   || (_multiscrollController != null && _multiscrollController.SelectedBlock != TileType.ERROR_NOT_DEFINED);

            if (block.TileType != TileType.ERROR_NOT_DEFINED || hasSelectedTile)
            {
                var parent = _levelMaker.m_activeLevel.GetGroundFloorTransform(
                    block.GetGridFloor() + 1);
                var focusedPos = block.GetPos();
                var position = new Vector3(focusedPos.x, focusedPos.y + 1f, focusedPos.z);
                var prefabName = (selectedTileOverride ?? _multiscrollController?.SelectedBlock)?.ToString() ??
                                 ChooseBlockType(player, block, selectedTileOverride).ToString();
                var newObj = AddressablesManager.FindFromCacheAndInstantiatePrefab(prefabName, position, parent, rotation);
                AllowOptions?.Invoke(_playerOnefocusedBlockGizmoCache);
                return newObj;
            }

            RefreshButtonOptions?.Invoke(_playerOnefocusedBlockGizmoCache);
            Debug.Log("No block under, selection ignored");
            BlinkUpperFloorImageBackground?.Invoke();
            return null;
        }

        private GameObject InstantiatePreviewForSameFloor(IBlock block, TileType blockType, Quaternion? rotation)
        {
            var floor = block.GetGridFloor();
            if(floor == BlockDepth.Water)
            {
                floor = BlockDepth.MainStructures;
            }
            
            var parent = _levelMaker.m_activeLevel.GetGroundFloorTransform(floor);
            var position = block.GetPos();
            AllowOptions?.Invoke(_playerOnefocusedBlockGizmoCache);
            var newObj = AddressablesManager.FindFromCacheAndInstantiatePrefab(blockType.ToString(), position, parent, rotation);
            return newObj;
        }

        private void FinalizeCreatedPreview(GameObject newObj, IBlock block, TileType blockType)
        {
            if (newObj == null)
            {
                Debug.LogWarning($"ShowPreviewBlock: failed to instantiate prefab for blockType={blockType} prefabName likely missing or newObj null");
                return;
            }

            var blockComp = newObj.GetComponent<Block>();
            if (blockComp == null)
            {
                GameObject.Destroy(newObj);
                return;
            }

            blockComp.IsEmptyNew = block.IsEmptyNew;

            var mr = newObj.GetComponent<MeshRenderer>();
            if (mr != null)
            {
                mr.material = GameManager.Instance.PrefabManager.HighlighMaterial;
            }
            var blink = newObj.AddComponent<BlinkMaterialAlpha>();
            blink.Init(blockType == TileType.ERROR_NOT_DEFINED);
            if (blockComp.Data == null)
            {
                blockComp.PopulateJsonData();
            }

            PreviewBlock = blockComp;
            _cachedPreviewRotation = newObj.transform.rotation;

            _allowColliderTestCheck = false;
            _colliderTestDone = false;
            _pointerInput?.ResetPressState();

            RefreshButtonOptions?.Invoke(PreviewBlock);
            _menuManager?.LevelMakerUI?.RefreshAvailableOptions();
        }

        private void HandleHighlightSelection()
        {
            if (_allowColliderTestCheck && !_colliderTestDone)
            {
                _allowColliderTestCheck = false;
                if (PreviewBlock != null)
                {
                    Destroy(PreviewBlock.gameObject);
                    PreviewBlock = null;
                }

                if (_currentlyInspectionBlock != null && HasAnyBlueprintNeighbours(_currentlyInspectionBlock))
                {
                    var (highlightedBlock, emptyBlock) = LevelMakerBlockDepthHandling(_currentlyInspectionBlock);
                    if (highlightedBlock is null && emptyBlock != null)
                    {
                        SetPlayerFocusedBlock(emptyBlock);
                    }
                    else
                    {
                        SetPlayerFocusedBlock(highlightedBlock);
                    }
                }
                else
                {
                    if (!TryGetFirstPlayer(out var player))
                    {
                    }
                    else
                    {
                        player.FocusedBlock = null;
                    }

                    _playerOnefocusedBlockGizmoCache = null;
                    LevelMakerUI.PointerMissed?.Invoke();
                }

                _colliderTestDone = true;
            }
        }

        private void SetPlayerFocusedBlock(IBlock block)
        {
            if (!TryGetFirstPlayer(out var p))
            {
                Debug.LogWarning("LevelBlockPointing: No player available to set focused block.");
                return;
            }

            p.FocusedBlock = block;
            _playerOnefocusedBlockGizmoCache = block;

            ShowPreviewBlock(p, block, null, _cachedPreviewRotation);
        }

        public void RefocusAfterBlockChange(IBlock focusedBlock)
        {
            if (!TryGetFirstPlayer(out var p))
            {
                return;
            }
            p.FocusedBlock = focusedBlock;
            _playerOnefocusedBlockGizmoCache = focusedBlock;
            Quaternion? preserved = null;
            if (PreviewBlock != null)
            {
                if (PreviewBlock.transform != null)
                {
                    preserved = PreviewBlock.transform.rotation;
                }
                else
                {
                    preserved = null;
                }

                GameObject.Destroy(PreviewBlock.gameObject);
                PreviewBlock = null;
            }

            ShowPreviewBlock(p, focusedBlock, null, preserved ?? _cachedPreviewRotation);
        }

        public void RotatePreviewBlock(float angleDegrees = 90f)
        {
            if (PreviewBlock == null)
            {
                Debug.LogWarning("RotatePreviewBlock: no ghost to rotate");
                return;
            }

            if (PreviewBlock.Data != null)
            {
                PreviewBlock.Data.rotationY += angleDegrees;
            }

            if (_levelMaker != null)
            {
                _levelMaker.RotatePreviewBlockOnly(PreviewBlock, animate: true);
                return;
            }

            var gt = PreviewBlock.transform;
            if (gt == null)
            {
                Debug.LogWarning("RotatePreviewBlock: ghost has no transform");
                return;
            }

            if (!_isPreviewRotating)
            {
                StartCoroutine(RotatePreviewBlockSlowly(gt, angleDegrees));
            }
            else
            {
                var newRot = gt.rotation * Quaternion.Euler(0, angleDegrees, 0);
                gt.rotation = newRot;
            }

            _cachedPreviewRotation = gt.rotation;
        }

        private IEnumerator RotatePreviewBlockSlowly(Transform target, float angle)
        {
            _isPreviewRotating = true;
            float timeElapsed = 0f;
            Quaternion startRotation = target.rotation;
            Quaternion targetRotation = target.rotation * Quaternion.Euler(0, angle, 0);

            while (timeElapsed < _previewBlockLerpDuration)
            {
                var t = timeElapsed / _previewBlockLerpDuration;
                var rot = Quaternion.Slerp(startRotation, targetRotation, t);
                target.rotation = rot;

                timeElapsed += Time.deltaTime;
                yield return null;
            }

            target.rotation = targetRotation;
            _cachedPreviewRotation = target.rotation;
            _isPreviewRotating = false;
        }

        public void CachePreviewBlockRotation(Quaternion rotation)
        {
            _cachedPreviewRotation = rotation;
        }

        public Quaternion? GetCachedPreviewBlockRotation()
        {
            return _cachedPreviewRotation;
        }

        public void TriggerCollisionCheck()
        {
            _allowColliderTestCheck = true;
            _colliderTestDone = false;
        }

        private (IBlock Block, IBlock EmptyBlock) LevelMakerBlockDepthHandling(IBlock targetBlock)
        {
            EmptyBlock emptyBlock = null;
            IBlock target = null;
            if (targetBlock.Data.isBluePrintBlock ||
                (targetBlock.TileType ==
                 TileType.Flowers)) //TODO: separate starting point == Flowers from other flowers
            {
                target = targetBlock;
            }
            else //case empty block
            {
                var floor = targetBlock.GetGridFloor();
                if (floor < BlockDepth.UpperlevelStructures)
                {
                    if (targetBlock?.NeighbourData?.Above == null)
                    {
                        emptyBlock = new EmptyBlock(floor + 1, new Vector3(targetBlock.transform.position.x,
                            targetBlock.transform.position.y + 1f,
                            targetBlock.transform.position.z));
                    }
                }
                else
                {
                    Debug.Log("There are no blocks above, deselecting highlighted block");
                }
            }

            return (target, emptyBlock);
        }

        public void OnDrawGizmos()
        {
            if (_showDebugGizmo)
            {
                Gizmos.color = Color.yellow;
                Gizmos.DrawWireCube(_debugPos, NeighbourBlueprintProximityCheckSize);
            }

            if (_playerOnefocusedBlockGizmoCache != null)
            {
                float mainLevelGroupHeight = 2f;
                float halfBlockHeight = 0.5f;

                Vector3 boxPos = default;

                var actualPos = _playerOnefocusedBlockGizmoCache.GetPos();
                boxPos = new Vector3(actualPos.x, mainLevelGroupHeight + halfBlockHeight, actualPos.z);

                Gizmos.color = Color.red;
                Gizmos.DrawWireCube(boxPos, new Vector3(1, 1, 1));
            }
        }

        private bool HasAnyBlueprintNeighbours(Block target)
        {
            var focusedBlockPos = new Vector3(target.transform.position.x, ProximityCheckGroundLevelY,
                target.transform.position.z);

            StartCoroutine(DisplayGizmoForSeconds(3f, focusedBlockPos));

            int layerMaskToUse = m_LayerMask.value != 0 ? m_LayerMask.value : _mouseRaycastLayerMask;

            Collider[] hitColliders = Physics.OverlapBox(focusedBlockPos, NeighbourBlueprintProximityCheckSize / 2,
                Quaternion.identity, layerMaskToUse);

            for (int i = 0; i < hitColliders.Length; i++)
            {
                var block = hitColliders[i].GetComponent<Block>();
                if (block != null)
                {
                    var isBlueprint = block.IsBluePrintBlock;
                    if (!isBlueprint && block.Data != null)
                    {
                        isBlueprint = block.Data.isBluePrintBlock;
                    }

                    if (isBlueprint || block.TileType == Constants.StartPositionSymbol)
                    {
                        return true;
                    }
                }
            }

            return false;
        }

        private IEnumerator DisplayGizmoForSeconds(float seconds, Vector3 atPos)
        {
            _debugPos = atPos;
            _showDebugGizmo = true;
            yield return new WaitForSeconds(seconds);
            _showDebugGizmo = false;
        }

        private bool TryGetFirstPlayer(out IFocusableBlock player)
        {
            player = null;
            if (_players == null || _players.Length == 0)
            {
                return false;
            }
            player = _players.FirstOrDefault();
            return player != null;
        }
    }
}



