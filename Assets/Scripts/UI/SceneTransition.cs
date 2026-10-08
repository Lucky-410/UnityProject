namespace Relicfall.UI
{
    public static class SceneTransition
    {
        public static bool IsLoading => LoadingManager.IsLoading;
        public static bool Load(string sceneName) => LoadingManager.Load(sceneName);
    }
}
