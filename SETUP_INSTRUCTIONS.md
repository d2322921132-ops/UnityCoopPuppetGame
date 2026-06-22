# Unity 挂载步骤指南

## 第一步：创建玩家预制体（Player Prefab）

1. 在 Hierarchy 窗口右键 → **Create Empty**，命名为 `PlayerPrefab`
2. 选中 `PlayerPrefab`，在 Inspector 中点击 **Add Component**
3. 搜索并添加 `PlayerSync` 脚本
4. 点击 **Add Component** 再次搜索添加 `NetworkObject`（Photon Fusion 组件）
5. 点击 **Add Component** 再次搜索添加 `NetworkTransform`（Photon Fusion 组件）
6. 点击 **Add Component** 再次搜索添加 `NetworkRigidbody`（Photon Fusion 组件）
7. 在 Hierarchy 中选中 `PlayerPrefab`，拖到 **Project 窗口的 Assets/Prefabs 文件夹**（如果没有就右键 Create → Folder 创建）
8. 在 Hierarchy 中**删除** `PlayerPrefab`（保留 Project 里的预制体）

## 第二步：创建网络管理器（只需拖拽一次）

1. 在 Hierarchy 窗口右键 → **Create Empty**，命名为 `NetworkManager`
2. 选中 `NetworkManager`，在 Inspector 中点击 **Add Component**
3. 搜索并添加 `NetworkManager` 脚本
4. 在 Inspector 中找到 **Player Prefab** 字段
5. 从 Project 窗口把 `PlayerPrefab` 预制体拖到 **Player Prefab** 槽位

完成！现在点击 Play 即可自动联机。

---

## 可选：添加跨平台配置

1. 选中 Hierarchy 中的 `NetworkManager`
2. 点击 **Add Component**
3. 搜索并添加 `CrossPlatformConfig` 脚本

---

## 场景要求

确保场景中有：
- 地面（Plane 或 Cube，带 Collider）
- 方向光（Directional Light）
- 相机（Camera，tag 设为 MainCamera）

---

## 安卓打包设置

1. `Edit → Project Settings → Player → Android`
2. Package Name: `com.yourcompany.couplegame`
3. Minimum API Level: `Android 8.0 (API 26)`
4. `File → Build Settings → Android → Switch Platform`
5. `Add Open Scenes`
6. `Build` 生成 APK

---

## iOS 打包设置

1. `Edit → Project Settings → Player → iOS`
2. Bundle Identifier: `com.yourcompany.couplegame`
3. Target SDK: `Device SDK`
4. `File → Build Settings → iOS → Switch Platform`
5. `Build` 生成 Xcode 项目
6. 用 Xcode 打开并签名打包

---

## 跨平台联机测试

1. 一台设备（或模拟器）运行 iOS 版本
2. 另一台安卓设备安装 APK
3. 两台设备都连接到互联网（4G/5G/WiFi 均可）
4. 同时打开应用，自动进入同一个 `CoupleRoom`
5. 看到两个玩家（蓝色是自己，红色是对方）
