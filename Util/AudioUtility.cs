using System;
using System.IO;
using BepInEx;
using UnityEngine;
using UnityEngine.Networking;

namespace ReadyCompany.Util
{
    // TODO: This should get cleaned up
    public static class AudioUtility
    {
        public static AudioClip LoadFromDiskToAudioClip(string path, AudioType type)
        {
            AudioClip clip = null!;
            using var uwr = UnityWebRequestMultimedia.GetAudioClip(path, type);
            uwr.SendWebRequest();

            // we have to wrap tasks in try/catch, otherwise it will just fail silently
            try
            {
                while (!uwr.isDone)
                {
                }

                if (uwr.result != UnityWebRequest.Result.Success)
                    ReadyCompany.Logger.LogError(
                        $"Failed to load WAV AudioClip from path: {path} Full error: {uwr.error}");
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

        public static AudioClip GetAudioClip(string modFolder, string soundName)
        {
            return GetAudioClip(modFolder, string.Empty, soundName);
        }

        public static AudioClip GetAudioClip(string modFolder, string subFolder, string soundName)
        {
            AudioType audioType;

            string[] parts = soundName.Split('.');
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
                    $"Failed to detect file type of a sound file! This may cause issues with other mod functionality. Sound defaulted to WAV. Sound: {soundName}");
            }

            return GetAudioClip(modFolder, subFolder, soundName, audioType);
        }

        public static AudioClip GetAudioClip(string modFolder, string soundName, AudioType audioType)
        {
            return GetAudioClip(modFolder, string.Empty, soundName, audioType);
        }

        public static AudioClip GetAudioClip(string modFolder, string subFolder, string soundName, AudioType audioType)
        {
            var tryLoading = true;
            var skipLegacyCheck = false;
            var legacy = " ";

            // path stuff
            var path = Path.Combine(Paths.PluginPath, modFolder, subFolder, soundName);
            var pathOmitSubDir = Path.Combine(Paths.PluginPath, modFolder, soundName);
            var pathDir = Path.Combine(Paths.PluginPath, modFolder, subFolder);

            var pathLegacy = Path.Combine(Paths.PluginPath, subFolder, soundName);
            var pathDirLegacy = Path.Combine(Paths.PluginPath, subFolder);

            // check if file and directory are valid, else skip loading
            if (!Directory.Exists(pathDir))
            {
                if (!string.IsNullOrEmpty(subFolder))
                    ReadyCompany.Logger.LogWarning(
                        $"Requested directory at BepInEx/Plugins/{modFolder}/{subFolder} does not exist!");
                else
                {
                    ReadyCompany.Logger.LogWarning(
                        $"Requested directory at BepInEx/Plugins/{modFolder} does not exist!");
                    if (!modFolder.Contains("-"))
                        ReadyCompany.Logger.LogWarning(
                            $"This sound mod might not be compatable with mod managers. You should contact the sound mod's author.");
                }

                tryLoading = false;
            }
            else
            {
                ReadyCompany.Logger.LogDebug("Skipping legacy path check...");
                skipLegacyCheck = true;
            }

            if (!File.Exists(path))
            {
                ReadyCompany.Logger.LogWarning($"Requested audio file does not exist at path {path}!");
                tryLoading = false;

                ReadyCompany.Logger.LogDebug($"Looking for audio file from mod root instead at {pathOmitSubDir}...");
                if (File.Exists(pathOmitSubDir))
                {
                    ReadyCompany.Logger.LogDebug($"Found audio file at path {pathOmitSubDir}!");
                    path = pathOmitSubDir;
                    tryLoading = true;
                    ReadyCompany.Logger.LogDebug("Skipping legacy path check...");
                    skipLegacyCheck = true;
                }
                else
                {
                    ReadyCompany.Logger.LogWarning(
                        $"Requested audio file does not exist at mod root path {pathOmitSubDir}!");
                }
            }
            else
            {
                ReadyCompany.Logger.LogDebug("Skipping legacy path check...");
                skipLegacyCheck = true;
            }

            if (Directory.Exists(pathDirLegacy) && !skipLegacyCheck)
            {
                if (!string.IsNullOrEmpty(subFolder))
                    ReadyCompany.Logger.LogWarning($"Legacy directory location at BepInEx/Plugins/{subFolder} found!");
                else if (!modFolder.Contains("-"))
                    ReadyCompany.Logger.LogWarning($"Legacy directory location at BepInEx/Plugins found!");
            }

            if (File.Exists(pathLegacy) && !skipLegacyCheck)
            {
                ReadyCompany.Logger.LogWarning($"Legacy path contains the requested audio file at path {pathLegacy}!");
                legacy = " legacy ";
                path = pathLegacy;
                tryLoading = true;
            }

            switch (audioType)
            {
                case AudioType.WAV:
                    ReadyCompany.Logger.LogDebug($"File defined as a WAV file!");
                    break;
                case AudioType.OGGVORBIS:
                    ReadyCompany.Logger.LogDebug($"File defined as an Ogg Vorbis file!");
                    break;
                case AudioType.MPEG:
                    ReadyCompany.Logger.LogDebug($"File defined as a MPEG MP3 file!");
                    break;
                default:
                    ReadyCompany.Logger.LogDebug($"File type not defined and was defaulted to WAV file!");
                    break;
            }

            AudioClip result = null!;

            if (tryLoading)
            {
                ReadyCompany.Logger.LogDebug($"Loading AudioClip {soundName} from{legacy}path: {path}");

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

                ReadyCompany.Logger.LogDebug($"Finished loading AudioClip {soundName} with length of {result.length}!");
            }
            else
            {
                ReadyCompany.Logger.LogWarning(
                    $"Failed to load AudioClip {soundName} from invalid{legacy}path at {path}!");
            }

            // Workaround to ensure the clip always gets named because for some reason Unity doesn't always get the name and leaves it blank sometimes???
            if (string.IsNullOrEmpty(result.GetName()))
            {
                var finalName = string.Empty;
                string[] nameParts = [];

                switch (audioType)
                {
                    case AudioType.WAV:

                        finalName = soundName.Replace(".wav", "");

                        nameParts = finalName.Split('/');

                        if (nameParts.Length <= 1)
                        {
                            nameParts = finalName.Split('\\');
                        }

                        finalName = nameParts[^1];

                        result.name = finalName;
                        break;
                    case AudioType.OGGVORBIS:
                        finalName = soundName.Replace(".ogg", "");

                        nameParts = finalName.Split('/');

                        if (nameParts.Length <= 1)
                        {
                            nameParts = finalName.Split('\\');
                        }

                        finalName = nameParts[^1];

                        result.name = finalName;
                        break;
                    case AudioType.MPEG:
                        finalName = soundName.Replace(".mp3", "");

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

            if (result != null)
            {
                var clipName = result.GetName();

                //if (clipTypes.ContainsKey(clipName))
                //{
                //    clipTypes[clipName] = audioType;
                //}
                //else
                //{
                //    clipTypes.Add(clipName, audioType);
                //}
            }

            // return the clip we got
            return result;
        }

        public static AudioClip GetAudioClip(string path)
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

            AudioClip result = null!;

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

            ReadyCompany.Logger.LogDebug($"Finished loading AudioClip {fileName} with length of {result.length}!");

            // Workaround to ensure the clip always gets named because for some reason Unity doesn't always get the name and leaves it blank sometimes???
            if (string.IsNullOrEmpty(result.GetName()))
            {
                var finalName = string.Empty;
                string[] nameParts = [];

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

            if (result != null)
            {
                var clipName = result.GetName();

                //if (clipTypes.ContainsKey(clipName))
                //{
                //    clipTypes[clipName] = audioType;
                //}
                //else
                //{
                //    clipTypes.Add(clipName, audioType);
                //}
            }

            // return the clip we got
            return result;
        }
    }
}