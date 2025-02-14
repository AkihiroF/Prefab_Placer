using System.Collections.Generic;
using PlacerData;
using UnityEditor;
using UnityEngine;

namespace PlacerEditor
{
    [CustomEditor(typeof(PrefabDataCollectionSO))]
    public class PrefabDataCollectionSOEditor : UnityEditor.Editor
    {
        private SerializedProperty _prefabsProp;
        
        private int _selectedIndex = -1;
        
        private List<GUIContent> _gridContents;

        // Настройки сетки
        private const int GridColumns = 3; 
        private const float GridButtonSize = 64f;

        private void OnEnable()
        {
            // Получаем сериализованное свойство (список PrefabInfo)
            _prefabsProp = serializedObject.FindProperty("prefabs");
            RebuildGridContents();
        }

        public override void OnInspectorGUI()
        {
            // Обновляем сериализованные данные
            serializedObject.Update();

            // Если размер списка изменился (добавили/удалили элементы извне), пересоберём контент
            if (_gridContents == null || _gridContents.Count != _prefabsProp.arraySize)
            {
                RebuildGridContents();
            }
            
            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Add Prefab", GUILayout.Width(100)))
            {
                int newIndex = _prefabsProp.arraySize;
                _prefabsProp.arraySize++;
                _prefabsProp.GetArrayElementAtIndex(newIndex);
                RebuildGridContents(); 
            }
            
            bool canRemove = (_selectedIndex >= 0 && _selectedIndex < _prefabsProp.arraySize);
            GUI.enabled = canRemove;
            if (GUILayout.Button("Remove Selected", GUILayout.Width(120)))
            {
                _prefabsProp.DeleteArrayElementAtIndex(_selectedIndex);
                _selectedIndex = -1;
                RebuildGridContents();
            }

            GUI.enabled = true;
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.Space(5);
            
            DrawSelectionGrid();

            EditorGUILayout.Space(10);
            
            if (_selectedIndex >= 0 && _selectedIndex < _prefabsProp.arraySize)
            {
                SerializedProperty selectedElement = _prefabsProp.GetArrayElementAtIndex(_selectedIndex);

                EditorGUILayout.LabelField("Selected Prefab Info", EditorStyles.boldLabel);
                EditorGUI.indentLevel++;
                EditorGUILayout.PropertyField(selectedElement, includeChildren: true);
                EditorGUI.indentLevel--;
            }
            
            serializedObject.ApplyModifiedProperties();
        }
        
        private void DrawSelectionGrid()
        {
            if (_gridContents == null || _gridContents.Count == 0)
            {
                EditorGUILayout.HelpBox("No Prefabs in the list.", MessageType.Info);
                return;
            }
            
            var gridStyle = new GUIStyle(GUI.skin.button)
            {
                alignment = TextAnchor.MiddleCenter,
                fixedWidth = GridButtonSize,
                fixedHeight = GridButtonSize,
                richText = true,
                contentOffset = Vector2.one
            };
            
            int newSelected = GUILayout.SelectionGrid(
                _selectedIndex,
                _gridContents.ToArray(),
                GridColumns,
                gridStyle
            );

            if (newSelected != _selectedIndex)
            {
                _selectedIndex = newSelected;
            }
        }
        private void RebuildGridContents()
        {
            if (_prefabsProp == null) return;

            _gridContents = new List<GUIContent>(_prefabsProp.arraySize);
            for (int i = 0; i < _prefabsProp.arraySize; i++)
            {
                //SerializedProperty element = _prefabsProp.GetArrayElementAtIndex(i);
                GUIContent iconContent = new GUIContent($"{i + 1}");

                _gridContents.Add(iconContent);
            }
        }
    }
}