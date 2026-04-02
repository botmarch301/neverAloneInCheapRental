using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace NAICR.VN
{
    /// <summary>
    /// 외부 JSON 대사 파일 로드 및 관리.
    /// Assets/Data/Dialogues/ 폴더의 JSON 파일을 로드.
    /// </summary>
    public class ScriptParser : MonoBehaviour
    {
        public static ScriptParser Instance { get; private set; }

        private readonly Dictionary<string, DialogueScript> _scripts = new();

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            LoadAllScripts();
        }

        /// <summary>
        /// 모든 대사 파일 로드.
        /// </summary>
        private void LoadAllScripts()
        {
            // Resources/Dialogues/ 폴더에서 로드
            var textAssets = Resources.LoadAll<TextAsset>("Dialogues");

            foreach (var asset in textAssets)
            {
                try
                {
                    var file = JsonUtility.FromJson<DialogueFile>(asset.text);
                    if (file?.scripts == null) continue;

                    foreach (var script in file.scripts)
                    {
                        if (!string.IsNullOrEmpty(script.id))
                        {
                            _scripts[script.id] = script;
                        }
                    }

                    Debug.Log($"[ScriptParser] 로드 완료: {asset.name} ({file.scripts.Length}개 스크립트)");
                }
                catch (System.Exception e)
                {
                    Debug.LogError($"[ScriptParser] 파싱 실패: {asset.name} - {e.Message}");
                }
            }

            Debug.Log($"[ScriptParser] 전체 {_scripts.Count}개 스크립트 로드됨");
        }

        /// <summary>
        /// ID로 스크립트 조회.
        /// </summary>
        public DialogueScript GetScript(string id)
        {
            if (_scripts.TryGetValue(id, out var script))
                return script;

            Debug.LogWarning($"[ScriptParser] 스크립트 미발견: {id}");
            return null;
        }

        /// <summary>
        /// 트리거 조건으로 스크립트 검색.
        /// </summary>
        public DialogueScript GetScriptByTrigger(string trigger, int act = -1)
        {
            foreach (var kvp in _scripts)
            {
                var s = kvp.Value;
                if (s.trigger == trigger && (act < 0 || s.act == act))
                    return s;
            }
            return null;
        }

        /// <summary>
        /// 특정 막의 모든 스크립트 목록.
        /// </summary>
        public List<DialogueScript> GetScriptsByAct(int act)
        {
            var result = new List<DialogueScript>();
            foreach (var kvp in _scripts)
            {
                if (kvp.Value.act == act)
                    result.Add(kvp.Value);
            }
            return result;
        }

        /// <summary>
        /// 런타임에 스크립트 추가 (모딩 지원용).
        /// </summary>
        public void AddScript(DialogueScript script)
        {
            if (!string.IsNullOrEmpty(script.id))
            {
                _scripts[script.id] = script;
            }
        }

        /// <summary>
        /// 런타임에 외부 JSON 파일 로드 (모딩 지원용).
        /// </summary>
        public void LoadFromExternalFile(string path)
        {
            if (!File.Exists(path))
            {
                Debug.LogError($"[ScriptParser] 파일 없음: {path}");
                return;
            }

            string json = File.ReadAllText(path);
            var file = JsonUtility.FromJson<DialogueFile>(json);

            if (file?.scripts == null) return;

            foreach (var script in file.scripts)
            {
                AddScript(script);
            }

            Debug.Log($"[ScriptParser] 외부 파일 로드: {path} ({file.scripts.Length}개)");
        }
    }
}
