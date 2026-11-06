/// <summary>
/// 状态机中状态的接口
/// </summary>
/// <typeparam name="T"></typeparam>
public interface IFsmState<T>
{
    T GetStateType();

    /// <summary>
    /// 进入状态
    /// </summary>
    void Enter();

    /// <summary>
    /// 离开状态
    /// </summary>
    void Leave();

    /// <summary>
    /// 更新状态
    /// </summary>
    void Update();
}