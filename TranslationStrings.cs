#if UNITY_EDITOR

using UnityEditor;
using UnityEngine;
using System.Collections.Generic;

namespace Myy
{
    public enum StringID
    {
        Invalid = 0,
        Label_AvatarToConfigure,
        Label_ObjectToLock,
        Label_LockAtWorldOrigin,
        Tooltip_LockAtWorldOrigin,
        Message_SelectAvatar,
        Message_InvalidSaveFolderProvided,
        Message_InsufficientSpaceMainMenu   
    }

    public static class TranslationStrings
    {

        public static Dictionary<StringID, string> messages = new Dictionary<StringID, string> {
            [StringID.Invalid] = "Invalid Message",
            [StringID.Label_AvatarToConfigure] = "Avatar to configure",
            [StringID.Label_ObjectToLock] = "Object to lock",
            [StringID.Label_LockAtWorldOrigin] = "Pin the object from world origin (0,0,0)",
            [StringID.Tooltip_LockAtWorldOrigin] =
                "By default, pinned objects will stay relative to the avatar coordinates at the pin time.\n" +
                "When activating this option, pinned objects will stay relative to the world origin (0,0,0).",
            [StringID.Message_SelectAvatar] = "Select the avatar to configure",
            [StringID.Message_InvalidSaveFolderProvided] =
                "The object provided is not a folder, and has no save folder associated.",
            [StringID.Message_InsufficientSpaceMainMenu] =
                "This avatar has too much menu items in the main menu.\n" +
                "Reduce the main menu to {maxMenuitems} items.\n" +
                "Else the script cannot add the required buttons.\n",
            
        };

        /* FIXME Use get/set */
        /* FIXME Make the type a specific type. Implement the function on it. */
        public static string Translation(this StringID stringID)
        {
            /* TODO Logic to get the right translation */
            /* TODO Error management */
            return messages[stringID];
        }

        

    }
}

#endif