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

using System.Collections.Generic;

namespace UnityFSMCodeGenerator
{
    // If enabled, the compilation process will output extra information and
    // have the generated FSM implement this interface.
    public interface IFsmIntrospectionSupport
    {
        // These string names will be whatever the name of the states were before
        // the generator trimmed and turned them into enum values.
        string State { get; }
        List<string> AllStates { get; }
        object EnumStateFromString(string stateName);
        string StateFromEnumState(object state);
        string GeneratedFromPrefabGUID { get; }
    }
}
