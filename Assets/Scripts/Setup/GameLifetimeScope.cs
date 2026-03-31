
using UnityEngine.SceneManagement;
using VContainer;
using VContainer.Unity;

namespace BlockAndDagger.Setup
{
    public class GameLifetimeScope : LifetimeScope
    {
        /// <summary>
        /// Reference https://vcontainer.hadashikick.jp/
        /// REMEMBER!:
        /// - Use "Construct" naming for injection setup methods
        /// - Remember to register to receive injected dependencies
        /// </summary>
        protected override void Configure(IContainerBuilder builder)
        {
            LifeTimeConfigureSettings.SharedConfiguration(builder);


            if (SceneManager.GetActiveScene().name == Constants.MainMenuSceneName)
            {
                LifeTimeConfigureSettings.ConfigureMainMenuScene(builder);
            }
            else if(SceneManager.GetActiveScene().name == Constants.LevelSelectionSceneName)
            {
                LifeTimeConfigureSettings.ConfigureLevelSelectionAndGame(builder);
            }
            else if(SceneManager.GetActiveScene().name == Constants.GameSceneName)
            {
                LifeTimeConfigureSettings.ConfigureGameScene(builder);
            }
        }
    }
}