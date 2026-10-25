# 音频目录说明

17 个音频文件已按交付标准接入游戏，但 Unity 运行时需要通过 `Resources` 才能随包可靠加载，因此实际文件位于：

- `Assets/Resources/Audio/SFX/`：14 个音效
- `Assets/Resources/Audio/Music/`：3 首 BGM

文件名与《弹珠机音效交付标准》完全一致。

## 配置

- 主配置：`Assets/StreamingAssets/audio_config.json`
- 打包兜底：`Assets/Resources/Config/audio_config.json`

调整音量、随机 pitch、并发上限、最小间隔、优先级、BGM 循环与淡入淡出时，只改配置，不改代码。

## 代码入口

- `Assets/Scripts/Runtime/Audio/AudioManager.cs`：单例、音效并发 / 间隔 / 优先级、BGM 交叉淡入淡出、全局开关。
- `Assets/Scripts/Runtime/Audio/AudioConfig.cs`：配置结构与加载器。

业务脚本只调用 `AudioManager.Play(id)`、`AudioManager.PlayBgm(id)`，不直接持有 `AudioSource`，也不调用 `PlayOneShot`。
