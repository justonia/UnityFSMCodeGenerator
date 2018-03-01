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
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;

namespace UnityFSMCodeGenerator.Examples
{
    public interface ITelephoneConnected : IFsmActionDelegate
    {
        void OnEnter();
        void OnExit();
    }

    public interface ITelephoneOffHook : IFsmActionDelegate
    {
        void OnExit();
    }

    public interface ITelephoneHaptics : IFsmActionDelegate
    {
        void Pulse();
    }

    public interface IVolumeControl : IFsmActionDelegate
    {
        void Mute();
        void Unmute();
        void ChangeVolume();
    }

    public class Telephone : MonoBehaviour, 
        ITelephoneConnected, 
        ITelephoneOffHook, 
        ITelephoneHaptics, 
        IVolumeControl
    {
        private TelephoneFSM fsm;
        private TelephoneFSM.IContext context;

        public Text activeState;
        public Text statusMessage;
        public AudioSource ringer;

        private void Awake()
        {
            fsm = new TelephoneFSM();
            context = TelephoneFSM.NewDefaultContext(this, this, this);
            fsm.Bind(context);
        }

        private void Update()
        {
            activeState.text = context.State.ToString();
        }

        void ITelephoneConnected.OnEnter()
        {
        }

        void ITelephoneConnected.OnExit()
        {
        }

        void ITelephoneOffHook.OnExit()
        {
        }

        void ITelephoneHaptics.Pulse()
        {
        }

        void IVolumeControl.Mute()
        {
        }

        void IVolumeControl.Unmute()
        {
        }

        void IVolumeControl.ChangeVolume()
        {
        }
    }
}
