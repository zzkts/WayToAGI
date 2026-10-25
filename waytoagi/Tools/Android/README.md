# Android 构建辅助

这些脚本针对当前机器：Unity `6000.3.8f1`，Windows 用户目录 `C:\Users\刘诚毅` 含中文。

## 脚本

- `LaunchUnityAndroid.bat`：把 `TEMP`、`TMP`、`GRADLE_USER_HOME` 指到纯英文路径后打开 Unity。构建入口现在也会自动设置这些变量；该脚本仍适合需要提前固定环境、手动调用 Gradle 或排查日志的场景。
- `BuildAndroidBatch.bat`：关闭 Unity 后双击，命令行构建 APK，日志写到 `D:\Android\unity-android-build.log`。
- `aliyun-mirrors.gradle`：Gradle 初始化脚本备份。构建入口会自动把它复制到 `D:\Android\gradle-home\init.d\aliyun-mirrors.gradle`；它把阿里云 Google / Maven Central / Gradle Plugin 镜像排在默认仓库之前，避免 `dl.google.com` 直连被重置。旧用户目录 `%USERPROFILE%\.gradle\init.d` 下的副本可保留。

## 当前工具链

| 工具 | 路径 |
| --- | --- |
| OpenJDK 17.0.20.1 | `C:\Program Files\Eclipse Adoptium\jdk-17.0.20.101-hotspot` |
| Android SDK | `D:\Android\Sdk` |
| Android NDK r27c | `D:\Android\Sdk\ndk\27.2.12479018` |
| CMake 3.22.1 | `D:\Android\Sdk\cmake\3.22.1` |
| Gradle 缓存 | `D:\Android\gradle-home` |
| Gradle 临时目录 | `D:\Android\temp` |

产物：`Builds/Android/PachinkoMVP.apk`（当前 50 MB，2026-10-25 14:51 重建，含 K6 数值与额外弹珠升级；arm64-v8a，Android Debug 签名）。

本次构建使用上述纯英文环境直接执行 Unity 生成的 Gradle 工程 `assembleRelease`，`BUILD SUCCESSFUL in 47s`。编辑器菜单构建的环境设置代码已编译，但尚未从菜单完整复跑。
