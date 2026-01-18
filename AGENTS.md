# 仓库指引

## 项目结构与模块划分
- 根目录：`ControlEntradaSalida.sln`、`ControlEntradaSalida.csproj`，WinForms 窗体（`*.cs/*.Designer.cs/*.resx`），如 `MDIParent`、`GestionEmpleados`、`GestionDispositivos`、`LoginDevice`。
- SDK/互操作：`HCNetSDK*.cs`，辅助类在 `Common.cs`、`DeviceConnectionManager.cs`、`DeviceStatusControl.cs`、`DeviceStatusEngine.cs`。
- 数据：`Database/`（SQL 脚本、报表 `*.rdlc`）；`App.config`（连接字符串名称 `mysql`）。
- 资源：`Resources/`、`Properties/`。输出在 `bin/`（Debug/Release），中间产物在 `obj/`。
- 测试（如新增）：`tests/ControlEntradaSalida.Tests`；集成示例在 `tests/Integration/`。

## 构建、测试与开发命令
- 还原包：`nuget restore ControlEntradaSalida.sln` — 还原 `packages.config` 依赖。
- Debug 构建：`msbuild ControlEntradaSalida.sln /t:Build /p:Configuration=Debug`。
- 运行（Debug）：`bin/Debug/ControlEntradaSalida.exe`。
- Release 构建：`msbuild ControlEntradaSalida.sln /p:Configuration=Release`。
- 推荐构建：`dotnet build ControlEntradaSalida.sln --verbosity minimal`。
说明：运行时需确保第三方 DLL（Hikvision SDK、`SqlServerTypes`）在 `bin/` 或引用目录中。

## 编码风格与命名规范
- 缩进：4 空格；Allman 大括号风格（`{` 换行）。
- C#：类/方法/公有成员使用 PascalCase；局部变量/参数使用 camelCase；私有字段使用 camelCase（不加前导下划线）。
- 每个文件仅一个 public 类型；不要修改 `.Designer.cs` 中的生成区域。
- UI 文案为中文；保持本地化一致性。

## 测试规范
- 框架：优先 MSTest 或 NUnit（`tests/ControlEntradaSalida.Tests`）。
- 命名：`ClassName_MethodName_ExpectedBehavior`（如 `Common_Login_ReturnsUserId`）。
- 设备/SDK 调用要 mock（`HCNetSDK`），避免单测依赖真实硬件。
- 集成示例放在 `tests/Integration/`，并用配置开关保护。

## 提交与 PR 规范
- 提交：Conventional Commits（`feat:`、`fix:`、`refactor:`、`perf:`、`docs:`），范围（scope）可选；中文描述可用。
  示例：`feat(设备状态): 新增卡片式显示`、`fix(UI): 修复悬停状态不一致`。
- PR：包含清晰说明、关联 issue、UI 截图、验证步骤，以及 DB/报表变更说明（`Database/`、`*.rdlc`）。若改动 `App.config` 键请同步文档。

## 安全与配置提示
- 不要提交密钥。使用 `App.config` 做本地开发，新增键位用脱敏示例。
- 依据 SDK 要求确认平台（x86/x64），DLL 放在 `bin/` 或引用目录。
- 连接字符串名称必须为 `mysql`；数据库变更通过 `Database/` 脚本协同。

在与设备交互并需要调用 SDK 接口时，请严格按照设备网络 SDK 编程指南（明眸-以人为中心）文件夹下的 `技术规范.md` 操作。务必查阅该文件夹下的相关技术文档，确保接口调用准确与安全。

- 接口调用：按文档要求使用正确接口、参数与返回值，避免未定义接口或错误参数类型。
- 错误处理：及时处理错误码，按错误码进行重试、日志记录或用户提示。
- 性能优化：按文档建议优化调用频率与数据量，避免频繁调用引发性能问题。

编译命令请使用 `dotnet build ControlEntradaSalida.sln --verbosity minimal`

回答请使用中文。

检查当前文件夹是否已使用 serena 激活为项目；若已激活，后续语义检索/符号级编辑/引用分析/代码写入必须优先使用 serena 工具。若未激活，请主动激活，并遵循 MCP Rules (MCP 调用规则)。
