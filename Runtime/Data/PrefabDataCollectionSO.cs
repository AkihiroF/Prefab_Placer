using System.Collections.Generic;
using UnityEngine;

namespace Map.Data
{
    [CreateAssetMenu(fileName = "PrefabDataCollection", menuName = "Map/Prefab Data Collection")]
    public class PrefabDataCollectionSO : ScriptableObject
    {
        [SerializeField]
        private List<PrefabInfo> prefabs = new List<PrefabInfo>();

        public List<PrefabInfo> Prefabs => prefabs;
    }
}