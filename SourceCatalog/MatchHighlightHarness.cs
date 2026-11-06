using System;using System.Collections.Generic;using Whitebox.Foundation;
class Verify:IOriginalMatchHighlightSurface,IOriginalMatchTypeProvider{
public int MatchType{get;set;}public float Alpha;public bool Active;public List<string> Trace=new List<string>();
public static uint Bits(float x)=>BitConverter.ToUInt32(BitConverter.GetBytes(x),0);
static float Float(string s)=>BitConverter.ToSingle(BitConverter.GetBytes(uint.Parse(s)),0);
public void SetHighlightActive(bool active){Active=active;Trace.Add("active:"+(active?1:0));}
public float HighlightAlpha{get{Trace.Add("get");return Alpha;}set{Trace.Add("set:"+Bits(value));Alpha=value;}}
static void Main(){string line;while((line=Console.ReadLine())!=null){var a=line.Split(',');var surface=new Verify{MatchType=3,Alpha=.2f};var h=new OriginalMatchHighlight(surface);h.BindModel(surface);
if(a[2]=="1")h.Highlight(true);surface.Trace.Clear();surface.Alpha=.2f;surface.Active=a[2]=="1";
surface.MatchType=int.Parse(a[3]);h.BindModel(a[4]=="1"?surface:null);h.IsUnderNonFallBlockingAboveStaticItem=a[1]=="1";
string result="";if(a[0]=="H")h.Highlight(a[5]=="1");else if(a[0]=="R")h.SetHighlightRatio(Float(a[5]));else if(a[0]=="M")result=Bits(h.GetMaxHighlightRatio()).ToString();else if(a[0]=="G")result=Bits(h.GetHighlightRatio()).ToString();
Console.WriteLine(string.Join("|",surface.Trace)+"#"+(h.IsHighlighted?1:0)+","+(surface.Active?1:0)+","+Bits(surface.Alpha)+","+result);}}}
