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
            Button_ResetPanel,
            Button_InspectExpressionMenu
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
            [StringID.Label_LockAtWorldOrigin] = "Lock from world origin",
            [StringID.Label_SaveDirectory] = "Save files to",
            [StringID.Tooltip_LockAtWorldOrigin] =
                "By default, pinned objects will appear relative to the avatar coordinates when enabled.\n" +
                "Activating this option will make the pinned objects always appear relative to the world origin (0,0,0).",
            [StringID.Message_SelectAvatarToConfigure] = "Select the avatar to configure",
            [StringID.Message_InvalidSaveFolderProvided] =
                "The object provided is not a folder, and has no save folder associated.",
            [StringID.Message_InsufficientSpaceMainMenu] =
                "This avatar Expression menu is full.\n" +
                "In order to add {0} additional controls,\n" +
                "Reduce its main menu to {1} controls.",
            [StringID.Message_SelectObjectToConfigure] =
                "Select the object to setup.\n" +
                "Just drop it on top of this window",
            [StringID.Message_SelectObjectsToConfigure] =
                "Select at least one object to setup.\n" +
                "You can drop the objects to configure inside this window.",
            [StringID.Button_SetupNewAvatar] = "APPLY",
            [StringID.Button_ResetPanel] = "RESET",
            [StringID.Button_InspectExpressionMenu] = "Open the Menu in the Inspector"

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
                "そのAvatarのExpression Menuは一杯です。\n" +
                "{0}個の操作ボタンを追加できるように、\n" +
                "MenuのControlsを{1}個まで減らしてください！",
            [StringID.Message_SelectObjectToConfigure] =
                "固定したいアイテムを選んでください。\n" +
                "ヒエラルキーから、その画面にドロップしてよろしいです。",
            [StringID.Message_SelectObjectsToConfigure] =
                "固定したいアイテムを選んでください。\n" +
                "ヒエラルキーから、その画面にドロップしてよろしいです。",
            [StringID.Button_SetupNewAvatar] = "適用",
            [StringID.Button_ResetPanel] = "リセット",
            [StringID.Button_InspectExpressionMenu] = "MenuをInspectorで開く"

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
