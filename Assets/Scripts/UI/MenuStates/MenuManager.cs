using BlockAndDagger.Utils;
using UnityEngine;

namespace BlockAndDagger
{
    public sealed class MenuManager : MonoBehaviour
    {
        [SerializeField] public MainMenuUI MainMenuUI;
        [SerializeField] public LevelMakerUI LevelMakerUI;
        [SerializeField] public LevelSelectionUI LevelSelectionUI;
        [SerializeField] public IngameUI IngameUI;
        [SerializeField] public CustomizeCharacterUI CustomizeCharacterUI;
        [SerializeField, ReadOnly] private MenuState m_currentState;
        
        private MainMenuState _mainMenuState;
        private LevelMakerState _levelMakerState;
        private PlayState _playState;
        private LevelSelectionState _levelSelectionState;
        private CustomizeState _customizeState;
        private SimpleStateMachine _simpleStateMachine;
        private Player[] _players;
        private ChooseBlueprintState _chooseBlueprintState;
        
        public MenuControls.MenuControls MenuControls;
        public MenuState CurrentState => _simpleStateMachine.CurrentState != null
            ? _simpleStateMachine.CurrentState.MenuState
            : MenuState.MainMenu;

        private void Awake()
        {
            MenuControls = gameObject.GetComponent<MenuControls.MenuControls>();
            _simpleStateMachine = new SimpleStateMachine();
            SyncInspectorState();
            
            //MainMenu scene
            _mainMenuState = new MainMenuState(MainMenuUI?.gameObject, MenuState.MainMenu);
            _customizeState = new CustomizeState(CustomizeCharacterUI?.gameObject, MenuState.ChooseAbilitiesMenu);   
            
            //LevelSelection
            _levelSelectionState = new LevelSelectionState(LevelSelectionUI?.gameObject, MenuState.LevelSelection);
            
            //LevelMaker
            _levelMakerState = new LevelMakerState(LevelMakerUI?.gameObject, MenuState.LevelMaker);
            _chooseBlueprintState = new ChooseBlueprintState(LevelMakerUI?.gameObject, MenuState.LevelMakerChooseBlueprint);
        }

        private void OnDestroy()
        {
            MenuControls?.MenuInputActions.Disable();
            MenuControls?.MenuInputActions.Dispose();
        }

        public void ActivateMenu(MenuState menuState, Player[] players = null, Level level = null)
        {
            switch (menuState)
            {
                case MenuState.MainMenu:
                    _simpleStateMachine.SetState(_mainMenuState);
                    break;
                
                case MenuState.LevelMakerChooseBlueprint:
                    _simpleStateMachine.SetState(_chooseBlueprintState);
                    break;

                case MenuState.LevelMaker:
                    _simpleStateMachine.SetState(_levelMakerState);
                    break;

                case MenuState.Play:
                    SetPlayMenu(players, level);
                    break;

                case MenuState.LevelSelection:
                    _simpleStateMachine.SetState(_levelSelectionState);
                    break;

                case MenuState.ChooseAbilitiesMenu:
                    _simpleStateMachine.SetState(_customizeState);
                    break;

                default:
                    Debug.LogError($"ActivateMenu: unhandled MenuState {menuState}");
                    break;
            }
            SyncInspectorState();
        }
        
        private void SyncInspectorState()
        {
            if (_simpleStateMachine != null && _simpleStateMachine.CurrentState != null)
            {
                m_currentState = _simpleStateMachine.CurrentState.MenuState;
            }
            else
            {
                m_currentState = MenuState.MainMenu;
            }
        }

        private void SetPlayMenu(Player[] players = null, Level level = null)
        {
            // players and level are required for Play
            if (players == null || level == null)
            {
                Debug.LogWarning("ActivateMenu(MenuState.Play) called without players or level.");
                return;
            }

            _players = players;
            _playState = new PlayState(IngameUI.gameObject, MenuState.Play);
            _simpleStateMachine.SetState(_playState);

            if (level.LevelData.BattleStateChanges == null)
            {
                var defaultBattleStateChanges = new BattleStateChange[3]
                {
                    new BattleStateChange{ duration = 10f, NewState = BattleState.Create },
                    new BattleStateChange{ duration = 5f, NewState = BattleState.Defend },
                    new BattleStateChange{ duration = 5f, NewState = BattleState.Freetime }
                };

                IngameUI.LateChallengeDataLoad(defaultBattleStateChanges);
            }
            else
            {
                IngameUI.LateChallengeDataLoad(level.LevelData.BattleStateChanges);
            }
        }

        public void ActivatePauseMenuControls()
        {
            foreach (var player in _players)
            {
                player.PlayerControls.DisableInGameInputs();  
            }
            MenuControls?.MenuInputActions.Enable();
        }

        /// <summary>
        /// Players can:
        /// - pause/unpause
        /// - pan camera(maybe disabled on the future) -> Possibly smoked screen so players cannot peek, depends on the difficulty we aim
        /// </summary>
        public void ActivateInGameControls()
        {
            foreach (var player in _players)
            {
                player.PlayerControls.EnableInGameInputs();  
            }
            MenuControls?.MenuInputActions?.Disable();
        }

        public void DeactivateAllMenus()
        {
            MainMenuUI?.gameObject.SetActive(false);
            LevelMakerUI?.gameObject.SetActive(false);
            LevelSelectionUI?.gameObject.SetActive(false);
            IngameUI?.gameObject.SetActive(false);
            CustomizeCharacterUI?.gameObject.SetActive(false);
        }
    }
}