using System;
using System.Linq;
using System.Collections;
using UnityEngine;

namespace BlockAndDagger
{
    public sealed class MultiscrollController : MonoBehaviour
    {
        [SerializeField] private GameObject m_scrollOne;
        [SerializeField] private GameObject m_scrollTwo;
        [SerializeField] private bool m_useTwoScrolls;
        [SerializeField] private TileType selecteBlockType;
        private VerticalSnapScroll _snapScrollOne;
        private VerticalSnapScroll _snapScrollTwo;

        public event Action OnInitialized;
        public bool IsInitialized { get; private set; }

        public TileType SelectedBlock
        {
            get { return selecteBlockType; }
            set
            {
                selecteBlockType = value;
                GameManager.Instance.MenuManager.LevelMakerUI
                    .RefreshAvailableOptions(); //TODO: bring reference by other means
            }
        }

        private void Awake()
        {
            _snapScrollOne = m_scrollOne.GetComponentInChildren<VerticalSnapScroll>();
        }

        private void Start()
        {
            //TODO: reference
            var blockTypes = GameManager.Instance.LevelBuilderPlayers.First().UnlockedBlockTypes;
            _snapScrollOne.Init(this, blockTypes);
            SelectedBlock = blockTypes.First(); //Scroll visual position might not match selection

            if (m_useTwoScrolls)
            {
                _snapScrollTwo = m_scrollTwo.GetComponentInChildren<VerticalSnapScroll>();
                if (_snapScrollTwo != null)
                {
                    _snapScrollTwo.gameObject.SetActive(true);
                    _snapScrollTwo.Init(this, blockTypes);
                }
            }

            // Ensure we only mark initialized once the snap scroll(s) report they have values
            StartCoroutine(WaitForSnapReadyAndNotify());
        }

        private IEnumerator WaitForSnapReadyAndNotify()
        {
            const float timeout = 2f;
            float elapsed = 0f;

            while (elapsed < timeout)
            {
                bool oneReady = _snapScrollOne != null && _snapScrollOne.HasValues;
                bool twoReady = _snapScrollTwo == null || !_snapScrollTwo.gameObject.activeSelf || _snapScrollTwo.HasValues;

                if (oneReady && twoReady)
                {
                    IsInitialized = true;
                    OnInitialized?.Invoke();
                    yield break;
                }

                elapsed += Time.deltaTime;
                yield return null;
            }

            IsInitialized = true;
            Debug.LogWarning("MultiscrollController: snap scroll(s) did not report values within timeout — invoking OnInitialized anyway.");
            OnInitialized?.Invoke();
        }

        //TODO: wall as the first block in the scroll or make scroll move visually to selected TileType
        public void SelectDefaultBlockType(TileType defaultBlockType)
        {
            var scroll = m_scrollOne.GetComponentInChildren<VerticalSnapScroll>();
            scroll.MultiscrollController.SelectedBlock = defaultBlockType;
        }
    }
}