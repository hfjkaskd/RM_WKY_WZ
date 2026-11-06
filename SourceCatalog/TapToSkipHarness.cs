using System;
using System.Collections.Generic;
using Whitebox.Foundation;
namespace Whitebox.Foundation { public sealed class OriginalActiveLevelData { public int Number; } }
public static class TapToSkipHarness
{
    private static readonly List<string> trace=new List<string>();
    private static int missing;
    private sealed class Context:IOriginalTapToSkipContext,IOriginalTapToSkipPresentation
    {
        public OriginalActiveLevelData Current;public bool Has;
        public OriginalActiveLevelData GetCurrentSession()
        {trace.Add("current");if(missing==1||missing==2)throw new NullReferenceException();return missing==3?null:Current;}
        public bool HasRemainingMovesManager(){trace.Add("has");return Has;}
        public void TriggerTapToSkipClick(){if(missing==8)throw new NullReferenceException();trace.Add("dispatch");if(missing==6)throw new InvalidOperationException();}
        public void SetInteractionEnabled(bool enabled){if(missing==4)throw new NullReferenceException();trace.Add("interaction,"+(enabled?1:0));}
        public void SetTextEnabled(bool enabled){if(missing==5)throw new NullReferenceException();trace.Add("text,"+(enabled?1:0));}
        public void ArrangeTextPosition()=>trace.Add("arrange");
        public void ShowText(){trace.Add("visual");if(missing==7)throw new NullReferenceException();}
    }
    public static void Main()
    {
        string line;
        while((line=Console.ReadLine())!=null)
        {
            trace.Clear();var parts=line.Split(';');var values=Array.ConvertAll(parts[0].Split(','),int.Parse);
            missing=values[1];var a=new OriginalActiveLevelData{Number=values[0]};var b=new OriginalActiveLevelData{Number=2};
            var context=new Context{Current=a,Has=values[2]!=0};var model=new OriginalTapToSkip(context,context);bool failed=false;
            try
            {
                for(int i=1;i<parts.Length;i++)
                {
                    string c=parts[i];switch(c[0])
                    {
                        case 'H':model.Hide();break;case 'E':model.EnableSkipWithoutText();break;case 'T':model.ShowSkipText();break;case 'C':model.Clicked();break;
                        case 'Q':trace.Add("result,"+(model.ShouldShowText()?1:0));break;
                        case 'N':a.Number=int.Parse(c.Substring(1));break;case 'A':b.Number=int.Parse(c.Substring(1));break;
                        case 'S':context.Current=c[1]=='0'?a:b;break;case 'M':context.Has=c[1]=='1';break;
                    }
                }
            }
            catch(Exception){failed=true;}
            Console.WriteLine((failed?1:0)+"#"+model.State+","+(model.IsTextShown?1:0)+"#"+string.Join(";",trace));
        }
    }
}
