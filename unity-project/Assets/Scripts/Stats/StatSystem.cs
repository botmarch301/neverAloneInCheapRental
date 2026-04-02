using UnityEngine;
using NAICR.Core;

namespace NAICR.Stats
{
    /// <summary>
    /// 단일 스탯. 범위 제한, 변동 추적, 이벤트 발행.
    /// </summary>
    [System.Serializable]
    public class Stat
    {
        [SerializeField] private string _name;
        [SerializeField] private float _value;
        [SerializeField] private float _min;
        [SerializeField] private float _max;
        [SerializeField] private float _initialValue;

        public string Name => _name;
        public float Value => _value;
        public float Min => _min;
        public float Max => _max;
        public float InitialValue => _initialValue;

        /// <summary>
        /// 정규화된 값 (0~1).
        /// </summary>
        public float Normalized => Mathf.InverseLerp(_min, _max, _value);

        public Stat(string name, float initialValue, float min = 0f, float max = 100f)
        {
            _name = name;
            _initialValue = initialValue;
            _value = initialValue;
            _min = min;
            _max = max;
        }

        /// <summary>
        /// 값을 변경한다. 범위 내로 클램프되고 이벤트를 발행한다.
        /// </summary>
        public void Modify(float delta)
        {
            if (Mathf.Approximately(delta, 0f)) return;

            float old = _value;
            _value = Mathf.Clamp(_value + delta, _min, _max);
            float actual = _value - old;

            if (!Mathf.Approximately(actual, 0f))
            {
                EventBus.Publish(new StatChangedEvent
                {
                    StatName = _name,
                    OldValue = old,
                    NewValue = _value,
                    Delta = actual
                });
            }
        }

        /// <summary>
        /// 값을 직접 설정한다.
        /// </summary>
        public void Set(float value)
        {
            float old = _value;
            _value = Mathf.Clamp(value, _min, _max);

            if (!Mathf.Approximately(_value, old))
            {
                EventBus.Publish(new StatChangedEvent
                {
                    StatName = _name,
                    OldValue = old,
                    NewValue = _value,
                    Delta = _value - old
                });
            }
        }

        /// <summary>
        /// 초기값으로 리셋.
        /// </summary>
        public void Reset()
        {
            Set(_initialValue);
        }
    }
}
