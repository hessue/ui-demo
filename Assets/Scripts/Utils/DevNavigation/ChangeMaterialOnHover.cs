
using UnityEngine;

namespace BlockAndDagger.Utils.DevTools
{

    public sealed class ChangeMaterialOnHover : MonoBehaviour
    {
        /// <summary>
        /// MouseOverColor
        /// </summary>
        [SerializeField] private Material highlightMat;

        private Material defaultMat;
        private static bool _highlightOn;

        [HideInInspector] public Block _block;
        public const float ChangeTileUpdateInterval = 0.05f;
        private bool _isHighlighted;
        /*public bool IsHighlighted => _block.IsSelected;

        private void Awake()
        {
            _block = GetComponent<Block>();
            defaultMat = GetComponent<Renderer>().material;
        }

        public void Select()
        {
            GetComponent<Renderer>().material = highlightMat;
            _highlightOn = true;
            _block.IsSelected = true;
            GridGame.Instance._currentHighlight = _block; //TODO callback or return val
            _isHighlighted = true;
        }

        public void Deselect()
        {
            GetComponent<Renderer>().material = defaultMat;
            _highlightOn = false;
            _block.IsSelected = false;
            _isHighlighted = false;
        }*/
    }
}