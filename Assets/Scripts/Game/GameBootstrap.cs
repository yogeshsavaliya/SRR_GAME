using UnityEngine;

namespace Arrows.Game
{
    /// <summary>
    /// Zero-configuration entry point. Using RuntimeInitializeOnLoadMethod means
    /// the game boots from whatever scene is loaded (including an empty
    /// SampleScene) without any manual scene wiring or prefabs.
    /// </summary>
    public static class GameBootstrap
    {
        private static bool _started;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Boot()
        {
            if (_started) return;
            _started = true;

            var go = new GameObject("ArrowsGame");
            Object.DontDestroyOnLoad(go);
            go.AddComponent<GameController>();
        }
    }
}
