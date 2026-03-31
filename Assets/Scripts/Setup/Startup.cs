using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using BlockAndDagger.Utils;
using UnityEngine;
using UnityEngine.SceneManagement;

public sealed class Startup : MonoBehaviour
{
    [SerializeField] private string m_sceneLabel;

    private async void Start()
    {
        //SceneManager.LoadScene(m_sceneName);
        await AddressablesManager.LoadSceneAsync(m_sceneLabel);
    }
}