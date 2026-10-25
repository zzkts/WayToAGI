# 弹珠机 V3.0 · 无限白球 · 动态循环 · 百万通关

单模式、无止境的物理弹珠游戏。玩家一次拉杆发射整组标准弹珠，通过手机倾斜连续改变重力方向，把弹珠送进奖励洞刷出同洞连击；每次循环结束后自动进入兑换，用可用积分解锁附魔、引爆技巧或减少挡板。累计得分达到 **1,000,000** 即通关，之后进入无尽模式。

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
自动弹出兑换：附魔 / 引爆 / 减少挡板
  ↓
达标 → 下一循环；未达标 → 同一循环重试
  ↓
累计得分达到 1,000,000 → 通关面板 → 无尽模式
```

### V3 固定规则

- **标准弹珠无限供应**：白色标准弹珠不购买、不消耗库存，每次齐射固定装填 3 颗。
- **一次拉杆全部发射**：出口常开，弹簧连续回弹；整组弹珠共享同一个重力方向。
- **动态循环**：每次循环随机 2~3 次齐射，随循环序号缓慢增加，封顶 6 次；玩家无法预判循环何时结束。
- **自动兑换**：循环结束立即弹出兑换入口，不打断弹珠下落。
- **低痛失败**：未达门槛只显示「本循环未达标」与差额，累计得分、已解锁附魔与兑换进度全部保留，立即重试。
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
| 奖励洞 | 1/3/5/7 号洞为奖励洞，倍率 ×1/×3/×8/×12，基础积分 50/120/260/300 |
| 空穴 | 2/4/6/8 号洞接住弹珠但不给分，并中断连击 |
| 同洞连击 | 连续命中同一奖励洞：×1 → ×2 → ×4 → ×8，封顶 ×8 |
| 连击中断 | 换到别的奖励洞、落入空穴或漏球都会归零 |
| 积分 | 得分按 1:1 计入可用积分；兑换只扣可用积分 |

> **废案处理**：策划案 5.2 的三洞表（×1/1000、×3/5000、×5/20000）已标记为废案，本实现按要求保留原有 8 洞布局（4 奖励洞 + 4 空穴）。策划案 8.2 的算例引用的是废案洞表，因此仅作为公式示例，不改变实际洞口配置。

## 附魔与技能

### 附魔（永久，两档）

| 档位 | 颜色 | 弹珠倍率 | 解锁价格 | 表现 |
| --- | --- | --- | --- | --- |
| 标准白球 | 冷白 | ×1 | 初始 | 白色弹珠 + 冷白彗星拖尾 |
| 赤红附魔 | 暖红 | ×2 | 10,000 | 红色弹珠 + 暖红彗星拖尾 |
| 湛蓝附魔 | 青蓝 | ×5 | 16,000 | 蓝色弹珠 + 青蓝彗星拖尾 |

价格按 `10,000 × 1.6^已解锁数量` 递增。附魔永久生效，失败重试不会丢失。

### 引爆技巧

- 解锁价格：**50,000** 可用积分。
- 每颗弹珠累计碰撞 **10 次**后立即引爆，计数归零重新累计。
- 以弹珠为中心，对半径 **120px（1.2 世界单位）**内的其他飞行弹珠施加 **力度 8** 的推力。
- 多颗弹珠同时在场时价值最高，是主动清场与改变落点的手段。

### 挡板

- 初始 **1 块**，最多配置 **3 块**；每次花 **20,000** 可用积分移除 1 块，最低 0 块。
- 挡板为运动学刚体，以 **2.0 秒**周期左右往返，总行程 **120px（±60px）**。
- 挡板不会被弹珠撞飞，只把弹珠弹开；减少挡板可降低台面复杂度。

## 动态循环与门槛

| 循环 | 最低得分门槛 |
| --- | --- |
| 第 1~3 循环 | 5,000 |
| 第 4~10 循环 | 20,000 |
| 第 11 循环起 | 每循环 ×1.3 递增 |

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

## 工程结构

```text
Assets/
  Editor/
    MvpSceneBuilder.cs       场景生成 / 数值自检入口 / Windows 与 Android 打包
    MvpSelfTest.cs           V3 数值、计分、循环、兑换、存档自检
  Scenes/Main.unity
  Scripts/Runtime/
    Config/
      GameConfig.cs          物理、经济、循环、引爆、挡板、附魔与洞口配置
    Core/
      GameManager.cs         发射 → 结算 → 循环判定 → 兑换 → 下一循环/重试
      LoopManager.cs         随机循环长度、循环阈值、百万目标进度
      ScoreRules.cs          同洞连击与积分 1:1 规则
      ScoreManager.cs        弹珠倍率 × 洞口倍率 × 基础分 × 连击
      ShopManager.cs         附魔 / 引爆 / 减少挡板三项永久兑换
      MagazineBuilder.cs     无限标准弹珠装填
      SaveSystem.cs          V5 存档、旧存档迁移、通关与兑换进度
      TiltController.cs      重力仪、校准、键盘/触屏降级
    Gameplay/
      MarbleController.cs    弹珠外观/拖尾、碰撞计数、引爆推力
      Baffle.cs              运动学挡板往返移动
      SpringLauncher.cs      一拉全发弹簧
      RewardHole.cs          奖励洞与空穴触发
      OneWayLaneGate.cs      发射道单向闸门
    Presentation/
      BoardBuilder.cs        机柜、半圆边框、钉阵、8 洞、3 块挡板
      Theme.cs               UI 与台面视觉主题
    UI/
      HUDController.cs       分数卡、弹珠卡、循环面板、设置入口
      HUDShop.cs             循环结算与三类兑换
      HUDOverlays.cs         主菜单、通关、设置、得分飘字
      UIFactory.cs           代码构建 UI 的工厂
    DebugTools/
      RuntimeDiagnostics.cs  -autoplay 自动验收与截图
      GatePhysicsProbe.cs    -gate-test 闸门物理验收
  StreamingAssets/game_config.json
  Resources/Config/game_config.json
Docs/
  验收记录.md
操作说明.md
```

## 数值配置

所有可调数值都在 `Assets/StreamingAssets/game_config.json`，并镜像到 `Assets/Resources/Config/game_config.json`：

- `economy`：每次齐射弹珠数、初始挡板数、附魔价格与递增系数。
- `loop`：循环长度范围、每几轮增长、三档门槛、百万目标。
- `explosion`：解锁价格、触发碰撞次数、半径、推力。
- `baffle`：最大数量、移除价格、总幅度、周期。
- `balls`：唯一的标准白球。
- `enchants`：赤红 ×2、湛蓝 ×5 及颜色。
- `holes`：8 洞的位置、倍率、基础积分、空穴标记。

## 构建与验证

### Android APK（可直接安装）

一次性环境配置：

1. 在 Unity Hub 中为 **6000.3.8f1** 安装：
   - **Android Build Support**
   - **Android SDK & NDK Tools**
   - **OpenJDK**
2. 打开项目，在 **File → Build Profiles** 中选择 **Android**，点击 **Switch Platform**，等待资源重新导入完成。
3. 构建脚本会固定以下 Android 参数：
   - 包名：`com.waytoagi.pachinko.mvp`
   - 竖屏锁定
   - **IL2CPP + ARM64**
   - `minSdk 25`，`targetSdk` 自动使用本机安装的最高版本
   - 未配置 keystore 时使用 Unity 调试签名，APK 可安装测试
4. 执行 **Pachinko → 4. 构建 Android APK**。不要用 **File → Build Profiles** 里的 `Build` 按钮，它会绕过构建脚本对包名、架构、签名和工具链的检查；即使直接点，新增的 `AndroidBuildGuard` 也会先检查工具链。

产物：`Builds/Android/PachinkoMVP.apk`

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
> **`JDK not found` 排查**：这不是项目问题，也不是单纯缺 `JAVA_HOME`。检查 `<UnityEditor>\Data\PlaybackEngines\AndroidPlayer\` 下是否同时存在 `OpenJDK\bin\java.exe`、`SDK\platform-tools\adb.exe` 和 `NDK\source.properties`。只安装 Android Support 主模块不会自动带上这三个子模块，需要在 Unity Hub / Unity 安装器中补装 **Android SDK & NDK Tools** 与 **OpenJDK**。当前构建入口会先做一次前置检查，直接列出缺失的 JDK / SDK / NDK 路径；若已使用外部工具链，脚本会读取 `Edit → Preferences → External Tools` 中的配置。
>
> **本机现状**：`C:\Program Files\Unity 6000.3.8f1` 是手动安装，目前只有 Android 平台主模块，`OpenJDK`、`SDK`、`NDK` 三个目录都不存在。关闭 Unity 后，优先在 Unity Hub 的 **Installs → 该编辑器的齿轮 → Add modules** 中勾选 **Android SDK & NDK Tools** 与 **OpenJDK**，按提示允许 UAC；如果 Hub 不允许给手动安装补模块，再用管理员身份运行已下载的 `UnitySetup64-6000.3.8f1.exe`，选择原安装目录补装。
>
> 也可以在**管理员终端**（先退出 Unity 和 Hub）用 CLI 补装：
>
> ```bat
> "C:\Program Files\Unity Hub\Unity Hub.exe" -- --headless install-modules --version 6000.3.8f1 -m android-sdk-ndk-tools android-open-jdk
> ```
>
> 或者直接双击 `Builds/Android/InstallAndroidModules.bat`：脚本会先检查 Unity / Hub 是否已退出，再请求管理员权限并调用上面的 Hub CLI，最后检查三个工具路径。
>
> **非致命警告**：Android 构建会提示 `Active Input Handling is set to Both`。项目代码只使用旧版 `Input`，当前 `Both` 仍可构建和运行；正式发布前可在 **Project Settings → Player → Other Settings → Active Input Handling** 改为 **Input Manager (Old)**，Unity 会要求重启编辑器。

### 编辑器与 Windows

- Unity 菜单：**Pachinko → 1. 生成 MVP 场景** / **2. 运行数值自检** / **3. 构建 Windows 试玩包** / **4. 构建 Android APK**
- 命令行自检：`PachinkoEditor.MvpSceneBuilder.RunSelfTestBatch`
- 命令行 Windows 构建：`PachinkoEditor.MvpSceneBuilder.RunBuildWindowsBatch`
- 命令行 Android 构建：`PachinkoEditor.MvpSceneBuilder.RunBuildAndroidBatch`
- Windows 包：`Builds/Windows/PachinkoMVP.exe`

> 项目已在 Unity 6000.3.8f1 中打开并完成脚本编译（最新一次编译无 `error CS`）；V3 数值自检尚未运行，Android APK 因缺少 OpenJDK / SDK / NDK 尚未构建，真机手感仍需复跑。

## V2 已移除的系统

为避免旧规则残留，以下系统已在 V3 中删除：

- 弹珠购买、库存、解锁、付费弹珠与分裂弹；
- 奖励洞升级与洞口专属技能；
- 连锁、狂热、头奖、余烬、赏金、全中奖励、护盾、磁引、弹跳；
- 里程碑、固定阶段过场、三页签商店；
- 旋转挡板与旧 `UpgradeCatalog`。

## 已知边界

- 多球齐射数量沿用 3 颗；策划案把多球数量列为待定项，5.2 废案的三洞表未实现。
- 保留原有 8 洞的 50/120/260/300 基础分后，单球收益低于策划案 8.1 给出的 1,000~100,000 量级，达到 1,000,000 需要较长积累；需按真机得分分布决定是否整体放大洞口分值或下调目标。
- 挡板初始 1 块（策划案建议首版先放一个），可在兑换中减到 0；配置最多 3 块。
- Android APK 尚未实际构建；当前只安装了 Android Build Support 主模块，需要补装 OpenJDK / SDK & NDK 后再在真机安装验证。
- 当前 Android 产物为 **ARM64 + 调试签名**；正式上架需要自定义 keystore，Google Play 还需要 AAB。
- Android 应用图标尚未配置，构建使用 Unity 默认图标。
- 真实 Android 加速度计方向与校准尚未实测。
- 引爆推力与挡板手感需要真机调参。
- 暂无音效；`弹珠机音效交付标准.md` 是音效外包说明。
