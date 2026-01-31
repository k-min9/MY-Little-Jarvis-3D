using System;
using System.Collections.Generic;

// 다중 캐릭터 대화 시스템 타입 정의
namespace ApiGeminiMulti
{
    // 대화 참여자 정보
    [Serializable]
    public class MultiParticipant
    {
        public string name;           // 시스템 내부 이름 (sensei, arona, plana)
        public string type;           // "user" | "ai"
        public string display_name;   // 표시 이름 (선생님, 아로나, 프라나)
        public string character_file; // 캐릭터 프롬프트 파일명
    }

    // API 호출 요청 파라미터
    public class MultiConversationRequest
    {
        public string query;                    // 사용자 입력 메시지
        public string currentSpeaker;           // 현재 발화자
        public string targetSpeaker;            // 답변할 캐릭터
        public string targetListener;           // 대화 대상 ("all", "sensei", "arona", "plana")
        public List<MultiParticipant> participants;  // 참여자 목록
        public List<Dictionary<string, string>> memoryList;  // 대화 기록
        public string aiLanguage;               // 언어 코드 ("ko", "ja", "en")
        public string chatIdx;                  // 대화 인덱스
        public List<string> guidelineList;      // 사용자 가이드라인
        public Dictionary<string, object> situationDict;  // 상황 설정
        public string playerName;               // 플레이어 이름
    }

    // 응답 결과
    public class MultiConversationResult
    {
        public List<string> sentences;    // 응답 문장 리스트
        public string speaker;            // 실제 응답한 캐릭터
        public string nextSpeaker;        // 다음 발화자
        public string reasoning;          // 판단 근거 (디버깅용)
        public bool isSuccess;            // 성공 여부
        public string errorMessage;       // 에러 메시지 (실패 시)
    }

    // 스트리밍 콜백 델리게이트
    public delegate void OnMultiChunkReceived(string sentence, string speaker, int sentenceIndex);
    public delegate void OnMultiStreamComplete(MultiConversationResult result);
    public delegate void OnMultiStreamError(string errorMessage);
}
