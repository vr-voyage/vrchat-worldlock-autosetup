using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace Myy
{


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

    public class SimpleEditorUI : List<SimpleUIElement>
    {
        public readonly SerializedObject serialO;

        public SimpleEditorUI(SerializedObject serialObject)
        {
            serialO = serialObject;
        }

        public SimpleEditorUI(UnityEngine.Object unityObject)
        {
            serialO = new SerializedObject(unityObject);
        }

        /* TODO Factorize this */
        public SimpleEditorUI(
            UnityEngine.Object unityObject,
            params (string label, string propertyName, Func<SerializedProperty, bool> checkFunc)[] fields)
        {
            serialO = new SerializedObject(unityObject);
            Add(fields);
        }

        public void Add(params (string label, string propertyName, Func<SerializedProperty, bool> checkFunc)[] fields)
        {
            foreach (var (label, propertyName, checkFunc) in fields)
            {
                Add(new SimpleUIElement() { property = serialO.FindProperty(propertyName), label = label, Check = checkFunc });
            }
        }

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