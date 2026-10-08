using UnityEngine;

namespace Relicfall.Environment
{
    public sealed class ParallaxLayer : MonoBehaviour
    {
        [SerializeField] private Camera targetCamera;
        [SerializeField, Range(-0.2f, 0.5f)] private float horizontalFactor = 0.15f;
        private float startingX;
        private float cameraStartingX;

        public float HorizontalFactor => horizontalFactor;

        private void Start()
        {
            if (targetCamera == null) targetCamera = Camera.main;
            startingX = transform.position.x;
            if (targetCamera != null) cameraStartingX = targetCamera.transform.position.x;
        }

        private void LateUpdate()
        {
            if (targetCamera == null) return;
            Vector3 position = transform.position;
            position.x = startingX +
                (targetCamera.transform.position.x - cameraStartingX) * horizontalFactor;
            transform.position = position;
        }

#if UNITY_EDITOR
        public void Configure(Camera camera, float factor)
        {
            targetCamera = camera;
            horizontalFactor = Mathf.Clamp(factor, -0.2f, 0.5f);
        }
#endif
    }
}
