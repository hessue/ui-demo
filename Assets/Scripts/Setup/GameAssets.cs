using UnityEngine;

namespace BlockAndDagger
{
    //Do not make a SingletonMono
    public class GameAssets : MonoBehaviour
    {
        [SerializeField] private Player m_playerPrefab;
        [SerializeField] private GameObject m_enemyPrefab;
        
        public Player PlayerPrefab => m_playerPrefab;
        public GameObject EnemyPrefab => m_enemyPrefab;
    }
}
