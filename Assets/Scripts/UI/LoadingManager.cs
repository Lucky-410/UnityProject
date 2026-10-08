using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;
using UnityEngine.ResourceManagement.ResourceProviders;
using UnityEngine.SceneManagement;

namespace Relicfall.UI
{
    public sealed class LoadingManager : MonoBehaviour
    {
        private static LoadingManager instance;
        private AsyncOperationHandle<IList<GameObject>> gamePreload;
        private bool hasGamePreload;
        private bool loading;
        private string destination;
        private string error;
        private float progress;

        public static string Error => instance != null ? instance.error : null;

        public static bool IsLoading => instance != null && instance.loading;
        public static float Progress => instance != null ? instance.progress : 0f;

        public static bool Load(string sceneName)
        {
            if (string.IsNullOrWhiteSpace(sceneName)) return false;
            if (instance == null) new GameObject("LoadingManager").AddComponent<LoadingManager>();
            if (instance.loading) return false;
            instance.destination = sceneName;
            instance.error = null;
            instance.progress = 0f;
            instance.loading = true;
            UIManager.Instance.BeginLoading();
            instance.StartCoroutine(instance.LoadRoutine());
            return true;
        }

        private void Awake()
        {
            if (instance != null && instance != this)
            {
                Destroy(gameObject);
                return;
            }
            instance = this;
            DontDestroyOnLoad(gameObject);
        }

        private IEnumerator LoadRoutine()
        {
            float started = Time.realtimeSinceStartup;
            yield return null;
            AsyncOperationHandle<UnityEngine.AddressableAssets.ResourceLocators.IResourceLocator> init =
                Addressables.InitializeAsync(false);
            while (!init.IsDone)
            {
                progress = Mathf.Max(progress, init.PercentComplete * 0.12f);
                yield return null;
            }
            if (init.Status != AsyncOperationStatus.Succeeded)
            {
                Addressables.Release(init);
                Fail("资源目录初始化失败");
                yield break;
            }
            Addressables.Release(init);

            AsyncOperationHandle download = Addressables.DownloadDependenciesAsync(destination);
            while (!download.IsDone)
            {
                progress = Mathf.Max(progress, 0.12f + download.PercentComplete * 0.17f);
                yield return null;
            }
            if (download.Status != AsyncOperationStatus.Succeeded)
            {
                Addressables.Release(download);
                Fail("场景依赖资源加载失败");
                yield break;
            }
            Addressables.Release(download);

            AsyncOperationHandle<IList<GameObject>> pendingPreload = default;
            bool hasPendingPreload = destination == "GameScene";
            if (hasPendingPreload)
            {
                pendingPreload = Addressables.LoadAssetsAsync<GameObject>("game-preload", null);
                while (!pendingPreload.IsDone)
                {
                    progress = Mathf.Max(progress,
                        0.29f + pendingPreload.PercentComplete * 0.31f);
                    yield return null;
                }
                if (pendingPreload.Status != AsyncOperationStatus.Succeeded)
                {
                    Addressables.Release(pendingPreload);
                    Fail("游戏预加载资源失败");
                    yield break;
                }
            }

            AsyncOperationHandle<SceneInstance> scene =
                Addressables.LoadSceneAsync(destination, LoadSceneMode.Single);
            while (!scene.IsDone)
            {
                progress = Mathf.Max(progress, 0.60f + scene.PercentComplete * 0.39f);
                yield return null;
            }
            if (scene.Status != AsyncOperationStatus.Succeeded)
            {
                Addressables.Release(scene);
                if (hasPendingPreload) Addressables.Release(pendingPreload);
                Fail("场景切换失败");
                yield break;
            }

            // 新场景加载后，旧场景中的对象池已销毁，此时释放旧预加载句柄。
            if (hasGamePreload && gamePreload.IsValid()) Addressables.Release(gamePreload);
            gamePreload = pendingPreload;
            hasGamePreload = hasPendingPreload;
            while (Time.realtimeSinceStartup - started < 0.60f) yield return null;
            progress = 1f;
            loading = false;
            UIManager.Instance.EndLoading();
        }

        private void Fail(string message)
        {
            error = message;
            Debug.LogError(message);
        }

        public static void Retry()
        {
            if (instance == null || !instance.loading || instance.error == null) return;
            instance.error = null;
            instance.progress = 0f;
            instance.StartCoroutine(instance.LoadRoutine());
        }

        private void OnDestroy()
        {
            if (hasGamePreload && gamePreload.IsValid()) Addressables.Release(gamePreload);
            if (instance == this) instance = null;
        }
    }
}
