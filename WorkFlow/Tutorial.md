# Tutorial

```markdown
  [A00|entry_condition] 시작 진입 조건: 서버설정완료 == false
    │
    └─ [A00|entry_condition|1] "선생님, 안녕하세요!"

  [A01|free_server_offer] 무료 서버 재시도 여부 == true
    │
    ├─ [A01|free_server_offer|1] "전에 무료 서버를 사용하려고 하셨던 것 같아요."
    │
    ├─ [A01|free_server_offer|2] "다시 연결해볼까요?"
    │
    ├─ <응, 다시 해보자>
    │  └─ [A01-1|connect_try]
    │      ├─ [A01-1-1|connect_success|1] "성공적으로 연결되었어요, 선생님."
    │      ├─ [A01-1-1|connect_success|2] "다만, 무료 서버는 응답 속도가 느리거나 다시 요청해야 될 수도 있어요."
    │      ├─ [A01-1-1|connect_success|3] "대화가 자연스럽지 않거나, 힘드시면 다른 방법을 시도해주세요."
    │      └─ → [A99]
    │
    │      ├─ [A01-1-2|connect_failed|1] "연결에 실패했어요, 선생님."
    │      ├─ [A01-1-2|connect_failed|2] "무료 서버는 가끔 연결이 불안정할 수 있어요. 계속 시도해볼까요?"
    │      │
    │      ├─ <다시 시도할게>
    │      │  └─ [A01-1-2-1|connect_retry]
    │      │      └─ [A01-1-2-1|connect_retry|1] "다시 연결을 시도해볼게요."
    │      │
    │      ├─ <나중에 할래>
    │      │  └─ [A01-1-2-2|connect_pending]
    │      │      └─ [A01-1-2-2|connect_pending|1] "네. 원하실 때 언제든지 다시 시도하실 수 있어요."
    │      │         → 무료서버의향=true 저장
    │      │
    │      └─ <그만둘래>
    │         └─ [A01-1-2-3|connect_refuse]
    │             └─ [A01-1-2-3|connect_refuse|1] "네. 그러면..."
    │                → 무료서버의향=false 저장 → [A02]
    │
    └─ <아니, 안할래>
      └─ [A01-2|refuse_free]
          └─ [A01-2-1|connect_refuse|1] "네. 그러면..."
              → 무료서버의향=false 저장 → [A02]

  [A02|platform_check] 플랫폼 확인
    │
    ├─ [A02|platform_check|1] "저와 대화하시려면 먼저 환경 설정이 필요해요."
    ├─ [A02|platform_check|2] "제가 대화 전 세팅을 도와드릴게요."
    ├─ [A02|platform_check|3] "지금 접속하신 기기가 PC는 아닌 것 같은데, 맞으실까요?"
    │
    ├─ <응, 맞아>
    │  └─ [A02-1|check_server_status]
    │      ├─ [A02-1|check_server_status|1] "확인해주셔서 고마워요, 선생님."
    │      ├─ [A02-1|check_server_status|2] "일단 지금 AI 서버가 실행된 PC 정보가 있으실까요?"
    │      │
    │      ├─ <연결할 PC가 있어>
    │      │  └─ [A02-1-1|pc_id_input]
    │      │      └─ [A02-1-1|pc_id_input|1] "좋아요! 그럼 연결할 ID를 입력해주시면 바로 설정할게요."
    │      │          ├─ <ID를 입력한다.> → [A97]
    │      │          └─ <전 선택지로> → [check_server_status] 선택지로 돌아가기
    │      │
    │      ├─ <외부 플랫폼을 사용하려고 해> → [A04]
    │      └─ <자세히 설명해줄 수 있어?>
    │         └─ [A02-1-2|help_explain]
    │             ├─ [A02-1-2|help_explain|1] "저를 다운로드 받은 곳에서 PC 버전을 다운로드 받으실 수 있어요."
    │             ├─ [A02-1-2|help_explain|2] "서버프로그램(`server.exe`) 실행 시 입력한 ID를 제게도 입력해주시면 연결돼요."
    │             └─ [A02-1-2|help_explain|3] "자세한 내용은 M9Dev 유튜브 채널에서도 확인하실 수 있어요."
    │
    ├─ <아니, PC 맞아>
    │  └─ [A02-2|pc_confirmed]
    │      └─ [A02-2-1|pc_confirmed|1] "확인 감사합니다, 선생님." → [A03]
    │
    └─ <필요 없어>
      └─ [A02-3|refuse_platform_check]
          └─ [A02-3-1|refuse_platform_check|1] "언제든 저와 이야기하고 싶으실 때 불러주세요, 선생님!"

  [A03|inference_select] 연산 방식 선택
    │
    ├─ [A03|inference_select|1] "AI가 작동하려면 먼저 서버 설정이 필요해요."
    ├─ [A03|inference_select|2] "지금 사용 중인 이 PC로 연산하는 것도 가능해요."
    │
    ├─ <이 PC에서 연산할래>
    │  └─ [A03-1|local_compute]
    │      └─ [A03-1|local_compute|1] "확인해볼게요... CUDA 환경 지원 여부를 검사 중이에요."
    │      ├─ [A03-1-1|cuda_supported]
    │      │   └─ [A03-1-1|cuda_supported|1] "CUDA 환경이 감지되었어요."
    │      │   └─ [A03-1-1|cuda_supported|2] "선생님의 PC에서 GPU를 사용할 수 있어요. 더 빠른 응답을 원하신다면 좋은 선택이에요. 어떻게 하실래요?"
    │      │       ├─ <GPU를 사용> → [A99]
    │      │       ├─ <CPU만으로 사용> → [A99]
    │      │       └─ <취소할래> → [A98]
    │      └─ [A03-1-2|cuda_not_supported]
    │          └─ [A03-1-2|cuda_not_supported|1] "CUDA 환경을 찾지 못했어요."
    │          └─ [A03-1-2|cuda_not_supported|2] "아쉽게도 GPU는 지원되지 않지만, CPU로 작동하는 건 가능해요. 이어서 진행하실까요?"
    │              ├─ <진행할게> → [A99]
    │              └─ <취소할래> → [A98]
    │
    ├─ <외부 서버를 사용할래> → [A04]
    └─ <그만둘래> → [A98]

  [A04|external_server_select] 외부 서버 방식 선택
    │
    ├─ [A04|external_server_select|1] "그러면 외부 서버와 연결해볼게요."
    ├─ [A04|external_server_select|2] "무료 서버를 이용하시거나, API 키를 입력해서 외부 플랫폼(Gemini, ChatGPT...)과 연결하실 수 있어요."
    │
    ├─ <무료 서버 연결>
    │  └─ [A04-1|free_server_external]
    │      ├─ 연결 성공 → [A99]
    │      └─ 연결 실패
    │          └─ [A04-1-1|connect_failed|1] "연결이 잘 되지 않았어요. 다시 시도해볼까요?"
    │              ├─ <다시 시도> → 루프
    │              ├─ <나중에 할래> → "네. 언제든지 말씀만 해주시면 다시 시도해볼게요, 선생님." 무료서버의향=true 저장 [종료]
    │              └─ <그만둘래> → [A98]
    │
    ├─ <API 키 입력할래>
    │  └─ [A04-2|api_key_input]
    │      └─ [A04-2|api_key_input|1] "API KEY 관련 모델을 골라주세요"
    │          ├─ <ChatGPT> → [A97]
    │          ├─ <Gemini> → [A97]
    │          ├─ <OpenRouter> → [A97]
    │          └─ <전 선택지로> -> [A03] 선택지
    └─ <설정 취소할래> → [A98]

  [A97|connect_test] ID 연결 시도
    │
    ├─ [A97|connect_test|1] "연결을 시도하고 있어요..."
    ├─ [A97|connect_test|2] "성공했어요 선생님"
    ├─ [A97|connect_test|3] "실패했어요 선생님. 다시 시도해볼까요?"
    │   ├─ <다시 시도> → 루프
    │   └─ <다음에 해볼게> → [A98]

  [A98|config_cancel] 설정 취소
    ├─ [A98|config_cancel|1] "알겠어요 선생님."
    └─ [A98|config_cancel|2] "필요하실 땐 언제든지 다시 설정하실 수 있어요."

  [A99|config_end] 설정 완료
    ├─ [A99|config_end|1] "설정이 완료되었어요, 선생님!"
    ├─ [A99|config_end|2] "필요하실 땐 언제든지 다시 설정하실 수 있어요."
    └─ [A99|config_end|3] "이제 준비가 끝났어요, 선생님. 앞으로 나눌 이야기들이 정말 기대돼요!"
```