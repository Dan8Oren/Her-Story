using UnityEngine;
using System;
using System.Collections;

public class PassiveTimer  {
        [SerializeField] float _duration;

        public float StartTime {
            get {
                return endTime - Duration;
            }
            set {
                endTime = value + Duration;
                if (!IsSet) {
                    // Just in case endTime was set to 0. This could happen when
                    // initializing timers from the state such as run timer.
                    endTime = float.Epsilon;
                }
            }
        }

        public float EndTime {
            get {
                return endTime;
            }
            set {
                endTime = value;
            }
        }

        public float Duration {
            get {
                return _duration / timeScale;
            }
            set {
                _duration = value;
            }
        }

        public float UnscaledDuration {
            get {
                return _duration;
            }
        }

        public float Now {
            get {
                return IsPaused ? pausedTime : Time.time;
            }
        }

        public float ElapsedTime {
            get {
                if (!IsSet) return 0f;
                return Now - StartTime;
            }
            set {
                StartTime = Now - value;
            }
        }

        public float RemainingTime {
            get {
                return IsSet
                    ? IsActive
                        ? endTime - Now
                        : 0f
                    : Duration;
            }
            set {
                endTime = value + Now;
            }
        }

        public bool IsActive {
            get {
                return Now < endTime;
            }
        }

        public bool IsSet {
            get {
                return endTime > 0f || endTime < -1f;
            }
        }

        public float Progress {
            get {
                return IsSet && Duration > 0
                    ? ElapsedTime / Duration
                    : 0f;
            }
            set {
                ElapsedTime = value * Duration;
            }
        }

        public float ClampedProgress {
            get {
                return Mathf.Clamp01(Progress);
            }
        }

        public float TimeScale {
            get {
                return timeScale;
            }
            set {
                var newValue = Mathf.Max(value, 0.0001f);
                if (IsActive && !Mathf.Approximately(timeScale, newValue)) {
                    RemainingTime *= timeScale / newValue;
                }
                timeScale = newValue;
            }
        }

        public bool IsPaused {
            get {
                return pausedTime >= 0f;
            }
        }

        private float endTime;
        private float timeScale = 1f;
        private float pausedTime = -1f;

        public PassiveTimer() {}

        public PassiveTimer(float duration) {
            this._duration = duration;
        }

        public void Start() {
            pausedTime = -1f;
            #if UNITY_EDITOR
            if (Time.time == 0f && Duration == 0f) {
                endTime = 0.001f;
                return;
            }
            #endif
            endTime = Time.time + Duration;
        }

        public void Start(float duration) {
            pausedTime = -1f;
            this._duration = duration;
            this.endTime = Time.time + Duration;
        }

        public void Clear() {
            pausedTime = -1f;
            this.endTime = 0f;
        }

        public void Pause() {
            if (IsPaused) return;
            pausedTime = Time.time;
        }

        public void Unpause() {
            if (!IsPaused) return;
            var elapsedTime = ElapsedTime;
            pausedTime = -1f;
            ElapsedTime = elapsedTime;
        }

        public void ContinueTimer(PassiveTimer fromTimer) {
            if (!fromTimer.IsSet) {
                Clear();
                return;
            }
            pausedTime = -1f;
            endTime = fromTimer.EndTime;
            timeScale = fromTimer.TimeScale;
        }

        /// <summary>
        /// Set the remaining time to the timer. Time will be scaled according to the timer's
        /// time scale value.
        /// </summary>
        public void SetRemainingTimeAndPreserveStartTime(float remainingTime) {
            if (!IsActive) {
                RemainingTime = remainingTime / timeScale;
                return;
            }
            var additionalTime = remainingTime - RemainingTime;
            endTime += additionalTime / timeScale;
            _duration += additionalTime;
        }

        /// <summary>
        /// Add time to the timer. Time will be scaled according to the timer's
        /// time scale value.
        /// </summary>
        public void AddTimeAndPreserveStartTime(float additionalTime) {
            if (!IsActive) {
                RemainingTime = additionalTime / timeScale;
                return;
            }
            endTime += additionalTime / timeScale;
            _duration += additionalTime;
        }

        public void AddPercentToRemainingTime(float percentFromDuration) {
            if (!IsActive) {
                return;
            }
            var time = _duration * percentFromDuration;
            endTime += time;
        }
    }