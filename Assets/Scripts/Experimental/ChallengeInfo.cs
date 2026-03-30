using System;
using Newtonsoft.Json;
using UnityEngine;

namespace BlockAndDagger
{
    //TODO: rename LevelGoal/LevelObjective
    [Serializable]
    public class ChallengeInfo : ITargetLocationBlockObjective, IAgainstTimeGoalObjective, ICollectGoalObjective,
        ISlayEnemiesObjective
    {
        public ChallengeType challengeType;
        public float timeToBeat;
        public int collectCount;
        public int touchCount;
        public TileType touchTileType;
        public LevelObjectType collectableType;
        public int countToSlay;
        public LevelObjectType typeToSlay;

        [JsonIgnore] public ChallengeType ChallengeType => challengeType;
        [JsonIgnore] public float TimeToBeat => timeToBeat;
        [JsonIgnore] public int CollectCount => collectCount;
        [JsonIgnore] public int TouchCount => touchCount;
        [JsonIgnore] public TileType TouchTileType => touchTileType;
        [JsonIgnore] public LevelObjectType CollectableType => collectableType;
        [JsonIgnore] public int CountToSlay => countToSlay;
        [JsonIgnore] public LevelObjectType TypeToSlay => typeToSlay;

        public ChallengeInfo(ChallengeType challengeType)
        {
            this.challengeType = challengeType;
        }

        public void ValidateInfo()
        {
            switch (challengeType)
            {
                case ChallengeType.NOT_DEFINED:
                    Debug.LogWarning(
                        "ChallengeInfo validation error. TypeOfChallenge is NOT_DEFINED, please recreate map or add the type to Level json file manually");
                    break;
                case ChallengeType.AgainstTime:
                    if (TimeToBeat < 5)
                    {
                        Debug.LogWarning("ChallengeInfo validation error. TimeToBeat must be at least 5 seconds");
                    }

                    break;
                case ChallengeType.GetToTarget:
                    if (TouchCount <= 0)
                    {
                        Debug.LogWarning("ChallengeInfo validation error. TouchCount must be at least 1");
                    }
                    else if (TouchTileType == TileType.ERROR_NOT_DEFINED)
                    {
                        Debug.LogWarning("ChallengeInfo validation error. CollectableType cannot be zero");
                    }

                    break;
                case ChallengeType.Collect:
                    if (CollectCount <= 0)
                    {
                        Debug.LogWarning("ChallengeInfo validation error. CollectCount must be at least 1");
                    }
                    else if (CollectableType == LevelObjectType.NOT_DEFINED)
                    {
                        Debug.LogWarning("ChallengeInfo validation error. CollectableType cannot be zero");
                    }

                    break;
                case ChallengeType.SlayEnemies:
                    if (TypeToSlay != LevelObjectType.All && CountToSlay <= 0)
                    {
                        Debug.LogWarning("ChallengeInfo validation error. CountToSlay cannot be zero");
                    }
                    else if (TypeToSlay == LevelObjectType.NOT_DEFINED)
                    {
                        Debug.LogWarning("ChallengeInfo validation error. CountToSlay cannot be zero");
                    }

                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }
    }

    public interface IChallengeInfo
    {
        public ChallengeType ChallengeType { get; }
    }

    public interface ITargetLocationBlockObjective : IChallengeInfo
    {
        int TouchCount { get; }
        public TileType TouchTileType { get; }
    }

    public interface IAgainstTimeGoalObjective : IChallengeInfo
    {
        public float TimeToBeat { get; }
    }

    public interface ICollectGoalObjective : IChallengeInfo
    {
        public int CollectCount { get; }
        public LevelObjectType CollectableType { get; }
    }

    public interface ISlayEnemiesObjective : IChallengeInfo
    {
        public int CountToSlay { get; }
        public LevelObjectType TypeToSlay { get; }
    }
}