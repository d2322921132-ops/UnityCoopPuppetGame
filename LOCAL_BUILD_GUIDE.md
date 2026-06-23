# 本地 Unity 构建 APK 指南

## 方法：直接在 Unity 编辑器中构建（最简单）

### 第 1 步：打开项目
1. 打开 Unity Hub
2. 点击 **"打开项目"**
3. 选择你的项目文件夹 `UnityCoopPuppetGame`
4. 等待项目加载（首次加载会自动下载 Photon SDK）

### 第 2 步：构建 APK
1. 在 Unity 菜单栏点击 **"Build"**
2. 点击 **"Build Android APK"**
3. 选择输出文件夹（例如 `Builds/Android/`）
4. 等待构建完成（5-15 分钟）

### 第 3 步：安装到手机
1. 用 USB 连接手机
2. 开启手机的 **"开发者模式"** 和 **"USB 调试"**
3. 在 Unity 中点击 **"Build And Run"**
4. 或者直接复制 APK 到手机安装

---

## 方法 B：使用命令行构建（高级）

如果你不想打开 Unity 编辑器，可以使用命令行：

```bash
# 进入项目目录
cd "你的项目路径"

# 运行构建（需要知道 Unity.exe 路径）
"C:\Program Files\Unity\Hub\Editor\6000.0.0f1\Editor\Unity.exe" ^
  -quit -batchmode -nographics ^
  -projectPath "你的项目路径" ^
  -buildTarget Android ^
  -executeMethod BuildScript.BuildAndroid
```

---

## 常见问题

### Q: Photon SDK 没有自动下载？
A: 首次打开项目时，`PhotonSDKAutoInstaller.cs` 会自动下载。如果失败：
1. 手动下载：https://dashboard.photonengine.com
2. 导入 `Photon-Fusion-2.unitypackage`

### Q: 构建失败提示缺少 Android SDK？
A: 在 Unity Hub 中：
1. 点击 **"Installs"**
2. 找到你的 Unity 版本
3. 点击齿轮 → **"Add modules"**
4. 勾选 **"Android Build Support"**
5. 点击 **"Install"**

### Q: 构建成功但 APK 无法安装？
A: 检查：
- 手机是否允许 "未知来源" 安装
- APK 是否完整下载
- 手机 Android 版本是否 >= 8.0

---

## 构建输出位置
构建完成后，APK 文件位于：
```
项目文件夹/Builds/Android/UnityCoopPuppetGame.apk
```

---

## 下一步
构建成功后，你可以：
1. 直接安装到手机测试
2. 用微信发送给朋友安装
3. 上传到应用商店
