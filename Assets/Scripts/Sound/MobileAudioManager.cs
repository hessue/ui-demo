using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;

namespace BlockAndDagger.Sound
{
    public class MobileAudioManager : MonoBehaviour, IMobileAudioManager
    {
        [Header("SFX Pool")] public int sfxSourcePoolSize = 10;
        AudioSource _musicSource;
        private readonly List<AudioSource> _sfxSources = new();
        Coroutine _fadeCoroutine;
        private float _musicVolume = 1f;
        public AudioManagerUnloader Unloader { get; private set; } = new();

        private void Awake()
        {
            _musicSource = gameObject.AddComponent<AudioSource>();
            _musicSource.loop = true;
            _musicSource.playOnAwake = false;

            for (int i = 0; i < sfxSourcePoolSize; i++)
            {
                var go = new GameObject("SFX_" + i);
                go.transform.SetParent(transform);
                var src = go.AddComponent<AudioSource>();
                src.playOnAwake = false;
                _sfxSources.Add(src);
            }
        }
        
        public void PlaySFX(string key, float volume = 1f)
        {
            if (string.IsNullOrEmpty(key)) return;
            if (Unloader.Contains(key) && Unloader.TryGetHandle(key, out var h) && h.IsDone && h.Result != null)
            {
                PlaySFXInternal(h.Result, volume);
                return;
            }

            LoadClipAsync(key, clip => { if (clip != null) PlaySFXInternal(clip, volume); });
        }

        /// <summary>
        /// Play by addressable label
        /// </summary>
        public void PlayMusic(string key, float volume = 1f, bool loop = false)
        {
            if (_fadeCoroutine != null)
            {
                StopCoroutine(_fadeCoroutine);
                _fadeCoroutine = null;
            }

            _musicVolume = volume;
            LoadClipAsync(key, clip =>
            {
                if (clip == null)
                {
                    Debug.LogWarning($"No AudioClips found for label '{key}'");
                    return;
                }
                    
                _musicSource.clip = clip;
                _musicSource.loop = loop;
                _musicSource.volume = _musicVolume;
                _musicSource.Play();
            });
        }

        public void StopMusic(float fadetime = 0f)
        {
            if (_fadeCoroutine != null)
            {
                StopCoroutine(_fadeCoroutine);
                _fadeCoroutine = null;
            }

            if (fadetime <= 0f)
            {
                _musicSource.Stop();
                _musicSource.clip = null;
                return;
            }

            _fadeCoroutine = StartCoroutine(FadeOutAndStop(fadetime));
        }

        public void AdjustPlayingMusicVolume(float volume = 0f)
        {
            volume = Mathf.Clamp01(volume);
            _musicVolume = volume;
        
            if (_fadeCoroutine != null)
            {
                StopCoroutine(_fadeCoroutine);
                _fadeCoroutine = null;
            }
        
            if (_musicSource != null)
            {
                _musicSource.volume = _musicVolume;
            }
        }
        
#region Private
        private void LoadClipAsync(string key, Action<AudioClip> onLoaded = null)
        {
            if (string.IsNullOrEmpty(key))
            {
                onLoaded?.Invoke(null);
                return;
            }

            if (Unloader.Contains(key))
            {
                if (Unloader.TryGetHandle(key, out var cached))
                {
                    if (cached.IsDone && cached.Result != null)
                    {
                        onLoaded?.Invoke(cached.Result);
                        return;
                    }

                    cached.Completed += h => onLoaded?.Invoke(h.Result);
                    return;
                }
            }

            var handle = Addressables.LoadAssetAsync<AudioClip>(key);
            Unloader.AddHandle(key, handle);
            handle.Completed += h =>
            {
                if (h.Status == AsyncOperationStatus.Succeeded)
                {
                    onLoaded?.Invoke(h.Result);
                }
                else
                {
                    Debug.LogWarning($"Audio load failed for '{key}' (expected an AudioClip address).\nConsider using PlayMusicByLabel if '{key}' is a label.");
                    onLoaded?.Invoke(null);
                }
            };
        }
        
        private void LoadClipsByLabelAsync(string label, Action<IList<AudioClip>> onLoaded)
        {
            if (string.IsNullOrEmpty(label))
            {
                onLoaded?.Invoke(null);
                return;
            }

            var listHandle = Addressables.LoadAssetsAsync<AudioClip>(label, null);
            listHandle.Completed += h =>
            {
                if (h.Status == AsyncOperationStatus.Succeeded)
                {
                    onLoaded?.Invoke(h.Result);
                }
                else
                {
                    Debug.LogWarning($"Label load failed for '{label}'");
                    onLoaded?.Invoke(null);
                }
            };
        }

        private void PlaySFXInternal(AudioClip clip, float volume)
        {
            if (clip == null) return;
            var src = _sfxSources.Find(s => !s.isPlaying) ?? _sfxSources[0];
            src.PlayOneShot(clip, volume);
        }
        
        private IEnumerator FadeOutAndStop(float time)
        {
            float startVol = _musicSource.isPlaying ? _musicSource.volume : _musicVolume;
            float t = 0f;
            while (t < time)
            {
                t += Time.deltaTime;
                _musicSource.volume = Mathf.Lerp(startVol, 0f, t / time);
                yield return null;
            }

            _musicSource.Stop();
            _musicSource.clip = null;
            _musicSource.volume = _musicVolume;
            _fadeCoroutine = null;
        }
#endregion
    }
}
