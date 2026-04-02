using System;
using System.Collections.Generic;
using UnityEngine;
using NAICR.Core;

namespace NAICR.SIM
{
    /// <summary>
    /// 행동 기반 클래스. 모든 SIM 행동은 이것을 상속.
    /// </summary>
    [Serializable]
    public abstract class GameAction
    {
        public abstract string Id { get; }
        public abstract string DisplayName { get; }
        public abstract int TurnCost { get; }

        /// <summary>
        /// 이 행동을 현재 시간대에 수행할 수 있는지.
        /// </summary>
        public abstract bool CanPerform(TimeOfDay timeOfDay);

        /// <summary>
        /// 행동 실행. 스탯 변동 등 처리.
        /// </summary>
        public abstract void Perform();

        /// <summary>
        /// 추가 조건 (소지금, 아이템 보유 등).
        /// </summary>
        public virtual bool MeetsRequirements() => true;
    }

    /// <summary>
    /// 행동 관리 시스템. 현재 시간대에 가능한 행동 목록 제공, 실행 처리.
    /// </summary>
    public class ActionSystem : MonoBehaviour
    {
        public static ActionSystem Instance { get; private set; }

        private readonly List<GameAction> _registeredActions = new();

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;

            RegisterAllActions();
        }

        private void RegisterAllActions()
        {
            // 기본 행동 등록
            _registeredActions.Add(new Actions.EatAction());
            _registeredActions.Add(new Actions.WorkAction());
            _registeredActions.Add(new Actions.PhoneAction());
            _registeredActions.Add(new Actions.CleanAction());
            _registeredActions.Add(new Actions.GamePlayAction());
            _registeredActions.Add(new Actions.MusicAction());
            _registeredActions.Add(new Actions.MeditateAction());
            _registeredActions.Add(new Actions.VRAction());
        }

        /// <summary>
        /// 현재 시간대에 수행 가능한 행동 목록.
        /// </summary>
        public List<GameAction> GetAvailableActions()
        {
            var timeOfDay = TimeManager.Instance.CurrentTimeOfDay;
            var remainingTurns = TimeManager.Instance.GetRemainingTurns();
            var available = new List<GameAction>();

            foreach (var action in _registeredActions)
            {
                if (action.CanPerform(timeOfDay)
                    && action.TurnCost <= remainingTurns
                    && action.MeetsRequirements())
                {
                    available.Add(action);
                }
            }

            return available;
        }

        /// <summary>
        /// 행동을 실행한다.
        /// </summary>
        public bool PerformAction(GameAction action)
        {
            var timeOfDay = TimeManager.Instance.CurrentTimeOfDay;

            if (!action.CanPerform(timeOfDay))
            {
                Debug.LogWarning($"[ActionSystem] {action.DisplayName}: 현재 시간대에 수행 불가");
                return false;
            }

            if (action.TurnCost > TimeManager.Instance.GetRemainingTurns())
            {
                Debug.LogWarning($"[ActionSystem] {action.DisplayName}: 턴 부족");
                return false;
            }

            if (!action.MeetsRequirements())
            {
                Debug.LogWarning($"[ActionSystem] {action.DisplayName}: 조건 미충족");
                return false;
            }

            action.Perform();
            TimeManager.Instance.ConsumeTurns(action.TurnCost);

            EventBus.Publish(new ActionPerformedEvent
            {
                ActionId = action.Id,
                TurnsCost = action.TurnCost
            });

            return true;
        }
    }
}
