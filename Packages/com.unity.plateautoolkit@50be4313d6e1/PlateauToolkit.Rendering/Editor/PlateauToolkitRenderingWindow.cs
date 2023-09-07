using PlateauToolkit.Editor;
using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
#if UNITY_URP
using UnityEngine.Rendering.Universal;
#endif
#if UNITY_HDRP
using UnityEngine.Rendering.HighDefinition;
#endif

namespace PlateauToolkit.Rendering.Editor
{
    public class PlateauToolkitRenderingWindow : EditorWindow
    {
        public PlateauToolkitRenderingWindow m_Window;

        readonly List<GameObject> m_SelectedObjects = new List<GameObject>();

        // Auto texturing
        AutoTexturing m_AutoTextureProcessor;

        // LOD grouping
        Grouping m_Grouping;
        CreateLodGroup m_CreateLodGroup;
        HideInHierarchy m_ParentHoldingHideables;

        // Environment system
        GameObject m_EnvPrefab;
        string m_SkyboxName = string.Empty;
        Material m_NewSkybox;
        GameObject m_EnvVolumeUrp;
        Cubemap m_EnvSpaceEmission;

        EnvironmentController m_SelectedEnvironment;
        EnvironmentControllerEditor m_EnvEditor;
        HideInHierarchyController m_HideInHierarchyEditor;

        // UI blocker when a process is running
        bool m_BlockUI = false;

        enum Tab
        {
            LODGrouping,
            Shader,
            Environment,
        }

        Tab m_CurrentTab;

        void OnEnable()
        {
#if UNITY_URP
            m_EnvPrefab = UnityEditor.AssetDatabase.LoadAssetAtPath(PlateauToolkitRenderingPaths.k_EnvPrefabUrp, typeof(GameObject)) as GameObject;
            m_SkyboxName = PlateauRenderingConstants.k_SkyboxNewShaderName;
            m_NewSkybox = (Material)AssetDatabase.LoadAssetAtPath(PlateauToolkitRenderingPaths.k_SkyboxUrp, typeof(Material));
            m_EnvVolumeUrp = UnityEditor.AssetDatabase.LoadAssetAtPath(PlateauToolkitRenderingPaths.k_EnvVolumeUrp, typeof(GameObject)) as GameObject;
#elif UNITY_HDRP
            m_EnvPrefab = UnityEditor.AssetDatabase.LoadAssetAtPath(PlateauToolkitRenderingPaths.k_EnvPrefabHdrp, typeof(GameObject)) as GameObject;
            m_EnvSpaceEmission = UnityEditor.AssetDatabase.LoadAssetAtPath(PlateauToolkitRenderingPaths.k_EnvSpaceEmission, typeof(Cubemap)) as Cubemap;
#endif

            m_Grouping = new Grouping();
            m_Grouping.OnProcessingFinished += UnblockUI;

            m_CreateLodGroup = new CreateLodGroup();
            m_CreateLodGroup.OnProcessingFinished += UnblockUI;

            m_AutoTextureProcessor = new AutoTexturing();
            m_AutoTextureProcessor.Initialize();
            m_AutoTextureProcessor.OnProcessingFinished += UnblockUI;
            m_CurrentTab = Tab.Environment;

            SceneView scene = SceneView.lastActiveSceneView;
            if( scene != null)
            {
                scene.sceneViewState.alwaysRefresh = true;
            }
        }

        void OnGUI()
        {
            int mediumButtonWidth = 53;
            int mediumButtonHeight = 53;
            #region Header
            m_Window ??= GetWindow<PlateauToolkitRenderingWindow>();
            PlateauToolkitEditorGUILayout.HeaderLogo(m_Window.position.width);

            #endregion

            if (m_BlockUI)
            {
                return;
            }

            #region Rendering feature tabs

            var imageButtonGUILayout = new PlateauToolkitRenderingGUILayout.PlateauRenderingEditorImageButtonGUILayout(
              mediumButtonWidth,
            mediumButtonHeight);

            bool TabButton(string iconPath, Tab tab)
            {
                Color? buttonColor = tab == m_CurrentTab ? Color.cyan : null;
                if (imageButtonGUILayout.Button(iconPath, buttonColor))
                {
                    m_CurrentTab = tab;
                    return true;
                }

                return false;
            }

            PlateauToolkitRenderingGUILayout.GridLayout(
                m_Window.position.width,
                mediumButtonWidth,
                new Action[]
                {
                    () =>
                    {
                         if (TabButton(PlateauToolkitRenderingPaths.k_EnvIcon, Tab.Environment))
                        {
                        }
                    },
                    () =>
                    {
                        if (TabButton(PlateauToolkitRenderingPaths.k_ShaderIcon, Tab.Shader))
                        {

                        }
                    },
                    () =>
                    {
                         if (TabButton(PlateauToolkitRenderingPaths.k_LodIcon, Tab.LODGrouping))
                        {

                        }
                    }
                });

            switch (m_CurrentTab)
            {
                case Tab.LODGrouping:
                    PlateauToolkitRenderingGUILayout.Header("LODグループ生成");
                    EditorGUILayout.Space();
                    EditorGUILayout.HelpBox("カメラからの距離にしたがって表示される地物の詳細が変動します。遠くにあるものを簡易表示したり非表示することによってパフォーマンスが向上します。", MessageType.Info);
                    EditorGUILayout.Space();

                    if (GUILayout.Button("LODグループ生成"))
                    {
                        bool isOptionSelected = EditorUtility.DisplayDialog(
                               "LODグループ生成の確認",
                               "シーンのオブジェクトが変更されます。実行しますか？",
                               "はい",
                               "いいえ"
                           );

                        if (isOptionSelected)
                        {
                            m_BlockUI = true;
                            m_Grouping.TrySeparateMeshes();
                            m_Grouping.GroupObjects();
                            m_CreateLodGroup.CreateLodGroups();
                        }
                    }
                    break;
                case Tab.Shader:
                    PlateauToolkitRenderingGUILayout.Header("自動テクスチャ生成");
                    EditorGUILayout.Space();
                    EditorGUILayout.HelpBox("建物のテクスチャを自動的に生成します。実在する建物の見た目と異なる場合があります。", MessageType.Info);
                    EditorGUILayout.Space();

                    EditorGUI.BeginChangeCheck();
                    if (GUILayout.Button("テクスチャ生成"))
                    {
                        if (!SelectedObjectsExist())
                        {
                            EditorUtility.DisplayDialog(
                                "オブジェクト選択の確認",
                                "少なくともオブジェクトを一つ選択してください。",
                                "OK"
                                );
                        }
                        else
                        {
                            bool isOptionSelected = EditorUtility.DisplayDialog(
                                   "テクスチャ作成の確認",
                                   "選択された地物にテクスチャを生成します。必要に応じてHierarchy にある地物の構成が変更されることもあります。実行しますか？",
                                   "はい",
                                   "いいえ"
                               );

                            if (isOptionSelected)
                            {
                                m_BlockUI = true;
                                if (!PlateauRenderingBuildingUtilities.IsMeshCombined(m_SelectedObjects[0]) && !m_SelectedObjects[0].name.Contains(PlateauRenderingConstants.k_Grouped))
                                {
                                    m_Grouping.GroupObjects();
                                }
                                m_AutoTextureProcessor.RunOptimizeProcess(m_SelectedObjects);
                            }
                        }
                    }

                    EditorGUILayout.Space();
                    EditorGUILayout.HelpBox("自動生成されたテクスチャの窓を表示または非表示します。LOD２のみに有効です。", MessageType.Info);
                    EditorGUILayout.Space();

                    if (GUILayout.Button("窓の表示の切り替え"))
                    {
                        if (!SelectedObjectsExist())
                        {
                            EditorUtility.DisplayDialog(
                               "オブジェクト選択の確認",
                               "少なくともオブジェクトを一つ選択してください。",
                               "OK"
                               );
                        }
                        else
                        {
                            foreach (GameObject building in m_SelectedObjects)
                            {
                                if (IsLod2(building))
                                {
                                    PlateauRenderingBuildingUtilities.SetWindowFlag(building, !PlateauRenderingBuildingUtilities.GetWindowFlag(building));
                                }
                            }
                        }

                    }

                    break;
                case Tab.Environment:
                    PlateauToolkitRenderingGUILayout.Header("環境システムの設定");
                    EditorGUILayout.Space();
                    EditorGUILayout.HelpBox("シーンに昼と夜の明るさを調整したり雨のような天気の設定を行うことができます。Time of day を変更することによって１日における日光の方向調整ができます。", MessageType.Info);
                    EditorGUILayout.Space();

                    m_SelectedEnvironment = FindFirstObjectByType<EnvironmentController>();

                    // select the environment controller in the scene
                    if (m_SelectedEnvironment != null)
                    {
                        m_EnvEditor = (EnvironmentControllerEditor)UnityEditor.Editor.CreateEditor(m_SelectedEnvironment);
                        m_EnvEditor.OnInspectorGUI();
                        EditorGUILayout.Space();
                        m_ParentHoldingHideables = FindFirstObjectByType<HideInHierarchy>();
                        m_HideInHierarchyEditor = (HideInHierarchyController)UnityEditor.Editor.CreateEditor(m_ParentHoldingHideables);
                        m_HideInHierarchyEditor.OnInspectorGUI();
                    }
                    else
                    {
                        // if there is no environment controller object in the scene
                        if (GUILayout.Button("環境要素"))
                        {
                            if (m_EnvPrefab != null && FindFirstObjectByType<EnvironmentController>() == null)
                            {
                                // If there is default directional light, disable it. Only run this once when loading the environment system.
                                Light[] directionalLights = FindObjectsByType<Light>(FindObjectsSortMode.None);

                                // Disable the default directional light
                                foreach (Light light in directionalLights)
                                {
                                    if (light.type == LightType.Directional)
                                    {
                                        light.gameObject.SetActive(false);
                                    }
                                }

                                if (m_EnvPrefab != null && FindFirstObjectByType<EnvironmentController>() == null)
                                {
#if UNITY_URP
                                    GameObject env = Instantiate(m_EnvPrefab);
                                    env.name = "Environment";
                                    env.GetComponent<HideInHierarchy>().m_ToggleHideChildren = true;

                                    // Replace skybox
                                    Material skybox = RenderSettings.skybox;
                                    if (skybox == null || skybox.shader.name != m_SkyboxName)
                                    {
                                        RenderSettings.skybox = m_NewSkybox;
                                        RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Trilight;
                                    }

                                    // Instantiate k_EnvVolumeUrp prefab
                                    GameObject envVolume = Instantiate(m_EnvVolumeUrp);
                                    envVolume.name = "Environment Volume";
#endif
#if UNITY_HDRP
                                    var env = Instantiate(m_EnvPrefab);
                                    env.name = "Environment";
                                    env.GetComponent<HideInHierarchy>().m_ToggleHideChildren = true;

                                    GameObject skyAndFogVolumeObject = GameObject.Find("Sky and Fog Volume");
                                    if (skyAndFogVolumeObject != null)
                                    {
                                        var skyAndFogVolume = skyAndFogVolumeObject.GetComponent<Volume>();
                                        if (skyAndFogVolume != null)
                                        {
                                            VolumeProfile profile = skyAndFogVolume.sharedProfile;
                                            if (profile.TryGet<PhysicallyBasedSky>(out var physicallyBasedSky))
                                            {
                                                physicallyBasedSky.spaceEmissionTexture.overrideState = true;
                                                physicallyBasedSky.spaceEmissionTexture.value = m_EnvSpaceEmission;
                                                physicallyBasedSky.spaceEmissionMultiplier.overrideState = true;
                                                physicallyBasedSky.spaceEmissionMultiplier.value = 1.0f; // Adjust this value as needed
                                            }
                                        }
                                    }
#endif
                                }
                            }
                        }
                    }
                    break;
            }
            #endregion
        }

        /// <summary>
        /// Check that the selected objects are Plateau objects. If a non texturable Plateau object is included, we remove that from the selected list and only
        /// texture the remaining Plateau objects.
        /// </summary>
        /// <returns></returns>
        bool SelectedObjectsExist()
        {
            m_SelectedObjects.Clear();
            PlateauRenderingMeshUtilities.GetSelectedGameObjects(m_SelectedObjects);

            for (int i = m_SelectedObjects.Count - 1; i >= 0; i--)
            {
                if (!IsValidObject(m_SelectedObjects[i]))
                {
                    m_SelectedObjects.RemoveAt(i);
                }
            }

            return m_SelectedObjects.Count > 0;
        }

        bool IsValidObject(GameObject obj)
        {
            if (obj.transform.parent == null || obj.transform.parent.parent == null)
            {
                return false;
            }

            if ((obj.GetComponent<MeshRenderer>() == null || obj.GetComponent<MeshFilter>() == null) && !(obj.name.Contains("bldg") || obj.name.Contains("BLD")))
            {
                return false;
            }

            if (!(obj.transform.parent.parent.name.Contains("bldg") || obj.transform.parent.parent.name.Contains("BLD")) &&
                !(obj.transform.parent.parent.name.Contains("GroupedObjects")))
            {
                return false;
            }

            if (!(obj.name.Contains("bldg") || obj.name.Contains("BLD")) &&
                !(obj.transform.parent.parent.name.Contains("bldg") || obj.transform.parent.parent.name.Contains("BLD")))
            {
                return false;
            }

            return true;
        }

        public bool IsLod2(GameObject target)
        {
            if (target.name.Contains("LOD2") || target.transform.parent.name.Contains("LOD2"))
            {
                return true;
            }
            else
            {
                return false;
            }
        }

        internal void UnblockUI()
        {
            m_BlockUI = false;
            Repaint();
        }
    }
}