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
    public interface ITelephone : IFsmActionDelegate
    {
        void ConnectedToCall();
        void SuspendCall();
        void DisconnectCall();
    }

    public interface IHaptics : IFsmActionDelegate
    {
        void Pulse();
    }

    public interface IAudioControl : IFsmActionDelegate
    {
        void StartRinging();
        void StopRinging();
        void VolumeUp();
        void VolumeDown();
    }

    public class Telephone : MonoBehaviour, 
        ITelephone,
        IAudioControl,
        IHaptics,
        UnityFSMCodeGenerator.IHaveBaseFsm
    {
        private TelephoneFSM fsm;
        private TelephoneFSM.IContext context;
        private TelephoneVolumeFSM volumeFsm;
        private float volume;
        private Coroutine ringing;
        private Coroutine pulsing;
        private bool listenToggleChange = true;

        [Header("Status Info")]
        public Text activeState;
        public Text statusMessage;

        [Header("Call Control")]
        public Button callButton;
        public Button hangUpButton;
        public Button answerCallButton;
        public Image onHoldBackground;
        public Toggle onHoldToggle;

        [Header("\"Haptics\"")]
        public Animator imageAnimator;
        public int numHapticPulses = 2;

        [Header("Audio")]
        public float delayBetweenRings = 1f;
        public AudioSource ringer;
        public AudioSource voice;
        public float startVolume = 0.6f;
        public Button volumeUpButton;
        public Button volumeDownButton;
        public Text volumePercent;

        UnityFSMCodeGenerator.BaseFsm UnityFSMCodeGenerator.IHaveBaseFsm.BaseFsm { get { return fsm; }}

        private void Awake()
        {
            fsm = new TelephoneFSM();
            context = TelephoneFSM.NewDefaultContext(this, this, this);
            fsm.Bind(context);

            volumeFsm = new TelephoneVolumeFSM();
            volumeFsm.Bind(TelephoneVolumeFSM.NewDefaultContext(this));

            volume = startVolume;
            ringer.volume = startVolume;
            voice.volume = startVolume;

            statusMessage.text = "";

            volumePercent.text = ((int)(volume * 100f)).ToString() + "%";
        }

        private void Update()
        {
            // These should be event driven instead of polling, but for simplicity sake
            // it's implemented this way for the example.
            activeState.text = context.State.ToString();

            callButton.interactable = context.State == TelephoneFSM.State.OffHook;

            hangUpButton.interactable = 
                context.State == TelephoneFSM.State.Ringing ||
                context.State == TelephoneFSM.State.Connected ||
                context.State == TelephoneFSM.State.OnHold;

            answerCallButton.interactable = context.State == TelephoneFSM.State.Ringing;

            switch (context.State) {
            case TelephoneFSM.State.Connected:
            case TelephoneFSM.State.OnHold:
                onHoldBackground.color = Color.white;
                onHoldToggle.interactable = true;
                break;
            default:
                onHoldBackground.color = callButton.colors.disabledColor; // just steal this from button
                onHoldToggle.interactable = false;

                // Just reseting the visual of the toggle since we aren't in a valid state, squelch 
                // responding to change in isOn since this will send spurious events.
                listenToggleChange = false;
                onHoldToggle.isOn = false;
                listenToggleChange = true;
                break;
            }

            volumeUpButton.interactable = volume < 1f;
            volumeDownButton.interactable = volume > 0f;
        }

        public void OnCallPhoneClicked()
        {
            fsm.SendEvent(TelephoneFSM.Event.CallDialed);
        }

        public void OnHangUpClicked()
        {
            fsm.SendEvent(TelephoneFSM.Event.HungUp);
        }

        public void OnAnswerCallClicked()
        {
            fsm.SendEvent(TelephoneFSM.Event.CallConnected);
        }

        public void OnHoldChanged()
        {
            if (!listenToggleChange) {
                return;
            }

            if (onHoldToggle.isOn) {
                fsm.SendEvent(TelephoneFSM.Event.OnHold);
            }
            else {
                fsm.SendEvent(TelephoneFSM.Event.OffHold);
            }
        }

        public void OnVolumeUpClicked()
        {
            volumeFsm.SendEvent(TelephoneVolumeFSM.Event.VolumeUp);
        }

        public void OnVolumeDownClicked()
        {
            volumeFsm.SendEvent(TelephoneVolumeFSM.Event.VolumeDown);
        }

        #region ITelephone

        void ITelephone.ConnectedToCall()
        {
            if (voice.isPlaying) {
                voice.UnPause();
            }
            else {
                voice.Play();
            }
        }

        void ITelephone.SuspendCall()
        {
            voice.Pause();
        }

        void ITelephone.DisconnectCall()
        {
            voice.Stop();

            // Even though we are mid-dispatch of an event it is ok to call this as the
            // event will be queued up and sent after the current event finalizes.
            // See UML 'run to completion'
            fsm.SendEvent(TelephoneFSM.Event.HungUp);
        }

        #endregion

        #region IAudioControl
        
        void IAudioControl.StartRinging()
        {
            if (ringing == null) {
                ringing = StartCoroutine(Ring());
            }
        }
        
        void IAudioControl.StopRinging()
        {
            if (ringing != null) {
                StopCoroutine(ringing);
                ringing = null;
            }
            ringer.Stop();
        }


        private IEnumerator Ring()
        {
            var wait = new WaitForSeconds(delayBetweenRings);

            ringer.Play();
            while (true) {
                yield return new WaitUntil(() => !ringer.isPlaying);
                yield return wait;

                ringer.Play();
            }
        }

        void IAudioControl.VolumeUp()
        {
            volume = Mathf.Min(1f, volume + 0.25f);
            ringer.volume = volume;
            voice.volume = volume;
            volumePercent.text = ((int)(volume * 100f)).ToString() + "%";
            
            volumeFsm.SendEvent(TelephoneVolumeFSM.Event.VolumeChanged);
        }

        void IAudioControl.VolumeDown()
        {
            volume = Mathf.Max(0f, volume - 0.25f);
            ringer.volume = volume;
            voice.volume = volume;
            volumePercent.text = ((int)(volume * 100f)).ToString() + "%";
            
            volumeFsm.SendEvent(TelephoneVolumeFSM.Event.VolumeChanged);
        }

        #endregion

        #region IHaptics

        void IHaptics.Pulse()
        {
            if (pulsing != null) {
                StopCoroutine(pulsing);
            }

            pulsing = StartCoroutine(DoHapticPulse(numHapticPulses));
        }

        private IEnumerator DoHapticPulse(int num)
        {
            imageAnimator.enabled = true;
            imageAnimator.Play("TelephoneBuzz");

            imageAnimator.SetBool("Buzz", true);

            // Yea this is gross
            while (true) {
                var info = imageAnimator.GetCurrentAnimatorStateInfo(0);
                var numLoopsSoFar = (int)Mathf.Floor(info.normalizedTime);
                if (numLoopsSoFar == num) {
                    break;
                }

                yield return null;
            }
            
            imageAnimator.SetBool("Buzz", false);
            pulsing = null;
        }

        #endregion
    }
}
