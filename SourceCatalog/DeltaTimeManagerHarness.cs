using System;
using System.Collections.Generic;
using Whitebox.Foundation;
class UnityTime : IOriginalUnityTimeSource
{
    public float Delta, Scale;
    public float DeltaTime { get { Verify.Record("delta");return Delta; } }
    public float TimeScale { get { Verify.Record("getscale");return Scale; } set { Verify.Record("setscale");Scale=value; } }
}
class Updates : IOriginalGameLateUpdateRegistration
{
    public readonly OriginalGameLateUpdateDispatcher Dispatcher=new OriginalGameLateUpdateDispatcher();
    public void RegisterToLateUpdate(Action action) { Verify.Record("register");Dispatcher.RegisterToLateUpdate(action); }
}
class Verify
{
    static int fail,calls;
    static readonly List<string> trace=new List<string>();
    public static void Record(string value) { trace.Add(value);if(++calls==fail)throw new InvalidOperationException(); }
    static float Float(uint bits)=>BitConverter.ToSingle(BitConverter.GetBytes(bits),0);
    static uint Bits(float value)=>BitConverter.ToUInt32(BitConverter.GetBytes(value),0);
    static void Main()
    {
        string line;
        while((line=Console.ReadLine())!=null)
        {
            var updates=new Updates();var time=new UnityTime {Scale=1};OriginalDeltaTimeManager clock=null;
            var output=new List<string>();
            foreach(var command in line.Split(';'))
            {
                var r=command.Split(',');int mode=int.Parse(r[0]);uint value=uint.Parse(r[1]);fail=int.Parse(r[2]);time.Delta=Float(uint.Parse(r[3]));calls=0;trace.Clear();string result="-";
                try
                {
                    switch(mode)
                    {
                        case 0:clock=new OriginalDeltaTimeManager(updates,time);break;
                        case 1:result=Bits(clock.DeltaTime).ToString();break;
                        case 2:updates.Dispatcher.ManualLateUpdate();break;
                        case 3:clock.SetTimeScale(Float(value));break;
                        case 4:time.Scale=Float(value);break;
                        case 5:updates.Dispatcher.ClearCallbacks();break;
                        case 6:updates.Dispatcher.RegisterToLateUpdate(null);break;
                    }
                }
                catch(Exception){result="error";}
                output.Add(result+":"+(clock==null?"null":clock.FrameCount+","+Bits(clock.Time)+","+Bits(clock.TimeScale))+":"+Bits(time.Scale)+":"+string.Join("/",trace));
            }
            Console.WriteLine(string.Join("|",output));
        }
    }
}
