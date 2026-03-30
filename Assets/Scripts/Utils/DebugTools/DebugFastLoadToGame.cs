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
        private float delayBetweenCalls = 2f;
        
        
        private void Start()
        {
            SceneManager.sceneLoaded += OnSceneLoaded;
            GameManager.Instance.SetFocusedLevel(new LevelAndBlueprint(levelNameToLoad, null)); //TODO
            //TODO: possibility to skip but the scene holds the ActiveLevel gameObject for now 
            GameManager.Instance.SwitchSceneToLevelSelection();
        }

        private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            GameManager gm = GameManager.Instance ?? FindFirstObjectByType<GameManager>();
             if(scene.name == Constants.LevelSelectionSceneName && gm?.MenuManager?.CurrentState == MenuState.LevelMaker)
            {
                StartCoroutine(RunGame());
            }
            else 
            {
                //SceneManager.sceneLoaded -= OnSceneLoaded;    
                StartCoroutine(RunLevelBuilder());
            }
            
        }

        private IEnumerator RunLevelBuilder()
        {
            // Wait a frame so other Awake/Start run
            yield return null;
            
            GameManager gm = GameManager.Instance ?? FindFirstObjectByType<GameManager>();
            gm.OpenLevelBuilder(levelNameToLoad);
        }
        
        private IEnumerator RunGame()
        {
            // Wait a frame so other Awake/Start run
            yield return null;
            
            GameManager gm = GameManager.Instance ?? FindFirstObjectByType<GameManager>();
            //gm.SetFocusedLevel(new LevelAndBlueprint(levelNameToLoad, null));
            //GameManager.Instance.RunLevelSelectionAndLevelBuilder();
            
            gm.OpenLevelBuilder(levelNameToLoad);
            
            yield return new WaitForSeconds(delayBetweenCalls);

            //gm.SwitchSceneToGame();
        }
    }
}
