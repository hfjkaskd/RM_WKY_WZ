// Engine/factory fixture for the original-first-board integration verifier.
// Runs the production special creator, wait, base model activation and real
// cell mediation; concrete item render/animation implementation is a boundary.
using System;
using System.Collections;
using System.Collections.Generic;
using Whitebox.Foundation;

namespace Whitebox.Foundation { public class OriginalSpecialItemCreationParticles { } }
class PipelineSpecialView : OriginalItemSwapView, IOriginalSpecialCreationView
{
    public readonly PipelineSpecial Model;
    public bool Active = true;
    public int Animations, ParticleAttachments;
    public OriginalSwapSorting Sorting;
    public PipelineSpecialView(PipelineSpecial model) { Model = model; }
    public void SetGameObjectActive(bool active) { Active = active; }
    public void UpdateSorting(OriginalSwapSorting value) { Sorting = value; }
    public void PlayCreationAnimation(float start, bool sound)
    {
        if (!Model.IsActive || !Active || start != 0f || !sound || ++Animations != 1 || Sorting.Order != 1000)
            throw new Exception("Special activation/creation animation order");
    }
    public void SetIsUnderNonFallBlockingAboveStaticItem(bool under) { throw new Exception("First level has no above static item"); }
    public void SetCreationParticles(OriginalSpecialItemCreationParticles particles, bool above)
    {
        if (particles == null || above || Animations != 1 || ++ParticleAttachments != 1)
            throw new Exception("Special creation particle attachment order");
    }
}
class PipelineSpecial : OriginalSpecialBoardItem
{
    public readonly PipelineSpecialView CreationView;
    public PipelineSpecial(int id,int tiledId,PipelineSpecialServices services) : base(id,tiledId,()=>services.Elapsed,services)
    {
        CreationView=new PipelineSpecialView(this);
        BindSwapView(CreationView);FallComponent=new OriginalFallComponentState(this);MarkViewCreated();
    }
    public override IOriginalSpecialCreationView View => CreationView;
}
class PipelineSpecialServices : IOriginalSpecialItemCreationServices,IOriginalSpecialItemStaticState
{
    public static int TotalCreated;
    private readonly Services parent;
    private readonly OriginalSpecialItemCreator creator;
    private readonly List<IEnumerator> routines = new List<IEnumerator>();
    private readonly List<PipelineSpecial> created = new List<PipelineSpecial>();
    private readonly OriginalSpecialItemCreationParticles particles = new OriginalSpecialItemCreationParticles();
    public float Elapsed;
    private int increments, records, missions, madness;
    public int Count => created.Count;
    public PipelineSpecialServices(Services parent)
    {
        this.parent=parent;
        creator=new OriginalSpecialItemCreator(this,new OriginalMatchMergeSettings { Duration=.2f, ExplodeCompleteRatio=.85f });
    }
    public void Create(OriginalMatchGroupSnapshot group,OriginalGridCell cell,bool auto) { creator.CreateSpecialItemForMatchGroup(group,cell,auto); }
    public IOriginalSpecialCreationItem CreateItemAtCell(int tiledId,OriginalGridCell cell)
    {
        if (cell.Mediation.CurrentItem!=null || cell.Mediation.NextItem!=null || cell.Mediation.TargetItem!=null)
            throw new Exception("Special factory called before matched model release");
        var model=new PipelineSpecial(10000+created.Count,tiledId,this);
        cell.Mediation.SetAllItems(model);created.Add(model);TotalCreated++;return model;
    }
    public void ManualStartCoroutine(IEnumerator routine)
    {
        routines.Add(routine);
        if (!routine.MoveNext() || !(routine.Current is OriginalGameplayWait)) throw new Exception("Initial gameplay wait missing");
    }
    public object CreateGameplayWait(float duration)
    {
        if (duration!=.2f*.85f) throw new Exception("Creation delay does not derive from merge completion");
        return new OriginalGameplayWait(duration,()=>OriginalLevelClock.QuantizeDelta(1f/60f));
    }
    public int ItemSortingLayer => 231352099;
    public bool IsPlaying => true;
    public bool IsSessionStarted => true;
    public sbyte CurrentLevelDifficulty => 0;
    public bool IsAboveStaticSpreading(OriginalGridCell cell) => false;
    public bool IsUnderHoney(OriginalBoardItem item) => false;
    public bool IsUnderRoof(OriginalBoardItem item) => false;
    public void IncrementItemCreation(int type,bool auto)
    {
        var model=created[created.Count-1];
        if (auto || type!=model.ItemType || model.IsActive || model.CreationView.Active || !model.CurrentCell.IsFallFrozen
            || new OriginalItemSwapAvailability().IsAvailableForSwap(model)) throw new Exception("Special creation reservation state");
        increments++;
    }
    public void SpecialItemCreated(IOriginalSpecialCreationItem model,OriginalGridCell cell,int delay,int count,bool sticker)
    {
        if (model.CurrentCell!=cell || delay!=17 || count!=1 || !sticker) throw new Exception("Special session record");records++;
    }
    public void TryCollectMadness(OriginalGridCell cell,int type,int count,int delay,IOriginalSpecialCreationItem model,bool remaining)
    {
        if (cell!=model.CurrentCell || type<1 || count<4 || delay!=17 || remaining) throw new Exception("Special madness record");madness++;
    }
    public void AddMissionEvent(int type,sbyte difficulty,int count)
    {
        if(type!=created[created.Count-1].ItemType || difficulty!=0 || count!=1)throw new Exception("Special mission record");missions++;
    }
    public void UsefulMoveMade() { parent.UsefulMoveMade(); }
    public void ReportMissingCreationCell() { throw new Exception("Created first-level special lost its reserved cell"); }
    public OriginalSpecialItemCreationParticles PlayAppearParticle(IOriginalSpecialCreationView view) { return particles; }
    public void Tick()
    {
        Elapsed+=OriginalLevelClock.QuantizeDelta(1f/60f);
        for(int i=0;i<routines.Count;)
        {
            var routine=routines[i];var wait=routine.Current as OriginalGameplayWait;
            if(wait!=null && wait.keepWaiting) { i++;continue; }
            if(!routine.MoveNext()) routines.RemoveAt(i);else i++;
        }
    }
    public void VerifyCompleted(int expected)
    {
        if(Count!=expected || routines.Count!=0 || increments!=expected || records!=expected || missions!=expected || madness!=expected)
            throw new Exception("Special creator completion/record counts");
        for(int i=0;i<created.Count;i++)
        {
            var model=created[i];var cell=model.CurrentCell;
            if(cell==null || cell.Mediation.CurrentItem!=model || cell.Mediation.NextItem!=model || cell.Mediation.TargetItem!=model
                || !model.IsActive || !model.CreationView.Active || model.CreationView.Animations!=1
                || model.CreationView.ParticleAttachments!=1 || !new OriginalItemSwapAvailability().IsAvailableForSwap(model))
                throw new Exception("Final special model/view/mediation state");
        }
    }
}
