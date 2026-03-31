using System;

namespace BlockAndDagger
{
    [Serializable]
    public sealed class Wave
    {
        //Currently indexes, IDs would be better
        public int[] spawnsToUse;
        public int enemyCountToSpawn;
        public LevelObjectType[] spawnEnemyTypes;
    }
}
