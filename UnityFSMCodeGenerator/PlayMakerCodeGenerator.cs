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
using UnityEngine;

namespace UnityFSMCodeGenerator
{
    // Place this behavior on a game object that has a declarative PlayMakerFSM and 
    public class PlayMakerCodeGenerator : MonoBehaviour
    {
        [System.Serializable]
        public struct OutputOptions
        {
            public string className;
            public bool outputInPrefabDirectory;
            public string assetsRelativeOutputFile;
            public bool allowMakeDirs;
        }

        public MonoBehaviour fsm;
        public OutputOptions outputOptions;
        public ParserOptions parserOptions;
        public CodeGeneratorOptions generatorOptions;
        

        #if UNITY_EDITOR
        public string DebugDescription { get; set; }
        #endif

        private void OnValidate()
        {
            #if PLAYMAKER
            fsm = fsm is PlayMakerFSM ? fsm : null;
            #else
            fsm = null;
            #endif

            if (!string.IsNullOrEmpty(outputOptions.className)) {
                outputOptions.className = outputOptions.className.Replace(" ", "");
            }
        }
    }
}
