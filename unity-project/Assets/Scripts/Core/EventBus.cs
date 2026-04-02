using System;
using System.Collections.Generic;

namespace NAICR.Core
{
    /// <summary>
    /// 전역 이벤트 버스. 시스템 간 결합도를 낮추기 위해 사용.
    /// </summary>
    public static class EventBus
    {
        private static readonly Dictionary<Type, List<Delegate>> _handlers = new();

        public static void Subscribe<T>(Action<T> handler)
        {
            var type = typeof(T);
            if (!_handlers.ContainsKey(type))
                _handlers[type] = new List<Delegate>();
            _handlers[type].Add(handler);
        }

        public static void Unsubscribe<T>(Action<T> handler)
        {
            var type = typeof(T);
            if (_handlers.ContainsKey(type))
                _handlers[type].Remove(handler);
        }

        public static void Publish<T>(T evt)
        {
            var type = typeof(T);
            if (!_handlers.ContainsKey(type)) return;

            // 복사본으로 순회 (핸들러 내에서 구독/해지 가능)
            var snapshot = new List<Delegate>(_handlers[type]);
            foreach (var handler in snapshot)
            {
                (handler as Action<T>)?.Invoke(evt);
            }
        }

        public static void Clear()
        {
            _handlers.Clear();
        }
    }

    // ── 이벤트 정의 ──

    public struct StatChangedEvent
    {
        public string StatName;
        public float OldValue;
        public float NewValue;
        public float Delta;
    }

    public struct TurnEndedEvent
    {
        public int TurnNumber;
        public TimeOfDay TimeOfDay;
    }

    public struct DayEndedEvent
    {
        public int Day;
        public int MoonPhase; // 0~27, 14 = 보름달
    }

    public struct MoonPhaseEvent
    {
        public int Day;
        public bool IsFullMoon;
    }

    public struct ActionPerformedEvent
    {
        public string ActionId;
        public int TurnsCost;
    }

    public struct EndingTriggeredEvent
    {
        public string EndingId;
        public string EndingName;
    }

    public struct ActChangedEvent
    {
        public int PreviousAct;
        public int NewAct;
    }
}
