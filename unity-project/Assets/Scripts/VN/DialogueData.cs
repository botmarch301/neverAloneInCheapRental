using System;
using System.Collections.Generic;

namespace NAICR.VN
{
    /// <summary>
    /// 대사 데이터 구조. JSON으로 직렬화/역직렬화.
    /// 외부 파일에서 로드하여 코드 수정 없이 대사 추가/수정/삭제 가능.
    /// </summary>
    [Serializable]
    public class DialogueScript
    {
        public string id;              // 고유 ID (예: "act1_first_night_01")
        public int act;                // 소속 막 (1~4)
        public string trigger;         // 트리거 조건 (이벤트 ID)
        public DialogueLine[] lines;   // 대사 라인 목록
    }

    [Serializable]
    public class DialogueLine
    {
        public string speaker;         // 화자 (null = 나레이션/내면 독백)
        public string text;            // 대사 텍스트
        public string emotion;         // 캐릭터 표정 (happy, sad, scared 등)
        public string effect;          // 연출 효과 (screen_shake, fade_black 등)
        public float duration;         // 연출 지속 시간 (초)
        public string bgm;             // BGM 변경 (null = 유지)
        public string sfx;             // 효과음
        public string background;      // 배경 변경 (null = 유지)
        public string spriteChange;    // 캐릭터 스프라이트 변경
        public DialogueChoice[] choices; // 선택지 (null이면 다음 라인으로)
        public string next;            // 다음 스크립트 ID로 점프 (null이면 순차)
        public DialogueCondition condition; // 조건부 표시
    }

    [Serializable]
    public class DialogueChoice
    {
        public string text;            // 선택지 텍스트
        public string next;            // 선택 시 이동할 스크립트 ID
        public DialogueCondition condition; // 선택지 표시 조건
        public DialogueEffect[] effects;   // 선택 시 효과 (스탯 변동 등)
    }

    [Serializable]
    public class DialogueCondition
    {
        public string stat;            // 체크할 스탯 이름
        public string op;              // 연산자: >=, <=, ==, >, <
        public float value;            // 비교값
    }

    [Serializable]
    public class DialogueEffect
    {
        public string type;            // "stat_modify", "flag_set", "item_give" 등
        public string target;          // 대상 (스탯 이름, 플래그 이름 등)
        public float value;            // 변동량
    }

    /// <summary>
    /// 대사 파일 전체 (JSON 루트).
    /// </summary>
    [Serializable]
    public class DialogueFile
    {
        public DialogueScript[] scripts;
    }
}
