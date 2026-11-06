
using Bizza;
 [Obfuz.ObfuzIgnore]
public enum E_GraphEvent_Teach
{
}

 [Obfuz.ObfuzIgnore]

public class ActionContext_Teach : ActionContextBase, IOnRelease
{
    public override GameActor SelfOrOwner => self;

    public void OnRelease()
    {
        self = null;
    }
}
 [Obfuz.ObfuzIgnore]
 
public class TeachConfigData : GraphConfigData
{
    public override object Clone()
    {
        return new TeachConfigData();
    }

    public override void CloneTo(GraphConfigData data)
    {
    }
}

  [Obfuz.ObfuzIgnore]
[GraphElementInfo(Text = "教学")]
public class ActionGraph_Teach : ActionGraphBase
{
    // [InlineProperty][HideLabel][BoxGroup("Buff配置")]
    // public BuffConfigData config = new();
    public BoolWrapper conditionOverride => GetVariableNode<EventAction_Teach_TeachStart>()?.conditionOverride;

    public ActionGraph_Teach(string name) : base(name)
    {

    }

    internal override bool UpdateInGamePause => true;

    internal override GraphConfigData CreateConfigData()
    {
        return new TeachConfigData();
    }

    public override object Clone()
    {
        var clone = new ActionGraph_Teach(graphName);
        CloneBase(clone);
        clone.configData = (TeachConfigData)configData?.Clone();
        return clone;
    }
}
