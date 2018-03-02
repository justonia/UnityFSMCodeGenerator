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

using UnityEngine;
using UnityEditor;
using System;
using System.IO;
using System.Linq;
using System.Collections;
using System.Collections.Generic;

namespace UnityFSMCodeGenerator.Editor
{
    [CustomEditor(typeof(FsmViewer))]
    public class FsmViewerEditor : UnityEditor.Editor
    {
        private static GUILayoutOption labelWidth = GUILayout.Width(100);
        private static GUILayoutOption buttonMaxWidth = GUILayout.MaxWidth(150);
        private static GUILayoutOption objectMaxWidth = GUILayout.MaxWidth(250);
        private static GUILayoutOption typeMinWidth = GUILayout.MinWidth(150);

        static FsmViewerEditor()
        {
            // TODO: This only works in 2017.2 or later... need fallback
            EditorApplication.playModeStateChanged += (PlayModeStateChange state) => {
                if (state != PlayModeStateChange.ExitingPlayMode) {
                    return;
                }

                // Prevent being stuck in lock mode on a runtime FSM exiting play mode
                var activeFsm =  HutongGames.PlayMakerEditor.FsmEditor.SelectedFsmComponent;
                if (activeFsm != null && activeFsm.GetComponent<FsmViewerPrefabInstance>() != null) {
                    HutongGames.PlayMakerEditor.FsmEditorSettings.LockGraphView = false;
                    HutongGames.PlayMakerEditor.FsmEditor.SelectNone();
                }
            };
        }

        private void OnEnable()
        {
            if (Application.isPlaying) {
                (target as FsmViewer).WantRepaint += this.Repaint;
            }
        }

        private void OnDisable()
        {
            if (Application.isPlaying) {
                (target as FsmViewer).WantRepaint -= this.Repaint;
            }
        }

        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();

            var link = target as FsmViewer;

            if (!Application.isPlaying) {
                return;
            }

            foreach (var pair in link.tracking) {
                if (pair.targetFsm == null || pair.fsmOwner == null) {
                    continue;
                }

                EditorGUILayout.Space();

                EditorGUILayout.LabelField(pair.targetFsm.GetType().Name, EditorStyles.boldLabel, typeMinWidth);


                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.LabelField("Active State:", labelWidth);
                EditorGUILayout.LabelField(pair.fsmDebug.State, EditorStyles.boldLabel);
                EditorGUILayout.EndHorizontal();

                #if PLAYMAKER
                var activeFsm =  HutongGames.PlayMakerEditor.FsmEditor.SelectedFsmComponent;
                
                if (pair.view != null && activeFsm != pair.view) {
                    link.DestroyView(pair);
                }

                if (pair.view != null) {
                    EditorGUILayout.BeginHorizontal();

                    EditorGUILayout.LabelField("Graph View:", labelWidth);

                    if (GUILayout.Button("Close", buttonMaxWidth)) {
                        HutongGames.PlayMakerEditor.FsmEditorSettings.LockGraphView = false;
                        HutongGames.PlayMakerEditor.FsmEditor.SelectNone();
                        link.DestroyView(pair);
                    }

                    EditorGUILayout.EndHorizontal();
                }
                else if (link.CanShowView(pair)) {
                    EditorGUILayout.BeginHorizontal();

                    EditorGUILayout.LabelField("Graph View:", labelWidth);
                    if (GUILayout.Button("View", buttonMaxWidth)) {
                        link.ShowView(pair);
                        if (pair.view != null) {
                            var selectionBefore = Selection.objects;
                            HutongGames.PlayMakerEditor.FsmEditor.SelectFsm(pair.view.Fsm);
                            HutongGames.PlayMakerEditor.FsmEditorSettings.LockGraphView = true;
                            Selection.objects = selectionBefore;
                        }
                    }
                    
                    EditorGUILayout.EndHorizontal();
                }
                #endif
                
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.LabelField("Owner:", labelWidth);
                GUI.enabled = false;
                EditorGUILayout.ObjectField(pair.fsmOwner, typeof(MonoBehaviour), true, objectMaxWidth);
                GUI.enabled = true;
                EditorGUILayout.EndHorizontal();


            }
        }
    }
}
