using System;
using System.Collections.Generic;
using UnityEngine;

namespace BlockAndDagger
{
    [Serializable]
    public sealed class LevelEvents
    {
        [SerializeField] public int allWavesTotalEnemyCount;
        [HideInInspector] public int currentWaveIndex;
        [SerializeField] public List<BaseScenario> events;
    }
}
