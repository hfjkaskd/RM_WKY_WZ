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
    public sealed class OriginalGridCell
    {
        public string Name;public int X,Y;
        public void FreezeFallForDuration(float value){RocketComboTrace.Events.Add("freeze:"+Name+":"+BitConverter.ToInt32(BitConverter.GetBytes(value),0));}
    }
    public sealed class OriginalItemMediation
    {
        public OriginalRocketBoardItem Owner;
        public void ClearFromAllRegisteredCells(){RocketComboTrace.Events.Add("clear:"+Owner.Name);Owner.CurrentCell=null;}
    }
    public sealed class OriginalRocketBoardItem
    {
        public string Name;public int Orientation;public OriginalGridCell CurrentCell;public readonly OriginalItemMediation Mediator;
        public OriginalRocketBoardItem(){Mediator=new OriginalItemMediation{Owner=this};}
        public void ChangeOrientation(int v){Orientation=v;RocketComboTrace.Events.Add("orientation:"+Name+":"+v);}
        public void ChangePosition(UnityEngine.Vector3 p){RocketComboTrace.Events.Add("position:"+Name+":"+(int)p.x+":"+(int)p.y);}
        public void ExplodeWithPresetData(OriginalExplodeData trigger,OriginalExplodeData preset)
        {RocketComboTrace.Events.Add("explode:"+Name+":"+RocketComboTrace.Tag(trigger)+":"+RocketComboTrace.Tag(preset));}
    }
}
public static class VerifyRocketRocketCombo
{
    public static void Main()
    {
        var combo=new OriginalRocketRocketCombo();string line;
        while((line=Console.ReadLine())!=null)
        {
            var a=Array.ConvertAll(line.Split(','),int.Parse);
            var to=new OriginalRocketBoardItem{Name="to",Orientation=a[4],CurrentCell=new OriginalGridCell{Name="to",X=a[0],Y=a[1]}};
            var from=new OriginalRocketBoardItem{Name="from",Orientation=a[5],CurrentCell=new OriginalGridCell{Name="from",X=a[2],Y=a[3]}};
            RocketComboTrace.Events.Clear();OriginalExploder.Reset();
            combo.Prepare(to,from);
            combo.Explode(new OriginalExplodeData(2,a[6],new OriginalCellPoint(-1,-1),0,false));
            Console.WriteLine(string.Join("|",RocketComboTrace.Events));
        }
    }
}
