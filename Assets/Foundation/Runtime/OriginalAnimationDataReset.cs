namespace Whitebox.Foundation
{
    public sealed partial class OriginalActiveLevelAnimationValues
    {
        public void Reset() // Native AnimationData.Reset0x338ae28, exact mutation order.
        {
            LeagueCrown.Consume(); KingsCup.Consume(); TeamBattle.Consume();
            Coin.Consume(); CompletedNThousandLevelCoin.Consume(); SkyRace.Consume();
            TeamTreasure.Consume(); SpaceMission.Consume(); BalloonRise.Consume();
            HiddenTemple.Consume(); DragonNest.Consume(); MagicCauldron.Consume();
            DailyLightningRush.Consume(); PropellerRush.Consume();
            ((OriginalBeforeAfterData)PinataParty).Consume();
            ((OriginalBeforeAfterData)Madness).Consume(); ArcheryArena.Consume();
            DukesFortune.Consume(); TeamTournament.Consume(); TrainJourney.Consume();
            OceanOdyssey.Consume(); MissionEvents.ShouldConsume = false;
            PuzzleBreak.Consume(); RobotRivals.Consume(); MergeSmith.Consume();
            ((OriginalBeforeAfterData)ClashOfChampions).Consume(); RoyalWorldCup.Consume();
            ShortExpeditionEvent.Consume(); InviteFriends.ShouldConsume = false;
            DukesTreats.Consume(); ((OriginalBeforeAfterData)RoyalPass).Consume(); WeeklyPass.Consume();
            BoosterShouldConsume = false; CanPlayLevelWithMultiplier = false;
            Boosters.Clear(); UnlimitedBoosters.Clear(); EventItems.Clear(); Multipliers.Clear();
        }
    }
}
