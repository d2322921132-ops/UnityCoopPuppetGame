# 安卓打包配置指南

## 必须配置的 Project Settings

### 1. Player Settings (Android)
路径: `Edit > Project Settings > Player > Android`

| 配置项 | 推荐值 | 说明 |
|--------|--------|------|
| Company Name | YourCompany | 公司名 |
| Product Name | CoupleGame | 游戏名 |
| Package Name | com.yourcompany.couplegame | 包名（必须唯一） |
| Version | 1.0 | 版本号 |
| Bundle Version Code | 1 | 内部版本号 |
| Minimum API Level | Android 8.0 (API 26) | 最低安卓版本 |
| Target API Level | Automatic | 自动使用最高已安装版本 |

### 2. Other Settings
路径: `Edit > Project Settings > Player > Android > Other Settings`

| 配置项 | 推荐值 | 说明 |
|--------|--------|------|
| Scripting Backend | IL2CPP | 性能更好，支持 ARM64 |
| API Compatibility Level | .NET Standard 2.1 | 兼容性 |
| Target Architectures | ARMv7 + ARM64 | 支持绝大多数设备 |
| Internet Access | Require | 必须开启，用于联网 |
| Write Permission | External (SDCard) | 如需存储数据 |

### 3. Publishing Settings
路径: `Edit > Project Settings > Player > Android > Publishing Settings`

- 勾选 `Custom Keystore`
- 创建新的 Keystore 文件（用于应用签名）
- 设置 Keystore 密码和 Alias 密码

### 4. XR Plug-in Management
路径: `Edit > Project Settings > XR Plug-in Management`

如需 VR/AR 支持则启用对应插件，否则保持关闭。

### 5. Quality Settings
路径: `Edit > Project Settings > Quality`

为 Android 平台选择 `Medium` 或 `High` 质量等级。

### 6. Graphics Settings
路径: `Edit > Project Settings > Graphics`

确保 URP 渲染管线已正确配置。

---

## 打包步骤

1. `File > Build Settings`
2. 选择 `Android` 平台，点击 `Switch Platform`
3. 点击 `Player Settings` 检查上述配置
4. 点击 `Build` 或 `Build And Run`
5. 选择 APK 输出路径

## 注意事项

- 首次打包需要安装 Android SDK 和 NDK（Unity Hub 可自动安装）
- IL2CPP 模式下首次编译较慢，请耐心等待
- 确保手机开启 USB 调试模式（Build And Run 用）
