using UnityEngine;
using NAICR.Core;
using NAICR.Stats;

namespace NAICR.SIM.Actions
{
    /// <summary>
    /// VR 자위. SAN +3, 양기 -3~-4. VR 소유 필요.
    /// </summary>
    public class VRAction : GameAction
    {
        public override string Id => "vr";
        public override string DisplayName => "VR";
        public override int TurnCost => 1;

        public override bool CanPerform(TimeOfDay timeOfDay)
        {
            return timeOfDay == TimeOfDay.Night;
        }

        public override bool MeetsRequirements()
        {
            return PlayerStats.Instance.OwnsVR;
        }

        public override void Perform()
        {
            var ps = PlayerStats.Instance;
            ps.SAN.Modify(3f);
            ps.Qi.Modify(-Random.Range(3f, 4f));
        }
    }
}
