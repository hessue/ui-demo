using System.Linq;
using BlockAndDagger.Sound;
using BlockAndDagger.Utils;
using UnityEngine;
using UnityEngine.SceneManagement;
using VContainer;

namespace BlockAndDagger
{
    public sealed class GameManager : SingletonMono<GameManager>
    {
        [SerializeField] public bool m_useTutorialOnFirstTimePlay;
        [field: SerializeField] public Game Game { get; private set; }
        [field: SerializeField] public LevelMaker LevelMaker { get; private set; }
        [field: SerializeField] public PrefabManager PrefabManager { get; private set; }
        [field: SerializeField] public MenuManager MenuManager { get; private set; }
        [field: SerializeField] public IngameUI IngameUI { get; private set; }
        [field: SerializeField] public IMobileAudioManager AudioManager { get; private set; }
        [Header("*** DEBUG ***")] 
        [field: SerializeField] public DebugSettingsScriptableObject DebugSettings { get; private set; }
        
        private DataPersistenceManager _dataPersistenceManager;
        private FollowCamera _followCamera;
        private Camera _cam;
        private Player[] _players;
        public Player[] Players => _players;
        public MenuInputActions MenuInputActions => MenuManager?.MenuControls?.MenuInputActions;

        public IFocusableBlock[] LevelBuilderPlayers { get; } = new IFocusableBlock[1] { new LevelBuilderPlayer() };

        public ILogger _logger;
        public static bool IsGamePaused;

        //Cache stuff
        public static LevelAndBlueprint CurrentLevelAndBlueprint;
        public ProgressionData ProgressionData { get; private set; }
        public bool openLevelMakerAfterSceneRequest;
        private LevelName? _lastSelectedLevelName;
        private LevelAndBlueprint? _lastSelectedLevelAndBlueprint;

        [Inject]
        public void Construct(
            ILogger log,
            DataPersistenceManager dataPersistenceManager,
            ProgressionData progressionData,
            IMobileAudioManager audioManager)
        {
            _logger = log;
            _dataPersistenceManager = dataPersistenceManager;
            ProgressionData = progressionData;
            AudioManager = audioManager;
        }

        private new void Awake()
        {
            base.Awake();
        }

        private void OnEnable()
        {
            SceneManager.sceneLoaded += OnSceneLoaded;
            //FindManagersOnSceneLoad();
        }

        //Should not have any use. GameManager is a singleton
        private void OnDisable()
        {
            SceneManager.sceneLoaded -= OnSceneLoaded;
        }

        private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            FindManagersOnSceneLoad();
            RefindGOsOnSceneSwitch();
        }

        //Something like is still needed because the level content might have changed during gameplay
        public void DestroyActiveLevelObject()
        {
            var activeLevel = GameObject.Find("ActiveLevel");
            if(activeLevel != null)
            {
                Destroy(activeLevel.gameObject);
                Debug.Log($"{activeLevel.gameObject.name} has been destroyed");
            }
        }

        private void FindManagersOnSceneLoad()
        {
            _cam = Camera.main;
            var managers = GameObject.Find("Managers");

            if (SceneManager.GetActiveScene().name == Constants.MainMenuSceneName)
            {
                MenuManager = managers.GetComponentInChildren<MenuManager>(true);
            }
            else if (SceneManager.GetActiveScene().name == Constants.LevelSelectionSceneName)
            {
                LevelMaker = managers.GetComponentInChildren<LevelMaker>(true);
                PrefabManager = managers.GetComponentInChildren<PrefabManager>(true);
                MenuManager = managers.GetComponentInChildren<MenuManager>(true);
            }
            else if(SceneManager.GetActiveScene().name == Constants.GameSceneName)
            {
                Game = managers.GetComponentInChildren<Game>(true);
                PrefabManager = managers.GetComponentInChildren<PrefabManager>(true);
                MenuManager = managers.GetComponentInChildren<MenuManager>(true);
                //TODO:guessing: leave the LevelLoader and destroy LevelMaker
                LevelMaker = managers.GetComponentInChildren<LevelMaker>(true);
            }
            else
            {
                Debug.LogError($"Scene {SceneManager.GetActiveScene().name} not recognized by GameManager");
            }
            
            MenuManager.DeactivateAllMenus();//TODO: move to MenuManager
        }

        private void RefindGOsOnSceneSwitch()
        {
            if (SceneManager.GetActiveScene().name == Constants.MainMenuSceneName)
            {
                RunMainMenu();
            }
            else if (SceneManager.GetActiveScene().name == Constants.LevelSelectionSceneName)
            {
                LevelMaker.Init(_dataPersistenceManager, MenuManager.LevelMakerUI);
                if (openLevelMakerAfterSceneRequest)
                {
                    OpenLevelBuilder(_lastSelectedLevelName ?? _lastSelectedLevelAndBlueprint.Value.Level);
                }
                else
                {
                    RunLevelSelection();
                }
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

                //Find the persistent level (DontDestroyOnLoad)
                var activeLevel = GameObject.Find("ActiveLevel").gameObject.GetComponent<Level>();
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
            DestroyActiveLevelObject();
           _ = await AddressablesManager.LoadSceneAsync("mainmenu_scene");
        }
        
        public async void SwitchSceneToLevelSelection()
        {
            DestroyActiveLevelObject();
            _ = await AddressablesManager.LoadSceneAsync("levelselection_scene");
        }
        
        public async void SwitchSceneToGame()
        {
            _ = await AddressablesManager.LoadSceneAsync("game_scene");
        }

        private void OpenLevelBuilder(LevelName? levelName = null)
        {
            ResetState();
            MenuManager.ActivateMenu(MenuState.LevelMakerChooseBlueprint);
            var level = GetLevel(levelName);
            RunLevelMakerAndCreateLevel(level);
        }
        
        public LevelAndBlueprint GetLevel(LevelName? levelName)
        {
            var availableLevels =  GameManager.Instance.LevelMaker.LevelLoader.GetAllBlueprints();

            LevelAndBlueprint levelAndBluePrint = default;
            if (levelName is null)
            {
                levelAndBluePrint = availableLevels.First();
            }
            else
            {
                levelAndBluePrint = availableLevels.First(x => x.Level == levelName && string.IsNullOrWhiteSpace(x.BlueprintName));
            }

            return levelAndBluePrint;
        }

        public void RunLevelSelection()
        {
            ResetState();
            MenuManager.ActivateMenu(MenuState.LevelSelection);
            MenuManager.LevelSelectionUI.InitScrollContent(
                _lastSelectedLevelName
                ?? _lastSelectedLevelAndBlueprint?.Level
                ?? ProgressionData.HighestUnlockedLevelName);
        }

        public void CleanLevelAndReload()
        {
            LevelMaker.m_activeLevel.CleanLevel();
            SetFocusedLevel(ProgressionData
                .HighestUnlockedLevelName);
        }

        //TODO: get rid off and use levelAndBlueprint
        public void SetFocusedLevel(LevelName levelName)
        {
            _lastSelectedLevelName = levelName;
        }
        
        public void SetFocusedLevel(LevelAndBlueprint levelAndBlueprint)
        {
            _lastSelectedLevelAndBlueprint = levelAndBlueprint;
        }
        
        public void FocusNextAvailableToLevelSelection()
        {
            LevelMaker.m_activeLevel.CleanLevel();
            SetFocusedLevel(ProgressionData
                .HighestUnlockedLevelName);
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

            if (levelAndBlueprint is null)
            {
                levelAndBlueprint = CurrentLevelAndBlueprint;
            }

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
            var level = LevelMaker.m_activeLevel;
            
            //LevelBlockPointing.enabled = false;
            ResetState(); //TODO: pass parameters or callbacks like m_goPool.UnactivateAllPoolObjects() to replace this

            //TODO: camera follow adjustments
            InitPlayers();
            MenuManager.ActivateMenu(MenuState.Play, _players, level);

            //No need to set reset camera angle, because follow camera takes over the control
            _cam.transform.SetPositionAndRotation(new Vector3(0, 24, -12), Quaternion.Euler(70f, 0f, 0f));
            _followCamera.enabled = true;
            _followCamera.Init(_players.First().transform);

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

        public void ToggleInGamePause()
        {
            IsGamePaused = !IsGamePaused;
            SetPause(IsGamePaused);
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

                Destroy(player.gameObject);
            }

            PrefabManager.ResetInstantiatedPlayerCount();
        }

        public void ProceedToNextLevel()
        {
            //TODO: add found block types to UnlockedBlockTypes
            //TODO: Display LevelSelection and highlight new unlocked level
            SetPause(true);
            ResetState();
            ProgressionData.HighestUnlockedLevelName++;
            MenuManager.IngameUI.ShowSuccessScreen();
        }
    }
}
