using UnityEditor;
using UnityEngine;

namespace Code.Editor
{
    [CustomEditor(typeof(PrefabPlacer))]
    public class PrefabPlacerEditor : UnityEditor.Editor
    {
        private PrefabPlacer _placer;
        private float _progress;

        private void OnEnable()
        {
            _placer = serializedObject.targetObject as PrefabPlacer;
            _placer.progress.ObserveEveryValueChanged(x => x.Value).Do((value) => _progress = value);
        }

        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();
            
            GUILayout.Space(30);

            if(_placer.taskRunning)
            {
                if (GUILayout.Button("Cancel generating"))
                {
                    _placer.CancelGeneration();
                }
                ProgressBar();
            }
            else
            {
                if (GUILayout.Button("Generate"))
                {
                    _placer.Generate();
                }
                GUILayout.Space(10);

                if (GUILayout.Button("Clear only terrain") && _placer.targetTerrain != null)
                {
                    _placer.ClearTerrain();
                }
                GUILayout.Space(15);

                if (GUILayout.Button("Clear all"))
                {
                    _placer.Clear();
                }
            }
        }
        private void ProgressBar() {
            var fullRect = GUILayoutUtility.GetRect(100, 30);
            var completedRect = new Rect(fullRect.x, fullRect.y, fullRect.width * _progress, fullRect.height);
        
            EditorGUI.DrawRect(fullRect, Color.black);
            EditorGUI.DrawRect(completedRect, Color.Lerp(Color.red, Color.green, _progress));
        
            EditorGUI.LabelField(fullRect, $"{_progress * 100}%", EditorStyles.centeredGreyMiniLabel);
        }
    }
}