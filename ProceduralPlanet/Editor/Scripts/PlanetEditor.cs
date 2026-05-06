using UnityEditor;
using UnityEngine;
using ProceduralPlanet;

namespace ProceduralPlanet.Editor
{
    [CustomEditor(typeof(Planet))]
    public class PlanetEditor : UnityEditor.Editor
    {
        Planet planet;
        UnityEditor.Editor shapeEditor;
        UnityEditor.Editor colorEditor;

        public override void OnInspectorGUI()
        {
            using (var check = new EditorGUI.ChangeCheckScope())
            {
                base.OnInspectorGUI();
                if (check.changed)
                {
                    planet.GeneratePlanet();
                }
            }

            DrawPresetControls();
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Apply changes to planet", EditorStyles.boldLabel);
            if (GUILayout.Button("Generate Planet"))
            {
                planet.GeneratePlanet();
            }

            DrawSettingsEditor(planet.shapeSettings, planet.OnPlanetSettingsUpdated, ref planet.shapeSettingsFoldout, ref shapeEditor);
            DrawSettingsEditor(planet.colorSettings, planet.OnPlanetSettingsUpdated, ref planet.colorSettingsFoldout, ref colorEditor);



        }

        private void DrawPresetControls()
        {
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Planet Presets", EditorStyles.boldLabel);

            if (planet.presetDatabase == null)
            {
                EditorGUILayout.HelpBox("Assign a PlanetPresetDatabase asset to use preset slots.", MessageType.Info);
                return;
            }

            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("Save Current Slot"))
                {
                    if (planet.SavePresetToCurrentSlot())
                    {
                        EditorUtility.SetDirty(planet.presetDatabase);
                        EditorUtility.SetDirty(planet.shapeSettings);
                        EditorUtility.SetDirty(planet.colorSettings);
                        EditorUtility.SetDirty(planet);
                        AssetDatabase.SaveAssets();
                    }
                }

                if (GUILayout.Button("Load Current Slot"))
                {
                    if (planet.LoadPresetFromCurrentSlot())
                    {
                        EditorUtility.SetDirty(planet.shapeSettings);
                        EditorUtility.SetDirty(planet.colorSettings);
                        EditorUtility.SetDirty(planet);
                    }
                }
            }
        }

        private void DrawSettingsEditor(Object settings, System.Action onSettingsUpdated, ref bool foldout, ref UnityEditor.Editor editor)
        {
            if (settings == null)
            {
                EditorGUILayout.HelpBox("Settings object is null.", MessageType.Warning);
                return;
            }
            foldout = EditorGUILayout.InspectorTitlebar(foldout, settings);
            using (var check = new EditorGUI.ChangeCheckScope())
            {
                if (foldout)
                {
                    CreateCachedEditor(settings, null, ref editor);
                    editor.OnInspectorGUI();

                    if (check.changed)
                    {
                        if (onSettingsUpdated != null)
                        {
                            onSettingsUpdated();
                        }
                    }
                }
            }

        }

        private void OnEnable()
        {
            planet = (Planet)target;
        }
    }
}

