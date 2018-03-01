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

using System.Collections;
using System.Collections.Generic;
using Type=System.Type;
using MethodInfo=System.Reflection.MethodInfo;

namespace UnityFSMCodeGenerator
{
    public class FsmModel
    {
        public string description;
        public string fromPrefab;
        public string prefabGuid;
        public FsmContextModel context;
        public List<FsmEventModel> events;
        public List<FsmStateModel> states;
    }

    public class FsmContextModel
    {
        public List<Type> requiredInterfaces;
    }

    public class FsmStateModel
    {
        public string name;
        public bool isStart;
        public List<FsmOnEnterExitModel> onEnter;
        public List<FsmOnEnterExitModel> onExit;
        public List<FsmTransitionModel> transitions;
        public List<FsmInternalActionModel> internalActions;
    }

    public class FsmOnEnterExitModel
    {
        public FsmDelegateMethod _delegate;
    }

    public class FsmInternalActionModel
    {
        public FsmEventModel evt;
        public FsmDelegateMethod _delegate;
    }

    public class FsmTransitionModel
    {
        public FsmEventModel evt;
        public FsmStateModel from;
        public FsmStateModel to;
    }

    public class FsmEventModel
    {
        public string name;
    }

    public class FsmDelegateMethod
    {
        public Type _interface;            
        public MethodInfo method;         
        public string delegateDisplayName;
    }
}
