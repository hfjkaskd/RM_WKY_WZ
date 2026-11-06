using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using Whitebox.Foundation;
namespace UnityEngine { public struct Vector3 { public float x,y,z; public Vector3(float x,float y,float z){this.x=x;this.y=y;this.z=z;} } }
namespace Whitebox.Foundation
{
    public interface IOriginalFlowDialog { void Show(); }
    public abstract class OriginalFlowAction { public virtual void Complete(){} }
}
public static class DialogLifecycleHarness
{
    private static readonly List<string> trace=new List<string>();
    private static int[] v;private static bool initializing;private static Dialog dialog;private static Host host;
    private static void Hit(string value,int failure=0)
    {if(initializing)return;trace.Add(value);if(failure!=0&&v[10]==failure)throw new InvalidOperationException();}
    private sealed class Binding:IOriginalUiDialogAction
    {
        public IOriginalUiDialogManager Manager;
        public IOriginalUiDialogManager DialogManager=>Manager;
        public void Complete(){Hit("complete",15);if(v[11]==3)dialog.Config.ShouldNotDestroyOnClose=true;}
    }
    private sealed class Manager:IOriginalUiDialogManager
    {
        public bool Accept;
        public void ShowDialog(OriginalUiDialog d)=>Hit("show-manager",5);
        public bool CloseDialog(OriginalUiDialog d){Hit("close-manager",7);return Accept;}
        public void DialogClosed(OriginalUiDialog d){Hit("closed-manager",13);if(v[11]==4)d.Action=null;}
    }
    private sealed class Host:IOriginalUiDialogHost
    {
        public IEnumerator Routine;
        public void UpdateParent(OriginalUiDialog d)=>Hit("parent",3);
        public UnityEngine.Vector3 GetCameraCenter(){Hit("camera",4);return new UnityEngine.Vector3(2,3,99);}
        public void SetActive(bool value){Hit("object",8);Hit("active,"+(value?1:0),9);}
        public void StartDialogCoroutine(IEnumerator routine){Hit("start",10);Routine=routine;}
        public void DestroyDialog(float delay){Hit("object",8);Hit("destroy,"+delay.ToString("0.00",CultureInfo.InvariantCulture),16);}
    }
    private sealed class Dialog:OriginalUiDialog
    {
        public bool Allowed=true;public OriginalDialogConfig Config;
        public Dialog(IOriginalUiDialogAction a,IOriginalUiDialogHost h):base(a,h){}
        public override OriginalDialogConfig GetConfig(){Hit("config",11);return Config;}
        protected override bool CanClose(){Hit("can");return Allowed;}
        protected override void OnShow(UnityEngine.Vector3 p)=>Hit("shown,"+p.x+","+p.y+","+p.z,6);
        protected override void OnClose(){Hit("onclose",14);if(v[11]==1)Action=null;}
        protected override IEnumerator CloseAnimation(){Hit("animation",12);return Empty();}
        private IEnumerator Empty(){yield break;}
        protected override void CloseByBackground(){Hit("background");base.CloseByBackground();}
        protected override void CloseByEscape(){Hit("escape");base.CloseByEscape();}
        public override void SetSortingOffset(int offset){}
        public override void SetTouchSorting(int order){}
    }
    public static void Main()
    {
        string line;
        while((line=Console.ReadLine())!=null)
        {
            v=Array.ConvertAll(line.Split(','),int.Parse);trace.Clear();initializing=true;
            host=new Host();var manager=new Manager();var binding=new Binding{Manager=manager};dialog=new Dialog(binding,host);
            if(v[1]!=0)dialog.Show();
            if(v[1]==2)dialog.Close(); // Rejected manager preserves the closing flag.
            initializing=false;trace.Clear();
            dialog.Allowed=v[2]!=0;manager.Accept=v[3]!=0;
            dialog.Config=new OriginalDialogConfig{ShouldPlayCloseAnimation=v[4]!=0,ShouldNotDestroyOnClose=v[5]!=0,
                ShouldCloseOnEscape=v[6]!=0,ShouldCloseOnTouch=v[7]!=0,ShouldCloseOnSwipe=v[8]!=0,DestroyOnCloseDelay=v[9]/4f};
            if(v[10]==1)dialog.Action=null;if(v[10]==2)binding.Manager=null;
            bool failed=false;
            try
            {
                switch(v[0]){case 0:dialog.Show();break;case 1:dialog.Close();break;case 2:dialog.ForceClose();break;
                    case 3:dialog.PressBackground(false);break;case 4:dialog.PressBackground(true);break;case 5:dialog.PressEscape();break;}
                if(host.Routine!=null)
                {
                    while(host.Routine.MoveNext())
                    {Hit("yield");var inner=(IEnumerator)host.Routine.Current;while(inner.MoveNext()){};}
                }
            }
            catch(Exception){failed=true;}
            Console.WriteLine((failed?1:0)+"#"+(dialog.IsShowing?1:0)+","+(dialog.IsClosing?1:0)+"#"+string.Join(";",trace));
        }
    }
}
