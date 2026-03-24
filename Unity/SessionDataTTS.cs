using System.Collections.Generic;

// TTS 순차 재생을 위한 세션 데이터 구조체
// seq(순서) 기반으로 상태를 관리하여 병렬 생성 + 순차 재생을 구현
public struct SessionDataTTS
{
    // 세션 격리용
    public int sessionId;      // 대화 시작 시 증가
    public int chatIdxNum;     // 풍선 기준 대화 번호

    // 순서 제어
    public int nextSeqToPlay;      // 다음 재생 대상
    public int nextSeqToAllocate;  // 새 문장 등록 시 seq 부여

    // seq별 데이터
    public Dictionary<int, string> textBySeq;          // 확정된 재생 텍스트
    public Dictionary<int, byte[]> wavBySeq;           // TTS 성공 결과
    public Dictionary<int, string> stateBySeq;         // "pending","in_flight","ready","failed","skipped","played"
    public Dictionary<int, float> waitStartTimeBySeq;  // 내 차례에서 대기 시작 시간

    // 세션 초기화
    public void Reset(int newSessionId, int newChatIdxNum)
    {
        sessionId = newSessionId;
        chatIdxNum = newChatIdxNum;

        nextSeqToPlay = 0;
        nextSeqToAllocate = 0;

        textBySeq = new Dictionary<int, string>();
        wavBySeq = new Dictionary<int, byte[]>();
        stateBySeq = new Dictionary<int, string>();
        waitStartTimeBySeq = new Dictionary<int, float>();
    }
}
