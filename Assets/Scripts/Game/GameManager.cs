using System.Collections.Generic;
using ZLinq;
using BlockAndDagger.Core;
using BlockAndDagger.Services;
using BlockAndDagger.Setup;
using BlockAndDagger.Sound;
using BlockAndDagger.Utils;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using VContainer;

namespace BlockAndDagger
{
    public sealed class GameManager : SingletonMono<GameManager>, IGameManager
    {
        [SerializeField] public bool m_useTutorialOnFirstTimePlay;
        [field: SerializeField] public Game Game { get; private set; }
        [field: SerializeField] public LevelMaker LevelMaker { get; private set; }
        [field: SerializeField] public IPrefabManager PrefabManager { get; private set; }
        [field: SerializeField] public MenuManager MenuManager { get; private set; }
        [field: SerializeField] public IngameUI IngameUI { get; private set; }
        [field: SerializeField] public IMobileAudioManager AudioManager { get; private set; }

#if UNITY_EDITOR || DEVELOPMENT_BUILD
        [Header("*** DEBUG ***")]
        [field: SerializeField] public DebugSettingsScriptableObject DebugSettings { get; set; } //TODO: must no be private
#endif

        private DataPersistenceManager _dataPersistenceManager;
        private FollowCamera _followCamera;
        private Camera _cam;
        private Player[] _players;
        public Player[] Players => _players;
        public MenuInputActions MenuInputActions => MenuManager?.MenuControls?.MenuInputActions;

        public IFocusableBlock[] LevelBuilderPlayers { get; } = new IFocusableBlock[1] { new LevelBuilderPlayer() };

        public ILogger _logger;
        private IAddressablesManager _addressablesManager;
        public IAddressablesManager AddressablesManager => _addressablesManager;
        public static bool IsGamePaused;

        //Cache stuff
        public ProgressionData ProgressionData { get; private set; }
        public bool openLevelMakerAfterSceneRequest;
        public bool doNotDestroyLevel;
        private LevelName? _levelNameOverrideFromInspector;
        private List<ManifestEntry> _userUnlockedBlockTileTypes;
        private RewardService _rewardService;
        private bool _coreAddressablesLoaded = false;
        private bool _sceneLoadedSubscribed = false;
        private bool _setNextLevelAfterSwitchToLevelSelection;
        private readonly HashSet<string> _pinnedUnlockedGroups = new HashSet<string>(System.StringComparer.OrdinalIgnoreCase);

        //TODO: fix
        //temp values to InitScrollContent to work on first time
        public static LevelAndBlueprint? FirstLevelFallBackInfo = new LevelAndBlueprint(LevelName.Level_1, null);
        private static LevelAndBlueprint? _currentLevelAndBlueprint;
        public static LevelAndBlueprint CurrentLevelAndBlueprint
        {
            get => (LevelAndBlueprint)(_currentLevelAndBlueprint ?? FirstLevelFallBackInfo);
            set => _currentLevelAndBlueprint = value;
        }

        [Inject]
        public void Construct(
            ILogger log,
            DataPersistenceManager dataPersistenceManager,
            ProgressionData progressionData,
            IMobileAudioManager audioManager, RewardService  rewardService,
            IPrefabManager prefabManager, IAddressablesManager addressablesManager)
        {
            _logger = log;
            _dataPersistenceManager = dataPersistenceManager;
            ProgressionData = progressionData;
            AudioManager = audioManager;
            _rewardService = rewardService;
            _addressablesManager = addressablesManager;
            PrefabManager = prefabManager;
        }

        private new void Awake()
        {
            base.Awake();

            if (SceneManager.GetActiveScene().name == Constants.LevelSelectionSceneName)
            {
                // Ensure addressables and related assets are fully loaded before enabling UI
                StartCoroutine(PreloadCoreAddressablesThenEnableUI());
            }
        }

        private System.Collections.IEnumerator Start()
        {
            // If we are in LevelSelection scene, wait until the EnsureAddressables coroutine finishes
            if (SceneManager.GetActiveScene().name == Constants.LevelSelectionSceneName)
            {
                while (!_coreAddressablesLoaded)
                {
                    yield return null;
                }
            }

            _userUnlockedBlockTileTypes = _rewardService.LoadGainedRewards();
            //"User scroll/gained items" Keep this to test gained level blocks
            _userUnlockedBlockTileTypes.Add(new ManifestEntry
            {
                guid = "dontuse",
                group = "blocks_castle",
                tileType = nameof(TileType.Fence)
            });
            SyncUnlockedGroupPins();

            if (!_sceneLoadedSubscribed)
            {
                SceneManager.sceneLoaded += OnSceneLoaded;
                _sceneLoadedSubscribed = true;
            }
        }

        private void OnEnable()
        {
            if (_rewardService.HasNewRewardsAvailable)
            {
                _userUnlockedBlockTileTypes = _rewardService.LoadGainedRewards();
                SyncUnlockedGroupPins();
            }
        }

        //Should not have any use. GameManager is a singleton
        private void OnDisable()
        {
            if (_sceneLoadedSubscribed)
            {
                SceneManager.sceneLoaded -= OnSceneLoaded;
                _sceneLoadedSubscribed = false;
            }
        }

        private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            StartCoroutine(OnSceneLoadedRoutine(scene, mode));
        }
        
        private System.Collections.IEnumerator OnSceneLoadedRoutine(Scene scene, LoadSceneMode mode)
        {
            if (scene.name == Constants.LevelSelectionSceneName)
            {
                _coreAddressablesLoaded = false;
                //TODO: We should drop the yield and replace with loading bar and disable buttons until
                yield return StartCoroutine(PreloadCoreAddressablesThenEnableUI());
            }

            FindManagersOnSceneLoad();
            RefindGOsOnSceneSwitch();
        }

        private System.Collections.IEnumerator PreloadCoreAddressablesThenEnableUI()
        {
            // Guard against double-start
            if (_coreAddressablesLoaded)
            {
                yield break;
            }

            EnsureAddressablesLoader.Start(_addressablesManager, new[] { "level_selection_assets", "cross_scene_assets" });

            float timeout = 2f; //Adjust as needed
            float elapsed = 0f;
            while ((PrefabManager == null || PrefabManager.LevelSelectionAssets == null || PrefabManager.CrossSceneAssets == null) && elapsed < timeout)
            {
                elapsed += Time.deltaTime;
                yield return null;
            }

            _coreAddressablesLoaded = true;
        }

        /// Do not call this when preserving the level (doNotDestroyLevel == true).
        public void ReleaseActiveLevelForDestruction()
        {
            var activeLevel = FindActiveLevel();
            if (activeLevel != null)
            {
                _addressablesManager.ReleaseInstance(activeLevel.gameObject);
            }
            
            _addressablesManager.ReleasePreloadedGroup();
          
        }
        
        private GameObject FindRootObjectByName(string name)
        {
            var rootObjects = SceneManager.GetActiveScene().GetRootGameObjects();
            foreach (var root in rootObjects)
            {
                if (root.name == name)
                {
                    return root;
                }
            }
            return null;
        }

        private void FindManagersOnSceneLoad()
        {
            _cam = Camera.main;
            var managers = FindRootObjectByName("Managers");

            if (SceneManager.GetActiveScene().name == Constants.MainMenuSceneName)
            {
                MenuManager = managers.GetComponentInChildren<MenuManager>(true);
            }
            else if (SceneManager.GetActiveScene().name == Constants.LevelSelectionSceneName)
            {
                if (PrefabManager.LevelSelectionAssets == null)
                {
                    PrefabManager.LevelSelectionAssets = GameObject.Find("LevelSelectionAssets").GetComponent<LevelSelectionAssets>();
                }
                
                if (PrefabManager.CrossSceneAssets == null)
                {
                    PrefabManager.InitCrossSceneAssets();
                }

                LevelMaker = managers.GetComponentInChildren<LevelMaker>(true);
                MenuManager = managers.GetComponentInChildren<MenuManager>(true);

                if (MenuManager != null)
                {
                    MenuManager.gameObject.SetActive(true);
                }

                if (MenuManager.LevelMakerUI != null)
                {
                    MenuManager.LevelMakerUI.gameObject.SetActive(true);
                }

                if (LevelMaker != null)
                {
                    LevelMaker.gameObject.SetActive(true);
                    //TODO: can be cleaner:
                    // If the serialized reference inside LevelMaker was lost (cross-scene preserved
                    // object scenario), try to locate the ActiveLevel in the scene now and bind
                    // it so later flows (LoadLevelToLevelMaker / user edits) parent correctly.
                    if (LevelMaker.m_activeLevel == null)
                    {
                        var activeLevel = FindActiveLevelOrPreserved();
                        var foundLevel = activeLevel.GetComponent<Level>();
                        if (foundLevel != null)
                        {
                            LevelMaker.m_activeLevel = foundLevel;
                        }
                    }
                }

                MenuManager.LevelMakerUI.Initialize(LevelMaker, MenuManager);
            }
            else if(SceneManager.GetActiveScene().name == Constants.GameSceneName)
            {
                if (PrefabManager.GameAssets == null)
                {
                    PrefabManager.GameAssets = managers.GetComponentInChildren<GameAssets>(true);
                }
                
                Game = managers.GetComponentInChildren<Game>(true);
                MenuManager = managers.GetComponentInChildren<MenuManager>(true);
                //TODO:guessing: leave the LevelLoader and destroy LevelMaker
                LevelMaker = managers.GetComponentInChildren<LevelMaker>(true);
            }
            
            MenuManager.DeactivateAllMenus();//TODO: move to MenuManager
        }


        private void HandleActiveLevelInstance()
        {
            var preserved = FindActiveLevelPreserved();
            if (preserved != null)
            {
                var sceneActive = FindActiveLevel();
                if (sceneActive != null && sceneActive != preserved)
                {
                    //Debug.Log("GameManager: Releasing scene-created ActiveLevel in favor of preserved instance (early restore)");
                    _addressablesManager.ReleaseInstance(sceneActive);
                }

                preserved.name = "ActiveLevel";
                SceneManager.MoveGameObjectToScene(preserved, SceneManager.GetActiveScene());

                if (LevelMaker != null)
                {
                    LevelMaker.m_activeLevel = preserved.GetComponent<Level>();
                    LevelMaker.m_activeLevel.TryRebindSceneReferences();
                }
            }
        }

        public void RunLevelSelectionAndLevelBuilder()
        {
            HandleActiveLevelInstance();
            LevelMaker.Init(_dataPersistenceManager, MenuManager.LevelMakerUI, _userUnlockedBlockTileTypes.AsValueEnumerable().Select(x => x.group).ToList());
            if (_setNextLevelAfterSwitchToLevelSelection)
            {
                _setNextLevelAfterSwitchToLevelSelection = false;
                var nextLevel = GetLevel(CurrentLevelAndBlueprint.Level + 1);
                SetFocusedLevel(nextLevel);
            }

            if (openLevelMakerAfterSceneRequest)
            {
                openLevelMakerAfterSceneRequest = false;
                OpenLevelBuilder(_levelNameOverrideFromInspector ?? CurrentLevelAndBlueprint.Level);
            }
            else
            {
                RunLevelSelection();
            }
        }
        
        private void RefindGOsOnSceneSwitch()
        {
            if (SceneManager.GetActiveScene().name == Constants.MainMenuSceneName)
            {
                RunMainMenu();
            }
            else if (SceneManager.GetActiveScene().name == Constants.LevelSelectionSceneName)
            {
                RunLevelSelectionAndLevelBuilder();
            }
            else if (SceneManager.GetActiveScene().name == Constants.GameSceneName)
            {
                _followCamera = FindFirstObjectByType<FollowCamera>();
                _followCamera.enabled = false;
                var canvas = GameObject.Find("Canvas");
                var hud = canvas.transform.Find("InGameHUD");
                IngameUI = hud.GetComponent<IngameUI>();
                if (IngameUI != null)
                {
                    MenuManager.IngameUI = IngameUI;
                }

                // Prefer a previously preserved ActiveLevel (DontDestroyOnLoad)
                GameObject activeLevelGo = null;
                var preservedGo = FindActiveLevelPreserved();
                if (preservedGo != null)
                {
                    preservedGo.name = "ActiveLevel";
                    SceneManager.MoveGameObjectToScene(preservedGo, SceneManager.GetActiveScene());
                    activeLevelGo = preservedGo;
                    Debug.Log("GameManager: Restored ActiveLevelPreserved into Game scene as ActiveLevel");
                    doNotDestroyLevel = false;
                }
                else
                {
                    activeLevelGo = FindActiveLevel();
                }

                Level activeLevel = null;
                if (activeLevelGo != null)
                {
                    activeLevel = activeLevelGo.GetComponent<Level>();
                    if (activeLevel != null)
                    {
                        if (LevelMaker.m_activeLevel == null)
                        {
                            LevelMaker.m_activeLevel = activeLevel;
                        }
                    }
                }

                Assert.IsNotNull(activeLevel, "LevelMaker.m_activeLevel should not be null after scene load.");
                RunGame(activeLevel);
            }
        }

        public void RunMainMenu()
        {
            ResetState();
            MenuManager.ActivateMenu(MenuState.MainMenu);
            AudioManager.PlayMusic("title_song", 0.5f);
        }

        public async void SwitchSceneToMainMenu()
        {
            ReleaseActiveLevelForDestruction();
           _ = await _addressablesManager.LoadSceneAsync("mainmenu_scene");
        }

        public async void SwitchSceneToLevelSelection(bool preserveCurrentActiveLevel = false)
        {
            if (!preserveCurrentActiveLevel)
            {
                ReleaseActiveLevelForDestruction();
            }
            _ = await _addressablesManager.LoadSceneAsync("levelselection_scene");
        }

        public async void SwitchSceneToGame()
        {
            if (LevelMaker != null && LevelMaker.m_activeLevel != null)
            {
                var activeLevelGo = LevelMaker.m_activeLevel.gameObject;
                if (activeLevelGo != null)
                {
                    Object.DontDestroyOnLoad(activeLevelGo);
                    doNotDestroyLevel = true;
                }
            }

            _ = await _addressablesManager.LoadSceneAsync("game_scene");
        }

        public void OpenLevelBuilder(LevelName? levelName = null)
        {
            ResetState();
            MenuManager.ActivateMenu(MenuState.LevelMakerChooseBlueprint);
            var level = GetLevel(levelName);
            //Only set only once
            if(FirstLevelFallBackInfo == null)
            {
                FirstLevelFallBackInfo = level;
            }
            RunLevelMakerAndCreateLevel(level);
        }

        public LevelAndBlueprint GetLevel(LevelName? levelName)
        {
            var availableLevels =  GameManager.Instance.LevelMaker.LevelLoader.GetAllBlueprints();
            
            LevelAndBlueprint levelAndBluePrint = default;
            if (levelName is null)
            {
                levelAndBluePrint = availableLevels.AsValueEnumerable().First();
            }
            else
            {
                levelAndBluePrint = availableLevels.AsValueEnumerable().First(x => x.Level == levelName && string.IsNullOrWhiteSpace(x.BlueprintName));
            }

            return levelAndBluePrint;
        }

        public void RunLevelSelection()
        {
            ResetState();
            MenuManager.ActivateMenu(MenuState.LevelSelection);
            MenuManager.LevelSelectionUI.InitScrollContent(
                CurrentLevelAndBlueprint.Level,
                ProgressionData.HighestUnlockedLevelName);
        }

        public void CleanLevelAndReload()
        {
            LevelMaker.m_activeLevel.CleanLevel();
            SetFocusedLevel(CurrentLevelAndBlueprint);
        }

        public void SetFocusedLevel(LevelAndBlueprint levelAndBlueprint)
        {
            CurrentLevelAndBlueprint = levelAndBlueprint;
        }
        
        public void FocusNextAvailableToLevelSelection()
        {
            //LevelMaker.m_activeLevel.CleanLevel();
            _setNextLevelAfterSwitchToLevelSelection = true;
            SwitchSceneToLevelSelection();
        }

        public void RunCustomizeMenu()
        {
            MenuManager.ActivateMenu(MenuState.ChooseAbilitiesMenu);
        }

        /// <summary>
        /// </summary>
        /// <param name="levelAndBlueprint">uses previously used or saved LevelAndBlueprint if not provided</param>
        public void RunLevelMakerAndCreateLevel(LevelAndBlueprint? levelAndBlueprint)
        {
            ResetState();
            Camera.main.transform.SetPositionAndRotation(new Vector3(0, 24, -12), Quaternion.Euler(70f, 0f, 0f));

            StartCoroutine(
                Utils.AsyncUtilities.WaitForTask(
                    this.LevelMaker.LoadLevelToLevelMaker((LevelAndBlueprint)levelAndBlueprint)));

            MenuManager.ActivateMenu(MenuState.LevelMakerChooseBlueprint);
            //TODO: separate menuUIs and their actions
            MenuManager.MenuControls.MenuInputActions.Enable();
        }

        /// <summary>
        /// </summary>
        /// <param name="activeLevel">uses previously used or saved Level if not provided</param>
        public void RunGame(Level activeLevel = null)
        {
            //TODO: fragile, separate LevelLoader and LevelMaker for Game scene
            if (activeLevel != null)
            {
                LevelMaker.m_activeLevel = activeLevel;
            }
            var level = LevelMaker?.m_activeLevel;
            if (level == null)
            {
                var preservedGo = FindActiveLevelPreserved();
                var sceneGo = FindActiveLevel();
                if (preservedGo != null)
                {
                    level = preservedGo.GetComponent<Level>();
                    if (level != null && LevelMaker != null)
                    {
                        LevelMaker.m_activeLevel = level;
                    }
                    Debug.LogWarning("RunGame: LevelMaker.m_activeLevel was null, restored from ActiveLevelPreserved.");
                }
                else if (sceneGo != null)
                {
                    level = sceneGo.GetComponent<Level>();
                    if (level != null && LevelMaker != null)
                    {
                        LevelMaker.m_activeLevel = level;
                    }
                }
            }

            ResetState(); //TODO: pass parameters or callbacks like m_goPool.UnactivateAllPoolObjects() to replace this

            //TODO: camera follow adjustments
            InitPlayers();
            MenuManager.ActivateMenu(MenuState.Play, _players, level);

            //No need to set reset camera angle, because follow camera takes over the control
            _cam.transform.SetPositionAndRotation(new Vector3(0, 24, -12), Quaternion.Euler(70f, 0f, 0f));
            _followCamera.enabled = true;
            _followCamera.Init(_players[0].transform);

            foreach (var player in _players)
            {
                player.gameObject.SetActive(true);
                player.SetAlive();
                player.SetChallengeSettings(level.LevelData.ChallengeInfo);
                SetPlayerAtStartingPosition(player);
            }

            MenuManager.ActivateInGameControls();
            level.ActivatePlayModeSettings();
            Game.StartGame(level);

            AudioManager.StopMusic();
            AudioManager.Unloader.UnloadClip("title_song");
            AudioManager.PlayMusic("freetime_song", 1f);

            StartCoroutine(CameraUtils.SetCameraFov(_cam, 22f, 2f));
        }
        
        public void ProceedToNextLevel()
        {
            if(_players[0]._hasFallen == HealthStatus.Dead)
            { return;}
            
            _rewardService.CheckRewards(CurrentLevelAndBlueprint.Level);
            if (_rewardService.HasNewRewardsAvailable)
            {
                _userUnlockedBlockTileTypes = _rewardService.LoadGainedRewards();
                SyncUnlockedGroupPins();
            }
            
            //TODO: Display LevelSelection and highlight new unlocked level
            SetPause(true);
            ResetState();
            ProgressionData.HighestUnlockedLevelName++;
            MenuManager.IngameUI.ShowSuccessScreen();
        }

        private void SyncUnlockedGroupPins()
        {
            var desiredGroups = new HashSet<string>(System.StringComparer.OrdinalIgnoreCase);
            if (_userUnlockedBlockTileTypes != null)
            {
                foreach (var entry in _userUnlockedBlockTileTypes)
                {
                    if (entry == null || string.IsNullOrEmpty(entry.group))
                    {
                        continue;
                    }

                    desiredGroups.Add(entry.group);
                }
            }

            var groupsToUnpin = _pinnedUnlockedGroups.AsValueEnumerable().Where(x => !desiredGroups.Contains(x)).ToList();
            foreach (var group in groupsToUnpin)
            {
                _addressablesManager.UnpinKey(group);
                _pinnedUnlockedGroups.Remove(group);
            }

            foreach (var group in desiredGroups)
            {
                if (!_addressablesManager.IsPinned(group))
                {
                    _addressablesManager.PinKey(group);
                }

                _pinnedUnlockedGroups.Add(group);
            }
        }
        
        public void ToggleInGamePause()
        {
            IsGamePaused = !IsGamePaused;
            SetPause(IsGamePaused);
        }
        
        private void InitPlayers()
        {
            _players = new Player[1];
            var player = PrefabManager.CreateNewPlayer();
            _players[0] = player;
        }

        private void SetPlayerAtStartingPosition(Player player)
        {
            player.gameObject.GetComponent<Collider>().enabled = false;
            if (LevelMaker != null)
            {
                player.gameObject.transform.localPosition = LevelMaker.m_activeLevel.StartPosition;
            }
           
            player.gameObject.GetComponent<Collider>().enabled = true;
        }

        private void SetPause(bool pauseGame)
        {
            IsGamePaused = pauseGame;
            if (pauseGame)
            {
                Game.PauseGame(true);
                _logger.Log("Game in pause");
                Time.timeScale = 0f;
                MenuManager.ActivatePauseMenuControls();
            }
            else
            {
                _logger.Log("Game unpaused");
                Time.timeScale = 1;
                Game.PauseGame(false);
                MenuManager.ActivateInGameControls();
            }
        }

        //TODO: reimplement reset state
        /// <summary>
        /// Fast and dirty way but overkill, can cause lots of unwanted side effects!!
        /// </summary>
        private void ResetState()
        {
            if (IsGamePaused)
            {
                ToggleInGamePause();
            }

            if (Game != null)
            {
                Game.StopGame();
            }

            if (_followCamera != null)
            {
                _followCamera.enabled = false;
            }

            if (_players is null)
                return;

            foreach (var player in _players)
            {
                if (player == null || player.gameObject == null)
                {
                    continue;
                }

                _addressablesManager.ReleaseInstance(player.gameObject);
            }

            PrefabManager.ResetInstantiatedPlayerCount();
        }
        
        private GameObject FindActiveLevelPreserved()
        {
            return GameObject.Find("ActiveLevelPreserved");
        }

        private GameObject FindActiveLevel()
        {
            return GameObject.Find("ActiveLevel");
        }

        private GameObject FindActiveLevelOrPreserved()
        {
            var preserved = FindActiveLevelPreserved();
            if (preserved != null)
            {
                return preserved;
            }
            return FindActiveLevel();
        }
    }
}
