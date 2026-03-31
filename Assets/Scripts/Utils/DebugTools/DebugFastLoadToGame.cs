using System;
using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace BlockAndDagger
{
    /// <summary>
    /// Minimal preload helper: automatically runs MainMenu -> LevelSelection -> LevelMaker -> and RunGame
    /// </summary>
    [Serializable]
    public class DebugFastLoadToGame : MonoBehaviour
    {
        [SerializeField] public LevelName levelNameToLoad;
        [Tooltip("Small delay to allow LevelMaker to initialize before starting the game - RunGame calls.")]
        [SerializeField] private float delayBetweenCalls = 2f;

        private void Start()
        {
            if (SceneManager.GetActiveScene().name == Constants.LevelSelectionSceneName)
            {
                StartCoroutine(SimpleAutoRun());
            }
            else
            {
                SceneManager.sceneLoaded += OnSceneLoaded;
                GameManager.Instance.SetFocusedLevel(levelNameToLoad);
                //TODO: possibility to skip but the scene holds the ActiveLevel gameObject for now 
                GameManager.Instance.SwitchSceneToLevelSelection();
            }
        }

        private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            if (scene.name == Constants.LevelSelectionSceneName)
            {
                SceneManager.sceneLoaded -= OnSceneLoaded;
                StartCoroutine(SimpleAutoRun());
            }
        }

        private IEnumerator SimpleAutoRun()
        {
            // Wait a frame so other Awake/Start run
            yield return null;
            
            GameManager gm = GameManager.Instance ?? FindFirstObjectByType<GameManager>();
            yield return new WaitForSeconds(delayBetweenCalls);
            
            gm.SwitchSceneToGame();
        }
    }
}
