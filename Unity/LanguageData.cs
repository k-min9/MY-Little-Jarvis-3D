using System.Collections.Generic;
using System.Linq;

// LanguageManager에서 관리할 Data를 등록후, LanguageManger에서 GameObject 등록
public static class LanguageData
{
    public static readonly List<Dictionary<string, string>> Texts = new List<Dictionary<string, string>>
    {
        // UI
        new Dictionary<string, string> { { "ko", "설정" }, { "jp", "設定" }, { "en", "Settings" } },
        new Dictionary<string, string> { { "ko", "플레이어 이름" }, { "jp", "プレイヤー名" }, { "en", "Player Name" } },
        new Dictionary<string, string> { { "ko", "서버 ID" }, { "jp", "サーバーID" }, { "en", "Server ID" } },
        new Dictionary<string, string> { { "ko", "항상 위에 표시" }, { "jp", "常に上に表示" }, { "en", "Always on Top" } },
        new Dictionary<string, string> { { "ko", "클릭 시 채팅창 표시" }, { "jp", "クリック時チャット画面表示" }, { "en", "Show Chatbox on Click" } },
        new Dictionary<string, string> { { "ko", "채팅 시 튜토리얼 표시" }, { "jp", "チャット時にチュートリアル表示" }, { "en", "Show Tutorial On Chat" } },
        new Dictionary<string, string> { { "ko", "기동 시 서버 실행" }, { "jp", "起動時にサーバー実行" }, { "en", "Start Server On Init" } },


        new Dictionary<string, string> { { "ko", "중력 적용" }, { "jp", "重力を適用" }, { "en", "Apply Gravity" } },
        new Dictionary<string, string> { { "ko", "윈도우 충돌" }, { "jp", "ウィンドウ衝突" }, { "en", "Windows Collision" } },
        new Dictionary<string, string> { { "ko", "마지막 캐릭터로 시작" }, { "jp", "最後のキャラで開始" }, { "en", "Start With Last Character" } },
        new Dictionary<string, string> { { "ko", "캐릭터별 의상 기억" }, { "jp", "キャラ別衣装を記憶" }, { "en", "Remember Char Outfits" } },

        // Menu
        new Dictionary<string, string> { { "ko", "안내" }, { "jp", "案内" }, { "en", "Inform" } },
        new Dictionary<string, string> { { "ko", "메뉴" }, { "jp", "メニュー" }, { "en", "Menu" } },
        // Main Categories
        new Dictionary<string, string> { { "ko", "세팅" }, { "jp", "セッティング" }, { "en", "Settings" } },  // 사용은 안할거임. 기본 영어로.
        new Dictionary<string, string> { { "ko", "캐릭터" }, { "jp", "キャラクター" }, { "en", "Character" } },
        new Dictionary<string, string> { { "ko", "채팅" }, { "jp", "チャット" }, { "en", "Chat" } },
        new Dictionary<string, string> { { "ko", "제어" }, { "jp", "コントロール" }, { "en", "Control" } },
        new Dictionary<string, string> { { "ko", "화면" }, { "jp", "スクリーン" }, { "en", "Screen" } },
        new Dictionary<string, string> { { "ko", "대화" }, { "jp", "トーク" }, { "en", "Talk" } },
        new Dictionary<string, string> { { "ko", "유틸" }, { "jp", "ユーティリティ" }, { "en", "Util" } },
        new Dictionary<string, string> { { "ko", "개발" }, { "jp", "開発" }, { "en", "Dev" } },
        new Dictionary<string, string> { { "ko", "버전" }, { "jp", "バージョン" }, { "en", "Version" } },
        new Dictionary<string, string> { { "ko", "종료" }, { "jp", "終了" }, { "en", "Exit" } },

        // Character submenu
        new Dictionary<string, string> { { "ko", "행동" }, { "jp", "アクション" }, { "en", "Action" } },
        new Dictionary<string, string> { { "ko", "캐릭터 변경" }, { "jp", "キャラ変更" }, { "en", "Change Char" } },
        new Dictionary<string, string> { { "ko", "캐릭터 소환" }, { "jp", "キャラ召喚" }, { "en", "Summon Char" } },
        new Dictionary<string, string> { { "ko", "의상 변경" }, { "jp", "衣装変更" }, { "en", "Change Clothes" } },

        // Chat submenu
        new Dictionary<string, string> { { "ko", "지침" }, { "jp", "指針" }, { "en", "Guideline" } },  // 대화에 참고할 행동기준
        new Dictionary<string, string> { { "ko", "상황" }, { "jp", "シチュエーション" }, { "en", "Situation" } },
        new Dictionary<string, string> { { "ko", "이력" }, { "jp", "履歴" }, { "en", "Chat History" } },
        new Dictionary<string, string> { { "ko", "기억 초기화" }, { "jp", "記憶消去" }, { "en", "Erase Memory" } },
        new Dictionary<string, string> { { "ko", "새 대화" }, { "jp", "新しいチャット" }, { "en", "New Chat" } },

        // Control submenu
        new Dictionary<string, string> { { "ko", "음성 제어창 열기" }, { "jp", "音声コントロール表示" }, { "en", "Show Voice Panel" } },
        new Dictionary<string, string> { { "ko", "대화 정보 보기"}, { "jp", "トーク情報表示"}, { "en", "Show TalkInfo"} },
        new Dictionary<string, string> { { "ko", "대화 정보 숨기기"}, { "jp", "トーク情報非表示"}, { "en", "Hide TalkInfo"} },

        // Screen submenu
        new Dictionary<string, string> { { "ko", "스크린샷 영역 설정" }, { "jp", "スクショエリア設定" }, { "en", "Set Screenshot Area" } },
        new Dictionary<string, string> { { "ko", "스크린샷 결과 보기" }, { "jp", "スクショ結果表示" }, { "en", "Show Screenshot Result" } },

        // Talk submenu
        new Dictionary<string, string> { { "ko", "튜토리얼 시작" }, { "jp", "チュートリアル開始" }, { "en", "Show Tutorial" } },
        new Dictionary<string, string> { { "ko", "잡담" }, { "jp", "フリートーク" }, { "en", "Idle Talk" } },

        // Util submenu
        new Dictionary<string, string> { { "ko", "알람" }, { "jp", "アラーム" }, { "en", "Alarm" } },
        new Dictionary<string, string> { { "ko", "미니게임1" }, { "jp", "ミニゲーム1" }, { "en", "Minigame1" } },

        // Dev submenu
        new Dictionary<string, string> { { "ko", "디버그" }, { "jp", "デバッグ" }, { "en", "Debug" } },
        new Dictionary<string, string> { { "ko", "테스트1" }, { "jp", "テスト1" }, { "en", "Test" } },
        new Dictionary<string, string> { { "ko", "테스트2" }, { "jp", "テスト2" }, { "en", "Test2" } },
        new Dictionary<string, string> { { "ko", "테스트3" }, { "jp", "テスト3" }, { "en", "Test3" } },


        // ToolTip Trigger
        new Dictionary<string, string> { { "ko", "답변에 이미지 사용" }, { "jp", "回答に画像を使用" }, { "en", "Use Image in Answer" } },
        new Dictionary<string, string> { { "ko", "Web에서 검색하기" }, { "jp", "Webで検索する" }, { "en", "Search the Web" } },
        new Dictionary<string, string> { { "ko", "서버 튜토리얼 시작" }, { "jp", "サーバーチュートリアルを開始" }, { "en", "Start Tutorial For Server" } },


        /// Scenario - Turoial
        // A00 - 시작 진입 조건
        new Dictionary<string, string> { { "ko", "선생님, 안녕하세요!" }, { "jp", "先生、こんにちは！" }, { "en", "Hello, Sensei!" } },

        // A01 - 무료 서버 재시도
        new Dictionary<string, string> { { "ko", "전에 무료 서버를 사용하려고 하셨던 것 같아요." }, { "jp", "前に無料サーバーを使おうとされていたようですね。" }, { "en", "It seems like you were trying to use a free server before." } },
        new Dictionary<string, string> { { "ko", "다시 연결해볼까요?" }, { "jp", "もう一度接続してみますか？" }, { "en", "Shall we try connecting again?" } },

        // A01-1-1 - 연결 성공
        new Dictionary<string, string> { { "ko", "성공적으로 연결되었어요, 선생님." }, { "jp", "正常に接続されました、先生。" }, { "en", "Successfully connected, Sensei." } },
        new Dictionary<string, string> { { "ko", "다만, 무료 서버는 응답 속도가 느리거나 다시 요청해야 될 수도 있어요." }, { "jp", "ただし、無료サーバーは応答速度が遅かったり、再度リクエストが必要になることがあります。" }, { "en", "However, free servers may have slow response times or require retrying requests." } },
        new Dictionary<string, string> { { "ko", "대화가 자연스럽지 않거나, 힘드시면 다른 방법을 시도해주세요." }, { "jp", "会話が自然でなかったり、困難でしたら他の方法を試してみてください。" }, { "en", "If the conversation feels unnatural or difficult, please try other methods." } },

        // A01-1-2 - 연결 실패
        new Dictionary<string, string> { { "ko", "연결에 실패했어요, 선생님." }, { "jp", "接続に失敗しました、先生。" }, { "en", "Connection failed, Sensei." } },
        new Dictionary<string, string> { { "ko", "무료 서버는 가끔 연결이 불안정할 수 있어요. 계속 시도해볼까요?" }, { "jp", "無料サーバーは時々接続が不安定になることがあります。続けて試してみますか？" }, { "en", "Free servers can sometimes have unstable connections. Shall we keep trying?" } },

        // A01-1-2-1 - 재시도
        new Dictionary<string, string> { { "ko", "다시 연결을 시도해볼게요." }, { "jp", "もう一度接続を試してみますね。" }, { "en", "I'll try connecting again." } },

        // A01-1-2-2 - 나중에
        new Dictionary<string, string> { { "ko", "네. 원하실 때 언제든지 다시 시도하실 수 있어요." }, { "jp", "はい。お望みの時にいつでも再度お試しいただけます。" }, { "en", "Yes. You can try again anytime you want." } },

        // A01-1-2-3 / A01-2-1 - 거부
        new Dictionary<string, string> { { "ko", "네. 그러면..." }, { "jp", "はい。それでは..." }, { "en", "Yes. Then..." } },

        // A02 - 플랫폼 확인
        new Dictionary<string, string> { { "ko", "저와 대화하시려면 먼저 환경 설정이 필요해요." }, { "jp", "私と会話するには、まず環境設定が必要です。" }, { "en", "To chat with me, you need to set up the environment first." } },
        new Dictionary<string, string> { { "ko", "제가 대화 전 세팅을 도와드릴게요." }, { "jp", "私が会話前のセットアップをお手伝いします。" }, { "en", "I'll help you set up before we chat." } },
        new Dictionary<string, string> { { "ko", "지금 접속하신 기기가 PC는 아닌 것 같은데, 맞으실까요?" }, { "jp", "今アクセスされている機器はPCではないようですが、正しいでしょうか？" }, { "en", "It seems like the device you're accessing from isn't a PC, is that correct?" } },

        // A02-1 - 서버 상태 확인
        new Dictionary<string, string> { { "ko", "확인해주셔서 고마워요, 선생님." }, { "jp", "確認していただいてありがとうございます、先生。" }, { "en", "Thank you for confirming, Sensei." } },
        new Dictionary<string, string> { { "ko", "일단 지금 AI 서버가 실행된 PC 정보가 있으실까요?" }, { "jp", "とりあえず、今AIサーバーが実行されているPCの情報はお持ちでしょうか？" }, { "en", "Do you have information about the PC where the AI server is currently running?" } },

        // A02-1-1 - PC ID 입력
        new Dictionary<string, string> { { "ko", "좋아요! 그럼 연결할 ID를 입력해주시면 바로 설정할게요." }, { "jp", "いいですね！それでは接続するIDを入力していただければすぐに設定します。" }, { "en", "Great! Then please enter the ID to connect and I'll set it up right away." } },

        // A02-1-2 - 도움말 설명
        new Dictionary<string, string> { { "ko", "저를 다운로드 받은 곳에서 PC 버전을 다운로드 받으실 수 있어요." }, { "jp", "私をダウンロードした場所でPC版をダウンロードできます。" }, { "en", "You can download the PC version from where you downloaded me." } },
        new Dictionary<string, string> { { "ko", "서버프로그램(`server.exe`) 실행 시 입력한 ID를 제게도 입력해주시면 연결돼요." }, { "jp", "サーバープログラム（`server.exe`）実行時に入力したIDを私にも入力していただければ接続されます。" }, { "en", "If you enter the same ID you used when running the server program (`server.exe`), we'll be connected." } },
        new Dictionary<string, string> { { "ko", "자세한 내용은 M9Dev 유튜브 채널에서도 확인하실 수 있어요." }, { "jp", "詳細はM9DevのYouTubeチャンネルでもご確認いただけます。" }, { "en", "You can also check the details on the M9Dev YouTube channel." } },

        // A02-2-1 - PC 확인
        new Dictionary<string, string> { { "ko", "확인 감사합니다, 선생님." }, { "jp", "確認ありがとうございます、先生。" }, { "en", "Thank you for confirming, Sensei." } },

        // A02-3-1 - 플랫폼 확인 거부
        new Dictionary<string, string> { { "ko", "언제든 저와 이야기하고 싶으실 때 불러주세요, 선생님!" }, { "jp", "いつでも私とお話ししたくなったら呼んでください、先生！" }, { "en", "Please call me whenever you want to talk with me, Sensei!" } },

        // A03 - 연산 방식 선택
        new Dictionary<string, string> { { "ko", "AI가 작동하려면 먼저 서버 설정이 필요해요." }, { "jp", "AIが動作するには、まずサーバー設定が必要です。" }, { "en", "For AI to work, server setup is needed first." } },
        new Dictionary<string, string> { { "ko", "지금 사용 중인 이 PC로 연산하는 것도 가능해요." }, { "jp", "今使用中のこのPCで演算することも可能です。" }, { "en", "It's also possible to compute with this PC you're currently using." } },

        // A03-1 - 로컬 연산
        new Dictionary<string, string> { { "ko", "확인해볼게요... CUDA 환경 지원 여부를 검사 중이에요." }, { "jp", "確認してみますね...CUDA環境のサポート状況を検査中です。" }, { "en", "Let me check... I'm examining CUDA environment support." } },

        // A03-1-1 - CUDA 지원
        new Dictionary<string, string> { { "ko", "CUDA 환경이 감지되었어요." }, { "jp", "CUDA環境が検出されました。" }, { "en", "CUDA environment detected." } },
        new Dictionary<string, string> { { "ko", "선생님의 PC에서 GPU를 사용할 수 있어요. 더 빠른 응답을 원하신다면 좋은 선택이에요. 어떻게 하실래요?" }, { "jp", "先生のPCでGPUを使用できます。より速い応答をお望みでしたら良い選択です。どうされますか？" }, { "en", "You can use GPU on your PC, Sensei. It's a good choice if you want faster responses. What would you like to do?" } },

        // A03-1-2 - CUDA 미지원
        new Dictionary<string, string> { { "ko", "CUDA 환경을 찾지 못했어요." }, { "jp", "CUDA環境が見つかりませんでした。" }, { "en", "CUDA environment not found." } },
        new Dictionary<string, string> { { "ko", "아쉽게도 GPU는 지원되지 않지만, CPU로 작동하는 건 가능해요. 이어서 진행하실까요?" }, { "jp", "残念ながらGPUはサポートされませんが、CPUで動作することは可能です。続けて進めますか？" }, { "en", "Unfortunately GPU isn't supported, but it can work with CPU. Shall we continue?" } },

        // A04 - 외부 서버 선택
        new Dictionary<string, string> { { "ko", "그러면 외부 서버와 연결해볼게요." }, { "jp", "それでは外部サーバーと接続してみますね。" }, { "en", "Then let's try connecting to an external server." } },
        new Dictionary<string, string> { { "ko", "무료 서버를 이용하시거나, API 키를 입력해서 외부 플랫폼(Gemini, ChatGPT...)과 연결하실 수 있어요." }, { "jp", "無料サーバーをご利用いただくか、APIキーを入力して外部プラットフォーム（Gemini、ChatGPT...）と接続できます。" }, { "en", "You can use a free server or enter an API key to connect with external platforms (Gemini, ChatGPT...)." } },

        // A04-1-1 - 외부 무료 서버 연결 실패
        new Dictionary<string, string> { { "ko", "연결이 잘 되지 않았어요. 다시 시도해볼까요?" }, { "jp", "接続がうまくいきませんでした。もう一度試してみますか？" }, { "en", "The connection didn't work well. Shall we try again?" } },

        // A04-2 - API 키 입력
        new Dictionary<string, string> { { "ko", "API KEY 관련 모델을 골라주세요" }, { "jp", "API KEY関連のモデルを選んでください" }, { "en", "Please choose an API KEY related model" } },

        // A97 - 연결 테스트
        new Dictionary<string, string> { { "ko", "서버에 연결을 시도하고 있어요..." }, { "jp", "接続を試みています..." }, { "en", "Attempting to connect..." } },
        new Dictionary<string, string> { { "ko", "ChatGPT에 연결을 시도하고 있어요..." }, { "jp", "ChatGPTへの接続を試みています..." }, { "en", "Attempting to connect to ChatGPT..." } },
        new Dictionary<string, string> { { "ko", "Gemini에 연결을 시도하고 있어요..." }, { "jp", "Geminiへの接続を試みています..." }, { "en", "Attempting to connect to Gemini..." } },
        new Dictionary<string, string> { { "ko", "OpenRouter에 연결을 시도하고 있어요..." }, { "jp", "OpenRouterへの接続を試みています..." }, { "en", "Attempting to connect to OpenRouter..." } },
        new Dictionary<string, string> { { "ko", "성공했어요 선생님" }, { "jp", "成功しました、先生" }, { "en", "Success, Sensei" } },
        new Dictionary<string, string> { { "ko", "실패했어요 선생님. 다시 시도해볼까요?" }, { "jp", "失敗しました、先生。もう一度試してみますか？" }, { "en", "Failed, Sensei. Shall we try again?" } },

        // A98 - 설정 취소
        new Dictionary<string, string> { { "ko", "알겠어요 선생님." }, { "jp", "わかりました、先生。" }, { "en", "I understand, Sensei." } },
        new Dictionary<string, string> { { "ko", "필요하실 땐 언제든지 다시 설정하실 수 있어요." }, { "jp", "必要な時はいつでも再設定できます。" }, { "en", "You can set it up again anytime you need." } },

        // A99 - 설정 완료
        new Dictionary<string, string> { { "ko", "설정이 완료되었어요, 선생님!" }, { "jp", "設定が完了しました、先生！" }, { "en", "Setup is complete, Sensei!" } },
        new Dictionary<string, string> { { "ko", "필요하실 땐 언제든지 다시 설정하실 수 있어요." }, { "jp", "必要な時はいつでも再設정できます。" }, { "en", "You can set it up again anytime you need." } },
        new Dictionary<string, string> { { "ko", "이제 준비가 끝났어요, 선생님. 앞으로 나눌 이야기들이 정말 기대돼요!" }, { "jp", "準備が整いました、先生。これからお話しする内容が本当に楽しみです！" }, { "en", "Now we're all set, Sensei. I'm really looking forward to the conversations we'll have!" } },

        /// Scenario - Installer
        // I01 - 인스톨러 진입 조건
        new Dictionary<string, string> { { "ko", "선생님. 대화를 위한 기본적인 파일이 설치되어 있지 않아요." }, { "jp", "先生。会話のための基本的なファイルがインストールされていません。" }, { "en", "Sensei. The basic files for conversation are not installed." } },
        new Dictionary<string, string> { { "ko", "설치를 위한 프로그램을 구동해도 될까요?" }, { "jp", "インストール用のプログラムを起動してもよろしいですか？" }, { "en", "May I run the installation program?" } },

        // I01_0 - 버전 설명
        new Dictionary<string, string> { { "ko", "Lite 버전과 Full 버전 어느쪽으로 설치할까요?" }, { "jp", "LiteバージョンとFullバージョン、どちらをインストールしますか？" }, { "en", "Which version would you like to install: Lite or Full?" } },
        new Dictionary<string, string> { { "ko", "Lite는 음성 인식 같은 기본 기능만 설치돼요. 연산은 외부 플랫폼에 맡기기 때문에 설치도 빠르고 용량도 가벼워요." }, { "jp", "Liteは音声認識などの基本機能のみをインストールします。演算は外部プラットフォームに任せるので、インストールが速く容量も軽いです。" }, { "en", "Lite only installs basic features like speech recognition. Since computation is handled by an external platform, it's quick to install and lightweight." } },
        new Dictionary<string, string> { { "ko", "Full은 Lite의 기능이 제공되고, AI 연산도 선생님의 컴퓨터에서 직접 처리해요. 외부 서버 상태에 영향을 받지 않고 안정적인 품질을 기대하실 수 있어요." }, { "jp", "FullはLiteの機能を含み、AIの演算も先生のPCで直接行います。外部サーバーの状態に左右されず、安定した品質が期待できます。" }, { "en", "Full includes all the features of Lite and performs AI computation directly on your PC. You can expect stable quality without relying on external servers." } },
        new Dictionary<string, string> { { "ko", "그만큼 용량도 크고, 컴퓨터 성능에 따라 답변 속도가 달라질 수 있어요." }, { "jp", "その分、容量が大きくなり、PCの性能によって応答速度が変わることがあります。" }, { "en", "It takes up more space, and response speed may vary depending on your PC's performance." } },
        new Dictionary<string, string> { { "ko", "두 버전은 나중에 언제든지 바꾸실 수 있으니까, 편하게 골라주세요." }, { "jp", "どちらのバージョンも後からいつでも変更できますので、気軽に選んでくださいね。" }, { "en", "You can switch between versions anytime later, so feel free to choose the one you like." } },

        // I01-1 - 설치 진행
        new Dictionary<string, string> { { "ko", "설치 프로그램을 실행할게요." }, { "jp", "インストールプログラムを実行します。" }, { "en", "I'll run the installation program." } },

        // I01-2 - 나중에 설치
        new Dictionary<string, string> { { "ko", "언제든 필요하실 때 다시 말씀해주세요." }, { "jp", "いつでも必要でしたら、またお声かけください。" }, { "en", "Please let me know anytime you need it." } },

        // I01-3 - 이미 설치됨
        new Dictionary<string, string> { { "ko", "아, 이미 설치되어 있네요!" }, { "jp", "あ、もうインストールされていますね！" }, { "en", "Oh, it's already installed!" } },
        new Dictionary<string, string> { { "ko", "이제 서버를 시작하면 저와 대화할 수 있어요." }, { "jp", "これでサーバーを起動すれば、私と会話できます。" }, { "en", "Now you can start the server and chat with me." } },

        // I01-4 - 이미 실행 중
        new Dictionary<string, string> { { "ko", "설치 프로그램이 이미 실행 중이에요." }, { "jp", "インストールプログラムが既に実行中です。" }, { "en", "The installation program is already running." } },
        new Dictionary<string, string> { { "ko", "잠시만 기다려주세요." }, { "jp", "しばらくお待ちください。" }, { "en", "Please wait a moment." } },

        // I02 - 설치 완료 및 서버 실행
        new Dictionary<string, string> { { "ko", "설치가 완료되었어요!" }, { "jp", "インストールが完了しました！" }, { "en", "The installation is complete!" } },
        new Dictionary<string, string> { { "ko", "바로 서버를 시작해볼게요, 선생님!" }, { "jp", "すぐにサーバーを起動しますね、先生！" }, { "en", "I'll start the server right away, sir!" } },

        // C01 - 서버 준비 완료 알림
        new Dictionary<string, string> { { "ko", "선생님, 서버가 준비되었어요!" }, { "jp", "先生、サーバーの準備ができましたよ！" }, { "en", "Sensei, the server is now ready!" }},

        // C02 - 서버 기동 여부 확인
        new Dictionary<string, string> { { "ko", "선생님, 안녕하세요!" }, { "jp", "先生、こんにちは！" }, { "en", "Hello, Sensei!" } },
        new Dictionary<string, string> { { "ko", "현재 서버를 기동하지 않으셨는데 기동하셔도 괜찮으실까요?" }, { "jp", "現在、サーバーが起動されていませんが、起動してもよろしいですか？" }, { "en", "The server is not currently running. Would you like to start it, Sensei?" } },
        new Dictionary<string, string> { { "ko", "네! 서버를 기동할게요." }, { "jp", "はい！サーバーを起動しますね。" }, { "en", "Okay! I'll start the server now, Sensei!" } },
        new Dictionary<string, string> { { "ko", "마음이 바뀌시면 언제든지 다시 말 걸어주세요, 선생님!" }, { "jp", "気が変わったら、いつでもまた話しかけてくださいね、先生！" }, { "en", "If you change your mind, feel free to talk to me anytime, Sensei!" } },

        // C90 - 일반 안내
        new Dictionary<string, string> { { "ko", "현재 버전에서는 사용할 수 없는 설정이에요." }, { "jp", "このバージョンでは利用できない設定です。" }, { "en", "This setting is not available in the current version." } },
        new Dictionary<string, string> { { "ko", "Lite 이상의 Edition을 설치하시면 이용하실 수 있어요, 선생님." }, { "jp", "Lite以上のエディションをインストールすればご利用いただけますよ、先生。" }, { "en", "You can use this by installing the Lite edition or higher, Sensei." } },
        new Dictionary<string, string> { { "ko", "Full 이상의 Edition을 설치하시면 이용하실 수 있어요, 선생님." }, { "jp", "Full以上のエディションをインストールすればご利用いただけますよ、先生。" }, { "en", "You can use this by installing the Full edition or higher, Sensei." } },

        // C91 - API/Quota/모델 안내
        new Dictionary<string, string> { { "ko", "구글에서 개발자에게 제공된 일일 무료 사용량이 이미 모두 소진된 것 같아요." }, { "jp", "Googleが開発者に提供している1日あたりの無料利用枠はすでに使い切られたようです。" }, { "en", "The daily free usage quota provided by Google to developers seems to have already been used up." } },
        new Dictionary<string, string> { { "ko", "API 키를 발급받아 입력하시면 무료 또는 유료 서버와 연결하실 수 있어요." }, { "jp", "APIキーを発行して入力すれば、無料または有料のサーバーに接続できますよ。" }, { "en", "If you obtain and enter an API key, you can connect to either free or paid servers." } },
        new Dictionary<string, string> { { "ko", "구글에서 더 이상 해당 무료 AI 모델을 이용할 수 없는 것 같아요." }, { "jp", "Googleではその無料AIモデルはもう利用できないようです。" }, { "en", "It seems that this free AI model is no longer available from Google." } },

        // C99 - 준비/안내
        new Dictionary<string, string> { { "ko", "선생님, 죄송해요 아직 준비가 되지 않았어요..." }, { "jp", "先生、ごめんなさい。まだ準備ができていません..." }, { "en", "Sorry, Sensei... I'm not ready yet." } },
        new Dictionary<string, string> { { "ko", "곧 지원할 예정이에요. 기다려주세요 선생님!" }, { "jp", "まもなく対応予定です。待っていてください、先生！" }, { "en", "Support will be available soon. Please wait, Sensei!" } },
        new Dictionary<string, string> { { "ko", "관련 안내가 필요하실까요, 선생님?" }, { "jp", "ご案内が必要ですか、先生？" }, { "en", "Would you like me to guide you, Sensei?" } },


        // 단어 정보

        new Dictionary<string, string> { { "ko", "다운로드" }, { "jp", "ダウンロード" }, { "en", "Download" } },
        new Dictionary<string, string> { { "ko", "보유중" }, { "jp", "所持済" }, { "en", "Owned" } },

        new Dictionary<string, string> { { "ko", "왼쪽으로" }, { "jp", "左に" }, { "en", "Go Left" } },
        new Dictionary<string, string> { { "ko", "오른쪽으로" }, { "jp", "右に" }, { "en", "Go Right" } },
        new Dictionary<string, string> { { "ko", "춤추기" }, { "jp", "踊る" }, { "en", "Dance" } },
        new Dictionary<string, string> { { "ko", "대기" }, { "jp", "待機" }, { "en", "Idle" } },
        new Dictionary<string, string> { { "ko", "숨기" }, { "jp", "隠す" }, { "en", "Hide" } },

        new Dictionary<string, string> { { "ko", "언어" }, { "jp", "言語" }, { "en", "Language" } },
        new Dictionary<string, string> { { "ko", "음성인식" }, { "jp", "音声認識" }, { "en", "Speech Recognition" } },
        new Dictionary<string, string> { { "ko", "음성인식" }, { "jp", "音声認識" }, { "en", "Speech Recognition" } }, // "Speech R."
        new Dictionary<string, string> { { "ko", "사전로딩" }, { "jp", "先読み" }, { "en", "Preloading" } },
        new Dictionary<string, string> { { "ko", "기타" }, { "jp", "その他" }, { "en", "Extra" } },
        new Dictionary<string, string> { { "ko", "작고 빠른(X16)" }, { "jp", "小さくて速い(X16)" }, { "en", "Small, Fast (X16)" } },
        new Dictionary<string, string> { { "ko", "기본(X8)" }, { "jp", "基本(X8)" }, { "en", "Basic (X8)" } },
        new Dictionary<string, string> { { "ko", "크고 정확한(X3)" }, { "jp", "大きくて正確(X3)" }, { "en", "Large, Accurate (X3)" } },
        new Dictionary<string, string> { { "ko", "더 크고 정확한(X1)" }, { "jp", "より大きく、より正確(X1)" }, { "en", "Larger, More Accurate (X1)" } },
        new Dictionary<string, string> { { "ko", "다운로드" }, { "jp", "ダウンロード" }, { "en", "Download" } },
        new Dictionary<string, string> { { "ko", "테스트" }, { "jp", "テスト" }, { "en", "Test" } },
        new Dictionary<string, string> { { "ko", "음성 인식용 모듈 다운로드" }, { "jp", "音声認識用モジュールダウンロード" }, { "en", "Download modules for speech recognition" } },
        new Dictionary<string, string> { { "ko", "음성" }, { "jp", "音声" }, { "en", "Voice" } },
        new Dictionary<string, string> { { "ko", "프로그램 구동시 모듈을 미리 읽어서,\n최초 동작을 빠르게 합니다." }, { "jp", "プログラム駆動時モジュールを先読みして、\n初期動作を高速化します。" }, { "en", "Preloads modules when the program is run,\nspeeding up first-time behavior." } },
        new Dictionary<string, string> { { "ko", "메인 캐릭터의 음성 모듈을 미리 읽어옵니다." }, { "jp", "メインキャラクターの音声モジュールを先読みます。" }, { "en", "Preload main character's voice module." } },
        new Dictionary<string, string> { { "ko", "설정된 음성 인식 모듈을 미리 읽어옵니다." }, { "jp", "設定された音声認識モジュールを先読みます。" }, { "en", "Preload set speech recognition module." } },
        new Dictionary<string, string> { { "ko", "다운로드" }, { "jp", "ダウンロード" }, { "en", "Downloaded" } },
        new Dictionary<string, string> { { "ko", "확인" }, { "jp", "確認" }, { "en", "Confirm" } },
        new Dictionary<string, string> { { "ko", "작업 성공" }, { "jp", "作業成功" }, { "en", "Task Success" } },
        new Dictionary<string, string> { { "ko", "다운로드 받으시겠습니까?" }, { "jp", "ダウンロードしますか？" }, { "en", "Do you want to download?" } },
        new Dictionary<string, string> { { "ko", "의 용량이 필요합니다.\n" }, { "jp", "の容量が必要です。\n" }, { "en", "of space is required.\n" } },
        new Dictionary<string, string> { { "ko", "]이 Workspace에 무사히 추가되었습니다.\nInworld Name에 반영하시겠습니까?" }, { "jp", "]がWorkspaceに無事追加されました.\nInworld Nameに反映しますか？" }, { "en", "] have been successfully added to Workspace.\nDo you want to reflect to your [Inworld Name]?" } },
        new Dictionary<string, string> { { "ko", "현재 목소리 설정을 저장하시겠습니까?" }, { "jp", "現在の音声設定を保存しますか？" }, { "en", "Do you want to save your current voice settings?" } },
        new Dictionary<string, string> { { "ko", "정말 삭제하시겠습니까?" }, { "jp", "本当に削除しますか？" }, { "en", "Do you really want to delete it?" } },
        new Dictionary<string, string> { { "ko", "음성생성에 GPU 가속을 지금 바로 적용하시겠습니까?" }, { "jp", "音声生成にGPU加速を今すぐ適用してみますか？" }, { "en", "Want to enable GPU acceleration to voice generation right now?" } },
        new Dictionary<string, string> { { "ko", "음성인식을 바로 활성화 하시겠습니까?" }, { "jp", "音声認識を今すぐ適用してみますか？" }, { "en", "Want to enable voice recognition right now?" } },
        new Dictionary<string, string> { { "ko", "취소" }, { "jp", "キャンセル" }, { "en", "Cancel" } },
        new Dictionary<string, string> { { "ko", "완료" }, { "jp", "完了" }, { "en", "Finish" } },
        new Dictionary<string, string> { { "ko", "에러" }, { "jp", "エラー" }, { "en", "Error" } },
        new Dictionary<string, string> { { "ko", "안내" }, { "jp", "案内" }, { "en", "Guidance" } },
        new Dictionary<string, string> { { "ko", "작업이 취소되었습니다." }, { "jp", "作業は中止されました。" }, { "en", "The task was canceled." } },
        new Dictionary<string, string> { { "ko", "작업이 완료되었습니다." }, { "jp", "作業が完了しました。" }, { "en", "The task is completed." } },
        new Dictionary<string, string> { { "ko", "이미 다운로드 중인 파일이 있습니다." }, { "jp", "すでにダウンロード中のファイルがあります。" }, { "en", "You already have a downloading file." } },
        new Dictionary<string, string> { { "ko", "이미 해당 이름을 갖는 캐릭터가 있습니다." }, { "jp", "すでにその名前を持つキャラクターがあります。" }, { "en", "There is already a character with that name." } },
        new Dictionary<string, string> { { "ko", "창을 닫을 때 적용됩니다." }, { "jp", "ウィンドウ終了時に適用されます。" }, { "en", "It will be applied at closing the window." } },
        new Dictionary<string, string> { { "ko", "현재 버전은 Inworld와 Local만 제공합니다." }, { "jp", "現在のバージョンは[Inworld, Local]のみ提供しています。" }, { "en", "Current version only offers Inworld and Local." } },
        new Dictionary<string, string> { { "ko", "현재 버전은 Local만 제공합니다." }, { "jp", "現在のバージョンは[Local]のみ提供しています。" }, { "en", "Current version only offers Local." } },
        new Dictionary<string, string> { { "ko", "다음 중 하나의 값이 정확하지 않습니다.\n[Workspace 이름]\n[inworld api key(Workspace용)]\n[Inworld Name]" }, { "jp", "次のいずれかのデータが正しくありません。\n[Workspace名]\n[inworld api key(Workspace用)]\n[Inworld Name]" }, { "en", "One of the following values is incorrect\n[Workspace]\n[API Key(for workspace)]\n[Inworld Name]" } },
        new Dictionary<string, string> { { "ko", "우선 Studio API KEY를 입력해주세요." }, { "jp", "まず、Studio API KEYを入力してください。" }, { "en", "Enter your Studio API KEY first." } },
        new Dictionary<string, string> { { "ko", "에 저장하였습니다." }, { "jp", "に保存しました。" }, { "en", " : save location" } },
        new Dictionary<string, string> { { "ko", "inworld 폴더의 하위 위치에만 저장할 수 있습니다." }, { "jp", "inworldフォルダのサブロケーションにのみ保存できます。" }, { "en", "You can only save to a sublocation of the inworld folder." } },
        new Dictionary<string, string> { { "ko", "다음 중 하나의 값이 정확하지 않습니다.\n[Workspace]\n[Inworld Name]\n[Studio API KEY]" }, { "jp", "次のいずれかのデータが正しくありません。\n[Workspace]\n[Inworld Name]\n[Studio API KEY]" }, { "en", "One of the following values is incorrect.\n[Workspace]\n[Inworld Name]\n[Studio API KEY]" } },
        new Dictionary<string, string> { { "ko", "이름을 입력해주세요." }, { "jp", "名前を入力してください。" }, { "en", "Please enter a name." } },
        new Dictionary<string, string> { { "ko", "json 파일이 정상적이지 않습니다." }, { "jp", "jsonファイルが正常ではありません。" }, { "en", "The JSON file is not valid." } },
        new Dictionary<string, string> { { "ko", "json 파일을 선택해주세요." }, { "jp", "jsonファイルを選択してください。" }, { "en", "Select a JSON file." } },
        new Dictionary<string, string> { { "ko", "main.exe 하위 폴더에만 저장할 수 있습니다." }, { "jp", "main.exeサブフォルダにのみ保存できます。" }, { "en", "You can only store them in the main.exe subfolder." } },
        new Dictionary<string, string> { { "ko", "]으로 캐릭터를 변경하시겠습니까?" }, { "jp", "]にキャラクターを変更しますか？" }, { "en", "] : Change Character" } },
        new Dictionary<string, string> { { "ko", "\n'AI'가 설정되지 않았습니다." }, { "jp", "\n'AI'が設定されていません。" }, { "en", "\nNo 'AI' is set up." } },
        new Dictionary<string, string> { { "ko", "\n'Animation assets'가 설정되지 않았습니다." }, { "jp", "\n'Animation assets'が設定されていません。" }, { "en", "\nNo 'Animation assets' is set up." } },
        new Dictionary<string, string> { { "ko", "\n'Voice'가 설정되지 않았습니다." }, { "jp", "\n'Voice'が設定されていません。" }, { "en", "\nNo 'Voice' is set up." } },
        new Dictionary<string, string> { { "ko", "]으로 메인 캐릭터를 변경하였습니다." }, { "jp", "]にメインキャラクターを変更しました。" }, { "en", "] : Change Main Character" } },
        new Dictionary<string, string> { { "ko", "[arona]는 삭제할 수 없습니다." }, { "jp", "[arona]は削除できません。" }, { "en", "You cannot delete [arona]." } },
        new Dictionary<string, string> { { "ko", "메인 캐릭터는 삭제할 수 없습니다." }, { "jp", "メインキャラクターは削除できません。" }, { "en", "You cannot delete the main character." } },
        new Dictionary<string, string> { { "ko", "유효한 키가 아니거나, 통신 상태가 좋지 않습니다." }, { "jp", "有効なキーでないか、通信状態が悪いです。" }, { "en", "The key is not valid, or the connection is poor." } },
        new Dictionary<string, string> { { "ko", "[설정]에서 Voice를 [GPU]로 설정해주세요." }, { "jp", "[設定]でVoiceを[GPU]に設定してください。" }, { "en", "Set Voice to [GPU] in [Settings]." } },
        new Dictionary<string, string> { { "ko", "GPU 가속을 사용할 수 없는 컴퓨터입니다." }, { "jp", "GPU加速を使用できないコンピュータです。" }, { "en", "The computer is not capable of GPU acceleration." } },
        new Dictionary<string, string> { { "ko", "GPU 가속을 활성화하였습니다." }, { "jp", "GPU加速を起動しました。" }, { "en", "GPU acceleration is enabled." } },
        new Dictionary<string, string> { { "ko", "[설정]에서 Talk을 설정해주세요." }, { "jp", "[設定]でTalkを設定してください。" }, { "en", "Set up Talk in [Settings]." } },
        new Dictionary<string, string> { { "ko", "이미 음성인식이 활성화 되어있습니다." }, { "jp", "すでに音声認識が起動しています。" }, { "en", "Speech recognition is already enabled." } },
        new Dictionary<string, string> { { "ko", "마이크가 감지되지 않습니다." }, { "jp", "マイクが検知されません。" }, { "en", "The microphone is not detected." } },
        new Dictionary<string, string> { { "ko", "음성 인식을 활성화하였습니다." }, { "jp", "音声認識を起動しました。" }, { "en", "Speech recognition is enabled." } },
        new Dictionary<string, string> { { "ko", "음성 인식이 활성화되지 않았습니다." }, { "jp", "音声認識が起動されません。" }, { "en", "Speech recognition is not enabled." } },
        new Dictionary<string, string> { { "ko", "음성인식을 비활성화하였습니다." }, { "jp", "音声認識を非アクティブ化しました。" }, { "en", "Speech recognition is disabled." } },
        new Dictionary<string, string> { { "ko", "[enter]는 사용하실 수 없습니다." }, { "jp", "[enter]は使用できません。" }, { "en", "key [ENTER] is not allowed." } },
        new Dictionary<string, string> { { "ko", "충돌 방지를 위해 [대화] 옵션을 OFF로 설정하세요." }, { "jp", "衝突を防ぐため、[会話]オプションをOFFにしてください。" }, { "en", "Please set [Talk] Option to OFF to avoid conflicts." } },
        new Dictionary<string, string> { { "ko", "새로운 대화를 시작하겠습니까?" }, { "jp", "新しい会話を始めますか？" }, { "en", "Do you want to start a new conversation?" } },
        new Dictionary<string, string> { { "ko", "네" }, { "jp", "はい" }, { "en", "Yes" } },
        new Dictionary<string, string> { { "ko", "아니오" }, { "jp", "いいえ" }, { "en", "No" } },
        new Dictionary<string, string> { { "ko", "이름" }, { "jp", "名前" }, { "en", "Name" } },
        new Dictionary<string, string> { { "ko", "입력" }, { "jp", "入力" }, { "en", "Input" } },
        new Dictionary<string, string> { { "ko", "변경" }, { "jp", "変更" }, { "en", "Change" } },
        new Dictionary<string, string> { { "ko", "초기화" }, { "jp", "初期化" }, { "en", "Initialize" } },
        new Dictionary<string, string> { { "ko", "무료" }, { "jp", "無料" }, { "en", "Free" } },
        new Dictionary<string, string> { { "ko", "있음" }, { "jp", "あり" }, { "en", "Exist" } },
        new Dictionary<string, string> { { "ko", "선택" }, { "jp", "選択" }, { "en", "Select" } },
        new Dictionary<string, string> { { "ko", "카탈로그" }, { "jp", "カタログ" }, { "en", "Catalog" } },
        new Dictionary<string, string> { { "ko", "Inworld에서 사용할 캐릭터가 포함된 Workspace를 골라주세요." }, { "jp", "Inworldで使用するキャラクターが含まれているWorkspaceを選択してください。" }, { "en", "Select the Inworld Workspace that contains the characters you want to use." } },
        new Dictionary<string, string> { { "ko", "Workspace에 맞는 API Key를 골라주세요.\n(Workspace마다 API Key가 다릅니다.)" }, { "jp", "Workspaceに合ったAPI Keyを選択してください。\n(WorkspaceごとにAPI Keyが異なります)" }, { "en", "Choose an API Key that matches your Workspace.\n(Each Workspace has a different API Key)" } },
        new Dictionary<string, string> { { "ko", "사용할 캐릭터가 Inworld에 등록된 이름" }, { "jp", "使用するキャラクターがInworldに登録されている名前" }, { "en", "Name of the character registered in Inworld." } },
        new Dictionary<string, string> { { "ko", "animation 폴더에서 캐릭터가 사용할 애니메이션을 골라주세요." }, { "jp", "animationフォルダからキャラクターが使用するアニメーションを選択してください。" }, { "en", "Select the animation you want the character to use from the animation folder." } },
        new Dictionary<string, string> { { "ko", "idle : 평소\nsit : 앉기\npick : 집기\nfall : 낙하 (pick)\nthink : 채팅 입력\ntalk : 채팅 답변\nsmile : 설정(think)\nwalk : 걷기\n\n(해당 동작 애니메이션이 없을 경우, 괄호 속 애니메이션이 사용됩니다. \n그래도 사용할 애니메이션이 없으면 idle 애니메이션이 사용됩니다.)" }, { "jp", "idle : 通常\nsit : 座る\npick : つまみ\nfall : 落下(pick)\nthink : チャット入力\ntalk : チャット発言\nsmile : 設定(think)\nwalk : 歩く\n\n(該当する動作アニメーションがない場合、括弧内のアニメーションが使用されます。\nそれでも使用するアニメーションがない場合は、idleアニメーションが使用されます。)" }, { "en", "idle: idle\nsit: sitting\npick: picking up\nfall: falling (pick)\nthink: chat typing\ntalk: chat answer\nsmile: setting (think)\nwalk: walk\n\n(If there is no corresponding action animation, the animation in parentheses is used. \nIf there is still no animation to use, the idle animation is used.)" } },
        new Dictionary<string, string> { { "ko", "현재 애니메이션 사용 여부\n(idle은 해제할 수 없습니다.)" }, { "jp", "現在のアニメーションの使用有無\n(idleは解除できません。)" }, { "en", "Whether the current animation is enabled or disabled.\n(idle cannot be turned off)" } },
        new Dictionary<string, string> { { "ko", "현재 애니메이션 재생 길이\n(1000 = 1s)" }, { "jp", "現在のアニメーションの再生時間\n(1000 = 1s)" }, { "en", "Current animation play length\n(1000 = 1s)" } },
        new Dictionary<string, string> { { "ko", "현재 애니메이션 넓이" }, { "jp", "現在のアニメーションの幅" }, { "en", "Current animation width" } },
        new Dictionary<string, string> { { "ko", "현재 애니메이션 높이" }, { "jp", "現在のアニメーションの高さ" }, { "en", "Current animation height" } },
        new Dictionary<string, string> { { "ko", "현재 애니메이션 크기 비율" }, { "jp", "現在のアニメーションサイズ比率" }, { "en", "Current animation size rate" } },
        new Dictionary<string, string> { { "ko", "현재 애니메이션 바닥위치\n(우측 캔버스의 붉은 선 참조)" }, { "jp", "現在のアニメーションの底位置\n(右側キャンバスの赤線参照)" }, { "en", "Current animation bottom position\n(see red line on the right canvas)" } },
        new Dictionary<string, string> { { "ko", "캐릭터 이름을 적어주세요." }, { "jp", "キャラクター名を書いてください" }, { "en", "Write Name for character" } },
        new Dictionary<string, string> { { "ko", "이 프로그램은 무료로 사용할 수 있으며 많은 기부자들의 후원으로 제작되고 있습니다." }, { "jp", "このプログラムは無料で利用することができ、多くのパトロンの後援で制作されています。" }, { "en", "The program is free to use and is supported by many generous donors." } },
        new Dictionary<string, string> { { "ko", "클릭 반응속도" }, { "jp", "クリック反応速度 " }, { "en", "Click Reaction" } },
        new Dictionary<string, string> { { "ko", "최신" }, { "jp", "最新" }, { "en", "Latest" } },
        new Dictionary<string, string> { { "ko", "갱신가능" }, { "jp", "更新可能" }, { "en", "Updateable" } },
        new Dictionary<string, string> { { "ko", "업데이트를 위해 install.exe를 실행해주세요." }, { "jp", "アップデートのためにinstall.exeを実行してください。" }, { "en", "Run install.exe for the update." } },
        new Dictionary<string, string> { { "ko", "'idle'의 넓이로 설정합니다." }, { "jp", "'idle'の幅に設定します。" }, { "en", "Set to the width of 'idle'" } },
        new Dictionary<string, string> { { "ko", "원본의 넓이로 설정합니다." }, { "jp", "元の幅に設定します。" }, { "en", "Set to the width of the original" } },
        new Dictionary<string, string> { { "ko", "높이에 'idle'의 비율을 곱합니다." }, { "jp", "高さに'idle'の比率を掛けます。" }, { "en", "Multiply the height by the rate of 'idle'" } },
        new Dictionary<string, string> { { "ko", "'idle'의 높이로 설정합니다." }, { "jp", "'idle'の高さに設定します。" }, { "en", "Set to the height of 'idle'" } },
        new Dictionary<string, string> { { "ko", "원본의 높이로 설정합니다." }, { "jp", "元の高さに設定します。" }, { "en", "Set to the height of the original" } },
        new Dictionary<string, string> { { "ko", "넓이에 'idle'의 비율을 곱합니다." }, { "jp", "幅に'idle'の比率を掛けます。" }, { "en", "Multiply the width by the rate of 'idle'" } },
        new Dictionary<string, string> { { "ko", "추가" }, { "jp", "追加" }, { "en", "Add" } },
        new Dictionary<string, string> { { "ko", "삭제" }, { "jp", "削除" }, { "en", "Delete" } },
        new Dictionary<string, string> { { "ko", "수정" }, { "jp", "修正" }, { "en", "Edit" } },
        new Dictionary<string, string> { { "ko", "업데이트 중입니다..." }, { "jp", "アップデート中です..." }, { "en", "Under updating..." } },
        new Dictionary<string, string> { { "ko", "유저 이름" }, { "jp", "ユーザー名" }, { "en", "User name" } },
        new Dictionary<string, string> { { "ko", "UI/Setting에서 쓸 언어" }, { "jp", "UI/Settingで使う言語" }, { "en", "Languages to use in UI/Settings" } },
        new Dictionary<string, string> { { "ko", "음량" }, { "jp", "音量" }, { "en", "Volume" } },
        new Dictionary<string, string> { { "ko", "음소거" }, { "jp", "ミュート" }, { "en", "Mute" } },
        new Dictionary<string, string> { { "ko", "채팅 응답 언어\n(질문은 아무 언어나 OK)" }, { "jp", "チャット応答言語\n(質問はどの言語でもOK)" }, { "en", "Chat response language\n(Questions can be asked in any language)" } },
        new Dictionary<string, string> { { "ko", "채팅에 웹 검색 사용" }, { "jp", "チャットにWeb検索を使用" }, { "en", "Use Web search for chatting" } },
        new Dictionary<string, string> { { "ko", "좌클릭 1회로 채팅시작" }, { "jp", "左クリック1回でチャット開始" }, { "en", "Start chat with a single left click" } },
        new Dictionary<string, string> { { "ko", "키보드 키를 눌러서 채팅시작\n(클릭도 적용)" }, { "jp", "キーボードキーを押してチャット開始\n(Clickも適用)" }, { "en", "keyboard key to start chat\n(click also applies)" } },
        new Dictionary<string, string> { { "ko", "기본 대화" }, { "jp", "基本会話" }, { "en", "Default conversations" } },
        new Dictionary<string, string> { { "ko", "Web검색 결과를 대답에 반영" }, { "jp", "Web検索結果を回答に反映" }, { "en", "Web search results are reflected in the answer" } },
        new Dictionary<string, string> { { "ko", "Story 요소에 대한 대화가 가능" }, { "jp", "Story要素についての会話が可能" }, { "en", "Enables conversations about Story elements." } },
        new Dictionary<string, string> { { "ko", "AI를 이용한 번역. 리소스적으로 비추천" }, { "jp", "AIを使った翻訳。リソース的に非推奨。" }, { "en", "Translation with AI. Resourcefully not recommended." } },
        new Dictionary<string, string> { { "ko", "단기기억력. 최근 대화를 대화에 반영" }, { "jp", "短期記憶力。最近の会話を会話に反映。" }, { "en", "Short-term memory. Reflects recent conversations in the conversation." } },
        new Dictionary<string, string> { { "ko", "장기기억력. 대화를 통해 유저의 특성을 기억하고 대화에 반영" }, { "jp", "長期記憶力。会話を通じてユーザーの特性を記憶し、会話に反映。" }, { "en", "Long-term memory. It remembers user characteristics and reflects them in conversations." } },
        new Dictionary<string, string> { { "ko", "이미지 인식기능. 지정된 범위 내의 이미지나\n드래그 앤 드롭 된 이미지에 대한 대화가 가능" }, { "jp", "画像認識機能。指定された範囲内の画像や\nドラッグ＆ドロップされた画像に対する会話が可能。" }, { "en", "Image recognition. Enables conversations about images\nwithin a specified range or dragged and dropped images." } },
        new Dictionary<string, string> { { "ko", "음성 인식기능. AI와 마이크를 통해 직접 대화 가능" }, { "jp", "音声認識機能。AIとマイクで直接会話が可能" }, { "en", "Speech recognition. Enables direct conversation with AI via microphone" } },
        new Dictionary<string, string> { { "ko", "최소설정을 통한 빠른 로딩" }, { "jp", "最小設定による高速ローディング" }, { "en", "Fast loading with minimal settings" } },
        new Dictionary<string, string> { { "ko", "기본값. 모든 설정을 활성화" }, { "jp", "初期設定。すべてのオプションを起動" }, { "en", "Default. Enable all options" } },
        new Dictionary<string, string> { { "ko", "커스텀. 선택된 옵션대로 진행" }, { "jp", "カスタム。選択されたオプション通りに進行" }, { "en", "Custom. Proceed with the selected option" } },
        new Dictionary<string, string> { { "ko", "선생님, 이야기할 준비가 되었습니다." }, { "jp", "先生、話す準備はできています。" }, { "en", "I'm ready to talk, sensei." } },
        new Dictionary<string, string> { { "ko", "선생님? 대화를 시작할 수 없어요. 마이크 설정 등을 확인해주실 수 있으실까요?" }, { "jp", "先生？ 会話を始めることができません。マイクの設定などをご確認いただけますか？" }, { "en", "Sensei? I can't start a conversation. Could you please check your microphone settings or something?" } },
        new Dictionary<string, string> { { "ko", "응답에 CPU를 사용" }, { "jp", "応答にはCPUを使用。" }, { "en", "Use CPU for Normal response." } },
        new Dictionary<string, string> { { "ko", "빠른응답을 위해 Nvidia GPU 사용.\n(VRAM 약 8~10GB 필요)" }, { "jp", "高速応答のためにNvidia GPUを使用。\n(VRAM 約8～10GB必要)" }, { "en", "Use Nvidia GPU for fast response.\n(VRAM about 8~10GB needed)" } },
        new Dictionary<string, string> { { "ko", "이미 구동중입니다. 재기동하시겠습니까?" }, { "jp", "すでに稼働中です。再起動しますか？" }, { "en", "It's already running. Do you want to restart it?" } },
        new Dictionary<string, string> { { "ko", "재기동에 실패했습니다. 잠시 후 시도해주세요." }, { "jp", "再起動に失敗しました。 しばらくしてから再試行してください。" }, { "en", "Restart failed, please try again later." } },
        new Dictionary<string, string> { { "ko", "사전 로드된 모델 청소" }, { "jp", "Cleaned Preloaded Models" }, { "en", "Cleaned Preloaded Models" } }, // 일본어 번역 누락
        new Dictionary<string, string> { { "ko", "이미지 입력" }, { "jp", "画像追加" }, { "en", "Input Image" } },
        new Dictionary<string, string> { { "ko", "웹 검색" }, { "jp", "WEB検索" }, { "en", "Web Search" } },
        new Dictionary<string, string> { { "ko", "보내기" }, { "jp", "転送" }, { "en", "Send" } },
        new Dictionary<string, string> { { "ko", "Screenshot area 선택중..." }, { "jp", "Screenshot area 選択中..." }, { "en", "Selecting screenshot area..." } },
        new Dictionary<string, string> { { "ko", "Focus Mode 작동" }, { "jp", "Focus Mode 作動" }, { "en", "Focus mode activated" } },
        new Dictionary<string, string> { { "ko", "Focus Mode 해제" }, { "jp", "Focus Mode 解除" }, { "en", "Focus mode Deactivated" } },
        new Dictionary<string, string> { { "ko", "집중해서 볼게요, 선생님!" }, { "jp", "集中して見ますよ、先生！" }, { "en", "I'll focus on the focus area, sensei!" } },
        new Dictionary<string, string> { { "ko", "집중모드를 해제할게요, 선생님!" }, { "jp", "集中モードを解除します、先生！" }, { "en", "I'll turn off focus mode, sensei!" } },
        new Dictionary<string, string> { { "ko", "삭제" }, { "jp", "削除" }, { "en", "Delete" } },
        new Dictionary<string, string> { { "ko", "수정" }, { "jp", "修正" }, { "en", "Modify" } },
        new Dictionary<string, string> { { "ko", "상세" }, { "jp", "詳細" }, { "en", "Detail" } },
        new Dictionary<string, string> { { "ko", "AI 학습" }, { "jp", "AI学習 " }, { "en", "AI learning" } },
        new Dictionary<string, string> { { "ko", "현재 채팅" }, { "jp", "現在チャット" }, { "en", "Current Chat" } },
        new Dictionary<string, string> { { "ko", "새 채팅" }, { "jp", "新規チャット" }, { "en", "New Chat" } },
        new Dictionary<string, string> { { "ko", "대화 내역에서 학습할 정보를 분석합니다." }, { "jp", "会話履歴から学習する情報を分析します。" }, { "en", "Analyze the information to learn from the conversation history." } },
        new Dictionary<string, string> { { "ko", "대화 내역에서 제목 후보를 추천합니다." }, { "jp", "会話履歴からタイトル候補を推薦します。" }, { "en", "Suggest title suggestions from conversation history." } },

        // UserCard 관련 다국어 지원
        new Dictionary<string, string> { { "ko", "답변은 반드시 3~4문장 정도 길이로만 짧게 답변." }, { "jp", "回答は必ず3〜4文程度の長さで短く回答する。" }, { "en", "Answer must be short, only about 3-4 sentences long." } },
        new Dictionary<string, string> { { "ko", "반드시 답변에 괄호를 넣거나 동작을 묘사하지 않음" }, { "jp", "回答に必ず括弧を入れたり動作を描写しない" }, { "en", "Never use parentheses or describe actions in answers" } },
        new Dictionary<string, string> { { "ko", "모든 문장 끝에 \"~다냥\" 어미 사용" }, { "jp", "すべての文の終わりに\"〜だにゃ\"語尾を使用" }, { "en", "Use \"~danya\" ending at the end of every sentence" } },
        new Dictionary<string, string> { { "ko", "답변할 때마다 관련된 고사성어나 속담 인용" }, { "jp", "回答するたびに関連する四字熟語やことわざを引用" }, { "en", "Quote related idioms or proverbs in every answer" } },
        new Dictionary<string, string> { { "ko", "답변 시작을 항상 \"흠... 그렇다면\"으로 시작" }, { "jp", "回答の開始を常に\"うーん...それなら\"で始める" }, { "en", "Always start answers with \"Hmm... then\"" } },
        new Dictionary<string, string> { { "ko", "어떠한 경우에도 한국어를 유지해야 함" }, { "jp", "いかなる場合でも韓国語を維持しなければならない" }, { "en", "Must maintain Korean language in any case" } }
    };


    // 입력된 단어를 Full Scan하여 목표 언어로 번역합니다.
    // 번역할 단어, 목표 언어 
    public static string Translate(string word, string targetLang)
    {
        foreach (var entry in Texts)
        {
            if (entry.Values.Contains(word))
            {
                return entry.TryGetValue(targetLang, out var translated) ? translated : word;
            }
        }
        return word; // 번역 실패 시 원문 반환
    }
}