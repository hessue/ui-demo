using BlockAndDagger.DebugTools.Mocks;
using BlockAndDagger.Services;
using BlockAndDagger.Sound;
using BlockAndDagger.UI;
using UnityEngine;
using VContainer;
using VContainer.Unity;
using BlockAndDagger.Core;
namespace BlockAndDagger.Setup
{
    public static class LifeTimeConfigureSettings
    {
        private static IAddressablesManager _addressablesManager;
        public static IAddressablesManager AddressablesManager => _addressablesManager;

        public static void SharedConfiguration(IContainerBuilder builder)
        {
            var logger = UnityLogger.Default;
            Debug.unityLogger.logEnabled = Debug.isDebugBuild;
            builder.RegisterInstance<ILogger>(logger);
            builder.RegisterInstance(new ProgressionData() { HighestUnlockedLevelName = LevelName.Level_4 });
            var gm = Object.FindFirstObjectByType<GameManager>();
            if (_addressablesManager == null)
            {
                _addressablesManager = gm?.AddressablesManager ?? new AddressablesManager();
            }
            builder.RegisterInstance<IAddressablesManager>(_addressablesManager);
            builder.RegisterInstance(new TileDataTracker());
            builder.RegisterInstance(new RewardService(_addressablesManager));
            RegisterPrefabManager(builder);
            
            var jsonFilePersistence = new JsonFilePersistence(logger);
            builder.RegisterInstance(jsonFilePersistence);
            builder.RegisterInstance(new DataPersistenceManager(jsonFilePersistence, _addressablesManager));
            SetAudioManager(builder);

#if UNITY_EDITOR
            Application.targetFrameRate = 60; // quick solution, limits the fan noise and heat with laptops doing dev
#endif
        }

        public static void ConfigureMainMenuScene(IContainerBuilder builder)
        {
            builder.RegisterComponentInHierarchy<GameManager>(); //Persistent through scenes
            builder.RegisterComponentInHierarchy<MenuManager>();
            builder.RegisterComponentInHierarchy<MainMenuUI>();
            builder.RegisterInstance<IMenuFacade>(Object.FindFirstObjectByType<GameManager>());
        }

        public static void ConfigureLevelSelectionAndGame(IContainerBuilder builder)
        {
            builder.RegisterComponentInHierarchy<GOPool>();
            builder.RegisterComponentInHierarchy<MenuManager>();
            builder.RegisterComponentInHierarchy<LevelMaker>();
            builder.RegisterComponentInHierarchy<LevelMakerUI>();
            builder.RegisterComponentInHierarchy<LevelSelectionUI>();
        }

        public static void ConfigureGameScene(IContainerBuilder builder)
        {
            builder.RegisterComponentInHierarchy<GOPool>();
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

            
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            var assetPath = "Assets/Scripts/Setup/DebugSettings.asset";
            var ds = UnityEditor.AssetDatabase.LoadAssetAtPath<DebugSettingsScriptableObject>(assetPath);
            if (ds != null)
            {
                gm.DebugSettings = ds;
                Debug.Log($"GameManager.Awake - Using DebugSettings from {assetPath}");
            }
            
            if (gm != null && gm.DebugSettings.noAudio)
            {
               var audio = gm.transform.GetComponentInChildren<MobileAudioManager>();
               if (audio)
               { //no need
                                gm.AddressablesManager?.ReleaseInstance(audio.gameObject);
               }
               builder.RegisterInstance<IMobileAudioManager>(new MockAudioManager());
               return;
            }
#endif

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
        
        //Only required for Game and LevelSelection scenes, but we can register it here for simplicity
        private static void RegisterPrefabManager(IContainerBuilder builder)
        {
            var prefabManager = Object.FindFirstObjectByType(typeof(PrefabManager)) as PrefabManager;
            if (prefabManager != null)
            {
                //Object.DontDestroyOnLoad(prefabManager.gameObject);
                builder.RegisterInstance(prefabManager);
                builder.RegisterInstance<IPrefabManager>(prefabManager);
                return;
            }

            Debug.LogWarning("PrefabManager not found during container Configure. If this is expected, register an IPrefabManager or ensure PrefabManager exists before LifetimeScope.Configure runs.");
        }
    }
}
