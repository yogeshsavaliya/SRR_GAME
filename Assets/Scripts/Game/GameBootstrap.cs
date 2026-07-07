using UnityEngine;

namespace Arrows.Game
{
    /// <summary>
    /// Safety-net entry point. The game is wired into SampleScene via a
    /// GameController component, but if a scene has no GameController (e.g. a
    /// brand-new empty scene), this creates one so the game still runs on Play.
    /// </summary>
    public static class GameBootstrap
    {
        private static bool _started;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Boot()
        {
            if (_started) return;
            _started = true;

            // A GameController placed in the scene takes precedence.
            if (Object.FindFirstObjectByType<GameController>() != null) return;

            var go = new GameObject("ArrowsGame");
            Object.DontDestroyOnLoad(go);
            go.AddComponent<GameController>();
        }
    }
}
