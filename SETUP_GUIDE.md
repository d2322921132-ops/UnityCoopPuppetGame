# Unity 双人协作游戏 - 环境搭建与联机配置指南

## 阶段进度表

| 阶段 | 任务 | 状态 |
|------|------|------|
| P0 | 给 Trae 下指令，规划架构 | 已完成 |
| P1 | 搭建 Unity 环境，配置联机钥匙 | 正在进行 |
| P2 | 跑通物理连接，实现"锁链"原型 | 下一步 |
| P3 | 云存档集成，发布测试版 | 后续 |

---

## 第一步：安装 Unity 环境

### 1. 安装 Unity Hub

1. 访问 [Unity 官网](https://unity.com/download)
2. 下载并安装 **Unity Hub**
3. 注册/登录 Unity 账号

### 2. 安装 Unity 编辑器

1. 打开 Unity Hub
2. 点击左侧 **Installs** → **Install Editor**
3. 选择版本：**Unity 2022.3 LTS**（长期支持版，最稳定）
4. 安装时勾选以下模块：
   - **Android Build Support**（如果要发布到安卓）
   - **iOS Build Support**（如果要发布到 iOS）
   - **Visual Studio**（代码编辑器）
   - **Documentation**

### 3. 配置 Trae IDE

1. 安装 [Trae IDE](https://www.trae.ai/)
2. 在 Trae 中打开本项目文件夹：`UnityCoopGame/`
3. 确保 Trae 能识别到 `.cs` 和 `.shader` 文件

---

## 第二步：打开项目并测试第一个场景

### 1. 在 Unity Hub 中打开项目

1. 打开 Unity Hub
2. 点击 **Projects** → **Open**
3. 选择 `UnityCoopGame` 文件夹
4. Unity 会自动加载项目

### 2. 测试 MainGame 场景（锁链物理原型）

1. 在 Unity 编辑器中，打开 `Assets/Scenes/MainGame.unity`
2. 如果场景不存在，创建一个新场景：
   - `File` → `New Scene`
   - 保存为 `Assets/Scenes/MainGame.unity`
3. 在场景中创建一个空物体，命名为 `ChainTest`
4. 将 `ChainPhysicsTest.cs` 脚本挂载到 `ChainTest` 上
5. 创建两个材质：
   - `Assets/Materials/Player1Mat.mat`（蓝色）
   - `Assets/Materials/Player2Mat.mat`（红色）
6. 将材质拖到脚本的对应字段
7. 点击 **Play** 运行

### 3. 测试控制

- **Player1（蓝色方块）**：WASD 移动，Space 跳跃
- **Player2（红色方块）**：方向键移动，Enter 跳跃
- 观察两个方块之间的 Spring Joint 物理效果
- 屏幕左上角会显示当前距离和张力

---

## 第三步：配置 Photon Fusion 联机

### 1. 注册 Photon 账号

1. 访问 [Photon Engine 官网](https://www.photonengine.com/)
2. 点击 **Sign Up** 注册账号
3. 登录后进入 [Dashboard](https://dashboard.photonengine.com/)

### 2. 创建 Fusion 应用

1. 在 Dashboard 中点击 **Create a New App**
2. **Photon Type** 选择 **Fusion**
3. **App Name** 填写你的游戏名称（如 `CoopGame`）
4. 点击 **Create**
5. 复制生成的 **App ID**（格式如：`xxxxxxxx-xxxx-xxxx-xxxx-xxxxxxxxxxxx`）

### 3. 下载 Photon Fusion SDK

1. 在 Photon Dashboard 中找到你的应用
2. 点击 **Download** 或访问 [Photon Fusion 下载页](https://doc.photonengine.com/fusion/current/getting-started/sdk-download)
3. 下载 `Photon-Fusion-Installer.unitypackage`
4. 在 Unity 中：`Assets` → `Import Package` → `Custom Package`
5. 选择下载的 `.unitypackage` 文件，点击 **Import**

### 4. 配置 App ID

#### 方法一：通过脚本配置（推荐）

1. 在 Unity 场景中创建一个空物体，命名为 `PhotonSetup`
2. 挂载 `PhotonFusionSetup.cs` 脚本
3. 在 Inspector 面板中找到 **Photon App Id** 字段
4. 粘贴你的 App ID

#### 方法二：通过 Photon 设置面板

1. 在 Unity 菜单栏：`Window` → `Photon Unity Networking` → `Highlight Server Settings`
2. 或：`Window` → `Fusion` → `Fusion Hub`
3. 在设置面板中找到 **App ID** 字段
4. 粘贴你的 App ID
5. 点击 **Setup Project**

### 5. 验证配置

1. 运行场景
2. 查看 Game 视图右上角或 Console 窗口
3. 如果显示 **"Photon App ID 已配置"**，说明配置成功
4. 如果显示红色错误，检查 App ID 是否正确

---

## 第四步：测试联机功能

### 1. 创建测试场景

1. 打开 `MainGame` 场景
2. 确保场景中有：
   - `GameManager` 空物体（挂载 `GameManager.cs`）
   - `NetworkManager` 空物体（挂载 `NetworkManager.cs`）
   - `PhotonSetup` 空物体（挂载 `PhotonFusionSetup.cs`，已配置 App ID）

### 2. 运行测试

**测试方法一：同一台电脑多开**
1. 在 Unity 中点击 **File** → **Build Settings**
2. 选择 **PC, Mac & Linux Standalone**
3. 点击 **Build And Run**
4. 同时运行 Unity 编辑器（Play 模式）
5. 两个客户端应该能互相看到对方

**测试方法二：不同设备**
1. 在一台电脑上 Build 项目
2. 将可执行文件复制到另一台电脑
3. 确保两台设备在同一网络或都能访问互联网
4. 同时运行，测试跨地区联机

### 3. 常见问题排查

| 问题 | 解决方案 |
|------|---------|
| 无法连接到 Photon | 检查 App ID 是否正确，网络是否畅通 |
| 看不到其他玩家 | 确认两人在同一房间，检查房间名是否一致 |
| 延迟很高 | 在 Photon Dashboard 中选择离你们最近的服务器区域 |
| 连接频繁断开 | 检查防火墙设置，确保端口未被阻挡 |

---

## 第五步：项目文件结构确认

确保以下文件都已就位：

```
UnityCoopGame/
├── Assets/
│   ├── Scripts/
│   │   ├── Gameplay/
│   │   │   ├── GameManager.cs          (游戏状态管理)
│   │   │   ├── ChainPhysicsTest.cs     (锁链物理测试)
│   │   │   └── ChainSystem.cs          (锁链系统)
│   │   ├── Networking/
│   │   │   ├── NetworkManager.cs       (网络管理)
│   │   │   ├── PhotonFusionSetup.cs    (Photon配置)
│   │   │   └── CharacterControllerMobile.cs (角色控制器)
│   │   ├── Input/
│   │   │   ├── DynamicJoystick.cs      (动态摇杆)
│   │   │   ├── MobileActionButton.cs   (动作按钮)
│   │   │   └── MobileInputManager.cs   (输入管理)
│   │   └── UI/
│   │       └── ChainTensionMeter.cs    (张力条)
│   ├── Scenes/
│   │   ├── MainMenu.unity
│   │   └── MainGame.unity              (测试场景)
│   └── Materials/
│       ├── Player1Mat.mat              (蓝色材质)
│       └── Player2Mat.mat              (红色材质)
└── README.md
```

---

## 下一步（P2 阶段）

完成以上配置后，你将进入 **P2 阶段**：

1. 测试 Spring Joint 物理效果
2. 替换方块为人形角色模型
3. 实现 ChainSystem 的张力检测和 UI 反馈
4. 添加移动端触摸控制
5. 测试双人协作机制

---

## 技术支持

- **Photon 官方文档**: https://doc.photonengine.com/fusion/current/getting-started/fusion-intro
- **Unity 手册**: https://docs.unity3d.com/Manual/index.html
- **Photon Dashboard**: https://dashboard.photonengine.com/
