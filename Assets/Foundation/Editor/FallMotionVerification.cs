using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace Whitebox.Foundation.Editor
{
    // Integrates actual target selection, scheduling, filling admission and
    // per-item physics. View/static/SFX callbacks are diagnostic inputs only.
    public static class FallMotionVerification
    {
        private sealed class View : IOriginalFallItemView, IOriginalItemSwapView
        {
            public OriginalSwapPoint Position { get; set; }
            public OriginalSwapPoint LocalPosition => Position;
            public bool IsFillMaskEnabled { get; private set; } = true;
            public bool IsUnderNonFallBlockingAboveStaticItem { get; private set; }
            public int Started, Ended, Animations, States, MaskDisables;
            public bool IsSwapping=>false;
            public void DisableFillMask() { IsFillMaskEnabled=false; MaskDisables++; }
            public void ArrangeSelfSorting() { }
            public void UpdateSorting(OriginalSwapSorting sorting) { }
            public void UpdateFallState(int state,float previousSpeed,float maxSpeed) { States++; }
            public void FallStarted() { Started++; }
            public void FallEnded() { Ended++; }
            public void FallAnimationStarted() { Animations++; }
            public void SetIsUnderNonFallBlockingAboveStaticItem(bool under) { IsUnderNonFallBlockingAboveStaticItem=under; }
        }
        private sealed class Inputs : IOriginalFallMotionServices, IOriginalFallTargetServices,
            IOriginalFallTraversal, IOriginalFillingItemCreation
        {
            public readonly OriginalCellGrid Grid=new OriginalCellGrid(2,3);
            public readonly Dictionary<int,OriginalFillingCell> Filling=new Dictionary<int,OriginalFillingCell>();
            public readonly Dictionary<OriginalBoardItem,View> Views=new Dictionary<OriginalBoardItem,View>();
            public readonly List<OriginalFallComponentState> Falls=new List<OriginalFallComponentState>();
            private readonly OriginalBoardGridAccess access;
            private readonly IOriginalGridIteratorStrategy vertical,horizontal;
            public int Frame,Creates,Hits;
            public Inputs()
            {
                access=new OriginalBoardGridAccess(Grid,OriginalBoardLayout.Create(2,3,new int[6],false,false));
                for(int y=0;y<3;y++)for(int x=0;x<2;x++)
                {
                    OriginalGridCell cell=y==2?new OriginalFillingCell(x,y,2,0,1,false,false,0):new OriginalGridCell(x,y,1);
                    Grid.SetCell(x,y,cell);if(y==2)Filling.Add(x,(OriginalFillingCell)cell);
                }
                Grid.CacheNeighbors();
                for(int x=0;x<2;x++)Grid.GetCell(x,1).Mediation.SetAllItems(NewItem(x,1));
                vertical=new OriginalTotalBottomLeftVerticalStrategy(access);horizontal=new OriginalTotalBottomLeftHorizontalStrategy(access);
            }
            private OriginalBoardItem NewItem(float x,float y)
            {
                var item=new OriginalMatchBoardItem(Views.Count+1,1,1);var fall=new OriginalFallComponentState(item);
                fall.BindMotionServices(this);item.FallComponent=fall;Falls.Add(fall);var view=new View{Position=new OriginalSwapPoint(x,y,0)};Views.Add(item,view);item.BindSwapView(view);return item;
            }
            public OriginalBoardItem CreateItemForFillingCellAt(float x,float y,OriginalFillingCell cell)
            { Creates++;var item=NewItem(x,y);cell.AddCreatedItem(item);return item; }
            public float LevelTime=>Frame/60f;
            public float LevelDeltaTime=>1f/60f;
            public float MaxFallSpeed=>20f;
            public IOriginalFallItemView GetView(OriginalBoardItem item)=>Views[item];
            public OriginalSwapPoint GetItemPosition(OriginalBoardItem item)=>Views[item].Position;
            public OriginalSwapPoint GetCellPosition(OriginalGridCell cell)=>new OriginalSwapPoint(cell.X,cell.Y,0);
            public bool IsInCurrentBoard(OriginalGridCell cell)=>access.IsInCurrentBoard(cell);
            public OriginalSwapSorting GetCrossSorting(OriginalBoardItem item)=>OriginalCrossFallSorting.Get(item,0);
            public bool HasNonFallBlockingAboveStaticItemInLevel()=>false;
            public bool IsUnderNonFallBlockingAboveStaticItem(OriginalBoardItem item)=>false;
            public bool HasStaticItem(OriginalGridCell cell,int type)=>false;
            public void PlayFallHit(){Hits++;}
            public int CurrentBoardIndex=>0;
            public bool IsMultipleBoardLevel=>false;
            public bool CanCurrentBoardCrossFall(int x)=>true;
            public bool IsLevelWon=>false;
            public bool IsFallBlocked(OriginalGridCell cell)=>false;
            // ItemModel.IsAvailableForFall 0x2b462f8 rejects an item whose
            // mediator has no CurrentCell. Execute the actual native gates
            // against real match capabilities and the cell's static registry.
            public bool IsAvailableForFall(OriginalBoardItem item,OriginalGridCell to)=>item.IsAvailableForFall(to);
            public bool CanFallCross(OriginalBoardItem item)=>true;
            public bool IsSpecialItem(OriginalBoardItem item)=>false;
            public bool IsCrossFallBlocking(OriginalBoardItem item)=>false;
            public bool IsThereIceCubeInLevel()=>false;
            public IOriginalFallIceCubeHelper GetIceCubeHelper()=>throw new InvalidOperationException();
            public bool HasCrossFallFreeze(OriginalGridCell cell)=>false;
            public bool HasIncomingExploder(OriginalGridCell cell)=>false;
            public IReadOnlyDictionary<int,OriginalFillingCell> CurrentBoardFillingCells=>Filling;
            public bool IsPortalLevel=>false;
            public int BoardStartY=>access.BoardStartY;
            public IOriginalGridIteratorStrategy GetIteratorStrategy(int type)=>type==4?vertical:type==5?horizontal:throw new InvalidOperationException();
        }
        [Serializable] private sealed class Report
        {
            public bool passed=true;public string timestamp_utc=DateTime.UtcNow.ToString("o");
            public int frames,created_items,settled_items;
            public string scope="Actual planner, manager, grid/board traversal, filling placement/admission, complete per-item motion, native ItemModel fall availability with real match capability/current-cell/static registry queries, mediation and operation accounting run together for 180 simulated Unity frames. View/creation/SFX and planner obstacle configuration remain diagnostic inputs. No fixture is installed into production; player-driven resolution/cascades and application-owned view/mask/squash composition remain incomplete.";
        }
        [MenuItem("Recovery/Verify Fall Motion")]
        public static void Run()
        {
            var input=new Inputs();var columns=new OriginalFallRefillColumns();var operations=new OriginalGameOperations(()=>input.Frame);
            var planner=new OriginalFallTargetPlanner(input,columns);var manager=new OriginalFallManager(columns,planner,input,operations);manager.InitializeGrid(input);
            for(int i=0;i<180;i++){input.Frame=i;manager.ManualUpdate();}
            Require(manager.IsAllItemsSteady && operations.Container.GetCount(0)==0,"All items settle and falling operation releases");
            Require(input.Creates==4 && input.Falls.Count==6,"Finite filling admission creates exactly the remaining four items");
            for(int y=0;y<3;y++)for(int x=0;x<2;x++)
            {
                var cell=input.Grid.GetCell(x,y);var item=cell.Mediation.CurrentItem;
                Require(item!=null && ReferenceEquals(item,cell.Mediation.NextItem) && ReferenceEquals(item,cell.Mediation.TargetItem),"Current/next/target converge");
                var view=input.Views[item];Require(view.Position.X==x && view.Position.Y==y && view.Position.Z==0,"Native movement ends at exact cell position");
                Require(view.Started==1 && view.Ended==1 && view.Animations==1 && view.States==2,"One motion lifecycle per item");
            }
            foreach(var cell in input.Filling.Values)Require(!cell.HasCreatedItem,"Filling admission clears held references");
            File.WriteAllText(Path.Combine(Directory.GetParent(Application.dataPath).FullName,"fall_motion_unity_verification.json"),JsonUtility.ToJson(new Report{frames=180,created_items=input.Creates,settled_items=input.Falls.Count},true));
            Debug.Log("Fall motion integration verification passed.");
        }
        private static void Require(bool value,string label){if(!value)throw new InvalidOperationException(label);}
    }
}
