using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using Config;
using GameFlow.Signals;
using GameSaving;
using NUnit.Framework;
using Projectile;
using TankControl;
using TankSpawning;
using UnityEditor;
using UnityEngine;
using UnityEngine.TestTools;
using Zenject;

namespace Tanks.Tests
{
    /// <summary>
    /// Integration tests: they wire the real tank spawner and persistence through Zenject and
    /// instantiate the real tank prefab, tank config and projectile set from the project.
    /// </summary>
    [TestFixture]
    [Category("Integration")]
    public sealed class TankSpawningAndPersistenceTests
    {
        private const string TankPrefabPath = "Assets/Prefabs/EnemyTank.prefab";
        private const string TankConfigPath = "Assets/Config/Tanks/PlayerTankConfig.asset";
        private const string ProjectileSetPath = "Assets/Config/Projectiles/CartoonProjectileSet.asset";
        private const float RotationToleranceDegrees = 0.1f;

        private static readonly SpawnEntry PlayerEntry = new("player-test", TankControlType.LocalPlayer);
        private static readonly SpawnEntry ComputerEntry = new("computer-test", TankControlType.Computer);

        private readonly List<TankSpawningConfig> _configs = new();

        private ITankSpawner _spawner;
        private ITankStatePersistence _statePersistence;
        private RecordingUserTankRegistry _userRegistry;
        private RecordingComputerTankRegistry _computerRegistry;
        private RecordingCameraTargetRegistry _cameraRegistry;
        private RecordingMovementRestorer _movementRestorer;

        [SetUp]
        public void SetUp()
        {
            BuildSystem(PlayerEntry, ComputerEntry);
        }

        [TearDown]
        public void TearDown()
        {
            foreach (var chassis in _userRegistry.Registered
                         .Concat(_computerRegistry.Registered.Select(registration => registration.Chassis))
                         .Distinct())
            {
                if (chassis is Component component)
                {
                    Object.DestroyImmediate(component.gameObject);
                }
            }

            foreach (var config in _configs)
            {
                Object.DestroyImmediate(config);
            }

            _configs.Clear();
        }

        [Test]
        public void Spawn_LocalPlayer_RegistersWithUserCameraAndComputerTargetRegistries()
        {
            _spawner.Spawn(TankControlType.LocalPlayer, Vector3.zero, Quaternion.identity);

            Assert.That(_userRegistry.Registered, Has.Count.EqualTo(1));
            Assert.That(_cameraRegistry.Registered, Has.Count.EqualTo(1));
            Assert.That(_computerRegistry.PlayerTargets, Has.Count.EqualTo(1));
            Assert.That(_computerRegistry.PlayerTargets[0], Is.SameAs(_userRegistry.Registered[0]));
            Assert.That(_computerRegistry.Registered, Is.Empty);
        }

        [Test]
        public void Spawn_ComputerTank_RegistersOnlyWithTheComputerRegistry()
        {
            _spawner.Spawn(
                TankControlType.Computer,
                ComputerTankBehavior.RandomPatrol,
                Vector3.right,
                Quaternion.identity);

            Assert.That(_computerRegistry.Registered, Has.Count.EqualTo(1));
            Assert.That(_computerRegistry.Registered[0].Behavior, Is.EqualTo(ComputerTankBehavior.RandomPatrol));
            Assert.That(_userRegistry.Registered, Is.Empty);
            Assert.That(_cameraRegistry.Registered, Is.Empty);
            Assert.That(_computerRegistry.PlayerTargets, Is.Empty);
        }

        [Test]
        public void Despawn_LocalPlayer_UnregistersFromEveryPlayerRegistry()
        {
            var localPlayer = _spawner.Spawn(TankControlType.LocalPlayer, Vector3.zero, Quaternion.identity);

            Assert.That(_spawner.Despawn(localPlayer), Is.True);

            Assert.That(_userRegistry.Unregistered, Has.Count.EqualTo(1));
            Assert.That(_cameraRegistry.Unregistered, Has.Count.EqualTo(1));
            Assert.That(_computerRegistry.ClearedPlayerTargets, Has.Count.EqualTo(1));
        }

        [Test]
        public void Despawn_ComputerTank_UnregistersItAndRejectsASecondDespawn()
        {
            var computer = _spawner.Spawn(
                TankControlType.Computer,
                ComputerTankBehavior.RandomPatrol,
                Vector3.right,
                Quaternion.identity);

            Assert.That(_spawner.Despawn(computer), Is.True);
            Assert.That(_spawner.Despawn(computer), Is.False);
            Assert.That(_spawner.Despawn(null), Is.False);

            Assert.That(_computerRegistry.Unregistered, Has.Count.EqualTo(1));
        }

        [Test]
        public void Spawn_AfterDespawn_ReusesThePooledComputerTank()
        {
            var first = _spawner.Spawn(
                TankControlType.Computer,
                ComputerTankBehavior.RandomPatrol,
                Vector3.right,
                Quaternion.identity);
            var firstChassis = _computerRegistry.Registered[0].Chassis;
            _spawner.Despawn(first);

            _spawner.Spawn(
                TankControlType.Computer,
                ComputerTankBehavior.RandomPatrol,
                Vector3.forward,
                Quaternion.identity);

            Assert.That(_computerRegistry.Registered, Has.Count.EqualTo(2));
            Assert.That(_computerRegistry.Registered[1].Chassis, Is.SameAs(firstChassis));
        }

        [Test]
        public void TryCaptureState_WithoutTanks_ReturnsAnEmptyState()
        {
            Assert.That(_statePersistence.TryCaptureState(out var state), Is.True);

            Assert.That(state.playerTank, Is.Null);
            Assert.That(state.computerTanks, Is.Empty);
        }

        [Test]
        public void TryCaptureState_WithSpawnedTanks_ReportsSaveIdsPositionsAndRotations()
        {
            var playerPosition = new Vector3(1f, 0f, 2f);
            var computerPosition = new Vector3(-3f, 0f, 5f);
            _spawner.Spawn(TankControlType.LocalPlayer, playerPosition, Quaternion.Euler(0f, 45f, 0f));
            _spawner.Spawn(
                TankControlType.Computer,
                ComputerTankBehavior.RandomPatrol,
                computerPosition,
                Quaternion.Euler(0f, 200f, 0f));

            Assert.That(_statePersistence.TryCaptureState(out var state), Is.True);

            Assert.That(state.playerTank.tankSaveId, Is.EqualTo(PlayerEntry.SaveId));
            Assert.That(state.playerTank.position, Is.EqualTo(playerPosition));
            AssertSameRotation(state.playerTank.chassisRotation, Quaternion.Euler(0f, 45f, 0f));
            Assert.That(state.computerTanks, Has.Count.EqualTo(1));
            Assert.That(state.computerTanks[0].tankSaveId, Is.EqualTo(ComputerEntry.SaveId));
            Assert.That(state.computerTanks[0].position, Is.EqualTo(computerPosition));
            AssertSameRotation(state.computerTanks[0].chassisRotation, Quaternion.Euler(0f, 200f, 0f));
        }

        [Test]
        public void TryCaptureState_TankWithoutSaveId_Fails()
        {
            BuildSystem(new SpawnEntry(string.Empty, TankControlType.LocalPlayer));
            _spawner.Spawn(TankControlType.LocalPlayer, Vector3.zero, Quaternion.identity);
            LogAssert.Expect(LogType.Error, new Regex("has no save ID"));

            Assert.That(_statePersistence.TryCaptureState(out var state), Is.False);
            Assert.That(state, Is.Null);
        }

        [Test]
        public void TryCaptureState_TwoLocalPlayerTanks_Fails()
        {
            _spawner.Spawn(TankControlType.LocalPlayer, Vector3.zero, Quaternion.identity);
            _spawner.Spawn(TankControlType.LocalPlayer, Vector3.right, Quaternion.identity);
            LogAssert.Expect(LogType.Error, new Regex("Only one local player tank can be saved"));

            Assert.That(_statePersistence.TryCaptureState(out var state), Is.False);
            Assert.That(state, Is.Null);
        }

        [Test]
        public void RestoreState_RecreatesTanksAndRestoresComputerMovementTarget()
        {
            var playerPosition = new Vector3(1f, 0f, 2f);
            var computerPosition = new Vector3(-3f, 0f, 5f);
            var movementTarget = new Vector3(7f, 0f, -4f);
            var state = new TankRuntimeSaveState
            {
                playerTank = CreateSaveData(PlayerEntry.SaveId, playerPosition, 30f, 60f),
                computerTanks = new List<ComputerTankSaveData>
                {
                    CreateComputerSaveData(ComputerEntry.SaveId, computerPosition, movementTarget)
                }
            };

            var restored = _statePersistence.TryRestoreState(state);

            Assert.That(restored, Is.True);
            Assert.That(_movementRestorer.Requests, Has.Count.EqualTo(1));
            Assert.That(_movementRestorer.Requests[0].Destination, Is.EqualTo(movementTarget));
            Assert.That(_statePersistence.TryCaptureState(out var capturedState), Is.True);
            Assert.That(capturedState.playerTank.tankSaveId, Is.EqualTo(PlayerEntry.SaveId));
            Assert.That(capturedState.playerTank.position, Is.EqualTo(playerPosition));
            AssertSameRotation(capturedState.playerTank.chassisRotation, Quaternion.Euler(0f, 30f, 0f));
            AssertSameRotation(capturedState.playerTank.turretRotation, Quaternion.Euler(0f, 60f, 0f));
            Assert.That(capturedState.computerTanks, Has.Count.EqualTo(1));
            Assert.That(capturedState.computerTanks[0].tankSaveId, Is.EqualTo(ComputerEntry.SaveId));
            Assert.That(capturedState.computerTanks[0].position, Is.EqualTo(computerPosition));
        }

        [Test]
        public void RestoreState_ComputerTankWithoutMovementTarget_DoesNotRequestMovementRestore()
        {
            var state = new TankRuntimeSaveState
            {
                computerTanks = new List<ComputerTankSaveData>
                {
                    new()
                    {
                        tankSaveId = ComputerEntry.SaveId,
                        chassisRotation = Quaternion.identity,
                        turretRotation = Quaternion.identity,
                        hasMovementTarget = false
                    }
                }
            };

            Assert.That(_statePersistence.TryRestoreState(state), Is.True);
            Assert.That(_movementRestorer.Requests, Is.Empty);
        }

        [Test]
        public void RestoreState_ReplacesTheCurrentPopulation()
        {
            _spawner.Spawn(TankControlType.LocalPlayer, new Vector3(9f, 0f, 9f), Quaternion.identity);
            _spawner.Spawn(
                TankControlType.Computer,
                ComputerTankBehavior.RandomPatrol,
                Vector3.right,
                Quaternion.identity);
            var restoredPosition = new Vector3(1f, 0f, 2f);

            var restored = _statePersistence.TryRestoreState(new TankRuntimeSaveState
            {
                playerTank = CreateSaveData(PlayerEntry.SaveId, restoredPosition, 0f, 0f)
            });

            Assert.That(restored, Is.True);
            Assert.That(_userRegistry.Unregistered, Has.Count.EqualTo(1));
            Assert.That(_computerRegistry.Unregistered, Has.Count.EqualTo(1));
            Assert.That(_statePersistence.TryCaptureState(out var state), Is.True);
            Assert.That(state.playerTank.position, Is.EqualTo(restoredPosition));
            Assert.That(state.computerTanks, Is.Empty);
        }

        [Test]
        public void RestoreState_NullState_DoesNotReplaceTheCurrentPopulation()
        {
            AssertRestoreFailsAndKeepsPopulation(null, "Cannot restore an empty tank save state");
        }

        [Test]
        public void RestoreState_UnknownSaveId_DoesNotReplaceTheCurrentPopulation()
        {
            var state = new TankRuntimeSaveState
            {
                playerTank = CreateSaveData("missing-tank", Vector3.zero, 0f, 0f)
            };

            AssertRestoreFailsAndKeepsPopulation(state, "No configured tank matches save ID 'missing-tank'");
        }

        [Test]
        public void RestoreState_PlayerWithoutSaveId_DoesNotReplaceTheCurrentPopulation()
        {
            var state = new TankRuntimeSaveState
            {
                playerTank = CreateSaveData(string.Empty, Vector3.zero, 0f, 0f)
            };

            AssertRestoreFailsAndKeepsPopulation(state, "A saved tank has no save ID");
        }

        [Test]
        public void RestoreState_PlayerUsingAComputerSaveId_DoesNotReplaceTheCurrentPopulation()
        {
            var state = new TankRuntimeSaveState
            {
                playerTank = CreateSaveData(ComputerEntry.SaveId, Vector3.zero, 0f, 0f)
            };

            AssertRestoreFailsAndKeepsPopulation(state, "does not belong to 'LocalPlayer'");
        }

        [Test]
        public void RestoreState_ComputerUsingAPlayerSaveId_DoesNotReplaceTheCurrentPopulation()
        {
            var state = new TankRuntimeSaveState
            {
                computerTanks = new List<ComputerTankSaveData>
                {
                    CreateComputerSaveData(PlayerEntry.SaveId, Vector3.zero, Vector3.zero)
                }
            };

            AssertRestoreFailsAndKeepsPopulation(state, "does not belong to 'Computer'");
        }

        [Test]
        public void RestoreState_SaveIdConfiguredTwice_DoesNotReplaceTheCurrentPopulation()
        {
            var duplicated = new SpawnEntry("duplicated", TankControlType.Computer);
            BuildSystem(PlayerEntry, duplicated, duplicated);
            var state = new TankRuntimeSaveState
            {
                computerTanks = new List<ComputerTankSaveData>
                {
                    CreateComputerSaveData(duplicated.SaveId, Vector3.zero, Vector3.zero)
                }
            };

            AssertRestoreFailsAndKeepsPopulation(state, "configured more than once");
        }

        [Test]
        public void RestoreState_NullComputerEntry_DoesNotReplaceTheCurrentPopulation()
        {
            var state = new TankRuntimeSaveState
            {
                computerTanks = new List<ComputerTankSaveData> { null }
            };

            AssertRestoreFailsAndKeepsPopulation(state, null);
        }

        private void AssertRestoreFailsAndKeepsPopulation(TankRuntimeSaveState state, string expectedError)
        {
            _spawner.Spawn(TankControlType.LocalPlayer, new Vector3(2f, 0f, 0f), Quaternion.identity);
            Assert.That(_statePersistence.TryCaptureState(out var stateBefore), Is.True);
            if (expectedError != null)
            {
                LogAssert.Expect(LogType.Error, new Regex(expectedError));
            }

            var restored = _statePersistence.TryRestoreState(state);

            Assert.That(restored, Is.False);
            Assert.That(_statePersistence.TryCaptureState(out var stateAfter), Is.True);
            Assert.That(stateAfter.playerTank.tankSaveId, Is.EqualTo(stateBefore.playerTank.tankSaveId));
            Assert.That(stateAfter.playerTank.position, Is.EqualTo(stateBefore.playerTank.position));
            Assert.That(stateAfter.computerTanks, Has.Count.EqualTo(stateBefore.computerTanks.Count));
            Assert.That(_userRegistry.Unregistered, Is.Empty);
        }

        // Builds a fresh spawner and persistence for the given configuration. Call it before spawning
        // any tank, because the previous registries (and the tanks they track) are replaced.
        private void BuildSystem(params SpawnEntry[] entries)
        {
            var config = CreateSpawningConfig(entries);
            _configs.Add(config);
            _userRegistry = new RecordingUserTankRegistry();
            _computerRegistry = new RecordingComputerTankRegistry();
            _cameraRegistry = new RecordingCameraTargetRegistry();
            _movementRestorer = new RecordingMovementRestorer();

            var container = new DiContainer();
            SignalBusInstaller.Install(container);
            container.DeclareSignal<GameplayStartedSignal>().OptionalSubscriber();
            container.DeclareSignal<PlayerTankDestroyedSignal>().OptionalSubscriber();
            container.DeclareSignal<PlayerRespawnRequestedSignal>().OptionalSubscriber();
            container.DeclareSignal<PlayerTankRespawnedSignal>().OptionalSubscriber();
            container.Bind<IUserTankControlTargetRegistry>().FromInstance(_userRegistry).AsSingle();
            container.Bind<IComputerTankControlTargetRegistry>().FromInstance(_computerRegistry).AsSingle();
            container.Bind<CameraControl.IPlayerTankCameraTargetRegistry>().FromInstance(_cameraRegistry).AsSingle();
            container.Bind<IComputerTankMovementRestorer>().FromInstance(_movementRestorer).AsSingle();
            container.Bind<MapGeneration.INavMeshSpawnPointFinder>().FromInstance(new FakeSpawnPointFinder()).AsSingle();
            container.Bind<IProjectileEmitter>().FromInstance(new FakeProjectileEmitter()).AsSingle();
            TankSpawningBindings.Install(container, config);

            _spawner = container.Resolve<ITankSpawner>();
            _statePersistence = container.Resolve<ITankStatePersistence>();
        }

        private static TankSaveData CreateSaveData(
            string saveId,
            Vector3 position,
            float chassisYaw,
            float turretYaw)
        {
            return new TankSaveData
            {
                tankSaveId = saveId,
                position = position,
                chassisRotation = Quaternion.Euler(0f, chassisYaw, 0f),
                turretRotation = Quaternion.Euler(0f, turretYaw, 0f)
            };
        }

        private static ComputerTankSaveData CreateComputerSaveData(
            string saveId,
            Vector3 position,
            Vector3 movementTarget)
        {
            return new ComputerTankSaveData
            {
                tankSaveId = saveId,
                position = position,
                chassisRotation = Quaternion.Euler(0f, 90f, 0f),
                turretRotation = Quaternion.Euler(0f, 120f, 0f),
                hasMovementTarget = true,
                movementTarget = movementTarget
            };
        }

        private static void AssertSameRotation(Quaternion actual, Quaternion expected)
        {
            Assert.That(Quaternion.Angle(actual, expected), Is.LessThan(RotationToleranceDegrees));
        }

        private static TankSpawningConfig CreateSpawningConfig(SpawnEntry[] entries)
        {
            var config = ScriptableObject.CreateInstance<TankSpawningConfig>();
            var serializedConfig = new SerializedObject(config);
            var spawnData = serializedConfig.FindProperty("tankSpawnData");
            spawnData.arraySize = entries.Length;
            for (var index = 0; index < entries.Length; index++)
            {
                ConfigureSpawnData(spawnData.GetArrayElementAtIndex(index), entries[index]);
            }

            serializedConfig.ApplyModifiedPropertiesWithoutUndo();
            return config;
        }

        private static void ConfigureSpawnData(SerializedProperty entry, SpawnEntry spawnEntry)
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(TankPrefabPath);
            var tankConfig = AssetDatabase.LoadAssetAtPath<TankConfig>(TankConfigPath);
            var projectileSet = AssetDatabase.LoadAssetAtPath<ProjectileSetConfig>(ProjectileSetPath);
            Assert.That(prefab, Is.Not.Null, $"Missing test prefab at '{TankPrefabPath}'.");
            Assert.That(tankConfig, Is.Not.Null, $"Missing test tank config at '{TankConfigPath}'.");
            Assert.That(projectileSet, Is.Not.Null, $"Missing projectile set at '{ProjectileSetPath}'.");

            entry.FindPropertyRelative("saveId").stringValue = spawnEntry.SaveId;
            entry.FindPropertyRelative("prefab").objectReferenceValue = prefab;
            entry.FindPropertyRelative("controlType").enumValueIndex = (int)spawnEntry.ControlType;
            entry.FindPropertyRelative("spawnChance").floatValue = 1f;
            entry.FindPropertyRelative("tankConfig").objectReferenceValue = tankConfig;
            entry.FindPropertyRelative("projectileSet").objectReferenceValue = projectileSet;
            entry.FindPropertyRelative("computerBehavior").enumValueIndex = (int)ComputerTankBehavior.RandomPatrol;
        }

        private readonly struct SpawnEntry
        {
            public SpawnEntry(string saveId, TankControlType controlType)
            {
                SaveId = saveId;
                ControlType = controlType;
            }

            public string SaveId { get; }
            public TankControlType ControlType { get; }
        }
    }
}
