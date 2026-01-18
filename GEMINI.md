# Repository Guidelines

## Project Structure & Module Organization
- Root: `ControlEntradaSalida.sln`, `ControlEntradaSalida.csproj`, WinForms forms (`*.cs/*.Designer.cs/*.resx`) such as `MDIParent`, `GestionEmpleados`, `GestionDispositivos`, `LoginDevice`.
- SDK/Interop: `HCNetSDK*.cs`, helpers in `Common.cs`, `DeviceConnectionManager.cs`, `DeviceStatusControl.cs`.
- Data: `Database/` (SQL scripts, reports `*.rdlc`); `App.config` (connection string name `mysql`).
- Assets: `Resources/`, `Properties/`. Outputs in `bin/` (Debug/Release), intermediates in `obj/`.
- Tests (when added): `tests/ControlEntradaSalida.Tests`; integration samples in `tests/Integration/`.

## Build, Test, and Development Commands
- Restore packages: `nuget restore ControlEntradaSalida.sln` — restores `packages.config` dependencies.
- Debug build: `msbuild ControlEntradaSalida.sln /t:Build /p:Configuration=Debug`.
- Run (Debug): `bin/Debug/ControlEntradaSalida.exe`.
- Release build: `msbuild ControlEntradaSalida.sln /p:Configuration=Release`.
Notes: Vendor DLLs (Hikvision SDK, `SqlServerTypes`) must be present in `bin/` or referenced folders for runtime.

## Coding Style & Naming Conventions
- Indentation: 4 spaces; Allman braces (newline before `{`).
- C#: PascalCase for classes/methods/public members; camelCase for locals/parameters; private fields camelCase (no leading underscore).
- One public type per file; do not edit generated regions in `.Designer.cs`.
- UI strings are Chinese; keep localization consistent.

## Testing Guidelines
- Framework: prefer MSTest or NUnit in `tests/ControlEntradaSalida.Tests`.
- Naming: `ClassName_MethodName_ExpectedBehavior` (e.g., `Common_Login_ReturnsUserId`).
- Mock device/SDK calls (`HCNetSDK`); avoid real hardware in unit tests.
- Place integration samples under `tests/Integration/` and guard with config flags.

## Commit & Pull Request Guidelines
- Commits: Conventional Commits (`feat:`, `fix:`, `refactor:`, `perf:`, `docs:`); scope optional; Chinese descriptions welcome.
  Examples: `feat(设备状态): 新增卡片式显示`, `fix(UI): 修复悬停状态不一致`.
- PRs: include clear description, linked issue, screenshots for UI, verification steps, and notes for DB/report changes (`Database/`, `*.rdlc`). Update docs if `App.config` keys change.

## Security & Configuration Tips
- Do not commit secrets. Use `App.config` for local dev; provide sanitized samples for new keys.
- Ensure correct platform (x86/x64) per vendor SDK; place DLLs in `bin/` or referenced folders.
- Connection string name is `mysql`. Coordinate schema changes via scripts in `Database/`.
  
在与设备交互并需要调用SDK接口时，请严格按照设备网络SDK编程指南（明眸-以人为中心）文件夹下的技术规范.md进行操作。务必仔细查阅该文件夹下的所有相关技术文档，确保接口调用的准确性和安全性。

\- 接口调用：根据文档要求，使用正确的接口名称、参数和返回值。避免使用未定义的接口或错误的参数类型。

\- 错误处理：在调用接口时，及时处理返回的错误码。根据错误码进行相应的错误处理，如重试、日志记录或用户提示。

\- 性能优化：根据文档建议，优化接口调用的频率和数据量。避免频繁调用导致的性能问题。

编译命令请使用 dotnet build ControlEntradaSalida.sln --verbosity minimal

Translate your answer to Chinese.


使用serena将当前目录激活为项目