using System;
using System.IO;
using UnityEngine;
using UnityEngine.Networking;

namespace ReadyCompany.Util
{
    public static class AudioUtility
    {
        public static AudioClip? LoadFromDiskToAudioClip(string path, AudioType type)
        {
            AudioClip? clip = null;
            using var uwr = UnityWebRequestMultimedia.GetAudioClip(path, type);
            uwr.SendWebRequest();

            // we have to wrap tasks in try/catch, otherwise it will just fail silently
            try
            {
                while (!uwr.isDone)
                {
                }

                if (uwr.result != UnityWebRequest.Result.Success)
                    ReadyCompany.Logger.LogError($"Failed to load WAV AudioClip from path: {path} Full error: {uwr.error}");
                else
                {
                    clip = DownloadHandlerAudioClip.GetContent(uwr);
                }
            }
            catch (Exception err)
            {
                ReadyCompany.Logger.LogError($"{err.Message}, {err.StackTrace}");
            }

            return clip;
        }

        public static AudioClip? GetAudioClip(string path)
        {
            var fileName = Path.GetFileName(path);
            AudioType audioType;

            string[] parts = path.Split('.');
            if (parts[^1].ToLower().Contains("wav"))
            {
                audioType = AudioType.WAV;
                ReadyCompany.Logger.LogDebug($"File detected as a PCM WAVE file!");
            }
            else if (parts[^1].ToLower().Contains("ogg"))
            {
                audioType = AudioType.OGGVORBIS;
                ReadyCompany.Logger.LogDebug($"File detected as an Ogg Vorbis file!");
            }
            else if (parts[^1].ToLower().Contains("mp3"))
            {
                audioType = AudioType.MPEG;
                ReadyCompany.Logger.LogDebug($"File detected as a MPEG MP3 file!");
            }
            else
            {
                audioType = AudioType.WAV;
                ReadyCompany.Logger.LogWarning(
                    $"Failed to detect file type of a sound file! This may cause issues with other mod functionality. Sound defaulted to WAV. Sound: {fileName}");
            }

            AudioClip? result = null!;

            ReadyCompany.Logger.LogDebug($"Loading AudioClip path: {path}");

            switch (audioType)
            {
                case AudioType.WAV:
                    result = LoadFromDiskToAudioClip(path, AudioType.WAV);
                    break;
                case AudioType.OGGVORBIS:
                    result = LoadFromDiskToAudioClip(path, AudioType.OGGVORBIS);
                    break;
                case AudioType.MPEG:
                    result = LoadFromDiskToAudioClip(path, AudioType.MPEG);
                    break;
            }

            ReadyCompany.Logger.LogDebug($"Finished loading AudioClip {fileName} with length of {result?.length}!");
            
            if (result == null)
                return result;

            // Workaround to ensure the clip always gets named because for some reason Unity doesn't always get the name and leaves it blank sometimes???
            if (string.IsNullOrEmpty(result.GetName()))
            {
                string finalName;
                string[] nameParts;

                switch (audioType)
                {
                    case AudioType.WAV:

                        finalName = fileName.Replace(".wav", "");

                        nameParts = finalName.Split('/');

                        if (nameParts.Length <= 1)
                        {
                            nameParts = finalName.Split('\\');
                        }

                        finalName = nameParts[^1];

                        result.name = finalName;
                        break;
                    case AudioType.OGGVORBIS:
                        finalName = fileName.Replace(".ogg", "");

                        nameParts = finalName.Split('/');

                        if (nameParts.Length <= 1)
                        {
                            nameParts = finalName.Split('\\');
                        }

                        finalName = nameParts[^1];

                        result.name = finalName;
                        break;
                    case AudioType.MPEG:
                        finalName = fileName.Replace(".mp3", "");

                        nameParts = finalName.Split('/');

                        if (nameParts.Length <= 1)
                        {
                            nameParts = finalName.Split('\\');
                        }

                        finalName = nameParts[^1];

                        result.name = finalName;
                        break;
                }
            }

            // return the clip we got
            return result;
        }
    }
}