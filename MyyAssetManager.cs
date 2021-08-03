#if UNITY_EDITOR

using UnityEngine;
using UnityEditor;
using System;
using UnityEditor.Animations;

public partial class SetupWindow
{
    public class MyyAssetManager
    {
        public static string FilesystemFriendlyName(string name)
        {
            var invalids = System.IO.Path.GetInvalidFileNameChars();
            return string.Join("_", name.Split(invalids, StringSplitOptions.RemoveEmptyEntries)).TrimEnd('.');
        }

        public static string AssetFolder(string assetPath)
        {
            int lastIndex = assetPath.LastIndexOf('/');
            lastIndex = (lastIndex >= 0) ? lastIndex : assetPath.Length;
            return assetPath.Substring(0, lastIndex);
        }

        /* This just removes "Assets/" from the beginning of the path,
         * if it's present.
         * This does not convert absolute path to Asset relative path !
         */
        public static string AssetRelPath(string relPath)
        {
            if (relPath.StartsWith("Assets"))
            {
                return relPath.Substring("Assets".Length).TrimStart('/');
            }
            return relPath;
        }

        private string saveDirPath;

        public MyyAssetManager(string path = "")
        {
            SetPath(path);
        }

        public void SetPath(string newPath)
        {
            saveDirPath = ("Assets/" + newPath).Trim(' ', '/');
        }

        public string SavePath(string relativePath)
        {
            return AssetPath(relativePath).Substring("Assets/".Length);
        }

        public MyyAssetManager AssetManagerFrom(string newSavePath)
        {
            return new MyyAssetManager(SavePath(newSavePath));
        }

        public string AssetPath(string relativeFilePath)
        {
            return saveDirPath + "/" + relativeFilePath;
        }

        public string MkDir(string dirName)
        {
            if (AssetDatabase.CreateFolder(saveDirPath, dirName) != "")
            {
                return SavePath(dirName);
            }

            return "";

        }

        public void GenerateAsset(UnityEngine.Object o, string relativePath)
        {
            AssetDatabase.CreateAsset(o, AssetPath(relativePath));
        }

        public bool GenerateAssetCopy(UnityEngine.Object o, string newFileRelativePath)
        {
            string oldPath = AssetDatabase.GetAssetPath(o);
            string newPath = AssetPath(newFileRelativePath);
            MyyLogger.Log("Copying {0} to {1}", oldPath, newPath);
            if (oldPath == "")
            {
                MyyLogger.LogError(
                    "BROKEN ASSET ! {0} ({1}).\n" +
                    "GetAssetPath returned an empty string !", o, o.GetType().Name);
            }
            return AssetDatabase.CopyAsset(oldPath, newPath);
        }

        public T AssetGet<T>(string relativePath) where T : UnityEngine.Object
        {
            return AssetDatabase.LoadAssetAtPath<T>(AssetPath(relativePath));
        }

        public AnimatorController GenerateAnimController(string relativePath)
        {
            return AnimatorController.CreateAnimatorControllerAtPath(
                AssetPath(relativePath + ".controller"));
        }

        public AnimatorController ControllerBackup(
            AnimatorController controller, string newName)
        {
            string newFileName = newName + ".controller";
            GenerateAssetCopy(controller, newFileName);
            return AssetDatabase.LoadAssetAtPath<AnimatorController>(AssetPath(newFileName));
        }

        public bool CanAccessSavePath()
        {
            return AssetDatabase.IsValidFolder(AssetPath("").Trim(' ', '/'));
        }

        public T ScriptAssetCopyOrCreate<T> (
            T o,
            string copyName,
            T newO = null)
        where T : UnityEngine.ScriptableObject, new()
        {
            if (newO == null) newO = ScriptableObject.CreateInstance<T>();
            string copyFileName = FilesystemFriendlyName(copyName);
            T rex;
            if (o != null)
            {
                GenerateAssetCopy(o, copyFileName);
                var res = AssetGet<T>(copyFileName);
                rex = res;
            }
            else
            {
                GenerateAsset(newO, copyFileName);
                rex = newO;
            }
            return rex;
        }

    }
}

#endif