using System.Collections.Generic;
using UnityEngine;

namespace Extensions
{
    public static class TransformExtension
    {
        /// <summary>
        /// Destroy all Children of the parent transform.
        /// Optionally do not destroy a specific Transform.
        /// </summary>
        /// <param name="parent">Parent Object to destroy children</param>
        /// <param name="doNotDestroy">Optional: specified Transform that should NOT be destroyed</param>
        public static void DestroyAllChildren(this Transform parent, Transform doNotDestroy = null)
        {
            // Loop through all the children and add them to a List to then be destroyed
            List<GameObject> toBeDestroyed = new List<GameObject>();

            foreach (Transform t in parent)
            {
                if (t != doNotDestroy)
                {
                    toBeDestroyed.Add(t.gameObject);
                }
            }
            // Loop through list and destroy all children immediately, so the child count is updated immediately
            for (int i = 0; i < toBeDestroyed.Count; i++)
            {
                UnityEngine.Object.DestroyImmediate(toBeDestroyed[i]);
            }
        }
    }
}