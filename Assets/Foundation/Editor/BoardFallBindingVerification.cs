using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;
using Whitebox.Foundation.ConfigData;
using Object = UnityEngine.Object;

namespace Whitebox.Foundation.Editor
{
    // Diagnostic holes only. Motion/view/grid/clock/counts/audio are the actual
    // production owners; automatic target planning/refill is not installed here.
    public static class BoardFallBindingVerification
    {
        [Serializable] private sealed class Report
        {
            public bool passed = true, actual_world_positions, straight_landing, cross_landing, fall_audio_played;
            public bool preparation_inputs_verified;
            public int bound_match_views, frames;
            public string timestamp_utc = DateTime.UtcNow.ToString("o");
            public string scope = "Formal board motion services, all36 match bindings, actual cell world/local Transforms, current-frame quantized delta, level initial static counts and GameSfx. A formal item completes straight and cross falls through its already-bound native FallComponent in Unity frames, with actual cross-hit audio timestamp and source. Diagnostic target/hole setup is restored afterward. Full automatic FallManager/creation, player swap/resolution/refill and active session remain pending.";
        }
        private static OriginalInitialBoardView board;
        private static OriginalGameAudioHost audio;
        private static OriginalGridCell[] cells;
        private static OriginalBoardItem[] saved;
        private static Vector3[] positions;
        private static OriginalMatchItemSpriteView moving;
        private static OriginalFallComponentState fall;
        private static Report report;
        private static int phase, lastFrame;
        private static float started, beforeAudio;
        [MenuItem("Recovery/Verify Board Fall Binding (Play Mode)")]
        public static void Run()
        {
            if (!EditorApplication.isPlaying || board != null) throw new InvalidOperationException("Run once in Play mode.");
            try
            {
                board = Object.FindObjectOfType<OriginalInitialBoardView>(); audio = Object.FindObjectOfType<OriginalGameAudioHost>();
                Require(board != null && board.FallMotion != null && audio != null, "Actual board and audio owners");
                report = new Report();
                VerifyPreparationInputs(); report.preparation_inputs_verified = true;
                Require(board.InitialItemAmounts.ItemAmounts.Length == 116 && board.InitialItemAmounts.StaticItemAmounts.Length == 20 && board.InitialItemAmounts.GetItemAmount(6) == 36, "Native initial box pool count");
                Require(!board.FallMotion.HasNonFallBlockingAboveStaticItemInLevel(), "First level has no honey/roof initial amounts");
                for (int x = 0; x < board.Grid.Width; x++) for (int y = 0; y < board.Grid.Height; y++)
                {
                    var cell = board.Grid.GetCell(x,y); var p = board.FallMotion.GetCellPosition(cell);
                    Require(new Vector3(p.X,p.Y,p.Z) == cell.CellView.transform.position && board.FallMotion.IsInCurrentBoard(cell), "Actual formal world positions/board identity");
                    var item = cell.Mediation.CurrentItem;
                    if (item is OriginalMatchBoardItem)
                    {
                        Require(ReferenceEquals(board.FallMotion.GetView(item), item.SwapView), "Formal match view identity");
                        ((OriginalFallComponentState)item.FallComponent).ArrangeIsUnderNonFallBlockingAboveStaticItem();
                        report.bound_match_views++;
                    }
                }
                Require(report.bound_match_views == 36 && board.FallMotion.LevelTime == board.Clock.Time && board.FallMotion.MaxFallSpeed == 20f, "Actual initial match bindings/clock/speed");
                var parent = board.Presentation.CellParent; var original = parent.position;
                try
                {
                    parent.position += new Vector3(.125f,-.25f,.5f);
                    var p = board.FallMotion.GetCellPosition(board.Grid.GetCell(4,7));
                    Require(new Vector3(p.X,p.Y,p.Z) == board.Grid.GetCell(4,7).GetViewPosition(), "World position follows transformed cell parent including Z");
                }
                finally { parent.position = original; }
                report.actual_world_positions = true;
                cells = new[] { board.Grid.GetCell(4,7), board.Grid.GetCell(4,6), board.Grid.GetCell(4,5), board.Grid.GetCell(3,6) };
                saved = new OriginalBoardItem[cells.Length]; positions = new Vector3[cells.Length];
                for (int i=0;i<cells.Length;i++)
                {
                    saved[i] = cells[i].Mediation.CurrentItem;
                    var view = (OriginalMatchItemSpriteView)saved[i].SwapView; positions[i] = view.transform.position;
                }
                moving = (OriginalMatchItemSpriteView)saved[0].SwapView; fall = (OriginalFallComponentState)saved[0].FallComponent;
                phase = 0; PrepareFall(); started = Time.realtimeSinceStartup; lastFrame = Time.frameCount;
                EditorApplication.update += Tick;
            }
            catch { Cleanup(); throw; }
        }
        private static void PrepareFall()
        {
            for (int i=0;i<cells.Length;i++)
            {
                cells[i].Mediation.ClearAllItems();
                ((OriginalMatchItemSpriteView)saved[i].SwapView).gameObject.SetActive(i==0);
            }
            moving.transform.position = positions[0];
            cells[0].Mediation.SetAllItems(saved[0]);
            cells[phase==0?2:3].Mediation.SetTargetItem(saved[0]);
            beforeAudio = Mathf.Max(audio.Playback.GetClipLastPlayedTime(146), audio.Playback.GetClipLastPlayedTime(147));
        }
        private static void VerifyPreparationInputs()
        {
            var level = new FTiledLevel
            {
                Grid = new FTiledGrid { Width=2,Height=1,Items=new[]{1,1} },
                Goals = new[] {new FGoalData{Goal=11,Count=7,BoardId=0},new FGoalData{Goal=11,Count=3,BoardId=1}},
                Limits = new[] {new FLimitData{Limit=102,Count=13,BoardId=0},new FLimitData{Limit=102,Count=7,BoardId=1}},
                Counts = new[] {new FItemCount{Type=6,Count=9,BoardId=0},new FItemCount{Type=6,Count=3,BoardId=1},
                    new FItemCount{Type=-18,Count=7,BoardId=0},new FItemCount{Type=-18,Count=10,BoardId=1},
                    new FItemCount{Type=1,Count=12,BoardId=0},new FItemCount{Type=1,Count=33,BoardId=1}},
                Curtains = new[]{new FCurtainData{CellCount=3},new FCurtainData{CellCount=8}},
                ItemCurtains = new[]{new FItemCurtainData{CellCount=23}},
                Cryptexes = new[]{new FCryptexData{CellCount=17}},
                Treasuremaps = new[]{new FCellGroupData{Cells=new FPoint[5]},new FCellGroupData{Cells=new FPoint[2]}},
                Drills = new FDrillData[2],ChainCount=3,JellyReceivableCellCount=8,
                PouchConfig = new FPouchConfig{IsAvailableFalling=true},
                Coils = new[]{new FCurtainData{Target=9},new FCurtainData{Target=4}},
                Logs = new[]{new FCurtainData{Target=5},new FCurtainData{Target=14}},
                ColorMixers = new[]{new FCurtainData{Target=21}}
            };
            var parser = new OriginalLevelGoalPreparation(level,null);
            var goals = new List<Dictionary<int,OriginalGoalTuple>>(); var limits = new List<Dictionary<int,OriginalLimitTuple>>(); var counts = new List<Dictionary<int,int>>();
            parser.ParseGoals(goals); parser.ParseLimits(limits); parser.ParseCounts(counts);
            var amounts = new OriginalInitialItemAmounts(n=>n,()=>throw new InvalidOperationException());
            OriginalLevelItemAmountPreparation.Create(level,amounts,goals,counts,limits);
            Require(amounts.GetItemAmount(6)==7 && amounts.GetItemAmount(1)==33 && amounts.GetStaticItemAmount(18)==10,"Maximum board counts and goals retain separate native rules");
            Require(amounts.GetStaticItemAmount(5)==11 && amounts.GetStaticItemAmount(19)==23 && amounts.GetStaticItemAmount(16)==17,"Original CellCount fields");
            Require(amounts.GetItemAmount(29)==2 && amounts.GetStaticItemAmount(6)==3 && amounts.GetStaticItemAmount(3)==8 && amounts.GetItemAmount(37)==15,"Original drill/chain/jelly/pouch fields");
            Require(amounts.GetStaticItemAmount(8)==7 && amounts.GetItemAmount(48)==7 && amounts.GetItemAmount(62)==13 && amounts.GetItemAmount(80)==19 && amounts.GetItemAmount(110)==20 && amounts.GetItemAmount(113)==21,"Original group lengths/targets and turtle limits");
            level.Counts = new[]{new FItemCount{Type=6,Count=1},new FItemCount{Type=6,Count=2}};
            bool duplicateRejected=false;try{parser.ParseCounts(new List<Dictionary<int,int>>());}catch(ArgumentException){duplicateRejected=true;}
            Require(duplicateRejected,"Duplicate original count keys are rejected");
        }
        private static void Tick()
        {
            try
            {
                if(!EditorApplication.isPlaying) { Cleanup(); return; }
                if(lastFrame==Time.frameCount)return; lastFrame=Time.frameCount; report.frames++;
                Require(Time.realtimeSinceStartup-started<5f, "Formal fall completes within five seconds");
                Require(board.FallMotion.LevelDeltaTime == OriginalLevelClock.QuantizeDelta(Time.deltaTime), "Current Unity frame delta");
                fall.ManualUpdate();
                float afterAudio = Mathf.Max(audio.Playback.GetClipLastPlayedTime(146), audio.Playback.GetClipLastPlayedTime(147));
                if(afterAudio>beforeAudio && audio.SoundSource.isPlaying)report.fall_audio_played=true;
                var target = cells[phase==0?2:3];
                if(!fall.IsItemSteady || !ReferenceEquals(saved[0].Mediator.CurrentCell,target.Mediation))return;
                Require(moving.transform.position == target.GetViewPosition(), "Formal item lands at actual target Transform");
                if(phase==0)
                {
                    report.straight_landing=true; phase=1; PrepareFall(); started=Time.realtimeSinceStartup; return;
                }
                report.cross_landing=true;
                Require(report.fall_audio_played,"Real cross-fall GameSfx timestamp and AudioSource playback");
                File.WriteAllText(Path.Combine(Directory.GetParent(Application.dataPath).FullName,"board_fall_binding_unity_verification.json"),JsonUtility.ToJson(report,true));
                Cleanup();Debug.Log("Board fall binding Unity verification passed.");
            }
            catch { Cleanup(); throw; }
        }
        private static void Cleanup()
        {
            EditorApplication.update -= Tick;
            if(cells != null && saved != null)
            {
                for(int i=0;i<cells.Length;i++) cells[i].Mediation.ClearAllItems();
                for(int i=0;i<cells.Length;i++)
                {
                    if(saved[i]==null)continue;
                    cells[i].Mediation.SetAllItems(saved[i]);
                    var view=(OriginalMatchItemSpriteView)saved[i].SwapView;
                    if(view!=null) { view.transform.position=positions[i]; view.gameObject.SetActive(true); view.ArrangeSelfSorting(); }
                }
            }
            board=null;audio=null;cells=null;saved=null;positions=null;moving=null;fall=null;
        }
        private static void Require(bool value,string label) { if(!value)throw new InvalidOperationException(label); }
    }
}
