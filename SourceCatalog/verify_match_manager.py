"""Exercise search orchestration, boundary origins, falling and reuse together."""
from pathlib import Path
import subprocess, tempfile, json

project = Path(__file__).resolve().parents[1]
mono = Path('C:/Program Files/Unity/Hub/Editor/2022.3.62f2/Editor/Data/MonoBleedingEdge')
harness = r'''using System;using System.Collections.Generic;using Whitebox.Foundation;
namespace UnityEngine{public static class Debug{public static void LogError(object x){}}}
class State:IOriginalMatchBlockingState,IOriginalMatchFreezeOperations{
 public bool HasTouchBlockingItemExceptChain(OriginalGridCell c){return false;}
 public bool HasIncomingTouchBlockingItem(OriginalGridCell c){return false;}
 public void FreezeFall(OriginalGridCell c){throw new Exception("Search must not freeze cells");}}
class Verify{
 static string Format(IReadOnlyList<OriginalResolvedMatchGroup> g){string s="";for(int i=0;i<g.Count;i++){s+=g[i].PatternType+":";for(int j=0;j<g[i].CellCount;j++)s+=g[i].GetCell(j).X+","+g[i].GetCell(j).Y+";";}return s;}
 static void Main(){int count=0;var state=new State();
 for(int seed=0;seed<100;seed++){
  var random=new Random(seed);var grid=new OriginalCellGrid(6,6);var ids=new int[36];for(int i=0;i<36;i++)ids[i]=1;
  var layout=OriginalBoardLayout.Create(6,6,ids,false,false);
  for(int x=0;x<6;x++)for(int y=0;y<6;y++){var c=new OriginalGridCell(x,y,1);grid.SetCell(x,y,c);c.Mediation.SetAllItems(new OriginalMatchBoardItem(x+6*y,0,random.Next(1,5)));}
  grid.CacheNeighbors();var access=new OriginalBoardGridAccess(grid,layout);var strategy=new OriginalBottomLeftHorizontalStrategy(access);
  var manager=new OriginalMatchManager(grid,layout,state,state);var search=new OriginalMatchSearch(strategy,state);var groups=new OriginalMatchGroups(6,6,state);
  var threes=new List<OriginalTempMatch>();var candidates=new List<OriginalPossibleMatch>();
  for(int mode=0;mode<3;mode++){
   threes.Clear();int cx=seed%10-2,cy=(seed/10)%10-2,r=seed%4;
   if(mode==0){var it=new OriginalGridIterator(strategy);OriginalGridCell c;while((c=it.MoveNext())!=null)search.CollectForCell(c,false,threes);}
   else{for(int x=Math.Max(cx-r,0);x<=Math.Min(cx+r,5);x++)for(int y=Math.Max(cy-r,0);y<=Math.Min(cy+r,5);y++)search.CollectForCell(grid.GetCell(x,y),mode==2,threes);}
   OriginalMatchCandidates.Build(threes,candidates);OriginalMatchCandidates.Sort(candidates);var expected=groups.Build(threes,candidates);
   if(mode!=1)groups.ExtendGroups(state,mode==2);string wanted=Format(expected);
   var actual=mode==0?manager.FindAllMatches():mode==1?manager.FindMatchesInArea(cx,cy,r):manager.FindExtensiveMatchesInArea(cx,cy,r);
   if(Format(actual)!=wanted)throw new Exception("Search order/bounds "+seed+"/"+mode);count++;
  }
 }
 // Radius constrains origins, not the participating cells of a pattern.
 var line=new OriginalCellGrid(3,1);var board=OriginalBoardLayout.Create(3,1,new[]{1,1,1},false,false);
 for(int x=0;x<3;x++){var c=new OriginalGridCell(x,0,1);line.SetCell(x,0,c);c.Mediation.SetAllItems(new OriginalMatchBoardItem(x,0,1));}line.CacheNeighbors();
 var m=new OriginalMatchManager(line,board,state,state);
 if(m.FindMatchesInArea(0,0,0).Count!=1)throw new Exception("Boundary pattern lost");count++;
 var falling=line.GetCell(1,0).Mediation.CurrentItem;line.GetCell(1,0).Mediation.Next.ClearItem();
 if(m.FindExtensiveMatchesInArea(0,0,0).Count!=0)throw new Exception("Falling item included");count++;
 if(m.FindMatchesInArea(0,0,0).Count!=1)throw new Exception("Default search must include falling");count++;
 if(m.FindMatchesInArea(9,9,0).Count!=0)throw new Exception("Previous results leaked");count++;
 Console.WriteLine(count);
 }}'''
names = ('OriginalMatchManager','OriginalMatchSnapshot','OriginalCellMediation','OriginalGridIterator',
         'OriginalCellGrid','OriginalBoardLayout','OriginalLevelRules','OriginalMatchSearch',
         'OriginalMatchCandidates','OriginalMatchGroups','OriginalMatchExtensions')
with tempfile.TemporaryDirectory() as tmp:
    tmp=Path(tmp);source=tmp/'Verify.cs';source.write_text(harness);exe=tmp/'Verify.exe'
    result=subprocess.run([str(mono/'bin/mono.exe'),str(mono/'lib/mono/4.5/mcs.exe'),'-out:'+str(exe),str(source),
        *[str(project/'Assets/Foundation/Runtime'/f'{name}.cs') for name in names]],capture_output=True,text=True)
    assert result.returncode==0,result.stdout+result.stderr
    result=subprocess.run([str(mono/'bin/mono.exe'),str(exe)],capture_output=True,text=True,encoding='utf-8')
    assert result.returncode==0,result.stdout+result.stderr
    assert result.stdout.strip().lstrip('\ufeff')=='304',result.stdout
report={'passed':True,'managed_integration_cases':304,'native_evidence':['0x12e105c','0x12e11f0','0x12e1328','0x12e1448'],
 'scope':'Managed orchestration integration; existing native-verified stages. Area clipping/order, pattern origins beyond radius, falling exclusion and repeated search. This is not a native whole-manager differential test or playable-loop validation.'}
(project/'match_manager_verification.json').write_text(json.dumps(report,indent=2)+'\n')
print(json.dumps(report))

