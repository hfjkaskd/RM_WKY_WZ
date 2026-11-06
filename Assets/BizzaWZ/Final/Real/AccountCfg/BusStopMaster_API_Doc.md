# BusStopMaster API Document

- Source page: http://test.api.pandamerge.top/swagger/index.html
- Swagger JSON: http://test.api.pandamerge.top/swagger/doc.json
- Generated at: 2026-04-19 12:05:23
- Group: `BusStopMaster`
- Domain used for full URLs: http://test.api.pandamerge.top
- Swagger host field: 
- Swagger basePath field: 
- Endpoint count: 19

## Common Notes

- Common header: `X-Bundle`, type `string`, required, description `??`
- Most endpoints use `POST` + `application/json`
- Swagger `host` / `basePath` are empty, so the full URLs below are composed with `http://test.api.pandamerge.top`
- Request and response schemas are expanded below. Array item structures are marked with `[]`.

## Index

| No | Method | Path | Summary | operationId |
| --- | --- | --- | --- | --- |
| 1 | POST | /bus_stop/master/add_income | 添加收益 | BusStopMasterBalanceReportEcpm |
| 2 | POST | /bus_stop/master/add_reach | 关卡，完成次数上报 | BusStopMasterUserReachReport |
| 3 | POST | /bus_stop/master/apply_order | 去提现 | BusStopMasterApplyWithdrawalV2 |
| 4 | POST | /bus_stop/master/apply_task | 去提现-任务 | BusStopMasterApplyWithdrawalMoneyRoll |
| 5 | POST | /bus_stop/master/base_data | 应用配置信息-v3 | BusStopMasterAppOtherConfigV2 |
| 6 | POST | /bus_stop/master/from | 用户归因 | BusStopMasterUserAttrs |
| 7 | POST | /bus_stop/master/income_id | 获取收益ID | BusStopMasterGetBalanceReportId |
| 8 | POST | /bus_stop/master/line | 添加反馈 | BusStopMasterFeedback |
| 9 | POST | /bus_stop/master/line_list | 反馈列表 | BusStopMasterFeedbackList |
| 10 | POST | /bus_stop/master/login | 用户登陆 | BusStopMasterUserLogin |
| 11 | POST | /bus_stop/master/order_list | 订单列表 | BusStopMasterWithdrawalRecords |
| 12 | POST | /bus_stop/master/order_user | 提现用户列表 | BusStopMasterWithdrawalUserList |
| 13 | POST | /bus_stop/master/plat_list | 提现平台 | BusStopMasterWithdrawalPage |
| 14 | POST | /bus_stop/master/task_list | 每日任务-现金 | BusStopMasterTaskLookAdMoney |
| 15 | POST | /bus_stop/master/user_data | 用户信息 | BusStopMasterUserInfo |
| 16 | POST | /bus_stop/master/user_name | -昵称添加 | BusStopMasterUserNickName |
| 17 | POST | /bus_stop/master/user_new | 新用户奖励-领取 | BusStopMasterNewComerReport |
| 18 | POST | /bus_stop/masterLog/ad_logs | 广告上报 | BusStopMasterAdLogReport |
| 19 | POST | /bus_stop/masterLog/event_logs | 日志上报 | BusStopMasterAppEventReport |

## 1. 添加收益

- Method: `POST`
- Path: `/bus_stop/master/add_income`
- Full URL: `http://test.api.pandamerge.top/bus_stop/master/add_income`
- operationId: `BusStopMasterBalanceReportEcpm`
- Description: 添加收益
- Content-Type: `application/json`

### Headers

| Name | In | Type | Required | Description |
| --- | --- | --- | --- | --- |
| X-Bundle | header | string | Yes | 包名 |

### Other Parameters

None

### Request Body

- Parameter: `request.BusStopMasterAdRevenueReportReq`
- Description: 请求体
- Schema: `request.BusStopMasterAdRevenueReportReq`

| field | type | required | description |
| --- | --- | --- | --- |
| bsm_apid | string | No | 应用ID |
| bsm_ecm | number | No | 广告的ECPM |
| bsm_spl | string | No | 多少倍，0.5 为0.5倍，1为1倍(正常奖励),或task_任务ID |
| bsm_tcid | string | No | bId 获取收益ID |
| bsm_usid | string | No | 用户ID |
| vn | string | No |  |

### Responses

#### HTTP 200

- Description: OK
- Schema: `response.BusStopMasterCalculateAdRevenueResponse`

| field | type | required | description |
| --- | --- | --- | --- |
| bsm_bac | integer | No | 获取的积分 |
| bsm_con | integer | No | 获取的金币-有可能为0->coin |
| bsm_mul | number | No | 原基础上增加倍数，1为1倍，2为2倍，0.1为10%，0.05为5% |
| bsm_prc | number | No | 获取的金额-当前国家->price |
| bsm_uso | object (response.BusStopMasterUserInfo) | No | 用户信息-userInfo |
| bsm_uso.bsm_bac | number | No | 当前金额-balance |
| bsm_uso.bsm_baci | integer | No | 当前积分数-balance_int |
| bsm_uso.bsm_con | integer | No | 当前金币数-coin |
| bsm_uso.bsm_crc | string | No | 国家货币-currency |
| bsm_uso.bsm_crcs | string | No | 国家货币符号-currencySymbols |
| bsm_uso.bsm_cty | string | No | 国家-country |
| bsm_uso.bsm_ewl | number | No | 可提现金额-enableWithdrawal |
| bsm_uso.bsm_img | integer | No | 是否有消息未读，1是，0否 |
| bsm_uso.bsm_isn | boolean | No | 新手奖励信息-newComer |
| bsm_uso.bsm_lgd | integer | No | 用户连续登陆天数-loginDay |
| bsm_uso.bsm_lvl | integer | No | 当前关卡-level |
| bsm_uso.bsm_my | number | No | 用户真实的钱（货币） |
| bsm_uso.bsm_nnm | string | No | 用户昵称-nickName |
| bsm_uso.bsm_rl | number | No | 用户的-卷（货币） |
| bsm_uso.bsm_rti | integer | No | 注册时间-registerTime |
| bsm_uso.bsm_rts | integer | No | 当前关卡-reachTimes |
| bsm_uso.bsm_trt | number | No | 可提现比例-taxRate |

## 2. 关卡，完成次数上报

- Method: `POST`
- Path: `/bus_stop/master/add_reach`
- Full URL: `http://test.api.pandamerge.top/bus_stop/master/add_reach`
- operationId: `BusStopMasterUserReachReport`
- Description: 关卡，完成次数上报
- Content-Type: `application/json`

### Headers

| Name | In | Type | Required | Description |
| --- | --- | --- | --- | --- |
| X-Bundle | header | string | Yes | 包名 |

### Other Parameters

None

### Request Body

- Parameter: `request.BusStopMasterUserReachReportReq`
- Description: 请求体
- Schema: `request.BusStopMasterUserReachReportReq`

| field | type | required | description |
| --- | --- | --- | --- |
| bsm_apid | string | No | 应用ID |
| bsm_usid | string | No | 用户ID |

### Responses

#### HTTP 200

- Description: OK
- Schema: `array<response.BusStopMasterUserInfoResponse>`

| field | type | required | description |
| --- | --- | --- | --- |
| [].bsm_acg | object (response.BusStopMasterAppConfig) | No | 新手奖励信息-appConfig |
| [].bsm_acg.bsm_com | integer | No | commonMerge |
| [].bsm_acg.bsm_nba | integer | No | 新手奖励积分数-new_balance |
| [].bsm_acg.bsm_ncc | integer | No | 新手奖励金币-new_comer_coin |
| [].bsm_acg.bsm_rnw | number | No | 新手奖励金额-newComerReward |
| [].bsm_acg.bsm_usd | number | No | 转usd 比例-toUsd |
| [].bsm_uso | object (response.BusStopMasterUserInfo) | No | 用户信息-userInfo |
| [].bsm_uso.bsm_bac | number | No | 当前金额-balance |
| [].bsm_uso.bsm_baci | integer | No | 当前积分数-balance_int |
| [].bsm_uso.bsm_con | integer | No | 当前金币数-coin |
| [].bsm_uso.bsm_crc | string | No | 国家货币-currency |
| [].bsm_uso.bsm_crcs | string | No | 国家货币符号-currencySymbols |
| [].bsm_uso.bsm_cty | string | No | 国家-country |
| [].bsm_uso.bsm_ewl | number | No | 可提现金额-enableWithdrawal |
| [].bsm_uso.bsm_img | integer | No | 是否有消息未读，1是，0否 |
| [].bsm_uso.bsm_isn | boolean | No | 新手奖励信息-newComer |
| [].bsm_uso.bsm_lgd | integer | No | 用户连续登陆天数-loginDay |
| [].bsm_uso.bsm_lvl | integer | No | 当前关卡-level |
| [].bsm_uso.bsm_my | number | No | 用户真实的钱（货币） |
| [].bsm_uso.bsm_nnm | string | No | 用户昵称-nickName |
| [].bsm_uso.bsm_rl | number | No | 用户的-卷（货币） |
| [].bsm_uso.bsm_rti | integer | No | 注册时间-registerTime |
| [].bsm_uso.bsm_rts | integer | No | 当前关卡-reachTimes |
| [].bsm_uso.bsm_trt | number | No | 可提现比例-taxRate |

## 3. 去提现

- Method: `POST`
- Path: `/bus_stop/master/apply_order`
- Full URL: `http://test.api.pandamerge.top/bus_stop/master/apply_order`
- operationId: `BusStopMasterApplyWithdrawalV2`
- Description: 去提现
- Content-Type: `application/json`

### Headers

| Name | In | Type | Required | Description |
| --- | --- | --- | --- | --- |
| X-Bundle | header | string | Yes | 包名 |

### Other Parameters

None

### Request Body

- Parameter: `request.BusStopMasterApplyWithdrawalReq`
- Description: 请求体
- Schema: `request.BusStopMasterApplyWithdrawalReq`

| field | type | required | description |
| --- | --- | --- | --- |
| bsm_apid | string | No | 应用ID |
| bsm_cpf | string | No | CPF |
| bsm_gdid | integer | No | 商品ID |
| bsm_mtid | integer | No | 商品中的-支付配置ID |
| bsm_pbt | string | No | Pix账号绑定类型,P E C B |
| bsm_rae | string | No | ReceiverName |
| bsm_rat | string | No | ReceiverAccount |
| bsm_rel | string | No | ReceiverEmail |
| bsm_rme | string | No | ReceiverMobile |
| bsm_seid | string | No | 客户端UUID |
| bsm_trt | string | No | 选择提现的比例 |
| bsm_usid | string | No | 用户ID |
| vn | string | No | 版本 |

### Responses

#### HTTP 200

- Description: OK
- Schema: `response.BusStopMasterApplyWithdrawalResponse`

| field | type | required | description |
| --- | --- | --- | --- |
| bsm_orn | string | No | 订单号 - orderNo |

## 4. 去提现-任务

- Method: `POST`
- Path: `/bus_stop/master/apply_task`
- Full URL: `http://test.api.pandamerge.top/bus_stop/master/apply_task`
- operationId: `BusStopMasterApplyWithdrawalMoneyRoll`
- Description: 去提现-任务
- Content-Type: `application/json`

### Headers

| Name | In | Type | Required | Description |
| --- | --- | --- | --- | --- |
| X-Bundle | header | string | Yes | 包名 |

### Other Parameters

None

### Request Body

- Parameter: `request.BusStopMasterApplyWithdrawalMoneyRollReq`
- Description: 请求体
- Schema: `request.BusStopMasterApplyWithdrawalMoneyRollReq`

| field | type | required | description |
| --- | --- | --- | --- |
| At | string | No | 现金或卷提现 applyType   money |
| Mn | string | No | money现金提现节点 (当前关卡数) 或 task_任务ID(task_10000) |
| bsm_apid | string | No | 应用ID |
| bsm_cpf | string | No | CPF |
| bsm_gdid | integer | No | 商品ID |
| bsm_mtid | integer | No | 商品中的-支付配置ID |
| bsm_pbt | string | No | Pix账号绑定类型,P E C B |
| bsm_rae | string | No | ReceiverName |
| bsm_rat | string | No | ReceiverAccount |
| bsm_rel | string | No | ReceiverEmail |
| bsm_rme | string | No | ReceiverMobile |
| bsm_seid | string | No | 客户端UUID |
| bsm_usid | string | No | 用户ID |
| vn | string | No | vn |

### Responses

#### HTTP 200

- Description: OK
- Schema: `response.BusStopMasterApplyWithdrawalResponse`

| field | type | required | description |
| --- | --- | --- | --- |
| bsm_orn | string | No | 订单号 - orderNo |

## 5. 应用配置信息-v3

- Method: `POST`
- Path: `/bus_stop/master/base_data`
- Full URL: `http://test.api.pandamerge.top/bus_stop/master/base_data`
- operationId: `BusStopMasterAppOtherConfigV2`
- Description: 应用配置信息-v3
- Content-Type: `application/json`

### Headers

| Name | In | Type | Required | Description |
| --- | --- | --- | --- | --- |
| X-Bundle | header | string | Yes | 包名 |

### Other Parameters

None

### Request Body

- Parameter: `request.BusStopMasterAppOtherConfigReq`
- Description: 请求体
- Schema: `request.BusStopMasterAppOtherConfigReq`

| field | type | required | description |
| --- | --- | --- | --- |
| bsm_apid | string | No | 应用ID |
| bsm_cfn | string | No | 配置key |
| vn | string | No | vn |

### Responses

#### HTTP 200

- Description: json原样输出
- Schema: `None`

None

## 6. 用户归因

- Method: `POST`
- Path: `/bus_stop/master/from`
- Full URL: `http://test.api.pandamerge.top/bus_stop/master/from`
- operationId: `BusStopMasterUserAttrs`
- Description: 用户归因
- Content-Type: `application/json`

### Headers

| Name | In | Type | Required | Description |
| --- | --- | --- | --- | --- |
| X-Bundle | header | string | Yes | 包名 |

### Other Parameters

None

### Request Body

- Parameter: `request.BusStopMasterUserAttrsReq`
- Description: 请求体
- Schema: `request.BusStopMasterUserAttrsReq`

| field | type | required | description |
| --- | --- | --- | --- |
| bsm_adgp | string | No | 设备当前归因广告组的名称 |
| bsm_aid | string | No | 设备的唯一 Adjust ID |
| bsm_apid | string | No | 应用ID |
| bsm_cam | string | No | 设备当前归因-campaign |
| bsm_cat | number | No | 安装成本-costAmount |
| bsm_ccy | string | No | 成本相关的货币代码-costCurrency |
| bsm_ckl | string | No | 安装被标记的 点击标签-clickLabel |
| bsm_ctm | integer | No | 客户端收到回调的时间-clientTime |
| bsm_ctv | string | No | 设备当前归因素材的名称-creative |
| bsm_cty | string | No | 推广活动定价模型-costType |
| bsm_dbu | string | No | 投放参数-deepLinkUrl |
| bsm_evt | string | No | 触发归因的事件-event |
| bsm_fbr | string | No | 回调原始值-fbInstallReferrer |
| bsm_gbt | string | No | google点击时间-ggClickTime |
| bsm_nbt | string | No | 归因渠道名称-network |
| bsm_tra | string | No | 归因跟踪码-trackerName |
| bsm_trt | string | No | 归因跟踪名称-trackerToken |
| bsm_usid | string | No | 用户ID |
| vn | string | No |  |

### Responses

## 7. 获取收益ID

- Method: `POST`
- Path: `/bus_stop/master/income_id`
- Full URL: `http://test.api.pandamerge.top/bus_stop/master/income_id`
- operationId: `BusStopMasterGetBalanceReportId`
- Description: 获取收益ID
- Content-Type: `application/json`

### Headers

| Name | In | Type | Required | Description |
| --- | --- | --- | --- | --- |
| X-Bundle | header | string | Yes | 包名 |

### Other Parameters

None

### Request Body

- Parameter: `request.BusStopMasterGetAdRevenueReportIdReq`
- Description: 请求体
- Schema: `request.BusStopMasterGetAdRevenueReportIdReq`

| field | type | required | description |
| --- | --- | --- | --- |
| bsm_apid | string | No | 应用ID |
| bsm_usid | string | No | 用户ID |
| vn | string | No |  |

### Responses

#### HTTP 200

- Description: OK
- Schema: `response.BusStopMasterGetAdRevenueReportIdResponse`

| field | type | required | description |
| --- | --- | --- | --- |
| bsm_pfrm | string | No | 展示那一个广告平台的广告 max、topon、exceed（超出上限）、error（未知错误） |
| bsm_tcid | string | No | batch_id-收益ID |

## 8. 添加反馈

- Method: `POST`
- Path: `/bus_stop/master/line`
- Full URL: `http://test.api.pandamerge.top/bus_stop/master/line`
- operationId: `BusStopMasterFeedback`
- Description: 添加反馈
- Content-Type: `application/json`

### Headers

| Name | In | Type | Required | Description |
| --- | --- | --- | --- | --- |
| X-Bundle | header | string | Yes | 包名 |

### Other Parameters

None

### Request Body

- Parameter: `request.BusStopMasterFeedbackReq`
- Description: 请求体
- Schema: `request.BusStopMasterFeedbackReq`

| field | type | required | description |
| --- | --- | --- | --- |
| bsm_apid | string | No | 应用ID |
| bsm_dtn | string | No | 内容-description |
| bsm_usid | string | No | 用户ID |
| vn | string | No |  |

### Responses

## 9. 反馈列表

- Method: `POST`
- Path: `/bus_stop/master/line_list`
- Full URL: `http://test.api.pandamerge.top/bus_stop/master/line_list`
- operationId: `BusStopMasterFeedbackList`
- Description: 反馈列表
- Content-Type: `application/json`

### Headers

| Name | In | Type | Required | Description |
| --- | --- | --- | --- | --- |
| X-Bundle | header | string | Yes | 包名 |

### Other Parameters

None

### Request Body

- Parameter: `request.BusStopMasterFeedbackListV2Req`
- Description: 请求体
- Schema: `request.BusStopMasterFeedbackListV2Req`

| field | type | required | description |
| --- | --- | --- | --- |
| bsm_apid | string | No | 应用ID |
| bsm_usid | string | No | 用户ID |
| vn | string | No |  |

### Responses

#### HTTP 200

- Description: OK
- Schema: `response.BusStopMasterFeedbackListV2Response`

| field | type | required | description |
| --- | --- | --- | --- |
| bsm_msl | array<object (response.BusStopMasterFeedbackListV2ResponseData)> | No | 聊天信息->msg |
| bsm_msl[].bsm_apid | string | No | 应用ID |
| bsm_msl[].bsm_cda | string | No | 提交时间-created_at |
| bsm_msl[].bsm_cte | integer | No | 提交时间-毫秒-created_time |
| bsm_msl[].bsm_ctt | string | No | 内容-content->text->msg |
| bsm_msl[].bsm_id | integer | No | 数据库ID->id->id |
| bsm_msl[].bsm_tpe | integer | No | 1用户反馈，2后台回答-type |
| bsm_msl[].bsm_uda | string | No | 更新时间-updated_at |
| bsm_msl[].bsm_usid | string | No | 用户ID |
| bsm_msl[].bsm_ute | integer | No | 更新时间-毫秒-updated_time |
| bsm_msl[].vn | string | No | 版本号-vn |
| bsm_uso | object (response.BusStopMasterUserInfo) | No | 用户信息-userInfo |
| bsm_uso.bsm_bac | number | No | 当前金额-balance |
| bsm_uso.bsm_baci | integer | No | 当前积分数-balance_int |
| bsm_uso.bsm_con | integer | No | 当前金币数-coin |
| bsm_uso.bsm_crc | string | No | 国家货币-currency |
| bsm_uso.bsm_crcs | string | No | 国家货币符号-currencySymbols |
| bsm_uso.bsm_cty | string | No | 国家-country |
| bsm_uso.bsm_ewl | number | No | 可提现金额-enableWithdrawal |
| bsm_uso.bsm_img | integer | No | 是否有消息未读，1是，0否 |
| bsm_uso.bsm_isn | boolean | No | 新手奖励信息-newComer |
| bsm_uso.bsm_lgd | integer | No | 用户连续登陆天数-loginDay |
| bsm_uso.bsm_lvl | integer | No | 当前关卡-level |
| bsm_uso.bsm_my | number | No | 用户真实的钱（货币） |
| bsm_uso.bsm_nnm | string | No | 用户昵称-nickName |
| bsm_uso.bsm_rl | number | No | 用户的-卷（货币） |
| bsm_uso.bsm_rti | integer | No | 注册时间-registerTime |
| bsm_uso.bsm_rts | integer | No | 当前关卡-reachTimes |
| bsm_uso.bsm_trt | number | No | 可提现比例-taxRate |

## 10. 用户登陆

- Method: `POST`
- Path: `/bus_stop/master/login`
- Full URL: `http://test.api.pandamerge.top/bus_stop/master/login`
- operationId: `BusStopMasterUserLogin`
- Description: 用户登陆
- Content-Type: `application/json`

### Headers

| Name | In | Type | Required | Description |
| --- | --- | --- | --- | --- |
| X-Bundle | header | string | Yes | 包名 |

### Other Parameters

None

### Request Body

- Parameter: `request.BusStopMasterUserLoginReq`
- Description: 请求体
- Schema: `request.BusStopMasterUserLoginReq`

| field | type | required | description |
| --- | --- | --- | --- |
| bsm_anid | string | No | 安卓ID |
| bsm_apid | string | No | 应用ID |
| bsm_atd | integer | No | 调试模式 |
| bsm_bbd | string | No | 手机品牌 |
| bsm_cal | string | No | 来源 |
| bsm_ctr | integer | No | 当前小时值 |
| bsm_dtd | string | No | 屏幕密度 |
| bsm_dth | integer | No | 屏幕高 |
| bsm_dtw | integer | No | 屏幕宽 |
| bsm_gaid | string | No | GoogleId |
| bsm_lag | string | No | 语言 |
| bsm_mbl | string | No | 设备型号 |
| bsm_nbt | string | No | 网络 |
| bsm_obv | string | No | 系统版本 |
| bsm_rtt | integer | No | 是否root |
| bsm_sbi | integer | No | SIM卡 |
| bsm_seid | string | No | 客户端UUID |
| bsm_try | string | No | 国家码 |
| bsm_ttz | string | No | 时区 |
| bsm_usid | string | No | 用户ID |
| cpu | string | No | CPU架构 |
| ua | string | No | 客户端 |
| vc | integer | No | App版本号 |
| vn | string | No | App版本名 |

### Responses

#### HTTP 200

- Description: OK
- Schema: `response.BusStopMasterLoginResponse`

| field | type | required | description |
| --- | --- | --- | --- |
| bsm_are | object | No | activeRule |
| bsm_are.bsm_acm | number | No | ad_cpm |
| bsm_are.bsm_aiu | number | No | ad_ipu |
| bsm_im | integer | No | 当前模式，0无模式(开发)，1审核模式,2投放模式 |
| bsm_rdt | string | No | 注册日期 |
| bsm_rti | integer | No | 注册时间戳 |
| bsm_usid | string | No | 用户ID |

## 11. 订单列表

- Method: `POST`
- Path: `/bus_stop/master/order_list`
- Full URL: `http://test.api.pandamerge.top/bus_stop/master/order_list`
- operationId: `BusStopMasterWithdrawalRecords`
- Description: 订单列表
- Content-Type: `application/json`

### Headers

| Name | In | Type | Required | Description |
| --- | --- | --- | --- | --- |
| X-Bundle | header | string | Yes | 包名 |

### Other Parameters

None

### Request Body

- Parameter: `request.BusStopMasterUserInfoReq`
- Description: 请求体
- Schema: `request.BusStopMasterUserInfoReq`

| field | type | required | description |
| --- | --- | --- | --- |
| bsm_apid | string | No | 应用ID |
| bsm_usid | string | No | 用户ID |

### Responses

#### HTTP 200

- Description: OK
- Schema: `array<response.BusStopMasterWithdrawalRecord>`

| field | type | required | description |
| --- | --- | --- | --- |
| [].bsm_crc | string | No | 货币码-currency |
| [].bsm_crcs | string | No | 货币符号-currencySymbols |
| [].bsm_dat | string | No | 提现时间->date |
| [].bsm_ded | number | No | 扣除金额或现金提现时转roll金额 |
| [].bsm_id | integer | No | ID-订单 |
| [].bsm_prc | number | No | 金额-amount |
| [].bsm_pxe | string | No | Pix账号绑定类型,P E C B-pixBindType |
| [].bsm_pym | string | No | payName 支付名称 |
| [].bsm_rrm | string | No | 提示信息-remarks |
| [].bsm_rva | string | No | ReceiverAccount |
| [].bsm_rve | string | No | ReceiverEmail |
| [].bsm_rvm | string | No | ReceiverMobile |
| [].bsm_rvn | string | No | ReceiverName |
| [].bsm_rvp | string | No | CPF |
| [].bsm_sts | integer | No | 状态信息，1进行中，2违规被拒，3成功，4失败 |
| [].bsm_try | integer | No | 1 现金提现，2roll提现 |
| [].bsm_tsm | string | No | 提示信息（现金提现失败后） |

## 12. 提现用户列表

- Method: `POST`
- Path: `/bus_stop/master/order_user`
- Full URL: `http://test.api.pandamerge.top/bus_stop/master/order_user`
- operationId: `BusStopMasterWithdrawalUserList`
- Description: 提现用户列表
- Content-Type: `application/json`

### Headers

| Name | In | Type | Required | Description |
| --- | --- | --- | --- | --- |
| X-Bundle | header | string | Yes | 包名 |

### Other Parameters

None

### Request Body

- Parameter: `request.BusStopMasterWithdrawalListReq`
- Description: 请求体
- Schema: `request.BusStopMasterWithdrawalListReq`

| field | type | required | description |
| --- | --- | --- | --- |
| bsm_apid | string | No | 应用ID |
| bsm_usid | string | No | 用户ID |
| vn | string | No |  |

### Responses

#### HTTP 200

- Description: OK
- Schema: `array<response.BusStopMasterWithdrawalUserListResponseData>`

| field | type | required | description |
| --- | --- | --- | --- |
| [].bsm_css | string | No | 国家货币符号 |
| [].bsm_mny | number | No | 提现金额 |
| [].bsm_nnm | string | No | 用户昵称 |

## 13. 提现平台

- Method: `POST`
- Path: `/bus_stop/master/plat_list`
- Full URL: `http://test.api.pandamerge.top/bus_stop/master/plat_list`
- operationId: `BusStopMasterWithdrawalPage`
- Description: 提现平台
- Content-Type: `application/json`

### Headers

| Name | In | Type | Required | Description |
| --- | --- | --- | --- | --- |
| X-Bundle | header | string | Yes | 包名 |

### Other Parameters

None

### Request Body

- Parameter: `request.BusStopMasterWithdrawalPageReq`
- Description: 请求体
- Schema: `request.BusStopMasterWithdrawalPageReq`

| field | type | required | description |
| --- | --- | --- | --- |
| bsm_apid | string | No | 应用ID |
| bsm_usid | string | No | 用户ID |
| vn | string | No |  |

### Responses

#### HTTP 200

- Description: OK
- Schema: `response.BusStopMasterWithdrawalPageResponse`

| field | type | required | description |
| --- | --- | --- | --- |
| bsm_uso | object (response.BusStopMasterUserInfo) | No | 用户信息-userInfo |
| bsm_uso.bsm_bac | number | No | 当前金额-balance |
| bsm_uso.bsm_baci | integer | No | 当前积分数-balance_int |
| bsm_uso.bsm_con | integer | No | 当前金币数-coin |
| bsm_uso.bsm_crc | string | No | 国家货币-currency |
| bsm_uso.bsm_crcs | string | No | 国家货币符号-currencySymbols |
| bsm_uso.bsm_cty | string | No | 国家-country |
| bsm_uso.bsm_ewl | number | No | 可提现金额-enableWithdrawal |
| bsm_uso.bsm_img | integer | No | 是否有消息未读，1是，0否 |
| bsm_uso.bsm_isn | boolean | No | 新手奖励信息-newComer |
| bsm_uso.bsm_lgd | integer | No | 用户连续登陆天数-loginDay |
| bsm_uso.bsm_lvl | integer | No | 当前关卡-level |
| bsm_uso.bsm_my | number | No | 用户真实的钱（货币） |
| bsm_uso.bsm_nnm | string | No | 用户昵称-nickName |
| bsm_uso.bsm_rl | number | No | 用户的-卷（货币） |
| bsm_uso.bsm_rti | integer | No | 注册时间-registerTime |
| bsm_uso.bsm_rts | integer | No | 当前关卡-reachTimes |
| bsm_uso.bsm_trt | number | No | 可提现比例-taxRate |
| bsm_wr | array<object (response.BusStopMasterWithdrawRatio)> | No | 提现规则-WithdrawRatio （无用） |
| bsm_wr[].bsm_ad | integer | No | 提现档位-adFrequency |
| bsm_wr[].bsm_be | integer | No | 开始 begin |
| bsm_wr[].bsm_en | integer | No | 结束 end |
| bsm_wr[].bsm_wr | number | No | 提现比例 withdrawRatio |
| bsm_wwf | array<object (response.BusStopMasterNewWithdrawalMethodGoods)> | No | 提现平台-withdrawPlatform |
| bsm_wwf[].bsm_cn | string | No | 支付平台-图标->icon |
| bsm_wwf[].bsm_gdl | array<object (response.BusStopMasterWithdrawalGoodsResponseData)> | No | 商品列表 |
| bsm_wwf[].bsm_gdl[].bsm_apid | string | No | 应用ID |
| bsm_wwf[].bsm_gdl[].bsm_cn | string | No | 支付平台-图标->icon |
| bsm_wwf[].bsm_gdl[].bsm_crc | string | No | 币种-currency |
| bsm_wwf[].bsm_gdl[].bsm_crcs | string | No | 国家符号-currencySymbols |
| bsm_wwf[].bsm_gdl[].bsm_cty | string | No | 国家码-country_code |
| bsm_wwf[].bsm_gdl[].bsm_dlb | integer | No | 每日提现上限-day_limit_number |
| bsm_wwf[].bsm_gdl[].bsm_gdid | integer | No | 商品ID--去提现时用 |
| bsm_wwf[].bsm_gdl[].bsm_gdn | string | No | 商品名称 - good_name |
| bsm_wwf[].bsm_gdl[].bsm_gds | string | No | 商品类型-goods_type |
| bsm_wwf[].bsm_gdl[].bsm_ise | integer | No | 是否可以提现，0是，1否-is_order |
| bsm_wwf[].bsm_gdl[].bsm_mad | integer | No | 需要看广告次数,0为不限 |
| bsm_wwf[].bsm_gdl[].bsm_mar | string | No | 标识文案-mark |
| bsm_wwf[].bsm_gdl[].bsm_mdid | integer | No | 支付方式ID-method_id |
| bsm_wwf[].bsm_gdl[].bsm_me | string | No | 支付平台-名称->name |
| bsm_wwf[].bsm_gdl[].bsm_mil | number | No | 最低提现额-minimumLimit |
| bsm_wwf[].bsm_gdl[].bsm_mny | number | No | 要打款的币种数-price |
| bsm_wwf[].bsm_gdl[].bsm_mtid | integer | No | 支付配置ID-config_id |
| bsm_wwf[].bsm_gdl[].bsm_nd_ba | integer | No | 所需扣积分数-need_balance |
| bsm_wwf[].bsm_gdl[].bsm_nd_co | integer | No | 所需扣金币数-need_coin |
| bsm_wwf[].bsm_gdl[].bsm_nts | string | No | 提现条件-notes |
| bsm_wwf[].bsm_gdl[].bsm_srt | integer | No | 排序号-sort |
| bsm_wwf[].bsm_gdl[].bsm_sts | integer | No | 是否可提现，0可提现，1每日提现达到上限，2单个用户总提现次数，3所需金币或积分不足 |
| bsm_wwf[].bsm_gdl[].bsm_ulb | integer | No | 单个用户总提现次数-user_limit_number |
| bsm_wwf[].bsm_gdl[].bsm_xt | string | No | 支付平台-描述->text |
| bsm_wwf[].bsm_me | string | No | 支付平台-名称->name |
| bsm_wwf[].bsm_mid | integer | No | 配置ID-去提现时用 |
| bsm_wwf[].bsm_mlt | number | No | 是低提现值-minimumLimit |
| bsm_wwf[].bsm_xt | string | No | 支付平台-描述->text |
| bsm_wwf[].id | integer | No | ID-id->id |

## 14. 每日任务-现金

- Method: `POST`
- Path: `/bus_stop/master/task_list`
- Full URL: `http://test.api.pandamerge.top/bus_stop/master/task_list`
- operationId: `BusStopMasterTaskLookAdMoney`
- Description: 每日任务-现金-广告提现金
- Content-Type: `application/json`

### Headers

| Name | In | Type | Required | Description |
| --- | --- | --- | --- | --- |
| X-Bundle | header | string | Yes | 包名 |

### Other Parameters

None

### Request Body

- Parameter: `request.BusStopMasterRoutineTaskLookAdMoneyReq`
- Description: 请求体
- Schema: `request.BusStopMasterRoutineTaskLookAdMoneyReq`

| field | type | required | description |
| --- | --- | --- | --- |
| bsm_apid | string | No | 应用ID |
| bsm_usid | string | No | 用户ID |
| vn | string | No | vn |

### Responses

#### HTTP 200

- Description: OK
- Schema: `response.BusStopMasterRoutineTaskLookAdMoneyResponse`

| field | type | required | description |
| --- | --- | --- | --- |
| bsm_an | integer | No | 广告次数 |
| bsm_css | string | No | 国家货币符号 |
| bsm_ln | integer | No | 已看次数 |
| bsm_my | number | No | 可提现金额 |
| bsm_my_rt | number | No | 卷转金额的比例 |
| bsm_sr | integer | No | 排序 |
| bsm_ss | integer | No | 当前状态,1未达到条件（观看广告次数不足），2已达到条件（可以提现），3已提现 |
| bsm_tid | integer | No | 任务ID-领取奖励时使用用-提现时使用 |

## 15. 用户信息

- Method: `POST`
- Path: `/bus_stop/master/user_data`
- Full URL: `http://test.api.pandamerge.top/bus_stop/master/user_data`
- operationId: `BusStopMasterUserInfo`
- Description: 用户信息
- Content-Type: `application/json`

### Headers

| Name | In | Type | Required | Description |
| --- | --- | --- | --- | --- |
| X-Bundle | header | string | Yes | 包名 |

### Other Parameters

None

### Request Body

- Parameter: `request.BusStopMasterUserInfoReq`
- Description: 请求体
- Schema: `request.BusStopMasterUserInfoReq`

| field | type | required | description |
| --- | --- | --- | --- |
| bsm_apid | string | No | 应用ID |
| bsm_usid | string | No | 用户ID |

### Responses

#### HTTP 200

- Description: OK
- Schema: `response.BusStopMasterUserInfoResponse`

| field | type | required | description |
| --- | --- | --- | --- |
| bsm_acg | object (response.BusStopMasterAppConfig) | No | 新手奖励信息-appConfig |
| bsm_acg.bsm_com | integer | No | commonMerge |
| bsm_acg.bsm_nba | integer | No | 新手奖励积分数-new_balance |
| bsm_acg.bsm_ncc | integer | No | 新手奖励金币-new_comer_coin |
| bsm_acg.bsm_rnw | number | No | 新手奖励金额-newComerReward |
| bsm_acg.bsm_usd | number | No | 转usd 比例-toUsd |
| bsm_uso | object (response.BusStopMasterUserInfo) | No | 用户信息-userInfo |
| bsm_uso.bsm_bac | number | No | 当前金额-balance |
| bsm_uso.bsm_baci | integer | No | 当前积分数-balance_int |
| bsm_uso.bsm_con | integer | No | 当前金币数-coin |
| bsm_uso.bsm_crc | string | No | 国家货币-currency |
| bsm_uso.bsm_crcs | string | No | 国家货币符号-currencySymbols |
| bsm_uso.bsm_cty | string | No | 国家-country |
| bsm_uso.bsm_ewl | number | No | 可提现金额-enableWithdrawal |
| bsm_uso.bsm_img | integer | No | 是否有消息未读，1是，0否 |
| bsm_uso.bsm_isn | boolean | No | 新手奖励信息-newComer |
| bsm_uso.bsm_lgd | integer | No | 用户连续登陆天数-loginDay |
| bsm_uso.bsm_lvl | integer | No | 当前关卡-level |
| bsm_uso.bsm_my | number | No | 用户真实的钱（货币） |
| bsm_uso.bsm_nnm | string | No | 用户昵称-nickName |
| bsm_uso.bsm_rl | number | No | 用户的-卷（货币） |
| bsm_uso.bsm_rti | integer | No | 注册时间-registerTime |
| bsm_uso.bsm_rts | integer | No | 当前关卡-reachTimes |
| bsm_uso.bsm_trt | number | No | 可提现比例-taxRate |

## 16. -昵称添加

- Method: `POST`
- Path: `/bus_stop/master/user_name`
- Full URL: `http://test.api.pandamerge.top/bus_stop/master/user_name`
- operationId: `BusStopMasterUserNickName`
- Description: 添加修改昵称
- Content-Type: `application/json`

### Headers

| Name | In | Type | Required | Description |
| --- | --- | --- | --- | --- |
| X-Bundle | header | string | Yes | 包名 |

### Other Parameters

None

### Request Body

- Parameter: `request.BusStopMasterUserNickName`
- Description: 请求体
- Schema: `request.BusStopMasterUserNickName`

| field | type | required | description |
| --- | --- | --- | --- |
| bsm_apid | string | No | 应用ID |
| bsm_nnm | string | No | 昵称 |
| bsm_usid | string | No | 用户ID |

### Responses

## 17. 新用户奖励-领取

- Method: `POST`
- Path: `/bus_stop/master/user_new`
- Full URL: `http://test.api.pandamerge.top/bus_stop/master/user_new`
- operationId: `BusStopMasterNewComerReport`
- Description: 新用户奖励-领取
- Content-Type: `application/json`

### Headers

| Name | In | Type | Required | Description |
| --- | --- | --- | --- | --- |
| X-Bundle | header | string | Yes | 包名 |

### Other Parameters

None

### Request Body

- Parameter: `request.BusStopMasterUserReachReportReq`
- Description: 请求体
- Schema: `request.BusStopMasterUserReachReportReq`

| field | type | required | description |
| --- | --- | --- | --- |
| bsm_apid | string | No | 应用ID |
| bsm_usid | string | No | 用户ID |

### Responses

#### HTTP 200

- Description: OK
- Schema: `array<response.BusStopMasterUserInfoResponse>`

| field | type | required | description |
| --- | --- | --- | --- |
| [].bsm_acg | object (response.BusStopMasterAppConfig) | No | 新手奖励信息-appConfig |
| [].bsm_acg.bsm_com | integer | No | commonMerge |
| [].bsm_acg.bsm_nba | integer | No | 新手奖励积分数-new_balance |
| [].bsm_acg.bsm_ncc | integer | No | 新手奖励金币-new_comer_coin |
| [].bsm_acg.bsm_rnw | number | No | 新手奖励金额-newComerReward |
| [].bsm_acg.bsm_usd | number | No | 转usd 比例-toUsd |
| [].bsm_uso | object (response.BusStopMasterUserInfo) | No | 用户信息-userInfo |
| [].bsm_uso.bsm_bac | number | No | 当前金额-balance |
| [].bsm_uso.bsm_baci | integer | No | 当前积分数-balance_int |
| [].bsm_uso.bsm_con | integer | No | 当前金币数-coin |
| [].bsm_uso.bsm_crc | string | No | 国家货币-currency |
| [].bsm_uso.bsm_crcs | string | No | 国家货币符号-currencySymbols |
| [].bsm_uso.bsm_cty | string | No | 国家-country |
| [].bsm_uso.bsm_ewl | number | No | 可提现金额-enableWithdrawal |
| [].bsm_uso.bsm_img | integer | No | 是否有消息未读，1是，0否 |
| [].bsm_uso.bsm_isn | boolean | No | 新手奖励信息-newComer |
| [].bsm_uso.bsm_lgd | integer | No | 用户连续登陆天数-loginDay |
| [].bsm_uso.bsm_lvl | integer | No | 当前关卡-level |
| [].bsm_uso.bsm_my | number | No | 用户真实的钱（货币） |
| [].bsm_uso.bsm_nnm | string | No | 用户昵称-nickName |
| [].bsm_uso.bsm_rl | number | No | 用户的-卷（货币） |
| [].bsm_uso.bsm_rti | integer | No | 注册时间-registerTime |
| [].bsm_uso.bsm_rts | integer | No | 当前关卡-reachTimes |
| [].bsm_uso.bsm_trt | number | No | 可提现比例-taxRate |

## 18. 广告上报

- Method: `POST`
- Path: `/bus_stop/masterLog/ad_logs`
- Full URL: `http://test.api.pandamerge.top/bus_stop/masterLog/ad_logs`
- operationId: `BusStopMasterAdLogReport`
- Description: 广告上报
- Content-Type: `application/json`

### Headers

| Name | In | Type | Required | Description |
| --- | --- | --- | --- | --- |
| X-Bundle | header | string | Yes | 包名 |

### Other Parameters

None

### Request Body

- Parameter: `request.BusStopMasterAdLogReportReq`
- Description: 请求体
- Schema: `request.BusStopMasterAdLogReportReq`

| field | type | required | description |
| --- | --- | --- | --- |
| bsm_cif | object (request.BusStopMasterCommonInfo) | No | commonInfo |
| bsm_cif.bsm_apid | string | No | 应用ID |
| bsm_cif.bsm_usid | string | No | 用户ID |
| bsm_cif.vn | string | No | vn |
| bsm_epm | object (request.BusStopMasterAdLogExtendParam) | No | extendParam |
| bsm_epm.bsm_abd | string | No | adType |
| bsm_epm.bsm_adgp | string | No | adgroup 设备当前归因广告组的名称 |
| bsm_epm.bsm_cam | string | No | campaign 设备当前归因 |
| bsm_epm.bsm_cid | string | No | codeId |
| bsm_epm.bsm_ctc | string | No | currencyCode |
| bsm_epm.bsm_ecm | string | No | ecpm |
| bsm_epm.bsm_evm | string | No | eventMsg |
| bsm_epm.bsm_evt | string | No | event |
| bsm_epm.bsm_mbc | string | No | mediaCodeId |
| bsm_epm.bsm_mbp | string | No | mediaPlatform |
| bsm_epm.bsm_nbt | string | No | network 归因渠道名称 |
| bsm_epm.bsm_ptf | string | No | platform |
| bsm_epm.bsm_tcid | string | No | batchId |

### Responses

## 19. 日志上报

- Method: `POST`
- Path: `/bus_stop/masterLog/event_logs`
- Full URL: `http://test.api.pandamerge.top/bus_stop/masterLog/event_logs`
- operationId: `BusStopMasterAppEventReport`
- Description: 日志上报
- Content-Type: `application/json`

### Headers

| Name | In | Type | Required | Description |
| --- | --- | --- | --- | --- |
| X-Bundle | header | string | Yes | 包名 |

### Other Parameters

None

### Request Body

- Parameter: `request.BusStopMasterAppEventReportReq`
- Description: 请求体
- Schema: `request.BusStopMasterAppEventReportReq`

| field | type | required | description |
| --- | --- | --- | --- |
| bsm_cif | object (request.BusStopMasterCommonInfo) | No | commonInfo |
| bsm_cif.bsm_apid | string | No | 应用ID |
| bsm_cif.bsm_usid | string | No | 用户ID |
| bsm_cif.vn | string | No | vn |
| bsm_epm | object (request.BusStopMasterAppEventLogExtendParam) | No | extendParam |
| bsm_epm.bsm_bgd | string | No | pageId |
| bsm_epm.bsm_eve | string | No | eventExt |
| bsm_epm.bsm_evt | string | No | event |

### Responses
