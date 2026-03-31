using UnityEngine;
using UnityEngine.UI;
using VContainer;

namespace BlockAndDagger
{
    public class CustomizeCharacterUI : MonoBehaviour
    {
        [SerializeField] public ToggleItemPanelController m_characterPanel;
        [SerializeField] public ToggleItemPanelController m_abilityPanel;
        [SerializeField] public ToggleItemPanelController m_petPanel;
        [SerializeField] private Button m_returnButton;
        private MenuManager _menuManager;

        [Inject]
        public void Construct(IGameManager gameManager)
        {
            _menuManager = gameManager.MenuManager;
        }
        
        void OnEnable()
        {
            m_returnButton.onClick.AddListener(OnToMainMenu);
        }
        
        private void OnDisable()
        {
            m_returnButton.onClick.RemoveListener(OnToMainMenu);
        }

        private void OnToMainMenu()
        {
            GameManager.Instance.RunMainMenu();
        }
    }
}
