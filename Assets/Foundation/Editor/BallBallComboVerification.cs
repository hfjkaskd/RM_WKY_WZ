using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

namespace Whitebox.Foundation.Editor
{
    // Explicit factory fixtures only; all swaps, damage and fall work run in
    // the unchanged formal scene through the production input/board owners.
    public static class BallBallComboVerification
    {
        private static OriginalInitialBoardView board;
        private static OriginalPlayableBoard game;
        private static OriginalGameTouchInput device;
        private static OriginalGameTouchListener listener;
        private static Samples samples;
        private static OriginalGridCell source,destination;
        private static OriginalLightballBoardItem first,second;
        private static readonly List<Hit> hits=new List<Hit>();
        private static readonly List<int> waits=new List<int>();
        private static readonly Report report=new Report();
        private static int test,frame,sample,movesBefore,swapsBefore,refillsBefore,goalsBefore;
        private static float started,steadyAt;
        private static Vector3 boardPosition;
        private static bool merging,waving,shaking,captured,maskClosed,maskOpened;
        private static string ReportPath=>Path.Combine(Application.dataPath,"../live_ball_ball_combo_verification.json");
        public static void Run()
        {
            board=UnityEngine.Object.FindObjectOfType<OriginalInitialBoardView>();
            game=UnityEngine.Object.FindObjectOfType<FoundationController>().Gameplay;
            device=UnityEngine.Object.FindObjectOfType<OriginalGameTouchInput>();device.Unbind();
            samples=new Samples();listener=new OriginalGameTouchListener(samples,game);
            game.BallBallCombo.CellTriggered+=OnHit;game.BallBallCombo.RingWaitStarted+=OnWait;
            boardPosition=board.BoardRoot.localPosition;
            File.WriteAllText(ReportPath,"{\"passed\":false,\"status\":\"running\"}\n");
            Begin();EditorApplication.update+=Tick;
        }
        private static void Begin()
        {
            bool vertical=test>=4,reverse=test%4>=2,found=false;
            // Different centers, both axes and both gesture directions.
            for(int y=7;y>=5&&!found;y--)for(int i=0;i<(vertical?9:8);i++)
            {
                int x=test%2==0?i:(vertical?8:7)-i;
                var a=board.Grid.GetCell(x,y);var b=board.Grid.GetCell(vertical?x:x+1,vertical?y-1:y);
                if(!(a.Mediation.CurrentItem is OriginalMatchBoardItem)||!(b.Mediation.CurrentItem is OriginalMatchBoardItem))continue;
                source=reverse?b:a;destination=reverse?a:b;found=true;break;
            }
            Require(found,"Adjacent regular cells for a production-factory fixture");
            Clear(source);Clear(destination);
            first=(OriginalLightballBoardItem)game.CreateItemAtCell(50,source);
            second=(OriginalLightballBoardItem)game.CreateItemAtCell(50,destination);
            movesBefore=game.Moves.LeftMoves;swapsBefore=game.AcceptedSwaps;refillsBefore=game.CreatedRefills;
            goalsBefore=board.Goals.GetGoalLeftCount(11);hits.Clear();waits.Clear();sample=0;
            merging=waving=shaking=captured=maskClosed=maskOpened=false;started=Time.realtimeSinceStartup;steadyAt=-1f;
        }
        private static void Clear(OriginalGridCell cell)
        {var item=(OriginalMatchBoardItem)cell.Mediation.CurrentItem;item.Mediator.ClearFromAllRegisteredCells();item.RecycleView();}
        private static void OnHit(int radius,OriginalCellPoint point)
        {
            Require(game.BallBallCombo.IsExplosionActive&&listener.TouchDisableCount>=1,"Radial owner retains touch blocking: case="+test+", hit="+hits.Count+", active="+game.BallBallCombo.IsExplosionActive+", touches="+listener.TouchDisableCount+", sameListener="+ReferenceEquals(listener,game.TouchListener));
            // A naturally created lightball hit by the ring owns an additional
            // native strategy lock. It must finish and release that lock too.
            report.max_touch_locks[test]=Math.Max(report.max_touch_locks[test],listener.TouchDisableCount);
            Require(game.BallBallCombo.ExplodeData.Trigger==16,"Normal BallBall uses its own native trigger16");
            for(int x=0;x<board.Grid.Width;x++)for(int y=0;y<board.Grid.Height;y++)
                Require(board.Grid.GetCell(x,y).IsFallFrozen,"Every cell stays frozen during radial damage");
            hits.Add(new Hit{radius=radius,x=point.X,y=point.Y});waving=true;
        }
        private static void OnWait(int radius,float seconds)
        {
            float expected=(6f-Math.Min(.01f*radius*radius,1.5f))/160f;
            Require(Math.Abs(seconds-expected)<1e-7f,"Native radius-dependent gameplay delay");waits.Add(radius);
        }
        private static void Tick()
        {
            try{Step();}
            catch(Exception e){report.failure=e.ToString();File.WriteAllText(ReportPath,JsonUtility.ToJson(report,true));Cleanup();throw;}
        }
        private static void Step()
        {
            if(!EditorApplication.isPlaying||frame==Time.frameCount)return;frame=Time.frameCount;
            Require(Time.realtimeSinceStartup-started<30f,"Native merge/radial/refill completes: case="+test);
            if(sample<3)
            {
                Vector3 p=(sample==0?source:destination).GetViewPosition();samples.Position=new Vector2(p.x,p.y);
                samples.Down=sample==0;samples.Held=sample<2;samples.Up=sample==2;listener.ManualUpdate();sample++;return;
            }
            shaking|=Vector3.Distance(boardPosition,board.BoardRoot.localPosition)>.001f;
            var activeView=UnityEngine.Object.FindObjectOfType<OriginalBallBallComboView>();
            if(activeView!=null&&activeView.IsReady&&activeView.black.color.a>.59f)
            {
                if(!maskClosed&&activeView.sortingGroup.gameObject.activeSelf&&activeView.NormalizedTime>.5f)
                {CheckMaskPixels(activeView,false);maskClosed=true;}
                if(!maskOpened&&!activeView.sortingGroup.gameObject.activeSelf&&activeView.maskTransform.localScale.x>=20f)
                {CheckMaskPixels(activeView,true);maskOpened=true;if(test==0)Capture("ball_ball_wave.png");}
            }
            if(game.BallBallCombo.IsComboActive&&!game.BallBallCombo.IsExplosionActive&&hits.Count==0)
            {
                Require(listener.TouchDisableCount==1&&source.IsFallFrozen&&destination.IsFallFrozen,"Merge owns touch and both swap-cell freezes");
                Require(first.CurrentCell==null&&second.CurrentCell==null,"Prepare detaches both original lightball models");
                Require(board.Presentation.Pool.GetSpawnedCount(31)==1&&board.Presentation.Pool.GetSpawnedCount(33)==1,"Real combo31 and use33 prefabs are borrowed");
                var view=UnityEngine.Object.FindObjectOfType<OriginalBallBallComboView>();
                Require(view!=null&&view.IsReady&&view.animator.isActiveAndEnabled&&view.sortingGroup.sortingOrder==100,"Original animator and sorting group are active");
                merging=true;
                if(test==0&&!captured&&view.NormalizedTime>.2f){Capture("ball_ball_merging.png");captured=true;}
            }
            if(!merging||!waving||game.BallBallCombo.IsComboActive||game.State.HasAnyOperation()||game.State.GetSpecialOperationCount()!=0||!game.Fall.IsAllItemsSteady||
                game.Auto.IsThereAWaitingMatch||board.FirstSwapAction.IsSwapping||board.SecondSwapAction.IsSwapping){steadyAt=-1f;return;}
            if(steadyAt<0f){steadyAt=Time.time;return;}
            // The native mask/view and two-second particles have separate tails.
            if(Time.time-steadyAt<.4f||board.Presentation.Pool.GetSpawnedCount(31)!=0||board.Presentation.Pool.GetSpawnedCount(33)!=0||board.Presentation.Pool.GetSpawnedCount(34)!=0)return;
            Require(shaking&&Vector3.Distance(boardPosition,board.BoardRoot.localPosition)<.0001f,"Actual board root shakes and restores its position");
            Require(maskClosed&&maskOpened,"GPU pixels verify the native central hole and expanding reveal");
            Require(listener.TouchDisableCount==0&&game.CanUserMakeMove(),"Touch/operation release restores real input");
            Require(game.Moves.LeftMoves==movesBefore-1&&game.AcceptedSwaps==swapsBefore+1,"One gesture consumes exactly one move");
            Require(game.Session.SpecialItemCounts.Combos.TryGetValue(1,out int count)&&count==test+1,"Native combo1 session usage increments once");
            Require(game.CreatedRefills>refillsBefore,"Actual radial damage feeds native gravity/refill");
            if(test==0)Require(board.Goals.GetGoalLeftCount(11)<goalsBefore,"Actual box goals advance");
            CheckOrder();int balls=0;
            for(int x=0;x<board.Grid.Width;x++)for(int y=0;y<board.Grid.Height;y++)
            {
                var cell=board.Grid.GetCell(x,y);var item=cell.Mediation.CurrentItem;
                Require(item!=null&&!cell.IsFallFrozen&&cell.CrossFallFreezeCount==0,"All 72 cells are occupied and unfrozen");
                Require(ReferenceEquals(item,cell.Mediation.TargetItem)&&ReferenceEquals(item,cell.Mediation.NextItem),"All mediation slots settle together");
                if(item is OriginalLightballBoardItem)balls++;
            }
            Require(board.Presentation.Pool.GetSpawnedCount(23)==balls,"Only actual remaining lightballs own pool23 views");
            report.triggered_cells[test]=hits.Count;report.ring_waits[test]=waits.Count;report.completed_combos++;
            if(++test<8){Begin();return;}
            report.passed=true;report.left_moves=game.Moves.LeftMoves;report.goals_left=board.Goals.GetGoalLeftCount(11);
            Capture("ball_ball_refilled.png");File.WriteAllText(ReportPath,JsonUtility.ToJson(report,true)+"\n");Cleanup();
        }
        private static void CheckOrder()
        {
            var expected=new List<Hit>();
            for(int x=0;x<board.Grid.Width;x++)for(int y=0;y<board.Grid.Height;y++)
            {
                int dx=destination.X-x,dy=destination.Y-y;
                expected.Add(new Hit{radius=(int)Math.Round((float)Math.Sqrt(dx*dx+dy*dy)*2f),x=x,y=y});
            }
            expected.Sort((a,b)=>a.radius!=b.radius?a.radius.CompareTo(b.radius):a.x!=b.x?a.x.CompareTo(b.x):a.y.CompareTo(b.y));
            Require(hits.Count==expected.Count,"Exactly one radial hit for every formal cell");int ring=0;
            for(int i=0;i<expected.Count;i++)
            {
                var a=hits[i];var b=expected[i];Require(a.radius==b.radius&&a.x==b.x&&a.y==b.y,"Native ascending-radius, column-first order: hit="+i);
                if(i>0&&b.radius!=expected[i-1].radius){Require(waits[ring++]==expected[i-1].radius,"Every non-final ring waits once");}
            }
            Require(waits.Count==ring,"No extra final-ring wait");
        }
        private static void Capture(string name)
        {
            Camera camera=device.PointerCamera;var target=new RenderTexture(Mathf.RoundToInt(768*camera.aspect),768,24);
            var previous=camera.targetTexture;var active=RenderTexture.active;Texture2D pixels=null;
            try{camera.targetTexture=target;camera.Render();RenderTexture.active=target;pixels=new Texture2D(target.width,target.height,TextureFormat.RGB24,false);
                pixels.ReadPixels(new Rect(0,0,target.width,target.height),0,0);pixels.Apply();File.WriteAllBytes(Path.Combine(Application.dataPath,"../"+name),pixels.EncodeToPNG());}
            finally{camera.targetTexture=previous;RenderTexture.active=active;if(pixels!=null)UnityEngine.Object.Destroy(pixels);UnityEngine.Object.Destroy(target);}
        }
        private static void CheckMaskPixels(OriginalBallBallComboView view,bool expanded)
        {
            Require(view.black.sharedMaterial.shader.name=="Alpha Masked/Sprites Alpha Masked - World Coords"&&view.black.sharedMaterial.shader.isSupported,"Original mask shader is supported and bound to the actual black renderer");
            var target=new RenderTexture(129,129,24,RenderTextureFormat.ARGB32,RenderTextureReadWrite.Linear);
            var command=new CommandBuffer();Texture2D pixels=null;var previous=RenderTexture.active;
            try
            {
                command.SetRenderTarget(target);command.ClearRenderTarget(true,true,Color.clear);
                command.SetViewProjectionMatrices(Matrix4x4.Translate(-view.transform.position),GL.GetGPUProjectionMatrix(Matrix4x4.Ortho(-10,10,-10,10,-1,1),true));
                command.DrawRenderer(view.black,view.black.sharedMaterial);Graphics.ExecuteCommandBuffer(command);
                RenderTexture.active=target;pixels=new Texture2D(129,129,TextureFormat.RGBA32,false,true);
                pixels.ReadPixels(new Rect(0,0,129,129),0,0);pixels.Apply();
                float center=pixels.GetPixel(64,64).a,near=pixels.GetPixel(77,64).a;
                Require(center<.02f,"Native world-space mask leaves its center transparent: alpha="+center);
                Require(expanded?near<.05f:near>.5f,"Native reveal changes actual GPU alpha two world units from center: expanded="+expanded+", alpha="+near);
                report.mask_pixel_checks++;
            }
            finally{RenderTexture.active=previous;command.Release();if(pixels!=null)UnityEngine.Object.Destroy(pixels);UnityEngine.Object.Destroy(target);}
        }
        private static void Cleanup()
        {EditorApplication.update-=Tick;game.BallBallCombo.CellTriggered-=OnHit;game.BallBallCombo.RingWaitStarted-=OnWait;if(device.Listener==null)device.Bind(game);}
        private static void Require(bool value,string message){if(!value)throw new InvalidOperationException(message);}
        private sealed class Hit{public int radius,x,y;}
        private sealed class Samples:IOriginalGameTouchInput
        {public Vector2 Position;public bool Down,Held,Up;public Vector2 GetMouseWorldPosition()=>Position;public bool GetMouseButtonDown()=>Down;public bool GetMouseButton()=>Held;public bool GetMouseButtonUp()=>Up;}
        [Serializable]private sealed class Report
        {
            public bool passed;public string failure;public int completed_combos,left_moves,goals_left,mask_pixel_checks;
            public int[] triggered_cells=new int[8],ring_waits=new int[8],max_touch_locks=new int[8];
            public string scope="Eight explicit normal/no-jelly production-factory double-lightball fixtures in one formal scene use recorded input samples through the actual listener. Both axes/directions and multiple centers exercise merge animator/prefab pools, touch/freeze owners, 72 radial hits and native ring order/delays, box damage/goals, one move, refill, nested lightball lock release and subsequent input. Actual black-renderer GPU alpha verifies the central mask hole and expanding reveal. Shader equations/render state are translated from extracted original GLES programs; this does not prove cross-platform GPU equivalence or complete rendered parity. No exhaustive natural creation, physical input, super-lightball, jelly, concrete winning/conditional event owners or complete lifecycle parity claim.";
        }
    }
}
