using System;
using System.Collections.Generic;

namespace Whitebox.Foundation
{
    public partial class OriginalMadnessSessionData
    {
        public void TryAddMadnessForRemainingMoves(int count,int difficulty) // 0x32b38e4; no table-game gate in this native method.
        {
            bool valid=IsEventValid();
            if(count<1||!valid)return;
            AddExtra(unchecked(count*GetMultiplierValueForDifficulty(difficulty)*5));ShouldConsume=true;
        }
    }
    public partial class OriginalDailyLightningRushLevelData
    {
        // Super-booster lookup is required before IncrementCount checks event
        // validity. The owner resolves it separately for both native calls.
        public void AddLightningForRushRemainingMoves(int count,bool superLightball) // 0x338df68.
        {IncrementCount(unchecked(count*(superLightball?2:1)*5));}
        public void AddLightningForRushLightballsOnBoard(int count,bool superLightball) // 0x338e00c.
        {IncrementCount(unchecked(count*(superLightball?2:1)*20));}
    }
    public partial class OriginalClashOfChampionsLevelData
    {
        public void IncrementScoreForRemainingSpecialItems(Dictionary<int,int> items,bool superLightball) // 0x338d14c.
        {
            if(!CanIncreaseScore)return;
            int score=0;
            foreach(var item in items)
            {
                int value=item.Key==2?rocketItemScore:item.Key==3?propellerItemScore:item.Key==4?tntItemScore:
                    item.Key==5?unchecked(lightballItemScore*(superLightball?2:1)):0;
                score=unchecked(score+value*item.Value);
            }
            IncrementScore(unchecked(remainingSpecialItemsMultiplier*score));
        }
    }
    public sealed partial class OriginalActiveLevelAnimationValues
    {
        public void AddMissionEvents(int type,sbyte difficulty,int count=1) // 0x338b92c.
        {
            var data=StartSnapshots??throw new NullReferenceException();
            var mission=data.MissionEvents??throw new NullReferenceException();
            mission.AddExtraForItem(type,unchecked(OriginalBeforeAfterData.GetMultiplierValueForDifficulty(difficulty)*count));
        }
    }
}
