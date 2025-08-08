# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## 项目概述

这是一个基于 .NET Framework 4.7.2 的 Windows Forms 门禁控制系统，主要用于管理和监控海康威视设备的进出记录。

## 构建和运行

### 构建项目
```bash
# 使用 MSBuild 构建
msbuild ControlEntradaSalida.csproj /p:Configuration=Debug /p:Platform=AnyCPU

# 或使用 Visual Studio 构建
devenv ControlEntradaSalida.sln /Build Debug
```

### 运行应用
```bash
# 运行编译后的可执行文件
bin\Debug\ControlEntradaSalida.exe
```

## 核心架构

### 主要组件
- **MDIParent.cs**: 主界面窗体，管理所有子窗体和菜单
- **BaseDatosMySQL.cs**: MySQL 数据库连接和操作类
- **Common.cs**: 通用工具类，处理海康威视 SDK 通信和数据库连接
- **HCNetSDK.cs**: 海康威视设备 SDK 接口定义

### 功能模块
1. **设备管理** (GestionDispositivos.cs): 管理海康威视设备
2. **员工管理** (GestionEmpleados.cs): 管理员工信息和权限
3. **进出记录** (CapturaEntradaSalida.cs): 采集和显示进出记录
4. **报表系统** (Informe.cs, ParamInforme*.cs): 生成各种报表
5. **门禁控制** (controldoor.cs): 控制门禁设备

### 数据库配置
- 数据库类型: MySQL
- 连接字符串在 App.config 中配置
- 默认连接: `server=localhost;user=root;database=control_entrada_salida;port=3306;password=12345678`

## 关键依赖

### 核心包
- **MySql.Data (9.3.0)**: MySQL 数据库连接
- **Microsoft.ReportingServices.ReportViewerControl.Winforms (150.1404.0)**: 报表查看器
- **Newtonsoft.Json (13.0.3)**: JSON 处理
- **System.Configuration.ConfigurationManager (8.0.0)**: 配置管理

### 海康威视 SDK
- HCNetSDK.cs: 主要 SDK 接口
- HCNetSDK_Facial.cs: 人脸识别功能
- HCNetSDK_Tarjeta.cs: 卡片识别功能

## 开发注意事项

### SDK 初始化
- 应用启动时需要在 MDIParent_Load 中初始化海康威视 SDK
- 应用关闭时需要在 MDIParent_FormClosing 中清理 SDK 资源

### 数据库操作
- 使用 BaseDatosMySQL 类进行数据库连接
- 连接字符串通过 Common.obtenerCadenaConexion() 获取
- 所有数据库操作都需要处理 MySQL 异常

### 窗体管理
- 所有子窗体都设置为 MDI 子窗体
- 使用 MdiParent 属性设置父窗体
- 通过菜单项点击事件创建和显示窗体

### 错误处理
- SDK 操作需要检查返回值和错误码
- 数据库操作需要捕获 MySqlException
- 界面操作需要处理 Windows Forms 异常

## 文件结构

```
├── ProductAcs/               # 访问控制相关功能
│   ├── HolidayPlan.cs        # 假期计划
│   ├── WeekPlan.cs          # 周计划
│   └── PlanTemplateM.cs     # 计划模板
├── SqlServerTypes/          # SQL Server 类型支持
├── Resources/               # 界面资源文件
├── *.rdlc                   # 报表定义文件
└── *.cs                     # 主要源代码文件
```

## 平台支持

- 目标框架: .NET Framework 4.7.2
- 支持平台: x86, x64, AnyCPU
- 操作系统: Windows
- 数据库: MySQL 5.7+