using UnityEngine;
using NAICR.Core;
using NAICR.Stats;

namespace NAICR.SIM.Actions
{
    public class MusicAction : GameAction
    {
        public override string Id => "music";
        public override string DisplayName => "음악 감상";
        public override int TurnCost => 1;

        public override bool CanPerform(TimeOfDay timeOfDay)
        {
            return timeOfDay == TimeOfDay.Night;
        }

        public override void Perform()
        {
            PlayerStats.Instance.SAN.Modify(Random.Range(3f, 5f));
        }
    }
}
