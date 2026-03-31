using BlockAndDagger.DebugTools.Mocks;
using BlockAndDagger.Sound;
using UnityEngine;
using VContainer;
using VContainer.Unity;

namespace BlockAndDagger.Setup
{
    public static class LifeTimeConfigureSettings
    {

        public static void SharedConfiguration(IContainerBuilder builder)
        {
            var logger = UnityLogger.Default;
            Debug.unityLogger.logEnabled = Debug.isDebugBuild;
            builder.RegisterInstance<ILogger>(logger);
            builder.RegisterInstance(new ProgressionData() { HighestUnlockedLevelName = LevelName.Level_1 });

            var jsonFilePersistence = new JsonFilePersistence(logger);
            builder.RegisterInstance(jsonFilePersistence);
            builder.RegisterInstance(new DataPersistenceManager(jsonFilePersistence));
            SetAudioManager(builder);

#if UNITY_EDITOR
            Application.targetFrameRate = 60; // quick solution, limits the fan noise and heat with laptops
#endif
        }

        public static void ConfigureMainMenuScene(IContainerBuilder builder)
        {
            builder.RegisterComponentInHierarchy<MenuManager>();
            builder.RegisterComponentInHierarchy<GameManager>(); //Persistent through scenes
        }

        public static void ConfigureLevelSelectionAndGame(IContainerBuilder builder)
        {
            builder.RegisterComponentInHierarchy<PrefabManager>();
            builder.RegisterComponentInHierarchy<MenuManager>();
            builder.RegisterComponentInHierarchy<LevelMakerUI>();
        }

        public static void ConfigureGameScene(IContainerBuilder builder)
        {
            builder.RegisterComponentInHierarchy<PrefabManager>();
            builder.RegisterComponentInHierarchy<MenuManager>();
            builder.RegisterComponentInHierarchy<Game>();
            builder.RegisterComponentInHierarchy<IngameUI>();
        }

        private static void SetAudioManager(IContainerBuilder builder)
        {
            var gm = Object.FindFirstObjectByType<GameManager>();
            if (gm == null)
            {
                builder.RegisterInstance<IMobileAudioManager>(new MockAudioManager());
                return;
            }

            if (gm != null && gm.DebugSettings.noAudio)
            {
               var audio = gm.transform.GetComponentInChildren<MobileAudioManager>();
               if (audio)
               { //no need
                   Object.Destroy(audio.gameObject);
               }
               builder.RegisterInstance<IMobileAudioManager>(new MockAudioManager());
               return;
            }

            var existingAudio = gm.transform.GetComponentInChildren<MobileAudioManager>();
            if (existingAudio != null)
            {
                builder.RegisterInstance<IMobileAudioManager>(existingAudio);
            }
            else
            {
                builder.RegisterInstance<IMobileAudioManager>(new MockAudioManager());
            }
        }
    }
}
