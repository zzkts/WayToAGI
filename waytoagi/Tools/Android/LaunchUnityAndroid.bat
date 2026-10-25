@echo off
setlocal
rem Launch Unity with ASCII TEMP and GRADLE_USER_HOME.
rem Required on this machine because the Windows profile path contains Chinese
rem characters, which breaks the .bat files AGP generates for the prefab tool.
set "TEMP=D:\Android\temp"
set "TMP=D:\Android\temp"
set "GRADLE_USER_HOME=D:\Android\gradle-home"
if not exist "%TEMP%" mkdir "%TEMP%"
if not exist "%GRADLE_USER_HOME%" mkdir "%GRADLE_USER_HOME%"

start "" "C:\Program Files\Unity 6000.3.8f1\Editor\Unity.exe" ^
  -projectPath "D:\unity_projects\WayToAGI\waytoagi" ^
  -buildTarget Android
