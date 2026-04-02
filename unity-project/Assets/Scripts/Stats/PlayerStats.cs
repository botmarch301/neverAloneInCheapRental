using UnityEngine;
using NAICR.Core;

namespace NAICR.Stats
{
    /// <summary>
    /// 주인공 수치: HP, SAN, 포만감, 양기, GOLD
    /// stats-design.md 기준.
    /// </summary>
    public class PlayerStats : MonoBehaviour
    {
        public static PlayerStats Instance { get; private set; }

        // ── 스탯 정의 (stats-design.md 초기값 기준) ──
        public Stat HP { get; private set; }
        public Stat SAN { get; private set; }
        public Stat Satiety { get; private set; }
        public Stat Qi { get; private set; }
        public Stat Gold { get; private set; }

        // ── 상태 추적 ──
        public int ConsecutiveFrozenFoodDays { get; set; } = 0;
        public bool HasCaffeineAddiction { get; set; } = true;
        public bool HasNicotineAddiction { get; set; } = true; // 게임 시작 시 흡연 중독
        public bool OwnsVR { get; set; } = false;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;

            // stats-design.md 초기값
            HP = new Stat("HP", 70f, 0f, 100f);
            SAN = new Stat("SAN", 60f, 0f, 100f);
            Satiety = new Stat("포만감", 50f, 0f, 100f);
            Qi = new Stat("양기", 100f, 0f, 100f);
            Gold = new Stat("GOLD", 300000f, 0f, float.MaxValue);
        }

        private void OnEnable()
        {
            EventBus.Subscribe<DayEndedEvent>(OnDayEnded);
            EventBus.Subscribe<TurnEndedEvent>(OnTurnEnded);
        }

        private void OnDisable()
        {
            EventBus.Unsubscribe<DayEndedEvent>(OnDayEnded);
            EventBus.Unsubscribe<TurnEndedEvent>(OnTurnEnded);
        }

        // ── 일간 정산 ──
        private void OnDayEnded(DayEndedEvent evt)
        {
            ApplyDailySatietyDecay();
            ApplyQiPenalties();
            ApplyAddictionPenalties();
        }

        private void OnTurnEnded(TurnEndedEvent evt)
        {
            // 턴별 포만감 감소 (구간별 차등)
            ApplyTurnSatietyDecay();
        }

        // ── 포만감 구간별 하락 ──
        private void ApplyTurnSatietyDecay()
        {
            float current = Satiety.Value;
            float decay;

            if (current > 80f)
                decay = -0.375f;    // 100→80: -3/일, 8턴 기준
            else if (current > 40f)
                decay = -1.0f;      // 80→40: -8/일
            else if (current > 20f)
                decay = -0.84f;     // 40→20: -6.7/일
            else
                decay = -0.25f;     // 20→0: -2/일

            Satiety.Modify(decay);
        }

        private void ApplyDailySatietyDecay()
        {
            // 수면 중 하락량 감소 (x0.5) — 수면 턴은 별도 처리
        }

        // ── 양기 페널티 ──
        private void ApplyQiPenalties()
        {
            float qi = Qi.Value;

            // 과잉 (80~100): SAN 페널티
            if (qi >= 80f)
                SAN.Modify(-Random.Range(2f, 5f));
            else if (qi >= 70f)
                SAN.Modify(-1f);

            // 부족 (20~30): HP 페널티
            if (qi <= 20f)
                HP.Modify(-Random.Range(3f, 5f));
            else if (qi <= 30f)
                HP.Modify(-1f);

            // 자연 회복 (수면 +5, 식사 별도)
            Qi.Modify(5f); // 수면 자연 회복
        }

        // ── 중독 페널티 ──
        private void ApplyAddictionPenalties()
        {
            // 담배 금단 (중독 상태에서 금연 시)
            // 카페인 금단
            // 구체적 로직은 EconomySystem에서 미납 상태 확인 후 적용
        }

        // ── 수면 회복 ──
        public void ApplySleepRecovery()
        {
            float satiety = Satiety.Value;
            float hpRecovery = Random.Range(30f, 40f);

            // 기아 페널티
            if (satiety < 20f)
                hpRecovery *= 0.3f;
            else if (satiety < 40f)
                hpRecovery *= 0.5f;

            HP.Modify(hpRecovery);
            SAN.Modify(Random.Range(10f, 15f));
        }

        // ── 엔딩 조건 체크 ──
        public bool IsDeadHP => HP.Value <= 0f;
        public bool IsInsaneSAN => SAN.Value <= 0f;
        public bool IsQiDepleted => Qi.Value <= 0f;

        // ── 환경 피드백용 구간 ──

        public enum ConditionLevel { Good, Moderate, Poor, Critical }

        public ConditionLevel GetHPLevel()
        {
            float v = HP.Value;
            if (v >= 80f) return ConditionLevel.Good;
            if (v >= 50f) return ConditionLevel.Moderate;
            if (v >= 25f) return ConditionLevel.Poor;
            return ConditionLevel.Critical;
        }

        public ConditionLevel GetSANLevel()
        {
            float v = SAN.Value;
            if (v >= 60f) return ConditionLevel.Good;
            if (v >= 30f) return ConditionLevel.Moderate;
            if (v >= 10f) return ConditionLevel.Poor;
            return ConditionLevel.Critical;
        }

        public ConditionLevel GetQiLevel()
        {
            float v = Qi.Value;
            if (v >= 70f) return ConditionLevel.Good;
            if (v >= 40f) return ConditionLevel.Moderate;
            if (v >= 15f) return ConditionLevel.Poor;
            return ConditionLevel.Critical;
        }
    }
}
