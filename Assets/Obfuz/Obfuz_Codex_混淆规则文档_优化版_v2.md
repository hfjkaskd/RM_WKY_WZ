# Obfuz 混淆规则文档（给 Codex 使用）

> 适用对象：Unity 项目 + Obfuz  
> 目标：让 Codex 在修改或新增功能时，能够稳定判断哪些代码应继续正常混淆、哪些位置需要添加 `[Obfuz.ObfuzIgnore]` 或改用规则文件 / Pass 配置，从而尽量保证游戏功能在混淆后仍能正常运行。  
> 当前项目前提：**项目虽然包含热更新 / 热重载相关架构或预留模块，但当前版本功能全部在本地程序集内运行，不依赖运行时外部热更新 DLL。** 因此默认按 **普通 Unity 本地程序集项目** 处理混淆规则，而不是按 HybridCLR 热更新发布项目处理。

---

# 1. 文档目标

本规则文档用于约束 Codex 在 Unity 项目中使用 Obfuz 时的行为，核心原则如下：

1. **默认尽量保持混淆开启**，不要因为怕出错就大面积跳过混淆。
2. **只在必要位置加 `[Obfuz.ObfuzIgnore]`**，优先最小粒度。
3. **优先保护“名字会被外部系统或字符串访问”的元数据**，而不是无脑保护整段逻辑。
4. **当前项目不把热更新兼容作为默认前提**；只有未来真的启用外部热更新 DLL 时，才切换到热更新专项规则。
5. **不是所有例外都必须靠 Attribute 解决**；必要时可以使用：
   - Obfuscation Pass 配置
   - Symbol Obfus 规则文件
   - `[ObfuzIgnore]`

---

# 2. 项目前提与结论

## 2.1 当前项目状态

当前项目虽然可能带有以下内容：

- 热更新框架代码
- 热重载相关目录或工具链
- HybridCLR / xLua / 自定义热更新预留层
- 将来可能拆分为热更新模块的架构设计

但**当前运行逻辑全部在本地程序集内执行**，并不是“运行时外部加载热更新 DLL”的发布模式。

## 2.2 这对 Codex 的影响

因此 Codex 在改代码时：

- **默认按“非热更新发布项目”处理**
- **不要因为项目含有热更新相关代码，就默认大面积加 `[Obfuz.ObfuzIgnore]`**
- **先关注 UnityEvent、反射、外部桥接入口、特殊 const 反射访问** 这几类高风险点
- HybridCLR / 热更新混淆工作流只作为附录，不作为当前默认约束

---

# 3. Codex 必须先知道的 Obfuz 基本事实

## 3.1 Obfuz 不是“所有地方都要手写规则”的插件

Obfuz 与 Unity 工作流有较深集成。默认情况下，它已经自动处理了很多 Unity 常见特殊情况，**不需要因为是 Unity 项目就手动给大量代码补 Ignore**。

Codex 不应默认假设：

- 所有 `MonoBehaviour` 都要保护
- 所有 `[SerializeField]` 字段都要保护
- 所有 `[Serializable]` 类都要手动加 Ignore

重点不是“哪里重要”，而是“哪里会按原始名字访问”。

---

## 3.2 `[ObfuzIgnore]` 的本质

`[ObfuzIgnore]` 是一个代码级别的混淆豁免标记，可加在：

- 类型
- 方法
- 字段
- Property
- event

它的构造函数默认参数为 `ObfuzScope.All`，即默认会禁用对应目标上的全部相关混淆。

---

## 3.3 `ObfuzIgnoreAttribute` 的关键参数

### `Scope`
用于指定忽略的范围，可精确控制到名字、参数、函数体等维度。常用值包括：

- `TypeName`
- `Field`
- `MethodName`
- `MethodParameter`
- `MethodBody`
- `Method`
- `PropertyName`
- `PropertyGetterSetterName`
- `Property`
- `EventName`
- `EventAddRemoveFireName`
- `Event`
- `All`

### `ApplyToNestedTypes = true`
默认会传播到嵌套类型。

### `ApplyToChildTypes = false`
默认**不会**传播到派生类型或实现类。

---

## 3.4 `[ObfuzIgnore]` 不能用于程序集级

下面这种写法无效：

```csharp
[assembly: ObfuzIgnore]
```

如果不想混淆某个程序集，正确做法不是写程序集级 Attribute，而是把该程序集从 Obfuz 的混淆程序集列表中移除。

---

## 3.5 `[ObfuzIgnore]` 最终不会留在产物里

Obfuz 会在最终阶段移除这些 Attribute，因此它主要是“源码规则标记”，不会留在最终混淆后的程序集里。

---

# 4. 当前项目的最高优先级原则

## 原则 A：能不加整类 Ignore，就不要加整类

优先顺序：

1. 方法名级
2. 字段级
3. 属性名级
4. event 名级
5. 方法级
6. 类型名级
7. 类型级 All
8. 程序集移出混淆列表

原因：

- 类型级 Ignore 影响范围大
- 容易把大量本来可以正常混淆的代码也一起放弃保护
- 长期会让项目混淆规则越来越失控

---

## 原则 B：只保护“名字敏感”的地方

Obfuz 出问题的核心通常不是逻辑本身，而是**名称被改了**。  
因此 Codex 应优先识别这些场景：

- 代码里写死类型名字符串
- `Type.GetType("xxx")`
- `GetMethod("xxx")`
- `GetField("xxx")`
- `GetProperty("xxx")`
- Inspector 中 UnityEvent 绑定的方法
- 第三方框架按字符串找类名 / 方法名 / 字段名
- 外部系统按原始名称调用入口代码
- `Invoke("MethodName")`
- `SendMessage("MethodName")`

---

## 原则 C：默认相信 Obfuz 已自动处理的 Unity 常规情况

对以下内容，Codex 一般**不要习惯性补 Ignore**：

- `MonoBehaviour`
- `ScriptableObject`
- `[Serializable]` 类型的类型名 / 命名空间
- 这些类型参与序列化的字段
- `[SerializeField]` 私有字段
- 非 static public 属性
- `[Serializable]` 枚举的枚举项名
- `Awake` / `Start` 等 Unity 特殊事件函数

---

## 原则 D：优先“只保护名字”，不要默认把整个成员全保护掉

很多时候真正敏感的是“名字”，不是方法体本身。  
因此在可以精细控制时，应优先考虑：

- `ObfuzScope.MethodName`
- `ObfuzScope.PropertyName`
- `ObfuzScope.EventName`
- `ObfuzScope.TypeName`

而不要一上来就直接写默认的：

```csharp
[ObfuzIgnore]
```

因为默认 `All` 往往保护过头。

---

# 5. Codex 必须知道的默认安全范围

## 5.1 Unity 内建序列化默认已受保护的内容

对于 Unity 常规工作流，Obfuz 已做了特殊支持。以下内容一般默认安全：

- `MonoBehaviour`、`ScriptableObject`、`[Serializable]` 类型的**类型名与命名空间**
- 这些类型中**参与序列化的字段**
- 带 `[SerializeField]` 的私有字段
- 这些类型的**非 static public 属性**
- `[Serializable]` 枚举的枚举项名
- `MonoBehaviour` 中具有特殊用途的事件函数，如 `Awake`、`Start`

---

## 5.2 第三方序列化默认已受保护的内容

对需要序列化的类型添加 `[Serializable]` 后，Obfuz 默认会保护：

- 类型名
- 命名空间
- public 非 static 字段
- public 非 static 属性
- 枚举类型的枚举项名

这通常已能满足大多数 `Newtonsoft.Json` / 常规数据类使用场景。

---

# 6. Codex 必须加“保护规则”的场景

> 这里说的“必须保护”，不等于“必须只能靠 `[ObfuzIgnore]`”。  
> 可用方式有三种：
> 1. Obfuscation Pass 中禁用某些元数据的 `Symbol Obfus`
> 2. Symbol Obfus 规则文件中添加例外
> 3. 在代码中对目标元数据加 `[ObfuzIgnore]`

---

## 6.1 Inspector 中绑定的 UnityEvent 回调函数

这是最容易被忽略、但最关键的高风险场景之一。

Obfuz **没有自动处理场景对象上的 `UnityEvent` 字段**。  
如果某个 `UnityEvent` 在 Inspector 中绑定了一个方法，而该方法名被混淆，运行时就会找不到回调。

### 规则
- Inspector 绑定的方法：**必须保护其方法名**
- 最常用做法：给该方法加 `[ObfuzIgnore]`
- 更精细的做法：优先保护 `MethodName`

### 推荐写法

```csharp
using Obfuz;
using UnityEngine;

public class TestPanel : MonoBehaviour
{
    [ObfuzIgnore(ObfuzScope.MethodName)]
    public void OnClickStart()
    {
        StartGame();
    }

    private void StartGame()
    {
        // 业务逻辑
    }
}
```

---

## 6.2 通过字符串进行反射查找的类型、方法、字段、属性

符号混淆会改名字，因此基于名称的反射在混淆后会失败。

### 高风险示例

```csharp
Type.GetType("Game.Reward.RewardStrategy");
type.GetMethod("Execute");
type.GetField("rewardId");
type.GetProperty("Value");
```

### 规则
- 只保护**被反射查找的元数据**
- 不要因为有反射就整类 Ignore
- 若只是方法名被按字符串访问，就只保护方法名
- 若只是字段名被反射访问，就只保护字段
- 若只是属性名被反射访问，就只保护属性名

### 推荐写法

```csharp
using Obfuz;

public class RewardStrategy
{
    [ObfuzIgnore(ObfuzScope.MethodName)]
    public void Execute()
    {
    }
}
```

```csharp
using Obfuz;

public class RewardData
{
    [ObfuzIgnore(ObfuzScope.Field)]
    public int rewardId;
}
```

```csharp
using Obfuz;

public class PlayerInfo
{
    [ObfuzIgnore(ObfuzScope.PropertyName)]
    public string UserId { get; set; }
}
```

---

## 6.3 按原始类名查找类型时，优先考虑官方映射方案

若只是“按原始类名找类型”，不要第一反应整类 Ignore。  
优先考虑 Obfuz 提供的：

- `ObfuscationTypeMapper`
- `ObfuscationInstincts`

适用场景：

- 运行时只知道原始类型全名字符串
- 需要从原始名映射到混淆后的真实类型
- 需要拿到某个类型的“原始名字”

### 示例思路

```csharp
using System.Reflection;
using Obfuz;

class Boot
{
    void Init()
    {
        ObfuscationInstincts.RegisterReflectionType<MyClassA>();
        ObfuscationInstincts.RegisterReflectionType<MyClassB>();
    }

    void Test()
    {
        Assembly ass = typeof(Boot).Assembly;
        var typeA = ObfuscationTypeMapper.GetTypeByOriginalFullName(ass, "MyClassA");
    }
}
```

### 适合 Codex 的结论
- **按原始类名找类型**：优先 `ObfuscationTypeMapper`
- **当前位置已知具体类型**：优先 `ObfuscationInstincts.FullNameOf<T>()` / `NameOf<T>()`
- 只有在不方便接入映射时，才考虑保护类型名

---

## 6.4 外部系统按固定名称调用的方法

凡是 Unity 之外、或项目中某个框架会按名字访问的方法或类型，都要视为高风险，例如：

- 原生 Android / iOS 回调桥接
- Lua / xLua 注册方法
- GM 命令入口
- 自定义控制台命令
- 事件总线按字符串找 handler
- 配置表或注册表通过类名 / 方法名绑定逻辑
- 某些 SDK 或插件按方法名反射回调

### 规则
- 被外部系统按名字访问的方法或类型，要保护相应名称
- 优先最小粒度
- 不要因为桥接类里有 1 个入口就整类 Ignore，除非整类所有成员都依赖原始名称

### 推荐写法

```csharp
using Obfuz;

public class NativeBridge
{
    [ObfuzIgnore(ObfuzScope.MethodName)]
    public void OnNativeRewardCallback(string json)
    {
        // ...
    }
}
```

---

## 6.5 需要通过反射访问的 const 字段

Obfuz 默认会移除 const 字段，因为它们通常会被编译器内联。  
如果某个 `const` 字段需要在运行时通过反射访问，就必须保留。

### 规则
- 对需要保留的常量字段，加：
  - `[ObfuzIgnore(ObfuzScope.Field)]`
  - 或在规则中禁止其字段相关处理

### 示例

```csharp
using Obfuz;

public class ConfigKeys
{
    [ObfuzIgnore(ObfuzScope.Field)]
    public const string RewardId = "reward_id";
}
```

---

## 6.6 `Invoke("MethodName")`、`SendMessage("MethodName")`

这类调用本质上也是按字符串找方法名，应和反射风险同级处理。

### 示例

```csharp
Invoke("RefreshReward", 1f);
SendMessage("OnBuySuccess");
```

### 规则
- 被这些 API 按字符串调用的方法，至少要保护方法名

---

## 6.7 第三方序列化 / 配置系统依赖“非默认受保护成员名”时

如果类型已经带了 `[Serializable]`，且只是使用：

- public 非 static 字段
- public 非 static 属性

配合 `Newtonsoft.Json` 等做常规序列化，那么通常不用额外加 Ignore。

但如果依赖以下内容：

- private 字段名
- non-public 属性名
- public static 属性
- non-public 成员
- 自定义字符串映射流程仍会读取原始成员名

那么这些就不在默认保护范围内，Codex 应额外保护相应元数据。

---

# 7. Codex 一般不要加 `[ObfuzIgnore]` 的场景

## 7.1 普通业务逻辑方法

如果一个方法：

- 不会被 Inspector 绑定成 UnityEvent 回调
- 不会被反射按名字查找
- 不会被外部系统按原始名称调用

那它通常应该继续参与混淆。

**不要因为“这个方法很重要”就给它加 Ignore。**

---

## 7.2 普通 Unity 生命周期函数

例如：

- `Awake`
- `Start`
- `OnEnable`
- `Update`

这类 Unity 特殊用途函数，通常已在默认支持范围内。  
一般不要额外加 Ignore。

---

## 7.3 普通 `[SerializeField]` 字段

只要它属于 Unity 常规序列化工作流，通常已默认处理。  
除非它还会被其他系统按名称访问，否则不要额外加 Ignore。

---

## 7.4 普通 `[Serializable]` 数据类的常规 public 字段 / 属性

如果只是给：

- `JsonUtility`
- `Newtonsoft.Json`
- Unity 常规序列化

使用，而且成员在默认保护范围内，一般不要额外加 Ignore。

---

## 7.5 因为“项目有热更新架构”就整类或整模块 Ignore

当前项目并没有实际发布热更新 DLL，因此：

- 不要因为“以后可能热更新”就大面积保留原名
- 不要为了未来可能的外部加载，提前把大量业务类整类 Ignore
- 除非某段代码已经明确属于“未来热更新边界且当前也按名称访问”，否则按普通本地程序集规则处理

---

# 8. 更推荐的 `[ObfuzIgnore]` 使用粒度

## 8.1 推荐粒度顺序

从优到劣：

1. `MethodName`
2. `Field`
3. `PropertyName`
4. `EventName`
5. `Method`
6. `TypeName`
7. 类型级默认 `All`
8. 程序集整体移出混淆列表

---

## 8.2 常见推荐写法

### 只保护方法名
```csharp
[ObfuzIgnore(ObfuzScope.MethodName)]
public void Execute() {}
```

### 保护整个方法（仅在确有需要时）
```csharp
[ObfuzIgnore(ObfuzScope.Method)]
public void Execute() {}
```

### 只保护字段
```csharp
[ObfuzIgnore(ObfuzScope.Field)]
public int rewardId;
```

### 只保护属性名
```csharp
[ObfuzIgnore(ObfuzScope.PropertyName)]
public string UserId { get; set; }
```

### 只保护事件名
```csharp
[ObfuzIgnore(ObfuzScope.EventName)]
public event Action OnReward;
```

### 只保护类型名
```csharp
[ObfuzIgnore(ObfuzScope.TypeName)]
public class RewardStrategy {}
```

---

## 8.3 什么时候才用默认 `[ObfuzIgnore]`

只有在你明确希望该目标上的**全部相关混淆都关闭**时，才使用：

```csharp
[ObfuzIgnore]
```

否则优先写带 `Scope` 的版本。

---

# 9. 除了 Attribute，还有哪三种正规做法

## 9.1 Obfuscation Pass 配置

适用于：

- 某批元数据要统一禁用 `Symbol Obfus`
- 不希望在源码里散落大量 Attribute
- 想把混淆策略集中管理

---

## 9.2 Symbol Obfus 规则文件

适用于：

- 第三方程序集
- 大批量成员的统一例外
- 无法或不方便改源码的位置

---

## 9.3 代码中添加 `[ObfuzIgnore]`

适用于：

- 当前业务代码
- Codex 正在直接修改的源码
- 单点例外处理
- 需要和具体代码绑定的规则

---

# 10. 第三方插件 / 不可改源码程序集的处理规则

## 10.1 不可改源码时，不要强行走 Attribute 路线

如果问题出在第三方插件且源码不可控，Codex 不应尝试虚构一种“程序集级 `[ObfuzIgnore]`”方案。

### 正确做法
1. 把插件程序集从混淆列表中移除
2. 或在规则文件中配置例外
3. 或只对你自己的桥接层做保护

---

## 10.2 什么时候把程序集整个移出混淆列表

仅在以下情况考虑：

- 该插件大量依赖反射 / 名字约定
- 源码不可改
- 规则文件处理成本过高
- 已验证混淆后稳定性风险大

### 原则
能局部规则处理，就不要直接整程序集不混淆。  
但对第三方黑盒插件，**整程序集移出混淆** 是合法且现实的方案。

---

# 11. 反射与字符串访问的标准判断树

当 Codex 新增或修改一个 Unity 功能时，应按以下顺序判断：

## 第一步：这段代码会不会被“按名字”访问？

若不会：

- 默认不加 `[ObfuzIgnore]`

若会：

- 进入第二步

### 典型判断线索
- 有没有字符串类型名 / 方法名 / 字段名
- 有没有反射
- 有没有 UnityEvent Inspector 绑定
- 有没有原生桥接 / Lua / GM / 命令系统 / 注册系统
- 有没有 `Invoke("xxx")` / `SendMessage("xxx")`

---

## 第二步：是谁被按名字访问？

- 如果是类型名：优先 `TypeName` 或映射方案
- 如果是方法名：优先 `MethodName`
- 如果是字段名：优先 `Field`
- 如果是属性名：优先 `PropertyName`
- 如果是事件名：优先 `EventName`

**不要扩大范围。**

---

## 第三步：默认规则是否已自动保护？

如果属于以下内容，通常已自动处理：

- `MonoBehaviour`
- `ScriptableObject`
- `[Serializable]` 类型名 / 命名空间
- 这些类型参与序列化的字段
- `[SerializeField]` 私有字段
- 非 static public 属性
- `[Serializable]` 枚举项
- `Awake` / `Start` 等 Unity 特殊事件函数

若已自动处理，一般不要重复加 Ignore。

---

## 第四步：能否改成官方推荐替代方案？

如果只是“按原始类名找类型”，优先考虑：

- `ObfuscationTypeMapper`
- `ObfuscationInstincts`
- 手动维护原始名到类型的字典映射

不要直接整类 Ignore。

---

## 第五步：是否应改用规则文件或 Pass？

如果出现以下情况，优先考虑规则文件或 Pass：

- 第三方插件
- 大量同类规则
- 源码不可改
- 不想把大量混淆规则散落到代码里

---

# 12. Codex 必须警惕的典型高风险代码模式

下面这些模式一出现，Codex 应进入“检查是否要保护名字”模式：

```csharp
Type.GetType("xxx")
assembly.GetType("xxx")
type.GetMethod("xxx")
type.GetField("xxx")
type.GetProperty("xxx")
Invoke("MethodName", ...)
SendMessage("MethodName", ...)
```

---

# 13. 字段加密和 `[ObfuzIgnore]` 的关系

这是一个非常容易误判的点。

## 13.1 `[EncryptField]` 优先级高于 `[ObfuzIgnore]`

只要字段上加了 `[EncryptField]`，即使：

- 字段本身有 `[ObfuzIgnore]`
- 字段所在类型有 `[ObfuzIgnore]`

这个字段**仍然会被加密**。

### 示例

```csharp
using Obfuz;

[ObfuzIgnore]
class A
{
    [EncryptField]
    public int x1; // 仍然会被加密

    [ObfuzIgnore]
    [EncryptField]
    public int x2; // 仍然会被加密

    public int y; // 不加密
}
```

---

## 13.2 反射直接读写字段时，不要轻易做字段加密

因为字段加密后，反射直接读到的可能是加密值，而不是业务层期待的原始值。

### 规则
以下字段不要轻易加 `[EncryptField]`：

- 会被反射读写的字段
- 会被配置系统直接取值的字段
- 会被调试系统 / GM / Inspector 工具直接访问的字段
- 第三方框架直接反射读取的字段

---

# 14. Codex 改完代码后的验证流程

## 14.1 先看 Reflection Compatibility 相关检测

Obfuz 支持离线检测潜在的反射兼容问题，并打印错误或警告。  
因此新增功能后，应优先检查：

- 是否出现 reflection compatibility warning / error
- 是否提示某个反射调用在混淆后可能失效
- 是否提示某个 `ToString()` / 枚举名 / 字段名 / 方法名存在兼容性问题

### 处理原则
- 先修正具体元数据保护范围
- 不要第一反应整类 `[ObfuzIgnore]`

---

## 14.2 验证 UnityEvent

检查以下内容：

- Inspector 绑定是否仍然存在
- 混淆后按钮点击 / 回调是否正常触发
- 运行时是否出现“找不到回调函数”之类错误

---

## 14.3 验证字符串反射入口

检查以下内容：

- `Type.GetType`
- `GetMethod`
- `GetField`
- `GetProperty`
- 通过配置表 / 注册表 / Lua / GM 查找入口的逻辑

确认混淆后仍能正确找到目标。

---

## 14.4 验证字段加密是否误用

检查：

- 被反射访问的字段是否被加了 `[EncryptField]`
- 配置系统直接读取的字段是否被错误加密
- 是否存在“字段保留了名字，但读出来仍不对”的情况

---

# 15. 混淆后问题排查建议

## 15.1 不要一出问题就扩大 Ignore

混淆后出现问题时，优先定位它属于哪一类：

- UnityEvent 回调丢失
- 反射找不到类型
- 反射找不到方法 / 字段 / 属性
- 第三方插件依赖名称
- 字段加密导致读取异常

先缩小问题点，再决定是否加 Ignore。

---

## 15.2 优先保留映射与日志信息

如果项目工作流允许，建议保留混淆映射和相关诊断信息，用于排查线上问题。

---

## 15.3 使用还原混淆堆栈日志能力

混淆后如果线上调用栈难读，应优先结合 Obfuz 的 **DeobfuscateStackTrace** 工作流来还原堆栈，再决定是否要增加保护范围。  
不要因为“线上日志看不懂”就大面积关闭混淆。

---

# 16. 当前项目与 HybridCLR / 热更新的关系

## 16.1 当前结论

当前项目虽然有热更新 / 热重载架构，但**当前功能全部本地程序集运行**。因此：

- 当前 Codex 默认**不需要**按 HybridCLR 热更新发布项目的规则处理
- 也**不需要**先接 `obfuz4hybridclr`
- 也**不需要**为了“未来也许会热更新”而大面积保留类型名 / 方法名

---

## 16.2 什么时候才需要切换到热更新专项规则？

只有在将来满足以下条件时，才切换：

- 真正发布热更新 DLL
- 真正运行时外部加载这些 DLL
- 真正进入 HybridCLR 热更新发布链路

那时再引入：

- `obfuz4hybridclr`
- `HybridCLR/ObfuzExtension/GenerateAll`
- `HybridCLR/Obfuz/Extension/CompileAndObfuscateDll`
- link.xml 与热更新 DLL 混淆专项规则

---

# 17. 最终给 Codex 的执行规范（可直接贴给 Codex）

## 项目前提
本项目当前运行逻辑全部在本地程序集内执行，不以热更新 DLL 运行为默认前提。虽然项目中存在热更新 / 热重载相关架构或预留模块，但当前版本不依赖运行时外部加载 DLL。

## 必须遵守的规则

1. 默认保持代码参与混淆，不要随意新增 `[Obfuz.ObfuzIgnore]`。
2. 只有当代码会被“按原始名字访问”时，才考虑添加保护规则。
3. 发现风险时，优先采用**最小粒度**：
   - 方法名优先 `ObfuzScope.MethodName`
   - 字段优先 `ObfuzScope.Field`
   - 属性优先 `ObfuzScope.PropertyName`
   - 事件优先 `ObfuzScope.EventName`
   - 类型名优先 `ObfuzScope.TypeName`
4. 只有在确有需要时，才使用默认 `[ObfuzIgnore]`（即 `All`）。
5. 优先检查以下高风险场景：
   - Inspector 绑定的 `UnityEvent` 回调方法
   - 字符串反射：`Type.GetType`、`GetMethod`、`GetField`、`GetProperty`
   - 原生桥接、GM、命令系统、注册系统、配置系统中按名字访问的成员
   - 需要通过反射访问的 `const` 字段
   - `Invoke("MethodName")`、`SendMessage("MethodName")`
6. 以下内容一般不要额外加 `[Obfuz.ObfuzIgnore]`：
   - 普通业务逻辑函数
   - 普通 `[SerializeField]` 字段
   - `Awake`、`Start` 等 Unity 生命周期函数
   - 已使用 `[Serializable]` 且仅依赖 public 非 static 字段 / 属性进行常规序列化的类型
7. 若只是“按原始类型名查找类型”，优先考虑：
   - `ObfuscationTypeMapper`
   - `ObfuscationInstincts`
   - 手动维护原始名到类型的映射
8. 若字段会被反射直接读写，不要轻易增加 `[EncryptField]`。
9. 对于第三方插件、不可改源码程序集、大批量例外规则，优先考虑：
   - Obfuscation Pass
   - Symbol Obfus 规则文件
   - 必要时将程序集移出混淆列表
10. 当前项目不默认启用 HybridCLR 混淆专项规则；只有未来真的启用热更新 DLL 发布链路时，才切换到热更新专项工作流。
11. 每次改完后，优先检查：
   - Reflection Compatibility 日志
   - UnityEvent 回调是否仍正常
   - 反射 / 注册 / 命令 / 桥接入口是否仍能找到目标
   - 被反射访问的字段是否误用了 `[EncryptField]`

---

# 18. 常见示例对照表

## 示例 1：普通业务方法
```csharp
public void RefreshUI()
{
    // 普通逻辑
}
```

### 结论
- 不加 `[ObfuzIgnore]`

---

## 示例 2：Inspector 绑定按钮点击
```csharp
[ObfuzIgnore(ObfuzScope.MethodName)]
public void OnClickConfirm()
{
    Confirm();
}
```

### 结论
- 必须保护方法名
- 推荐最小粒度保护 `MethodName`

---

## 示例 3：反射查找方法
```csharp
var method = type.GetMethod("Execute");
```

### 推荐写法
```csharp
[ObfuzIgnore(ObfuzScope.MethodName)]
public void Execute()
{
}
```

### 结论
- `Execute` 若会被反射按原名查找，需要保护方法名

---

## 示例 4：普通 `[SerializeField]` 字段
```csharp
[SerializeField] private int score;
```

### 结论
- 一般不需要额外加 `[ObfuzIgnore]`

---

## 示例 5：const 被反射访问
```csharp
[ObfuzIgnore(ObfuzScope.Field)]
public const string Key = "reward_key";
```

### 结论
- 必须保护字段，不然可能被移除

---

## 示例 6：字段既 Ignore 又 Encrypt
```csharp
[ObfuzIgnore]
[EncryptField]
public int value;
```

### 结论
- 仍然会被加密
- 不要误以为 `[ObfuzIgnore]` 能覆盖 `[EncryptField]`

---

## 示例 7：按原始类名找类型
```csharp
var type = Type.GetType("Game.Reward.RewardStrategy");
```

### 更推荐做法
```csharp
ObfuscationInstincts.RegisterReflectionType<RewardStrategy>();
var type = ObfuscationTypeMapper.GetTypeByOriginalFullName(
    typeof(RewardStrategy).Assembly,
    "Game.Reward.RewardStrategy");
```

### 结论
- 优先用映射方案，不要默认整类 Ignore

---

## 示例 8：外部桥接方法
```csharp
[ObfuzIgnore(ObfuzScope.MethodName)]
public void OnNativeLoginResult(string json)
{
}
```

### 结论
- 保护入口方法名即可，通常不需要整类 Ignore

---

# 19. 给代码审查用的检查清单

每次 Codex 改完功能后，按下面清单检查：

## A. 是否新增了 Inspector 绑定回调？
- 如果是，是否保护了回调方法名？
- 是否优先使用了 `MethodName` 而不是默认 `All`？

## B. 是否新增了字符串反射？
- `Type.GetType`
- `GetMethod`
- `GetField`
- `GetProperty`
- `Invoke("xxx")`
- `SendMessage("xxx")`

若有，是否保护了对应元数据？

## C. 是否新增了外部桥接入口？
- Android / iOS / SDK / Lua / GM / 命令系统 / 配置系统  
若有，是否保护了入口方法或类型？

## D. 是否新增了 const 且会被反射访问？
- 若会，是否加了字段级保护？

## E. 是否误加了大面积 Ignore？
- 普通逻辑类是否被整类 Ignore？
- 普通序列化字段是否被重复保护？
- 是否仅因为“将来可能热更新”就保留了大量原始名称？

## F. 是否本该用映射，却错误用了 Ignore？
- 只是按原始类名查找类型时，是否优先考虑了 `ObfuscationTypeMapper` / `ObfuscationInstincts`？

## G. 是否把反射字段又加密了？
- 若字段会被反射直接访问，是否错误使用了 `[EncryptField]`？

## H. 是否应该改用规则文件或 Pass？
- 第三方插件
- 不可改源码程序集
- 批量规则场景  
是否还在用不必要的源码 Attribute 方案？

## I. 是否检查了混淆验证结果？
- Reflection Compatibility 日志是否正常？
- UnityEvent 是否仍然有效？
- 关键桥接 / 注册 / 命令入口是否仍正常？

---

# 20. 最终结论

一句话总结：

**当前项目下，Codex 的默认动作应当是“正常混淆”，只有在代码会被按原始名称访问时，才在最小粒度上添加保护规则。**

当前最需要重点防的不是“所有热更新相关代码”，而是这几类：

1. Inspector 绑定的 UnityEvent 回调  
2. 反射名字访问  
3. 外部桥接 / 注册入口  
4. 特殊 const 反射访问  
5. `Invoke` / `SendMessage` 这类字符串方法调用  
6. 被反射直接读取却又加密的字段  

除此之外，大部分普通本地业务代码都应该继续正常混淆。

---

# 21. 参考依据（整理所依据的官方文档主题）

本文件整理基于 Obfuz 官方文档中的以下主题：

- Intro / 简介
- Obfuz CustomAttributes
- Serialization / 序列化
- Reflection / 反射
- Remove Const Field / 移除常量字段
- Field Encryption / 字段加密
- Deobfuscate StackTrace / 还原混淆堆栈日志
- Work with HybridCLR / 与 HybridCLR 协同工作

> 本文档已按“当前项目全部本地程序集运行”的实际情况做了二次整理，因此其中关于 HybridCLR 的部分为“条件性规则”，不是当前默认执行规则。
