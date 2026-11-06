namespace Whitebox.Foundation
{
    public enum OriginalBoosterType { None=0,Rocket=1,Tnt=2,LightBall=3,Hammer=4,Arrow=5,Cannon=6,JesterHat=7,Propeller=8 }
    public static class OriginalBoosterUnlock
    {
        public static int GetUnlockLevel(OriginalBoosterType type) // 0x318d4c4, ELF table0x4053060.
        {
            switch(type){case OriginalBoosterType.Hammer:return 8;case OriginalBoosterType.Arrow:return 14;
                case OriginalBoosterType.Cannon:return 17;case OriginalBoosterType.JesterHat:return 19;default:return 7;}
        }
        public static bool IsUnlocked(OriginalBoosterType type,int userLevel) => userLevel>=GetUnlockLevel(type); // 0x318d4e8.
        public static bool IsNewUserFlowAbEnabled(int group) => group==2||group==3; // 0x32be230.
        public static bool IsNewUserFlowEnabled(bool abEnabled,int userLevel,bool skipMinimumLevel=false) =>
            abEnabled&&(skipMinimumLevel||userLevel>4); // 0x3268ad4.
    }
    public interface IOriginalBoosterInventoryPresentation
    {
        bool HasFreeUsage(OriginalBoosterType type);
        int GetBooster(OriginalBoosterType type);
    }
    public readonly struct OriginalBoosterButtonPresentation
    {
        public readonly bool Unlocked,Interactable,ShowLevel,ShowCount,ShowCountText,ShowAdd,ShowFreeRibbon,ShowFreeText;
        public readonly int Count;
        public OriginalBoosterButtonPresentation(bool unlocked,bool interactable,bool level,bool count,bool countText,bool add,bool ribbon,bool free,int amount)
        {Unlocked=unlocked;Interactable=interactable;ShowLevel=level;ShowCount=count;ShowCountText=countText;ShowAdd=add;ShowFreeRibbon=ribbon;ShowFreeText=free;Count=amount;}
        public static OriginalBoosterButtonPresentation Prepare(OriginalBoosterType type,int level,bool newUserFlowAb,bool tutorial,IOriginalBoosterInventoryPresentation inventory)
        {
            // InGameBoosterButton.PrepareButton0x13c14d4: locked and tutorial
            // paths must not query inventory or unlimited-use clocks.
            if(!OriginalBoosterUnlock.IsUnlocked(type,level))
                return new OriginalBoosterButtonPresentation(false,newUserFlowAb,newUserFlowAb,false,false,false,newUserFlowAb,false,0);
            if(tutorial)return new OriginalBoosterButtonPresentation(true,true,false,false,false,false,false,false,0);
            if(inventory==null)throw new System.NullReferenceException("Original booster inventory owner is not bound.");
            if(inventory.HasFreeUsage(type))return new OriginalBoosterButtonPresentation(true,true,false,false,false,false,true,true,0);
            int count=inventory.GetBooster(type);
            return new OriginalBoosterButtonPresentation(true,true,false,count!=0,count!=0,count==0,false,false,count);
        }
    }
}
