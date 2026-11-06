[UnityEngine.Scripting.Preserve]
[GraphElementInfo(Category = "基础", Text = "加法(拼接)", SupportTypes = new System.Type[] {typeof(ActionGraphBase)})]
[Obfuz.ObfuzIgnore]
public class Variable_String_Add : Variable_String
{
    public StringWrapper s1;
    public StringWrapper s2;

    public override object Clone()
    {
        var clone = new Variable_String_Add();
        clone.s1 = (StringWrapper) s1?.Clone();
        clone.s2 = (StringWrapper) s2?.Clone();
        return clone;
    }

    public override string GetValue(in ExecuteArgs executeArgs)
    {
        var sv1 = s1.GetValue(executeArgs);
        var sv2 = s2.GetValue(executeArgs);
        return sv1 + sv2;
    }
}