using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using ApiGeminiMulti;

// 다중 캐릭터 대화용 프롬프트 빌더
// Python prompt_multi.py, prompt_llm.py:get_gemma_multi_prompt 포팅
public static class ApiGeminiMultiPromptBuilder
{
    // Gemma 형식 다중 캐릭터 프롬프트 생성
    public static string BuildGemmaMultiPrompt(MultiConversationRequest request)
    {
        // 참여자가 2명 이하면 단일 캐릭터 프롬프트 사용 (기존 로직 위임)
        if (request.participants == null || request.participants.Count <= 2)
        {
            return ApiGeminiPromptBuilder.BuildGemmaPrompt(
                request.query,
                request.playerName,
                request.targetSpeaker,
                request.memoryList,
                request.aiLanguage,
                request.guidelineList,
                request.situationDict
            );
        }

        // 다중 캐릭터 메시지 리스트 생성
        List<Dictionary<string, string>> messages = BuildMultiCharacterMessages(request);

        // Gemma 포맷으로 조합
        StringBuilder prompt = new StringBuilder();
        prompt.Append("<bos>");

        foreach (var message in messages)
        {
            string role = message["role"];
            string content = message["content"];

            // Gemma는 system을 user로 처리
            if (role == "system")
            {
                prompt.Append(AddGemmaTurn("user", content));
                prompt.Append("\n");
            }
            else if (role == "user")
            {
                prompt.Append(AddGemmaTurn("user", content));
                prompt.Append("\n");
            }
            else if (role == "assistant")
            {
                prompt.Append(AddGemmaTurn("model", content));
                prompt.Append("\n");
            }
        }

        // 응답 시작 토큰 추가
        prompt.Append("<start_of_turn>model\n");

        return prompt.ToString();
    }

    // Gemma 턴 포맷 헬퍼
    private static string AddGemmaTurn(string role, string content)
    {
        return $"<start_of_turn>{role}\n{content}<end_of_turn>";
    }

    // 다중 캐릭터 메시지 리스트 생성 (prompt_multi.get_multi_character_messages 포팅)
    private static List<Dictionary<string, string>> BuildMultiCharacterMessages(MultiConversationRequest request)
    {
        List<Dictionary<string, string>> messages = new List<Dictionary<string, string>>();

        // 타겟 참여자 찾기
        MultiParticipant targetParticipant = null;
        MultiParticipant currentParticipant = null;

        if (!string.IsNullOrEmpty(request.targetSpeaker))
        {
            targetParticipant = request.participants.Find(p => p.name == request.targetSpeaker);
        }
        if (!string.IsNullOrEmpty(request.currentSpeaker))
        {
            currentParticipant = request.participants.Find(p => p.name == request.currentSpeaker);
        }

        // 1. 시스템 프롬프트
        string systemContent = BuildMultiCharacterSystemPrompt(
            request.targetSpeaker,
            request.participants,
            request.aiLanguage,
            request.situationDict,
            request.targetListener
        );
        messages.Add(new Dictionary<string, string> { { "role", "system" }, { "content", systemContent } });

        // 2. 캐릭터 프로필 (타겟 캐릭터)
        if (targetParticipant != null && !string.IsNullOrEmpty(targetParticipant.character_file))
        {
            string charProfile = ApiGeminiCharacterDataManager.Instance.GetCharacterPrompt(
                targetParticipant.character_file,
                request.aiLanguage
            );
            if (!string.IsNullOrEmpty(charProfile))
            {
                string profileLabel = GetLocalizedLabel(request.aiLanguage, "답변 캐릭터 프로필", "回答キャラクタープロフィール", "Responding Character Profile");
                messages.Add(new Dictionary<string, string>
                {
                    { "role", "system" },
                    { "content", $"## {profileLabel}\n{charProfile}" }
                });
            }
        }

        // 3. 유저 프로필
        MultiParticipant userParticipant = request.participants.Find(p => p.type == "user");
        if (userParticipant != null)
        {
            string personaKey = !string.IsNullOrEmpty(userParticipant.name) && userParticipant.name != "sensei"
                ? "kivotos_sensei_player_name"
                : "kivotos_sensei";

            string userProfile = ApiGeminiCharacterDataManager.Instance.GetCharacterPrompt(personaKey, request.aiLanguage);
            if (!string.IsNullOrEmpty(userProfile))
            {
                if (!string.IsNullOrEmpty(request.playerName))
                {
                    userProfile = userProfile.Replace("{player_name}", request.playerName);
                }
                string userLabel = GetLocalizedLabel(request.aiLanguage, "사용자 프로필", "ユーザープロフィール", "User Profile");
                messages.Add(new Dictionary<string, string>
                {
                    { "role", "system" },
                    { "content", $"## {userLabel}\n{userProfile}" }
                });
            }
        }

        // 4. 참여자 관계 정보
        string participantsInfo = BuildParticipantsInfo(request.targetSpeaker, request.participants, request.aiLanguage);
        if (!string.IsNullOrEmpty(participantsInfo))
        {
            messages.Add(new Dictionary<string, string> { { "role", "system" }, { "content", participantsInfo } });
        }

        // 5. 가이드라인
        if (request.guidelineList != null && request.guidelineList.Count > 0)
        {
            string guidelineContent = BuildGuidelineContent(request.guidelineList, request.aiLanguage);
            messages.Add(new Dictionary<string, string> { { "role", "system" }, { "content", guidelineContent } });
        }

        // 6. 메모리 (대화 기록)
        if (request.memoryList != null && request.memoryList.Count > 0)
        {
            foreach (var memory in request.memoryList)
            {
                // 언어별 메시지 선택
                string selectedMessage = SelectMessageByLanguage(memory, request.aiLanguage);
                if (string.IsNullOrEmpty(selectedMessage)) continue;

                string speaker = memory.ContainsKey("speaker") ? memory["speaker"] : "unknown";
                string role = memory.ContainsKey("role") ? memory["role"] : "assistant";

                // display_name 찾기
                string displayName = speaker;
                MultiParticipant participant = request.participants.Find(p => p.name == speaker);
                if (participant != null && !string.IsNullOrEmpty(participant.display_name))
                {
                    displayName = participant.display_name;
                }

                string formattedMessage = $"[{displayName}]: {selectedMessage}";
                messages.Add(new Dictionary<string, string> { { "role", role }, { "content", formattedMessage } });
            }
        }

        // 7. 현재 쿼리 추가
        if (!string.IsNullOrEmpty(request.currentSpeaker) && request.currentSpeaker != request.targetSpeaker)
        {
            if (currentParticipant != null && currentParticipant.type == "user")
            {
                string currentDisplayName = !string.IsNullOrEmpty(currentParticipant.display_name)
                    ? currentParticipant.display_name
                    : request.currentSpeaker;

                string formattedQuery = $"[{currentDisplayName}]: {request.query}";
                messages.Add(new Dictionary<string, string> { { "role", "user" }, { "content", formattedQuery } });
            }
        }

        return messages;
    }

    // 다중 캐릭터 시스템 프롬프트 생성 (prompt_multi.build_multi_character_system_prompt 포팅)
    private static string BuildMultiCharacterSystemPrompt(
        string targetSpeaker,
        List<MultiParticipant> participants,
        string lang,
        Dictionary<string, object> situationDict,
        string targetListener)
    {
        MultiParticipant targetParticipant = participants?.Find(p => p.name == targetSpeaker);
        if (targetParticipant == null)
        {
            targetParticipant = new MultiParticipant
            {
                name = targetSpeaker ?? "unknown",
                display_name = targetSpeaker ?? "Unknown"
            };
        }

        string displayName = targetParticipant.display_name ?? targetSpeaker;

        if (lang == "ko")
        {
            return BuildKoreanSystemPrompt(displayName, targetSpeaker, participants, situationDict, targetListener);
        }
        else if (lang == "ja" || lang == "jp")
        {
            return BuildJapaneseSystemPrompt(displayName, targetSpeaker, participants, situationDict, targetListener);
        }
        else
        {
            return BuildEnglishSystemPrompt(displayName, targetSpeaker, participants, situationDict, targetListener);
        }
    }

    // 한국어 시스템 프롬프트
    private static string BuildKoreanSystemPrompt(
        string displayName,
        string targetSpeaker,
        List<MultiParticipant> participants,
        Dictionary<string, object> situationDict,
        string targetListener)
    {
        StringBuilder sb = new StringBuilder();

        sb.AppendLine($@"# 다중 참여자 대화 시스템

## 핵심 정체성  
당신은 **{displayName}**입니다.
- 다른 사람을 칭할 때는 그들의 이름을 사용하세요
- 자신을 지칭할 때는 ""나"", ""저""를 사용하세요 (절대 자신의 이름을 3인칭으로 사용하지 마세요)");

        // 상황 설정
        if (situationDict != null && situationDict.Count > 0)
        {
            sb.AppendLine("\n## 현재 상황");
            foreach (var kvp in situationDict)
            {
                sb.AppendLine($"- {kvp.Key}: {kvp.Value}");
            }
        }

        // 참여자 정보
        sb.AppendLine("\n## 참여자 정보");
        if (participants != null)
        {
            foreach (var p in participants)
            {
                string roleDesc = p.type == "user" ? "사용자" : "AI 캐릭터";
                string pDisplayName = p.display_name ?? p.name;
                if (p.name == targetSpeaker)
                {
                    sb.AppendLine($"- **{pDisplayName}**: 바로 당신입니다");
                }
                else
                {
                    sb.AppendLine($"- {pDisplayName}: {roleDesc}");
                }
            }
        }

        // 관계별 말투 결정
        string listenerInfo;
        string speechStyle;

        if (targetListener == "sensei")
        {
            listenerInfo = "🎯 **대화 대상**: 선생님에게 개별적으로 말하고 있습니다";
            speechStyle = @"✅ **존댓말 필수**: ""~요"", ""~습니다"", ""~세요"" 등 존댓말 사용
✅ **정중한 표현**: ""안녕하세요"", ""말씀해주세요"", ""도와드리겠습니다"" 등";
        }
        else if (targetListener == "arona" || targetListener == "plana")
        {
            listenerInfo = $"🎯 **대화 대상**: {targetListener}에게 개별적으로 말하고 있습니다 (AI끼리 친한 관계)";
            speechStyle = $@"✅ **친근한 존댓말**: ""{targetListener}""에게는 편안하고 자연스러운 존댓말 사용
✅ **부드러운 표현**: ""그렇네요"", ""좋아요"", ""어떻게 생각하세요?"" 등 친근한 존댓말
✅ **자연스러운 어조**: ""~네요"", ""~죠"", ""~해요"" 등으로 편안하게 대화";
        }
        else
        {
            listenerInfo = "🎯 **대화 대상**: 전체 참여자에게 말하고 있습니다 (선생님 포함)";
            speechStyle = @"✅ **존댓말 필수**: 선생님이 들으므로 ""~요"", ""~습니다"", ""~세요"" 등 존댓말 사용
✅ **정중한 표현**: ""안녕하세요"", ""말씀해주세요"", ""도와드리겠습니다"" 등";
        }

        sb.AppendLine($@"
## 중요한 대화 규칙
1. **정체성 유지**: 당신은 {displayName}입니다
2. **1인칭 사용**: 자신을 ""나"", ""저""로 지칭하세요
3. **상대방 인식**: 대화 상대를 정확한 이름으로 부르세요
4. **연속성 유지**: 이전 대화 맥락을 이어가세요
5. **캐릭터 일관성**: {displayName}의 성격을 유지하세요
6. **중복 방지**: 이전에 말한 내용을 그대로 반복하지 마세요

{listenerInfo}

## 관계별 말투 규칙
{speechStyle}
✅ **캐릭터별 특성 반영**: 
   - 아로나: 밝고 활발한 성격 유지
   - 프라나: 차분하고 신중한 성격 유지
   - 기타 캐릭터: 해당 캐릭터 설정에 맞는 성격 유지

## 🚨 절대 금지 사항 (STRICTLY FORBIDDEN) 🚨
**다음 항목은 절대적으로 금지되며, 어떤 상황에서도 사용해서는 안 됩니다:**

🚫 **인터넷 슬랭/줄임말 ZERO TOLERANCE**: 
   - ""ㅎㅇ"", ""ㅇㅋ"", ""ㅋㅋ"", ""ㄱㄱ"", ""ㅎㅎ"", ""ㄷㄷ"", ""ㅠㅠ"", ""ㅜㅜ"", ""ㅅㄱ"" 등
   - ""어"", ""음"", ""엌"", ""앗"", ""아"", ""오"", ""우와"", ""헉"", ""엥"" 등 의성어/감탄사
   - ""그럼"", ""뭐임"", ""뭔데"", ""왜냐"", ""그냥"", ""걍"", ""쫌"", ""좀"", ""막"" 등 축약어

🚫 **캐주얼 표현 완전 금지**:
   - 반말 사용 (선생님께 절대 금지)
   - ""야"", ""너"", ""니"", ""걔"", ""얘"" 등 격식 없는 지칭
   - ""~함"", ""~임"", ""~지"", ""~네"" 등 반말 어미

🚫 **기타 절대 금지**:
   - 자신의 이름을 3인칭으로 사용 (예: ""아로나가"", ""프라나가"")
   - 다른 캐릭터의 대화 대신 작성
   - 동일한 내용 반복
   - 나레이션이나 상황 설명

⚠️ **위반 시 즉시 응답 중단 및 재생성 요구됩니다**

## ✅ 필수 응답 형식
1. **완전한 표준어 사용**: 모든 단어와 표현을 표준 한국어로 작성
2. **정중한 존댓말**: 선생님께는 ""~습니다"", ""~세요"", ""~께서"" 등 완전한 존댓말만 사용
3. **캐릭터 일관성**: {displayName}의 성격 설정을 100% 준수
4. **자연스러운 대화**: 위 규칙을 지키면서도 자연스럽고 매력적인 캐릭터 표현

⚠️ **이 모든 규칙은 예외 없이 모든 응답에 적용됩니다**");

        return sb.ToString();
    }

    // 일본어 시스템 프롬프트
    private static string BuildJapaneseSystemPrompt(
        string displayName,
        string targetSpeaker,
        List<MultiParticipant> participants,
        Dictionary<string, object> situationDict,
        string targetListener)
    {
        StringBuilder sb = new StringBuilder();

        sb.AppendLine($@"# マルチキャラクター会話システム

## 核心的アイデンティティ
あなたは**{displayName}**です。
- 他の人を呼ぶときは、その人の名前を使ってください
- 自分を指すときは「私」「僕」「俺」を使ってください（絶対に自分の名前を三人称で使わないでください）");

        // 상황 설정
        if (situationDict != null && situationDict.Count > 0)
        {
            sb.AppendLine("\n## 現在の状況");
            foreach (var kvp in situationDict)
            {
                sb.AppendLine($"- {kvp.Key}: {kvp.Value}");
            }
        }

        // 참여자 정보
        sb.AppendLine("\n## 参加者情報");
        if (participants != null)
        {
            foreach (var p in participants)
            {
                string roleDesc = p.type == "user" ? "ユーザー" : "AIキャラクター";
                string pDisplayName = p.display_name ?? p.name;
                if (p.name == targetSpeaker)
                {
                    sb.AppendLine($"- **{pDisplayName}**: まさにあなたです");
                }
                else
                {
                    sb.AppendLine($"- {pDisplayName}: {roleDesc}");
                }
            }
        }

        // 관계별 말투 결정
        string listenerInfo;
        string speechStyle;

        if (targetListener == "sensei")
        {
            listenerInfo = "🎯 **会話対象**: 先生に個別的に話しています";
            speechStyle = @"✅ **敬語必須**: 「です」「ます」「ください」等の敬語使用
✅ **丁寧な表現**: 「おはようございます」「教えてください」「お手伝いします」等";
        }
        else if (targetListener == "arona" || targetListener == "plana")
        {
            listenerInfo = $"🎯 **会話対象**: {targetListener}に個別的に話しています (AI同士の親しい関係)";
            speechStyle = $@"✅ **親しい敬語**: ""{targetListener}""には自然で親しみやすい敬語を使用
✅ **柔らかい表現**: 「そうですね」「いいですね」「どう思いますか？」等の親しい敬語
✅ **自然な語調**: 「〜ですね」「〜ましょう」「〜ですよ」等で親しく会話";
        }
        else
        {
            listenerInfo = "🎯 **会話対象**: 全体参加者に話しています (先生含む)";
            speechStyle = @"✅ **敬語必須**: 先生が聞くので「です」「ます」「ください」等の敬語使用
✅ **丁寧な表現**: 「おはようございます」「教えてください」「お手伝いします」等";
        }

        sb.AppendLine($@"
## 重要な会話ルール
1. **アイデンティティ維持**: あなたは{displayName}です
2. **一人称使用**: 自分を「私」「僕」「俺」で指してください
3. **相手認識**: 会話相手を正確な名前で呼んでください
4. **連続性維持**: 前の会話の文脈を続けてください
5. **キャラクター一貫性**: {displayName}の性格を維持してください
6. **重複防止**: 前に言った内容をそのまま繰り返さないでください

{listenerInfo}

## 関係別話し方ルール
{speechStyle}
✅ **キャラクター別特性反映**:
   - アロナ: 明るく活発な性格維持
   - プラナ: 落ち着いて慎重な性格維持
   - その他キャラクター: 該当キャラクター設定に合う性格維持

## 🚨 絶対禁止事項 (STRICTLY FORBIDDEN) 🚨
🚫 **インターネットスラング/略語 ZERO TOLERANCE**
🚫 **カジュアル表現完全禁止**
🚫 **自分の名前を三人称で使用禁止**

⚠️ **これらすべての規則は例外なくすべての応答に適用されます**");

        return sb.ToString();
    }

    // 영어 시스템 프롬프트
    private static string BuildEnglishSystemPrompt(
        string displayName,
        string targetSpeaker,
        List<MultiParticipant> participants,
        Dictionary<string, object> situationDict,
        string targetListener)
    {
        StringBuilder sb = new StringBuilder();

        sb.AppendLine($@"# Multi-Character Conversation System

## Core Identity
You are **{displayName}**.
- When referring to others, use their names
- When referring to yourself, use ""I"" or ""me"" (never use your own name in third person)");

        // 상황 설정
        if (situationDict != null && situationDict.Count > 0)
        {
            sb.AppendLine("\n## Current Situation");
            foreach (var kvp in situationDict)
            {
                sb.AppendLine($"- {kvp.Key}: {kvp.Value}");
            }
        }

        // 참여자 정보
        sb.AppendLine("\n## Participants");
        if (participants != null)
        {
            foreach (var p in participants)
            {
                string roleDesc = p.type == "user" ? "User" : "AI Character";
                string pDisplayName = p.display_name ?? p.name;
                if (p.name == targetSpeaker)
                {
                    sb.AppendLine($"- **{pDisplayName}**: This is you");
                }
                else
                {
                    sb.AppendLine($"- {pDisplayName}: {roleDesc}");
                }
            }
        }

        // 관계별 말투 결정
        string listenerInfo;
        string speechStyle;

        if (targetListener == "sensei")
        {
            listenerInfo = "🎯 **Conversation Target**: You are speaking individually to Sensei";
            speechStyle = @"✅ **Formal Language Required**: Use polite and respectful language
✅ **Respectful Tone**: Always maintain formal and courteous expressions";
        }
        else if (targetListener == "arona" || targetListener == "plana")
        {
            listenerInfo = $"🎯 **Conversation Target**: You are speaking individually to {targetListener} (friendly AI relationship)";
            speechStyle = $@"✅ **Friendly Polite Tone**: Speak to ""{targetListener}"" with warm and natural politeness
✅ **Gentle Expressions**: Use friendly but polite expressions";
        }
        else
        {
            listenerInfo = "🎯 **Conversation Target**: You are speaking to all participants (including Sensei)";
            speechStyle = @"✅ **Formal Language Required**: Since Sensei is listening, use polite and respectful language
✅ **Respectful Expressions**: Use formal expressions";
        }

        sb.AppendLine($@"
## Important Conversation Rules
1. **Identity Maintenance**: You are {displayName}
2. **First Person Usage**: Refer to yourself as ""I"" or ""me""
3. **Partner Recognition**: Address conversation partners by their correct names
4. **Continuity Maintenance**: Continue the previous conversation context
5. **Character Consistency**: Maintain {displayName}'s personality
6. **Avoid Duplication**: Don't repeat exactly what was said before

{listenerInfo}

## Relationship-Based Speech Rules
{speechStyle}
✅ **Character-Specific Traits**:
   - Arona: Maintain bright and energetic personality
   - Plana: Maintain calm and thoughtful personality

## 🚨 ABSOLUTELY PROHIBITED 🚨
🚫 **Internet Slang/Abbreviations ZERO TOLERANCE**
🚫 **Casual Language COMPLETELY BANNED**
🚫 **Using your own name in third person FORBIDDEN**

⚠️ **ALL THESE RULES APPLY TO EVERY SINGLE RESPONSE WITHOUT EXCEPTION**");

        return sb.ToString();
    }

    // 참여자 관계 정보 생성
    private static string BuildParticipantsInfo(string targetSpeaker, List<MultiParticipant> participants, string lang)
    {
        if (participants == null || participants.Count <= 2) return "";

        MultiParticipant targetParticipant = participants.Find(p => p.name == targetSpeaker);
        if (targetParticipant == null) return "";

        var otherParticipants = participants.FindAll(p => p.name != targetSpeaker);
        if (otherParticipants.Count == 0) return "";

        StringBuilder sb = new StringBuilder();

        if (lang == "ko")
        {
            sb.AppendLine("## 다른 참여자들과의 관계");
        }
        else if (lang == "ja" || lang == "jp")
        {
            sb.AppendLine("## 他の参加者との関係");
        }
        else
        {
            sb.AppendLine("## Relationships with Other Participants");
        }

        foreach (var p in otherParticipants)
        {
            string pDisplayName = p.display_name ?? p.name;
            string roleDesc = p.type == "user"
                ? GetLocalizedLabel(lang, "사용자", "ユーザー", "User")
                : GetLocalizedLabel(lang, "AI 캐릭터", "AIキャラクター", "AI Character");
            sb.AppendLine($"- {pDisplayName}: {roleDesc}");
        }

        return sb.ToString();
    }

    // 가이드라인 컨텐츠 생성
    private static string BuildGuidelineContent(List<string> guidelineList, string lang)
    {
        StringBuilder sb = new StringBuilder();

        if (lang == "ko")
        {
            sb.AppendLine(@"## 🚨 대화 지침 (절대 준수 사항) 🚨
다음은 사용자의 피드백과 선호도를 기반으로 한 **절대적으로 준수해야 할 규칙**입니다.

⚠️ **절대 준수 규칙** ⚠️");
        }
        else if (lang == "ja" || lang == "jp")
        {
            sb.AppendLine(@"## 🚨 会話ガイドライン（絶対遵守事項）🚨
以下はユーザーのフィードバックや好みに基づく、**絶対に守らなければならない規則**です。

⚠️ **絶対遵守規則** ⚠️");
        }
        else
        {
            sb.AppendLine(@"## 🚨 Conversation Guidelines (ABSOLUTE COMPLIANCE REQUIRED) 🚨
The following rules must be **ABSOLUTELY FOLLOWED**.

⚠️ **MANDATORY RULES** ⚠️");
        }

        for (int i = 0; i < guidelineList.Count; i++)
        {
            sb.AppendLine($"{i + 1}. ⚠️ {guidelineList[i].Trim()}");
        }

        return sb.ToString();
    }

    // 언어별 메시지 선택
    private static string SelectMessageByLanguage(Dictionary<string, string> memory, string lang)
    {
        if (lang == "ko")
        {
            return memory.ContainsKey("messageKo") ? memory["messageKo"]
                : memory.ContainsKey("message") ? memory["message"] : "";
        }
        else if (lang == "ja" || lang == "jp")
        {
            return memory.ContainsKey("messageJp") ? memory["messageJp"]
                : memory.ContainsKey("message") ? memory["message"] : "";
        }
        else
        {
            return memory.ContainsKey("messageEn") ? memory["messageEn"]
                : memory.ContainsKey("message") ? memory["message"] : "";
        }
    }

    // 언어별 라벨 반환 헬퍼
    private static string GetLocalizedLabel(string lang, string ko, string ja, string en)
    {
        if (lang == "ko") return ko;
        if (lang == "ja" || lang == "jp") return ja;
        return en;
    }
}
