using UnityEngine;
using UnityEditor;
using ProceduralPlanet;

//Original version of the ConditionalHideAttribute created by Brecht Lecluyse (www.brechtos.com)
//Modified by: Sebastian Lague

namespace ProceduralPlanet.Editor
{
    [CustomPropertyDrawer(typeof(ConditionalHideAttribute))]
    public class ConditionalHidePropertyDrawer : PropertyDrawer
    {

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            ConditionalHideAttribute condHAtt = (ConditionalHideAttribute)attribute;
            bool enabled = GetConditionalHideAttributeResult(condHAtt, property);

            if (enabled)
            {
                EditorGUI.PropertyField(position, property, label, true);
            }
        }

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            ConditionalHideAttribute condHAtt = (ConditionalHideAttribute)attribute;
            bool enabled = GetConditionalHideAttributeResult(condHAtt, property);

            if (enabled)
            {
                return EditorGUI.GetPropertyHeight(property, label);
            }
            // Undo the spacing added before and after the property.
            return -EditorGUIUtility.standardVerticalSpacing;

        }

        bool GetConditionalHideAttributeResult(ConditionalHideAttribute condHAtt, SerializedProperty property)
        {
            SerializedProperty sourcePropertyValue = null;

            // Use the full relative path so nested serialized properties can drive visibility.
            if (!property.isArray)
            {
                string propertyPath = property.propertyPath;
                string conditionPath = propertyPath.Replace(property.name, condHAtt.conditionalSourceField);
                sourcePropertyValue = property.serializedObject.FindProperty(conditionPath);

                // Fall back when the nested lookup fails.
                if (sourcePropertyValue == null)
                {
                    sourcePropertyValue = property.serializedObject.FindProperty(condHAtt.conditionalSourceField);
                }
            }
            else
            {
                sourcePropertyValue = property.serializedObject.FindProperty(condHAtt.conditionalSourceField);
            }


            if (sourcePropertyValue != null)
            {
                return CheckPropertyType(condHAtt, sourcePropertyValue);
            }

            return true;
        }

        bool CheckPropertyType(ConditionalHideAttribute condHAtt, SerializedProperty sourcePropertyValue)
        {
            // Add more property types here if needed.
            switch (sourcePropertyValue.propertyType)
            {
                case SerializedPropertyType.Boolean:
                    return sourcePropertyValue.boolValue;
                case SerializedPropertyType.Enum:
                    return sourcePropertyValue.enumValueIndex == condHAtt.enumIndex;
                default:
                    Debug.LogError("Data type of the property used for conditional hiding [" + sourcePropertyValue.propertyType + "] is currently not supported");
                    return true;
            }
        }
    }
}
