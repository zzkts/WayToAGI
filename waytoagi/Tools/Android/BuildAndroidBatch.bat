@echo off
setlocal
rem Batch APK build. Close the Unity editor first.
set "TEMP=D:\Android\temp"
set "TMP=D:\Android\temp"
set "GRADLE_USER_HOME=D:\Android\gradle-home"
if not exist "%TEMP%" mkdir "%TEMP%"
if not exist "%GRADLE_USER_HOME%" mkdir "%GRADLE_USER_HOME%"

tasklist /FI "IMAGENAME eq Unity.exe" 2>nul | findstr /I /C:"Unity.exe" >nul
if not errorlevel 1 (
  echo Unity Editor is still running. Close it first, then run this script again.
  pause
  exit /b 1
)

"C:\Program Files\Unity 6000.3.8f1\Editor\Unity.exe" ^
  -batchmode -nographics ^
  -projectPath "D:\unity_projects\WayToAGI\waytoagi" ^
  -buildTarget Android ^
  -executeMethod PachinkoEditor.MvpSceneBuilder.RunBuildAndroidBatch ^
  -logFile "D:\Android\unity-android-build.log"

echo Exit code: %errorlevel%
echo Log: D:\Android\unity-android-build.log
pause
