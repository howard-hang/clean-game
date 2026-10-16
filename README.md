# 洗洗更解压（CleanGame）

一款 **3D 清洁解压** 小游戏：拖动清洗脏污物体，用水枪冲泥、用海绵擦垢，在精致浴室场景里过关拿星。

| 项目 | 说明 |
|------|------|
| 引擎 | Unity **6000.3**（Unity 6 LTS 系列） |
| 包名 | `com.zhanghang.cleangame` |
| 版本 | 1.0.0 |
| 平台 | Windows / Android（横屏）/ 亦可打 WebGL |

## 玩法

- **5 关**：油腻青花盘 → 泥巴木箱 → 泥坑足球 → 古董花瓶 → 小黄鸭
- **双层污垢**：泥层 + 污垢膜，先冲后擦；洗净处会短暂湿亮
- **工具**：`1` 高压水枪 / `2` 泡泡海绵（手机用底部按钮切换）
- **操作（PC）**：左键拖动清洗；右键拖动物体旋转；滚轮缩放
- **操作（手机）**：单指清洗；双指旋转；双指开合缩放
- 干净度约 **95%** 过关，按用时评 **1～3 星**

## 仓库内容

```
Assets/
  Editor/          # BuildScript.cs（Win/Web/Android）、BuildScriptAndroid.cs
  Resources/       # FBX 模型、贴图、NotoSansSC 中文字体
  Scenes/Main.unity
  Scripts/CleanGame.cs   # 主玩法（运行时搭场景与 UI）
Packages/
ProjectSettings/
make_assets.py     # Blender 生成模型/贴图的脚本（可选）
screenshots/       # 游戏截图
```

**未纳入仓库**（体积大、可本地生成）：`Library/`、`Temp/`、`Obj/`、`Logs/`、`Build/`、`.apk` / `.exe` 等。

> 另有一份基于 Unity **2022.3** 的 Android 侧载工程（本机 `clean-game-android`），与本仓库玩法相同；若 Unity 6 的 Android 模块下载失败，可用 2022.3 + `BuildScriptAndroid.Android` 打 APK。

## 环境要求

1. 安装 [Unity Hub](https://unity.com/download)
2. 安装 **Unity 6000.3.x**（与 `ProjectSettings/ProjectVersion.txt` 接近即可）
3. 打 Windows：勾选 **Windows Build Support**
4. 打 Android：勾选 **Android Build Support**（SDK/NDK/OpenJDK），并准备 JDK

可选：本机有 Blender 时，可运行 `make_assets.py` 重新生成 `Assets/Resources` 下的模型与贴图。

## 用 Unity 打开

1. Unity Hub → **打开** → 选择本仓库根目录  
2. 等待导入完成（首次会生成 `Library/`）  
3. 打开 `Assets/Scenes/Main.unity`，点 Play 试玩  

主逻辑在 `CleanGame` 组件：运行时会搭建浴室、物体与 IMGUI 界面。

## 命令行打包

把下面的 `UNITY` 换成你的 Editor 路径，例如：

- Unity 6：`C:\Program Files\Unity\Hub\Editor\6000.3.26f1\Editor\Unity.exe`
- Unity 2022（仅 Android 备用）：`...\2022.3.xxf1\Editor\Unity.exe`

### Windows 可执行文件

```bat
"%UNITY%" -batchmode -quit -projectPath "%CD%" -executeMethod BuildScript.Win
```

产物：`Build/Windows/CleanGame.exe`

### Android APK

**方式 A（Unity 6，推荐，若已装好 Android 模块）：**

```bat
"%UNITY%" -batchmode -quit -projectPath "%CD%" -executeMethod BuildScript.Android
```

**方式 B（Unity 2022.3 兼容脚本）：**

```bat
"%UNITY%" -batchmode -quit -projectPath "%CD%" -executeMethod BuildScriptAndroid.Android
```

产物：`Build/Android/CleanGame.apk`（调试签名，可侧载；上架需换成正式 keystore）  
要求：Android 7.0+（API 24）、ARM64、横屏。

安装示例：

```bat
adb install -r Build\Android\CleanGame.apk
```

### WebGL（可选）

```bat
"%UNITY%" -batchmode -quit -projectPath "%CD%" -executeMethod BuildScript.Web
```

产物：`Build/WebGL/`

## 重新生成美术资源（可选）

```bat
blender --background --python make_assets.py
```

会写出 plate / crate / ball / vase / duck 的 FBX 与基础色贴图到 `Assets/Resources/`。

## 截图

见 `screenshots/` 目录（标题界面、浴室、清洗、通关等）。

## 许可说明

- 游戏代码与自研模型/贴图：可按个人/学习用途使用与修改。  
- `Assets/Resources/NotoSansSC.ttf` 来自 [Noto Sans SC](https://fonts.google.com/noto/specimen/Noto+Sans+SC)，遵循其 SIL Open Font License。  
- 请勿将调试签名的 APK 直接上架应用商店。
