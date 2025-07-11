using System.Collections.Generic;

public static class ChoiceData
{
    public static Dictionary<string, List<Dictionary<string, string>>> Choices = new Dictionary<string, List<Dictionary<string, string>>>
    {
        {
            "X00_test_start", new List<Dictionary<string, string>>
            {
                new Dictionary<string, string> { { "ko", "예" }, { "jp", "はい" }, { "en", "Yes" } },
                new Dictionary<string, string> { { "ko", "아니오" }, { "jp", "いいえ" }, { "en", "No" } },
                new Dictionary<string, string> { { "ko", "취소" }, { "jp", "キャンセル" }, { "en", "Cancel" } }
            }
        },
        {
            "A01_free_server_offer", new List<Dictionary<string, string>>
            {
                new Dictionary<string, string> { { "ko", "응, 다시 해보자" }, { "jp", "うん、もう一度やってみよう" }, { "en", "Yes, let's try again" } },
                new Dictionary<string, string> { { "ko", "아니, 안할래" }, { "jp", "いや、やめておく" }, { "en", "No, I won't" } }
            }
        },
        {
            "A01_1_2_connect_failed", new List<Dictionary<string, string>>
            {
                new Dictionary<string, string> { { "ko", "다시 시도할게" }, { "jp", "もう一度試す" }, { "en", "Try again" } },
                new Dictionary<string, string> { { "ko", "나중에 할래" }, { "jp", "後でにする" }, { "en", "Maybe later" } },
                new Dictionary<string, string> { { "ko", "그만둘래" }, { "jp", "やめる" }, { "en", "Cancel" } }
            }
        },
        {
            "A02_platform_check", new List<Dictionary<string, string>>
            {
                new Dictionary<string, string> { { "ko", "응, 맞아" }, { "jp", "うん、そうだよ" }, { "en", "Yes, that's right" } },
                new Dictionary<string, string> { { "ko", "아니, PC 맞아" }, { "jp", "いや、PCだよ" }, { "en", "No, it is a PC" } },
                new Dictionary<string, string> { { "ko", "필요 없어" }, { "jp", "必要ないよ" }, { "en", "No need" } }
            }
        },
        {
            "A02_1_check_server_status", new List<Dictionary<string, string>>
            {
                new Dictionary<string, string> { { "ko", "연결할 PC가 있어" }, { "jp", "接続するPCがある" }, { "en", "I have a PC to connect" } },
                new Dictionary<string, string> { { "ko", "외부 플랫폼을 사용하려고 해" }, { "jp", "外部プラットフォームを使いたい" }, { "en", "I want to use an external platform" } },
                new Dictionary<string, string> { { "ko", "자세히 설명해줄 수 있어?" }, { "jp", "詳しく説明してくれる？" }, { "en", "Can you explain in detail?" } }
            }
        },
        {
            "A03_inference_select", new List<Dictionary<string, string>>
            {
                new Dictionary<string, string> { { "ko", "이 PC에서 연산할래" }, { "jp", "このPCで計算する" }, { "en", "I'll compute on this PC" } },
                new Dictionary<string, string> { { "ko", "외부 서버를 사용할래" }, { "jp", "外部サーバーを使う" }, { "en", "I'll use an external server" } },
                new Dictionary<string, string> { { "ko", "그만둘래" }, { "jp", "やめておく" }, { "en", "Cancel it" } }
            }
        },
        {
            "A03_1_1_cuda_supported", new List<Dictionary<string, string>>
            {
                new Dictionary<string, string> { { "ko", "GPU를 사용" }, { "jp", "GPUを使う" }, { "en", "Use GPU" } },
                new Dictionary<string, string> { { "ko", "CPU만으로 사용" }, { "jp", "CPUだけ使う" }, { "en", "Use CPU only" } },
                new Dictionary<string, string> { { "ko", "취소할래" }, { "jp", "キャンセルする" }, { "en", "Cancel" } }
            }
        },
        {
            "A03_1_2_cuda_not_supported", new List<Dictionary<string, string>>
            {
                new Dictionary<string, string> { { "ko", "진행할게" }, { "jp", "進めるよ" }, { "en", "Continue" } },
                new Dictionary<string, string> { { "ko", "취소할래" }, { "jp", "キャンセルする" }, { "en", "Cancel" } }
            }
        },
        {
            "A04_external_server_select", new List<Dictionary<string, string>>
            {
                new Dictionary<string, string> { { "ko", "무료 서버 연결" }, { "jp", "無料サーバーに接続" }, { "en", "Connect to free server" } },
                new Dictionary<string, string> { { "ko", "API 키 입력할래" }, { "jp", "APIキーを入力する" }, { "en", "Enter API key" } },
                new Dictionary<string, string> { { "ko", "설정 취소할래" }, { "jp", "設定をキャンセルする" }, { "en", "Cancel setup" } }
            }
        },
        {
            "A04_1_1_connect_failed", new List<Dictionary<string, string>>
            {
                new Dictionary<string, string> { { "ko", "다시 시도" }, { "jp", "再試行する" }, { "en", "Try again" } },
                new Dictionary<string, string> { { "ko", "나중에 할래" }, { "jp", "あとにする" }, { "en", "Later" } },
                new Dictionary<string, string> { { "ko", "그만둘래" }, { "jp", "やめる" }, { "en", "Cancel" } }
            }
        },
        {
            "A04_2_api_key_input", new List<Dictionary<string, string>>
            {
                new Dictionary<string, string> { { "ko", "Gemini" }, { "jp", "Gemini" }, { "en", "Gemini" } },
                new Dictionary<string, string> { { "ko", "OpenRouter" }, { "jp", "OpenRouter" }, { "en", "OpenRouter" } },
                new Dictionary<string, string> { { "ko", "전 선택지로" }, { "jp", "前の選択肢に戻る" }, { "en", "Back to previous choices" } },

                new Dictionary<string, string> { { "ko", "ChatGPT" }, { "jp", "ChatGPT" }, { "en", "ChatGPT" } },  // TODO : ChatGPT
            }
        },
        {
            "A97_connect_test_retry", new List<Dictionary<string, string>>
            {
                new Dictionary<string, string> { { "ko", "다시 시도" }, { "jp", "再試行する" }, { "en", "Try again" } },
                new Dictionary<string, string> { { "ko", "다음에 해볼게" }, { "jp", "また今度にする" }, { "en", "Maybe next time" } }
            }
        },
        {
            "I01_installer_check", new List<Dictionary<string, string>>
            {
                new Dictionary<string, string> { { "ko", "응" }, { "jp", "はい" }, { "en", "Yes" } },
                new Dictionary<string, string> { { "ko", "아니" }, { "jp", "いいえ" }, { "en", "No" } }
            }
        }
    };
}
