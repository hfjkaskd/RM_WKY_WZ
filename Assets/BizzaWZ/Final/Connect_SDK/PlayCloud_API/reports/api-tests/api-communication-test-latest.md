# 项目 API 通信测试报告

> 本报告由 `tools/InvokeApiCommunicationTest.py` 生成。测试工具只读 Unity 文件，未调用网页 `/api/apply`，未写回 Unity 项目。

## 测试上下文

- 流程文档：`C:\Users\Administrator\.codex\skills\replace-yun-api\references\API testing process.md`
- 参数来源：发行信息参考文件：C:\Users\Administrator\.codex\skills\replace-yun-api\references\Fill in the release information.md
- API 域名：`https://pharaohs.top`
- AppId：`arrowpathbrainfw`
- 包名：`com.algalaldevo.brainflowarrow`
- Unity 版本：``
- Unity 工程：`E:\Puzzle_Project\FakeProjects\ArrowSnap`
- 配置接口数量：0
- 目标项目每日配置键：0
- 目标项目 JSON 字段映射：0
- 请求方法：POST；请求头：`Content-Type: application/json`、`X-Bundle`
- 流程：新会话 login → 依赖调用 → ad_info `ad_load, ad_show` → 等待 5 秒 → add_ecpm

## 静态检查

- 流程文档结构：通过
- Unity 文件存在：否
- Unity 文件只读：是（测试前后 SHA-256 会在 JSON 报告中记录）
- AccountModuleCfg.cs 接口数：0（每日配置键未计入）
- AccountModule.cs 非空：否
- XXTEA/加密调用存在：否
- X-Bundle 请求头实现存在：否
- API 路径常量识别：否
- JSON 字段映射识别：否

## 在线通信

未执行（命令未带 `--live`）。
