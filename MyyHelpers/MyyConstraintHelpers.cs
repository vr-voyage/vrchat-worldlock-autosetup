#if UNITY_EDITOR

using UnityEngine;
using UnityEngine.Animations;

namespace Myy
{
    /**
     * <summary>Utility functions to deal with Unity Constraints components.</summary>
     */
    public static class MyyConstraintHelpers
    {
        /**
         * <summary>Generate a Unity Constraint Source from a specific transform and a weight.</summary>
         * 
         * <remarks>Just a wrapper around new ConstraintSource() { ... }</remarks>
         * 
         * <param name="transform">The transform to link the constraint source to</param>
         * <param name="weight">The weight of this source</param>
         * 
         * <returns>A Constraint Source object, setup with the provided transform and weight.</returns>
         */
        public static ConstraintSource ConstraintSource(Transform transform, float weight)
        {
            return new ConstraintSource()
            {
                sourceTransform = transform,
                weight = weight
            };
        }

        private static int AddSourceTo(IConstraint constraint, Transform transform, float weight)
        {
            return constraint.AddSource(ConstraintSource(transform, weight));
        }

        /**
         * <summary>Add a transform as constraint source, with specified weight.</summary>
         * 
         * <param name="transform">
         * The transform used as a source.
         * </param>
         * 
         * <param name="weight">
         * The weight the provided source
         * </param>
         * 
         * <returns>
         * Returns the index of the added source.
         * </returns>
         */
        public static int AddSource(this IConstraint constraint, Transform transform, float weight = 1.0f)
        {
            return AddSourceTo(constraint, transform, weight);
        }

        /**
         * <summary>Add a GameObject as constraint source, with specified weight.</summary>
         * 
         * <param name="gameObject">
         * The GameObject, which transform will be used as a source.
         * </param>
         * 
         * <param name="weight">
         * The weight the provided source
         * </param>
         * 
         * <returns>
         * Returns the index of the added source.
         * </returns>
         */

        public static int AddSource(this IConstraint constraint, GameObject gameObject, float weight = 1.0f)
        {
            return AddSourceTo(constraint, gameObject.transform, weight);
        }

    }
}

#endif