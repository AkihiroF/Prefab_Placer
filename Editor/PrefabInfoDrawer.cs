using Map.Data;
using UnityEditor;
using UnityEngine;

namespace _Source.Editor
{
    [CustomPropertyDrawer(typeof(PrefabInfo))]
    public class PrefabInfoDrawer : PropertyDrawer
    {
        // Задаём отступ между строками
        private const float Padding = 2f;

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            // Начинаем рисование свойства
            EditorGUI.BeginProperty(position, label, property);

            // Подготовим базовую высоту и отступы
            float lineHeight = EditorGUIUtility.singleLineHeight;
            float yOffset = position.y;

            // Если хотите, можно нарисовать foldout для скрытия/раскрытия всех полей
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

                // Получаем ссылки на вложенные свойства через FindPropertyRelative
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

                // Рисуем базовые поля
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

                // Рисуем выбор режима измерения
                EditorGUI.PropertyField(
                    new Rect(position.x, yOffset, position.width, lineHeight),
                    measureModeProp
                );
                yOffset += lineHeight + Padding;

                // В зависимости от выбранного режима отрисовываем нужное поле:
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

        // Расчёт высоты, зависящий от того, раскрыто свойство или нет, и от режима measureMode
        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            float lineHeight = EditorGUIUtility.singleLineHeight;
            float height = lineHeight + Padding; // для foldout

            if (property.isExpanded)
            {
                // Количество базовых полей, которые всегда отображаются
                int lines = 7; // prefab, typePrefab, offset, isRandomRotation, randomScale, storeToTerrain, scaleRange
                lines += 1; // measureMode

                SerializedProperty measureModeProp = property.FindPropertyRelative("measureMode");
                // В зависимости от режима добавляем ещё одну строку
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