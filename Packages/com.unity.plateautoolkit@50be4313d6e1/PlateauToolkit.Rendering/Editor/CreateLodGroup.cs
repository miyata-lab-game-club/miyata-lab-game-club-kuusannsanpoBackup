using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace PlateauToolkit.Rendering.Editor
{

    class CreateLodGroup
    {
        internal event Action OnProcessingFinished;

        internal void CreateLodGroups()
        {
            LodGroupedObjectContainer groupedBuildingsParent = GameObject.FindObjectOfType<LodGroupedObjectContainer>();

            if (groupedBuildingsParent == null)
            {
                return;
            }
            foreach (Transform buildingGroup in groupedBuildingsParent.GetAllGroupedObjects())
            {
                if (buildingGroup.gameObject.GetComponent<LODGroup>() != null)
                {
                    continue;
                }

                if (buildingGroup.name.Contains(PlateauRenderingConstants.k_PostfixLodGrouped) || !buildingGroup.name.Contains(PlateauRenderingConstants.k_Grouped))
                {
                    continue;
                }
                Undo.IncrementCurrentGroup();
                Undo.SetCurrentGroupName("LOD grouping");
                Undo.RegisterCompleteObjectUndo(buildingGroup.gameObject, "LOD grouping");

                LODGroup lodGroup = buildingGroup.gameObject.AddComponent<LODGroup>();
                lodGroup.fadeMode = LODFadeMode.CrossFade;

                List<Renderer> lodRenderers = new List<Renderer>();
                List<LOD> lods = new List<LOD>();

                // Because the loop is reversed, the highest LOD as defined by Plateau will be stored as Unity's lowest LOD
                // This is assuming Plateau model will be stacked LOD0, LOD1, LOD2... etc
                for (int i = buildingGroup.transform.childCount - 1; i >= 0; i--)
                {
                    lodRenderers.Clear();
                    Transform lodBldgGroup = buildingGroup.transform.GetChild(i);
                    Renderer[] renderersInChildren = lodBldgGroup.GetComponentsInChildren<Renderer>();
                    lodBldgGroup.gameObject.SetActive(true);
                    lodRenderers.AddRange(renderersInChildren);
                    lods.Add(new LOD(PlateauRenderingConstants.k_LodDistances[i], lodRenderers.ToArray()));
                }

                lodGroup.SetLODs(lods.ToArray());
                lodGroup.RecalculateBounds();
                buildingGroup.gameObject.name += PlateauRenderingConstants.k_PostfixLodGrouped;

                // When creating LOD groups from combined mesh,
                // we break down the meshes and these operations include reparenting and destroying objects.
                // We need to clear the Undo stack for these operations.
                Undo.CollapseUndoOperations(Undo.GetCurrentGroup());
                Undo.ClearUndo(buildingGroup.gameObject);
            }
            OnProcessingFinished();
        }
    }
}