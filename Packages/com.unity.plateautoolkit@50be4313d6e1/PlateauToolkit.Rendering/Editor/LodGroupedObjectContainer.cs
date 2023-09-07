using System.Collections.Generic;
using UnityEngine;

namespace PlateauToolkit.Rendering.Editor
{
    class LodGroupedObjectContainer : MonoBehaviour
    {
        internal IEnumerable<Transform> GetAllGroupedObjects()
        {
            foreach (Transform child in transform)
            {
                yield return child;
            }
        }
    }
}