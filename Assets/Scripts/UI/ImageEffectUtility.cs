using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace BlockAndDagger.UI
{
    public class ImageEffectUtility
    {
        private readonly Dictionary<Image, Coroutine> _blinkCoroutines = new Dictionary<Image, Coroutine>();
        private readonly Dictionary<Image, Coroutine> _fadeCoroutines = new Dictionary<Image, Coroutine>();
        private readonly Dictionary<Image, Color> _originalColors = new Dictionary<Image, Color>();
        private MonoBehaviour _mono;

        public ImageEffectUtility(MonoBehaviour mono)
        {
            _mono = mono;
        }

        public void BlinkImageColor(Image image, Color32? blinkColor, float duration)
        {
            if (duration <= 0f)
            {
                throw new InvalidOperationException("Duration must be greater than zero.");
            }
            
            if (_blinkCoroutines.TryGetValue(image, out var existingBlink) && existingBlink != null)
            {
                _mono.StopCoroutine(existingBlink);
                if (_originalColors.TryGetValue(image, out var orig))
                    image.color = orig;
                _blinkCoroutines.Remove(image);
                _originalColors.Remove(image);
            }

            _originalColors[image] = image.color;
            var c = _mono.StartCoroutine(BlinkImage(duration, image, blinkColor));
            _blinkCoroutines[image] = c;
        }

        public void FadeImageAlpha(Image image, float targetAlpha, float duration)
        {
            targetAlpha = Mathf.Clamp01(targetAlpha);

            if (duration <= 0f)
            {
                var cImmediate = image.color;
                image.color = new Color(cImmediate.r, cImmediate.g, cImmediate.b, targetAlpha);
                return;
            }

            if (_fadeCoroutines.TryGetValue(image, out var existingFade) && existingFade != null)
            {
                _mono.StopCoroutine(existingFade);
                _fadeCoroutines.Remove(image);
            }

            if (_blinkCoroutines.TryGetValue(image, out var existingBlink) && existingBlink != null)
            {
                _mono.StopCoroutine(existingBlink);
                _blinkCoroutines.Remove(image);
                _originalColors.Remove(image);
            }

            var fade = _mono.StartCoroutine(FadeImageCoroutine(image, targetAlpha, duration));
            _fadeCoroutines[image] = fade;
        }

        private IEnumerator BlinkImage(float duration, Image image, Color32? blinkColor)
        {
            Color blink = blinkColor.HasValue ? blinkColor.Value : Color.purple;
            Color original = _originalColors.TryGetValue(image, out var o) ? o : image.color;
            float elapsed = 0f;
            while (elapsed < duration)
            {
                float t = (Mathf.Sin(elapsed * Mathf.PI * 4f) + 1f) * 0.5f;
                image.color = Color.Lerp(original, blink, t);
                elapsed += Time.deltaTime;
                yield return null;
            }

            if (_originalColors.TryGetValue(image, out var orig))
            {
                image.color = orig;
                _originalColors.Remove(image);
            }

            _blinkCoroutines.Remove(image);
        }

        public IEnumerator FadeImageCoroutine(Image image, float targetAlpha, float duration)
        {
            float elapsed = 0f;
            Color start = image.color;
            float startAlpha = start.a;
            targetAlpha = Mathf.Clamp01(targetAlpha);

            while (elapsed < duration)
            {
                float t = elapsed / duration;
                float a = Mathf.Lerp(startAlpha, targetAlpha, t);
                image.color = new Color(start.r, start.g, start.b, a);
                elapsed += Time.deltaTime;
                yield return null;
            }

            image.color = new Color(start.r, start.g, start.b, targetAlpha);
            _fadeCoroutines.Remove(image);
        }

        public void SetImageAlphaToDefault(Image image,  float targetAlpha)
        {
            Color start = image.color;
            image.color = new Color(start.r, start.g, start.b, targetAlpha);
        }

        public void StopEffectsForImage(Image image)
        {
            if (image == null) return;

            if (_blinkCoroutines.TryGetValue(image, out var b) && b != null)
            {
                _mono.StopCoroutine(b);
                _blinkCoroutines.Remove(image);
            }

            if (_fadeCoroutines.TryGetValue(image, out var f) && f != null)
            {
                _mono.StopCoroutine(f);
                _fadeCoroutines.Remove(image);
            }

            if (_originalColors.TryGetValue(image, out var orig))
            {
                if (image != null) image.color = orig;
                _originalColors.Remove(image);
            }
        }

        public void StopAllEffects()
        {
            foreach (var kv in new List<Coroutine>(_blinkCoroutines.Values))
            {
                if (kv != null) _mono.StopCoroutine(kv);
            }
            foreach (var kv in new List<Coroutine>(_fadeCoroutines.Values))
            {
                if (kv != null) _mono.StopCoroutine(kv);
            }

            foreach (var kv in _originalColors)
            {
                if (kv.Key != null) kv.Key.color = kv.Value;
            }

            _blinkCoroutines.Clear();
            _fadeCoroutines.Clear();
            _originalColors.Clear();
        }
    }
}
