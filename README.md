# 洗洗更解压 (Clean Game) v1.1

解压清洁小游戏：冲掉泥巴、搓出泡泡、洗到闪闪发亮。

## 主题与关卡（共 4 主题 · 24 关）

| 主题 | 物品 |
|------|------|
| 浴室 | 青花盘、小黄鸭、肥皂、洗发瓶、牙刷杯、丝瓜络 |
| 厨房 | 平底锅、茶壶、马克杯、大碗、砧板、水壶 |
| 车库 | 工具箱、轮胎、油桶、头盔、机油壶、扳手 |
| 花园 | 花盆、洒水壶、小矮人、鸟屋、青蛙、手推车 |

## 操作（v1.2 互不冲突）

底部先选模式，再单指拖动——**一种手势只做一件事**：

- **清洗模式**：单指在物体上拖动清洁；可选「高压水枪 / 泡泡海绵」
- **旋转模式**：单指拖动只转物体，**不会清洁**
- **双指开合 / 滚轮**：缩放（相机位置固定，只改远近）
- **右键拖动（电脑）**：随时旋转，从不清洁
- 快捷键：\Q\ 清洗 / \E\ 旋转 / \ \ 切换工具

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
