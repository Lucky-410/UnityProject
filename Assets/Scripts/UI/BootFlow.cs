using UnityEngine;

namespace Relicfall.UI
{
    public sealed class BootFlow : MonoBehaviour
    {
        private void Start() => SceneTransition.Load("MainMenuScene");
    }
}
