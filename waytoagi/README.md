# 弹珠机 V3.0 · 无限白球 · 动态循环 · 百万通关

单模式、无止境的物理弹珠游戏。玩家一次拉杆发射整组标准弹珠，通过手机倾斜连续改变重力方向，把弹珠送进奖励洞刷出同洞连击；每次循环结束后自动进入兑换，用可用积分解锁附魔、购买额外弹珠，或解锁引爆技巧与减少挡板。累计得分达到 **1,000,000** 即通关，之后进入无尽模式。

入口场景：`Assets/Scenes/Main.unity`  
引擎：Unity **6000.3.8f1**，URP 2D  
平台：Android 竖屏优先，APK 输出 `Builds/Android/PachinkoMVP.apk`；Windows 试玩包 `Builds/Windows/PachinkoMVP.exe`

## 核心循环

```text
发射：一次拉杆，弹簧连续回弹，整组弹珠全部出膛
  ↓
控球：左右倾斜手机，持续改变重力横向分量
  ↓
落洞：弹珠倍率 × 奖励洞倍率 × 洞口基础积分 × 同洞连击
  ↓
本循环的齐射次数用完后判定门槛
  ↓
自动弹出兑换：附魔 / 额外弹珠 / 引爆 / 减少挡板
  ↓
达标 → 下一循环；未达标 → 同一循环重试
  ↓
累计得分达到 1,000,000 → 通关面板 → 无尽模式
```

### V3 固定规则

- **标准弹珠无限供应**：白色标准弹珠不购买、不消耗库存，每次齐射基础装填 3 颗，可在兑换中永久增加，最多 16 颗。
- **一次拉杆全部发射**：出口常开，弹簧连续回弹；整组弹珠共享同一个重力方向。
- **动态循环**：每次循环随机 2~3 次齐射，随循环序号缓慢增加，封顶 6 次；玩家无法预判循环何时结束。
- **随机奖励洞**：底部 8 个固定洞位中，4 个奖励洞与 4 个空穴在每次齐射前重新分布；奖励倍率与基础分随奖励洞一起洗牌。
- **自动兑换**：循环结束立即弹出兑换入口，不打断弹珠下落。
- **低痛失败**：未达门槛只显示「本循环未达标」与差额，累计得分、已解锁附魔、额外弹珠与兑换进度全部保留，立即重试。
- **两个积分池**：累计得分只用于通关判定且只增不减；可用积分用于兑换，得分按 1:1 同时进入两个池。
- **通关不等于结束**：累计得分达到 1,000,000 后继续累积，阈值继续提高。

## 得分公式

```text
基础分 = 弹珠倍率 × 奖励洞倍率 × 奖励洞基础积分
本次得分 = 基础分 × 同洞连击倍率
```

| 项目 | 规则 |
| --- | --- |
| 弹珠倍率 | 无附魔 ×1；赤红附魔 ×2；湛蓝附魔 ×5 |
| 奖励洞 | 每次齐射前从 8 个固定洞位中随机选 4 个，倍率 ×1/×3/×8/×12 与基础积分 50/120/260/300 随机落位 |
| 空穴 | 其余 4 个洞位接住弹珠但不给分，并中断连击 |
| 同洞连击 | 连续命中同一个物理洞位（该洞位本轮为奖励洞）：×1 → ×2 → ×4 → ×8，封顶 ×8 |
| 连击中断 | 换到别的洞位、落入空穴或漏球都会归零 |
| 积分 | 得分按 1:1 计入可用积分；兑换只扣可用积分 |

> **废案处理**：策划案 5.2 的三洞表（×1/1000、×3/5000、×5/20000）已标记为废案，本实现按要求保留原有 8 洞布局（4 奖励洞 + 4 空穴）。配置中的 8 个位置是固定洞位，奖励/空穴只是模板，实际奖励位置每次齐射前随机重排。策划案 8.2 的算例引用的是废案洞表，因此仅作为公式示例，不改变实际奖励模板。

## 附魔、额外弹珠与技能

### 附魔（永久，两档）

| 档位 | 颜色 | 弹珠倍率 | 解锁价格 | 表现 |
| --- | --- | --- | --- | --- |
| 标准白球 | 冷白 | ×1 | 初始 | 白色弹珠 + 冷白彗星拖尾 |
| 赤红附魔 | 暖红 | ×2 | 8,000 | 红色弹珠 + 暖红彗星拖尾 |
| 湛蓝附魔 | 青蓝 | ×5 | 16,000 | 蓝色弹珠 + 青蓝彗星拖尾 |

价格按 `8,000 × 2.0^已解锁数量` 递增。附魔永久生效，失败重试不会丢失。

### 额外弹珠（永久）

- 基础每次齐射 **3 颗**；每购买一次永久 **+1 颗**，最多 **16 颗**。
- 价格按 `5,000 × 1.6^已购买数量` 递增：第 1 颗 5,000、第 2 颗 8,000、第 3 颗 12,800……
- 增加弹珠会线性提高单次齐射的得分上限，也会让引爆更容易触发；兑换面板会显示当前齐射数量与下一颗价格。
- 达到 16 颗后按钮显示「已达上限」，不再扣积分。

### 引爆技巧

- 解锁价格：**30,000** 可用积分。
- 每颗弹珠累计碰撞 **10 次**后立即引爆，计数归零重新累计。
- 以弹珠为中心，对半径 **120px（1.2 世界单位）**内的其他飞行弹珠施加 **力度 8** 的推力。
- 多颗弹珠同时在场时价值最高，是主动清场与改变落点的手段。

### 挡板

- 初始 **1 块**，最多配置 **3 块**；每次花 **12,000** 可用积分移除 1 块，最低 0 块。
- 挡板为运动学刚体，以 **2.0 秒**周期左右往返，总行程 **120px（±60px）**。
- 挡板不会被弹珠撞飞，只把弹珠弹开；减少挡板可降低台面复杂度。

## 动态循环与门槛

| 循环 | 最低得分门槛 |
| --- | --- |
| 第 1~4 循环 | 3,000 |
| 第 5~10 循环 | 15,000 |
| 第 11 循环起 | 每循环 ×1.08 递增 |

- 每次循环的齐射次数在 `2~3` 之间随机，每 4 个循环上限 +1，封顶 6 次。
- 循环达标：推进到下一循环。
- 未达标：同一循环号重试，已获得的累计得分与可用积分不扣除。
- 累计得分达到 **1,000,000**：显示通关面板；关闭后继续当前循环或进入兑换，随后进入无尽模式。
- 通关后所有循环都视为达标，门槛继续增长，但不再阻止玩家继续游玩。

## 操作

| 动作 | 手机 | 电脑 |
| --- | --- | --- |
| 开始 / 继续 | 点按主按钮 | 鼠标点击 |
| 蓄力发射 | 按住右侧弹簧推杆向下拖，松手 | 鼠标拖拽 |
| 连续控球 | 左右倾斜手机 | `A` / `D` 或 `←` / `→`，或按住屏幕左右半屏 |
| 循环兑换 | 点按兑换卡片按钮 | 鼠标点击 |
| 设置 / 校准 | 右上角「设置」 | 同左 |
| 关闭兑换 | 「完成兑换」按钮或 `Esc` | 同左 |

## 音乐与音效

音频由 `AudioManager` 单例统一播放，业务脚本只调用 `AudioManager.Play(id)` / `PlayBgm(id)`，不持有 `AudioSource`，也不直接调用 `PlayOneShot`。

| 事件 | 音效 ID |
| --- | --- |
| 弹珠落入奖励洞 | `score_base` + `combo_01`～`combo_05`（按同洞连击等级同时播放） |
| 推杆发射 | `launch` |
| 撞钉 / 撞挡板 | `hit_peg` / `hit_baffle` |
| 引爆 | `explode` |
| 循环未达标 / 百万通关 | `fail` / `win` |
| 按钮点击 / 兑换成功 | `ui_click` / `ui_unlock` |
| 主菜单 / 玩法 / 兑换 BGM | `menu_loop` / `main_loop` / `shop_loop` |

- **文件位置**：`Assets/Resources/Audio/SFX` 与 `Assets/Resources/Audio/Music`。Unity 需要通过 `Resources` 或直接引用才能把音频可靠打进包内，因此 17 个文件从交付目录 `Assets/Audio` 移到了 `Resources`；文件名与交付标准完全一致，原交付说明仍保留在 `Assets/Audio/弹珠机音效交付标准.md`。
- **配置**：`Assets/StreamingAssets/audio_config.json`，并镜像到 `Assets/Resources/Config/audio_config.json`。改音量、pitch、并发、优先级、淡入淡出只改配置，不改代码。
- **导入设置**：音效为 `Decompress On Load` + `Preload Audio Data` + `Vorbis`；BGM 为 `Streaming`、不预加载。
- **播放规则**：全部 2D（`Spatial Blend = 0`）；同一音效在最小间隔内重复触发直接丢弃；撞钉最多 4 路、撞挡板最多 2 路；对象池满时高优先级音效抢占低优先级。
- **缺失文件**：静默跳过，控制台输出 `[AudioManager] 缺少音频文件：<文件名>`，并汇总缺失清单，不中断游戏。
- **开关**：设置页新增「音乐 BGM / 音效 SFX」两个开关，状态写入 PlayerPrefs，重启游戏后保留。
- **音量组**：Master / BGM / SFX 三个逻辑组由配置的 `masterVolumeDb`、`bgmGroupVolumeDb`、`sfxGroupVolumeDb` 控制。BGM 每条配置本身已比 SFX 低约 10 dB，默认组增益为 0，避免重复衰减；需要整体再压低 BGM 时把 `bgmGroupVolumeDb` 改成 `-10` 即可。

## 工程结构

```text
Assets/
  Audio/
    弹珠机音效交付标准.md     音频外包规格与电平表
    README.md                 运行时文件位置说明
  Editor/
    MvpSceneBuilder.cs       场景生成 / 数值自检入口 / Windows 与 Android 打包
    MvpSelfTest.cs           V3 数值、计分、循环、兑换、额外弹珠、存档、音频配置自检
  Scenes/Main.unity
  Scripts/Runtime/
    Audio/
      AudioConfig.cs          音频配置结构、StreamingAssets / Resources 加载
      AudioManager.cs         音效并发与优先级、BGM 淡入淡出、全局开关
    Config/
      GameConfig.cs          物理、经济、循环、引爆、挡板、附魔与洞口配置
    Core/
      GameManager.cs         发射 → 结算 → 循环判定 → 兑换 → 下一循环/重试
      LoopManager.cs         随机循环长度、循环阈值、百万目标进度
      ScoreRules.cs          同洞连击与积分 1:1 规则
      ScoreManager.cs        弹珠倍率 × 洞口倍率 × 基础分 × 连击
      ShopManager.cs         附魔 / 额外弹珠 / 引爆 / 减少挡板四项永久兑换
      MagazineBuilder.cs     基础弹珠与额外弹珠装填
      SaveSystem.cs          V6 存档、旧存档迁移、齐射容量、通关与兑换进度
      TiltController.cs      重力仪、校准、键盘/触屏降级
    Gameplay/
      MarbleController.cs    弹珠外观/拖尾、碰撞计数、引爆推力
      Baffle.cs              运动学挡板往返移动
      SpringLauncher.cs      一拉全发弹簧
      RewardHole.cs          奖励洞与空穴触发
      HoleRandomizer.cs      每次齐射前重排奖励洞与空穴
      HoleVisual.cs          可重绑定的洞口颜色、光晕与积分标签
      OneWayLaneGate.cs      发射道单向闸门
    Presentation/
      BoardBuilder.cs        机柜、半圆边框、钉阵、8 个洞位、3 块移动挡板
      Theme.cs               UI 与台面视觉主题
    UI/
      HUDController.cs       分数卡、弹珠卡、循环面板、设置入口
      HUDShop.cs             循环结算与四类兑换
      HUDOverlays.cs         主菜单、通关、设置、得分飘字
      UIFactory.cs           代码构建 UI 的工厂
    DebugTools/
      RuntimeDiagnostics.cs  -autoplay 自动验收与截图
      GatePhysicsProbe.cs    -gate-test 闸门物理验收
  StreamingAssets/
    game_config.json
    audio_config.json
  Resources/
    Config/
      game_config.json
      audio_config.json
    Audio/
      SFX/                   14 个音效
      Music/                 3 首 BGM
Tools/
  Android/
    LaunchUnityAndroid.bat  设置纯英文 TEMP / GRADLE_USER_HOME 后启动 Unity
    BuildAndroidBatch.bat   关闭 Unity 后命令行构建 APK
    aliyun-mirrors.gradle   Gradle 阿里云镜像初始化脚本备份
    README.md               本机 Android 工具链与构建说明
Docs/
  验收记录.md
操作说明.md
```

## 数值配置

所有可调数值都在 `Assets/StreamingAssets/game_config.json`，并镜像到 `Assets/Resources/Config/game_config.json`：

- `economy`：每次齐射基础弹珠数、额外弹珠价格与递增系数、初始挡板数、附魔价格与递增系数。
- `loop`：循环长度范围、每几轮增长、三档门槛、百万目标。
- `explosion`：解锁价格、触发碰撞次数、半径、推力。
- `baffle`：最大数量、移除价格、总幅度、周期。
- `balls`：唯一的标准白球。
- `enchants`：赤红 ×2、湛蓝 ×5 及颜色。
- `holes`：8 个固定洞位的坐标，以及 4 个奖励模板 / 4 个空穴模板的倍率、基础积分、颜色。运行时奖励模板会在每次齐射前随机落到不同洞位。

## 构建与验证

### Android APK（可直接安装）

本机已配置外部工具链，路径写在 Unity 的 **Edit → Preferences → External Tools**：

| 工具 | 路径 / 版本 |
| --- | --- |
| OpenJDK | `C:\Program Files\Eclipse Adoptium\jdk-17.0.20.101-hotspot`（17.0.20.1） |
| Android SDK | `D:\Android\Sdk`（platform-tools 37.0.1、build-tools 36.0.0、platforms 34/35/36） |
| Android NDK | `D:\Android\Sdk\ndk\27.2.12479018`（r27c） |
| CMake | `D:\Android\Sdk\cmake\3.22.1` |

构建脚本固定以下参数：包名 `com.waytoagi.pachinko.mvp`、竖屏锁定、**IL2CPP + ARM64**、`minSdk 25`、`targetSdk 36`；未配置 keystore 时使用 Unity 调试签名。执行 **Pachinko → 4. 构建 Android APK**。不要用 **File → Build Profiles** 里的 `Build` 按钮，它会绕过构建脚本对包名、架构、签名和工具链的检查；即使直接点，`AndroidBuildGuard` 也会先设置构建环境并检查工具链。

> **中文用户名必读**：本机用户目录是 `C:\Users\刘诚毅`。AGP 会为 prefab 生成 `.bat`，把 Gradle 缓存和临时目录写进批处理；cmd 按 GBK 解析 UTF-8 中文路径时会破坏引号和反斜杠，构建会报 `'Eclipse' 不是内部或外部命令` 或 prefab 失败。因此构建前需要把 `TEMP`、`TMP`、`GRADLE_USER_HOME` 指到纯英文路径。当前 `BuildAndroid()` 与 `AndroidBuildGuard` 会在构建前自动调用 `ConfigureAndroidBuildEnvironment()` 完成设置，并把阿里云镜像脚本补齐到 `D:\Android\gradle-home\init.d`；以下脚本保留为兜底：
>
> - `Tools/Android/LaunchUnityAndroid.bat`：先设置环境变量再打开 Unity。
> - `Tools/Android/BuildAndroidBatch.bat`：关闭 Unity 后双击，命令行构建，日志写到 `D:\Android\unity-android-build.log`。
>
> Gradle 依赖通过 `D:\Android\gradle-home\init.d\aliyun-mirrors.gradle` 优先走阿里云 Maven 镜像（google / public / gradle-plugin），不依赖代理；镜像失效时改这个文件即可（`Tools/Android/aliyun-mirrors.gradle` 是备份）。

产物：`Builds/Android/PachinkoMVP.apk`（当前 50 MB，2026-10-25 14:51 重建，含当前 K6 数值与额外弹珠升级；`arm64-v8a`，Android Debug 签名）

安装：

```bat
adb install -r Builds/Android/PachinkoMVP.apk
```

也可以把 APK 复制到手机，在系统设置中允许“安装未知应用”后直接安装。调试签名 APK 只适合测试；正式发布需要创建自己的 keystore，Google Play 还需要 AAB，本项目当前只输出 APK。

命令行构建（必须先指定 `-buildTarget Android`）：

```bat
Unity.exe -batchmode -nographics -projectPath "D:\unity_projects\WayToAGI\waytoagi" ^
  -buildTarget Android ^
  -executeMethod PachinkoEditor.MvpSceneBuilder.RunBuildAndroidBatch ^
  -logFile "path\android-build.log"
```

> 当前 APK 仅包含 ARM64。若手机安装时报 `INSTALL_FAILED_NO_MATCHING_ABIS`，说明设备只支持 32 位，需要在 Player Settings 中同时勾选 **ARMv7 + ARM64** 后重新构建。
>
> 当前工程没有配置 Android 应用图标，构建会使用 Unity 默认图标；替换图标不影响本次 APK 导出。
>
> **`JDK not found` 已解决**：Unity 不再使用安装目录里缺失的嵌入式 OpenJDK，而是在 **External Tools** 中指向外部 Adoptium JDK 17。构建入口的 `EnsureAndroidTools()` 仍会先检查 JDK / SDK / NDK，缺失时直接列出具体路径。`Builds/Android/InstallAndroidModules.bat` 仅作为旧环境的重装备选，当前无需运行。
>
> **非致命警告**：Android 构建会提示 `Active Input Handling is set to Both`。项目代码只使用旧版 `Input`，当前 `Both` 仍可构建和运行；正式发布前可在 **Project Settings → Player → Other Settings → Active Input Handling** 改为 **Input Manager (Old)**，Unity 会要求重启编辑器。

### 编辑器与 Windows

- Unity 菜单：**Pachinko → 1. 生成 MVP 场景** / **2. 运行数值自检** / **3. 构建 Windows 试玩包** / **4. 构建 Android APK**
- 命令行自检：`PachinkoEditor.MvpSceneBuilder.RunSelfTestBatch`
- 命令行 Windows 构建：`PachinkoEditor.MvpSceneBuilder.RunBuildWindowsBatch`
- 命令行 Android 构建：`PachinkoEditor.MvpSceneBuilder.RunBuildAndroidBatch`
- Windows 包：`Builds/Windows/PachinkoMVP.exe`

> 项目已在 Unity 6000.3.8f1 中完成脚本编译；本轮额外弹珠功能的 `MagazineBuilder` 曾缺少 `using UnityEngine`，已修复并重新导入编译通过（仅 `HUDController._settingsOpen` 的 `CS0414` 未使用字段警告）。`AudioManager`、音频配置与自检代码均已编译。音频静态检查已通过：17/17 文件存在、配置两份一致、SFX / BGM 导入设置符合交付标准、业务脚本未直接持有 `AudioSource`。Android APK 已用当前源码于 2026-10-25 14:51 重建（50 MB），包内 `game_config.json` 与工程一致，包含本轮门槛/价格与额外弹珠升级，并通过 `aapt2` / `apksigner` / `zipalign` 校验（包名、minSdk 25、targetSdk 36、arm64-v8a、Debug 签名、对齐）。V3 数值自检与音频试听尚未运行，真机安装也尚未执行。本轮数值重整已用离线模拟校准（按基础 3 颗/齐射约 23 个循环达到 1,000,000）；若同时自动购买额外弹珠（5,000 × 1.6），模拟约 13 个循环即可通关，说明额外弹珠会明显降低难度，价格与 16 颗上限仍待真机调参。`MvpSceneBuilder` 的 Android 构建环境修复已由 Unity 于 14:52 重新编译，尚无报错；但编辑器菜单构建尚未完整复跑，本次 APK 是使用同一套环境变量直接执行 Gradle 生成的。

## V2 已移除的系统

为避免旧规则残留，以下系统已在 V3 中删除：

- 弹珠购买、库存、解锁、付费弹珠与分裂弹；
- 奖励洞升级与洞口专属技能；
- 连锁、狂热、头奖、余烬、赏金、全中奖励、护盾、磁引、弹跳；
- 里程碑、固定阶段过场、三页签商店；
- 旋转挡板与旧 `UpgradeCatalog`。

## 已知边界

- 基础齐射 3 颗，可消耗可用积分永久 +1 至 16 颗；策划案把多球数量列为待定项，5.2 废案的三洞表未实现。额外弹珠首价 5,000、每颗 ×1.6；自动购买时离线模拟约 13 个循环达到 1,000,000，会明显降低中后期难度，价格与上限仍需真机调参。
- 保留原有 8 洞的 50/120/260/300 基础分：单球收益低于策划案 8.1 的 1,000~100,000 量级，但本轮已按实际得分分布下调门槛与兑换价格（1~4 循环 3,000、5~10 循环 15,000、11 循环起 ×1.08；附魔 8,000/16,000、额外弹珠 5,000×1.6、引爆 30,000、减少挡板 12,000），按基础 3 颗/齐射离线模拟约 23 个循环达到 1,000,000。真机得分分布仍需实测后再微调。
- 挡板初始 1 块（策划案建议首版先放一个），可在兑换中减到 0；配置最多 3 块。底部左右导流挡板（`LeftFunnel` / `RightFunnel`）已按需求移除，只保留中部 3 块周期移动挡板。
- 奖励洞每次齐射前重新随机分布：8 个固定洞位中随机选 4 个奖励洞，倍率与基础分随奖励模板洗牌；同洞连击按物理洞位计算。`MvpSelfTest` 已在隔离工程副本中通过 **89/89**（含随机分布三项断言）；随机分布、洞口视觉切换与移除底部挡板后的边缘落球尚未做 Play / 真机验证。高倍率洞可能落在任意洞位，实际收益分布会偏离按固定洞位做的离线模拟，门槛可能需要重新采样。
- Android APK 已重建为 `Builds/Android/PachinkoMVP.apk`（**48.7 MB**，2026-10-25 15:32，含随机奖励洞、底部挡板移除、K6 数值与额外弹珠升级），并通过 `aapt2` / `apksigner` / `zipalign` 校验；包名 `com.waytoagi.pachinko.mvp`、minSdk 25、targetSdk 36、arm64-v8a、Android Debug 签名，包内配置与工程一致，IL2CPP 元数据含 `HoleRandomizer` / `HoleVisual` / `SlotX`。当前没有连接 Android 设备，尚未执行 `adb install` 与真机运行验证。
- 中文用户名路径限制：`C:\Users\刘诚毅` 会让 AGP 生成的 prefab 批处理在中文 `TEMP` 下失败。构建入口现在会自动设置 `TEMP` / `TMP` / `GRADLE_USER_HOME`；仍可用 `Tools/Android/LaunchUnityAndroid.bat` 启动 Unity，或关闭 Unity 后运行 `Tools/Android/BuildAndroidBatch.bat` 作为兜底。
- 当前 Android 产物为 **ARM64 + 调试签名**；正式上架需要自定义 keystore，Google Play 还需要 AAB。
- Android 应用图标尚未配置，构建使用 Unity 默认图标。
- 真实 Android 加速度计方向与校准尚未实测。
- 引爆推力与挡板手感需要真机调参。
- 17 个音频文件与播放逻辑已接入，但尚未实际试听验证；落洞 / 连击叠音、撞钉并发、BGM 切换与设置开关需要在编辑器或真机跑一遍。
- 没有创建 `AudioMixer.mixer` 资产：Master / BGM / SFX 三组由 `AudioManager` 按配置以源码增益实现并统一管理；若后续需要接 DSP 效果或把混音暴露给音频设计师，再补 `.mixer`。
- 音频文件放在 `Resources` 下会整体打进包体，当前 17 个文件约 7.7 MB；若包体吃紧，可改为 Addressables 或按需加载。
