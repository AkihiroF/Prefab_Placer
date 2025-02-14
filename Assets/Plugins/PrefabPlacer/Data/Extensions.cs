using UnityEngine;

namespace PlacerData
{
    public static class Extensions
    {
        private static System.Random random = new ();
        public static bool CheckLayer(this LayerMask original, int toCheck)
        {
            return (original.value & (1 << toCheck)) > 0;
        }
        public static bool WorldPosToTerrainDetailPos(this Terrain terrain, Vector3 worldPos, out Vector3 terrainDetailPos)
        {
            terrainDetailPos = Vector3.zero;
            Vector3 terrainLocation = terrain.GetPosition();
            Vector3 terrainPos = Vector3.zero;
            terrainPos.x = (worldPos.x - terrainLocation.x) / terrain.terrainData.size.x;
            terrainPos.y = 0;
            terrainPos.z = (worldPos.z - terrainLocation.z) / terrain.terrainData.size.z;

            terrainDetailPos.x = Mathf.FloorToInt(terrainPos.x * terrain.terrainData.detailResolution);
            terrainDetailPos.z = Mathf.FloorToInt(terrainPos.z * terrain.terrainData.detailResolution);

            return true;
        }
        public static bool TerrainDetailPosToWorldPos(this Terrain terrain, Vector3 terrainDetailPos, out Vector3 worldPos)
        {
            worldPos = Vector3.zero;
            Vector3 terrainLocation = terrain.GetPosition();
            Vector3 terrainPos = Vector3.zero;
            terrainPos.x = (terrainDetailPos.x / terrain.terrainData.detailResolution) * terrain.terrainData.size.x;
            terrainPos.y = 0;
            terrainPos.z = (terrainDetailPos.z / terrain.terrainData.detailResolution) * terrain.terrainData.size.z;

            worldPos.x = terrainLocation.x + terrainPos.x;
            worldPos.y = 0;
            worldPos.z = terrainLocation.z + terrainPos.z;

            return true;
        }
        
        public static float GetRandomFloat(this float min, float max)
        {
            double range = max - min;
            double sample = random.NextDouble();
            double scaled = (sample * range) + min;
            return (float)scaled;
        }
    }
}