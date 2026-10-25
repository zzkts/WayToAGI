using System;
using System.IO;
using UnityEngine;

namespace Pachinko
{
    /// <summary>单条音效配置。字段名与 audio_config.json 一一对应。</summary>
    [Serializable]
    public class SfxDef
    {
        public string id;
        public string file;
        public float volumeDb;
        public float pitchMin = 1f;
        public float pitchMax = 1f;
        public int maxConcurrent = 1;
        public int minIntervalMs;
        public int priority;
    }

    /// <summary>单条 BGM 配置。循环、淡入淡出时长均由配置决定。</summary>
    [Serializable]
    public class BgmDef
    {
        public string id;
        public string file;
        public float volumeDb;
        public bool loop = true;
        public int fadeInMs;
        public int fadeOutMs;
    }

    /// <summary>
    /// 音频总配置。Master / BGM / SFX 三个逻辑音量组在代码中实现：
    /// 每条音效的最终增益 = 配置 dB + 所在组 dB + Master dB。
    /// 交付表中的 BGM 电平（-12 / -14）已经比 SFX（-2 / -1）低约 10 dB，
    /// 因此 BGM 组默认不再额外衰减，避免重复压低。
    /// </summary>
    [Serializable]
    public class AudioConfig
    {
        public float masterVolumeDb;
        public float bgmGroupVolumeDb;
        public float sfxGroupVolumeDb;
        public int voicePoolSize = 16;
        public SfxDef[] sfx;
        public BgmDef[] bgm;

        public static float LinearVolume(float db)
        {
            return Mathf.Clamp01(Mathf.Pow(10f, db / 20f));
        }
    }

    /// <summary>
    /// 优先读取 StreamingAssets（桌面 / 编辑器可直接改文件调参），
    /// Android 等平台回退到 Resources 内置配置。
    /// </summary>
    public static class AudioConfigLoader
    {
        public const string FileName = "audio_config.json";
        private const string ResourcePath = "Config/audio_config";

        public static AudioConfig Load(out string source)
        {
            string streamingPath = Path.Combine(Application.streamingAssetsPath, FileName);
            try
            {
                if (File.Exists(streamingPath))
                {
                    var streaming = JsonUtility.FromJson<AudioConfig>(File.ReadAllText(streamingPath));
                    if (IsUsable(streaming))
                    {
                        source = streamingPath;
                        return streaming;
                    }
                    Debug.LogWarning($"[AudioManager] 音频配置缺少 sfx / bgm 数组，尝试 Resources 配置：{streamingPath}");
                }
            }
            catch (Exception e)
            {
                Debug.LogWarning($"[AudioManager] StreamingAssets 音频配置读取失败，将继续尝试 Resources：{e.Message}");
            }

            var asset = Resources.Load<TextAsset>(ResourcePath);
            if (asset == null)
            {
                source = null;
                Debug.LogError("[AudioManager] 找不到音频配置：StreamingAssets/audio_config.json 与 Resources/Config/audio_config.json 均不可用。");
                return null;
            }

            try
            {
                var fallback = JsonUtility.FromJson<AudioConfig>(asset.text);
                if (IsUsable(fallback))
                {
                    source = "Resources/Config/audio_config.json";
                    return fallback;
                }
            }
            catch (Exception e)
            {
                Debug.LogError($"[AudioManager] Resources 音频配置解析失败：{e.Message}");
            }

            source = null;
            Debug.LogError("[AudioManager] 音频配置无有效内容，音频系统不会播放。");
            return null;
        }

        private static bool IsUsable(AudioConfig config)
        {
            return config != null && config.sfx != null && config.bgm != null;
        }
    }
}
