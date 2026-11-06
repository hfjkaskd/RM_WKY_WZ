# OceanShine API Document

- Source page: http://test.api.pandamerge.top/swagger/index.html
- Swagger JSON: http://test.api.pandamerge.top/swagger/doc.json
- Generated at: 2026-04-19 14:37:45
- Group: `OceanShine`
- Domain used for full URLs: http://test.api.pandamerge.top
- Swagger host field: 
- Swagger basePath field: 
- Endpoint count: 21

## Common Notes

- Common header: `X-Bundle`, type `string`, required, description `??`
- Most endpoints use `POST` + `application/json`
- Swagger `host` / `basePath` are empty, so the full URLs below are composed with `http://test.api.pandamerge.top`
- Request and response schemas are expanded below. Array item structures are marked with `[]`.

## Index

| No | Method | Path | Summary | operationId |
| --- | --- | --- | --- | --- |
| 1 | POST | /ocean/api_v1/add_ecpm | 添加收益 | OceanShineBalanceReportEcpm |
| 2 | POST | /ocean/api_v1/add_order | 去提现-全部提现 | OceanShineApplyWithdrawal |
| 3 | POST | /ocean/api_v1/add_order_task | 新接口-去提现-现金或奖卷 | OceanShineApplyWithdrawalMoneyRoll |
| 4 | POST | /ocean/api_v1/app_suggest | 游戏推荐 | OceanShineGetAppSuggest |
| 5 | POST | /ocean/api_v1/base_list | 其它配置信息 | OceanShineAppOtherConfigV2 |
| 6 | POST | /ocean/api_v1/dt_rewal | 添加收益-DigitalTurbine | OceanShineBalanceReportDigitalTurbine |
| 7 | POST | /ocean/api_v1/from | 用户归因 | OceanShineUserAttrs |
| 8 | POST | /ocean/api_v1/get_ecpm_id | 获取收益ID | OceanShineGetBalanceReportId |
| 9 | POST | /ocean/api_v1/login | 用户登陆 | OceanShineUserLogin |
| 10 | POST | /ocean/api_v1/msg | 添加反馈 | OceanShineFeedback |
| 11 | POST | /ocean/api_v1/msg_list | 反馈列表 | OceanShineFeedbackList |
| 12 | POST | /ocean/api_v1/new_user | 新用户奖励-领取 | OceanShineNewComerReport |
| 13 | POST | /ocean/api_v1/number | 关卡，完成次数上报 | OceanShineUserReachReport |
| 14 | POST | /ocean/api_v1/order_list | 订单列表 | OceanShineWithdrawalRecords |
| 15 | POST | /ocean/api_v1/order_name | 提现用户列表 | OceanShineWithdrawalUserList |
| 16 | POST | /ocean/api_v1/plat_from | 提现平台 | OceanShineWithdrawalPage |
| 17 | POST | /ocean/api_v1/task_list | 每日任务-现金 | OceanShineaskLookAdMoney |
| 18 | POST | /ocean/api_v1/user_info | 用户信息 | OceanShineUserInfo |
| 19 | POST | /ocean/api_v1/user_name | 昵称添加 | OceanShineUserNickName |
| 20 | POST | /ocean/api_v1Log/ad_info | 广告上报 | OceanShineAdLogReport |
| 21 | POST | /ocean/api_v1Log/app_info | 日志上报 | OceanShineAppEventReport |

## 1. 添加收益

- Method: `POST`
- Path: `/ocean/api_v1/add_ecpm`
- Full URL: `http://test.api.pandamerge.top/ocean/api_v1/add_ecpm`
- operationId: `OceanShineBalanceReportEcpm`
- Description: 添加收益-定制版参数
- Content-Type: `application/json`

### Headers

| Name | In | Type | Required | Description |
| --- | --- | --- | --- | --- |
| X-Bundle | header | string | Yes | 包名 |

### Other Parameters

None

### Request Body

- Parameter: `request.OceanShineAdRevenueReportReq`
- Description: 请求体
- Schema: `request.OceanShineAdRevenueReportReq`

| field | type | required | description |
| --- | --- | --- | --- |
| Os_Apid | string | No | 应用ID |
| Os_Btid | string | No | bId 获取收益ID |
| Os_Ecm | number | No | 广告的ECPM |
| Os_Sal | string | No | 特殊标识 |
| Os_Usid | string | No | 用户ID |
| Os_Vn | string | No |  |

### Responses

#### HTTP 200

- Description: OK
- Schema: `response.OceanShineCalculateAdRevenueResponse`

| field | type | required | description |
| --- | --- | --- | --- |
| Os_Bac | integer | No | 获取的积分 |
| Os_Con | integer | No | 获取的金币-有可能为0->coin |
| Os_Mul | number | No | 原基础上增加倍数，1为1倍，2为2倍，0.1为10%，0.05为5% |
| Os_Prc | number | No | 获取的金额-当前国家->price |
| Os_Uso | object (response.OceanShineUserInfo) | No | 用户信息-userInfo |
| Os_Uso.Os_Bac | number | No | 当前金额-balance |
| Os_Uso.Os_Baci | integer | No | 当前积分数-balance_int |
| Os_Uso.Os_Con | integer | No | 当前金币数-coin |
| Os_Uso.Os_Crc | string | No | 国家货币-currency |
| Os_Uso.Os_Crcs | string | No | 国家货币符号-currencySymbols |
| Os_Uso.Os_Cty | string | No | 国家-country |
| Os_Uso.Os_Ewl | number | No | 可提现金额-enableWithdrawal |
| Os_Uso.Os_Img | integer | No | 是否有消息未读，1是，0否 |
| Os_Uso.Os_Lev | integer | No | 当前关卡-level |
| Os_Uso.Os_Lgd | integer | No | 用户连续登陆天数-loginDay |
| Os_Uso.Os_Mny | number | No | 用户真实的钱（货币） |
| Os_Uso.Os_Ncm | boolean | No | 新手奖励信息-newComer |
| Os_Uso.Os_Nnm | string | No | 用户昵称-nickName |
| Os_Uso.Os_Rol | number | No | 用户的-卷（货币） |
| Os_Uso.Os_Rti | integer | No | 注册时间-registerTime |
| Os_Uso.Os_Rts | integer | No | 当前关卡-reachTimes |
| Os_Uso.Os_Tra | number | No | 可提现比例-taxRate |

## 2. 去提现-全部提现

- Method: `POST`
- Path: `/ocean/api_v1/add_order`
- Full URL: `http://test.api.pandamerge.top/ocean/api_v1/add_order`
- operationId: `OceanShineApplyWithdrawal`
- Description: 去提现
- Content-Type: `application/json`

### Headers

| Name | In | Type | Required | Description |
| --- | --- | --- | --- | --- |
| X-Bundle | header | string | Yes | 包名 |

### Other Parameters

None

### Request Body

- Parameter: `request.OceanShineApplyWithdrawalReq`
- Description: 请求体
- Schema: `request.OceanShineApplyWithdrawalReq`

| field | type | required | description |
| --- | --- | --- | --- |
| Os_Apid | string | No | 应用ID |
| Os_Cp | string | No | CPF |
| Os_Gdid | integer | No | 商品ID |
| Os_Mid | integer | No | 商品中的-支付配置ID |
| Os_Pb | string | No | Pix账号绑定类型,P E C B |
| Os_Ra | string | No | ReceiverAccount |
| Os_Re | string | No | ReceiverEmail |
| Os_Rm | string | No | ReceiverMobile |
| Os_Rn | string | No | ReceiverName |
| Os_Seid | string | No | 客户端UUID |
| Os_Usid | string | No | 用户ID |
| Os_Vn | string | No | 版本 |

### Responses

#### HTTP 200

- Description: OK
- Schema: `response.OceanShineApplyWithdrawalResponse`

| field | type | required | description |
| --- | --- | --- | --- |
| Os_Odr | string | No | 订单号 - orderNo |

## 3. 新接口-去提现-现金或奖卷

- Method: `POST`
- Path: `/ocean/api_v1/add_order_task`
- Full URL: `http://test.api.pandamerge.top/ocean/api_v1/add_order_task`
- operationId: `OceanShineApplyWithdrawalMoneyRoll`
- Description: 新接口-去提现-现金或奖卷
- Content-Type: `application/json`

### Headers

| Name | In | Type | Required | Description |
| --- | --- | --- | --- | --- |
| X-Bundle | header | string | Yes | 包名 |

### Other Parameters

None

### Request Body

- Parameter: `request.OceanShineApplyWithdrawalMoneyRollReq`
- Description: 请求体
- Schema: `request.OceanShineApplyWithdrawalMoneyRollReq`

| field | type | required | description |
| --- | --- | --- | --- |
| Os_Apid | string | No | 应用ID |
| Os_Cp | string | No | CPF |
| Os_Gdid | integer | No | 商品ID |
| Os_Mid | integer | No | 商品中的-支付配置ID |
| Os_Mn | string | No | ApplyType       string `json:"Os_At"`   //现金或卷提现 applyType   money或rolll |
| Os_Pb | string | No | Pix账号绑定类型,P E C B |
| Os_Ra | string | No | ReceiverAccount |
| Os_Re | string | No | ReceiverEmail |
| Os_Rm | string | No | ReceiverMobile |
| Os_Rn | string | No | ReceiverName |
| Os_Seid | string | No | 客户端UUID |
| Os_Usid | string | No | 用户ID |
| Os_Vn | string | No | vn |

### Responses

#### HTTP 200

- Description: OK
- Schema: `response.OceanShineApplyWithdrawalResponse`

| field | type | required | description |
| --- | --- | --- | --- |
| Os_Odr | string | No | 订单号 - orderNo |

## 4. 游戏推荐

- Method: `POST`
- Path: `/ocean/api_v1/app_suggest`
- Full URL: `http://test.api.pandamerge.top/ocean/api_v1/app_suggest`
- operationId: `OceanShineGetAppSuggest`
- Description: 游戏推荐
- Content-Type: `application/json`

### Headers

| Name | In | Type | Required | Description |
| --- | --- | --- | --- | --- |
| X-Bundle | header | string | Yes | 包名 |

### Other Parameters

None

### Request Body

- Parameter: `request.OceanShineGetAppSuggestReq`
- Description: 请求体
- Schema: `request.OceanShineGetAppSuggestReq`

| field | type | required | description |
| --- | --- | --- | --- |
| Os_Apid | string | No | 应用ID |
| Os_Try | string | No | 国家码 |

### Responses

#### HTTP 200

- Description: OK
- Schema: `response.OceanShineGetAppSuggestResponse`

| field | type | required | description |
| --- | --- | --- | --- |
| Os_Apid | string | No |  |
| Os_Ic | string | No | 应用图标 |
| Os_Id | integer | No |  |
| Os_Nm | string | No | 应用名称 |
| Os_St | integer | No |  |
| Os_Tx | string | No | 应用描述 |
| Os_Ur | string | No | 应用打开地址 |

## 5. 其它配置信息

- Method: `POST`
- Path: `/ocean/api_v1/base_list`
- Full URL: `http://test.api.pandamerge.top/ocean/api_v1/base_list`
- operationId: `OceanShineAppOtherConfigV2`
- Description: 其它配置信息
- Content-Type: `application/json`

### Headers

| Name | In | Type | Required | Description |
| --- | --- | --- | --- | --- |
| X-Bundle | header | string | Yes | 包名 |

### Other Parameters

None

### Request Body

- Parameter: `request.OceanShineAppOtherConfigReq`
- Description: 请求体
- Schema: `request.OceanShineAppOtherConfigReq`

| field | type | required | description |
| --- | --- | --- | --- |
| Os_Apid | string | No | 应用ID |
| Os_Vn | string | No | vn |
| Os_cfn | string | No | 配置key |

### Responses

#### HTTP 200

- Description: json原样输出
- Schema: `None`

None

## 6. 添加收益-DigitalTurbine

- Method: `POST`
- Path: `/ocean/api_v1/dt_rewal`
- Full URL: `http://test.api.pandamerge.top/ocean/api_v1/dt_rewal`
- operationId: `OceanShineBalanceReportDigitalTurbine`
- Description: 添加收益-DigitalTurbine
- Content-Type: `application/json`

### Headers

| Name | In | Type | Required | Description |
| --- | --- | --- | --- | --- |
| X-Bundle | header | string | Yes | 包名 |

### Other Parameters

None

### Request Body

- Parameter: `request.OceanShineDigitalTurbineReportReq`
- Description: 请求体
- Schema: `request.OceanShineDigitalTurbineReportReq`

| field | type | required | description |
| --- | --- | --- | --- |
| Os_Apid | string | No | 应用ID |
| Os_Btid | string | No | bId 获取收益ID |
| Os_Usid | string | No | 用户ID |
| Os_Vn | string | No |  |

### Responses

#### HTTP 200

- Description: OK
- Schema: `response.OceanShineCalculateAdRevenueResponse`

| field | type | required | description |
| --- | --- | --- | --- |
| Os_Bac | integer | No | 获取的积分 |
| Os_Con | integer | No | 获取的金币-有可能为0->coin |
| Os_Mul | number | No | 原基础上增加倍数，1为1倍，2为2倍，0.1为10%，0.05为5% |
| Os_Prc | number | No | 获取的金额-当前国家->price |
| Os_Uso | object (response.OceanShineUserInfo) | No | 用户信息-userInfo |
| Os_Uso.Os_Bac | number | No | 当前金额-balance |
| Os_Uso.Os_Baci | integer | No | 当前积分数-balance_int |
| Os_Uso.Os_Con | integer | No | 当前金币数-coin |
| Os_Uso.Os_Crc | string | No | 国家货币-currency |
| Os_Uso.Os_Crcs | string | No | 国家货币符号-currencySymbols |
| Os_Uso.Os_Cty | string | No | 国家-country |
| Os_Uso.Os_Ewl | number | No | 可提现金额-enableWithdrawal |
| Os_Uso.Os_Img | integer | No | 是否有消息未读，1是，0否 |
| Os_Uso.Os_Lev | integer | No | 当前关卡-level |
| Os_Uso.Os_Lgd | integer | No | 用户连续登陆天数-loginDay |
| Os_Uso.Os_Mny | number | No | 用户真实的钱（货币） |
| Os_Uso.Os_Ncm | boolean | No | 新手奖励信息-newComer |
| Os_Uso.Os_Nnm | string | No | 用户昵称-nickName |
| Os_Uso.Os_Rol | number | No | 用户的-卷（货币） |
| Os_Uso.Os_Rti | integer | No | 注册时间-registerTime |
| Os_Uso.Os_Rts | integer | No | 当前关卡-reachTimes |
| Os_Uso.Os_Tra | number | No | 可提现比例-taxRate |

## 7. 用户归因

- Method: `POST`
- Path: `/ocean/api_v1/from`
- Full URL: `http://test.api.pandamerge.top/ocean/api_v1/from`
- operationId: `OceanShineUserAttrs`
- Description: 用户归因
- Content-Type: `application/json`

### Headers

| Name | In | Type | Required | Description |
| --- | --- | --- | --- | --- |
| X-Bundle | header | string | Yes | 包名 |

### Other Parameters

None

### Request Body

- Parameter: `request.OceanShineUserAttrsReq`
- Description: 请求体
- Schema: `request.OceanShineUserAttrsReq`

| field | type | required | description |
| --- | --- | --- | --- |
| Os_Agp | string | No | 设备当前归因广告组的名称 |
| Os_Aid | string | No | 设备的唯一 Adjust ID |
| Os_Apid | string | No | 应用ID |
| Os_Cat | number | No | 安装成本-costAmount |
| Os_Ccy | string | No | 成本相关的货币代码-costCurrency |
| Os_Ckl | string | No | 安装被标记的 点击标签-clickLabel |
| Os_Cmp | string | No | 设备当前归因-campaign |
| Os_Cti | string | No | 设备当前归因素材的名称-creative |
| Os_Ctm | integer | No | 客户端收到回调的时间-clientTime |
| Os_Cty | string | No | 推广活动定价模型-costType |
| Os_Dlu | string | No | 投放参数-deepLinkUrl |
| Os_Evt | string | No | 触发归因的事件-event |
| Os_Fbr | string | No | 回调原始值-fbInstallReferrer |
| Os_Gct | string | No | google点击时间-ggClickTime |
| Os_Net | string | No | 归因渠道名称-network |
| Os_Trn | string | No | 归因跟踪码-trackerName |
| Os_Trt | string | No | 归因跟踪名称-trackerToken |
| Os_Usid | string | No | 用户ID |
| Vn | string | No |  |

### Responses

## 8. 获取收益ID

- Method: `POST`
- Path: `/ocean/api_v1/get_ecpm_id`
- Full URL: `http://test.api.pandamerge.top/ocean/api_v1/get_ecpm_id`
- operationId: `OceanShineGetBalanceReportId`
- Description: 获取收益ID-定制版参数
- Content-Type: `application/json`

### Headers

| Name | In | Type | Required | Description |
| --- | --- | --- | --- | --- |
| X-Bundle | header | string | Yes | 包名 |

### Other Parameters

None

### Request Body

- Parameter: `request.OceanShineGetAdRevenueReportIdReq`
- Description: 请求体
- Schema: `request.OceanShineGetAdRevenueReportIdReq`

| field | type | required | description |
| --- | --- | --- | --- |
| Os_Apid | string | No | 应用ID |
| Os_Usid | string | No | 用户ID |
| Os_Vn | string | No |  |

### Responses

#### HTTP 200

- Description: OK
- Schema: `response.OceanShineGetAdRevenueReportIdResponse`

| field | type | required | description |
| --- | --- | --- | --- |
| Os_Btid | string | No | batch_id-收益ID |
| Os_Pfm | string | No | 展示那一个广告平台的广告 max、topon、exceed（超出上限）、error（未知错误） |

## 9. 用户登陆

- Method: `POST`
- Path: `/ocean/api_v1/login`
- Full URL: `http://test.api.pandamerge.top/ocean/api_v1/login`
- operationId: `OceanShineUserLogin`
- Description: 用户登陆-定制版参数
- Content-Type: `application/json`

### Headers

| Name | In | Type | Required | Description |
| --- | --- | --- | --- | --- |
| X-Bundle | header | string | Yes | 包名 |

### Other Parameters

None

### Request Body

- Parameter: `request.OceanShineUserLoginReq`
- Description: 请求体
- Schema: `request.OceanShineUserLoginReq`

| field | type | required | description |
| --- | --- | --- | --- |
| Os_Anid | string | No | 安卓ID |
| Os_Apid | string | No | 应用ID |
| Os_Atd | integer | No | 调试模式 |
| Os_Bbd | string | No | 手机品牌 |
| Os_Cal | string | No | 来源 |
| Os_Cpu | string | No | CPU架构 |
| Os_Ctr | integer | No | 当前小时值 |
| Os_Dtd | string | No | 屏幕密度 |
| Os_Dth | integer | No | 屏幕高 |
| Os_Dtw | integer | No | 屏幕宽 |
| Os_Gaid | string | No | GoogleId |
| Os_Lag | string | No | 语言 |
| Os_Mbl | string | No | 设备型号 |
| Os_Nbt | string | No | 网络 |
| Os_Obv | string | No | 系统版本 |
| Os_Rtt | integer | No | 是否root |
| Os_Sbi | integer | No | SIM卡 |
| Os_Seid | string | No | 客户端UUID |
| Os_Try | string | No | 国家码 |
| Os_Ttz | string | No | 时区 |
| Os_Ua | string | No | 客户端 |
| Os_Usid | string | No | 用户ID |
| Os_Vc | integer | No | App版本号 |
| Os_Vn | string | No | App版本名 |

### Responses

#### HTTP 200

- Description: OK
- Schema: `response.OceanShineLoginResponse`

| field | type | required | description |
| --- | --- | --- | --- |
| Os_Aru | object | No | activeRule |
| Os_Aru.Os_Acm | number | No | ad_cpm |
| Os_Aru.Os_Aiu | number | No | ad_ipu |
| Os_Rdt | string | No | 注册日期 |
| Os_Rti | integer | No | 注册时间戳 |
| Os_Rw | integer | No | 1为审核模式，0或2为投放模式 |
| Os_Usid | string | No | 用户ID |

## 10. 添加反馈

- Method: `POST`
- Path: `/ocean/api_v1/msg`
- Full URL: `http://test.api.pandamerge.top/ocean/api_v1/msg`
- operationId: `OceanShineFeedback`
- Description: 添加反馈
- Content-Type: `application/json`

### Headers

| Name | In | Type | Required | Description |
| --- | --- | --- | --- | --- |
| X-Bundle | header | string | Yes | 包名 |

### Other Parameters

None

### Request Body

- Parameter: `request.OceanShineFeedbackReq`
- Description: 请求体
- Schema: `request.OceanShineFeedbackReq`

| field | type | required | description |
| --- | --- | --- | --- |
| Os_Apid | string | No | 应用ID |
| Os_Des | string | No | 内容-description |
| Os_Usid | string | No | 用户ID |
| Os_Vn | string | No |  |

### Responses

## 11. 反馈列表

- Method: `POST`
- Path: `/ocean/api_v1/msg_list`
- Full URL: `http://test.api.pandamerge.top/ocean/api_v1/msg_list`
- operationId: `OceanShineFeedbackList`
- Description: 反馈列表
- Content-Type: `application/json`

### Headers

| Name | In | Type | Required | Description |
| --- | --- | --- | --- | --- |
| X-Bundle | header | string | Yes | 包名 |

### Other Parameters

None

### Request Body

- Parameter: `request.OceanShineFeedbackListV2Req`
- Description: 请求体
- Schema: `request.OceanShineFeedbackListV2Req`

| field | type | required | description |
| --- | --- | --- | --- |
| Os_Apid | string | No | 应用ID |
| Os_Usid | string | No | 用户ID |
| Os_Vn | string | No |  |

### Responses

#### HTTP 200

- Description: OK
- Schema: `response.OceanShineFeedbackListV2Response`

| field | type | required | description |
| --- | --- | --- | --- |
| Os_Msl | array<object (response.OceanShineFeedbackListV2ResponseData)> | No | 聊天信息->msg |
| Os_Msl[].Os_Apid | string | No | 应用ID |
| Os_Msl[].Os_Cda | string | No | 提交时间-created_at |
| Os_Msl[].Os_Cte | integer | No | 提交时间-毫秒-created_time |
| Os_Msl[].Os_Ctt | string | No | 内容-content->text->msg |
| Os_Msl[].Os_Id | integer | No | 数据库ID->id->id |
| Os_Msl[].Os_Tpe | integer | No | 1用户反馈，2后台回答-type |
| Os_Msl[].Os_Uda | string | No | 更新时间-updated_at |
| Os_Msl[].Os_Usid | string | No | 用户ID |
| Os_Msl[].Os_Ute | integer | No | 更新时间-毫秒-updated_time |
| Os_Msl[].Os_Vn | string | No | 版本号-vn |
| Os_Uso | object (response.OceanShineUserInfo) | No | 用户信息-userInfo |
| Os_Uso.Os_Bac | number | No | 当前金额-balance |
| Os_Uso.Os_Baci | integer | No | 当前积分数-balance_int |
| Os_Uso.Os_Con | integer | No | 当前金币数-coin |
| Os_Uso.Os_Crc | string | No | 国家货币-currency |
| Os_Uso.Os_Crcs | string | No | 国家货币符号-currencySymbols |
| Os_Uso.Os_Cty | string | No | 国家-country |
| Os_Uso.Os_Ewl | number | No | 可提现金额-enableWithdrawal |
| Os_Uso.Os_Img | integer | No | 是否有消息未读，1是，0否 |
| Os_Uso.Os_Lev | integer | No | 当前关卡-level |
| Os_Uso.Os_Lgd | integer | No | 用户连续登陆天数-loginDay |
| Os_Uso.Os_Mny | number | No | 用户真实的钱（货币） |
| Os_Uso.Os_Ncm | boolean | No | 新手奖励信息-newComer |
| Os_Uso.Os_Nnm | string | No | 用户昵称-nickName |
| Os_Uso.Os_Rol | number | No | 用户的-卷（货币） |
| Os_Uso.Os_Rti | integer | No | 注册时间-registerTime |
| Os_Uso.Os_Rts | integer | No | 当前关卡-reachTimes |
| Os_Uso.Os_Tra | number | No | 可提现比例-taxRate |

## 12. 新用户奖励-领取

- Method: `POST`
- Path: `/ocean/api_v1/new_user`
- Full URL: `http://test.api.pandamerge.top/ocean/api_v1/new_user`
- operationId: `OceanShineNewComerReport`
- Description: 新用户奖励-领取
- Content-Type: `application/json`

### Headers

| Name | In | Type | Required | Description |
| --- | --- | --- | --- | --- |
| X-Bundle | header | string | Yes | 包名 |

### Other Parameters

None

### Request Body

- Parameter: `request.OceanShineUserReachReportReq`
- Description: 请求体
- Schema: `request.OceanShineUserReachReportReq`

| field | type | required | description |
| --- | --- | --- | --- |
| Os_Apid | string | No | 应用ID |
| Os_Usid | string | No | 用户ID |

### Responses

#### HTTP 200

- Description: OK
- Schema: `array<response.OceanShineUserInfoResponse>`

| field | type | required | description |
| --- | --- | --- | --- |
| [].Os_Acg | object (response.OceanShineAppConfig) | No | 新手奖励信息-appConfig |
| [].Os_Acg.Os_Com | integer | No | commonMerge |
| [].Os_Acg.Os_Nba | integer | No | 新手奖励积分数-new_balance |
| [].Os_Acg.Os_Ncc | integer | No | 新手奖励金币-new_comer_coin |
| [].Os_Acg.Os_Rnw | number | No | 新手奖励金额-newComerReward |
| [].Os_Acg.Os_Usd | number | No | 转usd 比例-toUsd |
| [].Os_Uso | object (response.OceanShineUserInfo) | No | 用户信息-userInfo |
| [].Os_Uso.Os_Bac | number | No | 当前金额-balance |
| [].Os_Uso.Os_Baci | integer | No | 当前积分数-balance_int |
| [].Os_Uso.Os_Con | integer | No | 当前金币数-coin |
| [].Os_Uso.Os_Crc | string | No | 国家货币-currency |
| [].Os_Uso.Os_Crcs | string | No | 国家货币符号-currencySymbols |
| [].Os_Uso.Os_Cty | string | No | 国家-country |
| [].Os_Uso.Os_Ewl | number | No | 可提现金额-enableWithdrawal |
| [].Os_Uso.Os_Img | integer | No | 是否有消息未读，1是，0否 |
| [].Os_Uso.Os_Lev | integer | No | 当前关卡-level |
| [].Os_Uso.Os_Lgd | integer | No | 用户连续登陆天数-loginDay |
| [].Os_Uso.Os_Mny | number | No | 用户真实的钱（货币） |
| [].Os_Uso.Os_Ncm | boolean | No | 新手奖励信息-newComer |
| [].Os_Uso.Os_Nnm | string | No | 用户昵称-nickName |
| [].Os_Uso.Os_Rol | number | No | 用户的-卷（货币） |
| [].Os_Uso.Os_Rti | integer | No | 注册时间-registerTime |
| [].Os_Uso.Os_Rts | integer | No | 当前关卡-reachTimes |
| [].Os_Uso.Os_Tra | number | No | 可提现比例-taxRate |

## 13. 关卡，完成次数上报

- Method: `POST`
- Path: `/ocean/api_v1/number`
- Full URL: `http://test.api.pandamerge.top/ocean/api_v1/number`
- operationId: `OceanShineUserReachReport`
- Description: 关卡，完成次数上报
- Content-Type: `application/json`

### Headers

| Name | In | Type | Required | Description |
| --- | --- | --- | --- | --- |
| X-Bundle | header | string | Yes | 包名 |

### Other Parameters

None

### Request Body

- Parameter: `request.OceanShineUserReachReportReq`
- Description: 请求体
- Schema: `request.OceanShineUserReachReportReq`

| field | type | required | description |
| --- | --- | --- | --- |
| Os_Apid | string | No | 应用ID |
| Os_Usid | string | No | 用户ID |

### Responses

#### HTTP 200

- Description: OK
- Schema: `array<response.OceanShineUserInfoResponse>`

| field | type | required | description |
| --- | --- | --- | --- |
| [].Os_Acg | object (response.OceanShineAppConfig) | No | 新手奖励信息-appConfig |
| [].Os_Acg.Os_Com | integer | No | commonMerge |
| [].Os_Acg.Os_Nba | integer | No | 新手奖励积分数-new_balance |
| [].Os_Acg.Os_Ncc | integer | No | 新手奖励金币-new_comer_coin |
| [].Os_Acg.Os_Rnw | number | No | 新手奖励金额-newComerReward |
| [].Os_Acg.Os_Usd | number | No | 转usd 比例-toUsd |
| [].Os_Uso | object (response.OceanShineUserInfo) | No | 用户信息-userInfo |
| [].Os_Uso.Os_Bac | number | No | 当前金额-balance |
| [].Os_Uso.Os_Baci | integer | No | 当前积分数-balance_int |
| [].Os_Uso.Os_Con | integer | No | 当前金币数-coin |
| [].Os_Uso.Os_Crc | string | No | 国家货币-currency |
| [].Os_Uso.Os_Crcs | string | No | 国家货币符号-currencySymbols |
| [].Os_Uso.Os_Cty | string | No | 国家-country |
| [].Os_Uso.Os_Ewl | number | No | 可提现金额-enableWithdrawal |
| [].Os_Uso.Os_Img | integer | No | 是否有消息未读，1是，0否 |
| [].Os_Uso.Os_Lev | integer | No | 当前关卡-level |
| [].Os_Uso.Os_Lgd | integer | No | 用户连续登陆天数-loginDay |
| [].Os_Uso.Os_Mny | number | No | 用户真实的钱（货币） |
| [].Os_Uso.Os_Ncm | boolean | No | 新手奖励信息-newComer |
| [].Os_Uso.Os_Nnm | string | No | 用户昵称-nickName |
| [].Os_Uso.Os_Rol | number | No | 用户的-卷（货币） |
| [].Os_Uso.Os_Rti | integer | No | 注册时间-registerTime |
| [].Os_Uso.Os_Rts | integer | No | 当前关卡-reachTimes |
| [].Os_Uso.Os_Tra | number | No | 可提现比例-taxRate |

## 14. 订单列表

- Method: `POST`
- Path: `/ocean/api_v1/order_list`
- Full URL: `http://test.api.pandamerge.top/ocean/api_v1/order_list`
- operationId: `OceanShineWithdrawalRecords`
- Description: 订单列表
- Content-Type: `application/json`

### Headers

| Name | In | Type | Required | Description |
| --- | --- | --- | --- | --- |
| X-Bundle | header | string | Yes | 包名 |

### Other Parameters

None

### Request Body

- Parameter: `request.OceanShineUserInfoReq`
- Description: 请求体
- Schema: `request.OceanShineUserInfoReq`

| field | type | required | description |
| --- | --- | --- | --- |
| Os_Apid | string | No | 应用ID |
| Os_Usid | string | No | 用户ID |

### Responses

#### HTTP 200

- Description: OK
- Schema: `array<response.OceanShineWithdrawalRecord>`

| field | type | required | description |
| --- | --- | --- | --- |
| [].Os_Cp | string | No | CPF |
| [].Os_Crc | string | No | 货币码-currency |
| [].Os_Crcs | string | No | 货币符号-currencySymbols |
| [].Os_Dat | string | No | 提现时间->date |
| [].Os_Ded | number | No | 扣除金额或现金提现时转roll金额 |
| [].Os_Id | integer | No | ID-订单 |
| [].Os_Pb | string | No | Pix账号绑定类型,P E C B |
| [].Os_Prc | number | No | 金额-amount |
| [].Os_Pym | string | No | payName 支付名称 |
| [].Os_Ra | string | No | ReceiverAccount |
| [].Os_Re | string | No | ReceiverEmail |
| [].Os_Rm | string | No | ReceiverMobile |
| [].Os_Rn | string | No | ReceiverName |
| [].Os_Rrm | string | No | 提示信息-remarks |
| [].Os_Sts | integer | No | 状态信息，1进行中，2违规被拒，3成功，4失败 |
| [].Os_Try | integer | No | 1 现金提现，2roll提现 |
| [].Os_Tsm | string | No | 提示信息（现金提现失败后） |

## 15. 提现用户列表

- Method: `POST`
- Path: `/ocean/api_v1/order_name`
- Full URL: `http://test.api.pandamerge.top/ocean/api_v1/order_name`
- operationId: `OceanShineWithdrawalUserList`
- Description: 提现用户列表
- Content-Type: `application/json`

### Headers

| Name | In | Type | Required | Description |
| --- | --- | --- | --- | --- |
| X-Bundle | header | string | Yes | 包名 |

### Other Parameters

None

### Request Body

- Parameter: `request.OceanShineWithdrawalListReq`
- Description: 请求体
- Schema: `request.OceanShineWithdrawalListReq`

| field | type | required | description |
| --- | --- | --- | --- |
| Os_Apid | string | No | 应用ID |
| Os_Usid | string | No | 用户ID |
| Os_Vn | string | No |  |

### Responses

#### HTTP 200

- Description: OK
- Schema: `array<response.OceanShineWithdrawalUserListResponseData>`

| field | type | required | description |
| --- | --- | --- | --- |
| [].Os_Css | string | No | 国家货币符号 |
| [].Os_Mny | number | No | 提现金额 |
| [].Os_Nnm | string | No | 用户昵称 |

## 16. 提现平台

- Method: `POST`
- Path: `/ocean/api_v1/plat_from`
- Full URL: `http://test.api.pandamerge.top/ocean/api_v1/plat_from`
- operationId: `OceanShineWithdrawalPage`
- Description: 提现平台
- Content-Type: `application/json`

### Headers

| Name | In | Type | Required | Description |
| --- | --- | --- | --- | --- |
| X-Bundle | header | string | Yes | 包名 |

### Other Parameters

None

### Request Body

- Parameter: `request.OceanShineWithdrawalPageReq`
- Description: 请求体
- Schema: `request.OceanShineWithdrawalPageReq`

| field | type | required | description |
| --- | --- | --- | --- |
| Os_Apid | string | No | 应用ID |
| Os_Usid | string | No | 用户ID |
| Os_Vn | string | No |  |

### Responses

#### HTTP 200

- Description: OK
- Schema: `response.OceanShineWithdrawalPageResponse`

| field | type | required | description |
| --- | --- | --- | --- |
| Os_Uso | object (response.OceanShineUserInfo) | No | 用户信息-userInfo |
| Os_Uso.Os_Bac | number | No | 当前金额-balance |
| Os_Uso.Os_Baci | integer | No | 当前积分数-balance_int |
| Os_Uso.Os_Con | integer | No | 当前金币数-coin |
| Os_Uso.Os_Crc | string | No | 国家货币-currency |
| Os_Uso.Os_Crcs | string | No | 国家货币符号-currencySymbols |
| Os_Uso.Os_Cty | string | No | 国家-country |
| Os_Uso.Os_Ewl | number | No | 可提现金额-enableWithdrawal |
| Os_Uso.Os_Img | integer | No | 是否有消息未读，1是，0否 |
| Os_Uso.Os_Lev | integer | No | 当前关卡-level |
| Os_Uso.Os_Lgd | integer | No | 用户连续登陆天数-loginDay |
| Os_Uso.Os_Mny | number | No | 用户真实的钱（货币） |
| Os_Uso.Os_Ncm | boolean | No | 新手奖励信息-newComer |
| Os_Uso.Os_Nnm | string | No | 用户昵称-nickName |
| Os_Uso.Os_Rol | number | No | 用户的-卷（货币） |
| Os_Uso.Os_Rti | integer | No | 注册时间-registerTime |
| Os_Uso.Os_Rts | integer | No | 当前关卡-reachTimes |
| Os_Uso.Os_Tra | number | No | 可提现比例-taxRate |
| Os_Wr | array<object (response.OceanShineWithdrawRatio)> | No | 提现规则-WithdrawRatio |
| Os_Wr[].Os_Ady | integer | No | 提现档位-adFrequency |
| Os_Wr[].Os_Ben | integer | No | 开始 begin |
| Os_Wr[].Os_End | integer | No | 结束 end |
| Os_Wr[].Os_Wro | number | No | 提现比例 withdrawRatio |
| Os_Wwf | array<object (response.OceanShineNewWithdrawalMethodGoods)> | No | 提现平台-withdrawPlatform |
| Os_Wwf[].Os_Cn | string | No | 支付平台-图标->icon |
| Os_Wwf[].Os_Et | string | No | 支付平台-描述->text |
| Os_Wwf[].Os_Id | integer | No | ID-id->id |
| Os_Wwf[].Os_Me | string | No | 支付平台-名称->name |
| Os_Wwf[].Os_Mid | integer | No | 配置ID-去提现时用 |
| Os_Wwf[].Os_Mlt | number | No | 是低提现值-minimumLimit |

## 17. 每日任务-现金

- Method: `POST`
- Path: `/ocean/api_v1/task_list`
- Full URL: `http://test.api.pandamerge.top/ocean/api_v1/task_list`
- operationId: `OceanShineaskLookAdMoney`
- Description: 每日任务-现金-广告提现金
- Content-Type: `application/json`

### Headers

| Name | In | Type | Required | Description |
| --- | --- | --- | --- | --- |
| X-Bundle | header | string | Yes | 包名 |

### Other Parameters

None

### Request Body

- Parameter: `request.OceanShineRoutineTaskLookAdMoneyReq`
- Description: 请求体
- Schema: `request.OceanShineRoutineTaskLookAdMoneyReq`

| field | type | required | description |
| --- | --- | --- | --- |
| Os_Apid | string | No | 应用ID |
| Os_Usid | string | No | 用户ID |
| Os_Vn | string | No | vn |

### Responses

#### HTTP 200

- Description: OK
- Schema: `response.OceanShineRoutineTaskLookAdMoneyResponse`

| field | type | required | description |
| --- | --- | --- | --- |
| Os_An | integer | No | 广告次数 |
| Os_Css | string | No | 国家货币符号 |
| Os_Ln | integer | No | 已看次数 |
| Os_Mrt | number | No | 卷转金额的比例 |
| Os_My | number | No | 可提现金额 |
| Os_Sr | integer | No | 排序 |
| Os_Ss | integer | No | 当前状态,1未达到条件（观看广告次数不足），2已达到条件（可以提现），3已提现 |
| Os_Tid | integer | No | 任务ID-领取奖励时使用用-提现时使用 |

## 18. 用户信息

- Method: `POST`
- Path: `/ocean/api_v1/user_info`
- Full URL: `http://test.api.pandamerge.top/ocean/api_v1/user_info`
- operationId: `OceanShineUserInfo`
- Description: 用户信息-定制版参数（
- Content-Type: `application/json`

### Headers

| Name | In | Type | Required | Description |
| --- | --- | --- | --- | --- |
| X-Bundle | header | string | Yes | 包名 |

### Other Parameters

None

### Request Body

- Parameter: `request.OceanShineUserInfoReq`
- Description: 请求体
- Schema: `request.OceanShineUserInfoReq`

| field | type | required | description |
| --- | --- | --- | --- |
| Os_Apid | string | No | 应用ID |
| Os_Usid | string | No | 用户ID |

### Responses

#### HTTP 200

- Description: OK
- Schema: `response.OceanShineUserInfoResponse`

| field | type | required | description |
| --- | --- | --- | --- |
| Os_Acg | object (response.OceanShineAppConfig) | No | 新手奖励信息-appConfig |
| Os_Acg.Os_Com | integer | No | commonMerge |
| Os_Acg.Os_Nba | integer | No | 新手奖励积分数-new_balance |
| Os_Acg.Os_Ncc | integer | No | 新手奖励金币-new_comer_coin |
| Os_Acg.Os_Rnw | number | No | 新手奖励金额-newComerReward |
| Os_Acg.Os_Usd | number | No | 转usd 比例-toUsd |
| Os_Uso | object (response.OceanShineUserInfo) | No | 用户信息-userInfo |
| Os_Uso.Os_Bac | number | No | 当前金额-balance |
| Os_Uso.Os_Baci | integer | No | 当前积分数-balance_int |
| Os_Uso.Os_Con | integer | No | 当前金币数-coin |
| Os_Uso.Os_Crc | string | No | 国家货币-currency |
| Os_Uso.Os_Crcs | string | No | 国家货币符号-currencySymbols |
| Os_Uso.Os_Cty | string | No | 国家-country |
| Os_Uso.Os_Ewl | number | No | 可提现金额-enableWithdrawal |
| Os_Uso.Os_Img | integer | No | 是否有消息未读，1是，0否 |
| Os_Uso.Os_Lev | integer | No | 当前关卡-level |
| Os_Uso.Os_Lgd | integer | No | 用户连续登陆天数-loginDay |
| Os_Uso.Os_Mny | number | No | 用户真实的钱（货币） |
| Os_Uso.Os_Ncm | boolean | No | 新手奖励信息-newComer |
| Os_Uso.Os_Nnm | string | No | 用户昵称-nickName |
| Os_Uso.Os_Rol | number | No | 用户的-卷（货币） |
| Os_Uso.Os_Rti | integer | No | 注册时间-registerTime |
| Os_Uso.Os_Rts | integer | No | 当前关卡-reachTimes |
| Os_Uso.Os_Tra | number | No | 可提现比例-taxRate |

## 19. 昵称添加

- Method: `POST`
- Path: `/ocean/api_v1/user_name`
- Full URL: `http://test.api.pandamerge.top/ocean/api_v1/user_name`
- operationId: `OceanShineUserNickName`
- Description: 添加修改昵称
- Content-Type: `application/json`

### Headers

| Name | In | Type | Required | Description |
| --- | --- | --- | --- | --- |
| X-Bundle | header | string | Yes | 包名 |

### Other Parameters

None

### Request Body

- Parameter: `request.OceanShineUserNickName`
- Description: 请求体
- Schema: `request.OceanShineUserNickName`

| field | type | required | description |
| --- | --- | --- | --- |
| Os_Apid | string | No | 应用ID |
| Os_Nm | string | No | 昵称 |
| Os_Usid | string | No | 用户ID |

### Responses

## 20. 广告上报

- Method: `POST`
- Path: `/ocean/api_v1Log/ad_info`
- Full URL: `http://test.api.pandamerge.top/ocean/api_v1Log/ad_info`
- operationId: `OceanShineAdLogReport`
- Description: 广告上报
- Content-Type: `application/json`

### Headers

| Name | In | Type | Required | Description |
| --- | --- | --- | --- | --- |
| X-Bundle | header | string | Yes | 包名 |

### Other Parameters

None

### Request Body

- Parameter: `request.OceanShineAdLogReportReq`
- Description: 请求体
- Schema: `request.OceanShineAdLogReportReq`

| field | type | required | description |
| --- | --- | --- | --- |
| Os_Cnf | object (request.OceanShineCommonInfo) | No | commonInfo |
| Os_Cnf.Os_Apid | string | No | 应用ID |
| Os_Cnf.Os_Usid | string | No | 用户ID |
| Os_Cnf.Os_Vn | string | No | vn |
| Os_Epm | object (request.OceanShineAdLogExtendParam) | No | extendParam |
| Os_Epm.Os_Abd | string | No | adType |
| Os_Epm.Os_Adgp | string | No | adgroup 设备当前归因广告组的名称 |
| Os_Epm.Os_Btid | string | No | batchId |
| Os_Epm.Os_Cid | string | No | codeId |
| Os_Epm.Os_Cmp | string | No | campaign 设备当前归因 |
| Os_Epm.Os_Ctc | string | No | currencyCode |
| Os_Epm.Os_Ecm | string | No | ecpm |
| Os_Epm.Os_Evt | string | No | event |
| Os_Epm.Os_EvtM | string | No | eventMsg |
| Os_Epm.Os_Mbc | string | No | mediaCodeId |
| Os_Epm.Os_Mbp | string | No | mediaPlatform |
| Os_Epm.Os_Nbt | string | No | network 归因渠道名称 |
| Os_Epm.Os_Ptf | string | No | platform |

### Responses

## 21. 日志上报

- Method: `POST`
- Path: `/ocean/api_v1Log/app_info`
- Full URL: `http://test.api.pandamerge.top/ocean/api_v1Log/app_info`
- operationId: `OceanShineAppEventReport`
- Description: 日志上报
- Content-Type: `application/json`

### Headers

| Name | In | Type | Required | Description |
| --- | --- | --- | --- | --- |
| X-Bundle | header | string | Yes | 包名 |

### Other Parameters

None

### Request Body

- Parameter: `request.OceanShineAppEventReportReq`
- Description: 请求体
- Schema: `request.OceanShineAppEventReportReq`

| field | type | required | description |
| --- | --- | --- | --- |
| Os_Cnf | object (request.OceanShineCommonInfo) | No | commonInfo |
| Os_Cnf.Os_Apid | string | No | 应用ID |
| Os_Cnf.Os_Usid | string | No | 用户ID |
| Os_Cnf.Os_Vn | string | No | vn |
| Os_Epm | object (request.OceanShineAppEventLogExtendParam) | No | extendParam |
| Os_Epm.Os_Bgd | string | No | pageId |
| Os_Epm.Os_Evt | string | No | event |
| Os_Epm.Os_EvtE | string | No | eventExt |

### Responses
