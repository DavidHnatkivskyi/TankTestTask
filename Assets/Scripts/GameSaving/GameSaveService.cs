using System;
using System.IO;
using UnityEngine;

namespace GameSaving
{
    /// <summary>
    /// Stores the game session as JSON in Unity's platform-specific persistent data directory.
    /// </summary>
    public sealed class GameSaveService : IGameSaveService
    {
        private const int CurrentVersion = 1;
        private const string FileName = "game-save.json";

        private readonly ITankStatePersistence _tankStatePersistence;
        private readonly ICameraStatePersistence _cameraStatePersistence;

        public GameSaveService(
            ITankStatePersistence tankStatePersistence,
            ICameraStatePersistence cameraStatePersistence)
        {
            _tankStatePersistence = tankStatePersistence;
            _cameraStatePersistence = cameraStatePersistence;
        }

        public bool Save()
        {
            if (!_tankStatePersistence.TryCaptureState(out var tankState))
            {
                return false;
            }

            var saveData = new GameSaveData
            {
                version = CurrentVersion,
                camera = _cameraStatePersistence.CaptureState(),
                tanks = tankState
            };

            try
            {
                Directory.CreateDirectory(Application.persistentDataPath);
                File.WriteAllText(SavePath, JsonUtility.ToJson(saveData, true));
                return true;
            }
            catch (Exception exception)
            {
                Debug.LogError($"Could not save game state to '{SavePath}': {exception.Message}");
                return false;
            }
        }

        public bool Load()
        {
            if (!File.Exists(SavePath))
            {
                Debug.LogWarning($"No game save found at '{SavePath}'.");
                return false;
            }

            GameSaveData saveData;
            try
            {
                saveData = JsonUtility.FromJson<GameSaveData>(File.ReadAllText(SavePath));
            }
            catch (Exception exception)
            {
                Debug.LogError($"Could not read game save at '{SavePath}': {exception.Message}");
                return false;
            }

            if (!IsSupported(saveData))
            {
                return false;
            }

            if (!_tankStatePersistence.TryRestoreState(saveData.tanks))
            {
                return false;
            }

            _cameraStatePersistence.RestoreState(saveData.camera);
            return true;
        }

        private static bool IsSupported(GameSaveData saveData)
        {
            if (saveData != null &&
                saveData.version == CurrentVersion &&
                saveData.camera != null &&
                saveData.tanks != null)
            {
                return true;
            }

            Debug.LogError("Game save has an unsupported format or is incomplete.");
            return false;
        }

        private static string SavePath => Path.Combine(Application.persistentDataPath, FileName);
    }
}
