using UnityEngine;
using NAICR.Stats;

namespace NAICR.Core
{
    /// <summary>
    /// 게임 전체 상태 관리. 막(Act) 진행, 엔딩 체크.
    /// </summary>
    public class GameManager : MonoBehaviour
    {
        public static GameManager Instance { get; private set; }

        [Header("Game State")]
        [SerializeField] private int _currentAct = 1;
        [SerializeField] private bool _gameOver = false;

        public int CurrentAct => _currentAct;
        public bool IsGameOver => _gameOver;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }

        private void OnEnable()
        {
            EventBus.Subscribe<DayEndedEvent>(OnDayEnded);
            EventBus.Subscribe<EndingTriggeredEvent>(OnEndingTriggered);
        }

        private void OnDisable()
        {
            EventBus.Unsubscribe<DayEndedEvent>(OnDayEnded);
            EventBus.Unsubscribe<EndingTriggeredEvent>(OnEndingTriggered);
        }

        private void OnDayEnded(DayEndedEvent evt)
        {
            if (_gameOver) return;
            CheckEndingConditions();
        }

        // ── 막 전환 ──

        /// <summary>
        /// 막 전환 트리거. scenes.md 막 경계 기준.
        /// 1→2: VR 구입, 2→3: 벽 삽입→도망, 3→4: 첫 보름달 성교
        /// </summary>
        public void AdvanceAct()
        {
            int prev = _currentAct;
            _currentAct++;

            EventBus.Publish(new ActChangedEvent
            {
                PreviousAct = prev,
                NewAct = _currentAct
            });

            Debug.Log($"[GameManager] 막 전환: {prev}막 → {_currentAct}막");
        }

        // ── 엔딩 체크 ──

        private void CheckEndingConditions()
        {
            var ps = PlayerStats.Instance;
            if (ps == null) return;

            // 심장마비: HP ≤ 0
            if (ps.IsDeadHP)
            {
                TriggerEnding("heart_attack", "심장마비");
                return;
            }

            // SAN ≤ 0
            if (ps.IsInsaneSAN)
            {
                if (ps.HP.Value > 30f)
                {
                    TriggerEnding("insanity", "정신병");
                }
                else
                {
                    TriggerEnding("robbery_suicide", "강탈 → 자살");
                }
                return;
            }

            // 양기 고갈 (4막)
            if (_currentAct >= 4 && ps.IsQiDepleted)
            {
                TriggerEnding("qi_depletion", "양기 고갈");
                return;
            }

            // 트루/확장 트루 엔딩 체크
            var gs = GhostStats.Instance;
            if (gs != null && _currentAct >= 4)
            {
                if (gs.CanExtendedTrueEnd())
                {
                    TriggerEnding("extended_true", "확장 트루 (성불)");
                }
                else if (gs.CanAscend())
                {
                    TriggerEnding("true_end", "트루 (성불)");
                }
            }
        }

        private void TriggerEnding(string id, string name)
        {
            _gameOver = true;
            EventBus.Publish(new EndingTriggeredEvent
            {
                EndingId = id,
                EndingName = name
            });
            Debug.Log($"[GameManager] 엔딩 트리거: {name}");
        }

        private void OnEndingTriggered(EndingTriggeredEvent evt)
        {
            _gameOver = true;
            Debug.Log($"[GameManager] 엔딩: {evt.EndingName}");
            // TODO: 엔딩 씬 로드
        }
    }
}
