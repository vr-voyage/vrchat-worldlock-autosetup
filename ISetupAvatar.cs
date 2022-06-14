#if UNITY_EDITOR

using UnityEngine;
using VRC.SDK3.Avatars.Components;

namespace Myy
{
    public interface ISetupAvatar
    {
        /**
         * <summary>Set the base path for accessing the different assets
         * used by this tool.</summary>
         * 
         * <param name="path">Base path of the assets</param>
         */
        void SetAssetsPath(string path);

        /**
         * <summary>Setup the avatar, making it able to lock the provided objects
         * in the world.</summary>
         * 
         * <remarks>
         * <para>
         * Avatar here means a VRChat Avatar, not a Mecanim avatar.
         * </para>
         * <para>
         * Note : <paramref name="lockAtWorldCenter"/> might be ignored.</para>
         * </remarks>
         * 
         * <param name="avatar">A VRChat Avatar descriptor component</param>
         * <param name="lockAtWorldCenter">Whether to lock objects at world center</param>
         * <param name="objectsToFix">The objects to be able to lock into the world</param>
         */
        void Setup(VRCAvatarDescriptor avatar, bool lockAtWorldCenter, params GameObject[] objectsToFix);
    }

}

#endif
