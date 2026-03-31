using System.Collections;
using BlockAndDagger;
using BlockAndDagger.Sound;
using BlockAndDagger.UI;
using UnityEngine;
using UnityEngine.InputSystem.OnScreen;
using UnityEngine.UI;
using VContainer;

public sealed class IngameUI : MonoBehaviour
{
    [SerializeField] private ProgressBar m_progressBar;
    [SerializeField] private Button m_menuButton;
    [SerializeField] private Button m_pauseButton;
    [SerializeField] private Button m_ability1Button;
    [SerializeField] private Button m_ability2Button;
    [SerializeField] private OnScreenStick m_moveStick;
    [SerializeField] private int m_expValue;
    [SerializeField] private Transform m_successScreen;
    [SerializeField] private Transform m_deadScreen;
    [SerializeField] private Transform m_heartPanel;
    //DeadScreen buttons
    [SerializeField] private Button m_tryAgainButton;
    [SerializeField] private Button m_backToDesignerButton;
    [SerializeField] private Button m_returnToMainMenuButton;

    private IMobileAudioManager _audioManager;
    private BattleStateChange[] _battleStateChanges; //TODO remove
    private ImageEffectUtility _imageEffectUtility;
    private Image _pauseIcon;
    private Image _menuIcon;
    private bool _menuOpen;
    private bool _pauseOn;
    
    [Inject]
    public void Construct(IMobileAudioManager audioManager)
    {
        _audioManager = audioManager;
    }
    
    private void Awake()
    {
        _imageEffectUtility = new ImageEffectUtility(this);
        m_deadScreen.gameObject.SetActive(false);
        m_successScreen.gameObject.SetActive(false);
    }

    private void Start()
    {
        EnableMobileButtons(false);
        m_pauseButton.onClick.AddListener(TogglePauseButtonClicked);
        m_menuButton.onClick.AddListener(ToggleMenuButtonClicked);
        m_tryAgainButton.onClick.AddListener(() => OnTryAgainButtonClicked());
        m_backToDesignerButton.onClick.AddListener(() => OnBackToDesignerButtonClicked());
        m_returnToMainMenuButton.onClick.AddListener(() => OnReturnToMainMenuButtonClicked());

        _pauseIcon = m_pauseButton.GetComponent<Image>();
        _menuIcon= m_menuButton.GetComponent<Image>();

        _pauseIcon.color = Color.black;
        _menuIcon.color = Color.black;
        StartCoroutine(WaitAndFadeNotRelevantUIcons(_pauseIcon));
        StartCoroutine(WaitAndFadeNotRelevantUIcons(_menuIcon));
    }

    private IEnumerator WaitAndFadeNotRelevantUIcons(Image image)
    {
        yield return new WaitForSeconds(1f);
        const float blinkDuration = 3f;
        _imageEffectUtility.BlinkImageColor(image, Color.softYellow, blinkDuration);

        yield return new WaitForSeconds(blinkDuration);

        const float fadeDuration = 2f;
        const float targetAlpha = 0f;

        _imageEffectUtility.FadeImageAlpha(image, targetAlpha, fadeDuration);

        yield return new WaitForSeconds(fadeDuration);
    }
    
    public void LateChallengeDataLoad(BattleStateChange[] battleStateChanges)
    {
        this._battleStateChanges = battleStateChanges;
        m_progressBar.LoadData(_battleStateChanges);
        m_progressBar.MoveToNextBattleState();
    }

    private void OnEnable()
    {
        RefreshUIToInitialState();
    }

    private void OnDisable()
    {
        _imageEffectUtility.StopAllEffects();
        m_successScreen.gameObject.SetActive(false);
        m_deadScreen.gameObject.SetActive(false);
    }

    private void EnableMobileButtons(bool enable = false)
    {
        if (!GameManager.Instance.DebugSettings.allowTwoAbilityButtons) //TODO: obsolete
        {
            m_ability1Button.gameObject.SetActive(enable);
            m_ability2Button.gameObject.SetActive(enable);
            m_moveStick.gameObject.SetActive(enable);
        }
    }
    
    private void RefreshUIToInitialState()
    {
        m_pauseButton.gameObject.SetActive(true);
        EnableMobileButtons(true);
   
        m_deadScreen.gameObject.SetActive(false);
        m_tryAgainButton.gameObject.SetActive(false);
        m_backToDesignerButton.gameObject.SetActive(false);
        m_returnToMainMenuButton.gameObject.SetActive(false);
        
        //TODO: remember to call
        //m_progressBar.LoadData(m_battleStateChanges);
        //m_progressBar.MoveToNextBattleState();
    }

    public void ShowDeadScreen()
    {
        m_progressBar.gameObject.SetActive(false);
        m_pauseButton.gameObject.SetActive(false);
        EnableMobileButtons(false);
        m_deadScreen.gameObject.SetActive(true);
        m_tryAgainButton.gameObject.SetActive(true);
        m_backToDesignerButton.gameObject.SetActive(true);
        m_returnToMainMenuButton.gameObject.SetActive(true);
        _audioManager.StopMusic();
        _audioManager.PlayMusic("death", 0.4f);
    }

    public void ShowSuccessScreen()
    {
        m_successScreen.gameObject.SetActive(true);
    }

    public void FocusNextAvailableToLevelSelection()
    {
        GameManager.Instance.AudioManager.Unloader.UnloadAll();
        //GameManager.Instance.m_game?.StopGame(); //TODO: stop gracefully
        GameManager.Instance.FocusNextAvailableToLevelSelection();
    }

    private void OnTryAgainButtonClicked()
    {
        GameManager.Instance.RunGame();
    }
    
    private void OnBackToDesignerButtonClicked()
    {
        //TODO: track unsaved user blueprints and apply changes on the load after scene change
        GameManager.Instance.SetFocusedLevel(GameManager.CurrentLevelAndBlueprint.Level);
        GameManager.Instance.openLevelMakerAfterSceneRequest = true;
        GameManager.Instance.SwitchSceneToLevelSelection();
    }
    
    private void OnReturnToMainMenuButtonClicked()
    {
        GameManager.Instance.SwitchSceneToMainMenu();
    }

    private void TogglePauseButtonClicked()
    {
        GameManager.Instance.ToggleInGamePause();
        if(GameManager.IsGamePaused)
        {
            _imageEffectUtility.SetImageAlphaToDefault(_pauseIcon, 1f);
        }
        else
        {
            StartCoroutine(_imageEffectUtility.FadeImageCoroutine(_pauseIcon, 0f, 1f));
        }
    }

    public void ToggleMenuButtonClicked()
    {
        _menuOpen = !_menuOpen;
        if (_menuOpen)
        {
            _imageEffectUtility.SetImageAlphaToDefault(_menuIcon, 1f);
        }
        else
        {
            StartCoroutine(_imageEffectUtility.FadeImageCoroutine(_menuIcon, 0f, 1f));
        }
        GameManager.Instance.ToggleInGamePause();
    }
}
