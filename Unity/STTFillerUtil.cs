using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;

// 반문하는 로직 만들면 그 때 사용하는 걸로 
// whisper 특유의 Youtube 학습이 공백문자를 마무리 문자로 바꿔서 영상 마지막에 자주 나오는 단어로 나오는 경우가 있음
public class STTFillerUtil
{
    // Singleton instance
    private static STTFillerUtil _instance;
    public static STTFillerUtil Instance => _instance ?? (_instance = new STTFillerUtil());

    // List of regex patterns to match filler words
    private readonly List<string> PATTERNS = new List<string>
    {
        "^Thank you(?:\\.)?$",
        "^Thanks for listening(?:\\.)?$",
        "^That\\'s all(?:\\.)?$",
        "^That\\'s it(?:\\.)?$",
        "^Goodbye(?:\\.)?$",
        "^See you(?:\\.)?$",
        "^ご視聴ありがとうございました(?:。)?$",
        "^ご清聴ありがとうございました(?:。)?$",
        "^ありがとうございました(?:。)?$",
        "^以上です(?:。)?$",
        "^これで終わります(?:。)?$",
        "^お疲れ様でした(?:。)?$",
        "^ご視聴ありがとうございました$",
        "^Thanks for watching!$",
        "^本日はご覧いただきありがとうございます(?:。)?$",
        "^チャンネル登録高評価をよろしくお願いします(?:。)?$"
    };

    // Private constructor to prevent direct instantiation
    private STTFillerUtil() { }

    public bool CheckSTTFiller(string text)
    {
        foreach (string pattern in PATTERNS)
        {
            if (Regex.IsMatch(text, pattern))
            {
                return true;
            }
        }
        return false;
    }
}
