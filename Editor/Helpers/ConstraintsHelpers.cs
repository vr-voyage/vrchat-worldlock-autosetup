#if UNITY_EDITOR
using UnityEngine;
using System.Collections.Generic;
using VRC.SDK3.Avatars.Components;
using UnityEngine.Animations;
using VRC.Dynamics;

namespace Myy
{
    public static class ConstraintsHelpers
    {
        /** <summary>
         * Swap Unity Constraint sources referencing the original avatar
         * to reference the copy on which the item is being setup
         * </summary>
         * 
         * <example>
         * For example, if the item to lock references the 'Head' of the avatar to be copied,
         * we swap this reference to the 'Head of the copied avatar on which we're setting
         * the item on.
         * </example>
         * 
         * <param name="mainAvatar">The original avatar which the current item might reference</param>
         * <param name="avatarCopy">
         * The avatar reference to switch to,
         * if the current item constraints references mainAvatar
         * </param>
         */
        public static void FixUnityConstraintSources(GameObject avatar, GameObject avatarCopy, GameObject fixedObject)
        {
            List<ConstraintSource> constraintSources = new List<ConstraintSource>();
            foreach (var constraint in fixedObject.GetComponentsInChildren<IConstraint>())
            {
                constraintSources.Clear();

                /* Get the sources
                 * Check if they refer to a member of the mainAvatar
                 * If that's the case, find the same member in the copy
                 * and set it as the new source.
                 */
                constraint.GetSources(constraintSources);
                int nSources = constraintSources.Count;
                for (int i = 0; i < nSources; i++)
                {
                    /* For each source transform set on the object,
                     * Get the related GameObject (if any). */
                    var source = constraintSources[i];
                    if (source.sourceTransform == null) continue;

                    GameObject sourceObject = source.sourceTransform.gameObject;
                    if (sourceObject == null) continue;

                    /* Check if there's a direct path between the source GameObject and
                     * the maih Avatar */
                    string relativePath = sourceObject.PathFrom(avatar);

                    /* Since we can now add items to generated copies,
                     * we need to try being a little smarter when it comes to relocating
                     * Constraint sources.
                     * So now, if the object doesn't appear to be fixed to the current Avatar,
                     * we'll try to check if it's actually fixed to any avatar.
                     * If that's the case, we'll use the path from that avatar to the
                     * ConstraintSource and try to use it afterwards.
                     */
                    if (relativePath == null)
                    {
                        var owningAvatar = sourceObject.GetComponentInParent<VRCAvatarDescriptor>(true);
                        if (owningAvatar == null) continue;

                        if (owningAvatar.gameObject == null) continue;

                        relativePath = sourceObject.PathFrom(owningAvatar.gameObject);
                        Debug.Log($"[SetupObjectVRCConstraints] [FixConstraintSources] Mimicked relative path : {relativePath}");
                    }

                    if (relativePath == null) continue;

                    /* Try to use the same relative path from the avatar copy, to
                     * find a similar GameObject Transform. */
                    Transform copyTransform = avatarCopy.transform.Find(relativePath);

                    if (copyTransform == null) continue;

                    /* Use the found GameObject Transform copy as the new source */
                    source.sourceTransform = copyTransform;
                    constraintSources[i] = source; // Structure needs to be copied back
                }

                /* Set the potentially modified sources back */
                constraint.SetSources(constraintSources);

            }
        }

        /** <summary>
         * Swap VRChat Constraint sources referencing the original avatar
         * to reference the copy on which the item is being setup
         * </summary>
         * 
         * <example>
         * For example, if the item to lock references the 'Right Hand' of the avatar to be copied,
         * we swap this reference to the 'Right Hand' of the copied avatar on which we're setting
         * the item on.
         * </example>
         * 
         * <param name="mainAvatar">The original avatar which the current item might reference</param>
         * <param name="avatarCopy">
         * The avatar reference to switch to,
         * if the current item constraints references mainAvatar
         * </param>
         */
        /* FIXME Factorize */
        public static void FixVRChatConstraintSources(GameObject avatar, GameObject avatarCopy, GameObject fixedObject)
        {
            foreach (var constraint in fixedObject.GetComponentsInChildren<VRCConstraintBase>())
            {
                VRCConstraintSourceKeyableList constraintList = constraint.Sources;

                int nSources = constraintList.Count;
                for (int i = 0; i < nSources; i++)
                {
                    /* For each source transform set on the object,
                     * Get the related GameObject (if any). */
                    VRCConstraintSource source = constraintList[i];

                    Transform sourceTransform = source.SourceTransform;
                    if (sourceTransform == null) continue;

                    GameObject sourceObject = sourceTransform.gameObject;
                    if (sourceObject == null) continue;

                    /* Check if there's a direct path between the source GameObject and
                     * the maih Avatar */
                    string relativePath = sourceObject.PathFrom(avatar);

                    /* Since we can now add items to generated copies,
                     * we need to try being a little smarter when it comes to relocating
                     * Constraint sources.
                     * So now, if the object doesn't appear to be fixed to the current Avatar,
                     * we'll try to check if it's actually fixed to any avatar.
                     * If that's the case, we'll use the path from that avatar to the
                     * ConstraintSource and try to use it afterwards.
                     */
                    if (relativePath == null)
                    {
                        var owningAvatar = sourceObject.GetComponentInParent<VRCAvatarDescriptor>(true);
                        if (owningAvatar == null)
                        {
                            Debug.Log("No avatar component found");
                            continue;
                        }

                        if (owningAvatar.gameObject == null)
                        {
                            Debug.Log("Could not get the avatar it's linked to...  ???");
                            continue;
                        }

                        relativePath = sourceObject.PathFrom(owningAvatar.gameObject);
                        Debug.Log($"[SetupObjectVRCConstraints] [FixConstraintSources] Mimicked relative path : {relativePath}");
                    }

                    if (relativePath == null) continue;

                    /* Try to use the same relative path from the avatar copy, to
                     * find a similar GameObject Transform. */
                    Transform copyTransform = avatarCopy.transform.Find(relativePath);

                    if (copyTransform == null) continue;

                    /* Use the found GameObject Transform copy as the new source */
                    source.SourceTransform = copyTransform;
                    constraint.Sources[i] = source; // Structure needs to be copied back
                }

            }
        }

        /**
         * <summary>
         * Swap constraint sources referencing the original avatar
         * to reference the copy on which the item is being setup
         * </summary>
         * 
         * <param name="mainAvatar">The original avatar which the current item might reference</param>
         * <param name="avatarCopy">
         * The avatar reference to switch to,
         * if the current item constraints references mainAvatar
         * </param>
         */
        public static void FixExternalConstraintSources(GameObject mainAvatar, GameObject avatarCopy, GameObject fixedObjectCopy)
        {
            FixUnityConstraintSources(mainAvatar, avatarCopy, fixedObjectCopy);
            FixVRChatConstraintSources(mainAvatar, avatarCopy, fixedObjectCopy);
        }
    }

}

#endif