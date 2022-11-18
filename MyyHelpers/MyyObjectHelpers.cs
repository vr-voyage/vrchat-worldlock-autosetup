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

        /**
         * <summary>Returns the parent of this GameObject, if any</summary>
         * 
         * <returns>
         * <para>
         * The parent of this GameObject,
         * or null if no transform parent were found.
         * </para>
         * </returns>
         */
        public static GameObject GetParent(this GameObject gameObject)
        {
            Transform parentTransform = gameObject.transform.parent;

            return (parentTransform != null ? parentTransform.gameObject : null);
        }

        /**
         * <summary>
         * Get the Animation (or transform.Find) relative path from the provided object,
         * to this object
         * </summary>
         * 
         * <param name="from">The object to compute the relative path from</param>
         * 
         * <returns>
         * The relative path from the provided object, to this one, or an empty string
         * if <paramref name="from"/> is not a parent of this GameObject.
         * </returns>
         */
        public static string PathFrom(this GameObject gameObject, GameObject from)
        {
            if (from == gameObject)
            {
                return "";
            }

            List<string> path = new List<string>(8);
            GameObject currentObject = gameObject;

            do
            {
                path.Add(currentObject.name);
                currentObject = currentObject.GetParent();
            } while ((currentObject != null) & (currentObject != from));

            /* No direct path were found */
            if (currentObject == null)
            {
                return "";
            }

            path.Reverse();
            return string.Join("/", path);
        }

        /**
         * <summary>Checks if this object is a child of the provided object.</summary>
         * 
         * <param name="potentialParent">The GameObject to check relationship to</param>
         * 
         * <returns>
         * <para>Returns true if this GameObject is a child of <paramref name="potentialParent"/></para>
         * <para>Returns false otherwise.</para>
         * </returns>
         */
        public static bool IsChildOf(this GameObject gameObject, GameObject potentialParent)
        {
            GameObject currentParent = gameObject.GetParent();
            while (currentParent != null)
            {
                if (currentParent == potentialParent)
                {
                    return true;
                }
                currentParent = currentParent.GetParent();
            }
            return false;
        }
    }
}

#endif