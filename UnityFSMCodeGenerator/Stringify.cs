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
using System.Collections;
using System.Collections.Generic;

namespace UnityFSMCodeGenerator
{
    public static class Stringify
    {
        public static string CreateDescription(FsmModel model)
        {
            var sb = new System.Text.StringBuilder();
            sb.Append("\n");

            sb.Append("-----------------------\n");
            sb.Append("Required Interfaces \n");
            sb.Append("-----------------------\n\n");

            foreach (var _interface in model.context.requiredInterfaces) {
                sb.Append(_interface.FullName);
                sb.Append("\n");
            }

            sb.Append("\n");
            
            sb.Append("--------\n");
            sb.Append("Events \n");
            sb.Append("--------\n\n");

            foreach (var evt in model.events) {
                sb.Append(evt.name);
                sb.Append("\n");
            }

            sb.Append("\n");

            sb.Append("--------\n");
            sb.Append("States \n");
            sb.Append("--------\n\n");

            foreach (var state in model.states) {
                ToDebugStringAppendState(sb, state, 0);
                sb.Append("\n");
            }

            return sb.ToString();
        }

        private static void ToDebugStringAppendState(System.Text.StringBuilder sb, FsmStateModel state, int indent)
        {
            ToDebugStringIndent(sb, indent);

            sb.Append("s[ ");
            sb.Append(state.name);
            sb.Append(" ]\n");

            foreach (var onEnter in state.onEnter) {
                ToDebugStringIndent(sb, 4);
                sb.Append("OnEnter -> ");
                sb.Append(onEnter._delegate.delegateDisplayName);
                sb.Append("\n");
            }

            foreach (var action in state.internalActions) {
                ToDebugStringAppendInternalAction(sb, action, 4);
                sb.Append("\n");
            }

            foreach (var transition in state.transitions) {
                ToDebugStringAppendTransition(sb, transition, 4);
                sb.Append("\n");
            }

            foreach (var onExit in state.onExit) {
                ToDebugStringIndent(sb, 4);
                sb.Append("OnExit -> ");
                sb.Append(onExit._delegate.delegateDisplayName);
                sb.Append("\n");
            }

            foreach (var ignore in state.ignoreEvents) {
                ToDebugStringIndent(sb, 4);
                sb.Append("e[ ");
                sb.Append(ignore.name);
                sb.Append(" ] -> Ignore");
                sb.Append("\n");
            }
        }

        private static void ToDebugStringAppendTransition(System.Text.StringBuilder sb, FsmTransitionModel transition, int indent)
        {
            ToDebugStringIndent(sb, indent);

            sb.Append("e[ ");
            sb.Append(transition.evt.name);
            sb.Append(" ] -> s[ ");
            sb.Append(transition.to.name);
            sb.Append(" ]");
        }

        private static void ToDebugStringAppendInternalAction(System.Text.StringBuilder sb, FsmInternalActionModel action, int indent)
        {
            ToDebugStringIndent(sb, indent);

            sb.Append("e[ ");
            sb.Append(action.evt.name);
            sb.Append(" ] -> ");
            sb.Append(action._delegate.delegateDisplayName);
            sb.Append(" (internal action)");
        }

        private static void ToDebugStringIndent(System.Text.StringBuilder sb, int indent)
        {
            for (int i = 0; i < indent; i++) {
                sb.Append(" ");
            }
        }
    }
}
