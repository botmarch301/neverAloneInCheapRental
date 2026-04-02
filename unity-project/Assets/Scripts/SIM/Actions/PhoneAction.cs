using NAICR.Core;
using NAICR.Stats;

namespace NAICR.SIM.Actions
{
    /// <summary>
    /// 핸드폰 사용 (시간떼우기). HP +2, SAN +2.
    /// 쇼핑/배달은 별도 UI에서 처리.
    /// </summary>
    public class PhoneAction : GameAction
    {
        public override string Id => "phone";
        public override string DisplayName => "핸드폰";
        public override int TurnCost => 1;

        public override bool CanPerform(TimeOfDay timeOfDay)
        {
            // 아침 제외 모든 시간대
            return timeOfDay != TimeOfDay.Morning && timeOfDay != TimeOfDay.Midnight;
        }

        public override void Perform()
        {
            PlayerStats.Instance.HP.Modify(2f);
            PlayerStats.Instance.SAN.Modify(2f);
        }
    }
}
