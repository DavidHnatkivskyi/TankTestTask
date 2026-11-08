using System.Collections.Generic;
using Config;
using MapGeneration;
using Unity.AI.Navigation;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.AI;

namespace MapGeneration.Editor
{
    public sealed class MapLandscapeGeneratorWindow : EditorWindow
    {
        private const float PlaneSize = 10f;
        private const int PlacementAttemptsPerObstacle = 30;
        private const float MinimumWallSize = 0.001f;

        private MapElementsConfig _elementsConfig;
        private Camera _targetCamera;
        private Vector2 _approximateMapSize = new(40f, 40f);
        private int _obstacleCount = 10;
        private float _minimumObstacleDistance = 1f;
        private float _cameraHeight = 25f;
        private bool _replaceExistingMap = true;
        private IReadOnlyList<WallSegment> _availableWallSegments;
        private IReadOnlyList<WallSegment> _horizontalWallSegments;
        private IReadOnlyList<WallSegment> _verticalWallSegments;
        private Vector2 _generatedMapSize;
        private float _maximumWallThickness;

        [MenuItem("Tools/Tank Game/Map Landscape Generator")]
        private static void Open()
        {
            GetWindow<MapLandscapeGeneratorWindow>("Map Landscape Generator");
        }

        private void OnGUI()
        {
            EditorGUILayout.LabelField("Map Elements", EditorStyles.boldLabel);
            _elementsConfig = (MapElementsConfig)EditorGUILayout.ObjectField(
                "Elements Config", _elementsConfig, typeof(MapElementsConfig), false);

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Landscape", EditorStyles.boldLabel);
            _approximateMapSize = EditorGUILayout.Vector2Field("Approximate Map Size (X, Z)", _approximateMapSize);
            _obstacleCount = EditorGUILayout.IntField("Obstacle Count", _obstacleCount);
            _minimumObstacleDistance = EditorGUILayout.FloatField(
                "Minimum Obstacle Distance", _minimumObstacleDistance);
            _replaceExistingMap = EditorGUILayout.Toggle("Replace Generated Map", _replaceExistingMap);

            if (TryGetWallSegments(out _))
            {
                EditorGUILayout.HelpBox(
                    "Map size is approximate. The generator randomly selects wall segments until each side reaches the requested size, so the generated map can be larger.",
                    MessageType.Info);
            }

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Camera", EditorStyles.boldLabel);
            _targetCamera ??= Camera.main;
            _targetCamera = (Camera)EditorGUILayout.ObjectField(
                "Target Camera", _targetCamera, typeof(Camera), true);
            _cameraHeight = EditorGUILayout.FloatField("Camera Height", _cameraHeight);

            EditorGUILayout.Space();
            using (new EditorGUI.DisabledScope(!CanGenerate()))
            {
                if (GUILayout.Button("Generate Landscape"))
                {
                    GenerateLandscape();
                }
            }

            if (_elementsConfig == null)
            {
                EditorGUILayout.HelpBox(
                    "Create a Map Elements Config via Assets/Create/Config/Map Elements and assign prefabs for Wall and Obstacle.",
                    MessageType.Info);
            }
            else if (!HasAllElementPrefabs())
            {
                EditorGUILayout.HelpBox(
                    "The config must contain a prefab for each element type: Wall and Obstacle.",
                    MessageType.Warning);
            }

            if (_elementsConfig != null && !TryGetWallSegments(out _))
            {
                EditorGUILayout.HelpBox(
                    "Every Wall prefab needs a Renderer with a non-zero local X length and local Z thickness.",
                    MessageType.Warning);
            }

            if (_targetCamera == null)
            {
                EditorGUILayout.HelpBox("Assign a camera or tag it as MainCamera.", MessageType.Warning);
            }
        }

        private bool CanGenerate()
        {
            return _elementsConfig != null &&
                   HasAllElementPrefabs() &&
                   TryGetWallSegments(out _) &&
                   _approximateMapSize.x > 0f &&
                   _approximateMapSize.y > 0f &&
                   _obstacleCount >= 0 &&
                   _minimumObstacleDistance >= 0f &&
                   _cameraHeight > 0f &&
                   _targetCamera != null;
        }

        private bool HasAllElementPrefabs()
        {
            return _elementsConfig.TryGetPrefab(MapElementType.Wall, out _) &&
                   _elementsConfig.TryGetPrefab(MapElementType.Obstacle, out _);
        }

        private void GenerateLandscape()
        {
            TryGetWallSegments(out var wallSegments);
            _availableWallSegments = wallSegments;
            _horizontalWallSegments = CreateWallSequence(_approximateMapSize.x);
            _verticalWallSegments = CreateWallSequence(_approximateMapSize.y);
            _generatedMapSize = new Vector2(
                GetWallSequenceLength(_horizontalWallSegments),
                GetWallSequenceLength(_verticalWallSegments));
            _maximumWallThickness = GetMaximumWallThickness(_availableWallSegments);

            Undo.IncrementCurrentGroup();
            Undo.SetCurrentGroupName("Generate Tank Map Landscape");

            if (_replaceExistingMap)
            {
                RemoveExistingGeneratedMaps();
            }

            WarnAboutMissingColliders();

            var mapRoot = new GameObject("Generated Map");
            Undo.RegisterCreatedObjectUndo(mapRoot, "Create Generated Map");
            mapRoot.AddComponent<GeneratedMap>();

            var groundRoot = CreateContainer("Ground", mapRoot.transform);
            var wallsRoot = CreateContainer("Walls", mapRoot.transform);
            var obstaclesRoot = CreateContainer("Obstacles", mapRoot.transform);

            CreateGround(groundRoot.transform);
            CreateWalls(wallsRoot.transform);
            Physics.SyncTransforms();
            CreateObstacles(obstaclesRoot.transform, wallsRoot.GetComponentsInChildren<Collider>());
            BuildNavMesh(mapRoot);
            PositionCamera(mapRoot.transform.position);

            Selection.activeGameObject = mapRoot;
            EditorSceneManager.MarkSceneDirty(mapRoot.scene);
            Undo.CollapseUndoOperations(Undo.GetCurrentGroup());
        }

        private void RemoveExistingGeneratedMaps()
        {
            foreach (var generatedMap in FindObjectsByType<GeneratedMap>(FindObjectsSortMode.None))
            {
                Undo.DestroyObjectImmediate(generatedMap.gameObject);
            }
        }

        private GameObject CreateContainer(string name, Transform parent)
        {
            var container = new GameObject(name);
            Undo.RegisterCreatedObjectUndo(container, "Create Map Container");
            container.transform.SetParent(parent);
            return container;
        }

        private void CreateGround(Transform parent)
        {
            var ground = GameObject.CreatePrimitive(PrimitiveType.Plane);
            Undo.RegisterCreatedObjectUndo(ground, "Create Map Ground");
            ground.name = "Ground";
            ground.transform.SetParent(parent);
            ground.transform.localScale = new Vector3(
                _generatedMapSize.x / PlaneSize,
                1f,
                _generatedMapSize.y / PlaneSize);

            if (_elementsConfig.GroundMaterial != null)
            {
                ground.GetComponent<MeshRenderer>().sharedMaterial = _elementsConfig.GroundMaterial;
            }
        }

        private void CreateWalls(Transform parent)
        {
            var halfWidth = _generatedMapSize.x * 0.5f;
            var halfDepth = _generatedMapSize.y * 0.5f;

            CreateHorizontalWalls(_horizontalWallSegments, "North Wall", halfDepth, parent);
            CreateHorizontalWalls(_horizontalWallSegments, "South Wall", -halfDepth, parent);
            CreateVerticalWalls(_verticalWallSegments, "East Wall", halfWidth, parent);
            CreateVerticalWalls(_verticalWallSegments, "West Wall", -halfWidth, parent);
        }

        private void CreateHorizontalWalls(
            IReadOnlyList<WallSegment> segments,
            string sideName,
            float zPosition,
            Transform parent)
        {
            var xPosition = -_generatedMapSize.x * 0.5f;
            for (var index = 0; index < segments.Count; index++)
            {
                var segment = segments[index];
                xPosition += segment.Length * 0.5f;
                var wall = CreateWallSegment(
                    segment,
                    $"{sideName} {index + 1}",
                    new Vector3(xPosition, 0f, zPosition),
                    parent);
                ConfigureAsNavMeshBlocker(wall);
                xPosition += segment.Length * 0.5f;
            }
        }

        private void CreateVerticalWalls(
            IReadOnlyList<WallSegment> segments,
            string sideName,
            float xPosition,
            Transform parent)
        {
            var zPosition = -_generatedMapSize.y * 0.5f;
            for (var index = 0; index < segments.Count; index++)
            {
                var segment = segments[index];
                zPosition += segment.Length * 0.5f;
                var wall = CreateWallSegment(segment, $"{sideName} {index + 1}", new Vector3(xPosition, 0f, zPosition), parent);
                wall.transform.localRotation = Quaternion.Euler(0f, 90f, 0f);
                ConfigureAsNavMeshBlocker(wall);
                zPosition += segment.Length * 0.5f;
            }
        }

        private GameObject CreateWallSegment(WallSegment segment, string name, Vector3 position, Transform parent)
        {
            return CreateElement(segment.Prefab, name, position, parent);
        }

        private void CreateObstacles(Transform parent, IReadOnlyList<Collider> wallColliders)
        {
            var obstaclePrefabs = _elementsConfig.GetPrefabs(MapElementType.Obstacle);
            var placedObstacleColliders = new List<Collider>(_obstacleCount);

            for (var index = 0; index < _obstacleCount; index++)
            {
                GameObject obstacle = null;
                for (var attempt = 0; attempt < PlacementAttemptsPerObstacle; attempt++)
                {
                    var position = new Vector3(
                        Random.Range(-_generatedMapSize.x * 0.5f + _maximumWallThickness,
                            _generatedMapSize.x * 0.5f - _maximumWallThickness),
                        0f,
                        Random.Range(-_generatedMapSize.y * 0.5f + _maximumWallThickness,
                            _generatedMapSize.y * 0.5f - _maximumWallThickness));
                    var prefab = obstaclePrefabs[Random.Range(0, obstaclePrefabs.Count)];
                    var candidate = (GameObject)PrefabUtility.InstantiatePrefab(prefab, parent);
                    candidate.transform.SetLocalPositionAndRotation(
                        position,
                        Quaternion.Euler(0f, Random.Range(0f, 360f), 0f));
                    Physics.SyncTransforms();

                    var candidateColliders = candidate.GetComponentsInChildren<Collider>();
                    if (candidateColliders.Length > 0 &&
                        !AreCollidersOverlapping(candidateColliders, wallColliders) &&
                        AreCollidersFarEnough(candidateColliders, placedObstacleColliders))
                    {
                        obstacle = candidate;
                        break;
                    }

                    DestroyImmediate(candidate);
                }

                if (obstacle == null)
                {
                    Debug.LogWarning(
                        $"Placed {index} of {_obstacleCount} obstacles. Increase map size or reduce the minimum obstacle distance.",
                        this);
                    break;
                }

                obstacle.name = $"Obstacle {index + 1}";
                ConfigureAsNavMeshBlocker(obstacle);
                Undo.RegisterCreatedObjectUndo(obstacle, $"Create {obstacle.name}");
                placedObstacleColliders.AddRange(obstacle.GetComponentsInChildren<Collider>());
            }
        }

        private static void ConfigureAsNavMeshBlocker(GameObject element)
        {
            var modifier = element.AddComponent<NavMeshModifier>();
            modifier.overrideArea = true;
            modifier.area = NavMesh.GetAreaFromName("Not Walkable");
        }

        private void WarnAboutMissingColliders()
        {
            WarnAboutMissingColliders(MapElementType.Wall);
            WarnAboutMissingColliders(MapElementType.Obstacle);
        }

        private void WarnAboutMissingColliders(MapElementType elementType)
        {
            foreach (var prefab in _elementsConfig.GetPrefabs(elementType))
            {
                if (prefab.GetComponentInChildren<Collider>() == null)
                {
                    Debug.LogWarning($"{elementType} prefab '{prefab.name}' has no Collider.", prefab);
                }
            }
        }

        private static bool AreCollidersOverlapping(
            IReadOnlyList<Collider> candidateColliders,
            IReadOnlyList<Collider> blockingColliders)
        {
            foreach (var candidateCollider in candidateColliders)
            {
                foreach (var blockingCollider in blockingColliders)
                {
                    if (candidateCollider.bounds.Intersects(blockingCollider.bounds))
                    {
                        return true;
                    }
                }
            }

            return false;
        }

        private bool AreCollidersFarEnough(
            IReadOnlyList<Collider> candidateColliders,
            IReadOnlyList<Collider> blockingColliders)
        {
            var minimumDistanceSquared = _minimumObstacleDistance * _minimumObstacleDistance;
            foreach (var candidateCollider in candidateColliders)
            {
                foreach (var blockingCollider in blockingColliders)
                {
                    if (GetSquaredBoundsDistance(candidateCollider.bounds, blockingCollider.bounds) < minimumDistanceSquared)
                    {
                        return false;
                    }
                }
            }

            return true;
        }

        private static float GetSquaredBoundsDistance(Bounds first, Bounds second)
        {
            var xDistance = Mathf.Max(first.min.x - second.max.x, second.min.x - first.max.x, 0f);
            var yDistance = Mathf.Max(first.min.y - second.max.y, second.min.y - first.max.y, 0f);
            var zDistance = Mathf.Max(first.min.z - second.max.z, second.min.z - first.max.z, 0f);
            return xDistance * xDistance + yDistance * yDistance + zDistance * zDistance;
        }

        private GameObject CreateElement(GameObject prefab, string name, Vector3 localPosition, Transform parent)
        {
            var element = (GameObject)PrefabUtility.InstantiatePrefab(prefab, parent);
            Undo.RegisterCreatedObjectUndo(element, $"Create {name}");
            element.name = name;
            element.transform.localPosition = localPosition;
            element.transform.localRotation = Quaternion.identity;
            return element;
        }

        private static void BuildNavMesh(GameObject mapRoot)
        {
            var surface = mapRoot.AddComponent<NavMeshSurface>();
            surface.collectObjects = CollectObjects.Children;
            surface.useGeometry = NavMeshCollectGeometry.PhysicsColliders;
            surface.BuildNavMesh();
            EditorUtility.SetDirty(surface);
        }

        private void PositionCamera(Vector3 mapCenter)
        {
            var cameraTransform = _targetCamera.transform;
            Undo.RecordObject(cameraTransform, "Position Map Camera");
            cameraTransform.SetPositionAndRotation(
                mapCenter + Vector3.up * _cameraHeight,
                Quaternion.Euler(90f, 0f, 0f));
        }

        private bool TryGetWallSegments(out List<WallSegment> wallSegments)
        {
            wallSegments = new List<WallSegment>();

            if (_elementsConfig == null)
            {
                return false;
            }

            var wallPrefabs = _elementsConfig.GetPrefabs(MapElementType.Wall);
            foreach (var wallPrefab in wallPrefabs)
            {
                var renderer = wallPrefab.GetComponentInChildren<Renderer>();
                if (renderer == null)
                {
                    return false;
                }

                var size = renderer.bounds.size;
                if (size.x <= MinimumWallSize || size.z <= MinimumWallSize)
                {
                    return false;
                }

                wallSegments.Add(new WallSegment(wallPrefab, size.x, size.z));
            }

            return wallSegments.Count > 0;
        }

        private IReadOnlyList<WallSegment> CreateWallSequence(float approximateLength)
        {
            var wallSegments = new List<WallSegment>();
            var currentLength = 0f;
            while (currentLength < approximateLength)
            {
                var segment = _availableWallSegments[Random.Range(0, _availableWallSegments.Count)];
                wallSegments.Add(segment);
                currentLength += segment.Length;
            }

            return wallSegments;
        }

        private static float GetWallSequenceLength(IReadOnlyList<WallSegment> wallSegments)
        {
            var length = 0f;
            foreach (var segment in wallSegments)
            {
                length += segment.Length;
            }

            return length;
        }

        private static float GetMaximumWallThickness(IReadOnlyList<WallSegment> wallSegments)
        {
            var maximumThickness = 0f;
            foreach (var segment in wallSegments)
            {
                maximumThickness = Mathf.Max(maximumThickness, segment.Thickness);
            }

            return maximumThickness;
        }

        private readonly struct WallSegment
        {
            public WallSegment(GameObject prefab, float length, float thickness)
            {
                Prefab = prefab;
                Length = length;
                Thickness = thickness;
            }

            public GameObject Prefab { get; }
            public float Length { get; }
            public float Thickness { get; }
        }
    }
}
