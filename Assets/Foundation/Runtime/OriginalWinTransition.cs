using System;

namespace Whitebox.Foundation
{
    public interface IOriginalWinTransitionContext
    {
        bool AnimationImprovementEnabled { get; }
        void KingBeHappy();
        void KingStopWorry();
        int[] CreateWinLowMatchRatios();
        void UpdateLowMatchRatios(int[] ratios);
        void IncrementLives(int amount);
        bool IsStory { get; }
        void ResetEgoOnWin();
    }
    // Side effects after SetStateToWin in WinFail.Win0x1edd734. Application
    // King/Life/Ego owners are required dependencies, never silent fallbacks.
    public static class OriginalWinTransition
    {
        public static void OnWin(IOriginalWinTransitionContext context)
        {
            if (context == null) throw new NullReferenceException();
            if (context.AnimationImprovementEnabled) context.KingBeHappy(); else context.KingStopWorry();
            var ratios = context.CreateWinLowMatchRatios();
            context.UpdateLowMatchRatios(ratios);
            context.IncrementLives(1);
            if (!context.IsStory) context.ResetEgoOnWin();
        }
    }
}
