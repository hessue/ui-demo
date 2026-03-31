using UnityEngine;
using UnityEngine.SceneManagement;
using BlockAndDagger.Core;

public sealed class Startup : MonoBehaviour
{
    [SerializeField] private string m_sceneLabel;

    private async void Start()
    {
        //SceneManager.LoadScene(m_sceneName);
        await AddressablesManager.LoadSceneAsync(m_sceneLabel);
    }
}