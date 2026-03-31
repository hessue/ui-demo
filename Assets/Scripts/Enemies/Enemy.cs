using System;
using System.Linq;
using UnityEngine;

namespace BlockAndDagger
{
    public class Enemy : MonoBehaviour, IFieldObject
    {
        public HealthStatus _status = HealthStatus.Dead;
        public int _health;
        public const int MaxHealth = 1;
        private EnemyAI _enemyAI;
        private EnemyController _enemyController; //TODO: implement own version
        private bool _livingAndKicking;
        private IFieldObject _fieldObjectImplementation;

        [field: SerializeField] public PoolStatus PoolStatus { get; set; }
        
        [field: SerializeField] public EnemyType EnemyType { get; private set; }

        private void Awake()
        {
            _enemyAI = GetComponent<EnemyAI>();
            _enemyController = GetComponent<EnemyController>();
        }

        private void Update()
        {
            if (!_livingAndKicking)
            {
                TurnAliveOnFirstGroundHit();
            }
        }
        
        // Could do agent.Warp(), but enemies falling from cliffs will look more interesting
        private void TurnAliveOnFirstGroundHit()
        {
            if (_enemyController.IsGrounded)
            {
                _enemyController.enabled = false; //Won't work with NavMeshAgent, quick hax so disable it .... =) 
                _livingAndKicking = true;
                StartLiving();
            }
        }

        public void Init()
        {
            _health = MaxHealth;
            _livingAndKicking = false;
        }

        public void StartLiving()
        {
            gameObject.SetActive(true);
            var playerOne = GameManager.Instance.Players.First();
            if (playerOne.transform != null)
            {
                _enemyAI.SetTarget(playerOne.transform);
            }
           
            PoolStatus = PoolStatus.Living;
            _status = HealthStatus.Alive;
        }

        public void KillAndReturnToPool()
        {
            PoolStatus = PoolStatus.ReturnedToPool;
            _livingAndKicking = false;
            gameObject.SetActive(false);
        }
    }
    
    public enum EnemyType {
        Basic,
        Highground
    }
}