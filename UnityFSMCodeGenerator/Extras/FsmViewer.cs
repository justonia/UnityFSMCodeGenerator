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

using System;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
#if UNITY_EDITOR
using UnityEditor;
#endif
#if PLAYMAKER
using HutongGames.PlayMaker;
#endif

namespace UnityFSMCodeGenerator
{
    // Stick this class on a GameObject and drag references to a MonoBehaviour 
    // implementing IHaveBaseFsm. If PlayMaker is installed and you link the 
    // prefab that generated the FSM into the tracking entries, you will be able
    // to view the FSM in the PlayMaker UI as well.
    public class FsmViewer : MonoBehaviour
    {
        private bool isDirty;
        private List<TrackingPair> tracking = new List<TrackingPair>();

        public class TrackingPair
        {
            public PlayMakerCodeGenerator fsmPrefab;
            public MonoBehaviour fsmOwner;
            public BaseFsm targetFsm;
            public IFsmIntrospectionSupport fsmIntrospection;
            public IFsmDebugSupport fsmDebug;
            #if PLAYMAKER
            public PlayMakerFSM view;
            public bool settingPlayMakerBreakpoint;
            public HashSet<FsmState> currentEnterBreakpoints = new HashSet<FsmState>();
            public HashSet<FsmState> wantEnterBreakpoints = new HashSet<FsmState>();
            #endif
        }

        public List<TrackingPair> Tracking { get { return tracking; }}

        public delegate void RepaintAction();
        public event RepaintAction WantRepaint;

        private void OnEnable()
        {
            // Don't want this crap to run unless we're in the editor
            #if UNITY_EDITOR
            foreach (var pair in tracking) {
                if (pair.fsmDebug != null) {
                    pair.fsmDebug.OnBreakpointHit -= OnFsmBreakpointHit;
                    pair.fsmDebug.OnBreakpointSet -= OnFsmBreakpointSet;
                    pair.fsmDebug.OnBreakpointsReset -= OnFsmBreakpointsReset;
                }
            }
            tracking.Clear();
            DiscoverTrackingPairs();

            foreach (var pair in tracking) {
                if (pair.fsmOwner == null || !(pair.fsmOwner is IHaveBaseFsm)) {
                    continue;
                }
                StartCoroutine(Track(pair));
            }
            #endif
        }

        private void DiscoverTrackingPairs()
        {
            #if UNITY_EDITOR
            // Discover all FSMs on this game object
            var owners = GetComponents<MonoBehaviour>().Where(b => b is IHaveBaseFsm).ToList();

            foreach (var owner in owners) {
                var fsms = (owner as IHaveBaseFsm).BaseFsms;
                if (fsms == null) {
                    continue;
                }

                foreach (var fsm in fsms) {
                    var pair = new TrackingPair{
                        fsmOwner = owner,
                        targetFsm = fsm,
                        fsmIntrospection = fsm as IFsmIntrospectionSupport,
                        fsmDebug = fsm as IFsmDebugSupport,
                    };

                    if (pair.fsmIntrospection == null) {
                        continue;
                    }

                    if (pair.fsmDebug != null) {
                        pair.fsmDebug.OnBreakpointHit += OnFsmBreakpointHit;
                        pair.fsmDebug.OnBreakpointSet += OnFsmBreakpointSet;
                        pair.fsmDebug.OnBreakpointsReset += OnFsmBreakpointsReset;
                    }

                    tracking.Add(pair);
                }
            }

            // Collect all prefab FSMs
            var prefabs = new Dictionary<string, PlayMakerCodeGenerator>();
            foreach (var guid in tracking.Select(p => p.fsmIntrospection.GeneratedFromPrefabGUID)) {
                if (string.IsNullOrEmpty(guid)) {
                    continue;
                }
                
                var path = AssetDatabase.GUIDToAssetPath(guid);
                if (string.IsNullOrEmpty(path)) {
                    continue;
                }

                var gen = AssetDatabase.LoadAssetAtPath(path, typeof(PlayMakerCodeGenerator));
                if (gen == null) {
                    continue;
                }

                prefabs[guid] = gen as PlayMakerCodeGenerator;
            }


            foreach (var pair in tracking) {
                var guid = pair.fsmIntrospection.GeneratedFromPrefabGUID;

                if (string.IsNullOrEmpty(guid)) {
                    continue;
                }

                prefabs.TryGetValue(guid, out pair.fsmPrefab);
            }
            #endif
        }

        private void OnDisable()
        {
            #if UNITY_EDITOR
            StopAllCoroutines();
            foreach (var pair in tracking) {
                DestroyView(pair);
                pair.targetFsm = null;
                pair.fsmIntrospection = null;
            }
            #endif
        }

        #if UNITY_EDITOR
        private void Update()
        {
            if (isDirty && WantRepaint != null && Time.frameCount % 5 == 0) {
                WantRepaint();
                isDirty = false;
            }
        }
        #endif

        private IEnumerator Track(TrackingPair pair)
        {
            while (true) {
                var currentFsmState = pair.fsmIntrospection.State;
                
                #if PLAYMAKER
                if (pair.view != null) { 
                    pair.view.SetState(currentFsmState);
                }
                #endif

                while (currentFsmState == pair.fsmIntrospection.State) {
                    #if PLAYMAKER
                    if (pair.view != null) {
                        UpdatePlayMakerBreakpoints(pair);
                        pair.view.Fsm.Update();
                    }
                    #endif
                    yield return null;
                }
            }
        }

        #if PLAYMAKER
        private HashSet<FsmState> tempBreakpoints = new HashSet<FsmState>();

        private void UpdatePlayMakerBreakpoints(TrackingPair pair)
        {
            tempBreakpoints.Clear();

            pair.wantEnterBreakpoints.Clear(); 
            foreach (var state in pair.view.FsmStates) {
                if (state.IsBreakpoint) {
                    pair.wantEnterBreakpoints.Add(state);
                }
            }
             
            // Find and remove new/removed breakpoints
            bool isDirty = false; 
            foreach (var breakpoint in pair.wantEnterBreakpoints) {
                if (!pair.currentEnterBreakpoints.Contains(breakpoint)) {
                    tempBreakpoints.Add(breakpoint);
                    isDirty = true; 
                }
            }

            foreach (var breakpoint in pair.currentEnterBreakpoints) {
                if (pair.wantEnterBreakpoints.Contains(breakpoint)) {
                    tempBreakpoints.Add(breakpoint);
                }
                else {
                    isDirty = true;
                }
            }

            if (isDirty) {
                var b = pair.currentEnterBreakpoints;
                pair.currentEnterBreakpoints = tempBreakpoints;
                tempBreakpoints = b;

                if (pair.fsmDebug == null) {
                    Debug.LogWarningFormat("Cannot set breakpoint on {0}, IFsmDebugSupport was not enabled in compiler options", pair.targetFsm.GetType().Name);
                }
                else {
                    pair.settingPlayMakerBreakpoint = true;
                    pair.fsmDebug.ResetBreakpoints();
                    foreach (var breakpoint in pair.currentEnterBreakpoints) {
                        pair.fsmDebug.SetOnEnterBreakpoint(
                            pair.fsmIntrospection.EnumStateFromString(breakpoint.Name));
                    }
                    pair.settingPlayMakerBreakpoint = false;
                }
                isDirty = false;
            }
        }
        #endif

        private void OnFsmBreakpointHit(BaseFsm fsm, object state)
        {
            #if PLAYMAKER
            var pair = GetTrackingPair(fsm);
            if (pair == null || pair.view == null) {
                return;
            }

            // Force update UI
            if (pair.view != null) { 
                var currentFsmState = pair.fsmIntrospection.State;
                pair.view.SetState(currentFsmState);
                pair.view.Fsm.Update();
                if (WantRepaint != null) {
                    WantRepaint();
                }
            }
            #endif
        }

        private void OnFsmBreakpointSet(BaseFsm fsm, object state)
        {
            #if PLAYMAKER
            var pair = GetTrackingPair(fsm);
            if (pair == null || pair.view == null) {
                return;
            }

            // Been set from code so update PlayMaker 
            if (!pair.settingPlayMakerBreakpoint) {
                var targetStateName = pair.fsmIntrospection.StateFromEnumState(state);
                
                foreach (var fsmState in pair.view.FsmStates) {
                    if (fsmState.Name == targetStateName) {
                        pair.currentEnterBreakpoints.Add(fsmState);
                        fsmState.IsBreakpoint = true;
                    }
                }
            }
            #endif

            if (WantRepaint != null) {
                WantRepaint();
            }
        }
        
        private void OnFsmBreakpointsReset(BaseFsm fsm)
        {
            #if PLAYMAKER
            var pair = GetTrackingPair(fsm);
            if (pair == null || pair.view == null) {
                return;
            }

            // Been set from code so update PlayMaker 
            if (!pair.settingPlayMakerBreakpoint) {
                foreach (var state in pair.view.FsmStates) {
                    state.IsBreakpoint = false;
                }

                if (WantRepaint != null) {
                    WantRepaint();
                }
            }
            #endif

        }

        private TrackingPair GetTrackingPair(BaseFsm fsm)
        {
            if (tracking == null) {
                return null;
            }

            foreach (var pair in tracking) {
                if (pair.targetFsm == fsm) {
                    return pair;
                }
            }

            return null;
        }

        public bool CanShowView(TrackingPair pair)
        {
            #if UNITY_EDITOR && PLAYMAKER
            return pair.targetFsm != null && pair.fsmPrefab != null && pair.fsmIntrospection != null;
            #else
            return false;
            #endif
        }

        public void ShowView(TrackingPair pair)
        {
            #if PLAYMAKER
            if (pair.view != null || !CanShowView(pair)) {
                return;
            }

            var go = Instantiate(pair.fsmPrefab.gameObject);
            go.transform.parent = transform;
            var fsm = go.GetComponent<PlayMakerFSM>();
            if (fsm == null) {
                Debug.LogFormat(gameObject, "fsmPrefab does not have a PlayMakerFSM on it");
                Destroy(go);
                return;
            }

            fsm.Fsm.ManualUpdate = true;

            // Sanity check that states line up
            var fsmStates = fsm.FsmStates.Select(s => s.Name).ToList();
            var intersectingStates = pair.fsmIntrospection.AllStates.Intersect(fsmStates).ToList();

            if (fsmStates.Count != pair.fsmIntrospection.AllStates.Count || intersectingStates.Count != pair.fsmIntrospection.AllStates.Count) {
                Debug.LogErrorFormat(gameObject, "Generated FSM and PlayMakerFSM do not have the same set of states");
                Destroy(go);
                return;
            }

            go.AddComponent<FsmViewerPrefabInstance>();

            pair.view = fsm;
            #endif
        }

        public void DestroyView(TrackingPair pair)
        {
            #if PLAYMAKER
            if (pair.view == null) {
                return;
            }

            Destroy(pair.view.gameObject);
            #endif
        }
    }
}
