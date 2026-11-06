using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using Whitebox.Foundation;
namespace UnityEngine
{
    public struct Vector3 { public float x,y,z;public Vector3(float a,float b,float c){x=a;y=b;z=c;} }
    public static class Mathf { public static float Sqrt(float v)=>(float)Math.Sqrt(v);public static int RoundToInt(float v)=>(int)Math.Round(v);public static float Min(float a,float b)=>Math.Min(a,b); }
}
namespace Whitebox.Foundation
{
    // Typed boundaries for non-geometric owners; no simulated damage claim.
    public struct OriginalCellPoint {public int X,Y;public OriginalCellPoint(int x,int y){X=x;Y=y;} }
    public struct OriginalExplodeData {public int Trigger;public OriginalCellPoint Point;}
    public static class OriginalExploder { public static OriginalExplodeData Next(int t,bool j,OriginalCellPoint p)=>new OriginalExplodeData{Trigger=t,Point=p}; }
    public class OriginalCellMediation {public OriginalCellMediation(OriginalGridCell cell){}public void ClearAllItems(){} }
    public class OriginalLightballBoardItem {public OriginalGridCell CurrentCell;public Mediator Mediator=new Mediator();}
    public class Mediator {public void ClearFromAllRegisteredCells(){} }
    public partial class OriginalGridCell
    {
        public int Frozen;public void FreezeFall(){Frozen++;}public void UnFreezeFall(){if(--Frozen<0)throw new Exception("Freeze underflow");}
        public UnityEngine.Vector3 GetViewPosition()=>new UnityEngine.Vector3(X,Y,0);
        public void ExplodeCurrentItem(OriginalExplodeData d){if(d.Trigger!=16)throw new Exception("Wrong native trigger");}
    }
}
class BallBallGeometryHarness:IOriginalBallBallComboServices,IOriginalBoardStartGridAccess
{
    OriginalGridCell[,] cells;OriginalBallBallCombo combo;Action visualCallback;
    readonly List<string> hits=new List<string>(),waits=new List<string>();int touch,operation,swipe,madness;
    public OriginalGridCell CurrentBoardBottomLeftCell=>cells[0,0];
    public int BoardStartY=>0;
    public OriginalGridCell GetCell(int x,int y)=>x>=0&&x<cells.GetLength(0)&&y>=0&&y<cells.GetLength(1)?cells[x,y]:null;
    public bool IsInCurrentBoard(OriginalGridCell cell)=>cell!=null;
    public OriginalGridCell GetCell(OriginalCellPoint p)=>GetCell(p.X,p.Y);
    public void RecycleLightballForCombo(OriginalLightballBoardItem item){}
    public void StartSpecialOperation(){operation++;}public void FinishSpecialOperation(){operation--;}
    public void DisableComboTouch(){touch++;}public void EnableComboTouch(){touch--;}
    public void PlayBallBallView(UnityEngine.Vector3 p,int s,Action a){swipe=s;visualCallback=a;}
    public void ShakeBallBallGrid(){}
    public void StartBallBallCoroutine(IEnumerator r){Drain(r);}
    void Drain(IEnumerator r){while(r.MoveNext()){if(r.Current is IEnumerator child)Drain(child);}}
    public object WaitBallBall(float seconds)=>null;
    public void CollectBallBallMadness(OriginalExplodeData d,OriginalGridCell c){madness++;}
    static void Main()
    {
        CultureInfo.CurrentCulture=CultureInfo.InvariantCulture;string line;
        while((line=Console.ReadLine())!=null)
        {
            var v=Array.ConvertAll(line.Split(','),int.Parse);var h=new BallBallGeometryHarness();h.cells=new OriginalGridCell[v[0],v[1]];
            for(int x=0;x<v[0];x++)for(int y=0;y<v[1];y++)h.cells[x,y]=new OriginalGridCell(x,y,1);
            for(int x=0;x<v[0];x++)for(int y=0;y<v[1]-1;y++)h.cells[x,y].Neighbors.Set(1,h.cells[x,y+1]);
            h.combo=new OriginalBallBallCombo(new OriginalBottomLeftVerticalStrategy(h),h);
            h.combo.CellTriggered+=(radius,p)=>{if(!h.combo.IsExplosionActive||h.touch!=1||h.operation!=1)throw new Exception("Owner state");
                foreach(var c in h.cells)if(c.Frozen!=1)throw new Exception("Unfrozen damage cell");h.hits.Add(radius+":"+p.X+":"+p.Y);};
            h.combo.RingWaitStarted+=(radius,seconds)=>h.waits.Add(radius+":"+BitConverter.ToUInt32(BitConverter.GetBytes(seconds),0));
            h.combo.Prepare(new OriginalLightballBoardItem{CurrentCell=h.GetCell(v[4],v[5])},new OriginalLightballBoardItem{CurrentCell=h.GetCell(v[2],v[3])});
            h.combo.Explode();if(!h.combo.IsComboActive||h.touch!=1||h.operation!=1)throw new Exception("Merge state");h.visualCallback();
            if(h.combo.IsComboActive||h.combo.IsExplosionActive||h.touch!=0||h.operation!=0||h.madness!=1)throw new Exception("Completion state");
            foreach(var c in h.cells)if(c.Frozen!=0)throw new Exception("Freeze leak");
            Console.WriteLine(h.swipe+"|"+string.Join(";",h.hits)+"|"+string.Join(";",h.waits));
        }
    }
}
