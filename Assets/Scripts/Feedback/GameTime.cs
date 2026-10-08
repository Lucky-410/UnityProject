using UnityEngine;

namespace Relicfall.Feedback
{
    public static class GameTime
    {
        private static bool paused;
        private static float hitStopUntil;
        public static bool IsHitStopped => Time.realtimeSinceStartup < hitStopUntil;
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void Reset() { paused = false; hitStopUntil = 0; Time.timeScale = 1; }
        public static void SetPaused(bool value) { paused = value; Tick(); }
        public static void HitStop(float seconds) { hitStopUntil = Mathf.Max(hitStopUntil, Time.realtimeSinceStartup + seconds); Tick(); }
        public static void ClearTransient() { hitStopUntil = 0; Tick(); }
        public static void Tick()
        {
            float scale = paused || IsHitStopped ? 0 : 1;
            if (Time.timeScale != scale) Time.timeScale = scale;
        }
    }
}
