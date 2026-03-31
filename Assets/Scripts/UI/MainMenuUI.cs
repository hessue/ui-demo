using System.Threading.Tasks;
using BlockAndDagger.Utils;
using UnityEngine;
using UnityEngine.UI;

namespace BlockAndDagger
{
    public sealed class MainMenuUI : MonoBehaviour
    {
        [SerializeField] private Button m_customizeButton;
        [SerializeField] private Button m_playButton;
        [SerializeField] private Button m_exitButton;
        
        private void OnEnable()
        {
            m_customizeButton.onClick.AddListener(() => GameManager.Instance.RunCustomizeMenu());
            m_playButton.onClick.AddListener(OnPlayButtonClicked);
            m_exitButton.onClick.AddListener(MoveAndroidApplicationToBack);
        }

        private void OnDisable()
        {
            m_customizeButton.onClick.RemoveListener(() => GameManager.Instance.RunCustomizeMenu());
            m_playButton.onClick.RemoveListener(OnPlayButtonClicked);
            m_exitButton.onClick.RemoveListener(MoveAndroidApplicationToBack);
        }

    private void OnPlayButtonClicked()
        {
            _ = LoadSceneAsync();
        }

        private static async Task LoadSceneAsync()
        {
            await AddressablesManager.LoadSceneAsync("levelselection_scene");
        }
        
        private void MoveAndroidApplicationToBack()
        {

#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#elif UNITY_ANDROID
                  AndroidJavaObject activity =
                            new AndroidJavaClass("com.unity3d.player.UnityPlayer").GetStatic<AndroidJavaObject>(
                                "currentActivity");
                        activity.Call<bool>("moveTaskToBack", true);
#else
        Application.Quit();
#endif
        }
    }
}
