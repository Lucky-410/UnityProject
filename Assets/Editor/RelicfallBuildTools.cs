using System;
using System.IO;
using UnityEditor;
using UnityEditor.AddressableAssets.Settings;
using UnityEditor.Build.Reporting;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace Relicfall.Editor
{
    public static class RelicfallBuildTools
    {
        [MenuItem("Tools/Relicfall/构建资源内容")]
        public static void BuildContent()
        {
            if (EditorApplication.isPlaying) throw new InvalidOperationException("请先退出运行模式。");
            AssetDatabase.SaveAssets();
            AddressableAssetSettings.BuildPlayerContent(out var result);
            if (!string.IsNullOrEmpty(result.Error)) throw new InvalidOperationException("资源构建失败：" + result.Error);
            Debug.Log("Relicfall 资源内容构建完成。");
        }

        [MenuItem("Tools/Relicfall/构建 Windows 版本")]
        public static void BuildWindows()
        {
            if (EditorApplication.isPlaying) throw new InvalidOperationException("请先退出运行模式。");
            EditorSceneManager.SaveOpenScenes();
            BuildContent();
            Directory.CreateDirectory("Builds/Relicfall");
            BuildReport result = BuildPipeline.BuildPlayer(new BuildPlayerOptions
            {
                scenes = new[] { "Assets/Scenes/BootScene.unity" },
                locationPathName = "Builds/Relicfall/Relicfall.exe",
                target = BuildTarget.StandaloneWindows64,
                options = BuildOptions.StrictMode
            });
            if (result.summary.result != BuildResult.Succeeded)
                throw new InvalidOperationException("Windows 构建失败：" + result.summary.result);
            Debug.Log("Relicfall Windows 版本构建完成：Builds/Relicfall/Relicfall.exe");
        }
    }
}
