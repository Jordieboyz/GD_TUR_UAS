using UnityEditor;
using UnityEngine;
using Utils;

#if (UNITY_EDITOR) 
[CustomPropertyDrawer(typeof(PairedGameObject))]
public class PairedGameObjectDrawer : PropertyDrawer
{
    public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
    {
        // Start property drawing
        EditorGUI.BeginProperty(position, label, property);

        // Check if we're in an array and draw an expandable header
        bool isExpanded = property.isExpanded;
        isExpanded = EditorGUI.Foldout(new Rect(position.x, position.y, position.width, EditorGUIUtility.singleLineHeight), isExpanded, label);
        property.isExpanded = isExpanded;
        
        if (isExpanded)
        {
            // Calculate positions for fields
            float lineHeight = EditorGUIUtility.singleLineHeight + 2;
            Rect objRect = new Rect(position.x, position.y + lineHeight, position.width, EditorGUIUtility.singleLineHeight);
            Rect ppSettingRect = new Rect(position.x, position.y + 2 * lineHeight, position.width, EditorGUIUtility.singleLineHeight);

            // Draw GameObject and PPSetting fields
            SerializedProperty objProperty = property.FindPropertyRelative("obj");
            SerializedProperty ppSettingProperty = property.FindPropertyRelative("PPSetting");
            EditorGUI.PropertyField(objRect, objProperty, new GUIContent("GameObject"));
            ppSettingProperty.boolValue = EditorGUI.Toggle(ppSettingRect, "PPSetting", ppSettingProperty.boolValue);

            // Conditionally draw the other settings
            if (ppSettingProperty.boolValue)
            {
                Rect gameSettingRect = new Rect(position.x + 20, position.y + 3 * lineHeight, position.width - 20, EditorGUIUtility.singleLineHeight);
                Rect playerSettingRect = new Rect(position.x + 20, position.y + 4 * lineHeight, position.width - 20, EditorGUIUtility.singleLineHeight);

                SerializedProperty gameSettingProperty = property.FindPropertyRelative("StaticGameSetting");
                SerializedProperty playerSettingProperty = property.FindPropertyRelative("StaticPlayerSetting");

                // GameSetting field with mutual exclusion
                bool newGameSetting = EditorGUI.Toggle(gameSettingRect, "Game Setting", gameSettingProperty.boolValue);
                if (newGameSetting && playerSettingProperty.boolValue)
                {
                    playerSettingProperty.boolValue = false;
                }
                gameSettingProperty.boolValue = newGameSetting;

                // PlayerSetting field with mutual exclusion
                bool newPlayerSetting = EditorGUI.Toggle(playerSettingRect, "Player Setting", playerSettingProperty.boolValue);
                if (newPlayerSetting && gameSettingProperty.boolValue)
                {
                    gameSettingProperty.boolValue = false;
                }
                playerSettingProperty.boolValue = newPlayerSetting;
            }
        }

        // End property drawing
        EditorGUI.EndProperty();
    }

    public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
    {
        // Calculate height dynamically based on expansion and settings
        bool isExpanded = property.isExpanded;
        SerializedProperty ppSettingProperty = property.FindPropertyRelative("PPSetting");
        float baseHeight = EditorGUIUtility.singleLineHeight + 4; // Header height
        if (!isExpanded) return baseHeight;

        baseHeight += 2 * (EditorGUIUtility.singleLineHeight + 2); // Always-visible fields
        if (ppSettingProperty.boolValue)
        {
            baseHeight += 2 * (EditorGUIUtility.singleLineHeight + 2); // Conditional fields
        }
        return baseHeight;
    }
}
#endif