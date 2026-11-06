using System;
using System.IO;
using System.Collections.Generic;
using Whitebox.Foundation;
namespace UnityEngine { public struct Vector3 { public float x,y,z; public Vector3(float x,float y,float z){this.x=x;this.y=y;this.z=z;} } }
namespace Whitebox.Foundation
{
    public static class RocketComboTrace
    {
        public static readonly List<string> Events = new List<string>();
        public static string Tag(OriginalExplodeData d)
        {
            using(var memory = new MemoryStream()) using(var w = new BinaryWriter(memory))
            {
                w.Write(d.Point.X);w.Write(d.Point.Y);w.Write(d.Trigger);w.Write(d.Id);w.Write(d.MatchType);
                w.Write(d.CanSpreadJelly);w.Write(d.IsHorizontalRocket);w.Write((ushort)0);w.Write(d.ComboId);
                w.Write(d.LastTargetItemIdentifier.ItemType);w.Write(d.LastTargetItemIdentifier.StaticItemType);w.Write(d.LastTargetItemIdentifier.ItemGroupId);
                w.Write(d.ExploderIndex);w.Write(d.IncomingX);w.Write(d.IncomingY);w.Write(d.IncomingZ);
                return BitConverter.ToString(memory.ToArray()).Replace("-", "").ToLowerInvariant();
            }
        }
    }
    public sealed class OriginalGridCell { public int X,Y; }
    public sealed class OriginalItemMediation
    {
        public OriginalSpecialBoardItem Owner;
        public void ClearFromAllRegisteredCells(){Owner.CurrentCell=null;}
    }
    public class OriginalSpecialBoardItem
    {
        public OriginalGridCell CurrentCell;public readonly OriginalItemMediation Mediator;
        public OriginalSpecialBoardItem(){Mediator=new OriginalItemMediation{Owner=this};}
    }
    public sealed class OriginalTntBoardItem : OriginalSpecialBoardItem { }
    public sealed class OriginalRocketBoardItem : OriginalSpecialBoardItem
    {
        public void ExplodeWithPresetData(OriginalExplodeData trigger,OriginalExplodeData preset)
        {RocketComboTrace.Events.Add("explode:"+RocketComboTrace.Tag(trigger)+":"+RocketComboTrace.Tag(preset));}
    }
    public sealed class TntRocketServices : IOriginalTntRocketComboServices
    {
        public Action Complete;
        public void StartComboOperation(){}
        public void FinishComboOperation(){RocketComboTrace.Events.Add("finish");}
        public void AnimateTntRocket(OriginalGridCell center,OriginalGridCell empty,OriginalSpecialBoardItem to,OriginalSpecialBoardItem from,int swipe,Action completed)
        {RocketComboTrace.Events.Add("swipe:"+swipe);Complete=completed;}
        public void PlaySfx(int type,float delay){if(type!=316||delay!=.04f)throw new Exception("Audio mapping");}
        public void MarkTntRocketCells(OriginalGridCell cell,OriginalExplodeData h,OriginalExplodeData v)
        {RocketComboTrace.Events.Add("mark:"+RocketComboTrace.Tag(h)+":"+RocketComboTrace.Tag(v));}
        public OriginalRocketBoardItem CreateComboRocketAt(int tile,OriginalCellPoint point)
        {RocketComboTrace.Events.Add("create:"+tile+":"+point.X+":"+point.Y);return new OriginalRocketBoardItem();}
        public void RecycleComboViews(OriginalTntBoardItem tnt,OriginalRocketBoardItem rocket){RocketComboTrace.Events.Add("recycle");}
    }
}
public static class VerifyTntRocketCombo
{
    public static void Main()
    {
        var services=new TntRocketServices();var combo=new OriginalTntRocketCombo(services);string line;
        combo.OnComboMade+=(p,h,v)=>RocketComboTrace.Events.Add("event:"+p.X+":"+p.Y+":"+h+":"+v);
        while((line=Console.ReadLine())!=null)
        {
            var a=Array.ConvertAll(line.Split(','),int.Parse);
            OriginalSpecialBoardItem to=a[4]==0?(OriginalSpecialBoardItem)new OriginalTntBoardItem():new OriginalRocketBoardItem();
            OriginalSpecialBoardItem from=a[4]==0?(OriginalSpecialBoardItem)new OriginalRocketBoardItem():new OriginalTntBoardItem();
            to.CurrentCell=new OriginalGridCell{X=a[0],Y=a[1]};from.CurrentCell=new OriginalGridCell{X=a[2],Y=a[3]};
            RocketComboTrace.Events.Clear();OriginalExploder.Reset();for(int i=0;i<a[5];i++)OriginalExploder.Next(2);
            combo.Prepare(to,from);combo.Explode(new OriginalExplodeData(2,999,OriginalCellPoint.Default,0,false));services.Complete();
            Console.WriteLine(string.Join("|",RocketComboTrace.Events));
        }
    }
}
