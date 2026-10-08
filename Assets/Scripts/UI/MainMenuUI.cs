using Relicfall.Save;
using UnityEngine;

namespace Relicfall.UI
{
    public sealed class MainMenuUI : MonoBehaviour
    {
        private void OnEnable() => UIManager.Instance.BindMainMenu(this);
        private void OnDisable() => UIManager.Existing?.UnbindMainMenu(this);
        public void StartJourney(bool resume)
        {
            if (SceneTransition.IsLoading) return;
            GameSaveController.ContinueRequested = resume;
            SceneTransition.Load("GameScene");
        }
        public void Exit()
        {
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }
    }
}
