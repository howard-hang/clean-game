# 洗洗更解压 (Clean Game) v1.1

解压清洁小游戏：冲掉泥巴、搓出泡泡、洗到闪闪发亮。

## 主题与关卡（共 4 主题 · 24 关）

| 主题 | 物品 |
|------|------|
| 浴室 | 青花盘、小黄鸭、肥皂、洗发瓶、牙刷杯、丝瓜络 |
| 厨房 | 平底锅、茶壶、马克杯、大碗、砧板、水壶 |
| 车库 | 工具箱、轮胎、油桶、头盔、机油壶、扳手 |
| 花园 | 花盆、洒水壶、小矮人、鸟屋、青蛙、手推车 |

## 操作

- **单指 / 左键拖动**：清洁
- **双指拖动 / 右键拖动**：旋转物体（相机不动）
- **双指开合 / 滚轮**：缩放
- **底部按钮或 1/2**：切换高压水枪 / 泡泡海绵

## 工程

- Windows：Unity **6000.3.26f1**，打开本仓库，菜单或 `BuildScript.Win`
- Android：Unity **2022.3.58f1c1**（Unity 6 Android 模块当前 CDN 404），用 `clean-game-android` 同源工程 `BuildScript.Android`
- 模型：`make_assets.py` + Blender 5.x 批处理烘焙

```bash
blender -b -P make_assets.py -- Assets/Resources
```

## 下载

- 源码：[GitHub](https://github.com/howard-hang/clean-game)
- Android APK：见 [Releases](https://github.com/howard-hang/clean-game/releases)

## 说明

调试签名 APK，可侧载；模型为程序化 Blender 风格化资产，适合移动端面数。
