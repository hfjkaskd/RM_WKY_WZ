using System;
using System.Collections;
using UnityEngine;

namespace Whitebox.Foundation
{
    public interface IOriginalWinFlowContext : IOriginalLevelWinDataContext
    {
        bool IsBonus { get; }
        void MultiplierTokenOnStoryOrBonusLevelWin();
        bool ShouldSpeedUpLevelEnd { get; }
        void KingBeHappy();
        IEnumerator FadeOutSoundSource();
        Coroutine ManualStartCoroutine(IEnumerator routine);
        OriginalFlowManager GetFlowManager();
        OriginalFlowAction CreateWinLogoAction();
        OriginalFlowAction CreateRemainingMovesAction();
        OriginalFlowAction CreateLevelWinAction();
    }
    // WinFailManager.StartWinFlow0x1eddefc. Flow/dialog construction and
    // application owners remain required typed factories and callbacks.
    public sealed class OriginalWinFlowCoordinator
    {
        private readonly IOriginalWinFlowContext context;
        public Coroutine FadeOutCoroutine { get; private set; }
        public OriginalWinFlowCoordinator(IOriginalWinFlowContext context) { this.context = context; }
        public void StartWinFlow()
        {
            if (context == null) throw new NullReferenceException();
            if (context.IsBonus) context.MultiplierTokenOnStoryOrBonusLevelWin();
            if (!context.ShouldSpeedUpLevelEnd) context.KingBeHappy();
            OriginalLevelEnd.UpdateLevelWinData(context);
            // Native reads the speed-up owner again after LevelEnd callbacks.
            if (context.ShouldSpeedUpLevelEnd)
            { var routine = context.FadeOutSoundSource(); FadeOutCoroutine = context.ManualStartCoroutine(routine); }
            var flow = context.GetFlowManager();
            var logo = context.CreateWinLogoAction();
            if (flow == null) throw new NullReferenceException();
            flow.Push(logo);
            flow.Push(context.CreateRemainingMovesAction());
            flow.Push(context.CreateLevelWinAction());
        }
    }
}
