using UnityEngine;

namespace BlockAndDagger
{
    [CreateAssetMenu(fileName = "Data", menuName = "ScriptableObjects/DebugSettingsScriptableObject", order = 1)]
    public class DebugSettingsScriptableObject : ScriptableObject
    {
        public bool noAudio;
        public bool allowTwoAbilityButtons;
        public bool showStaticBlockAsPurple;
        public bool noEnemies;
    }
}
