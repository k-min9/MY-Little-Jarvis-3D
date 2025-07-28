# UITranslator

1. LanguageManager에 변경할 UI등록
2. UIManager에서 GameObject연결
3. LanguageData에서 아래 format으로 작성

    ``` csharp
    new Dictionary<string, string> { { "ko", "중력 적용" }, { "jp", "重力を適用" }, { "en", "Apply Gravity" } },
    ```

4. SetUILanguage에 format대로 UI Update 코드 작성
5. 테스트
