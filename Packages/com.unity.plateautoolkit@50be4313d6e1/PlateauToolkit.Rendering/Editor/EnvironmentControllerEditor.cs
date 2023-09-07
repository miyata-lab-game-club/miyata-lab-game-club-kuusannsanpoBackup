using UnityEditor;
using UnityEngine;

namespace PlateauToolkit.Rendering.Editor
{
    [CustomEditor(typeof(EnvironmentController))]
    public class EnvironmentControllerEditor : UnityEditor.Editor
    {
        EnvironmentController m_Env;

        void OnEnable()
        {
            if (target is EnvironmentController)
            {
                m_Env = (EnvironmentController)target;
            }
            else
            {
                m_Env = null;
            }
        }


        public override void OnInspectorGUI()
        {
            if (m_Env == null)
            {
                return;
            }
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Location");
            CheckUndo<float>(m_Env, ref m_Env.Location.x, EditorGUILayout.FloatField(m_Env.Location.x, GUILayout.Width(100)), "");
            CheckUndo<float>(m_Env, ref m_Env.Location.y, EditorGUILayout.FloatField(m_Env.Location.y, GUILayout.Width(100)), "");
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Time of Day (" + m_Env.GetTimeString() + ")");
            CheckUndo<float>(m_Env, ref m_Env.TimeOfDay, EditorGUILayout.Slider(m_Env.TimeOfDay, 0f, 1f), "Time of Day");
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Rain");
            CheckUndo<float>(m_Env, ref m_Env.Rain, EditorGUILayout.Slider(m_Env.Rain, 0f, 1f), "Rain");
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Snow");
            CheckUndo<float>(m_Env, ref m_Env.Snow, EditorGUILayout.Slider(m_Env.Snow, 0f, 1f), "Snow");
            EditorGUILayout.EndHorizontal();

#if UNITY_URP
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Cloud");
            CheckUndo<float>(m_Env, ref m_Env.Cloud, EditorGUILayout.Slider(m_Env.Cloud, 0f, 1f), "Cloud");
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Fog Distance");
            CheckUndo<float>(m_Env, ref m_Env.FogDistance, EditorGUILayout.Slider(m_Env.FogDistance, 0f, 2000f), "Fog Distance");
            EditorGUILayout.EndHorizontal();
#endif

            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Sun Color");
            CheckUndo<Color>(m_Env, ref m_Env.SunColor, EditorGUILayout.ColorField(m_Env.SunColor), "Sun Color");
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Moon Color");
            CheckUndo<Color>(m_Env, ref m_Env.MoonColor, EditorGUILayout.ColorField(m_Env.MoonColor), "Moon Color");
            EditorGUILayout.EndHorizontal();

#if UNITY_HDRP
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Sun Intensity");
            CheckUndo<float>(m_Env, ref m_Env.SunIntensity, EditorGUILayout.Slider(m_Env.SunIntensity, 0f, 130000f), "Sun Intensity");
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Moon Intensity");
            CheckUndo<float>(m_Env, ref m_Env.MoonIntensity, EditorGUILayout.Slider(m_Env.MoonIntensity, 0f, 100f), "Moon Intensity");
            EditorGUILayout.EndHorizontal();
#elif UNITY_URP
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Sun Intensity");
            CheckUndo<float>(m_Env, ref m_Env.SunIntensity, EditorGUILayout.Slider(m_Env.SunIntensity, 0f, 10f), "Sun Intensity");
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Moon Intensity");
            CheckUndo<float>(m_Env, ref m_Env.MoonIntensity, EditorGUILayout.Slider(m_Env.MoonIntensity, 0f, 10f), "Moon Intensity");
            EditorGUILayout.EndHorizontal();
#endif

            // Hide material slider
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Weather Material Fade");
            CheckUndo<float>(m_Env, ref m_Env.MaterialFade, EditorGUILayout.Slider(m_Env.MaterialFade, 0f, 1f), "Hide Material");
            EditorGUILayout.EndHorizontal();

            // Material quality setting for urp
#if UNITY_URP
            string[] materialQualityNames = System.Enum.GetNames(typeof(EnvironmentController.MaterialQuality));
            GUIContent[] qualityOptions = new GUIContent[materialQualityNames.Length];
            for (int i = 0; i < materialQualityNames.Length; i++)
            {
                qualityOptions[i] = new GUIContent(materialQualityNames[i]);
            }

            int selectedIndex = (int)m_Env.Quality;
            selectedIndex = EditorGUILayout.Popup(new GUIContent("Material Quality"), selectedIndex, qualityOptions);

            if (selectedIndex != (int)m_Env.Quality)
            {
                Undo.RecordObject(m_Env, "Material Quality");
                m_Env.SetMaterialQuality((EnvironmentController.MaterialQuality)selectedIndex);
                EditorUtility.SetDirty(m_Env);
            }
#endif
        }

        public bool CheckUndo<T>(UnityEngine.Object recordTarget, ref T origin, T value, string log) where T : System.IEquatable<T>
        {
            if (!origin.Equals(value))
            {
                Undo.RecordObject(recordTarget, log);
                origin = value;
                PrefabUtility.RecordPrefabInstancePropertyModifications(recordTarget);
                return true;
            }

            return false;
        }
    }

}