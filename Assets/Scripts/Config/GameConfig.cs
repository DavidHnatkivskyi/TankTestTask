using UnityEngine;

namespace Config
{
    [CreateAssetMenu(fileName = "GameConfig", menuName = "Config/Game")]
    public sealed class GameConfig : ScriptableObject
    {
        [SerializeField] private Vector3 topDownOffset = new(0f, 6.25f, 0f);
        [SerializeField] private Vector3 thirdPersonOffset = new(0f, 1f, -2f);
        [SerializeField, Min(0f)] private float cameraMovementTime = 1f;
        [SerializeField, Min(0f)] private float topDownWallClearance = 0.05f;
        [SerializeField, Min(0f)] private float thirdPersonLookSensitivity = 0.1f;
        [SerializeField] private float thirdPersonDefaultPitch = 19f;
        [SerializeField] private float thirdPersonMinimumPitch = -60f;
        [SerializeField] private float thirdPersonMaximumPitch = 60f;
        [SerializeField, Min(0f)] private float thirdPersonAimDistance = 100f;
        [SerializeField] private bool forceMobileHud;
        [SerializeField, Min(0f)] private float mobileJoystickForwardMultiplier = 3f;
        [SerializeField, Min(0f)] private float playerRespawnWindowDelay = 1f;

        public Vector3 TopDownOffset => topDownOffset;

        public Vector3 ThirdPersonOffset => thirdPersonOffset;

        public float CameraMovementTime => cameraMovementTime;
        public float TopDownWallClearance => topDownWallClearance;
        public float ThirdPersonLookSensitivity => thirdPersonLookSensitivity;
        public float ThirdPersonDefaultPitch => thirdPersonDefaultPitch;
        public float ThirdPersonMinimumPitch => thirdPersonMinimumPitch;
        public float ThirdPersonMaximumPitch => thirdPersonMaximumPitch;
        public float ThirdPersonAimDistance => thirdPersonAimDistance;

        public bool ForceMobileHud => forceMobileHud;

        public float MobileJoystickForwardMultiplier => mobileJoystickForwardMultiplier;

        public float PlayerRespawnWindowDelay => playerRespawnWindowDelay;
    }
}
