# GitHub 推送指南 + Unity Cloud Build 自动打包

## 第一步：在 GitHub 创建仓库

1. 打开 https://github.com/new
2. 仓库名称填写：`UnityCoopPuppetGame`
3. 选择 **Public**（公开）或 **Private**（私有）
4. **不要勾选** "Initialize this repository with a README"
5. 点击 **Create repository**

## 第二步：推送代码到 GitHub

创建仓库后，GitHub 会显示推送命令。请在你的本地 Unity 项目根目录（`D:\D` 或你的项目路径）打开 Git Bash 或 CMD，执行以下命令：

```bash
# 如果还没有安装 Git，先下载 https://git-scm.com/download/win

# 进入项目目录
cd "你的项目路径"

# 配置 Git（如果第一次使用）
git config --global user.email "你的邮箱"
git config --global user.name "你的名字"

# 初始化仓库（如果还没初始化）
git init

# 添加所有文件
git add -A

# 提交
git commit -m "Initial commit: Unity Coop Puppet Game"

# 关联远程仓库（将下面的 URL 替换为你自己的仓库地址）
git remote add origin https://github.com/你的用户名/UnityCoopPuppetGame.git

# 推送代码
git branch -M main
git push -u origin main
```

## 第三步：配置 Unity Cloud Build（免费自动打包）

### 3.1 访问 Unity Cloud Build
1. 打开 https://dashboard.unity3d.com
2. 使用 Unity ID 登录（就是 Unity Hub 的账号）
3. 点击左侧菜单 **Cloud Build**

### 3.2 创建 Cloud Build 项目
1. 点击 **"Create a new Cloud Build project"**
2. 选择 **"Connect a repository"**
3. 选择 **GitHub**
4. 授权 Unity 访问你的 GitHub 仓库
5. 选择刚才创建的 `UnityCoopPuppetGame` 仓库

### 3.3 构建设置
1. **Platform**: 选择 **Android**
2. **Unity Version**: 选择 **Unity 6000.0.x**（或你安装的版本）
3. **Branch**: 选择 **main**
4. **Target**: 选择 **APK**

### 3.4 高级设置（重要）
点击 **"Show advanced settings"**：

- **Build Target**: Android
- **Scripting Backend**: IL2CPP（推荐，性能更好）
- **Target Architectures**: ARMv7 + ARM64
- **Minimum API Level**: Android 8.0 (API 26)
- **Target API Level**: Android 14 (API 34)

### 3.5 开始构建
1. 点击 **"Save and Build"**
2. Unity Cloud Build 会自动：
   - 拉取你的代码
   - 安装依赖
   - 导入 Photon Fusion SDK（通过 PhotonSDKAutoInstaller.cs）
   - 编译项目
   - 生成 APK
3. 构建完成后，你会收到邮件通知
4. 在 Cloud Build 面板点击 **Download** 下载 APK

## 第四步：安装 APK 到手机

### 方法 A：直接下载安装
1. 在手机浏览器打开 Unity Cloud Build 下载链接
2. 下载 APK
3. 安装时如果提示"未知来源"，请允许

### 方法 B：通过电脑传输
1. 在电脑下载 APK
2. 用微信文件传输助手发送到手机
3. 在手机上点击安装

## 常见问题

### Q: Photon Fusion SDK 会自动下载吗？
A: 会！项目中包含 `PhotonSDKAutoInstaller.cs`，Unity Cloud Build 在编译时会自动触发下载和导入。

### Q: 构建失败怎么办？
A: 检查 Cloud Build 日志，常见问题：
- Unity 版本不匹配：确保 Cloud Build 选择的版本和你本地一致
- 缺少 Android SDK：Cloud Build 已预装，一般没问题
- 脚本编译错误：检查是否使用了本地特有的插件

### Q: 可以构建 iOS 版本吗？
A: 可以！但 iOS 构建需要 Apple Developer 账号（$99/年），且需要配置证书和描述文件。

### Q: 每次代码更新后需要手动构建吗？
A: 不需要！可以设置 **Auto-build on push**，每次推送到 GitHub 会自动触发构建。

## 项目结构说明

```
UnityCoopPuppetGame/
├── Assets/
│   ├── Scripts/
│   │   ├── Networking/     # 联机核心代码
│   │   ├── Player/         # 玩家控制 + 木偶生成
│   │   ├── Input/          # 移动端输入
│   │   ├── Gameplay/       # 游戏逻辑
│   │   ├── UI/             # 界面
│   │   └── Utils/          # 工具类
│   ├── Scenes/             # 游戏场景
│   ├── Materials/          # 材质
│   └── Shaders/            # 着色器
├── Packages/               # Unity 包管理
└── ProjectSettings/        # 项目设置
```

## 联系支持

如果在 Unity Cloud Build 中遇到问题：
1. 查看构建日志（Cloud Build 面板中有详细日志）
2. 访问 https://support.unity.com
3. 或返回这里告诉我错误信息
