# Unity 双人协作移动端游戏 - 完整项目

## 项目概述

这是一个基于 Unity 2022.3 LTS 开发的双人协作移动端游戏完整项目框架，集成了 Photon Fusion 网络同步、Firebase 云存档、URP 渲染管线等核心技术。

## 技术栈

| 技术领域 | 方案选择 |
|---------|---------|
| 游戏引擎 | Unity 2022.3 LTS |
| 编程语言 | C# |
| 网络同步 | Photon Fusion (Shared Mode) |
| 云存档 | Firebase Realtime Database |
| 渲染管线 | URP (Universal Render Pipeline) |
| 输入系统 | 自定义触摸输入 |

## 项目结构

```
UnityCoopGame/
├── Assets/
│   ├── Scripts/
│   │   ├── Networking/      # 网络同步
│   │   ├── CloudSave/       # 云存档
│   │   ├── Input/           # 输入系统
│   │   ├── Audio/           # 音频管理
│   │   ├── UI/              # UI 系统
│   │   ├── Gameplay/        # 游戏逻辑
│   │   └── Utils/           # 工具类
│   ├── Shaders/             # 自定义 Shader
│   ├── Prefabs/             # 预制体
│   ├── Scenes/              # 场景
│   ├── Resources/           # 资源
│   └── ...
├── Packages/                # 包管理
└── ProjectSettings/         # 项目设置
```

## 核心功能

### 1. 网络同步 (Photon Fusion)
- `NetworkManager.cs` - 连接管理、房间管理
- `PlayerController.cs` - 玩家移动同步、RPC 交互
- `NetworkPlayerData.cs` - 网络数据结构

### 2. 云存档 (Firebase)
- `CloudSaveManager.cs` - 存档读写、自动保存
- `GameSaveData` - 存档数据结构
- 每 30 秒自动保存

### 3. 输入系统
- `InputManager.cs` - 浮动摇杆、触摸交互
- `FloatingJoystick.cs` - 动态摇杆组件
- 左半屏移动、右半屏交互

### 4. 音频系统
- `AudioManager.cs` - BGM 切换、环境音效
- 交叉淡入淡出
- 混音器控制

### 5. 游戏逻辑
- `GameManager.cs` - 核心状态管理
- `PlayerStats.cs` - 玩家属性系统
- `CoopObjective.cs` - 协作目标系统
- `InteractableObject.cs` - 可交互对象

### 6. 渲染效果
- `URP_MobileLit.shader` - 移动端 PBR 光照
- `BloomEffect.shader` - Bloom 泛光

### 7. 工具类
- `HapticFeedback.cs` - 震动反馈
- `CameraFollow.cs` - 相机跟随
- `EasingAnimations.cs` - 17 种缓动动画
- `SceneTransitionManager.cs` - 场景过渡

## 快速开始

### 环境准备
1. 安装 Unity Hub 和 Unity 2022.3 LTS
2. 安装 Android/iOS Build Support 模块

### 插件安装
1. **Photon Fusion**: 从 Photon 官网下载 SDK，导入到 `Assets/Plugins/Photon/`
2. **Firebase**: 在 Firebase 控制台创建项目，导入 Unity SDK

### 场景设置
在场景中创建以下管理器空物体：
- GameManager (挂载 GameManager.cs)
- NetworkManager (挂载 NetworkManager.cs)
- CloudSaveManager (挂载 CloudSaveManager.cs)
- AudioManager (挂载 AudioManager.cs)
- InputManager (挂载 InputManager.cs)
- UIManager (挂载 UIManager.cs)
- SceneTransitionManager (挂载 SceneTransitionManager.cs)

### URP 配置
1. 在 Package Manager 安装 Universal RP
2. 创建 URP Asset: `Assets > Create > Rendering > URP Asset`
3. 在 Graphics Settings 中指定 URP Asset

## 使用说明

### 网络连接
```csharp
// 连接到服务器
NetworkManager.Instance.ConnectToServer();

// 创建房间
NetworkManager.Instance.CreateRoom("RoomName");

// 断开连接
NetworkManager.Instance.Disconnect();
```

### 存档操作
```csharp
// 保存游戏
var saveData = GameManager.Instance.CollectSaveData();
CloudSaveManager.Instance.SaveGame(saveData);

// 加载游戏
CloudSaveManager.Instance.LoadGame();
```

### 音频播放
```csharp
// 切换场景 BGM
AudioManager.Instance.SwitchBGMForScene("GameScene");

// 播放音效
AudioManager.Instance.PlaySFX("Jump");
```

### 缓动动画
```csharp
// 缩放动画
StartCoroutine(transform.ScaleTo(Vector3.one * 1.5f, 0.5f));

// 淡入效果
StartCoroutine(canvasGroup.FadeIn(0.3f));
```

## 性能优化

- 使用 URP SRP Batcher
- GPU Instancing
- 控制 Draw Call < 200
- LOD 系统
- 对象池
- ASTC/ETC2 纹理压缩

## 许可证

本项目为示例框架，可自由修改和使用。
