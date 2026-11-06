using System;
using System.IO;
using UnityEditor;
using UnityEngine;
using Object=UnityEngine.Object;

namespace Whitebox.Foundation.Editor
{
    // Actual pooled prefab + Unity Update + normal FallComponent. Grid/clock,
    // roof owner and SFX context are diagnostic inputs, never production hooks.
    public static class FallPresentationVerification
    {
        private sealed class TouchBlocker : IOriginalStaticTouchItem
        { public bool IsTouchBlocking=>true;public bool IsTouchBlockingExceptChain=>true; }
        private sealed class Inputs : IOriginalFallMotionServices, IOriginalFallSquashContext
        {
            public sealed class Roof : IOriginalRoofFallItem
            {
                public int Notifications;
                public int StaticItemType=>7;
                public OriginalGridCell CurrentCell{get;set;}
                public bool DoesBlockFall=>false;
                public bool IsTouchBlocking=>false;
                public bool IsTouchBlockingExceptChain=>false;
                public int RemainingExplodeCount()=>throw new InvalidOperationException();
                public void NotifyForFallUnderneath(){Notifications++;}
            }
            public readonly Roof RoofItem=new Roof();
            public readonly OriginalCellGrid Grid=new OriginalCellGrid(1,3);
            public OriginalMatchItemSpriteView View;
            public OriginalFallComponentState Fall;
            public OriginalRootTransform Root;
            public Transform Inner;
            public Inputs(OriginalMatchItemSpriteView view)
            {
                View=view;Root=view.GetComponentInChildren<OriginalRootTransform>(true);
                Inner=view.GetComponentInChildren<OriginalFallSquash>(true).transform;
                for(int y=0;y<3;y++)Grid.SetCell(0,y,new OriginalGridCell(0,y,1));Grid.CacheNeighbors();
                var item=new OriginalMatchBoardItem(20001,OriginalTiledItemTypes.AsItemType(1),1);
                Fall=new OriginalFallComponentState(item);Fall.BindMotionServices(this);item.FallComponent=Fall;
                Grid.GetCell(0,2).Mediation.SetAllItems(item);Grid.GetCell(0,0).Mediation.SetTargetItem(item);
                Grid.GetCell(0,2).AddStaticItem(RoofItem);
                View.BindModel(item);View.BindFallContext(this);
                View.Bind(Resources.Load<Sprite>("Recovered/Sprite/Items-MatchItem-shield"),
                    Resources.Load<Sprite>("Recovered/Sprite/Items-MatchItem-shadow_shield"),
                    new OriginalMatchViewPose{BaseOffset=new Vector3(.007f,-.01f,0),ShadowParentOffset=new Vector3(-.017f,-.015f,0),ShadowColor=new Color(1,1,1,.37f)});
                View.Position=new OriginalSwapPoint(4,10,0);
            }
            public bool IsScroll=>false;
            public float DeltaTime=>1f/60f;
            public float LevelTime=>Time.time;
            public float LevelDeltaTime=>DeltaTime;
            public float MaxFallSpeed=>20f;
            public IOriginalFallItemView GetView(OriginalBoardItem item)=>View;
            public OriginalSwapPoint GetItemPosition(OriginalBoardItem item)=>View.Position;
            public OriginalSwapPoint GetCellPosition(OriginalGridCell cell)=>new OriginalSwapPoint(4,8+cell.Y,0);
            public bool IsInCurrentBoard(OriginalGridCell cell)=>true;
            public OriginalSwapSorting GetCrossSorting(OriginalBoardItem item)=>OriginalCrossFallSorting.Get(item,View.GetSorting().Layer);
            public bool HasNonFallBlockingAboveStaticItemInLevel()=>false;
            public bool IsUnderNonFallBlockingAboveStaticItem(OriginalBoardItem item)=>OriginalItemFallQueries.IsUnderNonFallBlockingAboveStaticItem(item);
            public bool HasStaticItem(OriginalGridCell cell,int type)=>cell.StaticTouch.HasStaticItem(type);
            public void PlayFallHit()=>throw new InvalidOperationException();
        }
        [Serializable] private sealed class Report
        {
            public bool passed=true,reused_same_prefab;
            public string timestamp_utc=DateTime.UtcNow.ToString("o");
            public int frames,cycles,mask_renderers,attached_renderers;
            public float minimum_width=1,maximum_width=1,minimum_y,maximum_y;
            public string scope="Actual pooled MatchItemPrefab implements the normal fall-view contract: mask/sorting/squash, state and roof notification. Actual FallComponent drives that view directly through two Unity/pool cycles. Real static registries and ItemModel queries execute; a diagnostic roof owner checks notification count, under-static clears highlighting and refreshes normal sorting. Grid/clock/no-static-level/SFX remain diagnostic inputs; concrete roof models, complete production manager, creation and player-driven resolution remain pending. No original pixel reference pair was captured.";
        }
        private static Inputs input;
        private static OriginalLevelPresentation presentation;
        private static SpriteRenderer[] renderers;
        private static OriginalMatchItemSpriteView attachment;
        private static Report report;
        private static int frames,lastFrame,available;
        private static float startedAt;
        [MenuItem("Recovery/Verify Fall Presentation (Play Mode)")]
        public static void Run()
        {
            if(!EditorApplication.isPlaying || input!=null)throw new InvalidOperationException("Run once in Play mode.");
            presentation=Object.FindObjectOfType<OriginalLevelPresentation>();
            Require(presentation!=null && presentation.Pool!=null,"Running pool");
            available=presentation.Pool.GetPoolLength(2);report=new Report();
            try
            {
                input=new Inputs(presentation.SpawnMatchView());
                var cell=input.Grid.GetCell(0,2);var item=cell.Mediation.CurrentItem;var blocker=new TouchBlocker();
                cell.StaticTouch.AboveItems.Add(blocker);var cross=input.GetCrossSorting(item);
                Require(cross.Order==1550 && cross.SortEverything,"Original cross sorting under touch blocker");
                input.View.UpdateSorting(cross);Require(input.View.GetSorting().Order==1550,"Actual match renderer receives blocked cross sorting");
                cell.StaticTouch.AboveItems.Remove(blocker);cross=input.GetCrossSorting(item);
                Require(cross.Order==7000 && cross.SortEverything,"Original unobstructed cross sorting");
                input.View.UpdateSorting(cross);Require(input.View.GetSorting().Order==7000,"Actual match renderer receives normal cross sorting");
                input.View.Highlight(true);Require(input.View.IsHighlighted,"Highlight before under-static transition");
                input.View.SetIsUnderNonFallBlockingAboveStaticItem(true);
                Require(!input.View.IsHighlighted && !input.View.IsHighlightActive && input.View.IsUnderNonFallBlockingAboveStaticItem,"Under-static disables highlight");
                var sorting=input.View.GetSorting();Require(sorting.Order==1000 && !sorting.SortEverything,"Under-static restores original normal match sorting");
                input.View.SetIsUnderNonFallBlockingAboveStaticItem(false);
                renderers=input.View.GetComponentsInChildren<SpriteRenderer>(true);Require(renderers.Length==3,"Original three renderers including inactive overlay");
                input.View.EnableFillMask();Require(input.View.IsFillMaskEnabled,"Fill mask flag");
                foreach(var r in renderers)Require(r.maskInteraction==SpriteMaskInteraction.VisibleOutsideMask,"All renderers use original mask interaction");
                attachment=presentation.SpawnMatchView();input.Root.AddChild(attachment.transform);
                var attached=attachment.GetComponentsInChildren<SpriteRenderer>(true);Require(attached.Length==3,"Attachment uses the configured pooled prefab");
                input.View.EnableFillMask();foreach(var r in attached)Require(r.maskInteraction==SpriteMaskInteraction.VisibleOutsideMask,"Runtime attachments included on enable");
                input.View.DisableFillMask();foreach(var r in attached)Require(r.maskInteraction==SpriteMaskInteraction.None,"Runtime attachments included on disable");
                report.attached_renderers=attached.Length;presentation.Pool.Recycle(attachment);attachment=null;input.View.EnableFillMask();
                report.mask_renderers=renderers.Length;frames=0;lastFrame=Time.frameCount;startedAt=Time.realtimeSinceStartup;
                EditorApplication.update+=Tick;
            }catch{Cleanup();throw;}
        }
        private static void Tick()
        {
            try{TickState();}catch{Cleanup();throw;}
        }
        private static void TickState()
        {
            if(!EditorApplication.isPlaying){Cleanup();return;}
            if(Time.realtimeSinceStartup-startedAt>20)throw new InvalidOperationException("Fall presentation did not finish within twenty seconds.");
            if(lastFrame==Time.frameCount)return;lastFrame=Time.frameCount;frames++;report.frames++;
            input.Fall.ManualUpdate();
            var scale=input.Inner.localScale;var local=input.Inner.localPosition;
            report.minimum_width=Mathf.Min(report.minimum_width,scale.x);report.maximum_width=Mathf.Max(report.maximum_width,scale.x);
            report.minimum_y=Mathf.Min(report.minimum_y,local.y);report.maximum_y=Mathf.Max(report.maximum_y,local.y);
            if(frames<100)return;
            Require(input.Fall.IsItemSteady && input.View.Position.Y==8,"Native motion settles at target");
            Require(input.Inner.localScale==Vector3.one && input.Inner.localPosition==Vector3.zero,"View bounce returns to original scale/position");
            Require(input.RoofItem.Notifications==1,"Native fall-animation callback notifies actual roof registry owner once");
            Require(!input.View.IsFillMaskEnabled,"Motion disables filling mask");
            foreach(var r in renderers)Require(r.maskInteraction==SpriteMaskInteraction.None,"All mask interactions cleared");
            report.cycles++;
            if(report.cycles==1)
            {
                var previous=input.View;input.Root.HideView();input.Root.SetPosition(new Vector3(.2f,.3f,.4f));input.Root.SetScale(new Vector3(2,2,2));
                presentation.Pool.Recycle(previous);var recycled=presentation.SpawnMatchView();
                Require(ReferenceEquals(previous,recycled),"Real pool reuses same view");report.reused_same_prefab=true;
                input=new Inputs(recycled);Require(!input.Root.IsHidden && input.Inner.gameObject.activeSelf,"Init restores RootTransform visibility");
                Require(input.Root.GetPosition()==Vector3.zero && input.Root.GetScale()==Vector3.one,"Init resets outer animation root");
                frames=0;input.View.EnableFillMask();return;
            }
            Require(report.minimum_width<.99f && report.maximum_width>1.001f && report.minimum_y<0 && report.maximum_y>0,"Observed fall narrowing, compression and full bounce");
            Cleanup();Require(presentation.Pool.GetPoolLength(2)==available,"Pool count restored after two cycles");
            File.WriteAllText(Path.Combine(Directory.GetParent(Application.dataPath).FullName,"fall_presentation_unity_verification.json"),JsonUtility.ToJson(report,true));
            Debug.Log("Fall presentation verification passed.");
        }
        private static void Cleanup()
        {
            EditorApplication.update-=Tick;
            if(attachment!=null && presentation!=null && presentation.Pool!=null)presentation.Pool.Recycle(attachment);
            attachment=null;
            if(input!=null && presentation!=null && presentation.Pool!=null && input.View!=null)presentation.Pool.Recycle(input.View);
            input=null;
        }
        private static void Require(bool value,string label){if(!value)throw new InvalidOperationException(label);}
    }
}
