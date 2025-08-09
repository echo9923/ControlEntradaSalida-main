# GEMINI.md

This file provides guidance to Qwen Code (Qwen) when working with code in this repository.

## 项目概述

这是一个基于 .NET Framework 4.7.2 的 Windows Forms 门禁控制系统。它用于管理海康威视（Hikvision）门禁设备，包括设备管理、员工管理、进出记录采集、报表生成和门禁控制等功能。

## 构建和运行

### 构建项目
```bash
# 使用 MSBuild 构建 (需安装 Visual Studio 或 Build Tools)
msbuild ControlEntradaSalida.csproj /p:Configuration=Debug /p:Platform=AnyCPU

# 或使用 Visual Studio 构建
devenv ControlEntradaSalida.sln /Build Debug
```

### 运行应用
```bash
# 运行编译后的可执行文件
bin\Debug\ControlEntradaSalida.exe
```
**注意:** 应用程序需要 MySQL 数据库和海康威视设备才能正常运行。

### 数据库设置
- 数据库类型: MySQL
- 需要先创建数据库 `control_entrada_salida`。
- 使用 `control_entradas_salidas.sql` 脚本初始化数据库表结构。
- 连接字符串配置在 `App.config` 文件中，默认为 `server=localhost;user=root;database=control_entrada_salida;port=3306;password=12345678`。

## 核心架构

### 主要组件
- **MDIParent.cs**: 主界面窗体，是应用程序的入口点，管理所有子窗体和菜单。
- **BaseDatosMySQL.cs**: 封装了 MySQL 数据库连接和断开连接的逻辑。
- **Common.cs**: 通用工具类，负责初始化海康威视 SDK、读取数据库连接字符串、创建本地数据目录以及封装 SDK 登录和 ISAPI 请求。
- **HCNetSDK.cs / HCNetSDK_Facial.cs / HCNetSDK_Tarjeta.cs**: 海康威视设备 SDK 接口定义和特定功能封装。

### 功能模块
1.  **设备管理** (`GestionDispositivos.cs`): 添加、编辑、删除和连接海康威视设备。
2.  **员工管理** (`GestionEmpleados.cs`): 管理员工信息（姓名、照片、卡号）及其在设备上的状态。
3.  **进出记录采集** (`CapturaEntradaSalida.cs`): 从连接的设备中获取进出事件记录并存储到本地数据库。
4.  **报表系统** (`Informe.cs`, `ParamInforme*.cs`, `*.rdlc`): 根据日期范围等参数生成进出记录和事件报表。
5.  **门禁控制** (`controldoor.cs`): 远程控制门的开关。
6.  **计划模板** (`Plantemplate.cs`, `ProductAcs\*.cs`): 管理假期计划、周计划等门禁策略。

### 数据库配置
- 数据库类型: MySQL
- 连接字符串在 `App.config` 中配置。
- 默认连接: `server=localhost;user=root;database=control_entrada_salida;port=3306;password=12345678`

## 关键依赖

### 核心 NuGet 包
- **MySql.Data (9.3.0)**: 用于连接和操作 MySQL 数据库。
- **Microsoft.ReportingServices.ReportViewerControl.Winforms (150.1404.0)**: 用于在 WinForms 应用中显示 RDLC 报表。
- **Newtonsoft.Json (13.0.3)**: 用于处理 JSON 数据。
- **System.Configuration.ConfigurationManager (8.0.0)**: 用于读取 `App.config` 配置文件。
- **BouncyCastle.Cryptography (2.5.1)**: 可能用于加密相关功能。
- **Google.Protobuf (3.30.0)**: 可能用于数据序列化。
- **ZstdSharp (0.8.5)**: 可能用于数据压缩。

### 海康威视 SDK
- **HCNetSDK.dll 及相关文件**: 与海康威视设备进行通信的核心库。项目中通过 `HCNetSDK.cs` 等文件进行 P/Invoke 调用。

## 开发注意事项

### SDK 初始化与清理
- 应用启动时需要在 `MDIParent_Load` 中调用 `Common.InicializarSDKHikVision()` 初始化海康威视 SDK。
- 应用关闭时需要在 `MDIParent_FormClosing` 中调用 `HCNetSDK.NET_DVR_Logout_V30` 和 `HCNetSDK.NET_DVR_Cleanup` 清理 SDK 资源。

### 数据库操作
- 使用 `BaseDatosMySQL` 类进行数据库连接 (`conectarMySQL`) 和断开连接 (`desconectarMySQL`)。
- 连接字符串通过 `Common.obtenerCadenaConexion()` 获取。
- 所有数据库操作都需要处理 `MySqlException`。

### 窗体管理
- 所有子窗体都设置为 MDI 子窗体 (`frm.MdiParent = this;`)。
- 通过主窗体 `MDIParent` 的菜单项点击事件创建和显示功能窗体。

### 错误处理
- SDK 操作需要检查返回值和通过 `HCNetSDK.NET_DVR_GetLastError()` 获取错误码。
- 数据库操作需要捕获 `MySqlException`。
- 界面操作需要处理 `System.Exception` 等 Windows Forms 异常。

## 文件结构

```
├── ProductAcs/               # 访问控制相关高级功能模块
│   ├── HolidayPlan.cs        # 假期计划管理窗体
│   ├── HolidayGroupPlan.cs   # 假期组计划管理窗体
│   ├── PlanTemplateM.cs      # 计划模板管理窗体
│   ├── WeekPlan.cs           # 周计划管理窗体
│   └── *.designer.cs, *.resx # 对应窗体的设计器和资源文件
├── SqlServerTypes/          # SQL Server 类型支持（报表功能依赖）
├── Resources/               # 界面使用的图标等资源文件
├── *.rdlc                   # 报表定义文件 (RDLC)
├── *.cs                     # 主要的 C# 源代码文件
├── *.designer.cs            # Windows Forms 设计器生成的代码
├── *.resx                   # Windows Forms 资源文件
├── App.config               # 应用程序配置文件（数据库连接字符串等）
├── ControlEntradaSalida.csproj # 项目文件
├── ControlEntradaSalida.sln # 解决方案文件
├── packages.config          # NuGet 包配置文件
└── control_entradas_salidas.sql # 数据库初始化脚本
```

## 平台支持

- 目标框架: .NET Framework 4.7.2
- 支持平台: x86, x64, AnyCPU
- 操作系统: Windows
- 数据库: MySQL 5.7+