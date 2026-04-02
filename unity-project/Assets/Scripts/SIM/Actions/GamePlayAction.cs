using UnityEngine;
using NAICR.Core;
using NAICR.Stats;

namespace NAICR.SIM.Actions
{
    public class GamePlayAction : GameAction
    {
        public override string Id => "game";
        public override string DisplayName => "게임";
        public override int TurnCost => 1; // 몰입하면 2턴, 기본 1턴

        public override bool CanPerform(TimeOfDay timeOfDay)
        {
            return timeOfDay == TimeOfDay.Night;
        }

        public override void Perform()
        {
            PlayerStats.Instance.SAN.Modify(Random.Range(5f, 8f));
        }
    }
}
