using System;
using System.Collections.Generic;
using System.Linq;
using System.Timers;
using BlockAndDagger.Sound;
using BlockAndDagger.Utils;
using Unity.AI.Navigation;
using UnityEngine;
using VContainer;

namespace BlockAndDagger
{
    public class Game : MonoBehaviour
    {
        [SerializeField] private GOPool m_goPool;

        private WaveCoordinator _waveCoordinator;
        private Level _level;
        private Timers _timerEvents;
        private const Difficulty difficulty = Difficulty.Insane;
        
        [Inject]
        private readonly ILogger _logger;
        
        private IMobileAudioManager m_audioManager;
    
        [Inject]
        public void Construct(IMobileAudioManager audioManager)
        {
            m_audioManager = audioManager;
        }
        
        public void StartGame(Level level)
        {
            Init(level);
            level.BuildNavMeshSurface();
            if (_timerEvents != null)
            {
                _timerEvents.Start();
            }
            
            //All instances have been created, clean cache
            AddressablesManager.ReleasePreloadedGroup();
        }

        public void PauseGame(bool pause)
        {
            if (_timerEvents is null)
                return;

            m_audioManager.AdjustPlayingMusicVolume(pause ? 0.25f : 1f);
            _timerEvents.PauseAllTimers(pause);
        }

        public void StopGame()
        {
            StopAllCoroutines();
            m_goPool.UnactivateAllPoolObjects();
            //should be called only in case of changing level
            m_goPool.DestroyAllPoolObjects();
            
            if (_timerEvents is null)
                return;

            _timerEvents.StopAndDisposeAllTimers();
        }

        private void Init(Level level)
        {
            _level = level;
            _waveCoordinator = new WaveCoordinator(GameManager.Instance.PrefabManager, level, m_goPool, this);
            if (level.LevelData.LevelEvents != null && level.LevelData.LevelEvents.events.Any())
            {
                SetEventTimers(_level.LevelData);
            }
        }

        private void SetEventTimers(LevelData levelData)
        {
            var list = new List<(ScenarioTimer Timer, Action<object, ElapsedEventArgs> Callback)>();
            if (levelData.ChallengeInfo.ChallengeType == ChallengeType.AgainstTime)
            {
                AddTimeSurvivorTimer(list,levelData.ChallengeInfo);
            }

            if (levelData.LevelEvents.events.Any())
            {
                for (int i = 0; i < levelData.LevelEvents.events.Count; i++)
                {
                    foreach (var wave in levelData.LevelEvents.events)
                    {
                        if (wave.scenarioType == ScenarioType.Enemywave)
                        {
                            var scenarioTimer = new ScenarioTimer(wave, _logger);
                            (ScenarioTimer Timer, Action<object, ElapsedEventArgs> Callback) timedEvent = 
                                (scenarioTimer, (obj, args) => { _waveCoordinator.ReleaseWave(scenarioTimer, args); });

                            list.Add(timedEvent);
                        }
                    }
                }
            }
            
            _timerEvents = new Timers(list);
        }
 
        private void AddTimeSurvivorTimer(List<(ScenarioTimer Timer, Action<object, ElapsedEventArgs> Callback)> list, IAgainstTimeGoalObjective challengeInfo)
        {
            var timerSurvivorLevel = new BaseScenario(){scenarioType = ScenarioType.TimeSurvivor, interval = challengeInfo.TimeToBeat, description = "Time survivor level"};
            var scenarioTimer = new ScenarioTimer(timerSurvivorLevel, _logger);
                
             (ScenarioTimer Timer, Action<object, ElapsedEventArgs> Callback) timedEvent = 
                (scenarioTimer, (obj, args) =>
                {
                    GameManager.Instance.ProceedToNextLevel();
                });
             
             list.Add(timedEvent);
        }
        
        void Update()
        {
            _timerEvents?.OnUpdate();
        }
    }
}