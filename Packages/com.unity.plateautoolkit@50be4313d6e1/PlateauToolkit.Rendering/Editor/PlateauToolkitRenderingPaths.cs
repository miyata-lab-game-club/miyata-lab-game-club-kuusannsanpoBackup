﻿namespace PlateauToolkit.Rendering
{

    public class PlateauToolkitRenderingPaths
    {
        const string k_EditorRootFolder = "Packages/com.unity.plateautoolkit/PlateauToolkit.Rendering/Editor";
        const string k_RuntimeRootFolder = "Packages/com.unity.plateautoolkit/PlateauToolkit.Rendering/Runtime";
        const string k_SpritesFolder = "Packages/com.unity.plateautoolkit/PlateauToolkit.Rendering/Editor/Sprites";
        const string k_PrefabsFolder = "Packages/com.unity.plateautoolkit/PlateauToolkit.Rendering/Editor/Prefabs";
        const string k_CommonSpritesFolder = "Packages/com.unity.plateautoolkit/PlateauToolkit.Foundation/Editor/Sprites";
        const string k_DayNightCycleFolder = "Packages/com.unity.plateautoolkit/PlateauToolkit.Rendering/Runtime/DayNightCycle";
        const string k_MaterialFolder = "Packages/com.unity.plateautoolkit/PlateauToolkit.Rendering/Runtime";

        public static readonly string k_PlateauLogo = $"{k_CommonSpritesFolder}/PlateauToolkit_Logo.png";
        public static readonly string k_EnvIcon = $"{k_SpritesFolder}/PlateauToolkitRendering_Env.png";
        public static readonly string k_ShaderIcon = $"{k_SpritesFolder}/PlateauToolkitRendering_Autotexture.png";
        public static readonly string k_LodIcon = $"{k_SpritesFolder}/PlateauToolkitRendering_Lod.png";
        public static readonly string k_EnvPrefabUrp = $"{k_PrefabsFolder}/URP/Environment.prefab";
        public static readonly string k_EnvPrefabHdrp = $"{k_PrefabsFolder}/HDRP/Environment.prefab";
        public static readonly string k_EnvVolumeUrp = $"{k_PrefabsFolder}/URP/EnvironmentVolume.prefab";
        public static readonly string k_SkyboxUrp = $"{k_DayNightCycleFolder}/URP/PhysicallyBasedSky.mat";
        public static readonly string k_EnvSpaceEmission = $"{k_DayNightCycleFolder}/HDRP/SpaceEmission.exr";
        public static readonly string k_BuildingTextureAssetUrp = $"{k_EditorRootFolder}/Building Plateau Material Assignment Table URP.asset";
        public static readonly string k_ObstacleLightPrefabPathHdrp = $"{k_RuntimeRootFolder}/ObstacleLight/Prefabs/HDRP/ObstacleLight.prefab";
        public static readonly string k_FloorEmissionMaterialPathUrp = $"{k_RuntimeRootFolder}/FloorEmission/Materials/URP/FloorEmission.mat";
        public static readonly string k_FloorEmissionMaterialPathHdrp = $"{k_RuntimeRootFolder}/FloorEmission/Materials/HDRP/FloorEmission.mat";
        public static readonly string k_ObstacleLightPrefabPathUrp = $"{k_RuntimeRootFolder}/ObstacleLight/Prefabs/URP/ObstacleLight.prefab";
        public static readonly string k_BuildingTextureAssetHdrp = $"{k_EditorRootFolder}/Building Plateau Material Assignment Table HDRP.asset";
        public static readonly string k_SkyboxMaterialAssetUrp = $"{k_MaterialFolder}/DayNightCycle/URPPhysicallyBasedSky.mat";
    }

}