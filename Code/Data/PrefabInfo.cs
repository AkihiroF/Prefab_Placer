using System;
using UnityEngine;

namespace Code.Data
{
    [Serializable]
    public class PrefabInfo
    {
        #region Internal

        [Serializable]
        public enum CountMeasureMode
        {
            ByAbsoluteCount = 0,
            ByDensity = 1
        }
        
        [Serializable]
        public enum TypePrefab
        {
            Grass = 0,
            Tree = 1,
            Stumps = 2,
            Other = 3
        }

        #endregion
        
        public GameObject prefab;
        public TypePrefab typePrefab;
        public Vector3 offset;
    
        public bool isRandomRotation;
        public bool randomScale;

        public bool storeToTerrain;
        public Vector2 scaleRange;
        
        public CountMeasureMode measureMode;
        
        public int absoluteCount;
        
        public float density;
    }
}