# Unity Cloud Build 自动打包 - 已完成配置

## 项目仓库
https://github.com/d2322921132-ops/UnityCoopPuppetGame

## 已完成的工作
- 所有代码已推送到你的 GitHub 仓库
- 添加了 Unity Cloud Build 配置文件 `unitycloudbuild.yaml`
- Photon SDK 自动安装脚本已包含

## 你只需做这一步

### 1. 访问 Unity Cloud Build
打开 https://dashboard.unity3d.com
用 Unity Hub 账号登录

### 2. 创建 Cloud Build 项目
1. 点击 **Cloud Build**
2. 点击 **Create a new Cloud Build project**
3. 选择 **Connect a repository**
4. 选择 **GitHub**
5. 授权 Unity 访问你的 GitHub
6. 选择仓库 `UnityCoopPuppetGame`

### 3. 构建设置（已自动配置大部分）
- **Platform**: Android
- **Unity Version**: 6000.0.x
- **Branch**: main
- **Target**: APK

### 4. 高级设置
- Scripting Backend: IL2CPP
- Target Architectures: ARMv7 + ARM64
- Minimum API Level: 26
- Target API Level: 34

### 5. 开始构建
点击 **Save and Build**

等待 5-15 分钟后，APK 生成完成，点击 Download 下载。

## 自动构建
可以开启 **Auto-build on push**，每次推送代码到 main 分支会自动触发构建。
