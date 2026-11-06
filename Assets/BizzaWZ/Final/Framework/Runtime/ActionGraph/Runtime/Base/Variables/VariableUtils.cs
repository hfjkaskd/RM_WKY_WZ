using System;

public static class VariableUtils
{
    public static GraphBlackBoard GetBlackBoard(E_VariableScope scope, in ExecuteArgs executeArgs, GameActor actor = null)
    {
        var scopedActor = actor != null ? actor : executeArgs.SelfOrOwner;
        var actionCmpt = scopedActor != null ? scopedActor.GetComponent<ActorCmpt_Action>() : null;
        return scope switch
        {
            E_VariableScope.Graph => executeArgs.graph.blackBoard,
            E_VariableScope.Actor => actionCmpt != null ? actionCmpt.blackBoard : null,
            E_VariableScope.Global => ActionModule.Instance.blackBoard,
            E_VariableScope.BattleGlobal => ActionModule.Instance.battleGlobal,
            _ => null
        };
    }

    public static Variable Get(E_VariableScope scope, string varName, in ExecuteArgs executeArgs, GameActor actor = null)
    {
        var bb = GetBlackBoard(scope, executeArgs, actor);
        if (bb == null)
        {
            ActionGraphLog.Error("Get Variable blackboard null");
            return null;
        }

        return bb.Get(varName);
    }

    public static Variable Set(E_VariableScope scope, string varName, in ExecuteArgs executeArgs, VariableWrapperBase wrapper, GameActor actor = null)
    {
        if (wrapper == null || wrapper.internalValue == null)
        {
            ActionGraphLog.Error("Set Variable wrapper null");
            return null;
        }

        var variable = Get(scope, varName, executeArgs, actor);

        if (variable == null)
        {
            variable = Variable.Create(varName, Variable.CreateDirectValue(wrapper.VariableType));
            var bb = GetBlackBoard(scope, executeArgs, actor);
            if (bb == null)
            {
                return null;
            }

            bb.Set(variable);
        }

        switch (wrapper.VariableType)
        {
            case E_VariableType.Bool:
                variable.SetValue_Bool(executeArgs, (wrapper as BoolWrapper).GetValue(executeArgs));
                break;
            case E_VariableType.Float:
                variable.SetValue_Float(executeArgs, (wrapper as FloatWrapper).GetValue(executeArgs));
                break;
            case E_VariableType.String:
                variable.SetValue_String(executeArgs, (wrapper as StringWrapper).GetValue(executeArgs));
                break;
            case E_VariableType.Vector3:
                variable.SetValue_Vector3(executeArgs, (wrapper as Vector3Wrapper).GetValue(executeArgs));
                break;
            case E_VariableType.Actor:
                variable.SetValue_Actor(executeArgs, (wrapper as ActorWrapper).GetValue(executeArgs));
                break;
            default:
                ActionGraphLog.Error($"不支持SetValue：{wrapper.VariableType}");
                break;
        }

        return variable;
    }
}
