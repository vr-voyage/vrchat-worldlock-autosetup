#if UNITY_EDITOR

using UnityEngine;
using UnityEngine.Animations;

namespace Myy
{
    public partial class SetupWindow
    {
        public class MyyConstraintHelpers
        {
            public static ConstraintSource ConstraintSource(Transform transform, float weight)
            {
                return new ConstraintSource()
                {
                    sourceTransform = transform,
                    weight = weight
                };
            }
        }
    }
}

#endif