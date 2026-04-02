using UnityEngine;

namespace NAICR.Core
{
    /// <summary>
    /// 시간대 열거형
    /// </summary>
    public enum TimeOfDay
    {
        Morning,    // 아침
        Daytime,    // 낮
        Evening,    // 저녁
        Night,      // 밤
        Midnight    // 심야 (수면)
    }

    /// <summary>
    /// 시간 체계 관리. 턴, 시간대, 일수, 보름달 주기.
    /// </summary>
    public class TimeManager : MonoBehaviour
    {
        public static TimeManager Instance { get; private set; }

        [Header("Current State")]
        [SerializeField] private int _currentDay = 1;
        [SerializeField] private int _currentTurn = 0;
        [SerializeField] private TimeOfDay _currentTimeOfDay = TimeOfDay.Morning;

        // 시간대별 사용 가능한 턴 수
        private static readonly int[] TurnsPerTimeOfDay = { 2, 3, 3, 3, 0 };
        // Morning=2, Daytime=3, Evening=3, Night=3, Midnight=자동(수면)

        private int _turnsRemainingInPeriod;

        // ── Properties ──

        public int CurrentDay => _currentDay;
        public int CurrentTurn => _currentTurn;
        public TimeOfDay CurrentTimeOfDay => _currentTimeOfDay;

        /// <summary>
        /// 보름달 주기 내 위치 (0~27). 14 = 보름달.
        /// </summary>
        public int MoonPhase => (_currentDay - 1) % 28;

        /// <summary>
        /// 오늘이 보름달인지.
        /// </summary>
        public bool IsFullMoon => MoonPhase == 14;

        // ── Lifecycle ──

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            _turnsRemainingInPeriod = TurnsPerTimeOfDay[(int)_currentTimeOfDay];
        }

        // ── Public Methods ──

        /// <summary>
        /// 턴을 소비한다. 행동에 따라 여러 턴을 소비할 수 있음.
        /// </summary>
        public void ConsumeTurns(int turns)
        {
            for (int i = 0; i < turns; i++)
            {
                _currentTurn++;
                _turnsRemainingInPeriod--;

                EventBus.Publish(new TurnEndedEvent
                {
                    TurnNumber = _currentTurn,
                    TimeOfDay = _currentTimeOfDay
                });

                if (_turnsRemainingInPeriod <= 0)
                {
                    AdvanceTimeOfDay();
                }
            }
        }

        /// <summary>
        /// 현재 시간대에서 남은 턴 수.
        /// </summary>
        public int GetRemainingTurns()
        {
            return _turnsRemainingInPeriod;
        }

        /// <summary>
        /// 강제로 다음 시간대로 이동 (남은 턴 스킵).
        /// </summary>
        public void SkipToNextPeriod()
        {
            AdvanceTimeOfDay();
        }

        // ── Private Methods ──

        private void AdvanceTimeOfDay()
        {
            var next = (TimeOfDay)((int)_currentTimeOfDay + 1);

            if (next > TimeOfDay.Midnight)
            {
                // 심야 → 다음 날 아침
                EndDay();
                return;
            }

            _currentTimeOfDay = next;

            if (_currentTimeOfDay == TimeOfDay.Midnight)
            {
                // 심야: 수면 처리 후 다음 날로
                EndDay();
            }
            else
            {
                _turnsRemainingInPeriod = TurnsPerTimeOfDay[(int)_currentTimeOfDay];
            }
        }

        private void EndDay()
        {
            EventBus.Publish(new DayEndedEvent
            {
                Day = _currentDay,
                MoonPhase = MoonPhase
            });

            // 보름달 이벤트
            if (IsFullMoon)
            {
                EventBus.Publish(new MoonPhaseEvent
                {
                    Day = _currentDay,
                    IsFullMoon = true
                });
            }

            // 다음 날
            _currentDay++;
            _currentTurn = 0;
            _currentTimeOfDay = TimeOfDay.Morning;
            _turnsRemainingInPeriod = TurnsPerTimeOfDay[(int)TimeOfDay.Morning];
        }

        // ── 시간대 표시용 ──

        public string GetTimeOfDayText()
        {
            return _currentTimeOfDay switch
            {
                TimeOfDay.Morning => "아침",
                TimeOfDay.Daytime => "낮",
                TimeOfDay.Evening => "저녁",
                TimeOfDay.Night => "밤",
                TimeOfDay.Midnight => "심야",
                _ => ""
            };
        }

        public string GetMoonPhaseText()
        {
            int phase = MoonPhase;
            if (phase == 14) return "보름달";
            if (phase == 0) return "그믐달";
            if (phase < 14) return "차오르는 달";
            return "기우는 달";
        }
    }
}
