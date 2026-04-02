namespace NAICR.Data
{
    /// <summary>
    /// 게임 상수. stats-design.md 수치 기준.
    /// </summary>
    public static class GameConstants
    {
        // ── 시간 ──
        public const int DaysPerMoonCycle = 28;
        public const int FullMoonDay = 14;

        // ── HP ──
        public const float HP_Initial = 70f;
        public const float HP_Max = 100f;
        public const float HP_SleepRecoveryMin = 30f;
        public const float HP_SleepRecoveryMax = 40f;
        public const float HP_BedBonusMin = 5f;
        public const float HP_BedBonusMax = 10f;

        // ── SAN ──
        public const float SAN_Initial = 60f;
        public const float SAN_Max = 100f;
        public const float SAN_SleepRecoveryMin = 10f;
        public const float SAN_SleepRecoveryMax = 15f;

        // ── 포만감 ──
        public const float Satiety_Initial = 50f;
        public const float Satiety_OvereatThreshold = 90f;
        public const float Satiety_HungerThreshold = 20f;
        public const float Satiety_StarvationThreshold = 0f;

        // ── 양기 ──
        public const float Qi_Initial = 100f;
        public const float Qi_OptimalMin = 30f;
        public const float Qi_OptimalMax = 70f;
        public const float Qi_ExcessThreshold = 80f;
        public const float Qi_WetDreamThreshold = 80f;
        public const float Qi_NaturalDailyRecovery = 5f;

        // ── GOLD ──
        public const float Gold_Initial = 300000f;
        public const float Gold_DailyWage = 80000f;
        public const float Gold_OvertimeWage = 130000f;
        public const float Gold_WeekendWage = 130000f;
        public const float Gold_TransportCost = 5000f;
        public const float Gold_MonthlyFixedExpense = 1309500f;
        public const float Gold_Rent = 250000f;
        public const int Gold_RentEvictionThreshold = 3;

        // ── 호감도 ──
        public const float Affection_Initial = 0f;
        public const float Affection_DailyNatural = 0.5f;
        public const float Affection_GenitalThreshold = 70f;
        public const float Affection_ExtendedTrueThreshold = 90f;

        // ── 만족도 ──
        public const float Satisfaction_AscendThreshold = 85f;
        public const int Satisfaction_NeglectDays = 5;

        // ── 4막 호감도 비용 ──
        public const float Affection_NewPositionCostMin = 5f;
        public const float Affection_NewPositionCostMax = 10f;
        public const float Affection_SkipForeplayCostMin = 8f;
        public const float Affection_SkipForeplayCostMax = 15f;
        public const float Affection_RejectedZoneCostMin = 3f;
        public const float Affection_RejectedZoneCostMax = 5f;

        // ── VR ──
        public const float VR_Price = 400000f;
        public const float VR_MonthlyInstallment = 44000f;
        public const int VR_InstallmentMonths = 10;
    }
}
