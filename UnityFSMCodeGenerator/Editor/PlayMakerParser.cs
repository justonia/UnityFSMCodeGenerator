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
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using HutongGames.PlayMaker;
using Type=System.Type;
using MethodInfo=System.Reflection.MethodInfo;

namespace UnityFSMCodeGenerator.Editor
{
    // TODO: Move this to non-editor assembly? Requires modifying TypeCache and assembly discovery.
    // TODO: Ensure internal actions do not use same transition event as defined in a state transition. 
    // TODO: Ensure that state names and event names contain only [A-Za-z]
    // TODO: Scan all states' actions and ensure only this library's actions are present.
    public class PlayMakerParser
    {
        private ParserOptions options;

        public PlayMakerParser(ParserOptions options)
        {
            this.options = options;
        }

        public FsmModel CreateModel(PlayMakerFSM fsm)
        {
            var model = new FsmModel{
                description = fsm.FsmDescription,
                states = new List<FsmStateModel>(),
            };

            GetPrefabData(fsm, model);

            // Pre-populate state models.
            var stateLookup = new Dictionary<string, FsmStateModel>();
            foreach (var state in fsm.FsmStates) {
                // Cannot support duplicate state names, and PlayMaker's GUI does not allow this but gotta check.
                if (stateLookup.ContainsKey(state.Name)) {
                    throw new System.Exception("Duplicate state name: " + state.Name);
                }

                var stateModel = new FsmStateModel{
                    name = state.Name,
                    transitions = new List<FsmTransitionModel>(),
                    isStart = fsm.Fsm.StartState == state.Name,
                };

                model.states.Add(stateModel);

                stateLookup[state.Name] = stateModel;
            }

            // All events
            var fsmEvents = new HashSet<string>(fsm.FsmEvents.Select(e => e.Name));

            // Now populate transitions
            foreach (var state in fsm.FsmStates) {
                var stateModel = stateLookup[state.Name];

                // Gather transition events
                var evts = new HashSet<string>();
                foreach (var transition in state.Transitions) {
                    if (evts.Contains(transition.FsmEvent.Name)) {
                        throw new System.Exception("Duplicate transition event: " + transition.FsmEvent.Name + " in state: " + state.Name);
                    }

                    FsmStateModel toState;
                    if (!stateLookup.TryGetValue(transition.ToState, out toState)) {
                        throw new System.Exception("Missing to state in transition: " + transition.FsmEvent.Name + " in state: " + state.Name);
                    }

                    if (toState == stateModel) {
                        throw new System.Exception("Detected self transition in state: " + state.Name + " for event: " + transition.FsmEvent.Name);
                    }

                    evts.Add(transition.FsmEvent.Name);
                }

                // Gather internal actions
                stateModel.internalActions = new List<FsmInternalActionModel>();
                foreach (var action in state.Actions) {
                    if (action is Actions.InternalAction) {
                        var i = action as Actions.InternalAction;
                        if (i._event == null || string.IsNullOrEmpty(i._event.Value)) {
                            throw new System.Exception(string.Format("Internal action in '{0}' has no event defined", state.Name));
                        }
                        else if (!fsmEvents.Contains(i._event.Value)) {
                            throw new System.Exception(string.Format("Internal action in '{0}' has event '{1}' but no event by that name is defined", state.Name, i._event.Value));
                        }

                        var actionModel = new FsmInternalActionModel{
                            evt = new FsmEventModel{
                                name = i._event.Value,
                            },
                            _delegate = new FsmDelegateMethod(),
                        };

                        FillInMethodInfo(stateModel, action as Actions.BaseDelegateAction, i._event.Value + " (internal action)", actionModel._delegate);

                        stateModel.internalActions.Add(actionModel);
                    }
                }
                
                // Gather transitions
                foreach (var transition in state.Transitions) {
                    FsmStateModel toState = stateLookup[transition.ToState];

                    var transitionModel = new FsmTransitionModel{
                        evt = new FsmEventModel{
                            name = transition.FsmEvent.Name,
                        },
                        from = stateModel,
                        to = toState,
                    };

                    stateModel.transitions.Add(transitionModel);
                }

                // Gather on enter
                stateModel.onEnter = new List<FsmOnEnterExitModel>();
                stateModel.onExit = new List<FsmOnEnterExitModel>();

                foreach (var action in state.Actions) {
                    AddEnterExitEvent(action as Actions.BaseDelegateAction, stateModel);
                }
            }

            // Now gather all required interfaces
            var interfaces = new HashSet<Type>();
            foreach (var state in model.states) {
                foreach (var m in state.onEnter) {
                    interfaces.Add(m._delegate._interface);
                }
                foreach (var m in state.internalActions) {
                    interfaces.Add(m._delegate._interface);
                }
                foreach (var m in state.onExit) {
                    interfaces.Add(m._delegate._interface);
                }
            }

            model.context = new FsmContextModel{
                requiredInterfaces = interfaces.OrderBy(t => t.FullName).ToList(),
            };

            // Gather all used events
            var events = new HashSet<string>();
            foreach (var state in model.states) {
                foreach (var transition in state.transitions) {
                    events.Add(transition.evt.name);
                }
                foreach (var action in state.internalActions) {
                    events.Add(action.evt.name);
                }
            }

            model.events = events.OrderBy(e => e).Select(e => new FsmEventModel{ name = e }).ToList();

            return model;
        }

        private void GetPrefabData(PlayMakerFSM fsm, FsmModel model)
        {
            if (PrefabUtility.GetPrefabType(fsm.gameObject) != PrefabType.Prefab) {
                return;
            }

            model.fromPrefab = AssetDatabase.GetAssetPath(fsm.gameObject);
            model.prefabGuid = AssetDatabase.AssetPathToGUID(model.fromPrefab);
        }

        private void AddEnterExitEvent(Actions.BaseDelegateAction action, FsmStateModel state)
        {
            if (action == null) {
                return;
            }
            if (!(action is Actions.OnEntryAction || action is Actions.OnExitAction)) {
                return;
            }

            var ls = action is Actions.OnEntryAction ? state.onEnter : state.onExit;
            var label = action is Actions.OnEntryAction ? "OnEnter" : "OnExit";

            var _delegate = new FsmDelegateMethod();
            FillInMethodInfo(state, action, label, _delegate);

            ls.Add(new FsmOnEnterExitModel{
                _delegate = _delegate,
            });
        }

        private void FillInMethodInfo(FsmStateModel state, Actions.BaseDelegateAction action, string label, FsmDelegateMethod model)
        {
            if (action.delegateInterface == null || string.IsNullOrEmpty(action.delegateInterface.Value)) {
                throw new System.Exception(string.Format("State '{0}' has action '{1}' that is missing delegate or method", state.name, label));
            }

            if (action.methodSignature == null || string.IsNullOrEmpty(action.methodSignature.Value)) {
                throw new System.Exception(string.Format("State '{0}' has action '{1}' that is missing delegate or method", state.name, label));
            }

            var _delegate = TypeCache.FindDelegate(action.delegateInterface.Value, action.methodSignature.Value);
            if (_delegate == null) {
                throw new System.Exception(string.Format("State '{0}' has action '{1}' that has a non-existant interface '{2}'", state.name, label, action.delegateInterface.Value));
            }
            if (_delegate.method == null) {
                throw new System.Exception(string.Format("State '{0}' has action '{1}' for  interface '{2}' that is missing method signature '{3}'", state.name, label, action.delegateInterface.Value, action.methodSignature.Value));
            }

            model._interface = _delegate.type;
            model.method = _delegate.method;
            model.delegateDisplayName = action.delegateInterface.Value + "." + action.methodSignature.Value;

            if (options.verbose) {
                Debug.LogFormat("For action '{0}' found delegate: {1}", label, model.delegateDisplayName);
            }
        }
    }
}
#endif
