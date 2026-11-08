using UnityEngine;

namespace GameUI
{
    /// <summary>
    /// Scene-owned references that define where UI prefabs are instantiated.
    /// </summary>
    public sealed class UISceneRoot : MonoBehaviour
    {
        [SerializeField] private Transform hudRoot;
        [SerializeField] private Transform modalRoot;
        [SerializeField] private StartWindowView startWindowPrefab;
        [SerializeField] private PlayerRespawnWindowView playerRespawnWindowPrefab;
        [SerializeField] private PauseWindowView pauseWindowPrefab;
        [SerializeField] private GameplayHudView gameplayHudPrefab;

        public Transform HudRoot => hudRoot;

        public Transform ModalRoot => modalRoot;

        public StartWindowView StartWindowPrefab => startWindowPrefab;

        public PlayerRespawnWindowView PlayerRespawnWindowPrefab => playerRespawnWindowPrefab;

        public PauseWindowView PauseWindowPrefab => pauseWindowPrefab;

        public GameplayHudView GameplayHudPrefab => gameplayHudPrefab;
    }
}
