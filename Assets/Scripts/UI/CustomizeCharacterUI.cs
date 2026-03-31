using UnityEngine;
using UnityEngine.UI;

namespace BlockAndDagger
{
    public class CustomizeCharacterUI : MonoBehaviour
    {
        [SerializeField] public ToggleItemPanelController m_characterPanel;
        [SerializeField] public ToggleItemPanelController m_abilityPanel;
        [SerializeField] public ToggleItemPanelController m_petPanel;
        [SerializeField] private Button m_returnButton;
        
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
            GameManager.Instance.MenuManager.ActivateMenu(MenuState.MainMenu);
        }
    }
}
