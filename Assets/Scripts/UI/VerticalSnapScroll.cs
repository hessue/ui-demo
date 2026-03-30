using UnityEngine;
using UnityEngine.UI;

namespace BlockAndDagger
{
    public sealed class VerticalSnapScroll : MonoBehaviour
    {
        [Range(0, 500)] public int m_panOffset;
        [Range(0f, 20f)] public float m_snapSpeed;
        [Range(0f, 10f)] public float m_scaleOffset;
        [Range(1f, 20f)] public float m_scaleSpeed;
        [SerializeField] private bool m_scaleFocused;
        [SerializeField] private float m_snapScrollImageSize;

        [SerializeField] private GameObject m_snapScrolItemPrefab;
        public ScrollRect m_scrollRect;
        private SnapScrollItem[] _blockList;
        private Vector2[] _pansPos;
        private Vector2[] _pansScale;
        private RectTransform _contentRect;
        private Vector2 _contentVector;
        private int _selectedPanID;
        private bool _isScrolling;
        private int _itemCount;
        private MultiscrollController _multiscrollController;
        public MultiscrollController MultiscrollController => _multiscrollController;
        private TileType[] _blockTypes;

        public bool HasValues { get; private set; }
        
        private void Start()
        {
            _contentRect = GetComponent<RectTransform>();
        }
        
        public void Init(MultiscrollController multiscrollController, TileType[] availableBlocks)
        {
            _itemCount = availableBlocks.Length;
            _multiscrollController = multiscrollController;
            _blockTypes = availableBlocks;
            _blockList = new SnapScrollItem[availableBlocks.Length];
            _contentRect = GetComponent<RectTransform>();
            _pansPos = new Vector2[availableBlocks.Length];
            _pansScale = new Vector2[availableBlocks.Length];

            for (int i = 0; i < availableBlocks.Length; i++)
            {
                var obj = Instantiate(m_snapScrolItemPrefab, transform, false);
                var scrollItem = obj.GetComponent<SnapScrollItem>();
                scrollItem.Init(availableBlocks[i], _multiscrollController, m_snapScrollImageSize);
                _blockList[i] = scrollItem;
                
                if (i == 0)
                {
                    _blockList[i].transform.localPosition = Vector2.zero;
                    continue;
                }
                
                _blockList[i].transform.localPosition = new Vector2(0,
                    _blockList[i-1].transform.localPosition.y + m_snapScrolItemPrefab.GetComponent<RectTransform>().sizeDelta.y + m_panOffset );
                _pansPos[i] = -_blockList[i].transform.localPosition;
            }
            HasValues = true;
        }
        
        private void FixedUpdate()
        {
            if (_contentRect.anchoredPosition.y >= _pansPos[0].y && !_isScrolling ||
                _contentRect.anchoredPosition.y <= _pansPos[_pansPos.Length - 1].y && !_isScrolling)
            {
                m_scrollRect.inertia = false;
            }

            float nearestPos = float.MaxValue;
            for (int i = 0; i < _itemCount; i++)
            {
                float distance = Mathf.Abs(_contentRect.anchoredPosition.y - _pansPos[i].y);
                if (distance < nearestPos)
                {
                    nearestPos = distance;
                    _selectedPanID = i;
                }

                if (m_scaleFocused)
                {
                    ScaleFocusedItem(i, distance);
                }
            }

            float scrollVelocity = Mathf.Abs(m_scrollRect.velocity.y);
            if (scrollVelocity < 400 && !_isScrolling)
            {
                m_scrollRect.inertia = false;
            }

            if (_isScrolling || scrollVelocity > 400)
            {
                return;
            }
            
            _contentVector.y = Mathf.SmoothStep(_contentRect.anchoredPosition.y, _pansPos[_selectedPanID].y,
                m_snapSpeed * Time.fixedDeltaTime);
            _contentRect.anchoredPosition = _contentVector;
        }

        private void ScaleFocusedItem(int i, float distance)
        {
            float scale = Mathf.Clamp(1 / (distance / m_panOffset) * m_scaleOffset, 0.5f, 1f);
            _pansScale[i].x = Mathf.SmoothStep(_blockList[i].Image.transform.localScale.x, scale + 0.3f, m_scaleSpeed * Time.fixedDeltaTime);
            _pansScale[i].y = Mathf.SmoothStep(_blockList[i].Image.transform.localScale.y, scale + 0.3f, m_scaleSpeed * Time.fixedDeltaTime);
            _blockList[i].Image.transform.localScale = _pansScale[i];
        }

        public void Scrolling(bool scroll)
        {
            _isScrolling = scroll;
            if (scroll) m_scrollRect.inertia = true;
        }
    }
}