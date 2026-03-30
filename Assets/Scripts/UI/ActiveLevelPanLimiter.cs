using UnityEngine;

namespace BlockAndDagger.UI
{
    //Compute world-space limits(clamped rectangle) based on level blocks (north/east/south/west)
    public sealed class ActiveLevelPanLimiter
    {
        private float _paddingMeters;
        private float _extraBottomPaddingMeters;
        private float _extraTopPaddingMeters;
        private float _minWorldX = float.NegativeInfinity;
        private float _maxWorldX = float.PositiveInfinity;
        private float _minWorldZ = float.NegativeInfinity;
        private float _maxWorldZ = float.PositiveInfinity;

        public bool IsEnabled => !float.IsNegativeInfinity(_minWorldX);

        public ActiveLevelPanLimiter(Level level, float paddingMeters = 0f, float extraBottomPaddingMeters = 0f, float extraTopPaddingMeters = 0f)
        {
            SetPanLimiter(level, paddingMeters, extraBottomPaddingMeters, extraTopPaddingMeters);
        }

        public void SetPanLimiter(Level level, float paddingMeters = 0f, float extraBottomPaddingMeters = 0f, float extraTopPaddingMeters = 0f)
        {
            _paddingMeters = paddingMeters;
            _extraBottomPaddingMeters = extraBottomPaddingMeters;
            _extraTopPaddingMeters = extraTopPaddingMeters;

            _minWorldX = float.PositiveInfinity;
            _maxWorldX = float.NegativeInfinity;
            _minWorldZ = float.PositiveInfinity;
            _maxWorldZ = float.NegativeInfinity;
            if (level?.LevelData != null)
            {
                ConsiderNewLimit(level.LevelData.GroundThree);
                ConsiderNewLimit(level.LevelData.GroundTwo);
                ConsiderNewLimit(level.LevelData.GroundOne);
                ConsiderNewLimit(level.LevelData.GroundZero);
                ConsiderNewLimit(level.LevelData.StaticMainStructures);
                ConsiderNewLimit(level.LevelData.StaticWalkingPlatform);

                if (float.IsPositiveInfinity(_minWorldX))
                {
                    // no blocks found, reset to infinite so old logic can apply
                    _minWorldX = float.NegativeInfinity;
                    _maxWorldX = float.PositiveInfinity;
                    _minWorldZ = float.NegativeInfinity;
                    _maxWorldZ = float.PositiveInfinity;
                }
                else
                {
                    _minWorldX -= _paddingMeters;
                    _maxWorldX += _paddingMeters;
                    _minWorldZ -= _paddingMeters;
                    _maxWorldZ += _paddingMeters + _extraTopPaddingMeters;
                    _minWorldZ -= _extraBottomPaddingMeters;
                }
            }
            
            void ConsiderNewLimit(Block[] arr)
            {
                if (arr == null) return;
                foreach (var b in arr)
                {
                    if (b == null) continue;
                    var p = b.transform.position;
                    _minWorldX = Mathf.Min(_minWorldX, p.x);
                    _maxWorldX = Mathf.Max(_maxWorldX, p.x);
                    _minWorldZ = Mathf.Min(_minWorldZ, p.z);
                    _maxWorldZ = Mathf.Max(_maxWorldZ, p.z);
                }
            }
        }

        public Vector3 ClampPosition(Vector3 desiredPosition, Camera cam)
        {
            // If limiter not ready (no computed world limits), do nothing
            if (!IsActive(cam))
            {
                return desiredPosition;
            }

            float clampedX = Mathf.Clamp(desiredPosition.x, _minWorldX, _maxWorldX);
            float clampedZ = Mathf.Clamp(desiredPosition.z, _minWorldZ, _maxWorldZ);
            return new Vector3(clampedX, desiredPosition.y, clampedZ);
        }

        public bool IsActive(Camera cam)
        {
            return !float.IsNegativeInfinity(_minWorldX);
        }
    }
}
