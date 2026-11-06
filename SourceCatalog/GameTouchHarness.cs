using System;using System.Collections.Generic;using Whitebox.Foundation;using UnityEngine;
namespace UnityEngine{
public struct Vector2{public float x,y;public Vector2(float x,float y){this.x=x;this.y=y;}public static Vector2 zero=>new Vector2(0,0);public static Vector2 operator -(Vector2 a,Vector2 b)=>new Vector2(a.x-b.x,a.y-b.y);}
public static class Debug{public static void LogError(object x){Verify.Trace.Add("enable-error");}}
}
namespace Whitebox.Foundation{
public struct OriginalCellPoint{public readonly int X,Y;public OriginalCellPoint(int x,int y){X=x;Y=y;}}
public class OriginalGridCell{public int X,Y,Id;}
public class OriginalCellGrid{
public OriginalGridCell[] Cells=new OriginalGridCell[96];public OriginalCellGrid(){for(int i=0;i<96;i++)Cells[i]=new OriginalGridCell{X=i%12,Y=i/12,Id=i+1};}
public OriginalGridCell GetCell(int x,int y){Verify.Trace.Add("cell:"+x+":"+y);return x>=0&&y>=0&&x<12&&y<8?Cells[x+y*12]:null;}}
}
class Verify:IOriginalGameTouchInput,IOriginalGameTouchServices{
public static List<string> Trace=new List<string>();OriginalCellGrid grid=new OriginalCellGrid();OriginalCellGridHitTest hit;
public bool ui,booster,can=true,should,down,held,up;Vector2 position;
public Verify(){hit=new OriginalCellGridHitTest(grid,Vector2.zero,false,false);}
public bool IsUiTouching{get{Trace.Add("ui");return ui;}}public bool IsBoosterSelected=>booster;
public bool CanUserMakeMove(){Trace.Add("can");return can;}public Vector2 GetMouseWorldPosition(){Trace.Add("mouse");Trace.Add("project");return position;}
public bool GetMouseButtonDown(){Trace.Add("down");return down;}public bool GetMouseButton(){Trace.Add("held");return held;}public bool GetMouseButtonUp(){Trace.Add("up");return up;}
public OriginalGridCell GetCellFromWorldPosition(Vector2 p)=>hit.GetCellFromWorldPosition(p);
public bool ShouldDeselectBoosterOnTouch(OriginalGridCell c){Trace.Add("should:"+(c==null?0:c.Id));return should;}
public void DeselectBooster(){Trace.Add("deselect");booster=false;}
public void CancelTouchedItemAnimation(){Trace.Add("cancel");}public void TouchCell(OriginalGridCell c){Trace.Add("touch:"+(c==null?0:c.Id));if(c==null)throw new NullReferenceException();}
public bool SwapCells(OriginalGridCell c,OriginalCellPoint p){Trace.Add("swap:"+(c==null?0:c.Id)+":"+p.X+":"+p.Y);return false;}
public bool TapCell(OriginalGridCell c){Trace.Add("tap:"+(c==null?0:c.Id));return false;}
public static uint B(float v)=>BitConverter.ToUInt32(BitConverter.GetBytes(v),0);public static float F(string v)=>BitConverter.ToSingle(BitConverter.GetBytes(uint.Parse(v)),0);
static void Main(){string line;while((line=Console.ReadLine())!=null){var a=line.Split(';');var services=new Verify();var listener=new OriginalGameTouchListener(services,services);
if(a[0]=="POINT"){var f=a[1].Split(',');var p=OriginalCellPointInput.FromMousePosition(new Vector2(F(f[0]),F(f[1])),f[2]=="1",f[3]=="1");Console.WriteLine(p.X+","+p.Y);continue;}
if(a[0]=="WORLD"){var f=a[1].Split(',');services.hit=new OriginalCellGridHitTest(services.grid,new Vector2(F(f[2]),F(f[3])),f[4]=="1",f[5]=="1");Trace.Clear();var c=services.GetCellFromWorldPosition(new Vector2(F(f[0]),F(f[1])));Console.WriteLine((c==null?0:c.Id)+";"+string.Join("|",Trace));continue;}
foreach(var command in a){var f=command.Split(',');Trace.Clear();bool error=false;string extra="";try{switch(f[0]){
case "M":services.position=new Vector2(F(f[1]),F(f[2]));services.down=f[3]=="1";services.held=f[4]=="1";services.up=f[5]=="1";services.ui=f[6]=="1";services.booster=f[7]=="1";services.can=f[8]=="1";services.should=f[9]=="1";listener.ManualUpdate();break;
case "D":listener.DisableTouch();break;case "E":listener.EnableTouch();break;case "C":listener.Clear(f[1]=="1");break;case "S":listener.StopTouch();break;
case "P":listener.TouchDown(new Vector2(F(f[1]),F(f[2])));break;case "V":listener.TouchMove(new Vector2(F(f[1]),F(f[2])));break;case "U":listener.TouchUp(new Vector2(F(f[1]),F(f[2])));break;
case "T":listener.StartTouch(new Vector2(F(f[1]),F(f[2])),f[3]=="0"?null:services.grid.Cells[int.Parse(f[3])-1]);break;
case "G":OriginalCellPoint p;bool ok=listener.GetTowardsCell(new Vector2(F(f[1]),F(f[2])),out p);extra=(ok?1:0)+":"+p.X+":"+p.Y;break;
}}
catch(NullReferenceException){error=true;}
Console.WriteLine(listener.TouchDisableCount+","+(listener.IsTouching?1:0)+","+B(listener.StartPosition.x)+","+B(listener.StartPosition.y)+","+(listener.SelectedCell==null?0:listener.SelectedCell.Id)+","+(error?1:0)+","+extra+";"+string.Join("|",Trace));}}}
}
