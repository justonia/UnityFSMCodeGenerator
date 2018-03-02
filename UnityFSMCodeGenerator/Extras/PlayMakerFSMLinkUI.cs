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
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace UnityFSMCodeGenerator
{
    // Stick this class on a GameObject that has a MonoBehaviour implementing IHaveBaseFsm.
    // Drag the prefab used to generate your FSM into the 'fsmPrefab' field and when you
    // click start in the inspector it will spawn an instance of that prefab and manually
    // update the PlayMaker UI to show state transitions of your generated FSM. Cool!
    public class PlayMakerFSMLinkUI : MonoBehaviour
    {
        #if UNITY_EDITOR
        private PlayMakerFSM fsm;
        private BaseFsm generatedFsm;
        private IFsmDebugSupport fsmDebug;
        private string currentFsmState;
        private Coroutine update;
        #endif

        public PlayMakerCodeGenerator fsmPrefab;

        public PlayMakerFSM Fsm;
        public BaseFsm TrackingFsm { get; private set; }

        // The expectation is that when you call Start() with a provided FSM that you never
        // change the IContext bound to the FSM.
        public void Track()
        {
            #if UNITY_EDITOR
            var components = GetComponents<MonoBehaviour>();
            IHaveBaseFsm getFsm = null; 
            foreach (var c in components) {
                if (c is IHaveBaseFsm) {
                    getFsm = c as IHaveBaseFsm;
                    break;
                }
            }

            if (getFsm == null) {
                Debug.LogError("PlayMakerFSMLinkUI: could not find a behaviour on this GameObject that implements IHaveBaseFsm");
                return;
            }

            var generatedFsm = getFsm.BaseFsm;
            if (generatedFsm == null || fsm != null || fsmPrefab == null) {
                return;
            }

            fsmDebug = generatedFsm as IFsmDebugSupport;
            if (fsmDebug == null) {
                Debug.LogErrorFormat(gameObject, "Generated FSM of type '{0}' does not implement IFsmDebugSupport", generatedFsm.GetType().Name);
                return;
            }

            var go = Instantiate(fsmPrefab);
            go.transform.parent = transform;
            fsm = go.GetComponent<PlayMakerFSM>();
            if (fsm == null) {
                Debug.LogFormat(gameObject, "fsmPrefab does not have a PlayMakerFSM on it");
                Destroy(go);
                return;
            }

            fsm.Fsm.ManualUpdate = true;

            // Sanity check that states line up
            var fsmStates = fsm.FsmStates.Select(s => s.Name).ToList();
            var intersectingStates = fsmDebug.AllStates.Intersect(fsmStates).ToList();

            if (fsmStates.Count != fsmDebug.AllStates.Count || intersectingStates.Count != fsmDebug.AllStates.Count) {
                Debug.LogErrorFormat(gameObject, "Generated FSM and PlayMakerFSM do not have the same set of states");
                Destroy(go);
                return;
            }

            currentFsmState = null;

            TrackingFsm = generatedFsm;
            Fsm = fsm;

            update = StartCoroutine(RunUpdate());
            #endif
        }

        private void OnDisable()
        {
            if (fsm != null) {
                Destroy(fsm.gameObject);
                fsm = null;
            }
        }

        public void StopTracking()
        {
            #if UNITY_EDITOR
            if (update != null) {
                StopCoroutine(update);
                update = null;
            }

            if (fsm == null) {
                return;
            }

            Destroy(fsm.gameObject);
            #endif
        }

        #if UNITY_EDITOR
        private IEnumerator RunUpdate()
        {
            while (true) {
                var state = fsmDebug.State;
                fsm.SetState(state);
                currentFsmState = state;

                fsm.Fsm.Update();

                while (currentFsmState == fsmDebug.State) {
                    yield return null;
                    fsm.Fsm.Update();
                }
            }
        }
        #endif
    }
}
#endif
