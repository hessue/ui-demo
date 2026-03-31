using System;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace BlockAndDagger
{
    public sealed class LongClickButton : MonoBehaviour, IPointerDownHandler, IPointerUpHandler
    {
        private bool _pointerDown;
        private float _pointerDownTimer;
        [SerializeField] private float requiredHoldTime;
        [SerializeField] private Image m_fillImage;
        public UnityEvent onLongClick;

        private void OnEnable()
        {
            Reset();
        }

        private void Update()
        {
            if (_pointerDown)
            {
                _pointerDownTimer += Time.deltaTime;
                if (_pointerDownTimer >= requiredHoldTime)
                {
                    onLongClick?.Invoke();
                }
            }

            m_fillImage.fillAmount = _pointerDownTimer / requiredHoldTime;
        }

        public void OnPointerDown(PointerEventData eventData)
        {
            _pointerDown = true;
        }

        public void OnPointerUp(PointerEventData eventData)
        {
            Reset();
        }

        private void Reset()
        {
            _pointerDown = false;
            _pointerDownTimer = 0;
            m_fillImage.fillAmount = _pointerDownTimer / requiredHoldTime;
        }
    }
}