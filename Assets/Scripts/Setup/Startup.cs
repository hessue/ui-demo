using UnityEngine;
using UnityEngine.SceneManagement;

public sealed class Startup : MonoBehaviour
{
    [SerializeField] private string m_sceneLabel;

    private void Start()
    {
        SceneManager.LoadScene(m_sceneLabel);
    }
}