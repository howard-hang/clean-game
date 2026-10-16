# 洗洗更解压 (Clean Game)

Unity casual cleaning game. Controls: 清洗/旋转 modes, water gun / sponge.

## v1.4.0 — 考古生物
- 新主题 `fossil` / 考古生物：清理远古化石骨架（沙赭强调色，发掘现场的暖尘天色）
- 6 关：霸王龙头骨 `trex_skull`、三角龙头骨 `triceratops_skull`、剑龙背板 `stego_plate`、菊石 `ammonite`、三叶虫 `trilobite`、猛犸象牙 `mammoth_tusk`
- 化石主题工具：考古凿 `dig_chisel`（较强，石屑扬尘，无水柱）/ 考古刷 `dig_brush`（较柔，尘土与干泡沫）
- 其它主题仍是 高压水枪 / 泡泡海绵；清洗与旋转继续互斥
- 房间是沙地发掘现场（方格网、帆布帐篷、石箱），沉积物为黏土褐色
- 仓库里还没有这些 FBX 时，用同名程序化占位模型进关；以后把 `Assets/Resources/<id>.fbx` 和 `<id>_base.png` 放进去就会自动换上

## v1.3.0 — Bathroom model upgrade
- AI concept four-view sheets under `refs/bath/<id>/fourview.jpg` (Flux via Pollinations)
- Blender orthographic four-views `refs/bath/<id>/blender_fourview.jpg`
- Rebuilt bathroom FBX meshes (plate/duck/soap/shampoo/cup/loofah) via Blender 5.2 CLI (Tripo unavailable)
- Windows + Android builds; keep 清洗/旋转 interaction

## Download
See [Releases](https://github.com/howard-hang/clean-game/releases)

# 洗洗更解压 (Clean Game) v1.4

解压清洁小游戏：冲掉泥巴、搓出泡泡，洗到闪闪发亮。考古主题则是扫开沉积物、凿出骨头。

## 主题与关卡（共 5 主题 · 30 关）

| 主题 | 物品 |
|------|------|
| 浴室 | 青花盘、小黄鸭、肥皂、洗发瓶、牙刷杯、丝瓜络 |
| 厨房 | 平底锅、茶壶、马克杯、大碗、砧板、水壶 |
| 车库 | 工具箱、轮胎、油桶、头盔、机油壶、扳手 |
| 花园 | 花盆、洒水壶、小矮人、鸟屋、青蛙、手推车 |
| 考古生物 | 霸王龙头骨、三角龙头骨、剑龙背板、菊石、三叶虫、猛犸象牙 |

化石关的工具是 **考古凿 / 考古刷**（道具 id：`dig_chisel` / `dig_brush`）。凿更有力，打出石屑和扬尘，不喷水；刷更轻，带尘土和干泡沫。浴室、厨房、车库、花园仍是 **高压水枪 / 泡泡海绵**。

## 操作（v1.2 互不冲突）

底部先选模式，再单指拖动——**一种手势只做一件事**：

- **清洗模式**：单指在物体上拖动清洁；可选「高压水枪 / 泡泡海绵」，化石主题则是「考古凿 / 考古刷」
- **旋转模式**：单指拖动只转物体，**不会清洁**
- **双指开合 / 滚轮**：缩放（相机位置固定，只改远近）
- **右键拖动（电脑）**：随时旋转，从不清洁
- 快捷键：`Q` 清洗 / `E` 旋转 / `1` `2` 切换工具

## 工程

- Windows：Unity **6000.3.26f1**，打开本仓库，菜单或 `BuildScript.Win`
- Android：Unity **2022.3.58f1c1**（Unity 6 Android 模块当前 CDN 404），用 `clean-game-android` 同源工程 `BuildScript.Android`
- 模型：`make_assets.py` + Blender 5.x 批处理烘焙
- 化石模型若尚未导入，运行时使用 `FossilPlaceholders` 占位，资源名与上表 mesh id 一致

```bash
blender -b -P make_assets.py -- Assets/Resources
```

## 下载

- 源码：[GitHub](https://github.com/howard-hang/clean-game)
- Android APK：见 [Releases](https://github.com/howard-hang/clean-game/releases)

## 说明

调试签名 APK，可侧载；模型为程序化 / Blender 风格化资产，适合移动端面数。
