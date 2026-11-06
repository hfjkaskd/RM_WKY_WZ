# APIReplace 执行提示

根据用户本次提供的发行参数和目标 Swagger，更新当前 Unity 项目。

只修改本次发行相关内容，不重构、不优化无关代码。

目标是同步：
发行配置、API Path、JSON Key、DTO 字段、对应请求构造。

不要求现有 C# 字段类型强制等于 Swagger 类型；类型差异按下文保留并报告。

---

## 0. Git 前置保护【必须先完成】

修改任何项目文件前，先检查：

```bash
git rev-parse --show-toplevel
git remote -v
git branch --show-current
git status --short --untracked-files=all
```

### 0.1 确认仓库

必须确认当前项目对应的 Git 仓库、远端地址和当前分支。

没有仓库、没有 remote，或无法确认仓库地址：
向用户索要 Git 仓库链接，并停止任务。

存在未解决冲突、正在进行的合并或变基等异常状态：
停止并报告。

禁止擅自 git init、猜测仓库地址、使用其它项目仓库或切换分支绕过问题。

### 0.2 必须新建一次提交

每次 APIReplace 开始前，都必须实际创建一条新提交。

有本地修改：提交当前内容。
没有本地修改：创建空提交。
禁止仅记录旧 HEAD 后直接开始修改。

先审查仓库和待提交范围。发现不应入库的敏感文件或无法确认的提交范围时，停止报告，不擅自删除文件或修改忽略规则。

确认后，在正确的仓库根目录执行：

```bash
git add -A
git commit --allow-empty -m "chore: backup before APIReplace"
git rev-parse HEAD
git status --short --untracked-files=all
```

必须确认提交成功且确实产生了新提交。

记录该提交的实际 SHA 为：

```text
APIReplaceBaselineCommit
```

本次计划修改的既有项目文件必须被基线覆盖。
提交失败、仍有未保存的目标文件改动，或无法取得有效 SHA 时，停止任务。

### 0.3 推送基线

将新建的基线提交推送到已确认的当前分支对应远端分支。

目标不明确、没有权限或 push 失败时，停止并报告。

禁止强制推送、自动合并、自动变基或改用其它远端绕过失败。

只有新提交成功、SHA 已记录、基线推送成功后，才开始修改项目文件。

### 0.4 修改结束后不自动提交

除非用户另有明确要求，不自动提交或推送本次 AI 修改。

保留修改供用户查看，并以 APIReplaceBaselineCommit 检查差异。

禁止重写基线提交、git reset --hard、git clean，或覆盖整个工作区。

---

## 1. 发行参数识别

按以下对应关系识别用户参数：

```text
应用 / 应用名称 / 游戏名称
→ 应用名称

包名 / Android 包名
→ Android Package Name

应用识别码 / Adjust识别码 / Adjust应用识别码
→ Adjust 应用识别码

SDK Key / MAX SDK Key / AppLovin SDK Key
→ AppLovin SDK Key

激励 / 激励视频 / reward
→ MAX 激励广告 ID

全屏 / 插屏 / interstitial
→ MAX 插屏广告 ID

应用ID
→ 后台 API AppId

密钥
→ 发行密钥

接口文档标签
→ Swagger Tag

接口域名地址
→ API Base URL

隐私链接
→ Privacy Policy URL
```

注意：

```text
应用识别码 ≠ 应用ID
SDK Key ≠ 发行密钥
```

重复且相同的值可以合并。
冲突值记录 Unresolved，停止依赖该参数的修改，不猜测。
未提供的参数保留旧值，不当作要求清空。

先确认当前项目中各配置的实际位置和用途，再更新。
禁止无条件全项目字符串替换。
位置无法确认时，保留原值并记录 Unresolved。

### 1.1 运行时配置来源核验【写回前必须完成】

修改发行配置前，必须从当前项目的实际启动入口追踪完整加载链路：

```text
启动入口
→ 配置加载方法
→ 实际读取文件的绝对路径
→ 解密 / 反序列化方法
→ 运行时配置实例
→ HTTP、Adjust、广告等实际使用位置
```

必须检查目标平台、编译宏，以及构建时是否存在配置生成、覆盖或复制步骤；不能仅凭文件存在、文件名相似、字段值匹配或旧项目经验确定修改位置。

写回前记录每组发行参数的：

```text
实际配置源与绝对路径
加载和使用代码的位置（文件、方法、行号）
解密 / 反序列化方法
构建时的生成或覆盖关系（如有）
本次修改字段
```

例如，若当前项目已经确认使用：

```text
WKY_Flow
→ ChannelConfigLoader
→ Assets/StreamingAssets/ChannelConfig.bytes
→ ChannelConfigBinarySerializer.Deserialize
→ ChannelConfig.Instance
```

则 AppId、域名、发行密钥、Adjust 识别码、激励和插屏 ID 应更新到该链路实际读取的 ChannelConfig.bytes。未参与运行时加载或构建生成的 Oversea.asset 等旧配置不得修改，也不得作为验证参数来源。

以上仅是加载链路示例，不能把 ChannelConfig.bytes 再当作所有项目的固定路径。应用名称、包名、MAX SDK Key、AdMob App ID、隐私链接等参数仍须分别确认实际配置来源，不得强行写入同一个文件。

配套 Skill、参考资料和测试脚本中的历史路径只可作为检索线索，不能代替上述核验。若它们仍固定使用 Oversea.asset，而当前代码读取其它配置，必须按已确认的实际链路处理，禁止静默回退到旧 asset。

无法确认加载链路、存在无法区分的配置源，或构建覆盖关系不明确时，记录 Unresolved-RuntimeConfigSource，停止相关变更组；不得先改旧模板凑齐配置。依赖同一应用身份的域名、AppId、密钥、API Path、DTO 和请求构造按第 7 节一起处理。

### 1.2 二进制配置写回与保护

实际配置源为 ChannelConfig.bytes 等二进制文件时：

- 优先使用当前项目自身的序列化器、解密方法及已确认的读写格式；不得将 JSON 或文本直接写入 bytes，也不得重新发明一套不兼容格式。
- 从现有文件读取完整配置，只更新用户本次提供的发行字段。未提供参数、调试开关、地区、广告策略和其它业务配置保持原值。
- 保持现有格式版本和加密约定；若现有工具会自动迁移版本或改写无关数据，先查明影响，无法在授权范围内解决时停止该组，不得静默升级。
- 保存后必须重新从磁盘读取实际配置文件，用项目对应的反序列化方法逐项核对发行参数和未修改配置；只检查保存前的内存对象不算验证。
- 序列化结果确定时，可在外部验证副本中恢复本次修改字段，比较序列化结果与原文件是否逐字节一致，证明没有改动其它配置；验证副本不得覆盖用户工作区。
- 若实际配置由其它源文件生成，必须核对源文件、生成步骤和最终加载文件，避免下次构建覆盖本次参数。不得为了省事修改运行时加载流程。
- 若发现此前误改了不生效的旧配置，只撤回本次 AI 对该文件的改动，保留用户和其它进程的修改。

---

## 2. Swagger 来源与接口范围

### 2.1 来源

优先按已确认的应用名称首字母查询：

```text
http://test.api.pandamerge.top/swagger/{a|b|c|...}/index.html
```

查找用户提供的 Swagger Tag。找不到，或无法确定首字母页面时，查询：

```text
http://test.api.pandamerge.top/swagger/all/index.html
```

接口通过“Swagger Tag + API 功能”准确匹配。
禁止根据相似名称、字段前缀或旧项目路径猜测。

字段以完整接口 Schema 为准，包括引用、组合定义、嵌套对象和数组元素。
不得只凭示例 JSON 中缺少某个 Key，就认定字段已经删除。

完整定义或目标接口无法确认时，记录 Unresolved-Swagger，停止对应修改。

### 2.2 空路径规则

以 APIReplaceBaselineCommit 中的原始 Path 判断是否接入：

- 普通可选接口 Path 为空：跳过，不补路径。
- Real Path 为空：只跳过 Real。
- Fake Path 为空：只跳过 Fake。
- base_list Path 为空：跳过配置查询，六个字符串保持不变。
- 用户归因：必须检查，不适用上述空路径跳过规则。

不得把原本非空的 Path 清空，以此规避检查。

归因 Path 为空时，只有现有配置位置明确、目标 Swagger 接口唯一确定，才允许补齐该 Path。
缺少归因接口或必要实现时，记录 Unresolved-RequiredAttribution；不得跳过，也不得擅自新建整套业务流程。

### 2.3 Path 更新

更新 AccountModuleCfg.cs 中本次已接入接口的 Path。

原本保存相对 Path，就继续保存相对 Path，不重复加入域名。

本文中的 OceanShine 类名和方法名是定位名称。
项目使用等价实现时检查对应实现，不强制改名或创建重复代码。

---

## 3. 统一保护规则

所有继续保留的现有 C# 字段，类型保持不变。

Swagger 与现有类型不同：

```text
保留原类型。
记录 TypeMismatch。
```

禁止通过删除后重建同一现有成员的方式改变类型。

Swagger 确实删除的字段，允许删除整个对应成员。
真正新增的字段，允许按照 Swagger 定义类型。

禁止修改：

```text
保留成员的名称和访问修饰符
现有方法名、返回类型、参数列表和参数类型
callback 类型或泛型
HttpUtil.RequestToServer<T> 泛型
现有业务取值表达式
业务条件判断和调用流程
全局序列化规则
```

允许的代码修改仅限：

```text
本次接口的 JsonProperty 和 DTO 字段增删
对应请求构造中的字段赋值增删
Dictionary / JObject / 手写 JSON 的对应字段和 Key
允许更新的 API Path
指定配置字段的字符串内容
本次发行配置
```

不得为了补充字段来源而新增方法参数或改写调用方业务。

同步要求与保护规则无法同时满足时，统一按第 7 节处理，不擅自放宽限制。

---

## 4. DTO 与请求构造同步

逐个接口、逐个对象层级比较 JSON Key。

范围包括：

```text
Request DTO
Response DTO
嵌套 class
List 内部 DTO
对应对象初始化
直接构造请求的 Dictionary / JObject / 手写 JSON
```

只处理对应接口的契约字段，不扩展删除无关的本地辅助成员。

### 4.1 Swagger 删除 Key

Swagger 中不存在的旧 Key，本地对应 DTO 中仍有时：

```text
删除对应 DTO 字段。
删除对应请求构造赋值。
删除直接构造 JSON 中的旧 Key。
```

不要默认用 JsonIgnore 代替删除。
不要只移除 JsonProperty，却保留旧契约字段并报告删除完成。

删除前检查引用和共享 DTO 用途。
涉及受保护业务代码，或会破坏其它接口且无法在允许范围内解决时，记录 Unresolved，按第 7 节处理。

### 4.2 Swagger 新增 Key

Swagger 中存在、本地对应 DTO 没有的 Key：

```text
增加对应 DTO 字段和正确的 JsonProperty。
新字段类型按照 Swagger 定义。
请求字段同时检查构造赋值。
```

不得因为本地暂时没有使用，就忽略新增字段。

### 4.3 明确的 Key 改名

有明确依据确认是同一业务字段改 Key 时，可以通过修改 JsonProperty 和请求构造中的 Key，完成旧 Key 移除、新 Key 接入。

保留原 C# 成员名、类型和业务取值。

无法确认是不是改名时，不为此停下来猜测，直接按字段集合执行删除和新增规则。

### 4.4 新增请求字段的数据来源

此规则统一适用于普通请求、Real、Fake 和用户归因：

```text
有明确的现成数据来源：
同步增加赋值。

没有明确数据来源：
记录 Unresolved-RequestValueSource。
不得编造值，不得修改受保护的方法签名或调用方业务。
不得报告该字段构造同步完成。
```

禁止无依据地使用空字符串、0、null、false，或依靠未赋值的默认值发送请求。

可选字段允许在请求中省略，不代表可以跳过 DTO 增加，也不代表缺少数据来源的构造同步已经完成。

受此问题影响的修改如何保留或撤回，统一按第 7 节处理。

---

## 5. Real / Fake 与用户归因专项

### 5.1 Real / Fake

分别按各自原始 Path 判断是否执行。

需要执行时，检查：

```csharp
OceanShineApplyWithdrawalRequestReal
GetApplyWithdrawalRequestReal

OceanShineApplyWithdrawalRequestFake
GetApplyWithdrawalRequestFake
```

同步核对 Swagger Request、DTO JSON 映射和对应构造赋值。

新增字段已有来源，必须实际增加赋值，不能用注释代替。
删除字段，同步删除 DTO 字段和构造赋值。

即使某个旧参数不再使用，也保留原方法参数列表。

### 5.2 用户归因【必须执行】

检查：

```csharp
OceanShineUserAttrsRequest

CreateFromJson(
    AdjustSdk.AdjustAttribution attribution,
    string eventName
)
```

按第 4 节同步：

```text
Swagger Request Key
DTO JsonProperty
CreateFromJson Dictionary Key
```

已有字段只改 Key 时，只修改 Dictionary 左侧 Key，不修改右侧业务取值，例如：

```text
attribution.Network
attribution.Campaign
attribution.Creative
attribution.CostAmount
eventName
Application.version
UserId
时间获取逻辑
```

新增字段没有来源，记录 Unresolved-AttributionValueSource，按统一缺失来源规则处理。

即使代码位于 #if BIZZA_ENABLE_ADJUST 内部，也必须检查。
不得因当前宏未启用而跳过，不擅自修改编译宏或 SDK 调用流程。

---

## 6. base_list 与六个字符串

检查 AccountModuleCfg 中：

```csharp
one_Count
one_Ratio
two_Count
two_Ratio
three_Count
three_Ratio
```

只更新这些字段中保存的字符串内容。
不修改字段名、类型或访问修饰符，也不把次数、金额、比例等业务数值当作目标内容写入。

### base_list 原始 Path 为空

不请求服务器。
六个字符串保持不变。
报告“不适用：原始路径为空”。

### base_list 原始 Path 非空

先确认目标 Path，再使用项目现有：

```csharp
OceanShineAppOtherConfigRequest
Request_AppOtherConfigRequest
```

或等价请求方式查询。

请求必须使用已确认属于同一目标应用的 Base URL、AppId、发行密钥及必要认证信息。
不得混用新旧应用参数。

写回前允许使用本次已确认的发行参数预查询；预查询成功只证明这组参数可通信，不能证明游戏实际配置已生效。
最终验收的配置查询必须从第 1.1 节确认的实际加载文件重新读取通信参数，并记录配置来源。不得用手填测试参数、旧 asset 或测试脚本的默认值代替运行时配置验证。

允许按接口实际要求自定义配置查询参数值，例如 Os_Cfn。
参数字段名必须符合接口定义，不得编造应用身份或认证信息，不得改写项目业务默认查询流程。

根据真实返回 JSON 中能够明确确认的对应关系，更新六个字符串。

禁止根据旧项目前缀、应用缩写或其它应用命名规律生成字符串。
仅在请求中试用过某个值，或看到服务器简单回显，不足以证明它就是正确配置。

无法确认的项保留原值，记录 Unresolved-BaseListConfig。
已确认项能否独立保留，统一按第 7 节判断。
只有六项均有明确依据并完成核对，才报告该专项通过。

---

## 7. 统一冲突与部分完成规则

开始修改前，识别哪些内容必须一起变化。
相互依赖的内容作为同一“变更组”处理，不按文件机械拆分。

例如：

```text
同一接口的 Path、DTO 和请求构造
被多个接口共用的 DTO
会共同影响请求的 Base URL、AppId、发行密钥及接口配置
```

处理规则：

1. Git 前置条件未完成：停止整个任务，不修改项目。
2. 某组缺少必要信息，或无法在保护规则内完整同步：停止该组；已经修改的，只撤回本次 AI 对该组的改动。
3. 其它已经确认独立、且能够完整同步的修改可以保留，整体报告部分完成。

不得只更新全局域名、AppId 或密钥，却留下与新配置不兼容的旧接口。
无法确认能否兼容，也不能视为已经独立完成。

TypeMismatch 按既定规则保留并报告，不单凭类型差异撤回已经完成的 Key 同步；若实际验证发现功能不兼容，再按本节处理。

仅因缺少环境而未能执行编译或运行验证，可以保留已经完成静态检查的修改，但必须报告“未验证”，不能宣称全部完成或全部验证通过。

撤回只针对本次 AI 改动，不恢复整个仓库，不覆盖用户或其它进程产生的修改。

---

## 8. 验证与 Git 差异检查

### 8.0 实际配置来源验证【必须执行】

确认本次修改的配置文件就是第 1.1 节证明会被目标平台加载的文件；如有生成链路，同时核对最终产物。

静态检查和允许的通信查询必须读取同一个已确认的配置来源。若现有测试脚本只支持 Oversea.asset 等旧路径，应在授权范围内适配外部测试工具的读取方式；不得据此反过来修改旧 asset，也不得将针对旧文件的检查报告为当前工程验证通过。

测试工具无法读取实际配置源时，记录 Unresolved-ConfigValidationSource，相关验证标记“未验证”。不得把复制一份手填配置、内存中的预期值或旧模板回退结果当作实际文件回读结果。

验证报告必须包含：实际配置绝对路径、加载代码证据、序列化 / 反序列化方法、字段回读结果、其它配置保持不变的检查结果，以及测试所用参数的配置来源。密钥与 Token 必须脱敏。

磁盘文件验证、Editor 中已加载实例和真机构建运行验证分别报告；文件写入或编译通过不能证明已运行的实例已刷新，更不能替代真机验证。

### 8.1 接口验证

对本次目标接口检查：

```text
新增字段是否补齐
删除字段是否移除
旧 Key 是否仍残留在对应请求构造中
嵌套对象和数组元素是否遗漏
请求字段是否有正确来源
响应字段是否能正确映射
共享 DTO 是否受到影响
```

Request 检查构造与序列化结果。
Response 检查字段映射与反序列化结果。

不要仅统计 JsonProperty 数量。
不要要求每次请求都发送全部可选字段。

Real/Fake 按各自接入范围检查。
用户归因必须检查。
base_list 核对真实返回依据及六个字符串。

除允许的 base_list 配置查询外，不自动调用服务器业务接口进行验证，尤其不得执行真实提现或归因上报。

### 8.2 保护与编译检查

对照基线检查字段类型、方法签名和业务取值是否被误改。
发现越界修改，只恢复本次误改部分。

有环境时执行编译并记录结果。
没有环境或依赖时报告“未验证”，不冒充通过或失败。

编译通过不等于 API 同步通过。

### 8.3 最终 Git 检查

先执行：

```bash
git status --short --untracked-files=all
```

检查修改、删除和新增文件。
对确认属于本次任务的新增文件，必要时执行：

```bash
git add -N -- "实际新增文件路径"
```

以下命令中的 <BASELINE_SHA> 必须替换为实际记录的提交 SHA，不得照抄占位符：

```bash
git diff --stat <BASELINE_SHA>
git diff --name-status <BASELINE_SHA>
git diff <BASELINE_SHA>
```

确认：

```text
差异都属于本次允许的修改
没有遗漏新增文件
没有无关格式化或重构
没有修改受保护的类型、签名和业务逻辑
没有误删或覆盖用户内容
最终报告与实际 Diff 一致
```

文件数量按最终去重清单统计。
发现并发产生的其它修改时，单独区分，不全部归为 AI 修改。

执行结束后不自动提交或推送 AI 修改。

---

## 9. 最终报告

状态统一定义：

```text
通过：实际检查或验证满足要求。
失败：实际检查或验证发现问题。
未验证：缺少环境、权限、依赖或必要数据，未完成验证。
不适用：按明确范围规则无需执行，并说明原因。
```

Unresolved 单独列出具体未解决事项。
归因不得报告为“不适用”。
必需项失败、未验证或存在阻塞性 Unresolved 时，不得报告全部完成。

简洁输出：

```text
执行结果：
- 完成 / 部分完成 / 已停止
- 未完成原因

Git：
- Repository / Branch
- Baseline Commit
- 执行前新提交结果
- 基线推送结果
- 本次修改文件数量与清单

修改：
- 实际配置源绝对路径、加载链路与代码证据
- 发行配置更新与保留项
- 二进制配置写回、生成关系及旧配置误改撤回情况（如有）
- API Path 修改
- JSON Key 修改
- DTO 新增与删除字段
- 请求构造同步
- 六个字符串更新情况及依据
- TypeMismatch：位置、原类型、Swagger 类型，确认未改类型

专项：
- Real：通过 / 失败 / 未验证 / 不适用，附原因
- Fake：通过 / 失败 / 未验证 / 不适用，附原因
- 用户归因：通过 / 失败 / 未验证，附原因
- base_list：通过 / 失败 / 未验证 / 不适用，附原因

验证：
- 实际配置文件回读、未修改配置保护及测试参数来源
- 类型和方法签名保护检查
- 序列化 / 反序列化验证
- 编译结果
- Git Diff 检查结果
- Unresolved

工作区：
- 未开始修改 / 已保留修改 / 已撤回全部本次修改 / 已保留部分修改
- 是否仍有未提交的 AI 修改
- 查看差异命令：git diff 实际基线SHA
```

仓库地址中的凭据、密钥和 Token 等敏感信息必须脱敏。
不要完整复述敏感值。