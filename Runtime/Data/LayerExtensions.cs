using System.Collections.Generic;
using UnityEngine;

namespace Utils
{
    public static class LayerExtensions
    {
        public static bool CheckLayer(this LayerMask original, int toCheck)
        {
            return (original.value & (1 << toCheck)) > 0;
        }
        public static string[] LayerNames(this LayerMask layerMask) {
            var names = new List<string>();
  
            for (int i = 0; i < 32; ++i) {
                int shifted = 1 << i;
                if ((layerMask & shifted) == shifted) {
                    string layerName = LayerMask.LayerToName(i);
                    if (!string.IsNullOrEmpty(layerName)) {
                        names.Add(layerName);
                    }
                }
            }
            return names.ToArray();
        }
    }
}