# LykyConnector — 邻医云 B2C 开放平台对接程序

> 药店桌面常驻对接网关 | C# .NET 8 + WPF + Kestrel + SQLite
> 仓库：https://github.com/Fyyk-Whua/LykyConnector

---

## 一、项目概述

**LykyConnector** 是一个运行在 Windows 药店电脑上的本地桌面对接网关程序，双向连接**邻医云 B2C 开放平台**（邻医快药）与药店 ERP/收银系统，实现医药电商订单的自动化同步。

```
三方平台（美团/京东/淘宝闪购/拼多多/抖音...）
          ↓ 订单汇聚
  邻医云 B2C 开放平台（store-api.lyky.cn）
          ↓ 推送订单         ↑ 同步库存/价格/发货
   【 LykyConnector 对接网关 】
          ↓ 落库/出库        ↑ 库存查询
       药店 ERP / 收银系统
```

### 业务背景

药店在美团、京东、淘宝闪购、拼多多、抖音等多个 O2O/电商平台售药。各平台订单通过邻医云开放平台统一汇聚后，需实时推送到药店 ERP 进行出库、销账；同时 ERP 中的库存、价格、货位变化需实时同步回各平台。人工搬单效率低、易出错，且医药订单（含处方药、追溯码）有严格的 GSP 合规要求。

### 核心特性

- **双向网关**：内置 Kestrel HTTP 服务接收平台推送的 7 类消息 → 验签 → 转发 ERP；同时作为 HTTP 客户端调用平台 7 个主动接口同步数据
- **7×24 稳定运行**：双进程互守看门狗，崩溃自动重启，断电恢复不丢消息
- **防随意退出**：关闭需管理员密码，Alt+F4 拦截，最小化到托盘常驻
- **开机自启**：注册表 + Windows 任务计划双保险，网络就绪后自动连接
- **持久化消息队列**：SQLite 本地队列，断网积压、恢复补发，消息零丢失
- **医药合规**：处方单特殊标记与提醒、追溯码同步、批准文号、收货信息脱敏

---

## 二、技术栈

| 层 | 技术 | 用途 |
|----|------|------|
| 桌面 UI | WPF + MVVM（CommunityToolkit.Mvvm） | 监控看板、配置管理、日志查看 |
| 内嵌 HTTP 服务 | ASP.NET Core Kestrel | 接收开放平台 7 类推送消息 |
| HTTP 客户端 | HttpClient + Polly | 调用开放平台 7 个主动接口 |
| 持久化队列 | SQLite + Microsoft.Data.Sqlite | 消息可靠存储，断电不丢 |
| 日志 | Serilog（文件，按天滚动） | 全量接口日志、系统日志 |
| 进程保护 | 双进程互守 + Windows 服务 | 看门狗守护，崩溃自拉起 |
| 托盘 | Hardcodet.NotifyIcon.Wpf | 系统托盘常驻，右键菜单 |
| 打包 | Inno Setup | 一键安装，含 .NET 运行时 |

**为什么不选 Electron/Python？** Electron 内存占用高（药店老电脑偏多）、进程保护弱；Python 打包与系统守护复杂。C# .NET 在 Windows 桌面、进程管理、系统服务集成方面是原生最优解。

---

## 三、接口规范（对接邻医云开放平台）

### 接入协议

- 网关地址：`https://store-api.lyky.cn/`
- 调用方式：`POST`，`Content-Type: application/x-www-form-urlencoded`
- 编码：`UTF-8`
- 系统级参数：`app_id`、`timestamp`（10 位秒值）、`signature`、`version`（固定 `v1`）

### 签名算法

```
1. 收集 app_id、timestamp、version、app_secret（不含 signature）
2. 按参数名字母升序排序（OrdinalIgnoreCase）
3. 取各参数值顺序拼接为字符串
4. 计算 MD5 → 32 位小写 hex 字符串
5. 对 hex 字符串做 Base64 编码 → signature

验证用示例（PHP 等价：base64_encode(md5(implode(ksort($params)))))：
  app_id     = dcc8ce40b7f76e21fcbeefe63497f690f
  app_secret = sdasklhdah2342jk4234h23kjsdas
  timestamp  = 1634124321
  version    = v1
  → signature = ZjczYTA2ODcxMTJkYTEyOGMwOTM3Y2Y3NDJiZWU0ZWI=
```

### 接口清单（5 类，14 个）

#### A. 主动调用类（ERP → 平台，7 个）

| 接口 | 路径 | 关键参数 |
|------|------|----------|
| 修改单个商品库存 | `v1/product-sync/update-stock-one` | `sku`, `stock`(int) |
| 修改单个商品货位 | `v1/product-sync/update-cargo-one` | `sku`, `cargo_name`, `cargo_shelves`(选) |
| 修改单个商品成本价 | `v1/product-sync/update-cost-price-one` | `sku`, `cost_price`(float) |
| 订单商品批号同步 | `v1/order-sync/batch` | `out_trade_no` + 批号列表 |
| 获取电子面单 | `v1/order-sync/get-way-bill` | `out_trade_no`（批量逗号分隔，≤50） |
| 取消电子面单 | `v1/order-sync/cancel-way-bill` | `out_trade_no` |
| 订单发货同步 | `v1/order-sync/delivery` | `out_trade_no`, `type`, `express_id`, `express_no` |

#### B. 消息推送类（平台 → ERP 回调，7 个）

平台 POST 到 ERP 在开放后台配置的回调地址，ERP 验签后返回 `{"data":"ok"}`：

| 接口 | 路由 |
|------|------|
| 推送原始订单 | `/push/new-order` |
| 推送售后单 | `/push/after-sale-order` |
| 商品发货通知 | `/push/product-delivery` |
| 代发商品发货通知 | `/push/other-product-delivery` |
| 退货通知 | `/push/product-refund` |
| 手动推送出库 | `/push/sale-outbound` |
| 订单追溯码更新 | `/push/order-trace-code-update` |

### 关键业务字段（医药特有）

- `is_recipe` 是否处方单（0/1）
- `prescription_url` 处方图片
- `authorized_no` 批准文号
- `manufacturer` 生产厂家
- 订单追溯码（医药监管）
- `encrypt_consignee/encrypt_mobile/encrypt_address` 收货信息密文（隐私保护）
- 平台枚举：1-京东 / 2-天猫 / 3-拼多多 / 4-淘宝闪购 / 5-美团 / 6-百度健康 / 7-京东药急送 / 8-邻医小程序 / 9-抖音

> 完整接口文档：https://b2c-open-doc.lyky.cn/#/zh-cn/apidoc/

---

## 四、项目结构

```
LykyConnector/
├─ src/
│  ├─ LykyConnector.Core/          # 核心业务（类库）
│  │  ├─ Sign/                     # 签名服务（MD5+Base64）
│  │  ├─ Client/                   # 开放平台 HTTP 客户端
│  │  ├─ Config/                   # 配置模型与 DPAPI 加密存储
│  │  ├─ Push/                     # 推送接收服务与消息处理器
│  │  ├─ Queue/                    # SQLite 持久化消息队列
│  │  ├─ Sync/                     # 主动同步接口封装与调度引擎
│  │  ├─ Erp/                      # ERP 适配器（HTTP/DB 双模）
│  │  ├─ Alert/                    # 告警通知服务
│  │  └─ Common/                   # 权限、审计、工具类
│  ├─ LykyConnector.App/           # WPF 桌面程序（主程序 + UI）
│  │  ├─ Views/                    # 页面（看板/订单/同步/日志/告警/配置）
│  │  ├─ ViewModels/               # MVVM ViewModel
│  │  └─ Services/                 # 托盘、防退出、开机自启、异常防护
│  └─ LykyConnector.Watchdog/      # 看门狗守护进程（独立 exe）
├─ tests/                          # 单元测试与集成测试
├─ installer/                      # Inno Setup 打包脚本
├─ docs/                           # 项目文档
│  ├─ 邻医云B2C对接程序-方案设计.md
│  └─ 邻医云B2C对接程序-开发计划与AI提示词.md
└─ README.md                       # 本文件
```

---

## 五、开发计划

项目按 **8 阶段 26 个最小模块** 开发，13 个执行批次。关键路径：

```
M0.1 项目骨架 → M1.1 签名 → M1.2 HTTP客户端 → M4.1/M4.2 同步接口 → M4.4 调度引擎 → M8.1 联调测试
```

首个里程碑：**第 7 批完成（M2.2 + M4.4）即可端到端跑通 "收推送 + 同步" 核心链路**（控制台验证，无需 UI）。

| 阶段 | 内容 | 模块数 |
|------|------|--------|
| 0 | 项目骨架 | 1 |
| 1 | 基础对接（签名/HTTP客户端/配置） | 3 |
| 2 | 推送接收（Kestrel/消息处理器） | 2 |
| 3 | 持久化队列（SQLite/消费调度） | 2 |
| 4 | 主动同步（接口封装/ERP适配/引擎） | 4 |
| 5 | 桌面 UI（主窗口/7页面/托盘） | 8 |
| 6 | 进程保护（看门狗/异常/防退出/开机） | 4 |
| 7 | 安全告警（通知/权限/审计） | 2 |
| 8 | 测试打包（联调/安装包） | 2 |

> 详细开发计划与每个模块的 AI 执行提示词：见 [`docs/邻医云B2C对接程序-开发计划与AI提示词.md`](docs/邻医云B2C对接程序-开发计划与AI提示词.md)

---

## 六、开发规范

### 代码规范

- **命名**：PascalCase（类/方法/属性）、`_camelCase`（私有字段）、`camelCase`（局部变量）
- **异步**：所有 I/O 操作用 `async/await`，方法以 `Async` 结尾，返回 `Task`/`Task<T>`
- **可空引用类型**：启用 `<Nullable>enable</Nullable>`
- **文件范围命名空间**：`namespace LykyConnector.Xxx;`（C# 10+ 语法）
- **using 排序**：System → NuGet 第三方 → 项目内部

### HTTP 调用规范

- 所有开放平台调用走 `ILykyApiClient.PostAsync<T>()`，统一签名注入
- 所有 HTTP 调用配置 Polly 策略：重试 3 次（指数退避 1s/2s/4s），超时 15s
- 仅对网络异常（`HttpRequestException`）和 5xx 状态码重试，4xx 不重试

### 日志规范

- 使用 Serilog 结构化日志：`_logger.Information("库存同步完成 Sku={Sku} Stock={Stock}", sku, stock)`
- 日志级别：`Information`（正常流程）、`Warning`（可恢复异常）、`Error`（需关注）、`Fatal`（崩溃）
- 日志文件：`logs/app-.log`，按天滚动，保留 30 天

### 配置与安全

- `app_secret` 必须用 DPAPI（`ProtectedData.Protect`，`CurrentUser` 范围）加密存储，不得明文落盘
- 配置通过 `ConfigStore`（SQLite）读写，DI 注入 `IOptions<AppConfig>` 或 `ConfigStore`
- 与开放平台全程 HTTPS，推送回调验签防伪造

### Git 规范

- 分支：`main` 保护，功能分支 `feature/xxx`，修复分支 `fix/xxx`
- Commit：中文，动词开头（"添加"/"修复"/"重构"/"完成"），如 `"完成 M1.1 签名服务"`
- 每完成一个可交付模块，提交并推送

### 测试规范

- Core 层用 xUnit + Moq，覆盖签名、队列、接口封装、适配器等
- 集成测试用 TestServer 模拟推送 → 全链路验证
- UI 层组件用 XAML 数据绑定测试（中期建立）

### 依赖注入

- 使用 `Microsoft.Extensions.DependencyInjection` / `Microsoft.Extensions.Hosting`
- 后台服务（队列消费、调度引擎、心跳、内存监控等）实现 `IHostedService`
- 模块通过接口解耦，在 `Program.cs` / `App.xaml.cs` 中统一注册

---

## 七、关键文档索引

| 文档 | 路径 | 说明 |
|------|------|------|
| 方案设计 | [`docs/邻医云B2C对接程序-方案设计.md`](docs/邻医云B2C对接程序-方案设计.md) | 需求分析、技术选型、系统架构、界面方案、功能列表 |
| 开发计划与 AI 提示词 | [`docs/邻医云B2C对接程序-开发计划与AI提示词.md`](docs/邻医云B2C对接程序-开发计划与AI提示词.md) | 26 模块拆解 + 每个模块的 AI 执行提示词 |
| 开放平台接口文档 | https://b2c-open-doc.lyky.cn/#/zh-cn/apidoc/ | 邻医云官方 API 文档 |
| 开放平台接入指南 | https://b2c-open-doc.lyky.cn/#/zh-cn/guide/ | 开发者入驻与鉴权 |
| 开发者中心 | https://b2c-open.lyky.cn | 创建应用、获取凭证、配置回调 |

---

## 八、快速开始

### 环境要求

- Windows 10/11（最低 Windows 7 SP1）
- .NET 8 SDK（https://dotnet.microsoft.com/download/dotnet/8.0）
- Git

### 克隆与构建

```bash
git clone https://github.com/Fyyk-Whua/LykyConnector.git
cd LykyConnector
dotnet restore
dotnet build
```

### 运行（开发阶段）

```bash
# 启动主程序
dotnet run --project src/LykyConnector.App

# 启动看门狗
dotnet run --project src/LykyConnector.Watchdog
```

### 首次配置

1. 前往 https://b2c-open.lyky.cn 创建应用，获取 `app_id` 和 `app_secret`
2. 在程序"系统配置"页填入凭证，配置回调端口（默认 8686）
3. 在开放后台填写回调地址：`http://<本机公网IP>:8686/push/new-order`
4. 配置 ERP 对接（HTTP 接口地址 或 数据库连接）
5. 开启沙箱模式进行联调测试

---

## 九、当前状态

- [x] 方案设计（需求分析 / 架构 / 界面 / 功能列表）
- [x] 开发计划与 AI 提示词（26 模块拆解）
- [x] GitHub 仓库与项目骨架
- [ ] M0.1 项目骨架（编码中）
- [ ] M1.1 签名服务
- [ ] … 后续模块

---

## 十、许可证

MIT License
