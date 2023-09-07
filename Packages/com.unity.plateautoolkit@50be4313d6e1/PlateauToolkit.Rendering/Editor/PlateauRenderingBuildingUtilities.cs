using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using Random = UnityEngine.Random;
using PLATEAU.CityInfo;
using UnityEngine.Rendering;

namespace PlateauToolkit.Rendering.Editor
{
    static class PlateauRenderingBuildingUtilities
    {
        const string k_LOD1 = "LOD1";
        const string k_LOD2 = "LOD2";
        const string k_LOD3 = "LOD3";
        const string k_Combined = " - Combined";
        const string k_Separated = " - Separated";

        public static string GetLODLevel(GameObject meshObject)
        {
            if(meshObject == null)
            {
                return "Null GameObject";
            }

            Transform parent = meshObject.transform.parent;
            if (parent == null)
            {
                return "Not a Plateau building mesh";
            }

            string name = meshObject.name;
            string parentName = parent.name;

            string grandparentName = null;
            if (parent.parent != null)
            {
                grandparentName = parent.parent.name;
            }

            if (!IsPlateauBuilding(meshObject.transform))
            {
                return "Not a Plateau building mesh";
            }

            string lodLevel = GetLodLevelFromNames(name, parentName, grandparentName, IsMeshCombined(meshObject), IsMeshSeparated(meshObject));

            return lodLevel ?? "LOD level not recognizable";
        }

        static string GetLodLevelFromNames(string name, string parentName, string grandparentName, bool isCombined, bool isSeparated)
        {
            string[] lODs = new string[] { k_LOD1, k_LOD2, k_LOD3 };

            foreach (string lod in lODs)
            {
                if (name.Contains(lod) || parentName.Contains(lod) || (!isCombined && grandparentName != null && grandparentName.Contains(lod)))
                {
                    if (isCombined)
                    {
                        return lod + k_Combined;
                    }
                    else if (isSeparated)
                    {
                        return lod + k_Separated;
                    }
                    else
                    {
                        return lod;
                    }
                }
            }

            return null;
        }

        public static bool IsMeshSeparated(GameObject obj)
        {
            if (obj.transform.childCount > 0)
            {
                Transform firstChild = obj.transform.GetChild(0);
                if (firstChild.GetComponent<MeshFilter>() != null)
                {
                    return true;
                }
            }
            return false;
        }

        public static bool IsMeshCombined(GameObject bldObj)
        {
            if (bldObj.name.StartsWith("group"))
            {
                return true;
            }
            else
            {
                return false;
            }
        }

        /// <summary>
        /// Checks if the input transform is from a Plateau City Model mesh.
        /// </summary>
        /// <remarks>First we check if the root object of this game object has a Plateau Model component. This is true with the original hierarchy after loading.
        /// The second check is for an object after changing hierarchy such as LOD grouping, which will look for a custom component.
        /// </remarks>
        /// <param name="tr"></param>
        /// <returns></returns>
        static bool IsPlateauBuilding(Transform tr)
        {
            return tr.root.gameObject.GetComponent<PLATEAUInstancedCityModel>() || tr.name.Contains(PlateauRenderingConstants.k_Grouped);
        }

        static bool IsSeparated(string name, string parentName)
        {
            return name.StartsWith("gnd_") || name.StartsWith("roof_") || name.StartsWith("wall_");
        }


        /// <summary>
        /// This method finds a sibling city model object with a different LOD level.
        /// We assume here the hierarchy has been modified such that different LOD levels of the same object are already grouped together.
        /// </summary>
        /// <param name="targetObject"></param>
        /// <param name="lodLevelToFind"></param>
        /// <returns></returns>
        public static GameObject FindSiblingLodObject(GameObject targetObject, string lodLevelToFind)
        {
            Transform parent = targetObject.transform.parent;
            foreach (Transform child in parent)
            {
                if(child != targetObject.transform && child.name.Contains(lodLevelToFind))
                {
                    return child.gameObject;
                }
            }

            return null;
        }

        public static GameObject CombineSeparatedLOD2(GameObject go)
        {
            string lodLevel = GetLODLevel(go);
            if (lodLevel == "LOD2 - Separated")
            {
                GameObject combinedObj = PlateauRenderingMeshUtilities.CombineChildren(go);

                // Get the MeshFilter and MeshRenderer from the combined object.
                MeshFilter meshFilter = combinedObj.GetComponent<MeshFilter>();
                MeshRenderer meshRenderer = combinedObj.GetComponent<MeshRenderer>();

                // Call WeldVertices and use the result to set the new mesh.
                if (meshFilter != null && meshRenderer != null)
                {
                    Mesh oldMesh = meshFilter.sharedMesh;
                    Mesh newMesh = PlateauRenderingMeshUtilities.WeldVertices(oldMesh, meshRenderer);
                    meshFilter.sharedMesh = newMesh;
                }

                return combinedObj;
            }
            else
            {
                Debug.Log(lodLevel + " is not LOD2 - Separated. Cannot combine.");
                return null;
            }
        }

        public static void SetLODVisibility(GameObject building, string lodLevelToShow)
        {
            // Start from the top of the hierarchy
            Transform currentTransform = building.transform;

            // Traverse down through the hierarchy
            while (currentTransform != null)
            {
                foreach (Transform descendant in currentTransform.GetComponentsInChildren<Transform>(true))
                {
                    // Check only objects with a MeshFilter component
                    MeshFilter meshFilter = descendant.gameObject.GetComponent<MeshFilter>();
                    if (meshFilter != null)
                    {
                        string lodLevel = PlateauRenderingBuildingUtilities.GetLODLevel(descendant.gameObject);

                        // If the LOD level matches the level to show, enable the game object, otherwise disable it
                        descendant.gameObject.SetActive(lodLevel.Contains(lodLevelToShow));
                    }
                }
                // Move to the next level in the hierarchy
                currentTransform = currentTransform.parent;
            }
        }

        public static List<Vector3> GetMinimumBoundingBoxOfRoof(GameObject selectedBuilding)
        {
            // Check if a GameObject is passed and it has a MeshFilter component
            if (selectedBuilding == null || selectedBuilding.GetComponent<MeshFilter>() == null)
            {
                Debug.LogError("No building selected or selected object has no MeshFilter component!");
                return null;
            }

            // Get all vertices and triangles from the mesh
            MeshFilter meshFilter = selectedBuilding.GetComponent<MeshFilter>();
            Vector3[] vertices = meshFilter.sharedMesh.vertices;
            int[] triangles = meshFilter.sharedMesh.triangles;

            var ceilingVerticesIndices = new HashSet<int>();

            // For each triangle, check if its normal is facing upwards
            for (int i = 0; i < triangles.Length; i += 3)
            {
                Vector3 v1 = selectedBuilding.transform.TransformPoint(vertices[triangles[i]]);
                Vector3 v2 = selectedBuilding.transform.TransformPoint(vertices[triangles[i + 1]]);
                Vector3 v3 = selectedBuilding.transform.TransformPoint(vertices[triangles[i + 2]]);

                var faceNormal = Vector3.Cross(v2 - v1, v3 - v1);

                // If the face is facing upwards, add its vertices to the ceiling vertices
                if (faceNormal.y > 0)
                {
                    ceilingVerticesIndices.Add(triangles[i]);
                    ceilingVerticesIndices.Add(triangles[i + 1]);
                    ceilingVerticesIndices.Add(triangles[i + 2]);
                }
            }

            // Transform vertices indices to actual world space vertices
            var ceilingVertices = ceilingVerticesIndices.Select(i => selectedBuilding.transform.TransformPoint(vertices[i])).ToList();

            // Calculate the convex hull from the ceiling vertices
            List<Vector3> hullVertices = PlateauRenderingGeomUtilities.GetConvexHull(ceilingVertices);

            // Determine the highest y value among all vertices of the convex hull
            float maxY = hullVertices.Max(vertex => vertex.y);

            // Adjust all y coordinates to match the highest y value
            for (int i = 0; i < hullVertices.Count; i++)
            {
                hullVertices[i] = new Vector3(hullVertices[i].x, maxY, hullVertices[i].z);
            }

            // Initialize boundingBox array
            var boundingBox = new Vector3[4];

            hullVertices = PlateauRenderingGeomUtilities.SortVerticesInClockwiseOrder(hullVertices);

            // Get the minimum bounding box for the convex hull
            Vector3[] mmb = PlateauRenderingGeomUtilities.OrientedMinimumBoundingBox2D(hullVertices, boundingBox);

            // Return the vertices of minimum bounding box as a list
            return new List<Vector3>(mmb);
        }

        public static void PlaceObstacleLightsOnBuildingCorners(GameObject go)
        {
            string lightPrefabPath = PlateauToolkitRenderingPaths.k_ObstacleLightPrefabPathUrp;
#if UNITY_HDRP
            lightPrefabPath = PlateauToolkitRenderingPaths.k_ObstacleLightPrefabPathHdrp;
#endif

            GameObject lightPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(lightPrefabPath);

            if (lightPrefab == null)
            {
                Debug.LogWarning("Obstacle light prefab not found at path: " + lightPrefabPath);
                return;
            }

            MeshFilter meshFilter = go.GetComponent<MeshFilter>();
            if (meshFilter == null)
            {
                return;
            }

            // calculate the bounding box of the building
            Bounds bounds = meshFilter.sharedMesh.bounds;

            // check if the building is higher than 60m
            if (bounds.size.y > 60)
            {
                // get roof triangles
                List<int> listofRoofTriangles = PlateauRenderingMeshUtilities.SelectFacesFacingUp(meshFilter.sharedMesh, 15);

                // get roof boundary edges
                List<Tuple<int, int>> roofBoundaryEdges = PlateauRenderingMeshUtilities.GetBoundaryEdges(listofRoofTriangles);

                // get roof vertices
                List<Vector3> roofVertices = GetRoofOutlineVertices(meshFilter.sharedMesh, roofBoundaryEdges);

                // If the average height of the roof vertices is less than 70% of the building's height,
                // use mesh vertices for roofVertices
                float avgHeight = roofVertices.Average(v => v.y);
                if (avgHeight < bounds.size.y * 0.7)
                {
                    roofVertices = meshFilter.sharedMesh.vertices.ToList();
                }

                // Get the minimum bounding box of the building's roof

                // Get the 4 corners of the roof's minimum bounding box
                List<Vector3> corners = GetMinimumBoundingBoxOfRoof(go);

                // Adjust the y coordinates of the corners to be the top of the bounding box
                for (int i = 0; i < corners.Count; i++)
                {
                    Vector3 corner = corners[i];
                    corners[i] = new Vector3(corner.x, bounds.max.y, corner.z);
                }

                // for each corner find the closest roof vertex
                var closestVertices = new Vector3[4];
                for (int i = 0; i < 4; i++)
                {
                    float minDistance = float.MaxValue;
                    Vector3 closestVertex = Vector3.zero;
                    foreach (Vector3 vertex in roofVertices)
                    {
                        float distance = Vector3.Distance(corners[i], vertex);
                        if (distance < minDistance)
                        {
                            minDistance = distance;
                            closestVertex = vertex;
                        }
                    }

                    Vector3 cornerWithSameY = new Vector3(corners[i].x, closestVertices[i].y, corners[i].z);
                    Vector3 direction = (cornerWithSameY - closestVertex).normalized;
                    closestVertices[i] = closestVertex + direction * -0.5f;
                }

                // Calculate median height of closestVertices
                List<float> heights = closestVertices.Select(v => v.y).OrderBy(v => v).ToList();
                float median;
                if (heights.Count % 2 == 0)
                    median = (heights[heights.Count / 2 - 1] + heights[heights.Count / 2]) / 2;
                else
                    median = heights[heights.Count / 2];

                // Exclude vertices whose height deviates a lot from the median
                float threshold = median * 0.05f; // 5% threshold
                closestVertices = closestVertices.Where(v => Math.Abs(v.y - median) <= threshold).ToList().ToArray();

                var vertices = closestVertices.ToList();
                Transform parent = meshFilter.transform;

                // Create a new GameObject for storing the light objects
                GameObject obstacleLightsNode = new GameObject("ObstacleLights");
                obstacleLightsNode.transform.SetParent(meshFilter.transform, false);

                foreach (Vector3 vertex in vertices)
                {
                    // Instantiate the lightPrefab
                    var light = PrefabUtility.InstantiatePrefab(lightPrefab) as GameObject;

                    // Set the position and rotation of the light
                    light.transform.position = vertex;
                    light.transform.rotation = Quaternion.identity;

                    // Set the parent of the light object
                    light.transform.SetParent(obstacleLightsNode.transform, true);

                    // Register the created light object for Undo operation
                    Undo.RegisterCreatedObjectUndo(light, "Place Lights On Building Corners");
                }
            }
        }
        static List<Vector3> GetRoofOutlineVertices(Mesh mesh, List<Tuple<int, int>> selectedEdges)
        {
            Vector3[] vertices = mesh.vertices;
            var roofVertices = new HashSet<Vector3>();

            foreach (Tuple<int, int> edge in selectedEdges)
            {
                Vector3 p0 = vertices[edge.Item1];
                Vector3 p1 = vertices[edge.Item2];

                roofVertices.Add(p0);
                roofVertices.Add(p1);
            }

            return roofVertices.ToList();
        }

        public static void SetWindowFlag(GameObject obj, bool isWindowOn)
        {
            int flagValue = isWindowOn ? 1 : 0;
            Renderer renderer = obj.GetComponent<Renderer>();
            if (renderer != null)
            {
                Undo.RecordObject(renderer, "Set Window Flag");

                foreach (Material material in renderer.sharedMaterials)
                {
                    if (material.HasProperty("_IsWindow"))
                    {
                        Undo.RecordObject(material, "Set Window Flag");
                        material.SetInt("_IsWindow", flagValue);
                    }
                }
            }
            else
            {
                Debug.LogWarning("Renderer not found on object: " + obj.name);
            }
        }

        public static bool GetWindowFlag(GameObject obj)
        {
            Renderer renderer = obj.GetComponent<Renderer>();
            if (renderer != null)
            {
                foreach (Material material in renderer.sharedMaterials)
                {
                    if (material.HasProperty("_IsWindow") && material.GetInt("_IsWindow") == 1)
                    {
                        return true;
                    }
                }
            }
            else
            {
                Debug.LogWarning("Renderer not found on object: " + obj.name);
            }
            return false;
        }

        public static void ChangeLOD2BuildingShader(GameObject obj)
        {
            string buildingShaderPath = "Shader Graphs/URP Building Shader";
#if UNITY_HDRP
            buildingShaderPath = "Shader Graphs/HDRP Building Shader";
#endif

            var newShader = Shader.Find(buildingShaderPath);
            if (newShader == null)
            {
                return;
            }

            Renderer renderer = obj.GetComponent<Renderer>();

            if (renderer != null)
            {
                Undo.RecordObject(renderer, "Change LOD2 Building Shader");

                foreach (Material material in renderer.sharedMaterials)
                {
                    // Check if the material has a main texture and if it's not null
                    if (material.mainTexture != null)
                    {
                        Undo.RecordObject(material, "Change LOD2 Building Shader");

                        // Store the previous shader for undo purposes
                        Shader previousShader = material.shader;

                        // Set the new shader
                        material.shader = newShader;

                        // Set shader-specific parameters if they exist
                        if (material.HasProperty("_FrameTileX"))
                        {
                            material.SetFloat("_FrameTileX", 0.4f);
                        }

                        if (material.HasProperty("_FrameTileY"))
                        {
                            material.SetFloat("_FrameTileY", 0.1f);
                        }

                        if (material.HasProperty("_FrameSizeX"))
                        {
                            material.SetFloat("_FrameSizeX", 0.7f);
                        }

                        if (material.HasProperty("_FrameSizeY"))
                        {
                            material.SetFloat("_FrameSizeY", 0.3f);
                        }

                        if (material.HasProperty("_NightEmission"))
                        {
                            material.SetFloat("_NightEmission", 0.35f);
                        }

                        if (material.HasProperty("_BaseMapOpacity"))
                        {
                            material.SetFloat("_BaseMapOpacity", 0.95f);
                        }
                        }
                    }
                }
            else
            {
                Debug.LogWarning("Renderer not found on object: " + obj.name);
            }
        }
        public static void SetBuildingVetexColorForWindow(Mesh mesh, Bounds boundingBox, GameObject go)
        {
            var colors = new Color[mesh.vertexCount];
            float largeFaceThreshold = 0.3f * boundingBox.size.y;

            // Find the minimum and maximum vertex heights in the mesh.
            float minY = float.PositiveInfinity;
            float maxY = float.NegativeInfinity;

            // Create a set to hold the world coordinates of upward facing vertices.
            HashSet<Vector3> upwardVertices = new HashSet<Vector3>();

            for (int i = 0; i < mesh.vertices.Length; i++)
            {
                Vector3 worldV = go.transform.TransformPoint(mesh.vertices[i]);
                minY = Mathf.Min(minY, worldV.y);
                maxY = Mathf.Max(maxY, worldV.y);

                // If the vertex is facing upwards and is at least 80% of the height of the object, add its world position to the set.
                if (mesh.normals[i].y > 0.9f && worldV.y >= minY + 0.8f * (maxY - minY))
                {
                    upwardVertices.Add(worldV);
                }
            }

            // Generate a single random alpha value for all vertices
            float randomAlpha = Random.Range(0f, 1f);

            for (int i = 0; i < mesh.triangles.Length; i += 3)
            {
                for (int j = 0; j < 3; j++)
                {
                    int vertexIndex = mesh.triangles[i + j];
                    Vector3 worldVertex = go.transform.TransformPoint(mesh.vertices[vertexIndex]);

                    // Calculate normalized height based on the min and max heights of the mesh.
                    float normalizedHeight = (worldVertex.y - minY) / (maxY - minY);

                    Color bottomColor = Color.green;
                    Color topColor = Color.black;

                    // Apply a linear gradient from bottomColor at the bottom of the mesh to topColor at the top if the vertex is below 80% of the height.
                    if (normalizedHeight < 0.8f)
                    {
                        Color vertexColor = Color.Lerp(bottomColor, topColor, normalizedHeight);
                        vertexColor.a = randomAlpha;
                        colors[vertexIndex] = vertexColor;
                    }
                    else if (upwardVertices.Contains(worldVertex)) // Check if the vertex is facing upwards only if it is above 80% of the height.
                    {
                        colors[vertexIndex] = new Color(topColor.r, topColor.g, topColor.b, randomAlpha);
                    }
                }
            }

            mesh.colors = colors;
        }

        public static void CreatePlaneUnderBuilding(GameObject building)
        {
            // Ensure the selectedBuilding and materialPath is valid
            if (building == null)
            {
                return;
            }

            string materialPath = PlateauToolkitRenderingPaths.k_FloorEmissionMaterialPathUrp;

#if UNITY_HDRP
            materialPath = PlateauToolkitRenderingPaths.k_FloorEmissionMaterialPathHdrp;
#endif

            Material floorMaterial = AssetDatabase.LoadAssetAtPath<Material>(materialPath);

            if (floorMaterial == null)
            {
                Debug.LogWarning("Floor Emission prefab not found at path: " + materialPath);
                return;
            }
            Material material = AssetDatabase.LoadAssetAtPath<Material>(materialPath);
            if (material == null)
            {
                Debug.LogWarning("Floor Emission Material not found at given path!");
                return;
            }

            // Get the minimum bounding box of the building's roof
            List<Vector3> boundingBox = GetMinimumBoundingBoxOfRoof(building);
            if (boundingBox == null || boundingBox.Count != 4)
            {
                return;
            }

            // Calculate the area of the bounding box
            float boundingBoxArea = Vector3.Cross(boundingBox[1] - boundingBox[0], boundingBox[2] - boundingBox[0]).magnitude * 0.5f;
            boundingBoxArea += Vector3.Cross(boundingBox[2] - boundingBox[0], boundingBox[3] - boundingBox[0]).magnitude * 0.5f;

            // If the area is smaller than the minimum, do not create the plane
            if (boundingBoxArea < 1.0f)
            {
                return;
            }

            // Create a plane under the selected building based on the bounding box
            Vector3 position = building.transform.position;
            position.y -= building.GetComponent<MeshFilter>().sharedMesh.bounds.size.y / 2;

            // Create a new GameObject which will be the parent of the plane
            var parentObject = new GameObject("FloorEmissionParent");

            // Set the position of the parent object to be the bottom of the building's bounding box in world space
            MeshFilter buildingMeshFilter = building.GetComponent<MeshFilter>();
            Bounds bounds = buildingMeshFilter.sharedMesh.bounds;
            Vector3 boundingBoxCenterLocal = bounds.center;
            Vector3 boundingBoxBottomLocal = boundingBoxCenterLocal - new Vector3(0, bounds.extents.y, 0); // Calculate the bottom of the bounding box in local space
            Vector3 boundingBoxBottomWorld = building.transform.TransformPoint(boundingBoxBottomLocal); // Convert from local to world space

            parentObject.transform.position = boundingBoxBottomWorld;

            // Create a new plane GameObject and configure it
            var plane = new GameObject("FloorEmission");
            plane.transform.SetParent(parentObject.transform); // Make the plane a child of the parent object
            plane.transform.localPosition = new Vector3(0.0f, 0.5f, 0.0f); // Set the position of the plane to be the same as its parent

            MeshFilter meshFilter = plane.AddComponent<MeshFilter>();
            meshFilter.sharedMesh = PlateauRenderingGeomUtilities.CreateMeshFromBoundingBox(boundingBox);

            var planeFaces = new List<int>(meshFilter.sharedMesh.GetTriangles(0));
            PlateauRenderingMeshUtilities.FlattenUVsWithBoundingBox(plane, meshFilter.sharedMesh, boundingBox);

            // Assign the material
            MeshRenderer meshRenderer = plane.AddComponent<MeshRenderer>();
            meshRenderer.sharedMaterial = material;

            // Disable the casting of shadows
            meshRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;

            // Scale the parent object
            parentObject.transform.localScale = new Vector3(1.8f, 1.0f, 1.8f);

            // Make the parent object a child of the selectedBuilding
            parentObject.transform.parent = building.transform;

            // Record the plane creation for the Undo system
            Undo.RegisterCreatedObjectUndo(parentObject, "Create Floor Emission Plane");

            return;
        }
    }
}
