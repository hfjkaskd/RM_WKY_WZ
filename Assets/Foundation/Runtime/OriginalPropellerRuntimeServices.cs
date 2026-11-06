using UnityEngine;

namespace Whitebox.Foundation
{
    public sealed partial class OriginalPlayableBoard : IOriginalPropellerRuntimeServices, IOriginalPropellerViewServices
    {
        private int propellerSortingOffset = 10000; // ExplodeTargetFinder.ResetSorting 0x1c94270.
        public int NextPropellerSortingOffset() // 0x1c94b0c, reduceOffset=false.
        {
            propellerSortingOffset = unchecked(propellerSortingOffset - 1);
            if (propellerSortingOffset < 0) propellerSortingOffset = 10000;
            return propellerSortingOffset;
        }
        int IOriginalPropellerPilotContext.FrameCount => board.Clock.FrameCount;
        float IOriginalPropellerPilotContext.LevelTime => board.Clock.Time;
        float IOriginalPropellerPilotContext.LevelDeltaTime => board.Clock.DeltaTime;
        float IOriginalPropellerPilotContext.UnityDeltaTime => Time.deltaTime;
        float IOriginalPropellerPilotContext.RandomValue() => UnityEngine.Random.value;
        void IOriginalPropellerPilotContext.ReportLargeDelta(float delta) => Debug.LogWarning("Propeller flight clamped a Unity delta above 0.5 seconds.");
        void IOriginalPropellerPilotContext.ReportStalledFlight(bool forcedExplosion) => Debug.LogWarning("Propeller flight remained still for 45 gameplay frames.");
        public IOriginalExplodeTarget ReservePropellerNeighbor(OriginalGridCell cell, OriginalExplodeData data) => guidedTargetServices.AddIncomingExploder(cell, data);
        public void CollectPropellerMadness(OriginalExplodeData data, OriginalGridCell center)
        {
            Vector3 p = center.GetViewPosition();
            Session.TryCollectMadnessForExploder(data, new OriginalSwapPoint(p.x, p.y, p.z), 0);
        }
        public void PlayPropellerTakeoffParticles(Vector3 position) => board.Presentation.PlayPropellerTakeoffParticles(position);
        public void PlayPropellerTargetParticles(Vector3 position) => board.Presentation.PlayPropellerTargetParticles(position);
        public void PlaySfxInLoop(int type) => audio.GameSfx.PlaySfxInLoop(type);
        public void StopSoundInLoop(int type) => audio.GameSfx.StopSoundInLoop(type);
        public float PropellerRocketTargetDelay=>board.Presentation.RocketAssets.propellerComboTargetDelay;
        int IOriginalPropellerRuntimeServices.GuidedBoardStartY=>board.Layout.CurrentBoard.Bottom;
        int IOriginalPropellerRuntimeServices.GuidedBoardEndY=>board.Layout.CurrentBoard.Top;
        public void StartApplicationCoroutine(System.Collections.IEnumerator routine)=>host.ApplicationCoroutines.ManualStartCoroutine(routine);
        public void PlayPropellerRocketParticles(Vector3 position)=>board.Presentation.Particles.GetGlobalParticle<OriginalPropellerRocketComboParticles>(13).Play(position);
        public IOriginalExplodeTarget FindGuidedTarget(OriginalExplodeData data,IOriginalExplodeTarget previous)
        {
            int guided=OriginalExplodeTargetRules.AsGuidedType(data.Trigger);IOriginalExplodeTarget result;
            if(data.Trigger==10||data.Trigger==11)
            {GuidedTargets.FillScores(guided,data.CanSpreadJelly);result=LineTargets.FindForRocket(data);}
            else if(data.Trigger==13)
            {result=AreaTargets.FindForTnt(data);if(result==null)GuidedTargets.FillScores(guided,data.CanSpreadJelly);}
            else return GuidedTargets.FindForPropeller(data,previous);
            if(result!=null)return result;
            return GuidedTargets.ResolveSingleTarget(data, previous);
        }
    }
}
