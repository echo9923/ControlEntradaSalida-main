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



MCP Rules (MCP 调用规则)

目标

- 为 Claude 提供4项 MCP 服务（Sequential Thinking、DuckDuckGo、Context7、Serena）的选择与调用规范，控制查询粒度、速率与输出格式，保证可追溯与安全。

全局策略

- 工具选择：根据任务意图选择最匹配的 MCP 服务；避免无意义并发调用。
- 结果可靠性：默认返回精简要点 + 必要引用来源；标注时间与局限。
- 单轮单工具：每轮对话最多调用 1 种外部服务；确需多种时串行并说明理由。
- 最小必要：收敛查询范围（tokens/结果数/时间窗/关键词），避免过度抓取与噪声。
- 可追溯性：统一在答复末尾追加“工具调用简报”（工具、输入摘要、参数、时间、来源/重试）。
- 安全合规：默认离线优先；外呼须遵守 robots/ToS 与隐私要求，必要时先征得授权。
- 降级优先：失败按“失败与降级”执行，无法外呼时提供本地保守答案并标注不确定性。
- 冲突处理：遵循“冲突与优先级”的顺序，出现冲突时采取更保守策略。

速率与并发限制

- 速率限制：若收到 429/限流提示，退避 20 秒，降低结果数/范围；必要时切换备选服务。

安全与权限边界

- 隐私与安全：不上传敏感信息；遵循只读网络访问；遵守网站 robots 与 ToS。

失败与降级

- 失败回退：首选服务失败时，按优先级尝试替代；不可用时给出明确降级说明。

Sequential Thinking（规划分解）

- 触发：分解复杂问题、规划步骤、生成执行计划、评估方案。
- 输入：简要问题、目标、约束；限制步骤数与深度。
- 输出：仅产出可执行计划与里程碑，不暴露中间推理细节。
- 约束：步骤上限 6-10；每步一句话；可附工具或数据依赖的占位符。

DuckDuckGo（Web 搜索）

- 触发：需要最新网页信息、官方链接、新闻文档入口。
- 查询：使用 12 个精准关键词 + 限定词（如 site:, filetype:, after:YYYY-MM）。
- 结果：返回前 35 条高置信来源；避免内容农场与异常站点。
- 输出：每条含标题、简述、URL、抓取时间；必要时附二次验证建议。
- 禁用：网络受限且未授权；可离线完成；查询包含敏感数据/隐私。
- 参数与执行：safesearch=moderate；地区/语言=auto（可指定）；结果上限≤35；超时=5s；严格串行；遇 429 退避 20 秒并降低结果数；必要时切换备选服务。
- 过滤与排序：优先官方域名与权威媒体；按相关度与时效排序；域名去重；剔除内容农场/异常站点/短链重定向。
- 失败与回退：无结果/歧义→建议更具体关键词或限定词；网络受限→请求授权或请用户提供候选来源；最多一次重试，仍失败则给出降级说明与保守答案。

Context7（技术文档知识聚合）

- 触发：查询 SDK/API/框架官方文档、快速知识提要、参数示例片段。
- 流程：先 resolve-library-id；确认最相关库；再 get-library-docs。
- 主题与查询：提供 topic/关键词聚焦；tokens 默认 5000，按需下调以避免冗长（示例 topic：hooks、routing、auth）。
- 筛选：多库匹配时优先信任度高与覆盖度高者；歧义时请求澄清或说明选择理由。
- 输出：精炼答案 + 引用文档段落链接或出处标识；标注库 ID/版本；给出关键片段摘要与定位（标题/段落/路径）；避免大段复制。
- 限制：网络受限或未授权不调用；遵守许可与引用规范。
- 失败与回退：无法 resolve 或无结果时，请求澄清或基于本地经验给出保守答案并标注不确定性。
- 无 Key 策略：可直接调用；若限流则提示并降级到 DuckDuckGo（优先官方站点）。

Serena（代码语义检索/符号级编辑)

- 用途：提供基于语言服务器（LSP）的符号级检索与代码编辑能力，帮助在大型代码库中高效定位、理解并修改代码。
- 触发：需要按符号/语义查找、跨文件引用分析、重构迁移、在指定符号前后插入或替换实现等场景。
- 流程：项目激活与索引 → 精准检索符号/引用 → 验证上下文 → 执行插入/替换 → 汇总变更与理由。
- 常用工具：
  - find_symbol / find_referencing_symbols / get_symbols_overview
  - insert_before_symbol / insert_after_symbol / replace_symbol_body
  - search_for_pattern / find_file / read_file / create_text_file / write_file
- 使用策略：优先小范围、精准操作；单轮单工具；输出需带符号/文件定位与变更原因，便于追溯。
- 示例范式：
  - “定位 Controller 方法并前置校验”：find_symbol → insert_before_symbol
  - “统计实体引用并逐点修订”：find_referencing_symbols → replace_symbol_body 或 replace_regex

服务清单与用途

- Sequential Thinking：规划与分解复杂任务，形成可执行计划与里程碑。
- Context7：检索并引用官方文档/API，用于库/框架/版本差异与配置问题。
- DuckDuckGo：获取最新网页信息、官方链接与新闻/公告来源聚合。
- Serena：代码语义检索、符号级编辑、引用分析

服务选择与调用

- 意图判定：规划/分解 → Sequential；文档/API → Context7；最新信息 → DuckDuckGo。
- 前置检查：网络与权限、敏感信息、是否可离线完成、范围是否最小必要。
- 单轮单工具：按“全局策略”执行；确需多种，串行并说明理由与预期产出。
- 调用流程：
  - 设定目标与范围（关键词/库ID/topic/tokens/结果数/时间窗）。
  - 执行调用（遵守速率限制与安全边界）。
  - 失败回退（按“失败与降级”）。
  - 输出简报（来源/参数/时间/重试），确保可追溯。
- 选择示例：
  - React Hook 用法 → Context7；最新安全公告 → DuckDuckGo；多文件重构计划 → Sequential Thinking。
- 终止条件：获得足够证据或达到步数/结果上限；超限则请求澄清。
