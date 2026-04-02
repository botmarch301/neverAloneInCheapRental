using System;
using System.Collections;
using UnityEngine;

namespace NAICR.VN
{
    /// <summary>
    /// 대사 재생 시스템. 타이핑 효과, 스킵, 오토 지원.
    /// </summary>
    public class DialogueSystem : MonoBehaviour
    {
        public static DialogueSystem Instance { get; private set; }

        [Header("Settings")]
        [SerializeField] private float _typingSpeed = 0.03f;   // 글자당 초
        [SerializeField] private float _autoDelay = 2.0f;      // 오토 모드 대기 시간
        [SerializeField] private bool _autoMode = false;
        [SerializeField] private bool _skipMode = false;

        // ── 상태 ──
        private DialogueScript _currentScript;
        private int _currentLineIndex;
        private bool _isTyping;
        private bool _isWaitingForInput;
        private string _fullText;
        private Coroutine _typingCoroutine;

        // ── 이벤트 (UI에서 구독) ──
        public event Action<string, string> OnLineDisplay;      // (speaker, text)
        public event Action<DialogueChoice[]> OnChoicesDisplay;  // 선택지 표시
        public event Action<string> OnEffectTrigger;             // 연출 효과
        public event Action<string> OnSpriteChange;              // 스프라이트 변경
        public event Action<string> OnBackgroundChange;          // 배경 변경
        public event Action<string> OnBGMChange;                 // BGM 변경
        public event Action<string> OnSFXPlay;                   // 효과음
        public event Action OnDialogueEnd;                       // 대사 종료

        // ── Properties ──
        public bool IsPlaying => _currentScript != null;
        public bool AutoMode { get => _autoMode; set => _autoMode = value; }
        public bool SkipMode { get => _skipMode; set => _skipMode = value; }
        public float TypingSpeed { get => _typingSpeed; set => _typingSpeed = value; }
        public float AutoDelay { get => _autoDelay; set => _autoDelay = value; }

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
        }

        // ── 재생 제어 ──

        /// <summary>
        /// 스크립트 ID로 대사 시작.
        /// </summary>
        public void PlayScript(string scriptId)
        {
            var script = ScriptParser.Instance.GetScript(scriptId);
            if (script == null)
            {
                Debug.LogWarning($"[Dialogue] 스크립트 없음: {scriptId}");
                return;
            }
            PlayScript(script);
        }

        /// <summary>
        /// 스크립트 객체로 대사 시작.
        /// </summary>
        public void PlayScript(DialogueScript script)
        {
            _currentScript = script;
            _currentLineIndex = 0;
            _skipMode = false;
            DisplayCurrentLine();
        }

        /// <summary>
        /// 다음으로 진행 (클릭/Enter).
        /// </summary>
        public void Advance()
        {
            if (_isTyping)
            {
                // 타이핑 중이면 즉시 완성
                CompleteTyping();
                return;
            }

            if (_isWaitingForInput)
            {
                _isWaitingForInput = false;
                _currentLineIndex++;
                DisplayCurrentLine();
            }
        }

        /// <summary>
        /// 선택지 선택.
        /// </summary>
        public void SelectChoice(int index)
        {
            if (_currentScript == null) return;
            var line = _currentScript.lines[_currentLineIndex];
            if (line.choices == null || index >= line.choices.Length) return;

            var choice = line.choices[index];

            // 선택 효과 적용
            if (choice.effects != null)
            {
                foreach (var effect in choice.effects)
                {
                    ApplyEffect(effect);
                }
            }

            // 다음 스크립트로 이동
            if (!string.IsNullOrEmpty(choice.next))
            {
                PlayScript(choice.next);
            }
            else
            {
                _currentLineIndex++;
                DisplayCurrentLine();
            }
        }

        // ── 내부 ──

        private void DisplayCurrentLine()
        {
            if (_currentScript == null) return;

            if (_currentLineIndex >= _currentScript.lines.Length)
            {
                EndDialogue();
                return;
            }

            var line = _currentScript.lines[_currentLineIndex];

            // 조건 체크
            if (line.condition != null && !EvaluateCondition(line.condition))
            {
                _currentLineIndex++;
                DisplayCurrentLine();
                return;
            }

            // 연출 효과
            if (!string.IsNullOrEmpty(line.effect))
                OnEffectTrigger?.Invoke(line.effect);
            if (!string.IsNullOrEmpty(line.background))
                OnBackgroundChange?.Invoke(line.background);
            if (!string.IsNullOrEmpty(line.spriteChange))
                OnSpriteChange?.Invoke(line.spriteChange);
            if (!string.IsNullOrEmpty(line.bgm))
                OnBGMChange?.Invoke(line.bgm);
            if (!string.IsNullOrEmpty(line.sfx))
                OnSFXPlay?.Invoke(line.sfx);

            // 선택지가 있으면
            if (line.choices != null && line.choices.Length > 0)
            {
                OnLineDisplay?.Invoke(line.speaker, line.text);
                OnChoicesDisplay?.Invoke(line.choices);
                return;
            }

            // 점프
            if (!string.IsNullOrEmpty(line.next))
            {
                OnLineDisplay?.Invoke(line.speaker, line.text);
                _isWaitingForInput = true;

                // 점프는 입력 후 실행
                StartCoroutine(JumpAfterInput(line.next));
                return;
            }

            // 일반 대사: 타이핑 효과
            if (_skipMode)
            {
                OnLineDisplay?.Invoke(line.speaker, line.text);
                _currentLineIndex++;
                StartCoroutine(SkipDelay());
            }
            else
            {
                StartTyping(line.speaker, line.text);
            }
        }

        private void StartTyping(string speaker, string text)
        {
            _fullText = text;
            _isTyping = true;

            if (_typingCoroutine != null)
                StopCoroutine(_typingCoroutine);

            _typingCoroutine = StartCoroutine(TypeText(speaker, text));
        }

        private IEnumerator TypeText(string speaker, string text)
        {
            for (int i = 1; i <= text.Length; i++)
            {
                OnLineDisplay?.Invoke(speaker, text[..i]);
                yield return new WaitForSeconds(_typingSpeed);
            }

            _isTyping = false;
            _isWaitingForInput = true;

            if (_autoMode)
            {
                yield return new WaitForSeconds(_autoDelay);
                if (_isWaitingForInput)
                {
                    Advance();
                }
            }
        }

        private void CompleteTyping()
        {
            if (_typingCoroutine != null)
                StopCoroutine(_typingCoroutine);

            _isTyping = false;
            _isWaitingForInput = true;

            var line = _currentScript.lines[_currentLineIndex];
            OnLineDisplay?.Invoke(line.speaker, line.text);

            if (_autoMode)
            {
                StartCoroutine(AutoAdvance());
            }
        }

        private IEnumerator AutoAdvance()
        {
            yield return new WaitForSeconds(_autoDelay);
            if (_isWaitingForInput)
            {
                Advance();
            }
        }

        private IEnumerator SkipDelay()
        {
            yield return new WaitForSeconds(0.02f);
            DisplayCurrentLine();
        }

        private IEnumerator JumpAfterInput(string nextId)
        {
            while (_isWaitingForInput)
                yield return null;

            PlayScript(nextId);
        }

        private void EndDialogue()
        {
            _currentScript = null;
            _currentLineIndex = 0;
            _isTyping = false;
            _isWaitingForInput = false;
            OnDialogueEnd?.Invoke();
        }

        // ── 조건/효과 평가 ──

        private bool EvaluateCondition(DialogueCondition cond)
        {
            // TODO: StatSystem 연동
            // 임시 구현: 항상 true
            return true;
        }

        private void ApplyEffect(DialogueEffect effect)
        {
            // TODO: StatSystem 연동
            Debug.Log($"[Dialogue] Effect: {effect.type} {effect.target} {effect.value}");
        }
    }
}
