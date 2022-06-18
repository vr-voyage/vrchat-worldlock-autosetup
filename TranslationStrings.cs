#if UNITY_EDITOR

using UnityEditor;
using UnityEngine;
using System.Collections.Generic;

namespace Myy
{


    public static class TranslationStrings
    {

        public enum StringID
        {
            Invalid = 0,
            Label_AvatarToConfigure,
            Label_ObjectToLock,
            Label_ObjectsToLock,
            Label_ConstraintsOptions,
            Label_LockAtWorldOrigin,
            Label_SaveDirectory,
            Tooltip_LockAtWorldOrigin,
            Message_SelectAvatarToConfigure,
            Message_InvalidSaveFolderProvided,
            Message_InsufficientSpaceMainMenu,
            Message_SelectObjectToConfigure,
            Message_SelectObjectsToConfigure,
            Button_SetupNewAvatar,
            Button_ResetPanel
        }

        enum Lang
        {
            EN_GB,
            FR_FR,
            JA_JP
        };

        public static Dictionary<StringID, string> messages = new Dictionary<StringID, string> {
            [StringID.Invalid] = "Invalid Message",
            [StringID.Label_AvatarToConfigure] = "Avatar to configure",
            [StringID.Label_ObjectToLock] = "Object to lock",
            [StringID.Label_ObjectsToLock] = "Objects to lock",
            [StringID.Label_ConstraintsOptions] = "Constraints options",
            [StringID.Label_LockAtWorldOrigin] = "Pin the object from world origin (0,0,0)",
            [StringID.Label_SaveDirectory] = "Save files to",
            [StringID.Tooltip_LockAtWorldOrigin] =
                "By default, pinned objects will stay relative to the avatar coordinates at the pin time.\n" +
                "When activating this option, pinned objects will stay relative to the world origin (0,0,0).",
            [StringID.Message_SelectAvatarToConfigure] = "Select the avatar to configure",
            [StringID.Message_InvalidSaveFolderProvided] =
                "The object provided is not a folder, and has no save folder associated.",
            [StringID.Message_InsufficientSpaceMainMenu] =
                "This tool will add {0} controls to the main menu.\n" +
                "Please ensure that this avatar has less than {1} controls.\n" +
                "Else, this script won't be able to add its own controls.",
            [StringID.Message_SelectObjectToConfigure] =
                "Select the object to setup.\n" +
                "Just drop it on top of this window",
            [StringID.Message_SelectObjectsToConfigure] =
                "Select at least one object to setup.\n" +
                "You can drop the objects to configure inside this window.",
            [StringID.Button_SetupNewAvatar] = "APPLY",
            [StringID.Button_ResetPanel] = "RESET"

        };

        public static Dictionary<StringID, string> messagesJP = new Dictionary<StringID, string>
        {
            [StringID.Invalid] = "メッセージが出ないはずだよ！",
            [StringID.Label_AvatarToConfigure] = "アバター",
            [StringID.Label_ObjectToLock] = "固定アイテム",
            [StringID.Label_ObjectsToLock] = "固定アイテム",
            [StringID.Label_ConstraintsOptions] = "束縛設定",
            [StringID.Label_LockAtWorldOrigin] = "ワールド原点から固定する",
            [StringID.Label_SaveDirectory] = "保存先",
            [StringID.Tooltip_LockAtWorldOrigin] =
                "普段は、アバターの位置からアイテムを置いて固定すします。\n" +
                "そのオプションを有効すると、ワールド原点から（0，0，0）アイテムを置いて固定します。",
            [StringID.Message_SelectAvatarToConfigure] = "アバターを選んでください",
            [StringID.Message_InvalidSaveFolderProvided] =
                "選んだ物はフォルダーじゃない。そして保存先もありません。",
            [StringID.Message_InsufficientSpaceMainMenu] =
                "このツールは{0}個のメニューアイテムを追加します。\n" +
                "選んだアバターのは今多すぎて、新しいを追加できません。\n" +
                "そのため、アバターのメニューアイテムを{1}個まで減らしてください。",
            [StringID.Message_SelectObjectToConfigure] =
                "固定したいアイテムを選んでください。\n" +
                "ヒエラルキーから、その画面にドロップしてよろしいです。",
            [StringID.Message_SelectObjectsToConfigure] =
                "固定したいアイテムを選んでください。\n" +
                "ヒエラルキーから、その画面にドロップしてよろしいです。",
            [StringID.Button_SetupNewAvatar] = "適用",
            [StringID.Button_ResetPanel] = "リセット"

        };

        public static Dictionary<StringID, string> currentTranslation = messagesJP;

        /* FIXME Use get/set */
        /* FIXME Make the type a specific type. Implement the function on it. */
        public static string Translate(StringID stringID)
        {
            /* TODO Logic to get the right translation */
            /* TODO Error management */
            return currentTranslation[stringID];
        }

        public static string Translate(StringID stringID, params object[] objects)
        {
            return string.Format(currentTranslation[stringID], objects);
        }
        

        

    }
}

#endif