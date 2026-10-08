using System;
using System.IO;
using System.Linq;
using Relicfall.UI;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Relicfall.Editor
{
    // 仅保留手动生成工具，不订阅运行模式事件，不自动切换或停止游戏。
    public static class RelicfallUguiMigration
    {
        private const string PrefabPath = "Assets/Resources/UI/RelicfallUI.prefab";

        [MenuItem("Tools/Relicfall/生成 UGUI 页面预制体")]
        public static void Generate()
        {
            if (EditorApplication.isPlaying) throw new InvalidOperationException("请先退出运行模式。");
            Directory.CreateDirectory("Assets/Resources/UI/Styles");
            AssetDatabase.Refresh();
            foreach (string name in UguiTheme.StyleNames)
            {
                string path = "Assets/Resources/UI/Styles/" + name + ".asset";
                Directory.CreateDirectory(Path.GetDirectoryName(path));
                Texture2D texture = Resources.Load<Texture2D>("PixelUIkit/Pixel UI kit/PNGs/" + name);
                if (texture == null) throw new InvalidOperationException("缺少 UI 素材：" + name);
                Sprite sprite = Sprite.Create(texture, new Rect(0, 0, texture.width, texture.height),
                    Vector2.one * 0.5f, 100f, 0, SpriteMeshType.FullRect, UguiTheme.Border(name));
                sprite.name = name;
                Sprite existing = AssetDatabase.LoadAssetAtPath<Sprite>(path);
                if (existing != null)
                {
                    EditorUtility.CopySerialized(sprite, existing);
                    UnityEngine.Object.DestroyImmediate(sprite);
                }
                else AssetDatabase.CreateAsset(sprite, path);
            }
            AssetDatabase.SaveAssets();
            var root = new GameObject("UIManager");
            try
            {
                UIManager manager = root.AddComponent<UIManager>();
                manager.Initialize();
                PrefabUtility.SaveAsPrefabAsset(root, PrefabPath);
            }
            finally { UnityEngine.Object.DestroyImmediate(root); }
            var boot = SceneManager.GetSceneByPath("Assets/Scenes/BootScene.unity");
            bool openedBoot = !boot.IsValid() || !boot.isLoaded;
            if (openedBoot) boot = EditorSceneManager.OpenScene("Assets/Scenes/BootScene.unity", OpenSceneMode.Additive);
            if (!boot.GetRootGameObjects().Any(go => go.GetComponent<UIManager>() != null))
                PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath), boot);
            EditorSceneManager.MarkSceneDirty(boot);
            EditorSceneManager.SaveScene(boot);
            if (openedBoot) EditorSceneManager.CloseScene(boot, true);
            AssetDatabase.SaveAssets();
        }

        [InitializeOnLoadMethod]
        private static void ClearOldValidationFlags()
        {
            SessionState.SetBool("Relicfall.UguiValidation.Running", false);
            SessionState.SetBool("Relicfall.UguiValidation.Restore", false);
        }
    }
}
