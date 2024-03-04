#if UNITY_EDITOR

using UnityEditor;
using UnityEngine;
using System.Collections.Generic;
using System.Globalization;
using System;

namespace Myy
{


    public static class TranslationStrings
    {

        const string configUILangKey = "Voyage_WorldLockAutoSetup_Lang";

        static TranslationStrings()
        {
            int key = EditorPrefs.GetInt(configUILangKey, -1);
            if (key == -1 || key >= (int)Lang.Count)
            {
                if (Application.systemLanguage == SystemLanguage.Japanese)
                {
                    key = (int)Lang.JA_JP;
                }
                else
                {
                    key = (int)Lang.EN_GB;
                }

                try
                {
                    EditorPrefs.SetInt(configUILangKey, key);
                }
                catch (Exception e)
                {
                    /* It's annoying if it fails, but it's not the
                     * end of the world.
                     * Just alert the user.
                     */
                    Debug.LogWarning("[World Lock Autosetup] Could not save the default language settings.");
                    Debug.LogWarning($"[World Lock Autosetup] Reason : {e.StackTrace}");
                }
            }
            if (key == (int)Lang.JA_JP)
            {
                currentTranslation = messagesJP;
            }
            else
            {
                currentTranslation = messages;
            }
            
            


        }

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
            Button_InspectExpressionMenu,
            Label_HiddenWhenOff,
            Label_DontDisableConstraints,
            Message_AvatarAlreadyConfiguredParticles,
            Message_AvatarHasNoFxLayerStrangeBug
        }

        public enum Lang
        {
            EN_GB,
            JA_JP,
            Count
        };

        /* FIXME :
         * Stupid hack to get a useable configuration window.
         */
        public enum HumanReadableLang
        {
            English,
            日本語
        };

        public static void SetLang(HumanReadableLang language)
        {
            Lang lang = Lang.EN_GB;
            switch (language)
            {
                case HumanReadableLang.English:
                    lang = Lang.EN_GB;
                    currentTranslation = messages;
                    break;
                case HumanReadableLang.日本語:
                    lang = Lang.JA_JP;
                    currentTranslation = messagesJP;
                    break;
                default:
                    lang = Lang.EN_GB;
                    Debug.LogWarning($"[World Lock Autosetup] Wrong language setting : {language}. Defaulting to English.");
                    currentTranslation = messages;
                    break;
            }

            EditorPrefs.SetInt(configUILangKey, (int)lang);
        }

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
            [StringID.Button_InspectExpressionMenu] = "Open the Menu in the Inspector",
            [StringID.Label_HiddenWhenOff] = "Hide these items when not locked",
            [StringID.Label_DontDisableConstraints] = "Disable these items own constraints when locked",
            [StringID.Message_AvatarAlreadyConfiguredParticles] = "Equipping another item on an already equipped avatar copy is not supported in this mode.",
            [StringID.Message_AvatarHasNoFxLayerStrangeBug] = "It seems impossible to setup an FX layer on this avatar.\nThis is a very strange bug. Could you report it with your configuration ?"
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
            [StringID.Button_InspectExpressionMenu] = "MenuをInspectorで開く",
            [StringID.Label_HiddenWhenOff] = "固定するまで、アイテムを隠す",
            [StringID.Label_DontDisableConstraints] = "固定する時、既設のConstraintを無効化する",
            [StringID.Message_AvatarAlreadyConfiguredParticles] = 
                "このモードでは、すでに装備しているアバターコピーにアイテムを装備することはできません。",
            [StringID.Message_AvatarHasNoFxLayerStrangeBug] = "FXを定義出来ないアバターみたいです。\nこのバグは非常に珍しいですから、報告出来たら幸いです"
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
