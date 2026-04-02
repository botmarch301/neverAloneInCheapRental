using UnityEngine;
using NAICR.Core;
using NAICR.Stats;

namespace NAICR.SIM.Actions
{
    public class MeditateAction : GameAction
    {
        public override string Id => "meditate";
        public override string DisplayName => "명상";
        public override int TurnCost => 1;

        public override bool CanPerform(TimeOfDay timeOfDay)
        {
            return timeOfDay == TimeOfDay.Night;
        }

        public override void Perform()
        {
            PlayerStats.Instance.SAN.Modify(Random.Range(8f, 12f));
        }
    }
}
