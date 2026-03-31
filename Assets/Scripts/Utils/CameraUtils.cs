using System.Collections;
using UnityEngine;

namespace BlockAndDagger
{
    public static class CameraUtils
    {
        public static IEnumerator SetCameraFov(Camera cam, float targetFov, float duration)
        {
            if (cam == null)
                yield break;

            // If duration is zero or negative, set immediately
            if (duration <= 0f)
            {
                cam.fieldOfView = targetFov;
                yield break;
            }

            float startFov = cam.fieldOfView;
            float elapsed = 0f;

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / duration);
                cam.fieldOfView = Mathf.Lerp(startFov, targetFov, t);
                yield return null;
            }

            // Ensure final value is exact
            cam.fieldOfView = targetFov;
        }
    }
}
