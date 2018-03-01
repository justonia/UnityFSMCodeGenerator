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
using UnityEditor;
using UnityEditor.Compilation;
using TypeHelpers=HutongGames.PlayMakerEditor.TypeHelpers;
using Type=System.Type;
using MethodInfo=System.Reflection.MethodInfo;

namespace UnityFSMCodeGenerator.Editor
{
    [InitializeOnLoad]
    public class TypeCache
    {
        private static Assembly[] assemblies;

        private static bool cached;
        private static IFsmActionDelegateInfo[] fsmActionDelegatesInfo;
        private static Type[] fsmActionDelegates;
        private static Dictionary<string, IFsmActionDelegateInfo> infoByFullName;
        private static Dictionary<int, IFsmActionDelegateInfo> infoByChoiceIndex;
        private static string[] fsmActionDelegatesStringsChoices;

        static TypeCache()
        {
            assemblies = CompilationPipeline.GetAssemblies().Where(a => (a.flags & AssemblyFlags.EditorAssembly) == 0).ToArray();
        }

        public class IFsmActionDelegateInfo
        {
            public Type type;
            public string choiceName;
            public int choiceIndex;
            public MethodInfo[] methods;
            public string[] methodChoices;
            public Dictionary<string, MethodInfo> methodSignatureToInfo;
        }

        public static IFsmActionDelegateInfo[] GetFsmActionDelegates()
        {
            BuildCache();

            return fsmActionDelegatesInfo;
        }

        public static IFsmActionDelegateInfo GetIFsmActionDelegateInfoByFullName(string name)
        {
            BuildCache();

            IFsmActionDelegateInfo info;
            if (!infoByFullName.TryGetValue(name, out info)) {
                return null;
            }

            return info;
        }

        public static IFsmActionDelegateInfo GetIFsmActionDelegateInfoByChoiceIndex(int index)
        {
            if (index == 0) {
                return null;
            }

            BuildCache();

            IFsmActionDelegateInfo info;
            if (!infoByChoiceIndex.TryGetValue(index, out info)) {
                return null;
            }

            return info;
        }

        public static string[] GetFsmActionDelegatesStringChoices()
        {
            BuildCache();

            return fsmActionDelegatesStringsChoices;
        }

        public class Delegate
        {
            public Type type;
            public MethodInfo method;
        }

        public static Delegate FindDelegate(string interfaceFullName, string methodSignature)
        {
            BuildCache();

            IFsmActionDelegateInfo info;
            if (!infoByFullName.TryGetValue(interfaceFullName, out info)) {
                return null;
            }

            MethodInfo method;
            if (!info.methodSignatureToInfo.TryGetValue(methodSignature, out method)) {
                return new Delegate{ type = info.type };
            }

            return new Delegate{ type = info.type, method = method };
        }

        #region Private Methods
        
        private static void BuildCache()
        {
            #if UNITY_EDITOR
            if (cached) {
                return;
            }

            fsmActionDelegates = BuildFsmActionDelegates().ToArray();

            fsmActionDelegatesInfo = fsmActionDelegates
                .Select(t => new IFsmActionDelegateInfo{
                    type = t,
                    choiceName = BuildChoiceName(t),
                    methods = BuildMethods(t),
                })
                .OrderByDescending(i => i.type.FullName)
                .ToArray();

            for (int i = 0; i < fsmActionDelegatesInfo.Length; i++) {
                fsmActionDelegatesInfo[i].choiceIndex = i+1;
                BuildMethodChoices(fsmActionDelegatesInfo[i]);
            }

            infoByFullName = new Dictionary<string, IFsmActionDelegateInfo>();
            infoByChoiceIndex = new Dictionary<int, IFsmActionDelegateInfo>();

            foreach (var i in fsmActionDelegatesInfo) {
                infoByFullName[i.type.FullName] = i;
                infoByChoiceIndex[i.choiceIndex] = i;
            }

            var _fsmActionDelegatesStrings = fsmActionDelegatesInfo.Select(t => t.choiceName).ToList();
            _fsmActionDelegatesStrings.Insert(0, "");
            fsmActionDelegatesStringsChoices = _fsmActionDelegatesStrings.ToArray();

            cached = true;
            #endif
        }

        private static string BuildChoiceName(Type t)
        {
            return t.FullName.Replace(".", "/");
        }

        private static void BuildMethodChoices(IFsmActionDelegateInfo info)
        {
            // TODO: Use custom method signature generator so we can break PlayMaker dependency and potential
            // backwards incompatibilities;
            info.methodChoices = info.methods
                .Select(m => TypeHelpers.GetMethodSignature(m))
                .ToArray();

            info.methodSignatureToInfo = new Dictionary<string, MethodInfo>();
            for (int i = 0; i < info.methods.Length; i++) {
                info.methodSignatureToInfo[info.methodChoices[i]] = info.methods[i];
            }
        }

        private static MethodInfo[] BuildMethods(Type t)
        {
            var ls = new List<MethodInfo>();
            _BuildMethods(t, ls);
            return ls.ToArray();
        }

        private static void _BuildMethods(Type t, List<MethodInfo> ls)
        {
            foreach (var method in t.GetMethods()) {
                // TODO: support more than no-argument methods?
                if (method.GetParameters().Length != 0) {
                    continue;
                }
                ls.Add(method);
            }

            foreach (var i in t.GetInterfaces()) {
                _BuildMethods(i, ls);
            }
        }

        private static List<Type> BuildFsmActionDelegates()
        {
            var ls = new List<Type>();

            var iFsmDelegate = typeof(IFsmActionDelegate);

            foreach (var assembly in assemblies) {
                var a = System.Reflection.Assembly.Load(assembly.name);

                var types = a.GetTypes();
                foreach (var type in types) {
                    if (!type.IsInterface || type == iFsmDelegate) {
                        continue;
                    }
                        
                    if (InterfaceIsActionDelegate(type)) {
                        ls.Add(type);
                    }
                }
            }

            return ls;
        }

        private static bool InterfaceIsActionDelegate(System.Type type)
        {
            if (type == typeof(IFsmActionDelegate)) {
                return true;
            }

            foreach (var i in type.GetInterfaces()) {
                if (InterfaceIsActionDelegate(i)) {
                    return true;
                }
            }

            return false;
        }

        #endregion
    }
}
#endif
