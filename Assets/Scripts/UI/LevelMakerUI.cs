using System;
using System.Collections;
using System.Linq;
using BlockAndDagger.Sound;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;
using VContainer;

namespace BlockAndDagger
{
    public sealed class LevelMakerUI : MonoBehaviour
    {
        [SerializeField] private Button m_backButton;
        [SerializeField] private Button m_removeButton;
        [SerializeField] private Button m_rotateButton;
        [SerializeField] private Button m_buildButton;
        [SerializeField] private Button m_saveButton;
        [SerializeField] private Button m_redPlayButton;
        [SerializeField] private Button m_greenPlayButton;
        [SerializeField] private Button m_shopButton;
        [SerializeField] private Button m_createCustomButton;
        [SerializeField] private Button m_generateButton;
        [SerializeField] private Image m_newChanges;
        [SerializeField] private LevelMaker m_levelMaker; //questionable reference, currently set from Inspector
        [SerializeField] public MultiscrollController m_blockScrollController;
        [SerializeField] private Image m_tutorialSmokeImage;
        [SerializeField] private Material m_greyscaleMaterial;
        [SerializeField] private Button m_switchFloorButton;
        [SerializeField] private RectTransform m_swipeArea;

        private LevelBlockPointing _levelBlockPointing;

        private Image _removeButtonImage;
        private Image _rotateButtonImage;
        private Image _buildButtonImage;
        
        private static bool _userSeenTutorial;
        //private InputAction _leftMouseButtonAction;
        private ILogger _logger;
        private IMobileAudioManager m_audio_manager;

        public RectTransform SwipeArea => m_swipeArea;
        /// <summary>
        /// Blueprint not generated or new changes has not been saved
        /// </summary>
        private bool _dirty;

        public bool HasNewChanges => _dirty;
        private MenuInputActions _menuInputActions;
        private const int Player1Index = 0;
        private bool _upperFloor = false;
        public bool UpperFloor => _upperFloor;
        private Image _upperFloorImage;
        private Image _lowerFloorImage;
        private Coroutine _blinkCoroutine;
        private UnityAction _buildButtonClicked;
        private UnityAction _removeButtonClicked;
        private UnityAction _switchFloorClicked;
        public static UnityAction PointerMissed;

        private void Awake()
        {
            _levelBlockPointing = m_levelMaker.LevelBlockPointing;
            var images = m_switchFloorButton.GetComponentsInChildren<Image>();
            _upperFloorImage = images[1];
            _lowerFloorImage = images[2];
            _upperFloor = true;
            ToggleFloor();
            _allowPlayCooldown = true;
        }

        [Inject]
        public void Construct(ILogger logger, IMobileAudioManager audioManager)
        {
            _logger = logger;
            m_audio_manager = audioManager;
        }

        void Start()
        {
            m_generateButton.gameObject.SetActive(true);
            m_backButton.gameObject.SetActive(true);
        }
        
        private void OnEnable()
        {
        
            m_newChanges.gameObject.SetActive(false);
            m_generateButton.onClick.AddListener(OnGenerateClicked);
            m_backButton.onClick.AddListener(OnBackToLevelSelectionScreen);

            if (GameManager.Instance.MenuManager.CurrentState == MenuState.LevelMakerChooseBlueprint)
            {
                EnableEditorButtons(false);
                EnableAskButtons(true);
                ShowChooseBlueprint();
            }
            else
            {
                EnableAskButtons(false);
                EnableEditorButtons(true);
                ShowEditor();
            }
        }
        
        void ShowChooseBlueprint()
        {
            m_createCustomButton.onClick.AddListener(OnCreateCustomButtonClicked);
            if (!_userSeenTutorial && GameManager.Instance.m_useTutorialOnFirstTimePlay)
            {
                _userSeenTutorial = true;
                m_tutorialSmokeImage.transform.gameObject.SetActive(true);
            }
        }

        private void EnableEditorButtons(bool enable)
        {
            m_redPlayButton.gameObject.SetActive(enable);
            m_saveButton.gameObject.SetActive(enable);
            m_greenPlayButton.gameObject.SetActive(enable);
            m_buildButton.gameObject.SetActive(enable);
            m_removeButton.gameObject.SetActive(enable);
            m_blockScrollController.gameObject.SetActive(enable);
            m_rotateButton.gameObject.SetActive(enable);
            m_switchFloorButton.gameObject.SetActive(enable);
            m_createCustomButton.gameObject.SetActive(!enable);
        }

        private void EnableAskButtons(bool enable)
        {
            m_createCustomButton.gameObject.SetActive(enable);
            m_tutorialSmokeImage.transform.gameObject.SetActive(enable);
        }

        void ShowEditor()
        {
            m_newChanges.gameObject.SetActive(false);
            EnableEditorButtons(true);
            SwitchToAmbienceMusic();

            LevelBlockPointing.BlinkUpperFloorImageBackground += OnBlinkUpperFloorImageBackground;
            LevelBlockPointing.RefreshButtonOptions += OnRefreshButtonOptions;
            LevelBlockPointing.AllowOptions += InspectionBlockHasTile;

            if (m_levelMaker.m_activeLevel.LevelData.Blueprint.HasValue)
            {
                m_generateButton.gameObject.SetActive(false);
                SetIsDirty(false);
            }
            else
            {
                if (!_userSeenTutorial && GameManager.Instance.m_useTutorialOnFirstTimePlay)
                {
                    _userSeenTutorial = true;
                    m_tutorialSmokeImage.transform.gameObject.SetActive(true);
                }
            }
            
            _menuInputActions = GameManager.Instance.MenuInputActions;
            //_leftMouseButtonAction = _menuInputActions.Menu.SelectTarget;

            m_saveButton.onClick.AddListener(OnSaveButtonClicked);
            m_greenPlayButton.onClick.AddListener(OnPlayButtonClicked);
            m_redPlayButton.onClick.AddListener(OnRedPlayButtonButtonClicked);

            _removeButtonImage = m_removeButton.transform.GetChild(1).GetComponent<Image>();
            _rotateButtonImage = m_rotateButton.transform.GetChild(1).GetComponent<Image>();
            _buildButtonImage = m_buildButton.transform.GetChild(1).GetComponent<Image>();

            _buildButtonClicked = () =>
            {
                OnBuildButtonClicked();
                RefreshAvailableOptions();
            };
            m_buildButton.onClick.AddListener(_buildButtonClicked);

            _removeButtonClicked = () =>
            {
                OnRemoveButtonClicked();
                RefreshAvailableOptions();
            };
            m_removeButton.onClick.AddListener(_removeButtonClicked);


            m_rotateButton.onClick.AddListener(OnRotateButtonClicked);

            _switchFloorClicked = () =>
            {
                ToggleFloor();
                RefreshAvailableOptions();
            };
            m_switchFloorButton.onClick.AddListener(_switchFloorClicked);

            PointerMissed += OnMissClicked;
        }

        private void ToggleFloor()
        {
            _upperFloor = !_upperFloor;
            _upperFloorImage.gameObject.SetActive(_upperFloor);
            _lowerFloorImage.gameObject.SetActive(!_upperFloor);
            _levelBlockPointing.TriggerCollisionCheck();
        }

        private void OnDisable()
        {
            LevelBlockPointing.BlinkUpperFloorImageBackground -= OnBlinkUpperFloorImageBackground;
            LevelBlockPointing.RefreshButtonOptions -= OnRefreshButtonOptions;
            LevelBlockPointing.AllowOptions -= InspectionBlockHasTile;

            if (m_saveButton != null) m_saveButton.onClick.RemoveListener(OnSaveButtonClicked);
            if (m_greenPlayButton != null) m_greenPlayButton.onClick.RemoveListener(OnPlayButtonClicked);
            if (m_redPlayButton != null) m_redPlayButton.onClick.RemoveListener(OnRedPlayButtonButtonClicked);
            if (m_backButton != null) m_backButton.onClick.RemoveListener(OnBackToLevelSelectionScreen);

            if (m_buildButton != null)
            {
                if (_buildButtonClicked != null)
                    m_buildButton.onClick.RemoveListener(_buildButtonClicked);
                else
                    m_buildButton.onClick.RemoveAllListeners();
            }

            if (m_removeButton != null)
            {
                if (_removeButtonClicked != null)
                    m_removeButton.onClick.RemoveListener(_removeButtonClicked);
                else
                    m_removeButton.onClick.RemoveAllListeners();
            }

            if (m_createCustomButton != null) m_createCustomButton.onClick.RemoveListener(OnCreateCustomButtonClicked);
            if (m_generateButton != null) m_generateButton.onClick.RemoveListener(OnGenerateClicked);
            if (m_rotateButton != null) m_rotateButton.onClick.RemoveListener(OnRotateButtonClicked);

            if (m_switchFloorButton != null)
            {
                if (_switchFloorClicked != null)
                    m_switchFloorButton.onClick.RemoveListener(_switchFloorClicked);
                else
                    m_switchFloorButton.onClick.RemoveAllListeners();
            }

            // Clear stored delegate references
            _buildButtonClicked = null;
            _removeButtonClicked = null;
            _switchFloorClicked = null;
            PointerMissed -= OnMissClicked;

            if (_levelBlockPointing)
            {
                _levelBlockPointing.enabled = false;
            }
        }

        private void OnMissClicked()
        {
            SetInspectionButtonsEnabled(false);
        }

        private void OnRefreshButtonOptions(IBlock selectedBlock)
        {
            if (selectedBlock == null || selectedBlock.IsEmptyNew)
            {
                SetInspectionButtonsEnabled(false);
            }
            else
            {
                SetInspectionButtonsEnabled(true);
            }
        }

        private void InspectionBlockHasTile(IBlock selectedBlock = null)
        {
            // If selection is null or represents an empty/new block, disable inspection options
            if (selectedBlock == null || selectedBlock.IsEmptyNew)
            {
                SetInspectionButtonsEnabled(false);
            }
            else
            {
                SetInspectionButtonsEnabled(true);
            }
        }

        private void SetInspectionButtonsEnabled(bool isEnabled)
        {
            m_removeButton.interactable = isEnabled;
            m_rotateButton.interactable = isEnabled;
            m_buildButton.interactable = isEnabled;

            if (isEnabled)
            {
                RemoveShader(_removeButtonImage);
                RemoveShader(_rotateButtonImage);
                RemoveShader(_buildButtonImage);
            }
            else
            {
                ApplyShader(_removeButtonImage);
                ApplyShader(_rotateButtonImage);
                ApplyShader(_buildButtonImage);
            }
        }

        private void ApplyShader(Image image)
        {
            image.material = m_greyscaleMaterial;
        }

        private void RemoveShader(Image image)
        {
            image.material = null;
        }

        //Called everytime a blueprint is modified
        private void SetIsDirty(bool isDirty)
        {
            _dirty = isDirty;
            m_greenPlayButton.gameObject.SetActive(!isDirty);
            m_redPlayButton.gameObject.SetActive(isDirty);
            m_newChanges.gameObject.SetActive(isDirty);
        }

        public void OnSaveButtonClicked()
        {
            if (m_levelMaker.m_activeLevel.LevelData.Blueprint.HasValue &&
                !m_levelMaker.m_activeLevel.LevelData.IsPredefinedBlueprint)
            {
                m_levelMaker.SaveAsEditedLevel(); //TODO: overrides existing
            }
            else //creates new file/"edited"
            {
                var figures = GameManager.Instance.LevelMaker.LevelLoader
                    .GetLevelAndBlueprintFigures(); //TODO: bring reference from elsewhere

                var currentBlueprintCount = !figures.Any() ? 0
                    : figures.First(x =>
                            x.Item1 == Enum.Parse<LevelName>(m_levelMaker.m_activeLevel.LevelData.LevelName))
                        .blueprintCount;
                m_levelMaker.SaveAsEditedLevel(currentBlueprintCount);
            }

            SetIsDirty(false);
        }

        private void OnBuildButtonClicked()
        {
            if (m_blockScrollController.SelectedBlock == TileType.ERROR_NOT_DEFINED)
            {
                _logger.Log("Nothing selected from the block scroll bar(TODO better UI highlighting)");
                return;
            }

            var player = GameManager.Instance.LevelBuilderPlayers[Player1Index];
            if (player.FocusedBlock == null)
            {
                _logger.Log("No focus on anything");
                return;
            }

            m_levelMaker.AddBlueprintBlock(player, m_blockScrollController.SelectedBlock);
            SetIsDirty(true); //TODO: check blueprint delta (track changes)
        }

        private void OnRemoveButtonClicked()
        {
            var player = GameManager.Instance.LevelBuilderPlayers[Player1Index];
            if (player.FocusedBlock == null || player.FocusedBlock.IsEmptyNew)
            {
                _logger.Log("No focus on anything");
                return;
            }

            m_levelMaker.RemoveBlock(player);
            SetIsDirty(true); //TODO: check blueprint delta (track changes)
        }

        private bool _allowPlayCooldown;
        public void OnPlayButtonClicked()
        {
            if (_allowPlayCooldown)
            {  _allowPlayCooldown = false;
                Invoke( "StartPressCooldown", 3f );
                GameManager.Instance.SwitchSceneToGame();
            }
        }

        private void StartPressCooldown()
        {
            _allowPlayCooldown = true;
        }

        /// <summary>
        /// LongHoldButton script calls the real deal: OnPlayButtonClicked
        /// </summary>
        private void OnRedPlayButtonButtonClicked()
        {
            _logger.Log("Hold to play without saving");
        }

        private void SwitchToAmbienceMusic()
        {
            m_audio_manager.Unloader.UnloadClip("title_song"); //should StopMusic();
            m_audio_manager.PlayMusic("ambience_song", 1f);
        }
        
        private void OnCreateCustomButtonClicked()
        {
            GameManager.Instance.MenuManager.ActivateMenu(MenuState.LevelMaker);
            _levelBlockPointing.enabled = true; //Script has disabled itself
            var gm = GameManager.Instance;
            _levelBlockPointing.Init(gm.LevelBuilderPlayers, m_blockScrollController, gm.LevelMaker, gm.MenuManager);
            RefreshAvailableOptions();
        }

        private void OnGenerateClicked()
        {
            m_tutorialSmokeImage.transform.gameObject.SetActive(false);
            m_levelMaker.LoadRandomPredefinedBlueprint();
            m_redPlayButton.gameObject.SetActive(false);
            m_greenPlayButton.gameObject.SetActive(true);
            EnableEditorButtons(true);
            SwitchToAmbienceMusic();
        }

        private void OnBackToLevelSelectionScreen()
        {
            GameManager.Instance.CleanLevelAndReload();//Not elegant but powerful
            GameManager.Instance.RunLevelSelection();
        }

        public void EnableGenerateButtonIfAnyBlueprints()
        {
            if (m_levelMaker.HasLevelAnyPredefinedBlueprintsAvailable())
            {
                m_generateButton.GetComponent<Button>().interactable = true;
                return;
            }

            _logger.Log("No predefined levels available. Disabling Generate button");
            m_generateButton.GetComponent<Button>().interactable = false;
        }

        private void OnRotateButtonClicked()
        {
            var player = GameManager.Instance.LevelBuilderPlayers[Player1Index];

            var previewBlock = _levelBlockPointing.PreviewBlock;
            Debug.Log($"OnRotateButtonClicked: playerFocusedBlock={(player?.FocusedBlock==null?"null":"present")}, focusedIsEmptyNew={(player?.FocusedBlock==null?"n/a":player.FocusedBlock.IsEmptyNew.ToString())}, previewBlockPresent={(previewBlock!=null)}");

            // If a concrete block is focused, rotate that block and sync the preview (preferred behavior)
            if (player.FocusedBlock != null && !player.FocusedBlock.IsEmptyNew)
            {
                m_levelMaker.RotateCurrentlyFocusedBlock(player);
                return;
            }

            // Otherwise, rotate the preview block if present
            if (previewBlock != null)
            {
                if (_levelBlockPointing != null)
                {
                    _levelBlockPointing.RotatePreviewBlock(90f);
                }
                else
                {
                    var levelMakerToUse = GameManager.Instance?.LevelMaker ?? m_levelMaker;
                    if (levelMakerToUse != null)
                    {
                        levelMakerToUse.RotatePreviewBlockOnly(previewBlock, animate: false);
                    }
                }

                return;
            }

            if (player.FocusedBlock is null || player.FocusedBlock.IsEmptyNew)
            {
                return;
            }

            var floor = player.FocusedBlock.GetGridFloor();
            if (floor != BlockDepth.MainStructures && floor != BlockDepth.UpperlevelStructures)
            {
                _logger.Log($"One cannot modify blocks on grid floor: {floor}");
                return;
            }

            m_levelMaker.RotateCurrentlyFocusedBlock(player);
        }

        public void RefreshAvailableOptions()
        {
            var player = GameManager.Instance.LevelBuilderPlayers[Player1Index];
            var previewBlock = _levelBlockPointing?.PreviewBlock;
            bool previewExists = previewBlock != null;

            // Disable remove when there's no concrete block at the focused/preview block position
            if (player.FocusedBlock == null || player.FocusedBlock.IsEmptyNew || (previewExists && previewBlock.IsEmptyNew))
            {
                m_removeButton.interactable = false;
            }
            else
            {
                m_removeButton.interactable = true;
            }

            // Additional rule: when UpperFloor is active and a preview block exists on the upper level,
            // ensure there is an actual block on that same level at the preview block grid position before
            // enabling Remove. If there is no real block on that upper level cell, Remove must be disabled.
            if (GameManager.Instance?.MenuManager?.LevelMakerUI?.UpperFloor == true && previewExists && previewBlock != null && previewBlock.Data != null)
            {
                var activeLevel = GameManager.Instance.LevelMaker?.m_activeLevel;
                if (activeLevel != null && activeLevel.TileDataTracker != null)
                {
                    var tracker = activeLevel.TileDataTracker;
                    var found = false;
                    foreach (var kv in tracker.TileDataList)
                    {
                        var pos = kv.Key;
                        if (Math.Abs(pos.x - previewBlock.Data.x) < TileDataTracker.VectorComparisonTolerance &&
                            Math.Abs(pos.y - previewBlock.Data.y) < TileDataTracker.VectorComparisonTolerance &&
                            Math.Abs(pos.z - previewBlock.Data.z) < TileDataTracker.VectorComparisonTolerance)
                        {
                            var trackedBlock = kv.Value.Block;
                            // Consider a tracked block 'concrete' if it's present and not an empty/new placeholder
                            if (trackedBlock != null && !trackedBlock.IsEmptyNew)
                            {
                                found = true;
                                break;
                            }
                        }
                    }

                    if (!found)
                    {
                        m_removeButton.interactable = false;
                    }
                }
            }

            // Rotate is allowed if there's either a concrete focused block or a block preview to rotate
            if (player.FocusedBlock == null || player.FocusedBlock.IsEmptyNew)
            {
                m_rotateButton.interactable = previewExists;
            }
            else
            {
                m_rotateButton.interactable = true;
            }

            if (player.FocusedBlock == null)
            {
                m_buildButton.interactable = false;
            }
            else
            {
                m_buildButton.interactable = true;
            }

            // Keep button visuals (greyscale shader) in sync with interactable state
            if (m_removeButton.interactable)
            {
                RemoveShader(_removeButtonImage);
            }
            else
            {
                ApplyShader(_removeButtonImage);
            }

            if (m_rotateButton.interactable)
            {
                RemoveShader(_rotateButtonImage);
            }
            else
            {
                ApplyShader(_rotateButtonImage);
            }

            if (m_buildButton.interactable)
            {
                RemoveShader(_buildButtonImage);
            }
            else
            {
                ApplyShader(_buildButtonImage);
            }
        }

        private Color _originalColor;
        private void OnBlinkUpperFloorImageBackground()
        {
            if (_upperFloorImage == null)
            {
                return;
            }
            
            if (_blinkCoroutine != null)
            {
                StopCoroutine(_blinkCoroutine);
                m_switchFloorButton.GetComponent<Image>().color = _originalColor;
            }

            _originalColor = m_switchFloorButton.GetComponent<Image>().color;
            _blinkCoroutine = StartCoroutine(BlinkImage(1.3f, m_switchFloorButton.GetComponent<Image>(),  Color.darkRed));
        }

        private IEnumerator BlinkImage(float duration, Image image, Color32? blinkColor)
        {
            Color blink = blinkColor ?? Color.purple;
            float elapsed = 0f;
            while (elapsed < duration)
            {
                float t = (Mathf.Sin(elapsed * Mathf.PI * 4f) + 1f) * 0.5f;
                image.color = Color.Lerp(_originalColor,blink, t);
                elapsed += Time.deltaTime;
                yield return null;
            }

            image.color = _originalColor;
            _blinkCoroutine = null;
        }
    }
}
