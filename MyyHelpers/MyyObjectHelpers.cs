#if UNITY_EDITOR

using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Myy
{
    /**
     * <summary>Set of extensions methods for Unity GameObject</summary>
     */
    public static class MyyObjectHelpers
    {
        /**
         * <summary>
         * Remove all the components of type T on the provided object.
         * </summary>
         * 
         * <returns>
         * true if at least one component was removed.
         * </returns>
         */
        public static bool RemoveComponents<T>(this GameObject from)
            where T : Component
        {
            var components = from.GetComponents<T>();

            foreach (var component in components)
            {
                UnityEngine.Object.DestroyImmediate(component);
            }

            return components.Length != 0;
        }

        /**
         * <summary>Get all the children of the provided object</summary>
         * 
         * <remarks>The list is not sorted.</remarks>
         * 
         * <returns>
         * <para>An array containing all the children of the provided object.</para>
         * <para>The array is empty if the object has no child.</para>
         * </returns>
         */
        public static GameObject[] GetChildren(this GameObject from)
        {
            HashSet<GameObject> children = new HashSet<GameObject>();
            foreach (Transform t in  from.GetComponentInChildren<Transform>())
            {
                GameObject go = t.gameObject;
                if (go != from)
                {
                    children.Add(go);
                }
            }

            return children.ToArray();
        }

        /**
         * <summary>Remove all the children of a GameObject</summary>
         */
        public static void RemoveChildren(this GameObject from)
        {
            foreach (GameObject go in from.GetChildren())
            {
                UnityEngine.Object.DestroyImmediate(go);
            }
        }

        /**
         * <summary>Keep only the provided components type on the object.</summary>
         * 
         * <remarks>
         * <para>This will remove all the others components type, beside Transform and RectTransform</para>
         * <para>You do not need to pass Transform and RectTransform. They won't be removed.</para>
         * </remarks>
         * 
         * <example>
         * <code>
         * gameObject.KeepOnlyComponents(typeof(Collider), typeof(MeshRenderer), typeof(MeshFilter))
         * </code>
         * </example>
         * 
         * <param name="types">
         * The components types to keep.
         * </param>
         */
        public static void KeepOnlyComponents(this GameObject gameObject, params System.Type[] types)
        {
            HashSet<System.Type> scannedTypes = new HashSet<System.Type>(types);
            scannedTypes.Add(typeof(Transform));
            scannedTypes.Add(typeof(RectTransform));

            foreach (var component in gameObject.GetComponents<Component>())
            {
                bool keep = false;
                foreach (var scannedType in scannedTypes)
                {
                    /* The reverse actually does not work.
                     * BoxCollider.isAssignableFrom(Collider) -> false
                     * Collider.isAssignableFrom(BoxCollider) -> true
                     */
                    keep = (scannedType.IsAssignableFrom(component.GetType()));
                    if (keep) break;
                }
                
                if (!keep)
                {
                    UnityEngine.Object.DestroyImmediate(component);
                }              
            }
        }
    }
}

#endif