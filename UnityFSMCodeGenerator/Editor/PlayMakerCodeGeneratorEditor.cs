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
using System;
using System.IO;
using System.Linq;
using System.Collections;
using System.Collections.Generic;

namespace UnityFSMCodeGenerator.Editor
{
    [CustomEditor(typeof(PlayMakerCodeGenerator))]
    public class PlayMakerCodeGeneratorEditor : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();
            
            if (Application.isPlaying) {
                return;
            }

            var compiler = target as PlayMakerCodeGenerator;

            if (compiler.fsm == null) {
                return;
            }

            GUI.enabled = !string.IsNullOrEmpty(compiler.outputOptions.className) && IsValidOutputFile(compiler);

            if (GUILayout.Button("Compile")) {
                WriteCode(compiler);
            }
            GUI.enabled = true;

            if (!string.IsNullOrEmpty(compiler.DebugDescription)) {
                GUI.enabled = false;
                EditorGUILayout.TextArea(compiler.DebugDescription);
                GUI.enabled = true;

                if (GUILayout.Button("Clear")) {
                    compiler.DebugDescription = null;
                }
            }

            if (GUILayout.Button("Show Debug Description")) {
                RegenerateDescription(compiler);
            }
        }

        private bool IsValidOutputFile(PlayMakerCodeGenerator compiler)
        {
            if (compiler.outputOptions.outputInPrefabDirectory) {
                if (PrefabUtility.GetPrefabType(compiler.gameObject) != PrefabType.Prefab) {
                    return false;
                }

                return true;
            }

            try {
                var path = compiler.outputOptions.assetsRelativeOutputFile;

                if (string.IsNullOrEmpty(path)) {
                    return false;
                }

                // If an exception happens we'll return false
                var filename = Path.GetFileName(path);
                var dir = Path.Combine(Application.dataPath, Path.GetDirectoryName(path));
                Path.Combine(dir, filename);

                return true;
            }
            catch(Exception) {
                return false;
            }
        }

        private bool GetOutputFilePath(PlayMakerCodeGenerator compiler, out string dstFile)
        {
            string filename = null;

            if (compiler.outputOptions.outputInPrefabDirectory) {
                var prefabPath = AssetDatabase.GetAssetPath(compiler.gameObject);
                var appPath = Application.dataPath.Substring(0, Application.dataPath.Length - "/Assets".Length);
                var fixedUpPrefabPath = (appPath + "/" + prefabPath).Replace("/", "\\");
                filename = Path.GetFileNameWithoutExtension(fixedUpPrefabPath) + ".Generated.cs";
                dstFile = Path.Combine(Path.GetDirectoryName(fixedUpPrefabPath), filename);
                return true;
            }

            var path = compiler.outputOptions.assetsRelativeOutputFile;
            filename = Path.GetFileName(path);
            var dir = Path.Combine(Application.dataPath, Path.GetDirectoryName(path));
            dstFile = Path.Combine(dir, filename);

            if (compiler.outputOptions.allowMakeDirs) {
                Directory.CreateDirectory(dir);
            }
            
            if (!Directory.Exists(dir)) {
                Debug.LogErrorFormat(compiler, "Cannot write FSM to '{0}', directories do not exist and allowMakeDirs is false", dstFile);
                return false;
            }

            return true;            
        }

        private void WriteCode(PlayMakerCodeGenerator compiler)
        {
            var parser = new PlayMakerParser(compiler.parserOptions);

            try {
                var fsm = compiler.fsm as PlayMakerFSM;
                var model = parser.CreateModel(fsm);
                var generator = new CodeGenerator(compiler.generatorOptions);

                var output = generator.Generate(model, compiler.outputOptions.className);
                
                string outputFile;
                if (!GetOutputFilePath(compiler, out outputFile)) {
                    return;
                }
                
                File.WriteAllText(outputFile, output.code);

                AssetDatabase.Refresh();
            }
            catch (System.Exception e) {
                Debug.LogErrorFormat("PlayMakerCodeGenerator: error writing FSM: {0}", e.ToString());
                return;
            }
        }

        private void RegenerateDescription(PlayMakerCodeGenerator compiler)
        {
            var parser = new PlayMakerParser(compiler.parserOptions);

            try {
                var fsm = compiler.fsm as PlayMakerFSM;
                var model = parser.CreateModel(fsm);
                compiler.DebugDescription = Stringify.CreateDescription(model);
            }
            catch (System.Exception e) {
                Debug.LogErrorFormat("PlayMakerStringify, error parsing FSM: {0}", e.ToString());
                return;
            }
        }
    }
}
#endif
