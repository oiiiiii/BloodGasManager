# 血气分析系统（BGSystem）

血气分析系统是一套面向医院检验科的端到端解决方案，覆盖从仪器串口数据采集、ASTM 协议解析、SQLite 持久化，到 REST API 服务、WinForms 桌面客户端审核/查询/打印的完整流程。

- **技术栈**：.NET 10、C#、WinForms、ASP.NET Core Minimal API、SQLite（Microsoft.Data.Sqlite）、xUnit
- **架构**：3 个主项目 + 1 个测试项目 + 1 个联调工具（TestInjector，未加入解决方案）
- **解决方案**：[BGSystem.sln](BGSystem.sln)

---

## 下载与安装

最新发行版：**BGSystem v1.0.0** → [Releases](https://github.com/oiiiiii/BloodGasManager/releases/latest)

| 附件 | 说明 |
|---|---|
| `BGSystem-v1.0.0-win-x64.zip` | Windows x64 自包含包（.NET 运行时已内置，无需额外安装） |

解压后目录结构：

```
BGSystem-v1.0.0-win-x64/
├── BGService/    # 后台托盘服务（BGService.exe + appsettings.json）
└── BGManager/    # 桌面客户端（BGManager.exe，config.json 首次运行后生成）
```

使用步骤：

1. 先启动 `BGService/BGService.exe`：以托盘图标运行，监听串口并提供 REST API（默认 `http://localhost:5000`）
2. 再启动 `BGManager/BGManager.exe`：桌面客户端，通过 API 读取/审核/打印标本
3. 首次使用请在 BGManager「设置」页配置串口参数、API 地址；局域网多机部署请在 `BGService/appsettings.json` 中配置 `Api:Key`

> 仅支持 Windows（WinForms 与串口依赖）。如需从源码构建，见[构建与运行](#构建与运行)。

---

## 目录

- [项目结构](#项目结构)
- [数据流](#数据流)
- [数据库](#数据库)
- [核心功能](#核心功能)
  - [1. 检验单列表自动匹配患者信息](#1-检验单列表自动匹配患者信息)
  - [2. 备注字段与审核快照](#2-备注字段与审核快照)
  - [3. 打印报告与右侧内容](#3-打印报告与右侧内容)
  - [4. 服务端维护：自动备份与数据保留](#4-服务端维护自动备份与数据保留)
  - [5. 患者录入悬浮窗](#5-患者录入悬浮窗)
  - [6. 患者信息智能识别](#6-患者信息智能识别)
  - [7. 血气分析自动解读](#7-血气分析自动解读)
- [REST API](#rest-api)
- [配置](#配置)
- [快捷键](#快捷键)
- [构建与运行](#构建与运行)
- [数据库迁移与老数据兼容](#数据库迁移与老数据兼容)
- [测试与联调](#测试与联调)
- [文档](#文档)

---

## 项目结构

| 项目 | SDK / 输出 | 职责 |
|---|---|---|
| **BGShared** | `Microsoft.NET.Sdk` / 类库 | 共享数据模型（[Specimen.cs](BGShared/Models/Specimen.cs)、[Patient.cs](BGShared/Models/Patient.cs)、[TestResult.cs](BGShared/Models/TestResult.cs)、[PrintTemplate.cs](BGShared/Models/PrintTemplate.cs) 等）、数据库连接工厂（[DbConnectionFactory.cs](BGShared/Database/DbConnectionFactory.cs)）、Schema 迁移（[SchemaMigration.cs](BGShared/Database/SchemaMigration.cs)）、SqlHelpers、打印模板管理器、显示项配置、血气自动解读（[AbgInterpreter.cs](BGShared/Analysis/AbgInterpreter.cs)）、通用工具与常量 |
| **BGService** | `Microsoft.NET.Sdk.Web` / WinExe + WinForms | 后台托盘服务：串口通信（[SerialPortService.cs](BGService/SerialPort/SerialPortService.cs)） → ASTM 协议解析（[ASTMParser.cs](BGService/Parser/ASTMParser.cs)） → [BGProcessor.cs](BGService/BGProcessor.cs) → SQLite 存储（[SpecimenRepository.cs](BGService/Data/SpecimenRepository.cs)） → REST API（Minimal API）；含 Tray（托盘图标/设置/控制台）、Archive（原始报文归档）、Config（SettingsManager）、[MaintenanceService.cs](BGService/Services/MaintenanceService.cs)（自动备份与数据清理）、[BackupService.cs](BGService/Data/BackupService.cs)（手动备份/导入）模块 |
| **BGManager** | `Microsoft.NET.Sdk` / WinExe + WinForms | 桌面客户端：[MainView](BGManager/Views/MainView.cs)（主界面三栏布局：检验单列表 / 患者信息 / 检查结果）、[QueryView](BGManager/Views/QueryView.cs)（查询与批量打印）、[PatientView](BGManager/Views/PatientView.cs) + [PatientEditForm](BGManager/Views/PatientEditForm.cs)（患者管理，支持悬浮录入与智能识别）、[SettingsView](BGManager/Views/SettingsView.cs)（双列卡片式设置页）、[DisplaySettingsForm](BGManager/Views/DisplaySettingsForm.cs)（项目显示配置）、[PrintTemplateSettingsForm](BGManager/Views/PrintTemplateSettingsForm.cs)（打印模板设置）、[AboutView](BGManager/Views/AboutView.cs)（关于 + 使用说明）；Services 层 [ApiClient.cs](BGManager/Services/ApiClient.cs) 负责调用 BGService REST API |
| **BGSystem.Tests** | xUnit | 单元测试：[ASTMParserTests.cs](BGSystem.Tests/ASTMParserTests.cs)（ASTM 报文解析）、[AbgInterpreterTests.cs](BGSystem.Tests/AbgInterpreterTests.cs)（血气解读规则） |
| **TestInjector** | 控制台程序 | 联调工具，向 `/api/admin/inject` 注入模拟 ASTM 报文（未加入 `BGSystem.sln`，用 `dotnet run --project TestInjector` 单独运行） |

### 关键特性

- 单实例互斥锁 + 命名事件唤醒托盘（[Program.cs](BGService/Program.cs)）
- DI 已部分引入（Repositories + BGProcessor 注册为 Singleton）
- [ApiKeyMiddleware.cs](BGService/Api/ApiKeyMiddleware.cs) 提供 API Key 鉴权：固定时间比较（防时序侧信道）、区分大小写；未配置 Key 时放行并在 `logs/service.log` 警告一次
- [SettingsManager.cs](BGService/Config/SettingsManager.cs) 配置持久化（API 端口、串口参数、DB 路径、归档路径），读写 exe 同目录的 `appsettings.json`
- 原始消息归档（[MessageArchiver.cs](BGService/Archive/MessageArchiver.cs) + `Archive/YYYYMM/*.json|*.hl7`）
- SQLite WAL 模式，Schema 版本化迁移；备份统一使用 `VACUUM INTO` 生成一致性快照
- 打印模板 JSON 持久化，支持导入/导出/重置
- 服务端兜底日志：异常不静默，写入 `logs/service.log`（Program.SafeLog）与 `logs/settings.log`

---

## 数据流

```
仪器 → 串口 → SerialPortService → ASTMParser → BGProcessor → SpecimenRepository → SQLite（bloodgas.db）
                                                                    ↓
                                                          REST API (/api/specimens, /patients, ...)
                                                                    ↓
                                                          BGManager WinForms UI

MaintenanceService（每小时检查）：自动备份（VACUUM INTO → archive/backups/）+ 超期数据清理
```

- BGService 监听串口，接收 ASTM E1394 格式报文，解析为标本和检验结果
- 标本存入 SQLite，原始报文同时归档到 `Archive/YYYYMM/` 目录
- BGManager 通过 `ApiClient`（带重试与 API Key）调用 BGService 的 REST API
- 主界面每 5 秒增量刷新当日标本列表；状态栏显示服务端连接、串口状态与今日标本数

---

## 数据库

数据库文件：`bloodgas.db`（SQLite，WAL 模式，位于 BGService.exe 同目录）

### 表结构

| 表 | 说明 |
|---|---|
| `specimens` | 标本主表，含患者信息字段 + 审核快照字段（snapshot_*）+ 备注（remark / snapshot_remark） |
| `test_results` | 检验结果明细，按 specimen_id 关联标本 |
| `patients` | 患者管理数据（病案号、姓名、姓名缩写、床号、科室、性别、年龄等） |
| `system_config` | 系统配置键值对（串口、归档、备份、数据保留等） |
| `system_logs` | 系统日志（含维护任务执行记录） |
| `service_status` | 服务运行状态（单行，id=1） |
| `schema_version` | Schema 版本记录 |

### specimens 表关键字段

| 字段 | 说明 |
|---|---|
| `specimen_id` | 标本唯一号（UNIQUE） |
| `patient_id` / `patient_name` / `bed_no` / `department` / `gender` | 患者信息（运行时可被患者管理数据覆盖） |
| `original_patient_id` | 仪器原始编号（匹配患者时的优先键） |
| `status` | 0=待审核(Pending)，1=已审核(Audited) |
| `remark` | 备注（未审核时可编辑） |
| `snapshot_patient_name` / `snapshot_bed_no` / ... | 审核时保存的快照 |
| `snapshot_remark` | 审核时保存的备注快照（审核后不可修改） |

---

## 核心功能

### 1. 检验单列表自动匹配患者信息

**位置**：[MainView.cs](BGManager/Views/MainView.cs) - `MatchPatientsForSpecimensAsync` 方法

**机制**：
1. 加载当日标本列表后（`LoadDateDataAsync` / `IncrementalRefreshAsync`），立即调用 `MatchPatientsForSpecimensAsync`
2. 从 API 获取所有活跃患者（`onlyActive=true`）到 `_patientCache`
3. 对每个**待审核**标本，按以下优先级匹配患者：
   - 床位号（`BedNo`）
   - 姓名缩写（`NameAbbreviation`）
   - 病案号（`PatientId`）
4. 匹配成功后，将患者姓名、床号、病案号、性别、科室同步到标本内存对象，使列表直接显示匹配后的信息
5. 匹配结果缓存到 `_matchedPatients` 字典，点击行时无需再次请求

**默认填充首项**：
- `BindSpecimenList` 自动选中第一项
- `LoadDateDataAsync` 在绑定列表后自动调用 `ShowSelectedDetail()`，将第一项的患者信息填充到中间列，无需用户点击

**结果表批量选择**：结果表底部按钮栏提供「全选/全不选」「反选」「仅勾选异常」「复制」「预览」，方便快速复制或打印部分检验项目。

### 2. 备注字段与审核快照

**UI**：中间列"患者信息"面板底部添加"备注"多行文本框（`txtRemark`）

**行为**：
- **未审核报告**：备注可编辑（`ReadOnly = false`），用户可自由输入
- **已审核报告**：备注只读，显示审核时保存的快照值（`SnapshotRemark`），不可修改

**审核流程**（[MainView.cs](BGManager/Views/MainView.cs) - `AuditCurrentAsync`）：
1. 审核时将 `txtRemark.Text` 作为 `snapshotRemark` 传给 API
2. [SpecimenRepository.cs](BGService/Data/SpecimenRepository.cs) - `AuditWithSnapshot` 将其写入 `snapshot_remark` 列
3. 取消审核（Unarchive）时清空所有快照字段，包括 `snapshot_remark`

**API 链路**：
- [ApiClient.cs](BGManager/Services/ApiClient.cs) - `AuditSpecimenWithSnapshotAsync(id, ..., snapshotRemark, auditUser)`
- [SpecimenEndpoints.cs](BGService/Api/SpecimenEndpoints.cs) - `AuditSpecimen` 端点接收 `SnapshotRemark`
- [ISpecimenRepository.cs](BGService/Data/ISpecimenRepository.cs) - `AuditWithSnapshot` 接口签名包含 `snapshotRemark`

### 3. 打印报告与右侧内容

**打印渲染**：[MainView.cs](BGManager/Views/MainView.cs) - `PrintSingleReport` 和 [QueryView.cs](BGManager/Views/QueryView.cs) - `PrintSingleReport`

**右侧内容来源**：
- 已审核报告 → `specimen.SnapshotRemark`
- 未审核报告 → `specimen.Remark`
- 渲染在页脚右侧（`template.ShowFooter` 为 true 时）

**模板设置变更**：[PrintTemplateSettingsForm.cs](BGManager/Views/PrintTemplateSettingsForm.cs)
- "右侧内容"文本框设为**只读**，显示提示"（由标本备注自动填充，不可预设）"
- 保存模板时 `FooterRight` 强制置空，不再保存用户预设值
- 打印时忽略模板中的 `FooterRight`，统一使用标本备注

### 4. 服务端维护：自动备份与数据保留

**位置**：[MaintenanceService.cs](BGService/Services/MaintenanceService.cs)（BackgroundService，每小时检查一次，配置改动无需重启）

**自动备份**：
- 按配置周期将数据库通过 `VACUUM INTO` 备份到 `archive/backups/bloodgas_yyyyMMdd_HHmmss.db`（WAL 模式下禁止直接拷贝数据库文件，VACUUM INTO 生成一致性快照）
- 超出保留份数时自动清理最旧备份
- 上次备份时间记入 `backup.last_run`，服务重启后计时不丢失
- 托盘「手动备份」走 [BackupService.cs](BGService/Data/BackupService.cs)，同样使用 VACUUM INTO

**数据保留**：
- `data.retention_days` > 0 时，清理 `COALESCE(test_time, receive_time)` 早于截止时间的标本及其结果（事务保护），默认 0 = 永久保留

**配置入口**：BGManager 设置页「服务端维护」区（备份开关/周期/保留份数/数据保留天数/上次备份时间），经 `/api/config` 读写 `system_config` 表。

### 5. 患者录入悬浮窗

**位置**：[PatientEditForm.cs](BGManager/Views/PatientEditForm.cs)

患者新增/编辑弹窗已做适老化（12pt 字体、50px 行距、大号按钮），并提供「弹窗显示」功能：
- 点击后弹窗置顶为悬浮窗并隐藏主界面，方便对照 HIS 系统界面录入患者信息
- 实现上先解除窗口 ownership（`Owner = null`），避免主窗口隐藏时弹窗被系统连带隐藏
- 点击确定/取消后自动恢复主界面；保存成功后患者列表自动刷新

### 6. 患者信息智能识别

**位置**：[PatientEditForm.cs](BGManager/Views/PatientEditForm.cs) - `BtnSmartParse_Click`

- 从 HIS 等界面直接复制患者信息文本，粘贴到弹窗中的文本框，点击「智能识别」自动填充表单
- 按规则解析首行有效文本，支持格式如：`01床 张三 30岁 男 诊断：呼衰 职工医保 总费用：1元 住院号：000001`
  - 床号：`01床` → `1`（忽略前导 0）
  - 姓名：床号后的 2-4 位中文姓名（支持少数民族姓名中的 `·`），并自动生成拼音首字母缩写（依赖 GB2312 代码页注册）
  - 性别：独立的「男 / 女」
  - 病案号：优先识别「住院号：xxx」，兼容「病案号：xxx」
- 未能识别任何字段时给出格式示例提示，不会覆盖已有内容

### 7. 血气分析自动解读

**位置**：[AbgInterpreter.cs](BGShared/Analysis/AbgInterpreter.cs)（规则算法，设计说明见[血气分析诊断算法设计](血气分析诊断算法设计.md)）

- 输入当前标本的检验结果，输出「考虑诊断」文本；只做解读，不做诊断断言，并附带免责声明
- 酸碱失衡判读：基于 pH / pCO2 判断原发紊乱，HCO3⁻ 缺失时按 Henderson-Hasselbalch 反算，进一步判定代偿情况、AG 升高（含 Delta-Delta）、乳酸分层
- 电解质与代谢：K⁺ / Na⁺ / Cl⁻ / Ca²⁺ / 血糖 / 渗透压，含危急值提示（如 K⁺ < 2.5 或 > 6.0）
- 氧合与血红蛋白：低氧血症、I/II 型呼吸衰竭判定，THb、SO2 参考提示
- 标本类型非「动脉」时整体降级为「仅供参考」

---

## REST API

所有 API 路径前缀：`/api`，需要 `X-API-Key` 请求头鉴权（`/api/health` 除外）。

### 标本接口

| 方法 | 路径 | 说明 |
|---|---|---|
| GET | `/api/specimens` | 分页查询标本（支持日期范围、状态、患者等筛选） |
| GET | `/api/specimens/{id}` | 获取单个标本详情 |
| POST | `/api/specimens/query` | POST 方式查询（请求体传参） |
| POST | `/api/specimens/{id}/audit` | 审核标本（含快照信息） |
| POST | `/api/specimens/{id}/unarchive` | 取消审核 |
| DELETE | `/api/specimens/{id}` | 删除标本 |
| GET | `/api/specimens/latest/{count}` | 获取最新 N 条标本 |

### 患者接口

| 方法 | 路径 | 说明 |
|---|---|---|
| GET | `/api/patients?onlyActive={bool}` | 获取患者列表 |
| GET | `/api/patients/{id}` | 按主键获取患者 |
| GET | `/api/patients/search?q={keyword}&limit={n}` | 搜索患者 |
| GET | `/api/patients/find?q={keyword}` | 查找单个患者（床号/缩写/病案号） |
| GET | `/api/patients/findbyid?patientId={id}` | 按病案号精确查找 |
| POST | `/api/patients` | 新增患者 |
| PUT | `/api/patients/{id}` | 更新患者 |
| DELETE | `/api/patients/{id}` | 删除患者 |

### 系统/管理接口

| 方法 | 路径 | 说明 |
|---|---|---|
| GET | `/api/health` | 健康检查（无需鉴权） |
| GET | `/api/status` | 服务状态（串口、今日标本数、运行时长等） |
| GET | `/api/status/stats/daily` | 当日统计 |
| GET | `/api/config` / `/api/config/{key}` | 读取系统配置 |
| PUT | `/api/config/{key}` | 写入系统配置（`{ value, category, description }`） |
| POST | `/api/admin/restart` | 重启串口 |
| POST | `/api/admin/reparse` | 重新解析归档报文 |
| POST | `/api/admin/inject` | 注入模拟消息 |

---

## 配置

### BGManager 配置

文件：`config.json`（与 BGManager.exe 同目录，不存在时使用默认值）

```json
{
  "ServerUrl": "http://localhost:5000",
  "ApiKey": "",
  "ShortcutKeys": {
    "Refresh": "F5",
    "Audit": "F6",
    "Unarchive": "F7",
    "Delete": "Del",
    "Search": "Ctrl+F",
    "Print": "Ctrl+P"
  }
}
```

### BGService 配置

- [appsettings.json](BGService/appsettings.json)：服务运行参数，由 [SettingsManager.cs](BGService/Config/SettingsManager.cs) 读写

```json
{
  "DatabasePath": "bloodgas.db",
  "ArchivePath": "archive",
  "LogPath": "logs",
  "ApiPort": 5000,
  "SerialPort": "COM1",
  "BaudRate": 9600,
  "AutoStart": false,
  "StartMinimized": true
}
```

- API 鉴权：可选配置 `"Api": { "Key": "xxx" }`，BGManager 的 `config.json` 中 `ApiKey` 需与之一致；未配置时 API 无鉴权，仅建议单机部署，局域网部署请配置 Key 并用防火墙限制端口来源

### system_config 表维护配置键

| 键 | 默认值 | 说明 |
|---|---|---|
| `backup.enabled` | `true` | 是否启用自动备份 |
| `backup.interval_hours` | `24` | 自动备份周期（小时） |
| `backup.keep_count` | `10` | 备份保留份数，超出清理最旧 |
| `backup.last_run` | - | 上次自动备份时间（系统写入） |
| `data.retention_days` | `0` | 数据保留天数，0 = 永久保留 |

### 打印 / 显示配置

- 打印模板：`%LOCALAPPDATA%\BGManager\print_template.json`，管理见 [PrintTemplateManager.cs](BGShared/Config/PrintTemplateManager.cs)，可通过设置界面导入/导出/重置
- 显示项配置：`%LOCALAPPDATA%\BGManager\display_config.json`，管理见 [DisplayConfigManager.cs](BGShared/Config/DisplayConfigManager.cs)，可在「显示设置」中调整项目顺序与参考范围

---

## 快捷键

| 功能 | 默认快捷键 |
|---|---|
| 刷新当日数据 | F5 |
| 审核 | F6 |
| 取消审核 | F7 |
| 删除标本 | Del |
| 跳转查询 | Ctrl+F |
| 打印预览 | Ctrl+P |

---

## 构建与运行

### 前置要求

- .NET 10 SDK
- Windows 操作系统（WinForms 依赖）

### 构建

```bash
dotnet build BGSystem.sln
```

### 运行 BGService（后台服务）

```bash
dotnet run --project BGService
```

BGService 启动后以托盘图标运行，监听串口并提供 REST API（默认端口 5000）。

### 运行 BGManager（桌面客户端）

```bash
dotnet run --project BGManager
```

### 打包发布（自包含，免装运行时）

```bash
dotnet publish BGService/BGService.csproj -c Release -r win-x64 --self-contained true -o out/BGService
dotnet publish BGManager/BGManager.csproj -c Release -r win-x64 --self-contained true -o out/BGManager
```

将 `out/BGService` 与 `out/BGManager` 两个目录一起分发即可，结构与发行包一致。

---

## 数据库迁移与老数据兼容

**位置**：[SchemaMigration.cs](BGShared/Database/SchemaMigration.cs)

采用版本化迁移机制，当前 Schema 版本为 **5**。

| 版本 | 内容 |
|---|---|
| V1 | 初始建表：specimens、test_results、patients、system_config、system_logs、service_status + 默认配置 |
| V2 | specimens 增加 `gender` 列 |
| V3 | specimens 增加 `original_patient_id` + 8 个 `snapshot_*` 列 |
| V4 | 创建全部索引（标本、结果、患者、日志） |
| V5 | specimens 增加 `snapshot_remark TEXT DEFAULT ''`（备注快照） |

### 老数据兼容性

- **V5 迁移**使用 `ALTER TABLE ... ADD COLUMN snapshot_remark TEXT DEFAULT ''`
- SQLite 的 `ADD COLUMN ... DEFAULT` 会自动为所有已有行填充默认值（空字符串），不存在数据不兼容问题
- `MapSpecimen` 通过列名 switch 读取 `snapshot_remark`，老库迁移后该列存在且默认为空串
- [LegacyDbMigrator.cs](BGManager/Tools/LegacyDbMigrator.cs) 提供从旧版数据库迁移的工具（重建 Schema + 迁移数据）

### 迁移触发时机

BGService 启动时 `DbConnectionFactory.CreateConnection()` 调用 `SchemaMigration.EnsureSchema`，自动检测版本并按需执行增量迁移。

---

## 测试与联调

### 单元测试

```bash
dotnet test BGSystem.Tests
```

- [ASTMParserTests.cs](BGSystem.Tests/ASTMParserTests.cs)：ASTM 协议解析测试
- [AbgInterpreterTests.cs](BGSystem.Tests/AbgInterpreterTests.cs)：血气解读规则测试

### TestInjector

控制台程序，向 BGService 的 `/api/admin/inject` 端点注入模拟 ASTM 报文，用于联调串口解析与存储流程：

```bash
dotnet run --project TestInjector
```

也可以使用 C# 脚本 [test_inject.csx](test_inject.csx)（需先修改脚本内的报文目录路径）。

### 归档报文

原始报文归档在 `Archive/YYYYMM/` 目录，同时保存 `.hl7`（原始）和 `.json`（解析结果）两种格式。

### 备份文件

自动/手动备份位于 `archive/backups/` 目录，文件名格式 `bloodgas_yyyyMMdd_HHmmss.db`，均为 VACUUM INTO 生成的完整一致性快照。

---

## 文档

- [血气分析诊断算法设计](血气分析诊断算法设计.md)：解读规则的判定逻辑与参考范围说明
- [README](README.MD)：本文件
