# 邻医云 B2C 对接程序 — 开发计划与 AI 提示词

> 基于《邻医云B2C对接程序-方案设计.md》拆解的最小功能模块开发计划
> 技术栈：C# .NET 8 + WPF(MVVM) + Kestrel + SQLite + Serilog + Polly
> 文档版本：v1.0 ｜ 日期：2026-07-22

---

## 一、模块总览

共 **8 个阶段、26 个最小模块**。每个模块独立可交付、可验证，配套一个可直接复制给 AI 编码助手的提示词。

### 模块依赖关系

```
阶段0 项目骨架
  └─ M0.1 解决方案骨架
       ├─阶段1 基础对接能力
       │    ├─ M1.1 签名服务 ─┐
       │    ├─ M1.2 HTTP客户端 ┤(依赖M1.1)
       │    └─ M1.3 配置与加密存储 ┘
       ├─阶段2 推送接收服务 (依赖M1.1,M1.3)
       │    ├─ M2.1 Kestrel接收骨架
       │    └─ M2.2 推送消息处理器
       ├─阶段3 持久化队列 (依赖M1.3)
       │    ├─ M3.1 SQLite消息队列
       │    └─ M3.2 消费与重试调度
       ├─阶段4 主动同步 (依赖M1.2,M3.1,M4.3)
       │    ├─ M4.1 商品同步接口封装
       │    ├─ M4.2 订单同步接口封装
       │    ├─ M4.3 ERP适配器
       │    └─ M4.4 同步调度引擎
       ├─阶段5 桌面UI (依赖M1.3,M3.1)
       │    ├─ M5.1 主窗口骨架
       │    ├─ M5.2 运行看板页
       │    ├─ M5.3 订单流水页
       │    ├─ M5.4 同步管理页
       │    ├─ M5.5 日志中心页
       │    ├─ M5.6 告警中心页
       │    ├─ M5.7 系统配置页
       │    └─ M5.8 系统托盘
       ├─阶段6 进程保护 (依赖M0.1)
       │    ├─ M6.1 看门狗守护进程
       │    ├─ M6.2 异常捕获与心跳
       │    ├─ M6.3 防退出机制
       │    └─ M6.4 开机自启
       ├─阶段7 安全与告警 (依赖M1.3,M3.1)
       │    ├─ M7.1 告警通知服务
       │    └─ M7.2 权限与审计
       └─阶段8 测试与打包
            ├─ M8.1 沙箱联调测试
            └─ M8.2 安装包打包
```

### 模块清单

| 阶段 | 模块 | 名称 | 依赖 | 预估产出文件 |
|------|------|------|------|------------|
| 0 | M0.1 | 解决方案骨架 | — | .sln / 3 项目 |
| 1 | M1.1 | 签名服务 | M0.1 | SignService.cs |
| 1 | M1.2 | HTTP 客户端 | M0.1,M1.1 | LykyApiClient.cs |
| 1 | M1.3 | 配置与加密存储 | M0.1 | AppConfig.cs / ConfigStore.cs |
| 2 | M2.1 | Kestrel 推收骨架 | M1.1,M1.3 | PushReceiver.cs |
| 2 | M2.2 | 推送消息处理器 | M2.1,M3.1 | PushHandlers/*.cs |
| 3 | M3.1 | SQLite 消息队列 | M1.3 | MessageQueue.cs / Models |
| 3 | M3.2 | 消费与重试调度 | M3.1 | QueueConsumer.cs |
| 4 | M4.1 | 商品同步接口 | M1.2 | ProductSyncService.cs |
| 4 | M4.2 | 订单同步接口 | M1.2 | OrderSyncService.cs |
| 4 | M4.3 | ERP 适配器 | M1.3 | ErpAdapter.cs |
| 4 | M4.4 | 同步调度引擎 | M4.1,M4.2,M4.3 | SyncEngine.cs |
| 5 | M5.1 | 主窗口骨架 | M0.1 | MainWindow / ViewModels |
| 5 | M5.2 | 运行看板页 | M5.1 | DashboardView |
| 5 | M5.3 | 订单流水页 | M5.1 | OrderListView |
| 5 | M5.4 | 同步管理页 | M5.1 | SyncView |
| 5 | M5.5 | 日志中心页 | M5.1 | LogView |
| 5 | M5.6 | 告警中心页 | M5.1 | AlertView |
| 5 | M5.7 | 系统配置页 | M5.1 | ConfigView |
| 5 | M5.8 | 系统托盘 | M5.1 | TrayService.cs |
| 6 | M6.1 | 看门狗守护进程 | M0.1 | Watchdog/Program.cs |
| 6 | M6.2 | 异常捕获与心跳 | M0.1 | ExceptionGuard.cs |
| 6 | M6.3 | 防退出机制 | M5.1 | ExitGuard.cs |
| 6 | M6.4 | 开机自启 | M0.1 | AutoStartService.cs |
| 7 | M7.1 | 告警通知服务 | M1.3 | AlertService.cs |
| 7 | M7.2 | 权限与审计 | M1.3 | AuthService / AuditLog |
| 8 | M8.1 | 沙箱联调测试 | 全部 | Tests |
| 8 | M8.2 | 安装包打包 | 全部 | InnoSetup.iss |

---

## 二、通用上下文（所有提示词共享的背景）

> 每个提示词默认包含以下背景，下文各模块提示词中以 `[通用上下文]` 标注引用处，使用时请将本节内容拼接到提示词开头。

```
【项目背景】
你正在开发"邻医云 B2C 对接程序"——一个运行在 Windows 药店电脑上的本地常驻对接网关。
它双向连接"邻医云 B2C 开放平台"(store-api.lyky.cn) 与药店 ERP：
- 作为服务端：内置 Kestrel HTTP 服务，接收开放平台推送的订单消息，验签后转发 ERP
- 作为客户端：调用开放平台接口，把 ERP 的库存/价格/货位/发货同步到平台

【技术栈】
- C# .NET 8（LTS），WPF + MVVM（CommunityToolkit.Mvvm）
- ASP.NET Core Kestrel（宿主于桌面进程接收推送）
- SQLite（持久化消息队列与配置）
- Serilog（日志）、Polly（HTTP 重试/熔断）
- System.Net.Http.HttpClient（调用平台接口）

【项目结构】（M0.1 建立的骨架）
LykyConnector.sln
├─ src/LykyConnector.Core/        # 核心业务（签名、客户端、队列、同步、适配器）
├─ src/LykyConnector.App/         # WPF 桌面程序（主程序 + UI + 托盘）
└─ src/LykyConnector.Watchdog/    # 看门狗守护进程（独立可执行）

【开放平台接口规范】
- 网关地址：https://store-api.lyky.cn/
- 调用方式：POST，Content-Type: application/x-www-form-urlencoded
- 编码：UTF-8
- 系统级参数：app_id、timestamp(10位秒)、signature、version(固定"v1")
- 签名算法：
  1) 收集 app_id、timestamp、version、app_secret（不含 signature）
  2) 按参数名字母升序排序
  3) 将各参数值顺序拼接成一个字符串
  4) 对该字符串计算 MD5（32位小写 hex 字符串），再对该 hex 字符串做 Base64 编码，得到 signature
  （等价 PHP：base64_encode(md5(implode(ksort($params)))）
  示例：app_id=dcc8ce40b7f76e21fcbeefe63497f690f, app_secret=sdasklhdah2342jk4234h23kjsdas,
        timestamp=1634124321, version=v1 → signature=ZjczYTA2ODcxMTJkYTEyOGMwOTM3Y2Y3NDJiZWU0ZWI=
- 通用响应：{"name":"success","message":"...","code":0,"status":200,"data":[]}
  code=0 成功，code=10001 等为失败
- 推送类接口：平台 POST 到 ERP 配置的回调地址，body 含系统级参数 + body(应用级)，
  ERP 验签后返回 {"data":"ok"} 表示接收成功
- 接口文档：https://b2c-open-doc.lyky.cn/#/zh-cn/apidoc/

【代码规范】
- 命名：PascalCase（类/方法/属性）、_camelCase（私有字段）
- 异步方法以 Async 结尾，返回 Task/Task<T>
- 所有 HTTP 调用走 Polly 策略（重试3次指数退避 + 10s超时）
- 日志用 Serilog，结构化字段
- 配置中的 app_secret 必须加密存储（DPAPI），不得明文落盘
```

---

## 三、各模块详情与 AI 提示词

---

### 阶段 0：项目骨架

#### M0.1 解决方案骨架与项目结构

**目标**：建立 .sln 与三个项目（Core / App / Watchdog），配置依赖包、DI 容器、Serilog 基础。

**产出**：`LykyConnector.sln`、三个 .csproj、Program.cs 入口。

**验收**：三个项目可编译；App 启动显示一个空白 WPF 窗口并输出 Serilog 日志。

**AI 提示词**：

```
[通用上下文]

【本模块任务】
创建解决方案骨架。要求：

1. 在 D:\LykyConnector 下创建 .NET 8 解决方案 LykyConnector.sln，含三个项目：
   - src/LykyConnector.Core（类库）：核心业务逻辑，不依赖 UI
   - src/LykyConnector.App（WPF 应用）：主程序，引用 Core；含 App.xaml / MainWindow.xaml
   - src/LykyConnector.Watchdog（控制台应用）：看门狗守护进程，引用 Core

2. NuGet 依赖：
   - Core：Serilog.Sinks.File、Polly、Microsoft.Data.Sqlite、Microsoft.Extensions.Http
   - App：CommunityToolkit.Mvvm、Hardcodet.NotifyIcon.Wpf（托盘）、Serilog.Sinks.File
   - 所有项目：Microsoft.Extensions.Hosting、Microsoft.Extensions.DependencyInjection

3. Core 项目建立目录结构：
   - Sign/（签名）、Client/（HTTP客户端）、Config/（配置）、Queue/（消息队列）
   - Push/（推送接收）、Sync/（主动同步）、Erp/（ERP适配）、Alert/（告警）、Common/

4. App 的 App.xaml.cs：
   - 用 Microsoft.Extensions.Hosting 的 Host 搭建 DI 容器
   - 配置 Serilog 输出到 logs/app-.log（按天滚动）
   - 启动时注册日志："应用启动"

5. MainWindow：标题"邻医云对接服务"，800x600，居中显示，内容区先放一个 TextBlock"启动中..."

6. Watchdog 的 Program.cs：Main 输出"看门狗启动"日志，先留空骨架。

【验收标准】
- dotnet build 三个项目全部成功
- 运行 App 能弹出标题窗口，logs 目录生成日志文件
- 输出完整的项目目录树
```

---

### 阶段 1：基础对接能力

#### M1.1 签名服务 SignService

**目标**：封装平台签名计算与推送验签。

**产出**：`Core/Sign/SignService.cs`

**验收**：用文档示例参数能算出 `ZjczYTA2ODcxMTJkYTEyOGMwOTM3Y2Y3NDJiZWU0ZWI=`。

**AI 提示词**：

```
[通用上下文]

【本模块任务】
在 Core/Sign/SignService.cs 实现签名服务。要求：

1. ISignService 接口 + SignService 实现：
   - string BuildSignature(string appId, string appSecret, long timestamp, string version = "v1")
   - bool VerifySignature(...)  // 用相同算法比对传入 signature

2. 签名算法（严格按平台规则）：
   - 参数字典：{ "app_id": appId, "app_secret": appSecret, "timestamp": timestamp.ToString(), "version": version }
   - 按参数名升序排序（OrdinalIgnoreCase）
   - 取各参数的"值"顺序拼接成字符串 plain
   - 计算 plain 的 MD5，得到 32 位小写 hex 字符串 md5Hex
     （注意：是 hex 字符串，不是 byte[]）
   - 对 md5Hex 字符串的 UTF-8 字节做 Base64 编码，得到 signature
   - 验签：用同样算法重算，与传入 signature 比较（常量时间比较防时序攻击）

3. 写 xUnit 测试 SignServiceTests：
   - 用示例：app_id=dcc8ce40b7f76e21fcbeefe63497f690f, app_secret=sdasklhdah2342jk4234h23kjsdas,
     timestamp=1634124321, version=v1 → 断言 signature == "ZjczYTA2ODcxMTJkYTEyOGMwOTM3Y2Y3NDJiZWU0ZWI="

4. 注意 .NET 实现：
   - MD5: MD5.HashData(Encoding.UTF8.GetBytes(plain)) → byte[16]
   - 转 hex：Convert.ToHexString(md5Bytes).ToLowerInvariant()
   - Base64: Convert.ToBase64String(Encoding.UTF8.GetBytes(md5Hex))

【验收标准】
- 单元测试通过，签名值与文档示例完全一致
- 验签方法对正确签名返回 true，篡改后返回 false
```

---

#### M1.2 开放平台 HTTP 客户端 LykyApiClient

**目标**：封装主动调用平台接口的统一客户端：系统参数注入、签名、POST、响应解析、Polly 重试。

**产出**：`Core/Client/LykyApiClient.cs`、`Core/Client/LykyResponse.cs`

**依赖**：M1.1

**AI 提示词**：

```
[通用上下文]

【前置】已完成 M1.1 签名服务 ISignService。

【本模块任务】
在 Core/Client/ 实现 HTTP 客户端。要求：

1. LykyResponse<T> 模型：
   - string Name、string Message、int Code、int Status、T? Data
   - bool IsSuccess => Code == 0

2. ILykyApiClient 接口 + LykyApiClient 实现：
   - 构造注入 IOptions<LykyOptions>（含 AppId、AppSecret、BaseUrl="https://store-api.lyky.cn/"）、ISignService
   - Task<LykyResponse<T>> PostAsync<T>(string path, Dictionary<string,string?> bizParams, CancellationToken ct = default)

3. PostAsync 实现：
   - timestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds()（10位）
   - signature = signService.BuildSignature(appId, appSecret, timestamp)
   - 表单字段：app_id, timestamp, signature, version="v1" + bizParams 全部加入
   - content-type: application/x-www-form-urlencoded
   - POST 到 BaseUrl + path
   - Polly 策略：重试3次（指数退避 1s/2s/4s），超时15s，仅对网络异常/5xx重试
   - 响应 JSON 反序列化为 LykyResponse<T>
   - 记录 Serilog 日志（path、耗时、code）

4. LykyOptions 模型：AppId、AppSecret、BaseUrl，放 Core/Config/

5. xUnit 测试：用 HttpClient 的 DelegatingHandler 模拟返回 {"name":"success","code":0,"data":[]}
   验证请求表单含 app_id/timestamp/signature/version 四个系统参数

【验收标准】
- 单元测试通过
- 请求表单字段齐全且 content-type 正确
- 网络异常时按策略重试
```

---

#### M1.3 配置模型与加密存储

**目标**：定义全部配置模型，用 SQLite + DPAPI 持久化，app_secret 加密。

**产出**：`Core/Config/AppConfig.cs`、`Core/Config/ConfigStore.cs`

**AI 提示词**：

```
[通用上下文]

【本模块任务】
在 Core/Config/ 实现配置存储。要求：

1. 配置模型 AppConfig（含以下节）：
   - LykyOptions Lyky（AppId、AppSecret、BaseUrl、CallbackPort=8686）
   - ErpOptions Erp（Mode: "Http"|"Db"、HttpUrl、HttpToken、DbConnectionString、字段映射字典 FieldMapping）
   - List<StoreBinding> Stores（StoreId、MerchantId、StoreName、Platform）
   - RunOptions Run（AutoStart=true、ProcessGuard=true、ReconnectIntervalSec=30、SandboxMode=false）
   - AlertOptions Alert（Enabled、WebhookUrl、WebhookType="wechat"|"dingtalk"、SoundEnabled=true、QuietHoursStart、QuietHoursEnd）

2. ConfigStore（Core/Config/ConfigStore.cs）：
   - 用 Microsoft.Data.Sqlite，库文件 appdata/lyky.db，表 config(key TEXT PK, value TEXT)
   - 全部配置序列化为一个 JSON 存到 key="appconfig"
   - Load() / Save(AppConfig) 方法
   - AppSecret 字段在存盘前用 DPAPI（ProtectedData.Protect，CurrentUser 范围）加密为 Base64；
     读取时解密。封装 DpapiProtector 静态类：Protect(string)->string / Unprotect(string)->string
   - 首次无配置时返回带默认值的 AppConfig

3. DI 注册扩展：AddLykyConfig(this IServiceCollection) 注册 ConfigStore 单例

4. xUnit 测试：Save 后 Load，断言字段一致且 db 中 app_secret 为密文（不等于明文）

【验收标准】
- 配置可读写往返，app_secret 落库为密文
- 切换用户后密文不可解密（DPAPI CurrentUser 绑定）
```

---

### 阶段 2：推送接收服务

#### M2.1 Kestrel 推送接收服务骨架

**目标**：在 App 进程内启动 Kestrel，监听回调端口，接收 7 类推送，验签后入队，立即返回 `{"data":"ok"}`。

**产出**：`Core/Push/PushReceiver.cs`、`Core/Push/PushEndpoints.cs`

**依赖**：M1.1、M1.3

**AI 提示词**：

```
[通用上下文]

【前置】已完成 M1.1 签名、M1.3 配置（含 CallbackPort）。

【本模块任务】
在 Core/Push/ 实现推送接收服务。要求：

1. PushMessage 模型：PushType(int 枚举 1-7)、RawBody(string)、ReceivedAt(DateTime)、Signature、Timestamp、AppId

2. IPushReceiver 接口 + PushReceiver 实现：
   - 内部用 ASP.NET Core Kestrel（Microsoft.AspNetCore.Hosting 构造 IHost）
   - StartAsync(CancellationToken)：读取 ConfigStore 的 CallbackPort，监听 http://0.0.0.0:{port}
   - 注册 7 个 POST 路由（最小 API）：
     /push/new-order          → PushType=1
     /push/after-sale-order   → PushType=2
     /push/product-delivery   → PushType=3
     /push/other-product-delivery → PushType=4
     /push/product-refund     → PushType=5
     /push/sale-outbound      → PushType=6
     /push/order-trace-code-update → PushType=7
   - StopAsync(CancellationToken)

3. 每个路由处理流程（统一中间件 HandlePush）：
   - 从 form 读取 app_id、timestamp、signature、version 及剩余业务字段(合并为 body JSON)
   - 用 ISignService.VerifySignature 校验；失败记录日志并返回 401
   - 验签通过：构造 PushMessage，调用注入的 Action<PushMessage> 回调（由上层入队）
   - 立即返回 200 {"data":"ok"}（不等处理完成，保证不超时）

4. 用 WebApplication.CreateBuilder 但不启动控制台，仅用 Kestrel 监听

5. 集成测试：用 HttpClient 向 /push/new-order POST 模拟推送，验证返回 {"data":"ok"} 且验签失败时返回 401

【验收标准】
- 7 个路由均可响应
- 验签通过返回 {"data":"ok"}，验签失败返回 401
- 处理异步化，不阻塞响应
```

---

#### M2.2 推送消息处理器

**目标**：7 类推送消息的解析模型与分发处理器，解析后经消息队列转发 ERP。

**产出**：`Core/Push/Models/*.cs`、`Core/Push/PushDispatcher.cs`

**依赖**：M2.1、M3.1

**AI 提示词**：

```
[通用上下文]

【前置】已完成 M2.1（PushReceiver 调用回调入队）、M3.1（消息队列）。

【本模块任务】
在 Core/Push/ 实现推送消息处理。要求：

1. 解析模型（Core/Push/Models/）：
   - OrderPushModel（推送原始订单）：含文档全部字段
     id, merchant_id, store_id, store_name, platform(1-9枚举), out_trade_no,
     is_recipe, prescription_url, consignee, mobile, province, city, area, address,
     encrypt_consignee, encrypt_mobile, encrypt_address, remark,
     pay_price, goods_price, discount_price, merchant_discount_price, platform_price,
     platform_price_two, estimated_price, refund_price, postage,
     has_invoiced, invoice_title, taxpayer_id, invoice_price,
     refund_status(0-3), status(1-4), create_time, pay_time, created_at, updated_at,
     products[](id, original_order_id, out_trade_no, order_no, platform, product_id, name,
       sku, parent_sku, custom_sku, upc, spec, manufacturer, authorized_no,
       product_price, original_price, num, refund_num, is_gift, combination_id, created_at, updated_at)
   - 其他6类用 JObject 通用解析即可（结构相对简单），定义基类 BasePushModel{ out_trade_no, ... }
   - Platform 枚举：1京东 2天猫 3拼多多 4淘宝闪购 5美团 6百度健康 7京东药急送 8邻医小程序 9抖音

2. PushDispatcher：
   - 注入 IErpAdapter（M4.3，先用接口占位）、IMessageQueue
   - DispatchAsync(PushMessage msg)：
     - 按 PushType 选择解析器，反序列化 RawBody
     - 调用 IErpAdapter.ForwardToErpAsync(type, payload)（实际由队列消费时调用，此处仅入队）
     - 入队一条 SyncMessage：Type="PushForward", Payload=解析后对象, out_trade_no

3. PushReceiver 的回调改为 msg => dispatcher.DispatchAsync(msg)

4. 单元测试：用文档"推送原始订单"示例 JSON 验证 OrderPushModel 解析正确，products 数组非空

【验收标准】
- 7 类消息均可正确路由与解析
- 原始订单示例 JSON 解析后字段值正确
- 处方单(is_recipe=1)可被识别
```

---

### 阶段 3：持久化消息队列

#### M3.1 SQLite 持久化消息队列

**目标**：可靠的本地持久化队列，支持入队/出队/状态流转/重试/死信，断电不丢。

**产出**：`Core/Queue/MessageQueue.cs`、`Core/Queue/Models/QueueMessage.cs`

**依赖**：M1.3

**AI 提示词**：

```
[通用上下文]

【前置】已完成 M1.3 配置（appdata/lyky.db）。

【本模块任务】
在 Core/Queue/ 实现持久化消息队列。要求：

1. QueueMessage 模型：
   - long Id、string Type（"PushForward"|"StockSync"|"PriceSync"|"CargoSync"|"BatchSync"|"Waybill"|"Delivery"）
   - string Payload(JSON)、string OutTradeNo、int TryCount、int MaxRetry=5
   - QueueStatus Status（0待处理 1处理中 2成功 3失败 4死信）
   - DateTime CreatedAt、DateTime? NextRetryAt、string? LastError、DateTime? DoneAt

2. IMessageQueue 接口 + MessageQueue 实现（用同一个 lyky.db）：
   - 建表 queue(id INTEGER PK AUTOINCREMENT, type, payload, out_trade_no, try_count, max_retry, status, created_at, next_retry_at, last_error, done_at)
   - 索引：status, next_retry_at
   - EnqueueAsync(QueueMessage)
   - DequeueAsync()：取 status=0 且 (next_retry_at is null or <= now)，CAS 改为 status=1（用事务防并发）
   - MarkSuccessAsync(id)、MarkFailedAsync(id, error)：
     - try_count++；若 try_count < max_retry 则 status=0 且 next_retry_at = now + 指数退避(10s*2^try_count)
     - 否则 status=4(死信)
   - GetPendingCountAsync()、GetDeadLettersAsync()
   - RequeueDeadLetterAsync(id)（人工重试死信）

3. 用 Dapper 或原生 SqliteCommand 均可，注意参数化防注入

4. xUnit 测试：入队10条→出队处理5成功5失败→失败的重试到死信；断电(重启)后未处理的消息仍在

【验收标准】
- 消息不丢失，状态流转正确
- 死信可达，可重投
- 并发出队不重复（CAS）
```

---

#### M3.2 消费与重试调度

**目标**：后台消费循环，从队列取消息调用对应处理器，失败按指数退避重试。

**产出**：`Core/Queue/QueueConsumer.cs`

**依赖**：M3.1

**AI 提示词**：

```
[通用上下文]

【前置】已完成 M3.1 消息队列。

【本模块任务】
在 Core/Queue/ 实现消费调度。要求：

1. IQueueConsumer 接口 + QueueConsumer 实现（IHostedService 后台服务）：
   - 注入 IMessageQueue、IServiceProvider（解析各 Type 对应的处理器）、ILogger
   - StartAsync：启动后台轮询循环（每 2 秒一批，每批最多 20 条）
   - StopAsync：优雅停止，等待当前批次完成

2. 处理器注册约定（IProcessor 接口）：
   - interface IProcessor { string Type {get;} Task<bool> HandleAsync(QueueMessage msg); }
   - 用 IServiceProvider.GetKeyedService<IProcessor>(msg.Type) 解析
   - HandleAsync 返回 true=成功，false=失败(将重试)
   - 未注册 Type 时记录警告并标记失败

3. 消费逻辑：
   - DequeueAsync 取消息
   - 调用对应 IProcessor.HandleAsync
   - 成功 → MarkSuccessAsync；失败 → MarkFailedAsync(error)
   - 单条超时 30s，超时视为失败

4. 启动时恢复：程序重启后 status=1(处理中) 的消息重置为 status=0 重新处理（幂等保护由处理器保证）

5. 单元测试：模拟处理器，验证成功/失败/重试/死信流转

【验收标准】
- 后台稳定消费，重启后自动恢复
- 各类型消息分发到正确处理器
```

---

### 阶段 4：主动同步接口

#### M4.1 商品同步接口封装

**目标**：封装库存、货位、成本价 3 个主动同步接口。

**产出**：`Core/Sync/ProductSyncService.cs`

**依赖**：M1.2

**AI 提示词**：

```
[通用上下文]

【前置】已完成 M1.2 LykyApiClient（PostAsync<T>）。

【本模块任务】
在 Core/Sync/ProductSyncService.cs 封装 3 个商品同步接口。要求：

1. IProductSyncService 接口 + ProductSyncService 实现（注入 ILykyApiClient）：

2. 修改单个商品库存：
   - Task<LykyResponse<List<SyncFailItem>>> UpdateStockAsync(string sku, int stock, CancellationToken ct=default)
   - path: v1/product-sync/update-stock-one
   - bizParams: sku, stock
   - 成功返回 data 为同步失败店铺列表 [{id,name,platform,num}]，全部成功则 data 为空

3. 修改单个商品货位：
   - Task<LykyResponse<object>> UpdateCargoAsync(string sku, string cargoName, string? cargoShelves=null, CancellationToken ct=default)
   - path: v1/product-sync/update-cargo-one
   - bizParams: sku, cargo_name, cargo_shelves(可选)

4. 修改单个商品成本价：
   - Task<LykyResponse<object>> UpdateCostPriceAsync(string sku, decimal costPrice, CancellationToken ct=default)
   - path: v1/product-sync/update-cost-price-one
   - bizParams: sku, cost_price

5. SyncFailItem 模型：id, name, platform, num

6. 每个方法记录 Serilog 日志（sku、接口、结果code、失败店铺数）

7. 实现 IProcessor（Type="StockSync"等）供队列消费调用：
   - StockSyncProcessor：从 Payload 反序列化 {sku,stock}，调用 UpdateStockAsync

8. xUnit 测试：用 mock LykyApiClient 验证请求参数正确、响应解析正确

【验收标准】
- 3 个接口参数与文档一致
- 失败店铺列表正确解析
```

---

#### M4.2 订单同步接口封装

**目标**：封装批号同步、电子面单获取/取消、发货同步 4 个接口。

**产出**：`Core/Sync/OrderSyncService.cs`、`Core/Sync/Models/WaybillResult.cs`

**依赖**：M1.2

**AI 提示词**：

```
[通用上下文]

【前置】已完成 M1.2 LykyApiClient。

【本模块任务】
在 Core/Sync/OrderSyncService.cs 封装 4 个订单同步接口。要求：

1. IOrderSyncService 接口 + OrderSyncService 实现（注入 ILykyApiClient）：

2. 订单商品批号同步：
   - Task<LykyResponse<object>> SyncBatchAsync(string outTradeNo, List<BatchItem> batches, CancellationToken ct=default)
   - path: v1/order-sync/batch
   - BatchItem: { sku, batch_no, num }
   - bizParams: out_trade_no + 批号数据（参考文档，参数参考 https://b2c-open-doc.lyky.cn/#/zh-cn/apidoc/order-sync/batch）

3. 获取电子面单：
   - Task<LykyResponse<WaybillResult>> GetWaybillAsync(List<string> outTradeNos, CancellationToken ct=default)
   - path: v1/order-sync/get-way-bill
   - bizParams: out_trade_no（多个用英文逗号拼接，最多50个，需校验数量）
   - WaybillResult: { success: List<WaybillSuccess>, fail: List<WaybillFail> }
   - WaybillSuccess: { out_trade_no, platform(1京东2菜鸟3拼多多4抖店5天猫6美团), express_no, express_name, content(打印数据JSON字符串) }
   - WaybillFail: { out_trade_no, msg }

4. 取消电子面单：
   - Task<LykyResponse<object>> CancelWaybillAsync(string outTradeNo, CancellationToken ct=default)
   - path: v1/order-sync/cancel-way-bill
   - bizParams: out_trade_no

5. 订单发货同步：
   - Task<LykyResponse<object>> DeliverAsync(string outTradeNo, int type=1, int? expressId=null, string? expressNo=null, CancellationToken ct=default)
   - path: v1/order-sync/delivery
   - bizParams: out_trade_no, type(1三方发货 2商家平台发货), express_id, express_no
   - 提供 ExpressCompany 静态枚举常量类：
     SF=1, EMS=3, ZJS=4, YZKD=5, DBL=6, KYE=12, YTO=18, ZTO=19, STO=24, YD=25, ANE=26, JD=96, JT=97, YZBJ=99, OTHER=999

6. 实现 IProcessor：WaybillProcessor(Type="Waybill")、DeliveryProcessor(Type="Delivery")、BatchSyncProcessor(Type="BatchSync")

7. xUnit 测试：mock 验证面单批量参数逗号拼接、≤50校验抛异常、发货快递id正确

【验收标准】
- 4 个接口参数与文档一致
- 面单 ≤50 校验生效
- 快递公司枚举完整
```

---

#### M4.3 ERP 适配器

**目标**：把推送消息转发到药店 ERP，支持 HTTP 模式与 DB 模式及字段映射。

**产出**：`Core/Erp/IErpAdapter.cs`、`Core/Erp/HttpErpAdapter.cs`、`Core/Erp/DbErpAdapter.cs`

**依赖**：M1.3

**AI 提示词**：

```
[通用上下文]

【前置】已完成 M1.3 配置（含 ErpOptions: Mode/HttpUrl/HttpToken/DbConnectionString/FieldMapping）。

【本模块任务】
在 Core/Erp/ 实现双模式 ERP 适配器。要求：

1. IErpAdapter 接口：
   - Task<bool> ForwardToErpAsync(int pushType, JObject payload, CancellationToken ct=default)
   - Task<StockInfo?> QueryStockAsync(string sku, CancellationToken ct=default)（ERP查库存）
   - Task<decimal?> QueryCostPriceAsync(string sku, CancellationToken ct=default)
   - Task<bool> HealthCheckAsync()

2. PushType 到 ERP 动作映射：
   - 1原始订单→erp.order.create、2售后单→erp.order.aftersale、3发货通知→erp.delivery.notify
   - 4代发通知→erp.delivery.other、5退货→erp.order.refund、6出库→erp.stock.outbound、7追溯码→erp.order.tracecode

3. HttpErpAdapter（Mode=Http）：
   - POST 到 HttpUrl + 动作路径，Header 带 Authorization: Bearer {HttpToken}
   - body 为 payload JSON，按 FieldMapping 做字段名转换（如 out_trade_no→orderNo）
   - ERP 返回 2xx 视为成功
   - 超时 10s，Polly 重试2次

4. DbErpAdapter（Mode=Db）：
   - 用 DbConnectionString 连接 ERP 数据库（默认 SQL Server/MySQL，可用泛型 DbConnection）
   - 原始订单→INSERT 到 erp_orders 表（字段按映射）
   - 库存查询→SELECT stock FROM erp_stock WHERE sku=@sku
   - 用字段映射表转换列名

5. FieldMapping：Dictionary<string,string>，源字段→ERP字段；未映射字段保留原名

6. 按 ErpOptions.Mode 用工厂 ErpAdapterFactory 创建实例并注册到 DI

7. 单元测试：mock HTTP 验证转发与字段映射；mock DbConnection 验证 SQL

【验收标准】
- HTTP/DB 双模式可切换
- 字段映射生效
- HealthCheck 可检测 ERP 是否在线
```

---

#### M4.4 同步调度引擎

**目标**：监听 ERP 库存/价格变化并触发同步，支持定时轮询、ERP 主动推送、手动触发三种模式。

**产出**：`Core/Sync/SyncEngine.cs`

**依赖**：M4.1、M4.2、M4.3、M3.1

**AI 提示词**：

```
[通用上下文]

【前置】已完成 M4.1 商品同步、M4.2 订单同步、M4.3 ERP适配器、M3.1 消息队列。

【本模块任务】
在 Core/Sync/SyncEngine.cs 实现同步调度引擎。要求：

1. ISyncEngine 接口 + SyncEngine 实现（IHostedService）：
   - StartAsync：启动定时轮询任务
   - StopAsync：优雅停止
   - Task EnqueueStockSyncAsync(string sku)（ERP 主动推送库存变化时调用，入队）
   - Task EnqueuePriceSyncAsync(string sku)
   - Task EnqueueCargoSyncAsync(string sku)
   - Task EnqueueDeliveryAsync(string outTradeNo, int expressId, string expressNo)（手动发货）

2. 定时轮询模式（默认每5分钟）：
   - 从 ERP 拉取"最近变更的商品 SKU 列表"（IErpAdapter 提供 GetChangedSkusAsync(DateTime since)）
   - 对每个 SKU 查询当前库存/价格，构造同步消息入队（走队列统一重试）

3. ERP 主动推送模式：
   - 暴露内部 HTTP 端点 /internal/stock-change（仅本机访问），ERP 调用即入队同步
   - 或监听 ERP 数据库触发（可选）

4. 手动触发：由 UI 调用 EnqueueXxxAsync 直接入队

5. 所有同步操作都走消息队列（入队 SyncMessage），由 QueueConsumer 调用对应 Processor 执行真实接口调用，保证可靠重试

6. 统计：记录同步成功/失败计数，供 UI 看板读取（SyncStats 静态或注入）

7. 单元测试：验证三种触发模式都正确入队对应 Type 的消息

【验收标准】
- 三种触发模式均可产生正确的同步消息
- 同步走队列，断网时积压、恢复后补发
```

---

### 阶段 5：桌面 UI

#### M5.1 WPF 主窗口骨架与导航

**目标**：建立 MVVM 主框架：左侧导航、顶栏、状态栏、页面路由、ViewModel 基类。

**产出**：`MainWindow.xaml`、`ViewModels/MainViewModel.cs`、`Views/*`（各页面占位）

**AI 提示词**：

```
[通用上下文]

【本模块任务】
在 LykyConnector.App 搭建 WPF 主界面骨架。要求：

1. MVVM：用 CommunityToolkit.Mvvm
   - ObservableObject 基类、RelayCommand
   - ViewModelLocator 或 DI 注入 ViewModel

2. MainWindow.xaml 布局（Grid 3行2列）：
   - 第1行(48px)顶栏：运行状态圆点 + "邻医云对接服务"标题 + "店铺 N · 今日订单 N" + 右侧"管理"按钮
   - 第2行(*)中间：左列(152px)导航 ListBox + 右列(*)内容区 Frame/ContentControl
   - 第3行(28px)状态栏：网络/ERP/队列/CPU/内存 + "进程保护已启用 · 开机自启"

3. 导航项（ObservableCollection<NavItem>）：运行看板/订单流水/同步管理/日志中心/告警中心/系统配置
   - 点击切换 ContentControl 内容，当前项高亮（浅蓝底）

4. 各页面先建占位 UserControl（Views/）：DashboardView/OrderListView/SyncView/LogView/AlertView/ConfigView，各放标题 TextBlock

5. 状态栏绑定 MainViewModel 的 Status 属性（Network/Erp/QueueCount/Cpu/Memory），用一个后台定时器每5秒刷新（先模拟数据）

6. 主题：支持浅色/深色/跟随系统切换（App.xaml 定义 ResourceDictionary），顶栏运行状态点绿=正常/橙=告警/红=异常

7. 窗口最小尺寸 900x600，默认 1100x720

【验收标准】
- 6 个导航项可切换，当前项高亮
- 状态栏实时刷新
- 浅色/深色主题切换生效
```

---

#### M5.2 运行看板页

**目标**：KPI 卡片、平台订单占比、实时订单流、失败店铺榜。

**产出**：`Views/DashboardView.xaml`、`ViewModels/DashboardViewModel.cs`

**依赖**：M5.1

**AI 提示词**：

```
[通用上下文]

【前置】已完成 M5.1 主窗口骨架。

【本模块任务】
实现运行看板页。要求：

1. DashboardViewModel（每10秒刷新）：
   - KPI：TodayOrderCount(int)、SyncSuccessRate(double)、PendingRecipeCount(int, 处方单)、AlertCount(int)
   - PlatformDistribution：ObservableCollection<PlatformStat>{ Name, Count, Color }
   - RecentOrders：ObservableCollection<OrderFlowItem>{ Time, Platform, Tag(处方/普通/退货), OrderNo, Status }
   - FailedStores：ObservableCollection<FailedStoreItem>{ StoreName, Platform, FailCount }

2. DashboardView.xaml 布局：
   - 顶部4列 KPI 卡片（无边框，浅灰底，圆角8）：今日订单/同步成功率/待处理处方单(橙红数字)/告警(橙数字)
   - 下方2列：左"平台订单占比"（横条形图，每平台一行：名称+进度条+数量）、右"实时订单流"（最近20条滚动列表）
   - 底部"失败店铺榜"列表（店铺名/平台/失败数）

3. 处方单标签用特殊色(橙红底)标记，普通单用浅蓝标签

4. 数据来源：从 Core 的统计服务/队列/日志读取；本模块先用模拟数据，定义 IDashboardDataService 接口（后续 M8.1 接真实数据）

5. 平台颜色：美团#1D9E75 京东#378ADD 淘宝闪购#EF9F27 拼多多#D4537E 抖音#7F77DD

【验收标准】
- 4 个 KPI 卡片显示
- 占比横条与实时流正确渲染
- 处方单视觉区分明显
```

---

#### M5.3 订单流水页

**目标**：订单列表、筛选、详情抽屉、重推。

**产出**：`Views/OrderListView.xaml`、`ViewModels/OrderListViewModel.cs`

**依赖**：M5.1

**AI 提示词**：

```
[通用上下文]

【前置】已完成 M5.1。

【本模块任务】
实现订单流水页。要求：

1. OrderListViewModel：
   - Orders：ObservableCollection<OrderRow>{ Time, Platform, OrderNo, Consignee(脱敏), Amount, Status, IsRecipe(bool) }
   - 筛选：PlatformFilter(全部+9平台)、StatusFilter(全部/待发货/已发货/已完成/已取消)、DateFilter、OnlyRecipe(bool)
   - 详情：SelectedOrder（绑定详情抽屉）
   - 命令：RefreshCommand、RepushCommand(重推ERP)、MarkHandledCommand

2. OrderListView.xaml：
   - 顶部筛选栏：平台下拉、状态下拉、日期选择、"仅处方单"复选、刷新按钮
   - 中间 DataGrid（虚拟化，支持大量行）：时间/平台/订单号/收货人(脱敏如"张*")/金额/状态/处方标记
   - 处方单行整行浅红背景
   - 右侧详情抽屉（选中行展开）：完整订单（商品明细表格、收货地址、批号、追溯码、处方图片链接）
   - 底部操作栏：重推ERP、标记已处理

3. 收货人/手机/地址默认脱敏显示，"查看明文"需权限（调用权限服务，本模块先预留按钮）

4. 数据来源：定义 IOrderQueryService 接口（查 SQLite 推送日志表），先用模拟数据

【验收标准】
- 筛选联动生效
- 虚拟化列表流畅
- 处方单视觉突出
- 详情抽屉信息完整
```

---

#### M5.4 同步管理页

**目标**：库存/价格/货位/面单/发货/批号的手动同步操作面板。

**产出**：`Views/SyncView.xaml`、`ViewModels/SyncViewModel.cs`

**依赖**：M5.1、M4.1、M4.2

**AI 提示词**：

```
[通用上下文]

【前置】已完成 M5.1、M4.1、M4.2。

【本模块任务】
实现同步管理页（含6个子区，用 TabControl 或分区）。要求：

1. 库存同步区：
   - SKU 输入 + 库存数量输入 + "同步"按钮 → 调 ISyncEngine.EnqueueStockSyncAsync
   - 最近同步记录列表（sku/库存/结果/时间）

2. 价格同步区：SKU + 成本价 + 同步按钮 → EnqueuePriceSyncAsync

3. 货位同步区：SKU + 货位名称 + 货架(可选) + 同步 → EnqueueCargoSyncAsync

4. 电子面单区：
   - 多个订单号输入（支持粘贴批量，逗号/换行分隔，校验≤50）
   - "获取面单"按钮 → IOrderSyncService.GetWaybillAsync
   - 结果列表：订单号/快递公司/快递单号/状态；"预览打印"按钮（打开 content 的打印预览窗）
   - "取消面单"按钮

5. 发货同步区：
   - 订单号 + 快递公司下拉(ExpressCompany枚举) + 快递单号 + "发货同步"按钮 → DeliverAsync
   - 待发货订单快捷列表（从推送订单中 status=1 的）

6. 批号同步区：订单号 + 商品批号列表（sku/batch_no/num）+ 同步按钮 → SyncBatchAsync

7. 每个操作显示 loading + 结果提示（成功/失败原因）；失败店铺列表单独弹窗展示

【验收标准】
- 6 个子区均可操作
- 面单批量≤50校验
- 快递公司下拉完整
- 操作有即时反馈
```

---

#### M5.5 日志中心页

**目标**：接口日志、推送日志、系统日志、审计日志四类，可搜索导出。

**产出**：`Views/LogView.xaml`、`ViewModels/LogViewModel.cs`

**依赖**：M5.1

**AI 提示词**：

```
[通用上下文]

【前置】已完成 M5.1。

【本模块任务】
实现日志中心页。要求：

1. LogViewModel：
   - TabItem：接口日志/推送日志/系统日志/审计日志
   - 筛选：日期、关键字、级别(Info/Warn/Error)、接口类型
   - Logs：ObservableCollection<LogItem>{ Time, Level, Type, Content }（虚拟化）
   - 命令：SearchCommand、ExportCommand(导出CSV)、ReplayCommand(推送日志重放)

2. 日志源：
   - 接口日志：Serilog + SQLite sink 记录的 API 调用（path/耗时/code）
   - 推送日志：queue 表中 Type=PushForward 的消息原文
   - 系统日志：Serilog 文件 logs/app-.log 解析
   - 审计日志：audit 表（M7.2）

3. 推送日志行双击 → 弹窗显示完整 RawBody JSON（格式化高亮），并提供"重放"按钮（重新入队处理）

4. 导出：按当前筛选导出 CSV

5. 本模块定义 ILogQueryService 接口，先用模拟数据

【验收标准】
- 四类日志切换
- 关键字搜索生效
- 推送日志可查看原文并重放
- 导出 CSV 成功
```

---

#### M5.6 告警中心页

**目标**：实时告警列表、规则配置、通知渠道配置。

**产出**：`Views/AlertView.xaml`、`ViewModels/AlertViewModel.cs`

**依赖**：M5.1、M7.1

**AI 提示词**：

```
[通用上下文]

【前置】已完成 M5.1、M7.1 告警服务。

【本模块任务】
实现告警中心页。要求：

1. AlertViewModel：
   - Alerts：ObservableCollection<AlertItem>{ Time, Level(警告/严重), Content, Handled(bool) }（虚拟化）
   - 筛选：级别、状态(未处理/已处理)、日期
   - 命令：MarkHandledCommand、ClearHandledCommand、TestNotifyCommand(测试通知)

2. 规则配置区：
   - 连续推送失败次数阈值（默认5）
   - ERP 断连告警开关 + 阈值
   - 库存同步失败店铺数阈值
   - 保存到 AppConfig.AlertOptions

3. 通知渠道配置区：
   - WebhookType 单选（企业微信/钉钉）
   - WebhookUrl 输入 + "测试发送"按钮（调用 IAlertService.SendTestAsync）
   - 声音开关、免打扰时段（开始/结束时间）

4. 告警列表严重级别用红/橙色区分，未处理加粗

5. 数据源：IAlertService 的告警记录表

【验收标准】
- 告警列表实时更新
- 规则与渠道配置可保存
- 测试通知可发送
```

---

#### M5.7 系统配置页

**目标**：应用凭证、回调地址、ERP 对接、店铺绑定、运行策略、沙箱切换、维护模式。

**产出**：`Views/ConfigView.xaml`、`ViewModels/ConfigViewModel.cs`

**依赖**：M5.1、M1.3

**AI 提示词**：

```
[通用上下文]

【前置】已完成 M5.1、M1.3 配置存储。

【本模块任务】
实现系统配置页（分区表单）。要求：

1. 应用凭证区：
   - AppId 输入、AppSecret 输入（密码框，默认掩码，"显示"切换）
   - BaseUrl（默认 https://store-api.lyky.cn/）
   - "测试连通"按钮（调一个轻量接口验证凭证）

2. 回调设置区：
   - CallbackPort 输入（默认8686）
   - 显示"请在开放后台填写回调地址：http://本机公网IP:{port}/push/new-order"（实际取本机IP展示）
   - "测试回调"按钮（本地 POST 测试）

3. ERP 对接区：
   - Mode 单选（HTTP/DB）
   - HTTP 模式：HttpUrl + HttpToken
   - DB 模式：DbConnectionString + "测试连接"按钮
   - 字段映射编辑器（键值对表格：源字段→ERP字段）

4. 店铺绑定区：
   - DataGrid：StoreId/MerchantId/StoreName/Platform，可增删

5. 运行策略区：开机自启(开关)、进程保护(开关)、重连间隔、沙箱模式(开关)

6. 维护模式区（需密码）：
   - "进入维护模式"按钮 → 弹密码框 → 验证后启用维护模式（停止看门狗守护，允许退出）

7. 所有配置修改"保存"按钮统一写回 ConfigStore；敏感字段保存前确认

【验收标准】
- 所有配置项可编辑保存并往返一致
- 凭证/ERP/回调均有测试按钮
- 维护模式需密码
```

---

#### M5.8 系统托盘

**目标**：托盘常驻、右键菜单、气泡通知、关闭拦截。

**产出**：`App/Services/TrayService.cs`

**依赖**：M5.1、M6.3

**AI 提示词**：

```
[通用上下文]

【前置】已完成 M5.1，依赖 Hardcodet.NotifyIcon.Wpf。

【本模块任务】
在 App/Services/ 实现系统托盘。要求：

1. TrayService（单例）：
   - 用 Hardcodet TaskbarIcon
   - 图标根据状态变色：正常(绿)/告警(橙)/异常(红)（可用不同 ico 或覆盖色点）
   - 启动时托盘显示，主窗口关闭时最小化到托盘（不退出）

2. 右键菜单：
   - "显示主界面" → 显示并激活 MainWindow
   - "暂停同步"（需密码）
   - "维护模式"（需密码）
   - "关于"
   - 不提供"退出"项（防误退）

3. 气泡通知：
   - 新订单到达 → 轻提示（可配置开关）
   - 处方单 → 特殊提示
   - 严重告警 → 警告气泡 + 声音
   - ShowBalloon(title, message, level) 方法供 AlertService 调用

4. 与 MainWindow 联动：
   - 关闭按钮拦截（WPF OnClosing）→ e.Cancel=true + 隐藏到托盘 + 气泡"程序仍在后台运行"
   - 双击托盘图标 → 显示主界面

5. 注入 IAlertService 以接收告警弹气泡

【验收标准】
- 关闭主窗口不退出程序，最小化到托盘
- 右键菜单无退出项
- 告警/订单气泡正常
- 双击托盘恢复窗口
```

---

### 阶段 6：进程保护与运行管理

#### M6.1 看门狗守护进程

**目标**：独立进程守护主程序，崩溃自拉起，双进程互守。

**产出**：`Watchdog/Program.cs`、`Core/Common/ProcessGuard.cs`

**AI 提示词**：

```
[通用上下文]

【本模块任务】
实现看门狗守护进程（LykyConnector.Watchdog 独立可执行）。要求：

1. Watchdog/Program.cs：
   - Main：记录启动日志，进入守护循环
   - 每3秒检测主程序进程(LykyConnector.App.exe)是否存活（按进程名或互斥锁）
   - 不存活则启动主程序（Process.Start，工作目录设为当前目录）
   - 启动参数带 --guarded 标记，主程序据此启用反向守护

2. ProcessGuard（Core/Common/，主程序与看门狗共用）：
   - StartWatchdogProcess()：主程序启动时确保看门狗在运行，没运行则拉起
   - IsProcessAlive(string processName)：检测进程
   - 双进程互守：主程序反向监控看门狗，看门狗挂了主程序拉起

3. 进程间心跳（轻量）：
   - 主程序每5秒写文件 appdata/heartbeat（内容=时间戳）
   - 看门狗检测：心跳文件超过15秒未更新 → 判定假死 → 杀进程并重启
   - 主程序退出码：0=正常退出(不重启)，非0=异常(重启)，看门狗据此决定是否拉起

4. 看门狗自身防多重启动（Mutex）

5. 日志：watchdog/logs/watchdog-.log（按天滚动）

【验收标准】
- 杀掉主程序进程，3秒内自动重启
- 杀掉看门狗，主程序拉起看门狗
- 主程序假死(不写心跳)15秒后被重启
- 正常退出码0不重启
```

---

#### M6.2 全局异常捕获与心跳

**目标**：三重异常捕获 + 心跳写入 + 崩溃前通知看门狗。

**产出**：`App/Services/ExceptionGuard.cs`

**AI 提示词**：

```
[通用上下文]

【前置】已完成 M6.1。

【本模块任务】
在 App/Services/ 实现全局异常防护。要求：

1. ExceptionGuard（在 App 启动时注册）：
   - App.DispatcherUnhandledException（UI线程异常）
   - AppDomain.CurrentDomain.UnhandledException（非UI线程异常）
   - TaskScheduler.UnobservedTaskException（未观察Task异常）
   - 三重捕获：记录 Serilog Fatal 日志 → 发告警 → 写崩溃标记文件 → 以非0退出码退出（触发看门狗重启）

2. 心跳服务（IHostedService）：
   - 每5秒写 appdata/heartbeat 文件（时间戳 + 进程状态摘要）
   - 内容含：时间戳、队列待处理数、最近错误时间

3. 崩溃标记：appdata/crash.flag（含崩溃时间+异常摘要），看门狗重启主程序时读取并上报，主程序启动后清除

4. 内存监控（IHostedService，每30秒）：
   - 当前进程 WorkingSet > 800MB → 记录警告 + 主动 GC.Collect
   - 持续3次超阈值 → 优雅重启（先 flush 队列，再非0退出让看门狗拉起）

5. 优雅关闭：收到关闭信号时，等待队列消费完成或10秒超时，再退出

【验收标准】
- 任意线程未捕获异常不导致静默崩溃，均被记录并触发重启
- 心跳文件持续更新
- 内存超限自动回收/重启
```

---

#### M6.3 防退出机制

**目标**：关闭需密码、拦截 Alt+F4、任务栏无关闭项。

**产出**：`App/Services/ExitGuard.cs`

**依赖**：M5.1

**AI 提示词**：

```
[通用上下文]

【前置】已完成 M5.1、M7.2 权限服务（密码验证）。

【本模块任务】
在 App/Services/ 实现防退出机制。要求：

1. ExitGuard：
   - 拦截 MainWindow OnClosing：e.Cancel=true，弹"退出确认"对话框
   - 对话框："退出将停止订单同步，可能导致订单丢失。请输入管理员密码确认退出。"
   - 密码验证通过 → 进入"维护模式确认"二次确认 → 都通过才允许退出（e.Cancel=false，退出码0让看门狗不重启）
   - 密码错误 → 提示并阻止

2. 拦截 Alt+F4：
   - 通过 Window 消息钩子（或 PreviewKeyDown 检测 Alt+Key.F4）拦截
   - 同样走密码确认流程

3. 任务栏：
   - 程序不在任务栏显示关闭按钮（ShowInTaskbar 按需，或通过 WindowStyle 控制）
   - 实际依赖托盘常驻（M5.8），关闭即最小化到托盘

4. 维护模式标志（ConfigStore.Run.MaintenanceMode）：
   - true 时看门狗停止守护（通过心跳文件特殊标记通知看门狗）
   - 仅维护模式 + 密码 才能正式退出

5. 管理员密码：存储于 ConfigStore（DPAPI加密），首次运行设置；密码哈希存储（SHA256+salt）

【验收标准】
- 关闭按钮/Alt+F4 均弹密码框
- 密码错误无法退出
- 正确密码+维护模式才能退出
- 退出后看门狗不重启
```

---

#### M6.4 开机自启

**目标**：注册表 + 任务计划双保险自启，含网络就绪检测。

**产出**：`App/Services/AutoStartService.cs`

**AI 提示词**：

```
[通用上下文]

【本模块任务】
在 App/Services/ 实现开机自启。要求：

1. AutoStartService：
   - EnableAutoStart() / DisableAutoStart()

2. 注册表自启（用户级）：
   - HKCU\Software\Microsoft\Windows\CurrentVersion\Run
   - 值名 "LykyConnector" = 看门狗 exe 完整路径 + --autostart

3. 任务计划自启（系统级，更可靠，需管理员权限，失败则回退注册表）：
   - 用 schtasks /create /tn "LykyConnector" /tr "watchdog路径 --autostart" /sc onlogon /rl highest /f
   - 勾选"即使电池供电也运行"
   - 可配置延迟30秒（等网络就绪）

4. 网络就绪检测（启动时）：
   - 检测是否能访问 store-api.lyky.cn（HTTP HEAD）
   - 未就绪则每10秒重试，期间 UI 显示"等待网络..."
   - 就绪后启动推送接收与同步引擎

5. 启动参数 --autostart：标记本次为自启启动，可跳过欢迎向导、直接最小化到托盘

6. 配置开关：ConfigStore.Run.AutoStart 控制是否启用

【验收标准】
- 开机/登录后看门狗自动启动并拉起主程序
- 网络未就绪时等待不报错
- 可通过配置开关启用/禁用自启
```

---

### 阶段 7：安全与告警

#### M7.1 告警通知服务

**目标**：异常检测 + 多渠道通知（UI弹窗/声音/企业微信钉钉Webhook）。

**产出**：`Core/Alert/AlertService.cs`

**依赖**：M1.3

**AI 提示词**：

```
[通用上下文]

【前置】已完成 M1.3 配置（AlertOptions）。

【本模块任务】
在 Core/Alert/ 实现告警服务。要求：

1. IAlertService 接口 + AlertService 实现：
   - RaiseAsync(AlertLevel level, string content, string? source=null)
   - SendTestAsync()（测试通知）
   - GetActiveAlertsAsync()（未处理告警列表）
   - MarkHandledAsync(long alertId)

2. 告警持久化：SQLite alerts 表(id, level, content, source, created_at, handled, handled_at)

3. 告警触发点（由各模块调用 RaiseAsync）：
   - 推送验签失败（严重）
   - ERP 连续不通超过阈值（严重）
   - 库存同步失败超阈值（警告）
   - 队列死信新增（警告）
   - 程序异常重启（严重）

4. 去重：相同 content+source 5分钟内只告警一次（记录 count）

5. 通知渠道：
   - UI：通过事件通知 TrayService/MainWindow 弹窗+气泡（注入 ITrayNotifier 接口，App 侧实现）
   - 声音：SystemSounds.Exclamation（严重）/ Hand（警告），受 SoundEnabled 与免打扰时段控制
   - Webhook：POST 到 AlertOptions.WebhookUrl
     企业微信：{"msgtype":"text","text":{"content":"[邻医云告警] ..."}}
     钉钉：{"msgtype":"text","text":{"content":"[邻医云告警] ..."}}
   - Webhook 发送失败不阻断主流程，记录日志

6. 免打扰时段：当前时间在 QuietHoursStart~QuietHoursEnd 时，仅严重告警发声音（Webhook照发）

【验收标准】
- 告警可触发、持久化、去重
- 三种渠道均生效
- 免打扰时段控制正确
```

---

#### M7.2 权限与审计

**目标**：角色权限分级 + 操作审计日志。

**产出**：`Core/Common/AuthService.cs`、`Core/Common/AuditLogService.cs`

**依赖**：M1.3

**AI 提示词**：

```
[通用上下文]

【前置】已完成 M1.3 配置。

【本模块任务】
在 Core/Common/ 实现权限与审计。要求：

1. 角色枚举：Admin（管理员，全部权限）、Operator（操作员，仅查看）、Maintainer（维护，IT模式）

2. AuthService：
   - 管理员密码：首次运行引导设置，SHA256(salt+password) 存 ConfigStore
   - VerifyPassword(string input)：校验
   - ChangePassword(old, new)
   - CurrentRole 状态（默认 Operator，验证密码后提升为 Admin，30分钟超时降级）

3. 权限控制点：
   - 退出程序：需 Admin
   - 维护模式：需 Admin
   - 暂停同步：需 Admin
   - 查看收货明文：需 Admin
   - 修改配置：需 Admin
   - 手动同步/查看日志：Operator 可用

4. AuditLogService：
   - SQLite audit 表(id, action, detail, operator_role, created_at)
   - LogAsync(action, detail)：记录审计
   - 记录点：配置变更、退出程序、进入维护模式、暂停同步、查看明文、密码修改、死信重投

5. UI 侧提供 [RequireAdmin] 标记的命令基类，执行前检查 CurrentRole，非 Admin 弹密码框

【验收标准】
- 权限分级生效，敏感操作需密码
- 审计日志完整记录关键操作
- Admin 超时自动降级
```

---

### 阶段 8：测试与打包

#### M8.1 沙箱联调与集成测试

**目标**：端到端联调，模拟推送+同步全链路，验证可靠性。

**产出**：`tests/LykyConnector.IntegrationTests/`

**AI 提示词**：

```
[通用上下文]

【前置】所有功能模块已完成。

【本模块任务】
编写集成测试与沙箱联调脚本。要求：

1. 集成测试项目 tests/LykyConnector.IntegrationTests：
   - 用 TestServer 启动 PushReceiver，模拟邻医云 POST 7类推送
   - 验证：验签→入队→消费→调用 mock ERPAdapter 全链路
   - 验证：库存同步→LykyApiClient(mock) 收到正确参数
   - 验证：断网(断 mock)时消息积压，恢复后补发
   - 验证：死信流转与重投
   - 验证：重启后未处理消息恢复

2. 沙箱联调工具（App 内"沙箱模式"开关）：
   - 切换 SandboxMode=true 时，BaseUrl 指向沙箱
   - 提供"模拟推送"按钮：构造测试订单 JSON POST 到本地回调，验证处理
   - 对照开放平台沙箱环境真实联调推送

3. 性能测试：
   - 连续推送1000条订单，验证队列不丢、消费延迟<2s
   - 库存同步并发100 SKU，验证全部成功

4. 回归测试用例清单文档

【验收标准】
- 全链路测试通过
- 断网恢复无丢消息
- 1000条推送稳定处理
```

---

#### M8.2 安装包打包与部署文档

**目标**：一键安装包 + 部署运维文档。

**产出**：`installer/LykyConnector.iss`、`部署手册.md`

**AI 提示词**：

```
[通用上下文]

【前置】所有模块完成。

【本模块任务】
制作安装包与部署文档。要求：

1. Inno Setup 脚本 installer/LykyConnector.iss：
   - 打包 LykyConnector.App.exe + LykyConnector.Watchdog.exe + 依赖
   - 自包含 .NET 8 运行时（或检测并引导安装）
   - 安装到 C:\Program Files\LykyConnector\
   - 创建开始菜单快捷方式
   - 安装时自动注册任务计划（开机自启）
   - 卸载时清理任务计划+注册表自启项（保留用户数据 appdata）
   - 安装后可选"立即启动"

2. 首次运行配置向导：
   - 引导填写 app_id/app_secret → 测试连通
   - 配置回调端口 → 展示回调地址
   - 配置 ERP 对接 → 测试
   - 设置管理员密码
   - 完成后启动守护

3. 部署手册.md：
   - 环境要求（Win7+/.NET8/网络）
   - 安装步骤
   - 开放后台配置（回调地址、应用授权）
   - ERP 对接配置
   - 常见问题排查（端口占用/防火墙/ERP不通/签名失败）
   - 升级与卸载
   - 日志位置与诊断方法

4. 自动更新（可选）：检查版本接口 + 增量更新

【验收标准】
- 安装包一键安装，卸载干净
- 首次配置向导可用
- 部署手册覆盖常见排障
```

---

## 四、开发顺序建议

按依赖关系，推荐执行顺序（可并行项标注）：

```
第一批：M0.1
第二批：M1.1 + M1.3（并行）
第三批：M1.2（依赖M1.1）
第四批：M3.1（依赖M1.3）
第五批：M2.1（依赖M1.1,M1.3）+ M4.3（依赖M1.3）+ M7.1（依赖M1.3）（并行）
第六批：M3.2（依赖M3.1）+ M4.1（依赖M1.2）+ M4.2（依赖M1.2）（并行）
第七批：M2.2（依赖M2.1,M3.1）+ M4.4（依赖M4.1,M4.2,M4.3,M3.1）（并行）
第八批：M5.1
第九批：M5.2~M5.8（并行，依赖M5.1）
第十批：M6.1 + M6.2 + M6.4（并行）
第十一批：M6.3（依赖M5.1,M7.2）+ M7.2（依赖M1.3）（并行）
第十二批：M8.1
第十三批：M8.2
```

**关键路径**：M0.1 → M1.1 → M1.2 → M4.1/M4.2 → M4.4 → M8.1

**可先出 demo 的里程碑**：完成到第七批（M2.2 + M4.4）即可端到端跑通"收推送+同步"核心链路（无 UI，控制台验证）。

---

*本文档每个模块的【AI 提示词】可独立复制给 AI 编码助手执行；执行前请确保其依赖模块已完成。*
