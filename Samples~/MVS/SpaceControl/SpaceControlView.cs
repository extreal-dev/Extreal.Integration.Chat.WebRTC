﻿using System;
using UniRx;
using UnityEngine;
using UnityEngine.UI;

namespace Extreal.Integration.Chat.WebRTC.MVS.SpaceControl
{
    public class SpaceControlView : MonoBehaviour
    {
        [SerializeField] private Button backButton;

        public IObservable<Unit> OnBackButtonClicked
            => backButton.OnClickAsObservable().TakeUntilDestroy(this);
    }
}
