@echo off
chcp 65001 >nul
echo ==========================================
echo   Unity Coop Puppet Game - Android 构建
echo ==========================================
echo.

set UNITY_PATH=C:\"Program Files"\Unity\Hub\Editor\6000.5.0f1\Editor\Unity.exe
set PROJECT_PATH=D:\UnityCoopPuppetGame

if not exist %UNITY_PATH% (
    echo [错误] 找不到 Unity.exe: %UNITY_PATH%
    echo 请检查 Unity 安装路径
    pause
    exit /b 1
)

if not exist "%PROJECT_PATH%\Assets" (
    echo [错误] 找不到项目文件夹: %PROJECT_PATH%
    echo 请检查项目路径
    pause
    exit /b 1
)

echo [1/3] 正在构建 Android APK...
echo 这可能需要 10-20 分钟，请耐心等待...
echo.

%UNITY_PATH% -quit -batchmode -nographics ^
    -projectPath "%PROJECT_PATH%" ^
    -buildTarget Android ^
    -executeMethod BuildScript.BuildAndroid ^
    -logFile "%PROJECT_PATH%\build.log"

if errorlevel 1 (
    echo.
    echo ==========================================
    echo   [构建失败]
    echo   请查看 build.log 了解错误详情
    echo ==========================================
    notepad "%PROJECT_PATH%\build.log"
    pause
    exit /b 1
)

echo.
echo ==========================================
echo   [构建成功!]
echo   APK 位置: %PROJECT_PATH%\Builds\Android\
echo ==========================================
pause
