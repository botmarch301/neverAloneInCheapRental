using UnityEngine;
using NAICR.Core;

namespace NAICR.Stats
{
    /// <summary>
    /// 귀신 수치: 호감도, 만족도
    /// stats-design.md 3-1, 3-2 기준.
    /// </summary>
    public class GhostStats : MonoBehaviour
    {
        public static GhostStats Instance { get; private set; }

        public Stat Affection { get; private set; }     // 호감도
        public Stat Satisfaction { get; private set; }   // 만족도 (한의 해소도)

        // ── 상태 추적 ──
        public bool HusbandPassed { get; set; } = false;  // 남편 성불 여부
        public int DaysSinceLastInteraction { get; set; } = 0;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;

            Affection = new Stat("호감도", 0f, 0f, 100f);
            Satisfaction = new Stat("만족도", 0f, 0f, 100f);
        }

        private void OnEnable()
        {
            EventBus.Subscribe<DayEndedEvent>(OnDayEnded);
        }

        private void OnDisable()
        {
            EventBus.Unsubscribe<DayEndedEvent>(OnDayEnded);
        }

        private void OnDayEnded(DayEndedEvent evt)
        {
            // 자연 호감도 증가 (+0.5/일)
            Affection.Modify(0.5f);

            // 방치 페널티: 5일 이상 방치 시 만족도 -1/일
            DaysSinceLastInteraction++;
            if (DaysSinceLastInteraction >= 5)
            {
                Satisfaction.Modify(-1f);
            }
        }

        // ── 호감도 변동 ──

        /// <summary>
        /// 행동에 의한 호감도 변동. 비용(소모)도 이 메서드로 처리.
        /// </summary>
        public void ModifyAffection(float delta, string reason = "")
        {
            Affection.Modify(delta);
            if (delta > 0) DaysSinceLastInteraction = 0;
        }

        // ── 호감도 기반 해금 체크 (3막 스킨십) ──

        public enum SkinshipLevel
        {
            Rejected,           // 0~29: 접촉 거부
            NonSexualOnly,      // 30~49: 비성적 스킨십만
            BreastThigh,        // 50~69: 유방/허벅지
            Genital             // 70+: 성기 접촉
        }

        public SkinshipLevel GetSkinshipLevel()
        {
            float v = Affection.Value;
            if (v >= 70f) return SkinshipLevel.Genital;
            if (v >= 50f) return SkinshipLevel.BreastThigh;
            if (v >= 30f) return SkinshipLevel.NonSexualOnly;
            return SkinshipLevel.Rejected;
        }

        // ── 4막: 호감도의 자원적 역할 ──

        /// <summary>
        /// 성적 행동의 호감도 비용 지불. 여유가 없으면 false 반환 (거부/중단).
        /// </summary>
        public bool TryPayAffectionCost(float cost)
        {
            if (Affection.Value < cost)
                return false;

            Affection.Modify(-cost);
            return true;
        }

        /// <summary>
        /// 새 자세 시도 비용 (-5~-10).
        /// </summary>
        public bool TryNewPosition(float cost)
        {
            return TryPayAffectionCost(cost);
        }

        /// <summary>
        /// 애무 없이 삽입 비용 (-8~-15).
        /// </summary>
        public bool TrySkipForeplay(float cost)
        {
            return TryPayAffectionCost(cost);
        }

        // ── 성감 분기: 거부형 부위 자극 가능 여부 ──

        /// <summary>
        /// 거부형(C계열) 부위 자극 시, 호감도가 충분하면 참아줌 (전환 가능).
        /// 부족하면 역효과.
        /// </summary>
        public bool CanAttemptRejectedZone(float requiredAffection = 70f)
        {
            return Affection.Value >= requiredAffection;
        }

        // ── 성감개발 실수 허용 ──

        public int GetMistakeAllowance()
        {
            float v = Affection.Value;
            if (v >= 90f) return 99;   // 사실상 무제한
            if (v >= 70f) return 4;
            if (v >= 50f) return 2;
            return 0;                  // 실수 즉시 중단
        }

        // ── 만족도 ──

        public void ModifySatisfaction(float delta)
        {
            Satisfaction.Modify(delta);
        }

        /// <summary>
        /// 성불 조건 체크: 만족도 85+ AND 남편 성불 완료 AND 보름달
        /// </summary>
        public bool CanAscend()
        {
            return Satisfaction.Value >= 85f
                && HusbandPassed
                && TimeManager.Instance.IsFullMoon;
        }

        /// <summary>
        /// 확장 트루 조건: 트루 조건 + 호감도 90+
        /// </summary>
        public bool CanExtendedTrueEnd()
        {
            return CanAscend() && Affection.Value >= 90f;
        }
    }
}
