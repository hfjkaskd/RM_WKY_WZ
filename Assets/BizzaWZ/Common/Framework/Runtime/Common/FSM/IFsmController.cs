/// <summary>
/// 状态机控制器的接口
/// </summary>
/// <typeparam name="T"></typeparam>
public interface IFsmController<T>
{
    /// <summary>
    /// 获取当前状态
    /// </summary>
    IFsmState<T> GetCurrentState();

    /// <summary>
    /// 初始化状态机
    /// </summary>
    void InitState();

    /// <summary>
    /// 获取给定类型的状态
    /// </summary>
    /// <param name="stateType">状态类型</param>
    IFsmState<T> GetState(T stateType);

    /// <summary>
    /// 切换状态
    /// </summary>
    /// <param name="stateType">新状态类型</param>
    void SwitchState(T stateType);

    /// <summary>
    /// 更新状态机
    /// </summary>
    void UpdateState();
}