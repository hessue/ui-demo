using System.Linq;
using UnityEngine;

namespace BlockAndDagger
{
    public class BlinkMaterialAlpha : MonoBehaviour
    {
        public Renderer _renderer;
        public Renderer[] renderers;
        private bool IsMultimesh = false;

        void Awake()
        {
            renderers = GetComponents<Renderer>();
            if (renderers != null && renderers.Length > 1)
            {
                IsMultimesh = true;
            }
            else
            {
                _renderer = renderers.First();
            }
        }

        private float _alphaBlinkMin;
        private float _alphaBlinkMax;
        public void Init(bool isEmptyBlock)
        {
            if (isEmptyBlock)
            {
                _alphaBlinkMin = 0.1f;
                _alphaBlinkMax = 0.6f;
            }
            else
            {
                _alphaBlinkMin = 0.1f;
                _alphaBlinkMax = 1f;
            }
        }

        void Update()
        {
            if (_renderer == null && renderers == null)
                return;

            if (IsMultimesh)
            {
                foreach (var rend in renderers)
                {
                    PingPongAlpha(rend);
                }
            }
            else
            {
                PingPongAlpha(_renderer);
            }
        }

        private void PingPongAlpha(Renderer renderer)
        {
            Color flashAlpha = renderer.material.color;
            var lerp = Mathf.PingPong(Time.time, 1f) / 2f;
            flashAlpha.a = Mathf.Lerp(_alphaBlinkMin, _alphaBlinkMax, Mathf.SmoothStep(0.0f, 1.0f, lerp));
            renderer.material.color = flashAlpha;
        }
    }
}