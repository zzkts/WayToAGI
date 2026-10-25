using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace Pachinko
{
    /// <summary>
    /// 音频统一入口。业务脚本只调用 AudioManager.Play(id) / PlayBgm(id)，
    /// 不持有 AudioSource，也不直接调用 PlayOneShot。
    /// 文件名、音量、pitch、并发上限、优先级与 BGM 淡入淡出全部来自 audio_config.json，
    /// 增删音效只需要改配置和放入 Resources/Audio 对应目录。
    /// </summary>
    [DefaultExecutionOrder(-400)]
    public class AudioManager : MonoBehaviour
    {
        public const string SfxEnabledPrefKey = "pachinko.audio.sfx";
        public const string BgmEnabledPrefKey = "pachinko.audio.bgm";

        private class Voice
        {
            public AudioSource source;
            public string id;
            public int priority;
            public double startedAt;
        }

        private static AudioManager _instance;

        private AudioConfig _config;
        private readonly Dictionary<string, SfxDef> _sfxDefs = new Dictionary<string, SfxDef>(StringComparer.Ordinal);
        private readonly Dictionary<string, BgmDef> _bgmDefs = new Dictionary<string, BgmDef>(StringComparer.Ordinal);
        private readonly Dictionary<string, AudioClip> _sfxClips = new Dictionary<string, AudioClip>(StringComparer.Ordinal);
        private readonly Dictionary<string, AudioClip> _bgmClips = new Dictionary<string, AudioClip>(StringComparer.Ordinal);
        private readonly Dictionary<string, double> _lastPlayedAt = new Dictionary<string, double>(StringComparer.Ordinal);
        private readonly List<Voice> _voices = new List<Voice>();
        private readonly List<AudioSource> _sfxSources = new List<AudioSource>();
        private readonly List<string> _missingFiles = new List<string>();
        private readonly HashSet<string> _warnedIds = new HashSet<string>(StringComparer.Ordinal);

        private AudioSource _bgmA;
        private AudioSource _bgmB;
        private AudioSource _activeBgm;
        private BgmDef _activeBgmDef;
        private string _requestedBgmId;
        private Coroutine _bgmRoutine;
        private int _bgmVersion;
        private bool _sfxEnabled = true;
        private bool _bgmEnabled = true;

        public static AudioManager Instance => _instance;
        public static bool SfxEnabled => _instance == null || _instance._sfxEnabled;
        public static bool BgmEnabled => _instance == null || _instance._bgmEnabled;
        public string ConfigSource { get; private set; }

        public static IReadOnlyList<string> MissingFiles
        {
            get
            {
                return _instance != null
                    ? (IReadOnlyList<string>)_instance._missingFiles
                    : Array.Empty<string>();
            }
        }

        /// <summary>由 GameBootstrap 在 UI 创建前调用；跨场景保留同一个实例。</summary>
        public static AudioManager EnsureExists()
        {
            if (_instance != null) return _instance;

            var existing = FindFirstObjectByType<AudioManager>();
            if (existing != null)
            {
                _instance = existing;
                return existing;
            }

            var go = new GameObject("[AudioManager]");
            return go.AddComponent<AudioManager>();
        }

        private void Awake()
        {
            if (_instance != null && _instance != this)
            {
                Destroy(gameObject);
                return;
            }

            _instance = this;
            DontDestroyOnLoad(gameObject);
            LoadPlayerSettings();
            Initialize();
        }

        private void OnDestroy()
        {
            if (_instance == this) _instance = null;
        }

        private void LoadPlayerSettings()
        {
            _sfxEnabled = PlayerPrefs.GetInt(SfxEnabledPrefKey, 1) != 0;
            _bgmEnabled = PlayerPrefs.GetInt(BgmEnabledPrefKey, 1) != 0;
        }

        private void Initialize()
        {
            string source;
            _config = AudioConfigLoader.Load(out source);
            ConfigSource = source;
            if (_config == null) return;

            LoadDefinitions();
            BuildSources();
            LogSummary();
        }

        private void LoadDefinitions()
        {
            if (_config.sfx != null)
            {
                foreach (var def in _config.sfx)
                {
                    if (def == null) continue;
                    if (string.IsNullOrEmpty(def.id) || string.IsNullOrEmpty(def.file))
                    {
                        Debug.LogWarning("[AudioManager] 音效配置缺少 id 或 file，条目已跳过。");
                        continue;
                    }
                    if (_sfxDefs.ContainsKey(def.id))
                    {
                        Debug.LogWarning($"[AudioManager] 音效 ID 重复，后续条目已跳过：{def.id}");
                        continue;
                    }

                    _sfxDefs.Add(def.id, def);
                    var clip = LoadClip(def.file, "SFX");
                    if (clip != null) _sfxClips.Add(def.id, clip);
                }
            }

            if (_config.bgm != null)
            {
                foreach (var def in _config.bgm)
                {
                    if (def == null) continue;
                    if (string.IsNullOrEmpty(def.id) || string.IsNullOrEmpty(def.file))
                    {
                        Debug.LogWarning("[AudioManager] BGM 配置缺少 id 或 file，条目已跳过。");
                        continue;
                    }
                    if (_bgmDefs.ContainsKey(def.id))
                    {
                        Debug.LogWarning($"[AudioManager] BGM ID 重复，后续条目已跳过：{def.id}");
                        continue;
                    }

                    _bgmDefs.Add(def.id, def);
                    var clip = LoadClip(def.file, "Music");
                    if (clip != null) _bgmClips.Add(def.id, clip);
                }
            }
        }

        private AudioClip LoadClip(string fileName, string category)
        {
            string resourceName = Path.GetFileNameWithoutExtension(fileName);
            var clip = Resources.Load<AudioClip>($"Audio/{category}/{resourceName}");
            if (clip != null) return clip;

            if (!_missingFiles.Contains(fileName)) _missingFiles.Add(fileName);
            Debug.LogWarning($"[AudioManager] 缺少音频文件：{fileName}");
            return null;
        }

        private void BuildSources()
        {
            int voiceCount = Mathf.Clamp(_config.voicePoolSize > 0 ? _config.voicePoolSize : 16, 4, 32);
            var sfxRoot = new GameObject("SFXVoices").transform;
            sfxRoot.SetParent(transform, false);
            for (int i = 0; i < voiceCount; i++)
            {
                var go = new GameObject("SFXVoice_" + i);
                go.transform.SetParent(sfxRoot, false);
                _sfxSources.Add(CreateSource(go));
            }

            var bgmRoot = new GameObject("BGM").transform;
            bgmRoot.SetParent(transform, false);

            var a = new GameObject("BGM_A");
            a.transform.SetParent(bgmRoot, false);
            _bgmA = CreateSource(a);

            var b = new GameObject("BGM_B");
            b.transform.SetParent(bgmRoot, false);
            _bgmB = CreateSource(b);
        }

        private static AudioSource CreateSource(GameObject go)
        {
            var source = go.AddComponent<AudioSource>();
            source.playOnAwake = false;
            source.loop = false;
            source.spatialBlend = 0f;
            source.dopplerLevel = 0f;
            source.bypassReverbZones = true;
            source.priority = 32;
            source.volume = 1f;
            return source;
        }

        private void LogSummary()
        {
            if (_missingFiles.Count == 0)
            {
                Debug.Log($"[AudioManager] 音频检查完成：{_sfxClips.Count} 个音效 / {_bgmClips.Count} 首 BGM 全部就绪（{ConfigSource}）。");
                return;
            }

            Debug.LogWarning($"[AudioManager] 缺失文件清单（{_missingFiles.Count}）：{string.Join("、", _missingFiles)}");
        }

        // ---------- 对外统一入口 ----------

        public static void Play(string id)
        {
            if (_instance != null) _instance.PlaySfx(id);
        }

        public static void PlayBgm(string id)
        {
            if (_instance != null) _instance.PlayBgmInternal(id);
        }

        public static void StopBgm()
        {
            if (_instance != null) _instance.StopBgmInternal();
        }

        public static void SetSfxEnabled(bool enabled)
        {
            if (_instance != null) _instance.SetSfxEnabledInternal(enabled);
        }

        public static void SetBgmEnabled(bool enabled)
        {
            if (_instance != null) _instance.SetBgmEnabledInternal(enabled);
        }

        // ---------- 音效 ----------

        private void PlaySfx(string id)
        {
            if (!_sfxEnabled || string.IsNullOrEmpty(id)) return;
            if (!_sfxDefs.TryGetValue(id, out var def))
            {
                WarnUnknown("音效", id);
                return;
            }
            if (!_sfxClips.TryGetValue(id, out var clip) || clip == null) return;

            double now = Time.unscaledTimeAsDouble;
            if (_lastPlayedAt.TryGetValue(id, out var last) &&
                (now - last) * 1000.0 < Mathf.Max(0, def.minIntervalMs))
            {
                return;
            }

            PruneFinishedVoices();

            // 同一音效的并发上限：满了直接丢弃新触发，不排队。
            int active = 0;
            Voice lowest = null;
            foreach (var voice in _voices)
            {
                if (voice.id != id) continue;
                active++;
                if (lowest == null || voice.priority < lowest.priority ||
                    (voice.priority == lowest.priority && voice.startedAt < lowest.startedAt))
                {
                    lowest = voice;
                }
            }

            if (active >= Mathf.Max(1, def.maxConcurrent))
            {
                if (lowest == null || def.priority <= lowest.priority) return;
                StopVoice(lowest);
            }

            var source = AcquireSource(def.priority);
            if (source == null) return;

            _lastPlayedAt[id] = now;
            source.clip = clip;
            source.loop = false;
            source.pitch = PickPitch(def);
            source.volume = AudioConfig.LinearVolume(
                def.volumeDb + _config.sfxGroupVolumeDb + _config.masterVolumeDb);
            source.Play();

            _voices.Add(new Voice
            {
                source = source,
                id = id,
                priority = def.priority,
                startedAt = now
            });
        }

        private AudioSource AcquireSource(int priority)
        {
            foreach (var source in _sfxSources)
            {
                if (!source.isPlaying) return source;
            }

            // 对象池也被占满时，只有更高优先级才能抢走最低优先级的发声位。
            Voice candidate = null;
            foreach (var voice in _voices)
            {
                if (candidate == null || voice.priority < candidate.priority ||
                    (voice.priority == candidate.priority && voice.startedAt < candidate.startedAt))
                {
                    candidate = voice;
                }
            }

            if (candidate == null || priority <= candidate.priority) return null;
            var stolen = candidate.source;
            StopVoice(candidate);
            return stolen;
        }

        private void PruneFinishedVoices()
        {
            for (int i = _voices.Count - 1; i >= 0; i--)
            {
                var voice = _voices[i];
                if (voice == null || voice.source == null || !voice.source.isPlaying)
                {
                    if (voice != null && voice.source != null) voice.source.clip = null;
                    _voices.RemoveAt(i);
                }
            }
        }

        private void StopVoice(Voice voice)
        {
            if (voice == null) return;
            if (voice.source != null)
            {
                voice.source.Stop();
                voice.source.clip = null;
            }
            _voices.Remove(voice);
        }

        private void StopAllSfx()
        {
            foreach (var source in _sfxSources)
            {
                if (source == null) continue;
                source.Stop();
                source.clip = null;
            }
            _voices.Clear();
        }

        private static float PickPitch(SfxDef def)
        {
            float min = Mathf.Max(0.01f, def.pitchMin);
            float max = Mathf.Max(min, def.pitchMax);
            return Mathf.Approximately(min, max) ? min : UnityEngine.Random.Range(min, max);
        }

        private void SetSfxEnabledInternal(bool enabled)
        {
            if (_sfxEnabled == enabled) return;
            _sfxEnabled = enabled;
            PlayerPrefs.SetInt(SfxEnabledPrefKey, enabled ? 1 : 0);
            PlayerPrefs.Save();
            if (!enabled) StopAllSfx();
        }

        private void WarnUnknown(string kind, string id)
        {
            if (!_warnedIds.Add(kind + ":" + id)) return;
            Debug.LogWarning($"[AudioManager] 配置中不存在{kind} ID：{id}");
        }

        // ---------- BGM ----------

        private void PlayBgmInternal(string id)
        {
            if (string.IsNullOrEmpty(id)) return;
            if (!_bgmDefs.TryGetValue(id, out var def))
            {
                WarnUnknown("BGM", id);
                return;
            }
            if (!_bgmClips.TryGetValue(id, out var clip) || clip == null) return;

            bool sameTrack = _requestedBgmId == id;
            _requestedBgmId = id;

            // 关闭 BGM 时仍记录目标曲目，重新打开后从该曲目恢复。
            if (!_bgmEnabled) return;
            if (sameTrack && _activeBgm != null && _activeBgm.isPlaying) return;

            StartBgm(def, clip);
        }

        private void StartBgm(BgmDef def, AudioClip clip)
        {
            var outgoing = _activeBgm;
            var incoming = outgoing == _bgmA ? _bgmB : _bgmA;
            if (incoming == null) return;

            int outgoingMs = outgoing != null && _activeBgmDef != null
                ? Mathf.Max(0, _activeBgmDef.fadeOutMs)
                : 0;
            int incomingMs = Mathf.Max(0, def.fadeInMs);
            float outgoingStart = outgoing != null ? outgoing.volume : 0f;
            float target = AudioConfig.LinearVolume(
                def.volumeDb + _config.bgmGroupVolumeDb + _config.masterVolumeDb);

            _bgmVersion++;
            int version = _bgmVersion;
            if (_bgmRoutine != null)
            {
                StopCoroutine(_bgmRoutine);
                _bgmRoutine = null;
            }

            if (incoming.isPlaying) incoming.Stop();
            incoming.clip = clip;
            incoming.loop = def.loop;
            incoming.pitch = 1f;
            incoming.volume = incomingMs > 0 ? 0f : target;
            incoming.Play();

            // 先切换活动引用，后续 PlayBgm 立即以新曲目为准。
            _activeBgm = incoming;
            _activeBgmDef = def;

            if (outgoingMs <= 0 && incomingMs <= 0)
            {
                StopSource(outgoing);
                incoming.volume = target;
                return;
            }

            _bgmRoutine = StartCoroutine(Crossfade(outgoing, outgoingStart, outgoingMs,
                incoming, target, incomingMs, version));
        }

        private IEnumerator Crossfade(AudioSource outgoing, float outgoingStart, int outgoingMs,
            AudioSource incoming, float target, int incomingMs, int version)
        {
            float duration = Mathf.Max(outgoingMs, incomingMs) / 1000f;
            float t = 0f;
            while (t < duration)
            {
                if (version != _bgmVersion) yield break;
                t += Time.unscaledDeltaTime;

                if (outgoing != null)
                {
                    outgoing.volume = outgoingMs > 0
                        ? outgoingStart * (1f - Mathf.Clamp01(t * 1000f / outgoingMs))
                        : 0f;
                }
                if (incoming != null)
                {
                    incoming.volume = target * (incomingMs > 0
                        ? Mathf.Clamp01(t * 1000f / incomingMs)
                        : 1f);
                }
                yield return null;
            }

            if (version != _bgmVersion) yield break;
            StopSource(outgoing);
            if (incoming != null) incoming.volume = target;
            _bgmRoutine = null;
        }

        private void StopBgmInternal()
        {
            _requestedBgmId = null;
            StopBgmSources();
        }

        private void StopBgmSources()
        {
            _bgmVersion++;
            if (_bgmRoutine != null)
            {
                StopCoroutine(_bgmRoutine);
                _bgmRoutine = null;
            }
            StopSource(_bgmA);
            StopSource(_bgmB);
            _activeBgm = null;
            _activeBgmDef = null;
        }

        private static void StopSource(AudioSource source)
        {
            if (source == null) return;
            source.Stop();
            source.clip = null;
            source.volume = 0f;
        }

        private void SetBgmEnabledInternal(bool enabled)
        {
            if (_bgmEnabled == enabled) return;
            _bgmEnabled = enabled;
            PlayerPrefs.SetInt(BgmEnabledPrefKey, enabled ? 1 : 0);
            PlayerPrefs.Save();

            if (!enabled)
            {
                StopBgmSources();
                return;
            }

            if (!string.IsNullOrEmpty(_requestedBgmId)) PlayBgmInternal(_requestedBgmId);
        }
    }
}
