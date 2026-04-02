using NAICR.Core;
using NAICR.Stats;

namespace NAICR.SIM.Actions
{
    /// <summary>
    /// 식사 행동. 음식 종류에 따라 HP/포만감/양기/GOLD 변동.
    /// </summary>
    public class EatAction : GameAction
    {
        public override string Id => "eat";
        public override string DisplayName => "식사";
        public override int TurnCost => 1;

        public override bool CanPerform(TimeOfDay timeOfDay)
        {
            // 아침, 저녁에 식사 가능
            return timeOfDay == TimeOfDay.Morning || timeOfDay == TimeOfDay.Evening;
        }

        public override bool MeetsRequirements()
        {
            // 포만감 90 이상이면 식사 불가
            return PlayerStats.Instance.Satiety.Value < 90f;
        }

        public override void Perform()
        {
            // 기본: 냉동식품 (가장 저렴한 선택)
            // 실제로는 UI에서 음식 선택 후 ApplyFood 호출
            ApplyFood(FoodType.FrozenFood);
        }

        public enum FoodType
        {
            FrozenFood,     // 냉동식품
            ConvenienceBento, // 편의점 도시락
            DeliveryNormal, // 배달 (일반)
            DeliveryBoyang, // 배달 (보양식)
            EnergyDrink,    // 에너지 드링크
            ColaSoda,       // 콜라/사이다
            CannedCoffee,   // 캔 커피
            Alcohol         // 술
        }

        public static void ApplyFood(FoodType type)
        {
            var ps = PlayerStats.Instance;

            switch (type)
            {
                case FoodType.FrozenFood:
                    ps.HP.Modify(4f);
                    ps.Satiety.Modify(30f);
                    ps.Qi.Modify(1f);
                    ps.Gold.Modify(-2500f);
                    ps.ConsecutiveFrozenFoodDays++;
                    if (ps.ConsecutiveFrozenFoodDays >= 3)
                    {
                        // 배탈: HP -10, 포만감 강제 0
                        ps.HP.Modify(-10f);
                        ps.Satiety.Set(0f);
                        ps.ConsecutiveFrozenFoodDays = 0;
                    }
                    break;

                case FoodType.ConvenienceBento:
                    ps.HP.Modify(5f);
                    ps.Satiety.Modify(35f);
                    ps.Qi.Modify(1f);
                    ps.Gold.Modify(-4000f);
                    ps.ConsecutiveFrozenFoodDays = 0;
                    break;

                case FoodType.DeliveryNormal:
                    ps.HP.Modify(8f);
                    ps.Satiety.Modify(40f);
                    ps.Qi.Modify(1f);
                    ps.Gold.Modify(-12000f);
                    ps.ConsecutiveFrozenFoodDays = 0;
                    break;

                case FoodType.DeliveryBoyang:
                    ps.HP.Modify(12f);
                    ps.Satiety.Modify(45f);
                    ps.Qi.Modify(3f);
                    ps.Gold.Modify(-25000f);
                    ps.ConsecutiveFrozenFoodDays = 0;
                    break;

                case FoodType.EnergyDrink:
                    ps.HP.Modify(5f);
                    ps.Satiety.Modify(5f);
                    ps.Gold.Modify(-2500f);
                    break;

                case FoodType.ColaSoda:
                    ps.HP.Modify(1f);
                    ps.SAN.Modify(1f);
                    ps.Satiety.Modify(10f);
                    ps.Gold.Modify(-1500f);
                    break;

                case FoodType.CannedCoffee:
                    ps.HP.Modify(2f);
                    ps.SAN.Modify(2f);
                    ps.Satiety.Modify(5f);
                    ps.Gold.Modify(-1500f);
                    break;

                case FoodType.Alcohol:
                    ps.SAN.Modify(5f);
                    ps.Satiety.Modify(10f);
                    ps.Gold.Modify(-3000f);
                    // 숙취: 다음 날 HP -5 (DayEnded에서 처리)
                    break;
            }
        }
    }
}
