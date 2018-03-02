// MIT License
// 
// Unity FSM Code Generator - github.com/justonia/UnityFSMCodeGenerator
//
// Copyright (c) 2018 Justin Larrabee
// 
// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files (the "Software"), to deal
// in the Software without restriction, including without limitation the rights
// to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
// copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions:
// 
// The above copyright notice and this permission notice shall be included in all
// copies or substantial portions of the Software.
// 
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
// SOFTWARE.

#if PLAYMAKER
using UnityEngine;
using UnityEditor;
using HutongGames.PlayMaker.Actions;
using HutongGames.PlayMakerEditor;
using System.Reflection;
using HutongGames.PlayMaker;
using UnityFSMCodeGenerator.Actions;

namespace UnityFSMCodeGenerator.Editor
{
    public abstract class BaseFsmActionEditor : CustomActionEditor
    {
        private bool editFieldChanged = false;
        private FsmString currentEditField;

        public bool EditEventField(string label, Fsm fsm, FsmString _event)
        {
            editFieldChanged = false;
            currentEditField = _event;

            EditorGUILayout.BeginHorizontal();
            {
                FsmEditorGUILayout.PrefixLabel(label);

                var buttonRect = GUILayoutUtility.GetRect(GUIContent.none, EditorStyles.popup);
                var currentEventName =  _event != null ? _event.Value : "";

                if (GUI.Button(buttonRect, currentEventName, EditorStyles.popup))
                {
                    FsmEditorGUILayout.GenerateEventSelectionMenu(
                        fsm, FindEvent(currentEventName), OnSelect, OnNew).DropDown(buttonRect);
                }
            }

            EditorGUILayout.EndHorizontal();

            return editFieldChanged;
        }

        private FsmEvent FindEvent(string name)
        {
            foreach (var evt in FsmEvent.EventList) {
                if (evt.Name == name) {
                    return evt;
                }
            }
            return null;
        }

        private void OnSelect(object _event)
        {
            editFieldChanged = true;

            var chosenEvent = _event as FsmEvent;
            if (chosenEvent == null) {
                currentEditField.Value = "";
                return;
            }

            currentEditField.Value = chosenEvent.Name;
        }

        private void OnNew()
        {
            // TODO: ?
        }
    }

    public class BaseDelegateActionEditor : BaseFsmActionEditor
    {
        private bool isDirty = false;

        private TypeCache.IFsmActionDelegateInfo DelegateInfo {
            get {
                var action = target as BaseDelegateAction;
                return action.delegateInterface != null
                    ? TypeCache.GetIFsmActionDelegateInfoByFullName(action.delegateInterface.Value) 
                    : null;
            }
        }

        public override bool OnGUI()
        {
            // If you need to reference the action directly:
            var action = target as BaseDelegateAction;
            var delegateInfo = action.delegateInterface != null ? TypeCache.GetIFsmActionDelegateInfoByFullName(action.delegateInterface.Value) : null;

            isDirty = false;

            //
            // TODO: Show errors when these values don't exist anymore
            //

            // Interface choice
            EditorGUILayout.BeginHorizontal();
            {
                FsmEditorGUILayout.PrefixLabel("Delegate");

                var buttonRect = GUILayoutUtility.GetRect(GUIContent.none, EditorStyles.popup);
                var interfaceName = delegateInfo != null ? delegateInfo.type.Name : "";

                if (GUI.Button(buttonRect, interfaceName, EditorStyles.popup))
                {
                    GetDelegateMenu(delegateInfo != null ? delegateInfo.choiceIndex : 0).DropDown(buttonRect);
                    delegateInfo = DelegateInfo;
                }
            }
            EditorGUILayout.EndHorizontal();

            FsmEditorGUILayout.ReadonlyTextField(action.delegateInterface != null ? action.delegateInterface.Value : "");

            //FsmEditorGUILayout.LightDivider();

            // Method choice
            EditorGUILayout.BeginHorizontal();
            {
                var enabledBefore = GUI.enabled;
                GUI.enabled = GUI.enabled && delegateInfo != null;
                
                FsmEditorGUILayout.PrefixLabel("Method");

                var buttonRect = GUILayoutUtility.GetRect(GUIContent.none, EditorStyles.popup);
                
                if (GUI.Button(buttonRect, GetMethodName(), EditorStyles.popup))
                {
                    GetMethodMenu().DropDown(buttonRect);
                }

                GUI.enabled = enabledBefore;
            }
            EditorGUILayout.EndHorizontal();

            FsmEditorGUILayout.ReadonlyTextField(action.methodSignature != null ? action.methodSignature.Value : "");

            return GUI.changed || isDirty;
        }

        private string GetMethodName()
        {
            var action = target as BaseDelegateAction;
            if (action == null || action.methodSignature == null) {
                return "";
            }
            
            var info = DelegateInfo;
            System.Reflection.MethodInfo methodInfo;
            if (info == null || !info.methodSignatureToInfo.TryGetValue(action.methodSignature.Value, out methodInfo)) {
                return "";
            }

            return methodInfo.Name;
        }

        private GenericMenu GetMethodMenu()
        {
            var choices = DelegateInfo.methodChoices;

            var menu = new GenericMenu();
            for (int i = 0; i < choices.Length; i++) {
                menu.AddItem(new GUIContent(choices[i]), false, OnMethodMenuSelected, choices[i]);
            }

            return menu;
        }

        private void OnMethodMenuSelected(object _method)
        {
            var action = target as BaseDelegateAction;
            var info = DelegateInfo;

            System.Reflection.MethodInfo methodInfo;
            if (!info.methodSignatureToInfo.TryGetValue(_method as string, out methodInfo)) {
                action.methodSignature = null;
                //action.methodName = null;
                isDirty = true;
                return;
            }

            if (action.methodSignature != null) {
                System.Reflection.MethodInfo currentMethodInfo;
                if (info.methodSignatureToInfo.TryGetValue(action.methodSignature.Value, out currentMethodInfo) && currentMethodInfo == methodInfo) {
                    return;
                }
            }

            action.methodSignature = _method as string;
        }

        private GenericMenu GetDelegateMenu(int selected)
        {
            var choices = TypeCache.GetFsmActionDelegates();

            var menu = new GenericMenu();
            for (int i = 0; i < choices.Length; i++) {
                menu.AddItem(new GUIContent(choices[i].choiceName), i == choices[i].choiceIndex, OnDelegateMenuSelected, choices[i]);
            }

            return menu;
        }

        private void OnDelegateMenuSelected(object _info)
        {
            var info = _info as TypeCache.IFsmActionDelegateInfo;
            if (info == DelegateInfo) {
                return;
            }

            var action = target as BaseDelegateAction;
            action.delegateInterface = info.type.FullName;

            if (action.methodSignature != null && !info.methodSignatureToInfo.ContainsKey(action.methodSignature.Value)) {
                action.methodSignature = null;
            }

            isDirty = true;
        }
    }
}
#endif
