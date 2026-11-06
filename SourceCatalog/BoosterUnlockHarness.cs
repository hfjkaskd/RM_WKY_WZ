using System;
using Whitebox.Foundation;
static class BoosterUnlockHarness
{
    sealed class Inventory:IOriginalBoosterInventoryPresentation
    {
        public bool Free;public int Count;public string Trace="";
        public bool HasFreeUsage(OriginalBoosterType t){Trace+="F";return Free;}
        public int GetBooster(OriginalBoosterType t){Trace+="C";return Count;}
    }
    static int Bit(bool v,int i)=>v?1<<i:0;
    static void Main()
    {
        string line;while((line=Console.ReadLine())!=null)
        {
            var f=line.Split('|');int kind=int.Parse(f[0]),type=int.Parse(f[1]),level=int.Parse(f[2]),group=int.Parse(f[3]);
            bool tutorial=f[4]=="1",free=f[5]=="1";int count=int.Parse(f[6]);
            var t=(OriginalBoosterType)type;
            if(kind==0){Console.WriteLine(OriginalBoosterUnlock.GetUnlockLevel(t));continue;}
            if(kind==1){Console.WriteLine(OriginalBoosterUnlock.IsUnlocked(t,level)?1:0);continue;}
            if(kind==2){Console.WriteLine(OriginalBoosterUnlock.IsNewUserFlowAbEnabled(group)?1:0);continue;}
            if(kind==3){Console.WriteLine(OriginalBoosterUnlock.IsNewUserFlowEnabled(group!=0,level,tutorial)?1:0);continue;}
            var inventory=new Inventory{Free=free,Count=count};var p=OriginalBoosterButtonPresentation.Prepare(t,level,group!=0,tutorial,inventory);
            int flags=Bit(p.Unlocked,0)|Bit(p.Interactable,1)|Bit(p.ShowLevel,2)|Bit(p.ShowCount,3)|Bit(p.ShowCountText,4)|Bit(p.ShowAdd,5)|Bit(p.ShowFreeRibbon,6)|Bit(p.ShowFreeText,7);
            Console.WriteLine(flags+","+p.Count+","+inventory.Trace);
        }
    }
}
