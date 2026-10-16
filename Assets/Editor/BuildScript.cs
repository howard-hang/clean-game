using UnityEditor;
using UnityEditor.Build;
using UnityEditor.SceneManagement;
using UnityEngine;
using System.IO;

public static class BuildScript {
  static string MakeScene() {
    var s = EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects, NewSceneMode.Single);
    new GameObject("CleanGame").AddComponent<CleanGame>();
    Directory.CreateDirectory("Assets/Scenes");
    EditorSceneManager.SaveScene(s, "Assets/Scenes/Main.unity");
    return "Assets/Scenes/Main.unity";
  }
  static void AlwaysInclude() {
    var gs = AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/GraphicsSettings.asset")[0];
    var so = new SerializedObject(gs);
    var arr = so.FindProperty("m_AlwaysIncludedShaders");
    foreach (var n in new[] { "Standard", "Sprites/Default" }) {
      var sh = Shader.Find(n); bool has = false;
      for (int i = 0; i < arr.arraySize; i++) if (arr.GetArrayElementAtIndex(i).objectReferenceValue == sh) has = true;
      if (!has) { arr.InsertArrayElementAtIndex(arr.arraySize); arr.GetArrayElementAtIndex(arr.arraySize - 1).objectReferenceValue = sh; }
    }
    so.ApplyModifiedProperties();
  }
  static void Common() {
    PlayerSettings.productName = "\u6d17\u6d17\u66f4\u89e3\u538b";
    PlayerSettings.companyName = "zhanghang";
    PlayerSettings.bundleVersion = "1.3.0";
    AlwaysInclude();
  }
  public static void Win() {
    Common();
    var sc = MakeScene();
    var r = BuildPipeline.BuildPlayer(new[] { sc }, "Build/Windows/CleanGame.exe", BuildTarget.StandaloneWindows64, BuildOptions.None);
    Debug.Log("BUILD_RESULT " + r.summary.result);
    EditorApplication.Exit(r.summary.result == UnityEditor.Build.Reporting.BuildResult.Succeeded ? 0 : 1);
  }
  public static void Web() {
    Common();
    var sc = MakeScene();
    PlayerSettings.WebGL.compressionFormat = WebGLCompressionFormat.Disabled;
    var r = BuildPipeline.BuildPlayer(new[] { sc }, "Build/WebGL", BuildTarget.WebGL, BuildOptions.None);
    Debug.Log("BUILD_RESULT " + r.summary.result);
    EditorApplication.Exit(r.summary.result == UnityEditor.Build.Reporting.BuildResult.Succeeded ? 0 : 1);
  }
  public static void Android() {
    Common();
    PlayerSettings.SetApplicationIdentifier(NamedBuildTarget.Android, "com.zhanghang.cleangame");
    PlayerSettings.Android.minSdkVersion = AndroidSdkVersions.AndroidApiLevel25;
    PlayerSettings.Android.targetSdkVersion = AndroidSdkVersions.AndroidApiLevelAuto;
    PlayerSettings.Android.targetArchitectures = AndroidArchitecture.ARM64;
    PlayerSettings.SetScriptingBackend(NamedBuildTarget.Android, ScriptingImplementation.IL2CPP);
    PlayerSettings.Android.forceSDCardPermission = false;
    PlayerSettings.defaultInterfaceOrientation = UIOrientation.LandscapeLeft;
    PlayerSettings.allowedAutorotateToLandscapeLeft = true;
    PlayerSettings.allowedAutorotateToLandscapeRight = true;
    PlayerSettings.allowedAutorotateToPortrait = false;
    PlayerSettings.allowedAutorotateToPortraitUpsideDown = false;
    PlayerSettings.Android.useCustomKeystore = false;
    PlayerSettings.Android.bundleVersionCode = 4;
    EditorUserBuildSettings.buildAppBundle = false;
    EditorUserBuildSettings.androidBuildSubtarget = MobileTextureSubtarget.ASTC;
    var sc = MakeScene();
    Directory.CreateDirectory("Build/Android");
    var r = BuildPipeline.BuildPlayer(new[] { sc }, "Build/Android/CleanGame.apk", BuildTarget.Android, BuildOptions.None);
    Debug.Log("BUILD_RESULT " + r.summary.result);
    if (r.summary.result == UnityEditor.Build.Reporting.BuildResult.Succeeded) {
      try {
        var desk = Path.Combine(System.Environment.GetFolderPath(System.Environment.SpecialFolder.DesktopDirectory), "CleanGame.apk");
        File.Copy(Path.GetFullPath("Build/Android/CleanGame.apk"), desk, true);
        Debug.Log("APK_COPIED_DESKTOP " + desk);
      } catch (System.Exception e) { Debug.LogWarning("Desktop copy failed: " + e.Message); }
    }
    EditorApplication.Exit(r.summary.result == UnityEditor.Build.Reporting.BuildResult.Succeeded ? 0 : 1);
  }
}
