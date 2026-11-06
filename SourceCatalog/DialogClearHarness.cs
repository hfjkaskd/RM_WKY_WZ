using System;
using System.Collections.Generic;
using Whitebox.Foundation;
namespace Whitebox.Foundation
{
    public struct OriginalDialogConfig { }
    public interface IOriginalUiDialogManager { }
    public abstract class OriginalUiDialog
    {
        public int DialogId;
        public virtual void SetSortingOffset(int n) { }
        public virtual void SetTouchSorting(int n) { }
        public virtual OriginalDialogConfig GetConfig()=>new OriginalDialogConfig();
        public abstract void ForceClose();
    }
}
public static class DialogClearHarness
{
    private static List<string> trace=new List<string>();
    private static OriginalDialogManager manager;private static Dialog[] dialogs;private static int mode;
    private sealed class Dialog:OriginalUiDialog
    {
        public int Index;
        public override void ForceClose()
        {
            trace.Add("force,"+Index);
            switch(mode)
            {
                case 1:manager.DialogClosed(this);break;
                case 2:manager.ShowDialog(dialogs[2]);break;
                case 3:throw new InvalidOperationException();
                case 4:manager.HardReset();break;
                case 5:manager.DialogClosed(this);manager.ShowDialog(this);break;
            }
        }
    }
    private sealed class Context:IOriginalDialogManagerContext
    {
        public void DisableTouchForGame(){} public void CloseTooltip(bool animate){}
        public void ShowBackground(int id,OriginalDialogConfig c,int offset,bool first){}
        public void HideBackground(int id,OriginalDialogConfig c,Action hidden){}
        public void EnableTouchForGame(){}
    }
    public static void Main()
    {
        string line;
        while((line=Console.ReadLine())!=null)
        {
            trace.Clear();var parts=line.Split(';');mode=int.Parse(parts[0]);manager=new OriginalDialogManager(new Context());
            dialogs=new[]{new Dialog{Index=0},new Dialog{Index=1},new Dialog{Index=2}};bool failed=false;
            try{for(int i=1;i<parts.Length;i++)switch(parts[i][0])
            {case 'S':manager.ShowDialog(dialogs[parts[i][1]-'0']);break;case 'C':manager.CloseDialogs();break;case 'A':manager.ClearDialogs();break;case 'H':manager.HardReset();break;}}
            catch(Exception){failed=true;}
            Console.WriteLine((failed?1:0)+"#"+manager.Count+","+manager.SortingOrder+","+dialogs[0].DialogId+","+dialogs[1].DialogId+","+dialogs[2].DialogId+"#"+string.Join(";",trace));
        }
    }
}
