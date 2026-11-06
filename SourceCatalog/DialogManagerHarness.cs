using System;
using System.Collections;
using System.Collections.Generic;
using Whitebox.Foundation;
namespace UnityEngine { public struct Vector3 { public float x,y,z; } }
namespace Whitebox.Foundation
{
    public interface IOriginalFlowDialog { void Show(); }
    public abstract class OriginalFlowAction { public virtual void Complete(){} }
}
public static class DialogManagerHarness
{
    public static List<string> Trace=new List<string>();
    private sealed class Dialog:OriginalUiDialog
    {
        public readonly int Index;public Dialog(int index):base(null,null){Index=index;}
        public override OriginalDialogConfig GetConfig(){Trace.Add("config,"+Index);return new OriginalDialogConfig();}
        public override void SetSortingOffset(int offset)=>Trace.Add("sort,"+Index+","+offset);
        public override void SetTouchSorting(int order)=>Trace.Add("touch,"+Index+","+order);
        protected override void OnShow(UnityEngine.Vector3 value){}
    }
    private sealed class Context:IOriginalDialogManagerContext
    {
        public readonly List<Action> Pending=new List<Action>();
        public void DisableTouchForGame()=>Trace.Add("disable");
        public void CloseTooltip(bool animate)=>Trace.Add("tooltip,"+(animate?1:0));
        public void ShowBackground(int id,OriginalDialogConfig c,int offset,bool first)=>Trace.Add("background,"+id+","+offset+","+(first?1:0));
        public void HideBackground(int id,OriginalDialogConfig c,Action hidden){Trace.Add("hide,"+id);Pending.Add(hidden);}
        public void EnableTouchForGame()=>Trace.Add("enable");
    }
    public static void Main()
    {
        string line;
        while((line=Console.ReadLine())!=null)
        {
            Trace.Clear();var context=new Context();var manager=new OriginalDialogManager(context);
            var dialogs=new[]{new Dialog(0),new Dialog(1),new Dialog(2)};
            manager.OnDialogShown+=d=>Trace.Add("shown,"+((Dialog)d).Index+","+manager.Count+","+manager.SortingOrder);
            manager.OnDialogClosed+=d=>Trace.Add("closed,"+((Dialog)d).Index+","+manager.Count+","+manager.SortingOrder);
            bool failed=false;
            foreach(string command in line.Split(';'))
            {
                try
                {
                    int index=command[1]-'0';
                    switch(command[0])
                    {
                        case 'S':manager.ShowDialog(dialogs[index]);break;
                        case 'C':Trace.Add("return,"+(manager.CloseDialog(dialogs[index])?1:0));break;
                        case 'D':manager.DialogClosed(dialogs[index]);break;
                        case 'H':var callback=context.Pending[index];context.Pending.RemoveAt(index);callback();break;
                    }
                }
                catch(Exception){failed=true;break;}
            }
            Console.WriteLine((failed?1:0)+"#"+manager.Count+","+manager.SortingOrder+","+dialogs[0].DialogId+","+dialogs[1].DialogId+","+dialogs[2].DialogId+"#"+string.Join(";",Trace));
        }
    }
}
