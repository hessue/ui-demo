using System;
using System.Collections;
using UnityEngine;
using UnityEngine.AI;

namespace BlockAndDagger
{
    public class EnemyAI : MonoBehaviour
    {
        [SerializeField] private NavMeshAgent m_navMeshAgent;
        [SerializeField] private Transform _target;
        
        public bool HasTarget => _target != null;

        public void Awake()
        {
            m_navMeshAgent = GetComponent<NavMeshAgent>();
        }

        private void Update()
        {
            if (m_navMeshAgent != null && m_navMeshAgent.isActiveAndEnabled)
            {
                m_navMeshAgent.destination = _target.position;
            }
        }

        public void SetTarget(Transform target)
        {
            _target = target;
            m_navMeshAgent.enabled = true;

            if (m_navMeshAgent.isActiveAndEnabled)
            {
                if (m_navMeshAgent.SetDestination(_target.position))
                {
                    //Console.Write("GREAT!");
                    return;
                }
            }

            Debug.Log("Expecting active NavMeshAgent at this point. Player is probably dead");
        }

        private void Move()
        {
            
        }
    }
}
