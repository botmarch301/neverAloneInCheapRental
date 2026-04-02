using NAICR.Core;
using NAICR.Stats;

namespace NAICR.SIM.Actions
{
    public class CleanAction : GameAction
    {
        public override string Id => "clean";
        public override string DisplayName => "청소";
        public override int TurnCost => 1;

        public override bool CanPerform(TimeOfDay timeOfDay)
        {
            return timeOfDay == TimeOfDay.Evening;
        }

        public override void Perform()
        {
            // 청소 자체의 스탯 효과는 미미하지만, 집 상태에 영향
            // 추후 집 상태 시스템 연동
        }
    }
}
