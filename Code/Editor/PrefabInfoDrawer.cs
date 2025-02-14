using Code.Data;
using UnityEditor;
using UnityEngine;

namespace Code.Editor
{
    [CustomPropertyDrawer(typeof(PrefabInfo))]
    public class PrefabInfoDrawer : PropertyDrawer
    {
        private const float Padding = 2f;

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            EditorGUI.BeginProperty(position, label, property);
            
            float lineHeight = EditorGUIUtility.singleLineHeight;
            float yOffset = position.y;
            
            property.isExpanded = EditorGUI.Foldout(
                new Rect(position.x, yOffset, position.width, lineHeight),
                property.isExpanded,
                label,
                true
            );
            yOffset += lineHeight + Padding;

            if (property.isExpanded)
            {
                EditorGUI.indentLevel++;
                
                SerializedProperty prefabProp = property.FindPropertyRelative("prefab");
                SerializedProperty typePrefabProp = property.FindPropertyRelative("typePrefab");
                SerializedProperty offsetProp = property.FindPropertyRelative("offset");
                SerializedProperty isRandomRotationProp = property.FindPropertyRelative("isRandomRotation");
                SerializedProperty randomScaleProp = property.FindPropertyRelative("randomScale");
                SerializedProperty storeToTerrainProp = property.FindPropertyRelative("storeToTerrain");
                SerializedProperty scaleRangeProp = property.FindPropertyRelative("scaleRange");
                SerializedProperty measureModeProp = property.FindPropertyRelative("measureMode");
                SerializedProperty absoluteCountProp = property.FindPropertyRelative("absoluteCount");
                SerializedProperty densityProp = property.FindPropertyRelative("density");
                
                EditorGUI.PropertyField(
                    new Rect(position.x, yOffset, position.width, lineHeight),
                    prefabProp
                );
                yOffset += lineHeight + Padding;

                EditorGUI.PropertyField(
                    new Rect(position.x, yOffset, position.width, lineHeight),
                    typePrefabProp
                );
                yOffset += lineHeight + Padding;

                EditorGUI.PropertyField(
                    new Rect(position.x, yOffset, position.width, lineHeight),
                    offsetProp
                );
                yOffset += lineHeight + Padding;

                EditorGUI.PropertyField(
                    new Rect(position.x, yOffset, position.width, lineHeight),
                    isRandomRotationProp
                );
                yOffset += lineHeight + Padding;

                EditorGUI.PropertyField(
                    new Rect(position.x, yOffset, position.width, lineHeight),
                    randomScaleProp
                );
                yOffset += lineHeight + Padding;

                if (randomScaleProp.boolValue == true)
                {
                    EditorGUI.PropertyField(
                        new Rect(position.x, yOffset, position.width, lineHeight),
                        scaleRangeProp
                    );
                    yOffset += lineHeight + Padding;
                }

                EditorGUI.PropertyField(
                    new Rect(position.x, yOffset, position.width, lineHeight),
                    storeToTerrainProp
                );
                yOffset += lineHeight + Padding;
                
                EditorGUI.PropertyField(
                    new Rect(position.x, yOffset, position.width, lineHeight),
                    measureModeProp
                );
                yOffset += lineHeight + Padding;
                
                if (measureModeProp.enumValueIndex == (int)PrefabInfo.CountMeasureMode.ByAbsoluteCount)
                {
                    EditorGUI.PropertyField(
                        new Rect(position.x, yOffset, position.width, lineHeight),
                        absoluteCountProp,
                        new GUIContent("Absolute Count")
                    );
                    yOffset += lineHeight + Padding;
                }
                else if (measureModeProp.enumValueIndex == (int)PrefabInfo.CountMeasureMode.ByDensity)
                {
                    EditorGUI.PropertyField(
                        new Rect(position.x, yOffset, position.width, lineHeight),
                        densityProp,
                        new GUIContent("Density")
                    );
                    yOffset += lineHeight + Padding;
                }

                EditorGUI.indentLevel--;
            }

            EditorGUI.EndProperty();
        }
        
        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            float lineHeight = EditorGUIUtility.singleLineHeight;
            float height = lineHeight + Padding;

            if (property.isExpanded)
            {
                int lines = 7;
                lines += 1; 
                SerializedProperty measureModeProp = property.FindPropertyRelative("measureMode");
                
                if (measureModeProp.enumValueIndex == (int)PrefabInfo.CountMeasureMode.ByAbsoluteCount ||
                    measureModeProp.enumValueIndex == (int)PrefabInfo.CountMeasureMode.ByDensity)
                {
                    lines += 1;
                }

                height += lines * (lineHeight + Padding);
            }

            return height;
        }
    }
}