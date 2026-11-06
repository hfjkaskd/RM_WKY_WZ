using System;
using System.Collections.Generic;
using Whitebox.Foundation;
namespace Whitebox.Foundation
{
    // Counter fixture; native counter instructions execute in the ARM64 run.
    public sealed class OriginalDisableCounter
    {public int Count;public bool IsEnabled=>Count>0;public void Enable(){Count=unchecked(Count+1);}public void Disable(){if(Count!=0)Count=unchecked(Count-1);}public void Reset(){Count=0;}}
}
public static class Harness
{
    public static readonly List<string> Trace=new List<string>();
    private class ActionBase:OriginalFlowAction
    {
        public int Id,Kind;public bool Multiple,One,Sync,CancelComplete,Delayed;
        public override int Type=>Kind;
        public override bool SupportsMultiple()=>Multiple;
        public override bool SupportsOneExceptRunning()=>One;
        public override void Execute(){Trace.Add("execute,"+Id);if(Sync)Complete();}
        public override void Cancel(OriginalFlowAction action){Trace.Add("cancel,"+Id+","+((ActionBase)action).Id);if(CancelComplete)Complete();}
        public override void Complete(){Trace.Add("complete,"+Id);base.Complete();}
        public override void Replace(OriginalFlowAction action){Trace.Add("replace,"+Id+","+((ActionBase)action).Id);base.Replace(action);}
        public override void Clear(){Trace.Add("clear,"+Id);}
    }
    private sealed class A:ActionBase{}private sealed class B:ActionBase{}private sealed class C:ActionBase{}private sealed class D:ActionBase{}
    private sealed class Context:IOriginalFlowExecutorContext
    {
        public bool Delay;
        public bool ShouldDelayAutoActions()=>Delay;
        public bool IsInDelayedActions(OriginalFlowAction action)=>((ActionBase)action).Delayed;
        public void DelayAutoAction(OriginalFlowAction action){Trace.Add("delay,"+((ActionBase)action).Id);}
    }
    public static void Main()
    {
        string line;while((line=Console.ReadLine())!=null)
        {
            Trace.Clear();var parts=line.Split('|');var configs=parts[0].Split(';');var actions=new ActionBase[configs.Length];
            for(int i=0;i<actions.Length;i++)
            {
                var v=Array.ConvertAll(configs[i].Split(','),int.Parse);ActionBase a=v[0]==0?(ActionBase)new A():v[0]==1?new B():v[0]==2?new C():new D();
                a.Id=i;a.Kind=v[1];a.Multiple=v[2]!=0;a.One=v[3]!=0;a.Sync=v[4]!=0;a.CancelComplete=v[5]!=0;a.Delayed=v[6]!=0;
                a.OnReplace+=()=>Trace.Add("replaced,"+a.Id);actions[i]=a;
            }
            var context=new Context();var flow=new OriginalFlowExecutor(context);
            foreach(string operation in parts[1].Split(';'))
            {
                var v=Array.ConvertAll(operation.Split(','),int.Parse);int command=v[0],value=v[1];
                if(command==0)flow.Push(actions[value]);
                else if(command==1)flow.ManualUpdate();
                else if(command==2){var running=flow.GetRunningAction();if(running!=null)running.Complete();}
                else if(command==3)flow.Pause();else if(command==4)flow.Resume();
                else if(command==5)flow.DisableNextActionOnPush();else if(command==6)flow.EnableNextActionOnPush();
                else if(command==7)flow.StartNextActionMode();else if(command==8)flow.FinishNextActionMode();
                else if(command==9)flow.Clear(value!=0);else context.Delay=value!=0;
                var current=(ActionBase)flow.GetRunningAction();
                Trace.Add("state,"+(flow.IsPaused?1:0)+","+(current==null?-1:current.Id)+","+flow.PendingCount+","+flow.AutoCount+","+flow.NextCount);
            }
            Console.WriteLine(string.Join(";",Trace));
        }
    }
}
