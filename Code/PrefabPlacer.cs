using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Code.Data;
using UnityEditor;
using UnityEngine;
using UnityEngine.Events;

namespace Code
{
    [ExecuteInEditMode]
    public class PrefabPlacer : MonoBehaviour
    {
        #region Internal

        [Serializable]
        public enum GenerationType
        {
            Quad = 0,
            Circle = 1,
            Mesh = 2
        }
        
        public enum DistributionType
        {
            Random = 0,
            Grid = 1,
        }

        #endregion

        [Header("Prefabs Settings")]
        [field: SerializeField]
        public PrefabDataCollectionSO dataCollection { get; private set; }

        [SerializeField] private UnityEvent ActionAfterGeneration;

        [SerializeField] public Terrain targetTerrain;


        [SerializeField] private Transform parentForGrass;
        [SerializeField] private Transform parentForTree;
        [SerializeField] private Transform parentForStumps;
        [SerializeField] private Transform parentForOther;

        [Header("Layer Mask")] [SerializeField]
        public LayerMask collideLayer;

        [SerializeField] public LayerMask targetLayer;

        [Header("Generation Settings")] public GenerationType generationType = GenerationType.Quad;
        public DistributionType distributionType = DistributionType.Random;
        
        public Vector2 size = new Vector2(10f, 10f);
        
        public Mesh sourceMesh;

        [Tooltip("Step for Grid.")]
        public float cellSize = 1f;


        [Tooltip("The maximum height for Raycast when searching for a point on the landscape.")]
        public float raycastHeight = 100f;
        
        public Vector3 randomRotationRange = new Vector3(0f, 360f, 0f);

        [Tooltip("Ignore triggers when Raycasting?")]
        public QueryTriggerInteraction triggerInteraction = QueryTriggerInteraction.Ignore;
        
        [HideInInspector] [SerializeField] private List<GameObject> spawnedObjects = new();

        [HideInInspector] public ReactiveProperty<float> progress;
        [HideInInspector] public bool taskRunning;

        private CancellationTokenSource _cts;


#if UNITY_EDITOR
        [ContextMenu("Clear Terrain")]
        public void ClearTerrain()
        {
            if (targetTerrain == null)
                targetTerrain = FindAnyObjectByType<Terrain>();

            switch (generationType)
            {
                case GenerationType.Quad:
                    ClearTerrainQuad(targetTerrain);
                    break;
                case GenerationType.Circle:
                    ClearTerrainCircle(targetTerrain);
                    break;
                case GenerationType.Mesh:
                    ClearTerrainMesh(targetTerrain);
                    break;
            }
        }

        [ContextMenu("Clear")]
        public void Clear()
        {
            if (spawnedObjects is { Count: <= 0 })
            {
                var spawnedPrefabs = new List<Transform>();
                spawnedPrefabs.AddRange(parentForGrass.GetComponentsInChildren<Transform>());
                spawnedPrefabs.AddRange(parentForTree.GetComponentsInChildren<Transform>());
                spawnedPrefabs.AddRange(parentForStumps.GetComponentsInChildren<Transform>());
                spawnedPrefabs.AddRange(parentForOther.GetComponentsInChildren<Transform>());

                spawnedPrefabs.ToArray();
                foreach (var child in spawnedPrefabs)
                {
                    if (child != parentForGrass
                        && child != parentForTree
                        && child != parentForStumps
                        && child != parentForOther
                        && child != null)
                    {
                        DestroyImmediate(child.gameObject);
                    }
                }
            }
            else
            {
                foreach (var go in spawnedObjects)
                {
                    if (go != null)
                    {
                        DestroyImmediate(go);
                    }
                }

                spawnedObjects.Clear();
            }
            
            ClearTerrain();

            EditorUtility.SetDirty(this);
        }

        public void CancelGeneration()
        {
            if (_cts != null)
                _cts.Cancel();
            taskRunning = false;
        }

        [ContextMenu("Generate")]
        public async void Generate()
        {
            try
            {
                progress = new ReactiveProperty<float>(0);
                _cts = new CancellationTokenSource();
                var token = _cts.Token;

                taskRunning = true;
                Clear();
                
                float area = CalculateSurfaceArea();
                Dictionary<PrefabInfo, int> indexes = new();
                Dictionary<PrefabInfo, List<Vector3>> infos = new();
                foreach (var info in dataCollection.Prefabs)
                {
                    if (info.storeToTerrain)
                    {
                        if (targetTerrain == null)
                        {
                            targetTerrain = FindAnyObjectByType<Terrain>();
                        }

                        if (indexes.Count == 0)
                        {
                            indexes = GetPrototypeIndexes(targetTerrain);
                        }
                    }

                    int count = 0;
                    switch (info.measureMode)
                    {
                        case PrefabInfo.CountMeasureMode.ByAbsoluteCount:
                            count = info.absoluteCount;
                            break;
                        case PrefabInfo.CountMeasureMode.ByDensity:
                            // допустим, densityPerSquareMeter
                            count = Mathf.RoundToInt(info.density * area);
                            break;
                    }
                    
                    var points = GeneratePoints(count);
                    var outPoints = new List<Vector3>();
                    for (int i = 0; i < points.Count; i++)
                    {
                        Vector3 rayStart = points[i] + Vector3.up * raycastHeight;
                        if (Physics.Raycast(rayStart, Vector3.down, out var hit, raycastHeight * 2f,
                                collideLayer, triggerInteraction) && hit.collider != null)
                        {
                            if (targetLayer.CheckLayer(hit.collider.gameObject.layer))
                                outPoints.Add(hit.point);
                        }
                    }

                    infos.Add(info, outPoints);
                }

                for (int i = 0; i < infos.Count; i++)
                {
                    progress.Value = i / infos.Count;
                    var pInfo = infos.Keys.ToArray()[i];
                    var targetPositions = infos[pInfo];
                    Quaternion rot = Quaternion.identity;
                    if (pInfo.isRandomRotation)
                    {
                        Vector3 euler = new Vector3(
                            UnityEngine.Random.Range(0f, randomRotationRange.x),
                            UnityEngine.Random.Range(0f, randomRotationRange.y),
                            UnityEngine.Random.Range(0f, randomRotationRange.z)
                        );
                        rot = Quaternion.Euler(euler);
                    }

                    if (pInfo.storeToTerrain)
                    {
                        if (pInfo.typePrefab == PrefabInfo.TypePrefab.Tree)
                        {
                            List<TreeInstance> newInstances = new();
                            List<Vector3> treePos = new();
                            targetPositions.ForEach((target) =>
                            {
                                var pos = target + pInfo.offset;
                                targetTerrain.WorldPosToTerrainDetailPos(pos, out var terrPos);
                                pos = new Vector3(pos.x / targetTerrain.terrainData.size.x, 0,
                                    pos.z / targetTerrain.terrainData.size.z);
                                treePos.Add(pos);
                            });
                            await Task.Run(() =>
                            {
                                for (int j = 0; j < targetPositions.Count; j++)
                                {
                                    newInstances.Add(new TreeInstance()
                                    {
                                        position = treePos[j],
                                        prototypeIndex = indexes[pInfo],
                                        rotation = rot.y,
                                        color = Color.white,
                                        widthScale = 1f,
                                        heightScale = 1f,
                                        lightmapColor = Color.white
                                    });
                                }
                            }, token);
                            newInstances.ForEach((instance) => targetTerrain.AddTreeInstance(instance));
                        }
                        else
                        {
                            var detailTargetPoints = new List<Vector3>();
                            targetPositions.ForEach((detailPos) =>
                            {
                                if (targetTerrain.WorldPosToTerrainDetailPos(detailPos, out var a))
                                    detailTargetPoints.Add(a);
                            });
                            var map = targetTerrain.terrainData.GetDetailLayer(0,
                                0,
                                targetTerrain.terrainData.detailWidth,
                                targetTerrain.terrainData.detailHeight,
                                indexes[pInfo]
                            );
                            await Task.Run(() =>
                            {
                                foreach (var targetPoint in detailTargetPoints)
                                {
                                    map[(int)targetPoint.z, (int)targetPoint.x]++;
                                }
                            }, token);

                            targetTerrain.terrainData.SetDetailLayer(0, 0, indexes[pInfo], map);
                        }

                        continue;
                    }

                    var parent = GetParent(pInfo);
                    foreach (var pos in targetPositions)
                    {
                        var obj = Instantiate(
                            pInfo.prefab,
                            pos + pInfo.offset,
                            rot,
                            parent
                        );
                        if (pInfo.randomScale)
                        {
                            float scaleValue = UnityEngine.Random.Range(pInfo.scaleRange.x, pInfo.scaleRange.y);
                            obj.transform.localScale = Vector3.one * scaleValue;
                        }
                    }
                }
                targetTerrain.Flush();
                ActionAfterGeneration?.Invoke();
                EditorUtility.SetDirty(this);
                CancelGeneration();
            }
            catch (Exception e)
            {
                UnityEngine.Debug.LogError(e);
                CancelGeneration();
            }
        }

        private Transform GetParent(PrefabInfo info)
        {
            switch (info.typePrefab)
            {
                case PrefabInfo.TypePrefab.Grass:
                    return parentForGrass;
                case PrefabInfo.TypePrefab.Tree:
                    return parentForTree;
                case PrefabInfo.TypePrefab.Stumps:
                    return parentForStumps;
                case PrefabInfo.TypePrefab.Other:
                    return parentForOther;
                default: return transform;
            }
        }

        private Vector3[] GetAreaCorners()
        {
            Vector3 halfSize = new Vector3(size.x * 0.5f, 0f, size.y * 0.5f);
            Vector3[] corners = new Vector3[4];
            corners[0] = transform.TransformPoint(new Vector3(-halfSize.x, 0, -halfSize.z));
            corners[1] = transform.TransformPoint(new Vector3(-halfSize.x, 0, halfSize.z));
            corners[2] = transform.TransformPoint(new Vector3(halfSize.x, 0, halfSize.z));
            corners[3] = transform.TransformPoint(new Vector3(halfSize.x, 0, -halfSize.z));
            return corners;
        }

        private bool IsInsideGenerationArea(Vector3 worldPos)
        {
            Vector3 localPos = transform.InverseTransformPoint(worldPos);
            return Mathf.Abs(localPos.x) <= size.x * 0.5f &&
                   Mathf.Abs(localPos.z) <= size.y * 0.5f;
        }

        private float CalculateSurfaceArea()
        {
            switch (generationType)
            {
                case GenerationType.Quad:
                {
                    float w = size.x;
                    float h = size.y;
                    return w * h;
                }
                case GenerationType.Circle:
                {
                    float r = size.x;
                    return 4f * Mathf.PI * r * r;
                }
                case GenerationType.Mesh:
                {
                    if (sourceMesh == null) return 0f;
                    return CalculateMeshArea(sourceMesh);
                }
            }

            return 0f;
        }

        private float CalculateMeshArea(Mesh mesh)
        {
            float total = 0f;
            var vertices = mesh.vertices;
            var triangles = mesh.triangles;
            for (int i = 0; i < triangles.Length; i += 3)
            {
                Vector3 v0 = transform.TransformPoint(vertices[triangles[i]]);
                Vector3 v1 = transform.TransformPoint(vertices[triangles[i + 1]]);
                Vector3 v2 = transform.TransformPoint(vertices[triangles[i + 2]]);

                float area = Vector3.Cross(v1 - v0, v2 - v0).magnitude * 0.5f;
                total += area;
            }

            return total;
        }

        #region Clean_Terrain

        private void ClearTerrainQuad(Terrain terrain)
        {
            Vector3[] corners = GetAreaCorners();

            Vector3 aabbMin = corners[0];
            Vector3 aabbMax = corners[0];
            foreach (var corner in corners)
            {
                aabbMin = Vector3.Min(aabbMin, corner);
                aabbMax = Vector3.Max(aabbMax, corner);
            }

            List<TreeInstance> treeList = new List<TreeInstance>(terrain.terrainData.treeInstances);
            treeList.RemoveAll(tree =>
            {
                Vector3 treeWorldPos =
                    terrain.transform.position + Vector3.Scale(tree.position, terrain.terrainData.size);
                return IsInsideGenerationArea(treeWorldPos);
            });
            terrain.terrainData.treeInstances = treeList.ToArray();

            int detailWidth = terrain.terrainData.detailWidth;
            int detailHeight = terrain.terrainData.detailHeight;
            Vector3 terrainPos = terrain.transform.position;
            Vector3 terrainSize = terrain.terrainData.size;

            Vector2 normAABBMin = new Vector2((aabbMin.x - terrainPos.x) / terrainSize.x,
                (aabbMin.z - terrainPos.z) / terrainSize.z);
            Vector2 normAABBMax = new Vector2((aabbMax.x - terrainPos.x) / terrainSize.x,
                (aabbMax.z - terrainPos.z) / terrainSize.z);

            int detailMinX = Mathf.Clamp(Mathf.FloorToInt(normAABBMin.x * detailWidth), 0, detailWidth - 1);
            int detailMinY = Mathf.Clamp(Mathf.FloorToInt(normAABBMin.y * detailHeight), 0, detailHeight - 1);
            int detailMaxX = Mathf.Clamp(Mathf.CeilToInt(normAABBMax.x * detailWidth), 0, detailWidth - 1);
            int detailMaxY = Mathf.Clamp(Mathf.CeilToInt(normAABBMax.y * detailHeight), 0, detailHeight - 1);


            for (int index = 0; index < terrain.terrainData.detailPrototypes.Length; index++)
            {
                int[,] detailMap = terrain.terrainData.GetDetailLayer(0, 0, detailWidth, detailHeight, index);

                for (int y = detailMinY; y <= detailMaxY; y++)
                {
                    for (int x = detailMinX; x <= detailMaxX; x++)
                    {
                        float normX = (x + 0.5f) / detailWidth;
                        float normY = (y + 0.5f) / detailHeight;
                        float worldX = terrainPos.x + normX * terrainSize.x;
                        float worldZ = terrainPos.z + normY * terrainSize.z;
                        Vector3 cellWorldPos = new Vector3(worldX, 0, worldZ);

                        if (IsInsideGenerationArea(cellWorldPos))
                        {
                            detailMap[y, x] = 0;
                        }
                    }
                }

                terrain.terrainData.SetDetailLayer(0, 0, index, detailMap);
            }

            terrain.Flush();
            EditorUtility.SetDirty(terrain);
        }

        private void ClearTerrainCircle(Terrain terrain)
        {
            float radius = size.x;
            Vector3 center = transform.position;

            List<TreeInstance> treeList = new List<TreeInstance>(terrain.terrainData.treeInstances);
            treeList.RemoveAll(tree =>
            {
                Vector3 treeWorldPos =
                    terrain.transform.position + Vector3.Scale(tree.position, terrain.terrainData.size);
                return Vector3.Distance(treeWorldPos, center) <= radius;
            });
            terrain.terrainData.treeInstances = treeList.ToArray();


            int detailWidth = terrain.terrainData.detailWidth;
            int detailHeight = terrain.terrainData.detailHeight;
            Vector3 terrainPos = terrain.transform.position;
            Vector3 terrainSize = terrain.terrainData.size;

            for (int index = 0; index < terrain.terrainData.detailPrototypes.Length; index++)
            {
                int[,] detailMap = terrain.terrainData.GetDetailLayer(0, 0, detailWidth, detailHeight, index);
                for (int y = 0; y < detailHeight; y++)
                {
                    for (int x = 0; x < detailWidth; x++)
                    {
                        float normX = (x + 0.5f) / detailWidth;
                        float normY = (y + 0.5f) / detailHeight;
                        float worldX = terrainPos.x + normX * terrainSize.x;
                        float worldZ = terrainPos.z + normY * terrainSize.z;
                        Vector3 cellWorldPos = new Vector3(worldX, 0, worldZ);

                        if (Vector3.Distance(cellWorldPos, center) <= radius)
                        {
                            detailMap[y, x] = 0;
                        }
                    }
                }

                terrain.terrainData.SetDetailLayer(0, 0, index, detailMap);
            }

            terrain.Flush();
            EditorUtility.SetDirty(terrain);
        }

        private async void ClearTerrainMesh(Terrain terrain)
        {
            if (sourceMesh == null)
            {
                Debug.LogWarning("Source mesh is not assigned.");
                return;
            }

            Vector3[] meshVertices = sourceMesh.vertices;
            List<Vector2> polygon = new List<Vector2>();
            for (int i = 0; i < meshVertices.Length; i++)
            {
                Vector3 worldPoint = transform.TransformPoint(meshVertices[i]);
                polygon.Add(new Vector2(worldPoint.x, worldPoint.z));
            }

            List<TreeInstance> treeList = new List<TreeInstance>(terrain.terrainData.treeInstances);
            treeList.RemoveAll(tree =>
            {
                Vector3 treeWorldPos =
                    terrain.transform.position + Vector3.Scale(tree.position, terrain.terrainData.size);
                Vector2 treePos2D = new Vector2(treeWorldPos.x, treeWorldPos.z);
                return IsPointInPolygon(treePos2D, polygon.ToArray());
            });
            terrain.terrainData.treeInstances = treeList.ToArray();

            int detailWidth = terrain.terrainData.detailWidth;
            int detailHeight = terrain.terrainData.detailHeight;
            Vector3 terrainPos = terrain.transform.position;
            Vector3 terrainSize = terrain.terrainData.size;

            for (int index = 0; index < terrain.terrainData.detailPrototypes.Length; index++)
            {
                int[,] detailMap = terrain.terrainData.GetDetailLayer(0, 0, detailWidth, detailHeight, index);
                await Task.Run(() =>
                {
                    for (int y = 0; y < detailHeight; y++)
                    {
                        for (int x = 0; x < detailWidth; x++)
                        {
                            float normX = (x + 0.5f) / detailWidth;
                            float normY = (y + 0.5f) / detailHeight;
                            float worldX = terrainPos.x + normX * terrainSize.x;
                            float worldZ = terrainPos.z + normY * terrainSize.z;
                            Vector2 cellPos2D = new Vector2(worldX, worldZ);

                            if (IsPointInPolygon(cellPos2D, polygon.ToArray()))
                            {
                                detailMap[y, x] = 0;
                            }
                        }
                    }
                });
                terrain.terrainData.SetDetailLayer(0, 0, index, detailMap);
            }

            terrain.Flush();
            EditorUtility.SetDirty(terrain);
        }

        private bool IsPointInPolygon(Vector2 point, Vector2[] polygon)
        {
            bool inside = false;
            int j = polygon.Length - 1;
            for (int i = 0; i < polygon.Length; i++)
            {
                if ((polygon[i].y > point.y) != (polygon[j].y > point.y) &&
                    (point.x <
                     (polygon[j].x - polygon[i].x) * (point.y - polygon[i].y) / (polygon[j].y - polygon[i].y) +
                     polygon[i].x))
                {
                    inside = !inside;
                }

                j = i;
            }

            return inside;
        }

        #endregion

        #region GenerationPoints

        private List<Vector3> GeneratePoints(int totalCount)
        {
            switch (generationType)
            {
                case GenerationType.Quad:
                    return GeneratePointsForQuad(totalCount);
                case GenerationType.Circle:
                    return GeneratePointsForSphere(totalCount);
                case GenerationType.Mesh:
                    return GeneratePointsForMesh(totalCount);
            }

            return new List<Vector3>();
        }
        
        private List<Vector3> GeneratePointsForQuad(int count)
        {
            switch (distributionType)
            {
                case DistributionType.Random:
                    return GenerateRandomPointsInQuad(count);
                case DistributionType.Grid:
                    return GenerateGridPointsInQuad();
            }

            return new List<Vector3>();
        }
        private List<Vector3> GeneratePointsForSphere(int count)
        {
            var result = new List<Vector3>();
            switch (distributionType)
            {
                case DistributionType.Random:
                    for (int i = 0; i < count; i++)
                    {
                        Vector3 randomDir = UnityEngine.Random.insideUnitSphere * size.x;
                        result.Add(transform.position + randomDir);
                    }

                    break;
                case DistributionType.Grid:
                    float step = cellSize;
                    for (float x = -size.x; x < size.x; x += step)
                    {
                        for (float y = -size.x; y < size.x; y += step)
                        {
                            for (float z = -size.x; z < size.x; z += step)
                            {
                                Vector3 point = new Vector3(x, y, z);
                                if (point.magnitude <= size.x) // внутри сферы
                                {
                                    result.Add(transform.position + point);
                                }
                            }
                        }
                    }

                    break;
            }
            
            if (result.Count > count)
            {
                result = result.Take(count).ToList();
            }

            return result;
        }
        
        private List<Vector3> GeneratePointsForMesh(int count)
        {
            var result = new List<Vector3>();
            if (sourceMesh == null)
            {
                Debug.LogWarning("Source mesh is not assigned.");
                return result;
            }
            
            Vector3[] vertices = sourceMesh.vertices;
            int[] triangles = sourceMesh.triangles;
            if (triangles.Length < 3) return result;
            
            float[] areas = new float[triangles.Length / 3];
            float totalArea = 0f;

            for (int i = 0; i < triangles.Length; i += 3)
            {
                Vector3 v0 = vertices[triangles[i]];
                Vector3 v1 = vertices[triangles[i + 1]];
                Vector3 v2 = vertices[triangles[i + 2]];
                
                v0 = transform.TransformPoint(v0);
                v1 = transform.TransformPoint(v1);
                v2 = transform.TransformPoint(v2);
                
                float area = Vector3.Cross((v1 - v0), (v2 - v0)).magnitude * 0.5f;
                areas[i / 3] = area;
                totalArea += area;
            }
            System.Random rnd = new System.Random();
            for (int i = 0; i < count; i++)
            {
                float r = (float)(rnd.NextDouble() * totalArea);
                int triIndex = 0;
                float accumArea = 0f;
                for (int j = 0; j < areas.Length; j++)
                {
                    accumArea += areas[j];
                    if (r <= accumArea)
                    {
                        triIndex = j;
                        break;
                    }
                }
                int iTri = triIndex * 3;
                Vector3 a = transform.TransformPoint(vertices[triangles[iTri]]);
                Vector3 b = transform.TransformPoint(vertices[triangles[iTri + 1]]);
                Vector3 c = transform.TransformPoint(vertices[triangles[iTri + 2]]);
                
                Vector3 randomPoint = GetRandomPointInTriangle(a, b, c);
                result.Add(randomPoint);
            }

            return result;
        }
        private Vector3 GetRandomPointInTriangle(Vector3 a, Vector3 b, Vector3 c)
        {
            float u = UnityEngine.Random.value;
            float v = UnityEngine.Random.value;

            if (u + v > 1f)
            {
                u = 1f - u;
                v = 1f - v;
            }
            return a + u * (b - a) + v * (c - a);
        }

        #endregion

        #region Terrain_Prototypes

        private Dictionary<PrefabInfo, int> GetPrototypeIndexes(Terrain terrain)
        {
            Dictionary<PrefabInfo, int> prototypeIndexes = new();
            foreach (var prefabInfo in dataCollection.Prefabs)
            {
                if(prefabInfo.storeToTerrain == false)
                    continue;
                
                var prefab = prefabInfo.prefab;
                if (prefabInfo.typePrefab == PrefabInfo.TypePrefab.Tree)
                {
                    var prototypes = terrain.terrainData.treePrototypes;
                    for (var index = 0; index < prototypes.Length; index++)
                    {
                        var prototype = prototypes[index];
                        if (prototype.prefab == prefab)
                        {
                            prototypeIndexes.Add(prefabInfo, index);
                        }
                    }
                }
                else
                {
                    var details = terrain.terrainData.detailPrototypes;
                    for (var index = 0; index < details.Length; index++)
                    {
                        var prototype = details[index];
                        if (prototype.prototype == prefab)
                        {
                            prototypeIndexes.Add(prefabInfo, index);
                        }
                    }
                }

                if (prototypeIndexes.ContainsKey(prefabInfo) == false)
                {
                    int outIndex;
                    if (prefabInfo.typePrefab == PrefabInfo.TypePrefab.Tree)
                    {
                        var newPrototypes = terrain.terrainData.treePrototypes.ToList();
                        newPrototypes.Add(new TreePrototype()
                        {
                            prefab = prefabInfo.prefab,
                        });
                        terrain.terrainData.treePrototypes = newPrototypes.ToArray();
                        outIndex = newPrototypes.Count - 1;
                    }
                    else
                    {
                        var newPrototypes = terrain.terrainData.detailPrototypes.ToList();

                        newPrototypes.Add(new DetailPrototype()
                        {
                            prototype = prefabInfo.prefab,
                            density = 100,
                            prototypeTexture = GetPreviewTexture(prefabInfo.prefab)
                        });
                        terrain.terrainData.detailPrototypes = newPrototypes.ToArray();
                        outIndex = newPrototypes.Count - 1;
                    }

                    prototypeIndexes.Add(prefabInfo, outIndex);
                }
            }

            EditorUtility.SetDirty(terrain);

            return prototypeIndexes;
        }

        private Texture2D GetPreviewTexture(GameObject prefab)
        {
            Texture2D texture = null;
            while (texture == null)
            {
                texture = AssetPreview.GetAssetPreview(prefab);
            }

            return texture;
        }

        #endregion

        #region Distribution Helpers (Quad)

        private List<Vector3> GenerateRandomPointsInQuad(int count)
        {
            List<Vector3> result = new List<Vector3>();
            for (int i = 0; i < count; i++)
            {
                float x = UnityEngine.Random.Range(-size.x * 0.5f, size.x * 0.5f);
                float z = UnityEngine.Random.Range(-size.y * 0.5f, size.y * 0.5f);
                
                Vector3 localPos = new Vector3(x, 0f, z);
                Vector3 worldPos = transform.TransformPoint(localPos);
                result.Add(worldPos);
            }

            return result;
        }

        private List<Vector3> GenerateGridPointsInQuad()
        {
            List<Vector3> result = new List<Vector3>();
            float halfX = size.x * 0.5f;
            float halfZ = size.y * 0.5f;

            for (float x = -halfX; x < halfX; x += cellSize)
            {
                for (float z = -halfZ; z < halfZ; z += cellSize)
                {
                    Vector3 localPos = new Vector3(x, 0f, z);
                    Vector3 worldPos = transform.TransformPoint(localPos);
                    result.Add(worldPos);
                }
            }

            return result;
        }
        #endregion

        #region Gizmos
        private void OnDrawGizmos()
        {
            Matrix4x4 oldMatrix = Gizmos.matrix;
            Gizmos.matrix = transform.localToWorldMatrix;

            Gizmos.color = Color.yellow;

            switch (generationType)
            {
                case GenerationType.Quad:
                    Gizmos.DrawWireCube(
                        Vector3.zero,
                        new Vector3(size.x, 0f, size.y)
                    );
                    break;

                case GenerationType.Circle:
                    DrawWireCylinder(Vector3.zero, size.x, raycastHeight, 40);
                    break;

                case GenerationType.Mesh:
                    if (sourceMesh != null)
                    {
                        Gizmos.DrawWireMesh(sourceMesh);
                    }

                    break;
            }
            Gizmos.matrix = oldMatrix;
        }
        private void DrawWireCircle(Vector3 center, float radius, int segments)
        {
            float angleStep = 2f * Mathf.PI / segments;
            Vector3 prevPoint = center + new Vector3(Mathf.Cos(0), 0f, Mathf.Sin(0)) * radius;
            for (int i = 1; i <= segments; i++)
            {
                float angle = i * angleStep;
                Vector3 newPoint = center + new Vector3(Mathf.Cos(angle), 0f, Mathf.Sin(angle)) * radius;
                Gizmos.DrawLine(prevPoint, newPoint);
                prevPoint = newPoint;
            }
        }
        private void DrawWireCylinder(Vector3 center, float radius, float height, int segments)
        {
            Vector3 topCenter = center + Vector3.up * (height / 2f);
            Vector3 bottomCenter = center - Vector3.up * (height / 2f);
            
            DrawWireCircle(topCenter, radius, segments);
            DrawWireCircle(bottomCenter, radius, segments);
            
            float angleStep = 2f * Mathf.PI / segments;
            for (int i = 0; i < segments; i++)
            {
                float angle = i * angleStep;
                Vector3 offset = new Vector3(Mathf.Cos(angle), 0f, Mathf.Sin(angle)) * radius;
                Gizmos.DrawLine(topCenter + offset, bottomCenter + offset);
            }
        }

        #endregion

#endif
    }
}