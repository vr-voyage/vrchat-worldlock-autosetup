#if UNITY_EDITOR

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using VRC.SDK3.Avatars.Components;

public partial class SetupWindow
{
    public interface ISetupAvatar
    {
        void SetAssetsPath(string path);
        void Setup(VRCAvatarDescriptor avatar, params GameObject[] objectsToFix);
    }
}

#endif
