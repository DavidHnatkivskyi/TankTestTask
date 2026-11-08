using System.IO;
using System.Text.RegularExpressions;
using GameSaving;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace Tanks.Tests
{
    /// <summary>
    /// GameSaveService stores the save at a fixed path inside Application.persistentDataPath,
    /// so these tests back up any real save, start from an empty folder and restore it afterwards.
    /// </summary>
    [TestFixture]
    public sealed class GameSaveServiceTests
    {
        private const string SaveFileName = "game-save.json";
        private const float RotationToleranceDegrees = 0.01f;

        private string _savePath;
        private bool _hadOriginalSave;
        private byte[] _originalSave;
        private FakeTankStatePersistence _tanks;
        private FakeCameraStatePersistence _camera;
        private GameSaveService _saveService;

        [SetUp]
        public void SetUp()
        {
            _savePath = Path.Combine(Application.persistentDataPath, SaveFileName);
            _hadOriginalSave = File.Exists(_savePath);
            _originalSave = _hadOriginalSave ? File.ReadAllBytes(_savePath) : null;
            DeleteSaveFile();

            _tanks = new FakeTankStatePersistence { CapturedState = new TankRuntimeSaveState() };
            _camera = new FakeCameraStatePersistence { CapturedState = new CameraSaveData() };
            _saveService = new GameSaveService(_tanks, _camera);
        }

        [TearDown]
        public void TearDown()
        {
            if (_hadOriginalSave)
            {
                Directory.CreateDirectory(Path.GetDirectoryName(_savePath));
                File.WriteAllBytes(_savePath, _originalSave);
                return;
            }

            DeleteSaveFile();
        }

        [Test]
        public void SaveThenLoad_RoundTripsTankAndCameraState()
        {
            _tanks.CapturedState = new TankRuntimeSaveState
            {
                playerTank = new TankSaveData
                {
                    tankSaveId = "player-test",
                    position = new Vector3(3f, 0f, 4f),
                    chassisRotation = Quaternion.Euler(0f, 30f, 0f),
                    turretRotation = Quaternion.Euler(0f, 60f, 0f)
                }
            };
            _camera.CapturedState = new CameraSaveData
            {
                viewMode = 1,
                thirdPersonYaw = 120f,
                thirdPersonPitch = 25f,
                hasThirdPersonOrientation = true
            };

            Assert.That(_saveService.Save(), Is.True);
            Assert.That(_saveService.Load(), Is.True);

            Assert.That(_tanks.RestoredStates, Has.Count.EqualTo(1));
            var player = _tanks.RestoredStates[0].playerTank;
            Assert.That(player.tankSaveId, Is.EqualTo("player-test"));
            Assert.That(player.position, Is.EqualTo(new Vector3(3f, 0f, 4f)));
            Assert.That(
                Quaternion.Angle(player.chassisRotation, Quaternion.Euler(0f, 30f, 0f)),
                Is.LessThan(RotationToleranceDegrees));
            Assert.That(
                Quaternion.Angle(player.turretRotation, Quaternion.Euler(0f, 60f, 0f)),
                Is.LessThan(RotationToleranceDegrees));
            Assert.That(_camera.RestoredStates, Has.Count.EqualTo(1));
            Assert.That(_camera.RestoredStates[0].viewMode, Is.EqualTo(1));
            Assert.That(_camera.RestoredStates[0].thirdPersonYaw, Is.EqualTo(120f));
            Assert.That(_camera.RestoredStates[0].thirdPersonPitch, Is.EqualTo(25f));
            Assert.That(_camera.RestoredStates[0].hasThirdPersonOrientation, Is.True);
        }

        [Test]
        public void SaveThenLoad_RoundTripsComputerTanksWithTheirMovementTargets()
        {
            _tanks.CapturedState = new TankRuntimeSaveState
            {
                computerTanks =
                {
                    new ComputerTankSaveData
                    {
                        tankSaveId = "computer-patrol",
                        position = new Vector3(-1f, 0f, 2f),
                        hasMovementTarget = true,
                        movementTarget = new Vector3(6f, 0f, -5f)
                    },
                    new ComputerTankSaveData
                    {
                        tankSaveId = "computer-idle",
                        hasMovementTarget = false
                    }
                }
            };

            Assert.That(_saveService.Save(), Is.True);
            Assert.That(_saveService.Load(), Is.True);

            var computerTanks = _tanks.RestoredStates[0].computerTanks;
            Assert.That(computerTanks, Has.Count.EqualTo(2));
            Assert.That(computerTanks[0].tankSaveId, Is.EqualTo("computer-patrol"));
            Assert.That(computerTanks[0].position, Is.EqualTo(new Vector3(-1f, 0f, 2f)));
            Assert.That(computerTanks[0].hasMovementTarget, Is.True);
            Assert.That(computerTanks[0].movementTarget, Is.EqualTo(new Vector3(6f, 0f, -5f)));
            Assert.That(computerTanks[1].tankSaveId, Is.EqualTo("computer-idle"));
            Assert.That(computerTanks[1].hasMovementTarget, Is.False);
        }

        [Test]
        public void Save_WhenTankStateCannotBeCaptured_ReturnsFalseAndWritesNothing()
        {
            _tanks.CanCapture = false;

            Assert.That(_saveService.Save(), Is.False);
            Assert.That(File.Exists(_savePath), Is.False);
        }

        [Test]
        public void Load_WithoutSaveFile_ReturnsFalseAndRestoresNothing()
        {
            LogAssert.Expect(LogType.Warning, new Regex("No game save found"));

            Assert.That(_saveService.Load(), Is.False);
            Assert.That(_tanks.RestoredStates, Is.Empty);
            Assert.That(_camera.RestoredStates, Is.Empty);
        }

        [Test]
        public void Load_WithCorruptedFile_ReturnsFalseAndRestoresNothing()
        {
            WriteSaveFile("this is not json");
            LogAssert.Expect(
                LogType.Error,
                new Regex("Could not read game save|unsupported format"));

            Assert.That(_saveService.Load(), Is.False);
            Assert.That(_tanks.RestoredStates, Is.Empty);
            Assert.That(_camera.RestoredStates, Is.Empty);
        }

        [Test]
        public void Load_WithUnsupportedVersion_ReturnsFalseAndRestoresNothing()
        {
            Assert.That(_saveService.Save(), Is.True);
            var saveData = JsonUtility.FromJson<GameSaveData>(File.ReadAllText(_savePath));
            saveData.version += 1;
            WriteSaveFile(JsonUtility.ToJson(saveData));
            LogAssert.Expect(LogType.Error, new Regex("unsupported format"));

            Assert.That(_saveService.Load(), Is.False);
            Assert.That(_tanks.RestoredStates, Is.Empty);
            Assert.That(_camera.RestoredStates, Is.Empty);
        }

        [Test]
        public void Load_WhenTankStateCannotBeRestored_DoesNotRestoreTheCamera()
        {
            Assert.That(_saveService.Save(), Is.True);
            _tanks.CanRestore = false;

            Assert.That(_saveService.Load(), Is.False);
            Assert.That(_tanks.RestoredStates, Has.Count.EqualTo(1));
            Assert.That(_camera.RestoredStates, Is.Empty);
        }

        private void WriteSaveFile(string contents)
        {
            Directory.CreateDirectory(Path.GetDirectoryName(_savePath));
            File.WriteAllText(_savePath, contents);
        }

        private void DeleteSaveFile()
        {
            if (File.Exists(_savePath))
            {
                File.Delete(_savePath);
            }
        }
    }
}
