#if UNITY_EDITOR

using UnityEngine;
using UnityEditor;
using System;
using UnityEditor.Animations;

namespace Myy
{
    /**
     * <summary>Utility class to manage assets from a specific directory.</summary>
     */
    public class MyyAssetsManager
    {

        private string saveDirPath;

        /**
         * <summary>Convert the provided name to a useable filename.</summary>
         * 
         * <remarks>This mostly remove all characters deemed "invalid" in filenames.</remarks>
         * 
         * <param name="name">The name to convert</param>
         * 
         * <returns>The provided name with invalid characters replaced by underscores</returns>
         */
        public static string FilesystemFriendlyName(string name)
        {
            var invalids = System.IO.Path.GetInvalidFileNameChars();
            return string.Join("_", name.Split(invalids, StringSplitOptions.RemoveEmptyEntries)).TrimEnd('.');
        }

        /**
         * <summary>Just returns the dirpath of the provided asset filepath.</summary>
         * 
         * <param name="assetPath">The filepath of the asset</param>
         * 
         * <returns>The dirpath of the asset</returns>
         */
        public static string AssetFolder(string assetPath)
        {
            int lastIndex = assetPath.LastIndexOf('/');
            lastIndex = (lastIndex >= 0) ? lastIndex : 0;
            return assetPath.Substring(0, lastIndex);
        }

        /**
         * <summary>Provides the path of the asset, relative to the Assets folder.</summary>
         * 
         * <remarks>This just trim "Assets/" from the filepath.</remarks>
         * 
         * <param name="relPath">The asset filepath, relative to the Unity project</param>
         * 
         * <returns>The asset filepath, relative to the Assets/ directory.</returns>
         */
        public static string DirPathFromAssets(string relPath)
        {
            if (relPath.StartsWith("Assets"))
            {
                return relPath.Substring("Assets".Length).TrimStart('/');
            }
            return relPath;
        }

        /**
         * <summary>
         * Construct an asset manager, for easier management of assets from
         * a specific folder.
         * </summary>
         * 
         * <remarks>
         * The path is relative to the Assets/ folder.
         * </remarks>
         * 
         * <param name="path">(Optional) Dirpath of the assets, relative to Assets/ folder</param>
         * 
         */
        public MyyAssetsManager(string path = "")
        {
            SetPath(path);
        }

        /**
         * <summary>Set the path of the assets, relative to the Assets/ directory.</summary>
         * 
         * <param name="newPath">
         * New path of the assets, relative to the Assets/ directory.
         * </param>
         */
        public void SetPath(string newPath)
        {
            saveDirPath = ("Assets/" + DirPathFromAssets(newPath)).Trim(' ', '/');
        }

        /**
         * <summary>Convert an AssetsManager relative filepath to Unity Assets/
         * directory relative filepath.</summary>
         * 
         * <param name="relativePath">Filepath relative to the AssetsManager save path</param>
         * 
         * <returns>The same filepath, relative to the Unity "Assets/" directory.</returns>
         */
        public string SavePath(string relativePath)
        {
            return DirPathFromAssets(AssetPath(relativePath));
        }

        /**
         * <summary>
         * Convert an AssetsManager filepath to a Unity project relative filepath.
         * </summary>
         * 
         * <param name="relativeFilePath">AssetsManager relative filepath to convert</param>
         * 
         * <returns>The filepath relative to the Unity project folder.</returns>
         */
        public string AssetPath(string relativeFilePath)
        {
            return saveDirPath + "/" + relativeFilePath;
        }

        /**
         * <summary>Create a directory, from the AssetsManager current directory.</summary>
         * 
         * <param name="dirName">The dirPath to create</param>
         * 
         * <returns>
         * On success, the path of the folder, relative to the Unity Assets/ directory.
         * On failure, an empty string.
         * </returns>
         */
        public string MkDir(string dirName)
        {
            if (AssetDatabase.CreateFolder(saveDirPath, dirName) != "")
            {
                return SavePath(dirName);
            }

            return "";

        }

        /**
         * <summary>
         * Generate an asset, from the provided object, at the provided
         * relative path.
         * </summary>
         * 
         * <remarks>
         * <para>
         * The provided filepath is considered to be relative to the
         * AssetsManager.
         * </para>
         * 
         * <para>
         * This just call AssetsDatabase.CreateAsset, which doesn't return
         * anything on success. Also, this function will throw the
         * same exceptions as CreateAsset, though.
         * </para>
         * </remarks>
         * 
         * <param name="o">The object to generate an asset file from.</param>
         * <param name="relativePath">The desired relative filepath for the generated asset.</param>
         */
        public void GenerateAsset(UnityEngine.Object o, string relativePath)
        {
            AssetDatabase.CreateAsset(o, AssetPath(relativePath));
        }

        /**
         * <summary> Copy a previously saved asset.</summary>
         * 
         * <param name="o">
         * The object to copy, from which the asset file will be copied.
         * </param>
         * 
         * <param name="newFileRelativePath">
         * The desired relative filepath for the copied asset.
         * </param>
         * 
         * <returns>
         * True if the copy was performed successfully.
         * False otherwise.
         * </returns>
         */
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
                return false;
            }
            return AssetDatabase.CopyAsset(oldPath, newPath);
        }

        /**
         * <summary>Returns the object represented by the provided asset file.</summary>
         * 
         * <param name="relativePath">The asset filepath, relative to the assets manager</param>
         * 
         * <returns>The object represented by the provided asset file.</returns>
         */
        public T AssetGet<T>(string relativePath) where T : UnityEngine.Object
        {
            return AssetDatabase.LoadAssetAtPath<T>(AssetPath(relativePath));
        }

        /**
         * <summary>Create a new Animator Controller asset.</summary>
         * 
         * <remarks>
         * This is just a wrapper around
         * AnimatorController.CreateAnimatorControllerAtPath
         * </remarks>
         * 
         * <param name="relativePath">
         * The desired relative filepath for the animator controller generated
         * </param>
         * 
         * <returns>
         * The generated AnimatorController.
         * </returns>
         */
        public AnimatorController GenerateAnimController(string relativePath)
        {
            return AnimatorController.CreateAnimatorControllerAtPath(
                AssetPath(relativePath + ".controller"));
        }

        /**
         * <summary>Copy an Animator Controller and save it as an asset.</summary>
         * 
         * <param name="controller">
         * The animator controller to copy
         * </param>
         * 
         * <param name="newName">
         * The desired relative new asset filepath for the copied controller
         * </param>
         * 
         * <returns>
         * The instantiation of the AnimatorController copy asset file.
         * </returns>
         */
        public AnimatorController ControllerBackup(
            AnimatorController controller, string newName)
        {
            /* FIXME
             * This parameter should be named newFilePath actually, and
             * contain the '.controller' extension.
             */
            string newFileName = newName + ".controller";
            GenerateAssetCopy(controller, newFileName);
            return AssetDatabase.LoadAssetAtPath<AnimatorController>(AssetPath(newFileName));
        }

        /**
         * <summary>Check if the current assets manager save dirpath is valid.</summary>
         * 
         * <returns>
         * True if the directory exists.
         * False otherwise.
         * </returns>
         */
        public bool CanAccessSavePath()
        {
            
            return AssetDatabase.IsValidFolder(AssetPath("").Trim(' ', '/'));
        }

        /**
         * <summary>
         * Create a new asset file for the provided ScriptableObject, or copy the
         * existing one if it already has one.
         * </summary>
         * 
         * <param name="o">The ScriptableObject to save or copy</param>
         * <param name="copyName">The desired filename for the generated asset file</param>
         * 
         * <returns>
         * A ScriptableObject instantiated from the generated asset file.
         * </returns>
         */
        public T ScriptAssetCopyOrCreate<T>(
            T o,
            string copyName)
        where T : UnityEngine.ScriptableObject, new()
        {
            string copyFileName = FilesystemFriendlyName(copyName);
            
            if (o == null)
            {
                o = ScriptableObject.CreateInstance<T>();
                GenerateAsset(o, copyFileName);
            }
            else
            {
                GenerateAssetCopy(o, copyFileName);

            }
            T rex = AssetGet<T>(copyFileName);
            return rex;
        }

    }
}
#endif