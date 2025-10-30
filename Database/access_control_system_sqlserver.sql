-- =====================================================
-- SQL Server Script for Access Control System
-- =====================================================
-- 系统名称: ControlEntradaSalida 门禁管理系统
-- 脚本版本: v2.0 (SQL Server Edition)
-- 生成日期: 2025-10-30
-- 说明: 从 MySQL 转换的数据库脚本,保持完全等价的表结构和字段
--
-- 核心功能:
-- 1. 设备管理 - 门禁设备的注册、配置和连接管理
-- 2. 员工管理 - 员工信息和门禁权限管理(已合并权限信息)
-- =====================================================

-- =====================================================
-- Database: access_control_system
-- =====================================================
-- 如果数据库存在则删除 (谨慎使用)
IF EXISTS (SELECT name FROM sys.databases WHERE name = N'access_control_system')
BEGIN
    ALTER DATABASE access_control_system SET SINGLE_USER WITH ROLLBACK IMMEDIATE;
    DROP DATABASE access_control_system;
END
GO

-- 创建数据库,使用 UTF-8 排序规则
CREATE DATABASE access_control_system
COLLATE Chinese_PRC_CI_AS;
GO

USE access_control_system;
GO

-- =====================================================
-- 表: devices (门禁设备表)
-- =====================================================
-- 用途: 存储门禁设备的基本信息和连接配置
-- 使用位置:
--   - LoginDevice.cs (设备注册和编辑)
--   - GestionDispositivos.cs (设备列表管理)
--   - DeviceConnectionManager.cs (设备连接管理)
--   - MDIParent.cs (设备状态监控)
-- =====================================================
IF OBJECT_ID('dbo.devices', 'U') IS NOT NULL
    DROP TABLE dbo.devices;
GO

CREATE TABLE dbo.devices (
    -- 设备唯一标识(主键)
    device_id INT NOT NULL IDENTITY(1,1),

    -- 设备名称
    device_name NVARCHAR(255) NOT NULL,

    -- 设备所属区域(生产区域/办公区域)
    description NVARCHAR(255) NULL,

    -- 设备IP地址
    ip_address NVARCHAR(20) NOT NULL,

    -- 设备端口号
    port NVARCHAR(5) NOT NULL DEFAULT '8000',

    -- 设备登录用户名
    username NVARCHAR(45) NOT NULL DEFAULT 'admin',

    -- 设备登录密码
    password NVARCHAR(255) NOT NULL,

    -- 设备状态: 1=启用 0=禁用 (新增时默认为1)
    status TINYINT NOT NULL DEFAULT 1,

    -- 最后使用时间(设备最后连接或操作的时间)
    last_used_time DATETIME2(0) NULL,

    -- 创建时间
    created_at DATETIME2(0) NOT NULL DEFAULT SYSDATETIME(),

    -- 最后更新时间
    updated_at DATETIME2(0) NOT NULL DEFAULT SYSDATETIME(),

    CONSTRAINT PK_devices PRIMARY KEY (device_id)
);
GO

-- 创建索引
CREATE INDEX idx_ip_address ON dbo.devices(ip_address);  -- 通过IP地址快速查找设备
CREATE INDEX idx_status ON dbo.devices(status);          -- 筛选启用/禁用的设备
GO

-- 创建触发器以实现 updated_at 自动更新
CREATE TRIGGER trg_devices_update
ON dbo.devices
AFTER UPDATE
AS
BEGIN
    SET NOCOUNT ON;
    UPDATE dbo.devices
    SET updated_at = SYSDATETIME()
    FROM dbo.devices d
    INNER JOIN inserted i ON d.device_id = i.device_id;
END;
GO

-- 添加表注释 (SQL Server 使用扩展属性)
EXEC sys.sp_addextendedproperty
    @name = N'MS_Description',
    @value = N'门禁设备信息表',
    @level0type = N'SCHEMA', @level0name = N'dbo',
    @level1type = N'TABLE', @level1name = N'devices';
GO

-- =====================================================
-- 表: employees (员工信息表)
-- =====================================================
-- 用途: 存储员工基本信息,用于门禁权限管理
-- 使用位置:
--   - GestionEmpleados.cs (员工的增删改查所有操作)
-- =====================================================
IF OBJECT_ID('dbo.employees', 'U') IS NOT NULL
    DROP TABLE dbo.employees;
GO

CREATE TABLE dbo.employees (
    -- 员工编号(主键)
    employee_id NVARCHAR(30) NOT NULL,

    -- 卡号(与员工编号相同,用于门禁识别)
    card_number NVARCHAR(20) NOT NULL,

    -- 员工完整姓名
    full_name NVARCHAR(255) NOT NULL,

    -- 人脸照片存储路径
    photo_path NVARCHAR(255) NOT NULL DEFAULT '',

    -- 员工状态: ACTIVE=在职 INACTIVE=离职
    status NVARCHAR(20) NOT NULL DEFAULT 'ACTIVE',

    -- 权限级别: 0=无任何权限 1=仅可进入办公区域 2=可进入办公和生产区域
    permission_level TINYINT NOT NULL DEFAULT 0,

    -- 最近一次同步到设备的权限级别
    last_synced_level TINYINT NULL,

    -- 权限级别最后更新时间
    permission_updated_at DATETIME2(0) NULL,

    -- 权限最近一次同步到设备的时间
    last_synced_at DATETIME2(0) NULL,

    -- 创建时间
    created_at DATETIME2(0) NOT NULL DEFAULT SYSDATETIME(),

    -- 最后更新时间
    updated_at DATETIME2(0) NOT NULL DEFAULT SYSDATETIME(),

    CONSTRAINT PK_employees PRIMARY KEY (employee_id),
    CONSTRAINT uk_card_number UNIQUE (card_number),
    CONSTRAINT chk_status CHECK (status IN ('ACTIVE', 'INACTIVE'))
);
GO

-- 创建索引
CREATE INDEX idx_status ON dbo.employees(status);                      -- 快速筛选在职/离职员工
CREATE INDEX idx_full_name ON dbo.employees(full_name);                -- 通过姓名搜索员工
CREATE INDEX idx_permission_level ON dbo.employees(permission_level);  -- 按权限级别筛选员工
GO

-- 创建触发器以实现 updated_at 自动更新
CREATE TRIGGER trg_employees_update
ON dbo.employees
AFTER UPDATE
AS
BEGIN
    SET NOCOUNT ON;
    UPDATE dbo.employees
    SET updated_at = SYSDATETIME()
    FROM dbo.employees e
    INNER JOIN inserted i ON e.employee_id = i.employee_id;
END;
GO

-- 添加表注释
EXEC sys.sp_addextendedproperty
    @name = N'MS_Description',
    @value = N'员工信息表(包含权限信息)',
    @level0type = N'SCHEMA', @level0name = N'dbo',
    @level1type = N'TABLE', @level1name = N'employees';
GO

-- =====================================================
-- 存储过程: delete_employee
-- =====================================================
-- 用途: 安全删除员工记录
-- 使用位置: GestionEmpleados.cs:679
-- 说明: 使用事务确保数据一致性
-- =====================================================
IF OBJECT_ID('dbo.delete_employee', 'P') IS NOT NULL
    DROP PROCEDURE dbo.delete_employee;
GO

CREATE PROCEDURE dbo.delete_employee
    @emp_id NVARCHAR(30)
AS
BEGIN
    SET NOCOUNT ON;

    BEGIN TRY
        BEGIN TRANSACTION;

        -- 删除员工记录
        DELETE FROM dbo.employees WHERE employee_id = @emp_id;

        COMMIT TRANSACTION;
    END TRY
    BEGIN CATCH
        IF @@TRANCOUNT > 0
            ROLLBACK TRANSACTION;

        -- 重新抛出错误
        DECLARE @ErrorMessage NVARCHAR(4000) = ERROR_MESSAGE();
        DECLARE @ErrorSeverity INT = ERROR_SEVERITY();
        DECLARE @ErrorState INT = ERROR_STATE();

        RAISERROR(@ErrorMessage, @ErrorSeverity, @ErrorState);
    END CATCH
END;
GO

-- =====================================================
-- 示例数据 (可选)
-- =====================================================
-- 取消下面注释以插入示例数据用于测试


-- 插入示例设备
INSERT INTO dbo.devices (device_name, description, ip_address, port, username, password, status) VALUES
(N'门禁设备103', N'生产区域', N'192.168.1.103', N'8000', N'admin', N'SXSSF1314te', 1),
(N'门禁设备101', N'办公区域', N'192.168.1.101', N'8000', N'admin', N'sxs1314te', 1);

-- 插入示例员工(包含权限信息)
INSERT INTO dbo.employees (employee_id, card_number, full_name, status, permission_level) VALUES
(N'00000004', N'00000004', N'韩立', N'ACTIVE', 2);


-- =====================================================
-- 脚本执行完成
-- =====================================================
--
-- ✓ 数据库 'access_control_system' 已创建
--
-- 已创建核心表:
-- 1. devices (门禁设备表) - 存储设备配置和连接信息
-- 2. employees (员工信息表) - 存储员工基本信息、状态和权限信息
--
-- 已创建存储过程:
-- 1. delete_employee - 安全删除员工记录(带事务)
--
-- 已创建触发器:
-- 1. trg_devices_update - 自动更新 devices.updated_at
-- 2. trg_employees_update - 自动更新 employees.updated_at
--
-- 优化说明:
-- - 所有表字段与代码实际使用完全一致
-- - 已将 user_permissions 表合并到 employees 表中,简化查询
-- - 移除了未使用的access_logs表和device_users_backup表
-- - 字段注释清晰说明了用途和默认值
-- - 索引优化,提高查询性能
-- - 字符集使用 Chinese_PRC_CI_AS,支持中文排序
-- - 示例数据已更新为当前系统使用的默认值
-- - 使用 DATETIME2(0) 替代 DATETIME 以获得更好的精度和性能
-- - 使用触发器实现 updated_at 的自动更新功能
--
-- =====================================================
