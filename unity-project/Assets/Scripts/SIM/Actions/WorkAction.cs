using UnityEngine;
using NAICR.Core;
using NAICR.Stats;

namespace NAICR.SIM.Actions
{
    /// <summary>
    /// 출근 행동. 3턴 소비, 야근 시 +2턴.
    /// </summary>
    public class WorkAction : GameAction
    {
        public override string Id => "work";
        public override string DisplayName => "출근";
        public override int TurnCost => 3;

        public override bool CanPerform(TimeOfDay timeOfDay)
        {
            return timeOfDay == TimeOfDay.Morning;
        }

        public override void Perform()
        {
            var ps = PlayerStats.Instance;

            ps.HP.Modify(-10f);
            ps.SAN.Modify(-Random.Range(3f, 5f));
            ps.Gold.Modify(-5000f); // 교통비

            // 주말 판단 (7일 주기, 6~7일째 = 주말)
            int dayOfWeek = (TimeManager.Instance.CurrentDay - 1) % 7;
            bool isWeekend = dayOfWeek >= 5;

            if (isWeekend)
            {
                ps.Gold.Modify(130000f); // 주말 일급 1.5배
            }
            else
            {
                ps.Gold.Modify(80000f); // 평일 일급
            }

            // 50% 확률로 랜덤 이벤트 (WorkEventSystem에서 처리)
            EventBus.Publish(new ActionPerformedEvent
            {
                ActionId = "work_event_check",
                TurnsCost = 0
            });
        }
    }

    /// <summary>
    /// 야근 행동. 출근 후 추가 발생. 2턴 추가 소비.
    /// </summary>
    public class OvertimeAction : GameAction
    {
        public override string Id => "overtime";
        public override string DisplayName => "야근";
        public override int TurnCost => 2;

        public override bool CanPerform(TimeOfDay timeOfDay)
        {
            return timeOfDay == TimeOfDay.Evening;
        }

        public override void Perform()
        {
            var ps = PlayerStats.Instance;

            ps.HP.Modify(-15f);
            ps.SAN.Modify(-8f);

            // 야근수당: 평일 기준 130,000 - 80,000 = 50,000 추가
            ps.Gold.Modify(50000f);
        }
    }
}
