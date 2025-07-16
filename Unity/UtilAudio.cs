using System;
using System.IO;
using UnityEngine;

public static class UtilAudio
{

    public static float RESULTWHENERROR = 3f;  // 길이 모르겠으면 기본값은 3

    // WAV 파일 길이 반환
    public static float GetWavDurationInSeconds(string filePath)
    {
        // wav는 streamingAsset내에만 보관할 것
        string fullPath = Path.Combine(Application.streamingAssetsPath, filePath);
        if (!File.Exists(fullPath))
        {
            Debug.LogWarning($"[UtilAudio] 파일 없음: {fullPath}");
            return RESULTWHENERROR;  
        }

        try
        {
            using (FileStream fs = File.OpenRead(fullPath))
            using (BinaryReader reader = new BinaryReader(fs))
            {
                // "RIFF" 헤더 확인
                string riff = new string(reader.ReadChars(4));
                if (riff != "RIFF") return RESULTWHENERROR;

                reader.ReadInt32(); // 전체 크기
                string wave = new string(reader.ReadChars(4));
                if (wave != "WAVE") return RESULTWHENERROR;

                // "fmt " 청크 탐색
                while (new string(reader.ReadChars(4)) != "fmt ")
                {
                    reader.BaseStream.Seek(-3, SeekOrigin.Current);
                }

                int fmtChunkSize = reader.ReadInt32();
                int audioFormat = reader.ReadInt16();
                int numChannels = reader.ReadInt16();
                int sampleRate = reader.ReadInt32();
                int byteRate = reader.ReadInt32();
                reader.ReadInt16(); // blockAlign
                int bitsPerSample = reader.ReadInt16();

                // "data" 청크 탐색
                while (new string(reader.ReadChars(4)) != "data")
                {
                    reader.BaseStream.Seek(-3, SeekOrigin.Current);
                }

                int dataSize = reader.ReadInt32();

                float duration = (float)dataSize / byteRate;
                return duration;
            }
        }
        catch (Exception e)
        {
            Debug.LogError($"[UtilAudio] WAV 길이 계산 실패: {e.Message}");
            return RESULTWHENERROR;
        }
    }
}
