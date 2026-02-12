using UnityEngine;

namespace JellyGunner
{
    public struct TimeGate
    {
        private float _interval;
        private float _nextPassTime;

        public TimeGate(float interval)
        {
            _interval = interval;
            _nextPassTime = 0f;
        }

        public bool TryPass()
        {
            if (Time.time < _nextPassTime) return false;
            _nextPassTime = Time.time + _interval;
            return true;
        }

        public void Reset() => _nextPassTime = 0f;
        public void SetInterval(float interval) => _interval = interval;
    }
}
