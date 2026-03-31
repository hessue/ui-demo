using System;
using System.Collections;
using System.Timers;
using UnityEngine;

namespace BlockAndDagger
{
    /// <summary>
    /// All spawns will be reserved for single event, other events will yield until _allSpawnsBooked = false
    /// </summary>
    public sealed class WaveCoordinator
    {
        private int _currentWaveIndex;
        private PrefabManager _prefabManager;
        private SpawnManager _spawner;
        private GOPool _goPool;
        private MonoBehaviour _monoBehaviour;
        private static bool _allSpawnsBooked;

        public WaveCoordinator(PrefabManager prefabManager, Level level,
            GOPool goPool, MonoBehaviour mono)
        {
            _monoBehaviour = mono;
            _prefabManager = prefabManager;
            _goPool = goPool;
            _goPool.PrepopulateWithEnemies(level.LevelData?.LevelEvents);
            _spawner = new SpawnManager(level, _goPool);
        }

        /// <summary>
        /// Any scenario or wave
        /// </summary>
        /// <param name="scenario"></param>
        private void ScenarioReleased(BaseScenario releasedEvent)
        {
            _currentWaveIndex++;
        }
        
        public void ReleaseWave(object sender,  ElapsedEventArgs args)
        {
            if (!GameManager.Instance.DebugSettings.noEnemies)
            {
                var targetTimer = (ScenarioTimer)sender;
                _monoBehaviour.StartCoroutine(ReleaseWave(targetTimer));
            }
        }

        IEnumerator ReleaseWave(ScenarioTimer targetTimer)
        {
            Console.WriteLine("Release wave triggered!");
             _monoBehaviour.StartCoroutine(SpawnGOs(targetTimer.ScenarioData));
             targetTimer.RestartImmediatelyIfRepeatsLeft();
            
            yield return null;
        }

        private IEnumerator SpawnGOs(BaseScenario eventData)
        {
            //TODO: some randomize
            //var pos = _spawner.GetRandomSpawn();
            
            //Waits here until previous events is fully complete
            if (_allSpawnsBooked)
            {
                yield return null;
            }

            _monoBehaviour.StartCoroutine(BookSpawnToSpawn(eventData));
        }

        private IEnumerator BookSpawnToSpawn(BaseScenario eventData)
        {
            _allSpawnsBooked = true;
            foreach (var wave in eventData.waves)
            {
                //TODO: scatter around spawn. Choose another spawn or change pos a bit
                yield return _monoBehaviour.StartCoroutine(SpawnObjects(wave, 0.5f));
            }
            
            ScenarioReleased(null);
            _allSpawnsBooked = false;
            yield return null;
        }

        private IEnumerator SpawnObjects(Wave wave, float waitTimeBetweenSpawning = 0.0f)
        {
            for (int i = 0; i < wave.enemyCountToSpawn; i++)
            {
                int enemyTypeIndex = UnityEngine.Random.Range(1, wave.spawnEnemyTypes.Length) - 1;
                var levelObjectType = wave.spawnEnemyTypes[enemyTypeIndex];
                
                yield return new WaitForSeconds(waitTimeBetweenSpawning); //Give a bit time for GameObject to fall on ground and move away when at same pos
                
                var targetSpawn = DetermineSpawn(wave);
                var spawnedObj = _spawner.SpawnObject(levelObjectType, targetSpawn.transform.position, false);
                
                //hack, to let the enemy to wakeup itself
                spawnedObj.SetActive(true);
            }
        }

        private SpawnerBlock DetermineSpawn(Wave wave)
        {
            int spawnIndex = 0;
            if (wave.spawnsToUse.Length > 1)
            {
                 spawnIndex = UnityEngine.Random.Range(0, wave.spawnsToUse.Length);
            }

            return _spawner.GetSpawnSpawnerBlock(spawnIndex);
        }
    }
}