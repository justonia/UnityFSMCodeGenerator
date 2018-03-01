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
    [CustomActionEditor(typeof(OnEntryAction))]
    public class OnEntryActionEditor : BaseDelegateActionEditor
    {
    }

    [CustomActionEditor(typeof(OnExitAction))]
    public class OnExitActionEditor : BaseDelegateActionEditor
    {
    }

    [CustomActionEditor(typeof(InternalAction))]
    public class InternalActionEditor : BaseDelegateActionEditor
    {
        private bool isDirty;

        public override bool OnGUI()
        {
            var action = target as InternalAction;
            isDirty = false;

            //EditField("_event");
            EditorGUILayout.BeginHorizontal();
            {
                FsmEditorGUILayout.PrefixLabel("Event");

                var buttonRect = GUILayoutUtility.GetRect(GUIContent.none, EditorStyles.popup);
                var currentEventName =  action._event != null ? action._event.Value : "";

                if (GUI.Button(buttonRect, currentEventName, EditorStyles.popup))
                {
                    FsmEditorGUILayout.GenerateEventSelectionMenu(action.Fsm, FindEvent(currentEventName), OnSelect, OnNew).DropDown(buttonRect);
                    //action._event.Value = FsmEditorGUILayout.FsmEventListPopup();
                    //action._event.Value = FsmEditorGUILayout.FsmEventPopup(action.Owner, action.
                }
            }
            EditorGUILayout.EndHorizontal();

            FsmEditorGUILayout.LightDivider();

            return base.OnGUI() || isDirty;
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
            var chosenEvent = _event as FsmEvent;

            if (chosenEvent == null || chosenEvent.Name != (target as InternalAction)._event.Value) {
                (target as InternalAction)._event.Value = chosenEvent.Name;
                isDirty = true;
            }
        }

        private void OnNew()
        {
        }
    }
}
#endif
