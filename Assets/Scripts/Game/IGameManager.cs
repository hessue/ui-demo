using BlockAndDagger.Sound;

namespace BlockAndDagger
{
    public interface IDevCheatService
    {
        Player[] Players { get; }
    }
    
    public interface IMenuFacade
    {
        void ProceedToNextLevel();
        void RunCustomizeMenu();
    }
    
    public interface IGameManager : IMenuFacade, IDevCheatService
    {
        MenuManager MenuManager { get; }
        PrefabManager PrefabManager { get; }
        IngameUI IngameUI { get; }
        IMobileAudioManager AudioManager { get; }
        new Player[] Players { get; }
        
        /*Game Game { get; }
        LevelMaker LevelMaker { get; }
        
       
        IngameUI IngameUI { get; }
        IMobileAudioManager AudioManager { get; }
        DebugSettingsScriptableObject DebugSettings { get; }
        Player[] Players { get; }
        MenuInputActions MenuInputActions { get; }
        IFocusableBlock[] LevelBuilderPlayers { get; }
        ProgressionData ProgressionData { get; }
        string name { get; set; }
        HideFlags hideFlags { get; set; }
        Transform transform { get; }
        TransformHandle transformHandle { get; }
        GameObject gameObject { get; }
        string tag { get; set; }
        Component rigidbody { get; }
        Component rigidbody2D { get; }
        Component camera { get; }
        Component light { get; }
        Component animation { get; }
        Component constantForce { get; }
        Component renderer { get; }
        Component audio { get; }
        Component networkView { get; }
        Component collider { get; }
        Component collider2D { get; }
        Component hingeJoint { get; }
        Component particleSystem { get; }
        bool enabled { get; set; }
        bool isActiveAndEnabled { get; }
        CancellationToken destroyCancellationToken { get; }
        bool useGUILayout { get; set; }
        bool didStart { get; }
        bool didAwake { get; }
        bool runInEditMode { get; set; }

        void Construct(
            ILogger log,
            DataPersistenceManager dataPersistenceManager,
            ProgressionData progressionData,
            IMobileAudioManager audioManager);

        void DestroyActiveLevelObject();
        void RunMainMenu();
        void SwitchSceneToMainMenu();
        void SwitchSceneToLevelSelection();
        void SwitchSceneToGame();
        LevelAndBlueprint GetLevel(LevelName? levelName);
        void RunLevelSelection();
        void CleanLevelAndReload();
        void SetFocusedLevel(LevelName levelName);
        void SetFocusedLevel(LevelAndBlueprint levelAndBlueprint);
        void FocusNextAvailableToLevelSelection();
        void RunCustomizeMenu();

        /// <summary>
        /// </summary>
        /// <param name="levelAndBlueprint">uses previously used or saved LevelAndBlueprint if not provided</param>
        void RunLevelMakerAndCreateLevel(LevelAndBlueprint? levelAndBlueprint);

        /// <summary>
        /// </summary>
        /// <param name="activeLevel">uses previously used or saved Level if not provided</param>
        void RunGame(Level activeLevel = null);

        void ToggleInGamePause();
        void ProceedToNextLevel();
        bool Equals(object other);
        int GetHashCode();
        string ToString();
        EntityId GetEntityId();
        int GetInstanceID();
        Component GetComponent(Type type);
        T GetComponent();
        Component GetComponent(string type);
        bool TryGetComponent(Type type, out Component component);
        bool TryGetComponent(out T component);
        Component GetComponentInChildren(Type t, bool includeInactive);
        Component GetComponentInChildren(Type t);
        T GetComponentInChildren(bool includeInactive);
        T GetComponentInChildren();
        Component[] GetComponentsInChildren(Type t, bool includeInactive);
        Component[] GetComponentsInChildren(Type t);
        T[] GetComponentsInChildren(bool includeInactive);
        void GetComponentsInChildren(bool includeInactive, List result);
        T[] GetComponentsInChildren();
        void GetComponentsInChildren(List results);
        Component GetComponentInParent(Type t, bool includeInactive);
        Component GetComponentInParent(Type t);
        T GetComponentInParent(bool includeInactive);
        T GetComponentInParent();
        Component[] GetComponentsInParent(Type t, bool includeInactive);
        Component[] GetComponentsInParent(Type t);
        T[] GetComponentsInParent(bool includeInactive);
        void GetComponentsInParent(bool includeInactive, List results);
        T[] GetComponentsInParent();
        Component[] GetComponents(Type type);
        void GetComponents(Type type, List<Component> results);
        void GetComponents(List results);
        T[] GetComponents();
        int GetComponentIndex();
        bool CompareTag(string tag);
        bool CompareTag(TagHandle tag);
        void SendMessageUpwards(string methodName, object value, SendMessageOptions options);
        void SendMessageUpwards(string methodName, object value);
        void SendMessageUpwards(string methodName);
        void SendMessageUpwards(string methodName, SendMessageOptions options);
        void SendMessage(string methodName, object value);
        void SendMessage(string methodName);
        void SendMessage(string methodName, object value, SendMessageOptions options);
        void SendMessage(string methodName, SendMessageOptions options);
        void BroadcastMessage(string methodName, object parameter, SendMessageOptions options);
        void BroadcastMessage(string methodName, object parameter);
        void BroadcastMessage(string methodName);
        void BroadcastMessage(string methodName, SendMessageOptions options);
        bool IsInvoking();
        bool IsInvoking(string methodName);
        void CancelInvoke();
        void CancelInvoke(string methodName);
        void Invoke(string methodName, float time);
        void InvokeRepeating(string methodName, float time, float repeatRate);
        Coroutine StartCoroutine(string methodName);
        Coroutine StartCoroutine(string methodName, object value);
        Coroutine StartCoroutine(IEnumerator routine);
        Coroutine StartCoroutine_Auto(IEnumerator routine);
        void StopCoroutine(IEnumerator routine);
        void StopCoroutine(Coroutine routine);
        void StopCoroutine(string methodName);
        void StopAllCoroutines();*/
    }
}