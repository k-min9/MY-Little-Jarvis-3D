using UnityEngine;
using System.IO;

// Scenario***Manager에서 호출되어 사용되는 코드 Util
public static class ScenarioUtil
{
    // 대사를 보여주고 wav 재생하고 길이 반환.
    public static float Narration(string scenarioId, string dialogue)
    {
        // 현재캐릭터가 아로나일 경우와 아닐경우에서 분기
        string nickname = CharManager.Instance.GetNickname(CharManager.Instance.GetCurrentCharacter());
        if (nickname == "arona")
        {
            // 안내문코드 : I01_installer_check_
            // 안내문 : 선생님. 대화를 위한 기본적인 파일이 설치되어 있지 않아요. 설치를 위한 프로그램을 구동해도 될까요?
            AnswerBalloonSimpleManager.Instance.ShowAnswerBalloonSimpleInf();
            AnswerBalloonSimpleManager.Instance.ModifyAnswerBalloonSimpleText(dialogue);

            // 음성재생
            string file_name = scenarioId + "_ja.wav";
            if (SettingManager.Instance.settings.sound_language == "ko")
            {
                file_name = scenarioId + "_ko.wav";
            }
            else if (SettingManager.Instance.settings.sound_language == "en")
            {
                file_name = scenarioId + "_en.wav";
            }

            string filePath = Path.Combine("Audio", file_name);
            VoiceManager.Instance.PlayWavAudioFromPath(filePath);  // 음성 재생

            // 선택지 보여주기
            float duration = 3f;
            try
            {
                duration = UtilAudio.GetWavDurationInSeconds(filePath);
                Debug.Log(file_name + " 길이 : " + duration);
            }
            catch (System.Exception)
            {

            }
            duration += 0.5f;

            return duration;
        }
        else
        {
            // OperatorManager의 호출
            OperatorManager.Instance.SetBasicPosition();
            OperatorManager.Instance.ShowPortrait(dialogue);  // StatusManager.Instance.IsAnsweringPortrait 때문에 서순 중요
            PortraitBalloonSimpleManager.Instance.Show();
            PortraitBalloonSimpleManager.Instance.ModifyText(dialogue);

            // 음성재생
            string file_name = scenarioId + "_ja.wav";
            if (SettingManager.Instance.settings.sound_language == "ko")
            {
                file_name = scenarioId + "_ko.wav";
            }
            else if (SettingManager.Instance.settings.sound_language == "en")
            {
                file_name = scenarioId + "_en.wav";
            }

            string filePath = Path.Combine("Audio", file_name);
            SubVoiceManager.Instance.PlayWavAudioFromPath(filePath);  // SubVoiceManager

            // 선택지 보여주기
            float duration = 3f;
            try
            {
                duration = UtilAudio.GetWavDurationInSeconds(filePath);
                Debug.Log(file_name + " 길이 : " + duration);
            }
            catch (System.Exception)
            {

            }
            duration += 0.5f;
            PortraitBalloonSimpleManager.Instance.SetHideTimer(duration);
            OperatorManager.Instance.SetHideTimer(duration);
            
            return duration;
        }

        // 무언가 잘못되었을때를 위한 return
        return 3f;
    }
    
    public static void ShowEmotion(string emotion)
    {
        // 현재캐릭터가 아로나일 경우와 아닐경우에서 분기
        string nickname = CharManager.Instance.GetNickname(CharManager.Instance.GetCurrentCharacter());
        if (nickname == "arona")
        {
            EmotionManager.Instance.ShowEmotion(emotion);  // "><"
        }
        else
        { 
            GameObject gameObject = OperatorManager.Instance.GetCurrentOperator();
            Debug.Log(gameObject);
            EmotionManager.Instance.ShowEmotion(emotion, gameObject);
        }
    }
}
