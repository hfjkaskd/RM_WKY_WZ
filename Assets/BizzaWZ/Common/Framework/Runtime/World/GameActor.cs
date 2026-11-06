using System;
using System.Collections;
using System.Collections.Generic;
using Bizza;
using UnityEngine;
using Sirenix.OdinInspector;

public class ComponentDataBase
{
    [LabelText("HUD位置")][FoldoutGroup("挂点")]
    public Transform hudContainer;
}

public abstract class GameActor : Actor
{
    // [LabelText("自动初始化")]
    // public bool autoInit;
    // [ShowIf("@autoInit")]
    // [LabelText("id 单位表id")]
    // public string autoInitId;
    // [ShowIf("@autoInit")]
    // [LabelText("阵营 怪物：0 玩家：1 假玩家：>1")]
    // public int autoInitCamp;
    //
    // [ReadOnly]
    // public string id;
    //
    // private int _campId;
    // [ShowInInspector]
    // public int CampId
    // {
    //     get => _campId;
    //     set
    //     {
    //         _campId = value;
    //         if (_campId == BattleDefine.PlayerCamp) _campType = E_CampType.LocalPlayer;
    //         if (_campId == BattleDefine.MonsterCamp) _campType = E_CampType.LocalEnemy;
    //     }
    // }
    //
    // public virtual ComponentDataBase componentData { get; }
    //
    // public Vector3 Position
    // {
    //     get => transform.position;
    //     set => transform.position = value;
    // }
    // public bool bPlayer => CampId == BattleDefine.PlayerCamp;
    // public virtual E_TagType Tag => E_TagType.None;
    // public virtual bool IsAvailable => true;
    // [HideInInspector] public ActorCmpt_Buff buffCmpt;
    // [HideInInspector] public ActionCmpt_Behavior behaviorCmpt;
    //
    // void Awake()
    // {
    //     OnAwake();
    // }
    //
    // protected virtual void OnAwake()
    // {
    //     buffCmpt = gameObject.GetOrAddComponent<ActorCmpt_Buff>();
    //     behaviorCmpt = gameObject.GetOrAddComponent<ActionCmpt_Behavior>();
    // }
    //
    // #region bizza game interface
    //
    // protected E_CampType _campType = E_CampType.None;
    //
    // public virtual E_CampType CampType // 玩家阵营
    // {
    //     get { return _campType; }
    //     set { _campType = value; }
    // }
    //
    // public virtual float MoveSpeed => 0;
    //
    // public virtual float Radius => 0;
    //
    // public Circle Circle => new Circle(transform.position, Radius);
    //
    // #endregion
    // public event Action PreReleaseEvent;
    //
    // protected override void OnPreRelease()
    // {
    //     PreReleaseEvent?.Invoke();
    //     PreReleaseEvent = null;
    // }
    //
    // protected override void OnRelease()
    // {
    //     CampId = 0;
    // }
}
