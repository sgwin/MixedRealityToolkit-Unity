﻿// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using Microsoft.MixedReality.Toolkit.Core.Definitions.InputSystem;
using Microsoft.MixedReality.Toolkit.Core.Definitions.Utilities;
using Microsoft.MixedReality.Toolkit.Core.Interfaces.Devices;
using Microsoft.MixedReality.Toolkit.Core.Interfaces.InputSystem;
using Microsoft.MixedReality.Toolkit.Core.Managers;
using System;
using System.Collections.Generic;
using UnityEngine;

#if UNITY_STANDALONE_WIN || UNITY_WSA || UNITY_EDITOR_WIN
using UnityEngine.Windows.Speech;
#endif // UNITY_STANDALONE_WIN || UNITY_WSA || UNITY_EDITOR_WIN

namespace Microsoft.MixedReality.Toolkit.Core.Devices.VoiceInput
{
#if UNITY_STANDALONE_WIN || UNITY_WSA || UNITY_EDITOR_WIN
    public class WindowsSpeechInputDeviceManager : BaseDeviceManager, IMixedRealitySpeechSystem
    {
        public WindowsSpeechInputDeviceManager(string name, uint priority) : base(name, priority) { }

        /// <summary>
        /// The keywords to be recognized and optional keyboard shortcuts.
        /// </summary>
        private static SpeechCommands[] Commands => MixedRealityManager.Instance.ActiveProfile.InputSystemProfile.SpeechCommandsProfile.SpeechCommands;

        /// <summary>
        /// The Input Source for Windows Speech Input.
        /// </summary>
        public IMixedRealityInputSource InputSource = null;


        private KeywordRecognizer keywordRecognizer;

        public RecognitionConfidenceLevel RecognitionConfidenceLevel { get; set; }

        /// <inheritdoc />
        public override void Enable()
        {
            if (!Application.isPlaying || Commands.Length == 0) { return; }

            InputSource = MixedRealityManager.InputSystem?.RequestNewGenericInputSource("Windows Speech Input Source");

            var newKeywords = new string[Commands.Length];

            for (int i = 0; i < Commands.Length; i++)
            {
                newKeywords[i] = Commands[i].Keyword;
            }

            RecognitionConfidenceLevel = MixedRealityManager.Instance.ActiveProfile.InputSystemProfile.SpeechCommandsProfile.SpeechRecognitionConfidenceLevel;
            keywordRecognizer = new KeywordRecognizer(newKeywords, (ConfidenceLevel) RecognitionConfidenceLevel);
            keywordRecognizer.OnPhraseRecognized += KeywordRecognizer_OnPhraseRecognized;

            if (MixedRealityManager.Instance.ActiveProfile.InputSystemProfile.SpeechCommandsProfile.SpeechRecognizerStartBehavior == AutoStartBehavior.AutoStart)
            {
                StartRecognition();
            }
        }

        /// <inheritdoc />
        public override void Update()
        {
            if (keywordRecognizer != null && keywordRecognizer.IsRunning)
            {
                for (int i = 0; i < Commands.Length; i++)
                {
                    if (Input.GetKeyDown(Commands[i].KeyCode))
                    {
                        OnPhraseRecognized((ConfidenceLevel) RecognitionConfidenceLevel, TimeSpan.Zero, DateTime.Now, Commands[i].Keyword);
                    }
                }
            }
        }

        /// <inheritdoc />
        public override void Disable()
        {
            if (keywordRecognizer != null)
            {
                StopRecognition();
                keywordRecognizer.OnPhraseRecognized -= KeywordRecognizer_OnPhraseRecognized;
                keywordRecognizer.Dispose();
            }
        }

        /// <summary>
        /// Make sure the keyword recognizer is off, then start it.
        /// Otherwise, leave it alone because it's already in the desired state.
        /// </summary>
        public void StartRecognition()
        {
            if (keywordRecognizer != null && !keywordRecognizer.IsRunning)
            {
                keywordRecognizer.Start();
            }
        }

        /// <summary>
        /// Make sure the keyword recognizer is on, then stop it.
        /// Otherwise, leave it alone because it's already in the desired state.
        /// </summary>
        public void StopRecognition()
        {
            if (keywordRecognizer != null && keywordRecognizer.IsRunning)
            {
                keywordRecognizer.Stop();
            }
        }

        private void KeywordRecognizer_OnPhraseRecognized(PhraseRecognizedEventArgs args)
        {
            OnPhraseRecognized(args.confidence, args.phraseDuration, args.phraseStartTime, args.text);
        }

        private void OnPhraseRecognized(ConfidenceLevel confidence, TimeSpan phraseDuration, DateTime phraseStartTime, string text)
        {
            for (int i = 0; i < Commands?.Length; i++)
            {
                if (Commands[i].Keyword == text)
                {
                    MixedRealityManager.InputSystem.RaiseSpeechCommandRecognized(InputSource, Commands[i].Action, (RecognitionConfidenceLevel)confidence, phraseDuration, phraseStartTime, text);
                    break;
                }
            }
        }
    }
#endif // UNITY_STANDALONE_WIN || UNITY_WSA || UNITY_EDITOR_WIN
}