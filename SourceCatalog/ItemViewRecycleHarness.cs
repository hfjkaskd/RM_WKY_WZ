using System;
using System.Collections.Generic;
using Whitebox.Foundation;
public static class ItemViewRecycleHarness
{
    static readonly List<string> trace=new List<string>();static int count,fail;static bool has,hidden,mask,rootMissing,alive;static int recycled;
    static void Step(string value){trace.Add(value);if(++count==fail)throw new InvalidOperationException();}
    sealed class View:IOriginalItemRecycleView
    {
        public bool IsAlive=>alive;
        public bool IsRootHidden{get{if(rootMissing)throw new NullReferenceException();return hidden;}}
        public bool IsFillMaskEnabled=>mask;
        public void Show(){Step("show");hidden=false;}
        public void DisableFillMask(){Step("mask");mask=false;}
        public void ResetAnimationToDefault()=>Step("reset");
    }
    sealed class Model:IOriginalItemViewRecycleModel
    {
        public View View;
        public bool HasView{get{Step("has");return has;}set{Step("set");has=value;}}
        public IOriginalItemRecycleView GetItemView(){Step("view");return View;}
    }
    sealed class Factory:IOriginalItemViewRecycleFactory
    {public void Recycle(IOriginalItemRecycleView view){Step("recycle");recycled++;}}
    public static void Main()
    {
        string line;while((line=Console.ReadLine())!=null)
        {
            var v=Array.ConvertAll(line.Split(','),int.Parse);trace.Clear();count=0;recycled=0;has=v[0]!=0;alive=v[1]==1;hidden=v[2]!=0;mask=v[3]!=0;rootMissing=v[4]!=0;fail=v[6];
            var model=new Model{View=v[1]==0?null:new View()};var factory=v[5]==0?new Factory():null;bool failed=false;
            try{for(int i=0;i<v[7];i++)OriginalItemViewRecycle.Recycle(model,factory);}catch(Exception){failed=true;}
            Console.WriteLine((failed?1:0)+"#"+(has?1:0)+","+(hidden?1:0)+","+(mask?1:0)+","+recycled+"#"+string.Join(";",trace));
        }
    }
}
