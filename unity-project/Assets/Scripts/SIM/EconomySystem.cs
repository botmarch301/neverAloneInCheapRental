using UnityEngine;
using NAICR.Core;
using NAICR.Stats;

namespace NAICR.SIM
{
    /// <summary>
    /// 경제 시스템. 월 고정지출 자동 차감, 미납 페널티.
    /// stats-design.md 2-5 기준.
    /// </summary>
    public class EconomySystem : MonoBehaviour
    {
        public static EconomySystem Instance { get; private set; }

        // ── 고정지출 항목 ──
        [System.Serializable]
        public struct MonthlyExpense
        {
            public string Name;
            public float Amount;
            public bool IsMandatory;    // 미납 시 페널티 있음
            public bool CanCancel;      // 플레이어가 취소 가능
        }

        // 월세 미납 횟수
        public int RentUnpaidCount { get; private set; } = 0;

        // VR 할부 남은 개월
        public int VRInstallmentMonths { get; set; } = 0;

        // 가전 할부 남은 개월
        public int ApplianceInstallmentMonths { get; private set; } = 10;

        // 현재 달 (28일 = 1개월)
        private int _currentMonth = 1;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
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
            // 28일마다 월말 정산
            if (evt.Day % 28 == 0)
            {
                ProcessMonthlyExpenses();
                _currentMonth++;
            }
        }

        private void ProcessMonthlyExpenses()
        {
            var ps = PlayerStats.Instance;
            float totalExpense = GetTotalMonthlyExpense();

            if (ps.Gold.Value >= totalExpense)
            {
                ps.Gold.Modify(-totalExpense);
                RentUnpaidCount = 0;

                if (ApplianceInstallmentMonths > 0)
                    ApplianceInstallmentMonths--;
                if (VRInstallmentMonths > 0)
                    VRInstallmentMonths--;
            }
            else
            {
                // 소지금 부족: 우선순위별 납부 (월세 최우선)
                ProcessPartialPayment(ps);
            }
        }

        private void ProcessPartialPayment(PlayerStats ps)
        {
            float remaining = ps.Gold.Value;

            // 월세 (250,000) 최우선
            if (remaining >= 250000f)
            {
                ps.Gold.Modify(-250000f);
                remaining -= 250000f;
                RentUnpaidCount = 0;
            }
            else
            {
                RentUnpaidCount++;
                HandleRentUnpaid();
            }

            // 나머지 고정비는 있는 만큼 차감
            float otherExpenses = GetTotalMonthlyExpense() - 250000f;
            float payable = Mathf.Min(remaining, otherExpenses);
            ps.Gold.Modify(-payable);
        }

        private void HandleRentUnpaid()
        {
            var ps = PlayerStats.Instance;

            switch (RentUnpaidCount)
            {
                case 1:
                    ps.SAN.Modify(-3f);
                    Debug.Log("[Economy] 월세 미납 1회: 문자 독촉");
                    break;
                case 2:
                    ps.SAN.Modify(-5f);
                    Debug.Log("[Economy] 월세 미납 2회: 전화 독촉, 매일 SAN -2");
                    break;
                case 3:
                    // 강제 퇴거 엔딩
                    EventBus.Publish(new EndingTriggeredEvent
                    {
                        EndingId = "forced_eviction",
                        EndingName = "강제 퇴거"
                    });
                    break;
            }
        }

        /// <summary>
        /// 총 월 고정지출 계산.
        /// </summary>
        public float GetTotalMonthlyExpense()
        {
            float total = 1309500f; // 기본 고정지출

            if (VRInstallmentMonths > 0)
                total += 44000f; // VR 할부

            if (ApplianceInstallmentMonths <= 0)
                total -= 347000f; // 가전 할부 완납

            return total;
        }

        /// <summary>
        /// VR 구매 처리.
        /// </summary>
        public void PurchaseVR()
        {
            VRInstallmentMonths = 10;
            PlayerStats.Instance.OwnsVR = true;
        }
    }
}
