using UnityEditor;
using UnityEditor.Build;
using UnityEditor.SceneManagement;
using UnityEngine;
using System.IO;

public static class BuildScriptAndroid {
  static string MakeScene() {
    var s = EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects, NewSceneMode.Single);
    new GameObject("CleanGame").AddComponent<CleanGame>();
    Directory.CreateDirectory("Assets/Scenes");
    EditorSceneManager.SaveScene(s, "Assets/Scenes/Main.unity");
    return "Assets/Scenes/Main.unity";
  }
  static void AlwaysInclude() {
    var assets = AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/GraphicsSettings.asset");
    if (assets == null || assets.Length == 0) return;
    var so = new SerializedObject(assets[0]);
    var arr = so.FindProperty("m_AlwaysIncludedShaders");
    if (arr == null) return;
    foreach (var n in new[] { "Standard", "Sprites/Default" }) {
      var sh = Shader.Find(n); if (sh == null) continue;
      bool has = false;
      for (int i = 0; i < arr.arraySize; i++) if (arr.GetArrayElementAtIndex(i).objectReferenceValue == sh) has = true;
      if (!has) { arr.InsertArrayElementAtIndex(arr.arraySize); arr.GetArrayElementAtIndex(arr.arraySize - 1).objectReferenceValue = sh; }
    }
    so.ApplyModifiedProperties();
  }
  public static void Android() {
    PlayerSettings.productName = "\u6d17\u6d17\u66f4\u89e3\u538b";
    PlayerSettings.companyName = "zhanghang";
    PlayerSettings.bundleVersion = "1.9.0";
    AlwaysInclude();
    PlayerSettings.SetApplicationIdentifier(NamedBuildTarget.Android, "com.zhanghang.cleangame");
    PlayerSettings.Android.minSdkVersion = AndroidSdkVersions.AndroidApiLevel25;
    PlayerSettings.Android.targetSdkVersion = (AndroidSdkVersions)34;
    PlayerSettings.Android.targetArchitectures = AndroidArchitecture.ARM64;
    PlayerSettings.SetScriptingBackend(NamedBuildTarget.Android, ScriptingImplementation.IL2CPP);
    PlayerSettings.defaultInterfaceOrientation = UIOrientation.LandscapeLeft;
    PlayerSettings.allowedAutorotateToLandscapeLeft = true;
    PlayerSettings.allowedAutorotateToLandscapeRight = true;
    PlayerSettings.allowedAutorotateToPortrait = false;
    PlayerSettings.allowedAutorotateToPortraitUpsideDown = false;
    PlayerSettings.Android.useCustomKeystore = false;
    PlayerSettings.Android.bundleVersionCode = 10;
    EditorUserBuildSettings.androidBuildSystem = AndroidBuildSystem.Gradle;
    EditorUserBuildSettings.buildAppBundle = false;
    EditorUserBuildSettings.androidBuildSubtarget = MobileTextureSubtarget.ASTC;
    if (EditorUserBuildSettings.activeBuildTarget != BuildTarget.Android) {
      EditorUserBuildSettings.SwitchActiveBuildTarget(BuildTargetGroup.Android, BuildTarget.Android);
    }
    var sc = MakeScene();
    Directory.CreateDirectory("Build/Android");
    var outPath = Path.GetFullPath("Build/Android/CleanGame.apk");
    var r = BuildPipeline.BuildPlayer(new[] { sc }, outPath, BuildTarget.Android, BuildOptions.None);
    Debug.Log("BUILD_RESULT " + r.summary.result);
    if (r.summary.result == UnityEditor.Build.Reporting.BuildResult.Succeeded) {
      try {
        var desk = Path.Combine(System.Environment.GetFolderPath(System.Environment.SpecialFolder.DesktopDirectory), "CleanGame.apk");
        File.Copy(outPath, desk, true);
        var also = @"C:\Users\zhang\Desktop\clean-game\Build\Android";
        Directory.CreateDirectory(also);
        File.Copy(outPath, Path.Combine(also, "CleanGame.apk"), true);
        Debug.Log("APK_COPIED " + desk + " size=" + new FileInfo(outPath).Length);
      } catch (System.Exception e) { Debug.LogWarning("copy fail: " + e.Message); }
    } else { Debug.LogError("ANDROID_BUILD_FAILED " + r.summary.result); }
    EditorApplication.Exit(r.summary.result == UnityEditor.Build.Reporting.BuildResult.Succeeded ? 0 : 1);
  }
}


