using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace Myy
{

    /**
     * <summary>An element used in SimpleUI</summary>
     */
    public struct SimpleUIElement
    {
        public SerializedProperty property;
        public string label;
        public Func<SerializedProperty, bool> Check;

        public bool DrawAndCheck()
        {
            EditorGUILayout.PropertyField(
                property: property,
                label: new GUIContent(label),
                includeChildren: true);
            EditorGUILayout.Space(12);
            return Check == null || Check(property);
        }
    }

    /**
     * <summary>A quick way to setup an editor tool UI.</summary>
     * 
     * <remarks>
     * <para>
     * This is a quick-and-dirty way to build Unity editor UI, since
     * I'm getting tired of using C# for that.
     * </para>
     * <para>This should be renamed "BasicUI" I guess, since it's
     * extremely basic in its use.
     * </para>
     * </remarks>
     */
    public class SimpleEditorUI : List<SimpleUIElement>
    {
        public readonly SerializedObject serialO;

        /**
         * <summary>Generate a simple Unity Editor UI.
         * 
         * <example>
         * Example :
         * <code>
         * new SimpleEditorUI(this,
         *   ("Avatar to configure", "avatar",        CheckProvidedAvatar),
         *   ("Object to add",       "gameObject",    CheckProvidedObject),
         *   ("Generate animations", "generateAnims", null))
         * </code>
         * </example>
         * </summary>
         * 
         * <param name="unityObject">
         * The Unity Editor Window object to generate a SerializedObject from.
         * </param>
         * 
         * <param name="fields">
         * <para>The fields of the UI.</para>
         * <para>Each field is defined by :
         * <list type="bullet">
         * <item>its label,</item>
         * <item>the associated public property name,</item>
         * <item>and a check function.</item>
         * </list></para>
         * </param>
         */
        public SimpleEditorUI(
            UnityEngine.Object unityObject,
            params (string label, string propertyName, Func<SerializedProperty, bool> checkFunc)[] fields)
        {
            serialO = new SerializedObject(unityObject);
            Add(fields);
        }


        /**
         * <summary>Add additional fields to the UI.</summary>
         * 
         * <remarks>
         * Currently this system is incomplete, and there's no way to reorder the added fields.
         * </remarks>
         * 
         * <param name="fields">
         * The UI fields to add.
         * </param>
         */
        public void Add(params (string label, string propertyName, Func<SerializedProperty, bool> checkFunc)[] fields)
        {
            foreach (var (label, propertyName, checkFunc) in fields)
            {
                Add(new SimpleUIElement() { property = serialO.FindProperty(propertyName), label = label, Check = checkFunc });
            }
        }


        /**
         * <summary>Draw the UI.</summary>
         * 
         * <remarks>
         * It's up to the check function to output appropriate error messages
         * if anything wrong happen. The return value of this function is
         * mostly used to display a final button if every check passed.
         * </remarks>
         * 
         * <returns>
         * True if all the check functions returned true.
         * False otherwise.
         * </returns>
         */
        public bool DrawFields()
        {
            bool alright = true;
            serialO.Update();
            foreach (var field in this)
            {
                alright &= field.DrawAndCheck();
            }
            serialO.ApplyModifiedProperties();
            return alright;
        }
    }
}