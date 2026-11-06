using System;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace Whitebox.Foundation.Editor
{
    public static class PropellerRocketComboVerification
    {
        private static OriginalInitialBoardView board;
        private static OriginalPlayableBoard game;
        private static OriginalGameTouchInput device;
        private static OriginalGameTouchListener listener;
        private static OriginalFallTargetPlanner fallQueries;
        private static Samples samples;
        private static OriginalGridCell source,destination;
        private static OriginalPropellerBoardItem propeller;
        private static OriginalRocketBoardItem rocket;
        private static IOriginalExplodeTarget reserved;
        private static bool[,] boxes;
        private static int test,frame,sample,movesBefore,swapsBefore,refillsBefore,goalsBefore;
        private static float started,steadyAt,arrivalAt,distance;
        private static Vector3 start,firstPartStart,secondPartStart;
        private static bool carried,wobbled,hidden,delayed,partsStarted,impactCaptured,carryCaptured;
        private static readonly Report report=new Report();
        private static string ReportPath=>Path.Combine(Application.dataPath,"../live_propeller_rocket_combo_verification.json");
        public static void Run()
        {
            board=UnityEngine.Object.FindObjectOfType<OriginalInitialBoardView>();game=UnityEngine.Object.FindObjectOfType<FoundationController>().Gameplay;
            device=UnityEngine.Object.FindObjectOfType<OriginalGameTouchInput>();device.Unbind();samples=new Samples();listener=new OriginalGameTouchListener(samples,game);
            fallQueries=new OriginalFallTargetPlanner(new OriginalBoardFallContext(board.Grid,board.Layout,board.InitialItemAmounts,game,null),new OriginalFallRefillColumns());
            boxes=new bool[board.Grid.Width,board.Grid.Height];
            for(int x=0;x<board.Grid.Width;x++)for(int y=0;y<board.Grid.Height;y++)boxes[x,y]=board.Grid.GetCell(x,y).Mediation.CurrentItem is OriginalBoxBoardItem;
            File.WriteAllText(ReportPath,"{\"passed\":false,\"status\":\"running\"}\n");Begin();EditorApplication.update+=Tick;
        }
        private static void Begin()
        {
            bool vertical=test>=8,reverse=test%8>=4,found=false;
            for(int y=7;y>=5&&!found;y--)for(int x=0;x<(vertical?9:8);x++)
            {
                var a=board.Grid.GetCell(x,y);var b=board.Grid.GetCell(vertical?x:x+1,vertical?y-1:y);
                if(!(a.Mediation.CurrentItem is OriginalMatchBoardItem)||!(b.Mediation.CurrentItem is OriginalMatchBoardItem))continue;
                source=reverse?b:a;destination=reverse?a:b;found=true;break;
            }
            Require(found,"Adjacent regular cells for a real production-factory swap fixture");Clear(source);Clear(destination);
            bool fromRocket=test%4>=2;
            rocket=(OriginalRocketBoardItem)game.CreateItemAtCell(test%2==0?30:20,fromRocket?source:destination);
            propeller=(OriginalPropellerBoardItem)game.CreateItemAtCell(10,fromRocket?destination:source);
            Require(rocket.Orientation==test%2,"Both actual rocket orientations are exercised");
            movesBefore=game.Moves.LeftMoves;swapsBefore=game.AcceptedSwaps;refillsBefore=game.CreatedRefills;goalsBefore=board.Goals.GetGoalLeftCount(11);
            started=Time.realtimeSinceStartup;steadyAt=arrivalAt=-1f;sample=0;distance=0;reserved=null;start=destination.GetViewPosition();
            carried=wobbled=hidden=delayed=partsStarted=impactCaptured=carryCaptured=false;
        }
        private static void Clear(OriginalGridCell cell)
        {var old=(OriginalMatchBoardItem)cell.Mediation.CurrentItem;old.Mediator.ClearFromAllRegisteredCells();old.RecycleView();}
        private static void Tick()
        {try{Step();}catch(Exception e){report.failure=e.ToString();File.WriteAllText(ReportPath,JsonUtility.ToJson(report,true));Cleanup();throw;}}
        private static void Step()
        {
            if(!EditorApplication.isPlaying||frame==Time.frameCount)return;frame=Time.frameCount;
            Require(Time.realtimeSinceStartup-started<30f,"Carry/rocket damage/refill and native view-tail routines complete without operation resets: case="+test+
                ", routines="+board.Presentation.Coroutines.Count+", rocketActive="+rocket.ItemView.gameObject.activeSelf);
            if(sample<3)
            {
                var p=(sample==0?source:destination).GetViewPosition();samples.Position=new Vector2(p.x,p.y);
                samples.Down=sample==0;samples.Held=sample<2;samples.Up=sample==2;listener.ManualUpdate();sample++;return;
            }
            int operation=rocket.Orientation==0?10:11,guided=rocket.Orientation==0?2:1;
            if(propeller.IsInFlight)
            {
                Require(propeller.TargetExplodeData.Trigger==(rocket.Orientation==0?11:10)&&rocket.CurrentCell==null,"Native orientation-specific trigger and detached rocket");
                Require(rocket.ItemView.transform.parent==propeller.ItemView.transform,"Actual rocket prefab is carried by the propeller");
                Require(game.State.Operations.Container.GetCount(3)>0&&game.State.Operations.Container.GetCount(operation)>0,"Flight and orientation-specific combo operation owners are active");
                int offset=rocket.ItemView.GetSorting().Order-propeller.ItemView.GetSorting().Order;Require(offset==10||offset==-10,"Native alternating front/back ordering");
                reserved=propeller.TargetItem;Require(reserved!=null&&reserved.GetExtraIncomingExplodeCount(guided)>0,"Selected target retains its incoming line reservation");
                if(rocket.Orientation==1)
                {
                    Require(propeller.FrozenColumnCellCount==board.Grid.Height,"Vertical flight owns a cross-fall freeze for every target-column cell");
                    int x=reserved.CurrentCell.X;
                    for(int y=0;y<board.Grid.Height;y++)Require(board.Grid.GetCell(x,y).CrossFallFreezeCount>0,"Actual target column blocks cross fall during flight");
                }
                else Require(propeller.FrozenColumnCellCount==0,"Horizontal flight does not freeze a column");
                carried=true;distance=Mathf.Max(distance,Vector3.Distance(start,propeller.ItemView.transform.position));
                wobbled|=Mathf.Abs(rocket.ItemView.rootTransform.GetPosition().x)>.01f;
                if(test==0&&!carryCaptured&&distance>.5f){Capture("propeller_rocket_carrying.png");carryCaptured=true;}
            }
            if(carried&&!propeller.IsInFlight)
            {
                if(arrivalAt<0f)arrivalAt=Time.time;
                hidden|=!rocket.IsExploded&&!rocket.ItemView.resetView.gameObject.activeSelf;
                delayed|=rocket.IsExploded&&game.State.Operations.Container.GetCount(operation)>0;
                if(rocket.IsExploded&&!partsStarted)
                {partsStarted=true;firstPartStart=rocket.ItemView.FirstPart.transform.position;secondPartStart=rocket.ItemView.SecondPart.transform.position;}
                if(partsStarted)
                {
                    report.first_part_distance[test]=Mathf.Max(report.first_part_distance[test],Vector3.Distance(firstPartStart,rocket.ItemView.FirstPart.transform.position));
                    report.second_part_distance[test]=Mathf.Max(report.second_part_distance[test],Vector3.Distance(secondPartStart,rocket.ItemView.SecondPart.transform.position));
                    if(test==0&&!impactCaptured&&game.State.Operations.Container.GetCount(4)>0){Capture("propeller_rocket_impact.png");impactCaptured=true;}
                }
            }
            if(!carried||!rocket.IsExploded||propeller.IsInFlight||game.State.HasAnyOperation()||game.State.GetSpecialOperationCount()!=0||!game.Fall.IsAllItemsSteady||
                game.Auto.IsThereAWaitingMatch||board.FirstSwapAction.IsSwapping||board.SecondSwapAction.IsSwapping||board.Presentation.Coroutines.Count!=0){steadyAt=-1f;return;}
            if(steadyAt<0f){steadyAt=Time.time;return;}if(Time.time-steadyAt<1.4f)return;
            Require(wobbled&&distance>.25f&&hidden&&delayed,"Actual carrying/root motion, hidden arrival delay and delayed operation release run");
            Require(report.first_part_distance[test]>1f&&report.second_part_distance[test]>1f,"Both actual rocket halves travel along the selected line");
            Require(reserved.GetExtraIncomingExplodeCount(guided)==0&&propeller.FrozenColumnCellCount==0,"Line reservation and delayed column freezes release");
            Require(game.Moves.LeftMoves==movesBefore-1&&game.AcceptedSwaps==swapsBefore+1,"One gesture consumes exactly one move");
            Require(game.Session.SpecialItemCounts.Combos.TryGetValue(9,out int count)&&count==test+1,"One native combo9 session count per swap");
            Require(game.CreatedRefills>refillsBefore&&game.CanUserMakeMove()&&listener.TouchDisableCount==0,"Actual line damage/refill completes and player input is restored");
            if(test==0)Require(board.Goals.GetGoalLeftCount(11)<goalsBefore,"Actual rocket line damage advances box goals");
            int props=0,rockets=0,holes=0;
            for(int x=0;x<board.Grid.Width;x++)for(int y=0;y<board.Grid.Height;y++)
            {
                var cell=board.Grid.GetCell(x,y);var item=cell.Mediation.CurrentItem;
                Require(!cell.IsFallFrozen&&cell.CrossFallFreezeCount==0,"No swap, column or rocket-flight freeze remains");
                if(item==null)
                {
                    var above=cell.Neighbors.Get(1);while(above!=null&&above.Mediation.CurrentItem==null)above=above.Neighbors.Get(1);
                    Require(boxes[x,y]&&above?.Mediation.CurrentItem is OriginalBoxBoardItem&&!fallQueries.IsFallPossibleFromCellToCell(above,cell)&&
                        fallQueries.GetNextItemOfCell(above,cell)==null&&fallQueries.FindTargetItemFromCrossCell(cell,cell,0)==null&&fallQueries.FindTargetItemFromCrossCell(cell,cell,2)==null,
                        "Only native unreachable original-box cavities may stay empty");holes++;continue;
                }
                Require(ReferenceEquals(item,cell.Mediation.NextItem)&&ReferenceEquals(item,cell.Mediation.TargetItem),"Refilled mediation slots agree");
                if(item is OriginalPropellerBoardItem)props++;if(item is OriginalRocketBoardItem)rockets++;
            }
            Require(board.Presentation.Pool.GetSpawnedCount(10)==props&&board.Presentation.Pool.GetSpawnedCount(8)==rockets,
                "All borrowed carrier/rocket views belong to remaining board models: case="+test+
                ", props="+props+"/"+board.Presentation.Pool.GetSpawnedCount(10)+", rockets="+rockets+"/"+board.Presentation.Pool.GetSpawnedCount(8)+
                ", carrierActive="+propeller.ItemView.gameObject.activeSelf+", rocketActive="+rocket.ItemView.gameObject.activeSelf);
            Require(board.Presentation.Pool.GetSpawnedCount(13)==1,"Native global combo13 particle instance is reused");
            report.flight_distance[test]=distance;report.native_box_cavities[test]=holes;report.completed_combos++;
            if(++test<16){Begin();return;}
            report.passed=true;report.left_moves=game.Moves.LeftMoves;report.goals_left=board.Goals.GetGoalLeftCount(11);Capture("propeller_rocket_refilled.png");
            File.WriteAllText(ReportPath,JsonUtility.ToJson(report,true)+"\n");Cleanup();
        }
        private static void Capture(string name)
        {
            var camera=device.PointerCamera;var target=new RenderTexture(Mathf.RoundToInt(768*camera.aspect),768,24);var previous=camera.targetTexture;var active=RenderTexture.active;Texture2D pixels=null;
            try{camera.targetTexture=target;camera.Render();RenderTexture.active=target;pixels=new Texture2D(target.width,target.height,TextureFormat.RGB24,false);pixels.ReadPixels(new Rect(0,0,target.width,target.height),0,0);pixels.Apply();File.WriteAllBytes(Path.Combine(Application.dataPath,"../"+name),pixels.EncodeToPNG());}
            finally{camera.targetTexture=previous;RenderTexture.active=active;if(pixels!=null)UnityEngine.Object.Destroy(pixels);UnityEngine.Object.Destroy(target);}
        }
        private static void Cleanup(){EditorApplication.update-=Tick;if(device!=null&&game!=null&&device.Listener==null)device.Bind(game);}
        private static void Require(bool value,string message){if(!value)throw new InvalidOperationException(message);}
        private sealed class Samples:IOriginalGameTouchInput
        {public Vector2 Position;public bool Down,Held,Up;public Vector2 GetMouseWorldPosition()=>Position;public bool GetMouseButtonDown()=>Down;public bool GetMouseButton()=>Held;public bool GetMouseButtonUp()=>Up;}
        [Serializable]private sealed class Report
        {
            public bool passed;public string failure;public int completed_combos,left_moves,goals_left;
            public float[] flight_distance=new float[16],first_part_distance=new float[16],second_part_distance=new float[16];public int[] native_box_cavities=new int[16];
            public string scope="Sixteen production-factory propeller/rocket fixtures in one formal first-board scene cover both rocket orientations, item roles, swipe axes and directions through recorded input samples. Actual carrier/root motion/sorting, triggers10/11, operations3/10/11, incoming reservations, vertical column cross-fall freeze and delayed release, hidden arrival wait, two real rocket halves/damage/goals/refill, one move and view/global13 pool ownership are checked. No exhaustive natural creation, physical input, extra-combo, helper/static/jelly/conditional goal/king/charger, original rendered, cross-platform or full settlement/lifecycle parity claim.";
        }
    }
}
