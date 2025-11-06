-- 切换到 master 数据库，确保可以安全地管理目标数据库
USE master;
GO

IF EXISTS (SELECT name FROM sys.databases WHERE name = N'access_control_system')
BEGIN
    DECLARE @kill_sessions NVARCHAR(MAX) = N'';

    SELECT @kill_sessions = @kill_sessions + N'KILL ' + CAST(session_id AS NVARCHAR(10)) + N';'
    FROM sys.dm_exec_sessions
    WHERE database_id = DB_ID(N'access_control_system')
      AND session_id <> @@SPID;

    IF (@kill_sessions <> N'')
    BEGIN
        EXEC sp_executesql @kill_sessions;
    END

    ALTER DATABASE access_control_system SET SINGLE_USER WITH ROLLBACK IMMEDIATE;
    DROP DATABASE access_control_system;
END
GO

-- 清理旧登录，确保后续使用全新的凭据
IF EXISTS (SELECT * FROM sys.server_principals WHERE name = N'admin_user')
    DROP LOGIN admin_user;
GO
IF EXISTS (SELECT * FROM sys.server_principals WHERE name = N'operator_user')
    DROP LOGIN operator_user;
GO
IF EXISTS (SELECT * FROM sys.server_principals WHERE name = N'monitor_user')
    DROP LOGIN monitor_user;
GO
IF EXISTS (SELECT * FROM sys.server_principals WHERE name = N'integration_user')
    DROP LOGIN integration_user;
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
    -- 设备唯一标识(主键,可手动编辑)
    device_id INT NOT NULL,

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
-- 表: system_users (人员信息表，与统一账号体系对接)
-- =====================================================
-- 用途: 统一存储账号体系中的人员主数据，username 字段保存实际工号，并承载门禁权限字段
-- 使用位置:
--   - PermissionRefreshManager.cs (权限同步与更新)
--   - PermissionUpdateGrpcServer.cs (外部权限推送)
-- =====================================================
IF OBJECT_ID(N'dbo.system_users', 'U') IS NOT NULL
    DROP TABLE dbo.system_users;
GO

CREATE TABLE dbo.system_users (
    -- 主键，自增保持与统一账号体系一致
    id BIGINT IDENTITY(1,1) NOT NULL,

    -- 工号/登录账号（唯一），对应旧版 employee_id
    username NVARCHAR(30) NOT NULL,

    -- 登录密码（密文）
    [password] NVARCHAR(100) NOT NULL DEFAULT N'',

    -- 中文名或显示名
    nickname NVARCHAR(30) NOT NULL,

    -- 备注
    remark NVARCHAR(500) NULL,

    -- 组织维度
    dept_id BIGINT NULL,
    post_ids NVARCHAR(255) NULL,

    -- 联系方式
    email NVARCHAR(50) NULL DEFAULT N'',
    mobile NVARCHAR(11) NULL DEFAULT N'',

    -- 基础属性
    sex TINYINT NULL DEFAULT 0,
    avatar NVARCHAR(512) NULL DEFAULT N'',

    -- 账号状态：0=启用 1=停用
    status TINYINT NOT NULL DEFAULT 0,

    -- 门禁权限级别：0-2
    access_permission TINYINT NOT NULL DEFAULT 2,

    -- 最近一次同步到设备的权限级别
    last_synced_level TINYINT NULL,

    -- 权限级别最后更新时间
    permission_updated_at DATETIME2(3) NULL,

    -- 权限最近一次同步到设备的时间
    last_synced_at DATETIME2(3) NULL,

    -- 登录信息
    login_ip NVARCHAR(50) NULL DEFAULT N'',
    login_date DATETIME2(3) NULL,

    -- 创建及更新审计字段
    creator NVARCHAR(64) NULL DEFAULT N'',
    create_time DATETIME2(3) NOT NULL DEFAULT SYSDATETIME(),
    updater NVARCHAR(64) NULL DEFAULT N'',
    update_time DATETIME2(3) NOT NULL DEFAULT SYSDATETIME(),

    -- 软删除与租户
    deleted BIT NOT NULL DEFAULT 0,
    tenant_id BIGINT NOT NULL DEFAULT 0,

    CONSTRAINT PK_system_users PRIMARY KEY CLUSTERED (id),
    CONSTRAINT UQ_system_users_username UNIQUE (username)
);
GO

-- 创建索引
CREATE INDEX idx_system_users_username ON dbo.system_users(username);           -- 工号快速定位
CREATE INDEX idx_system_users_status ON dbo.system_users(status);               -- 启停状态筛选
CREATE INDEX idx_system_users_permission ON dbo.system_users(access_permission);-- 权限级别筛选
CREATE INDEX idx_system_users_deleted ON dbo.system_users(deleted);             -- 软删除筛选
GO

-- 创建触发器以实现 update_time 自动更新
CREATE TRIGGER trg_system_users_update
ON dbo.system_users
AFTER UPDATE
AS
BEGIN
    SET NOCOUNT ON;
    UPDATE dbo.system_users
    SET update_time = SYSDATETIME()
    FROM dbo.system_users su
    INNER JOIN inserted i ON su.id = i.id;
END;
GO

-- 添加表注释
EXEC sys.sp_addextendedproperty
    @name = N'MS_Description',
    @value = N'统一账号体系人员主数据表（username 即工号），包含门禁权限扩展字段',
    @level0type = N'SCHEMA', @level0name = N'dbo',
    @level1type = N'TABLE', @level1name = N'system_users';
GO

-- =====================================================
-- 存储过程: delete_employee
-- =====================================================
-- 用途: 删除人员记录（以工号为唯一标识），保持与统一账号体系一致
-- 使用位置: 历史清理逻辑兼容
-- =====================================================
IF OBJECT_ID('dbo.delete_employee', 'P') IS NOT NULL
    DROP PROCEDURE dbo.delete_employee;
GO

CREATE PROCEDURE dbo.delete_employee
    @username NVARCHAR(30)
AS
BEGIN
    SET NOCOUNT ON;

    BEGIN TRY
        BEGIN TRANSACTION;

        -- 优先执行逻辑删除，保留审计字段
        UPDATE dbo.system_users
        SET deleted = 1,
            update_time = SYSDATETIME()
        WHERE username = @username;

        IF @@ROWCOUNT = 0
        BEGIN
            -- 若不存在记录则直接物理删除兜底
            DELETE FROM dbo.system_users WHERE username = @username;
        END;

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
INSERT INTO dbo.devices (device_id, device_name, description, ip_address, port, username, password, status) VALUES
(101, N'门禁设备101', N'办公区域', N'192.168.1.101', N'8000', N'admin', N'SXSSF1314te', 1),
(103, N'门禁设备103', N'生产区域', N'192.168.1.103', N'8000', N'admin', N'SXSSF1314te', 1);

-- 插入示例人员(包含权限信息)
INSERT INTO dbo.system_users (username, nickname, status, access_permission, deleted)
VALUES (N'00000004', N'韩立', 0, 2, 0);


-- =====================================================
-- 服务器登录与数据库用户
-- =====================================================

USE master;
GO

CREATE LOGIN admin_user 
WITH PASSWORD = '123456',
     DEFAULT_DATABASE = [access_control_system],
     CHECK_EXPIRATION = OFF,
     CHECK_POLICY = OFF;
GO

CREATE LOGIN operator_user 
WITH PASSWORD = 'Operator@123',
     DEFAULT_DATABASE = [access_control_system],
     CHECK_EXPIRATION = OFF,
     CHECK_POLICY = OFF;
GO

CREATE LOGIN monitor_user 
WITH PASSWORD = 'Monitor@123',
     DEFAULT_DATABASE = [access_control_system],
     CHECK_EXPIRATION = OFF,
     CHECK_POLICY = OFF;
GO

CREATE LOGIN integration_user 
WITH PASSWORD = 'Integration@123',
     DEFAULT_DATABASE = [access_control_system],
     CHECK_EXPIRATION = OFF,
     CHECK_POLICY = OFF;
GO

USE access_control_system;
GO

CREATE USER admin_user FOR LOGIN admin_user;
GO
ALTER ROLE db_owner ADD MEMBER admin_user;
GO
EXEC sys.sp_addextendedproperty
    @name = N'MS_Description',
    @value = N'门禁管理系统管理员用户，具有数据库完全控制权限',
    @level0type = N'USER', @level0name = N'admin_user';
GO

CREATE USER operator_user FOR LOGIN operator_user;
GO
ALTER ROLE db_owner ADD MEMBER operator_user;
GO
EXEC sys.sp_addextendedproperty
    @name = N'MS_Description',
    @value = N'门禁系统运营用户，具有数据库完全控制权限',
    @level0type = N'USER', @level0name = N'operator_user';
GO

CREATE USER monitor_user FOR LOGIN monitor_user;
GO
ALTER ROLE db_owner ADD MEMBER monitor_user;
GO
EXEC sys.sp_addextendedproperty
    @name = N'MS_Description',
    @value = N'门禁系统监控用户，具有数据库完全控制权限',
    @level0type = N'USER', @level0name = N'monitor_user';
GO

CREATE USER integration_user FOR LOGIN integration_user;
GO
ALTER ROLE db_owner ADD MEMBER integration_user;
GO
EXEC sys.sp_addextendedproperty
    @name = N'MS_Description',
    @value = N'门禁系统集成用户，具有数据库完全控制权限',
    @level0type = N'USER', @level0name = N'integration_user';
GO

SELECT 
    v.UserName AS 用户名,
    v.RoleName AS 角色,
    v.PermissionDescription AS 权限描述,
    v.InitialPassword AS 初始密码
FROM (VALUES
    (N'admin_user', N'db_owner', N'数据库完全控制权限', N'123456'),
    (N'operator_user', N'db_owner', N'数据库完全控制权限', N'Operator@123'),
    (N'monitor_user', N'db_owner', N'数据库完全控制权限', N'Monitor@123'),
    (N'integration_user', N'db_owner', N'数据库完全控制权限', N'Integration@123')
) AS v(UserName, RoleName, PermissionDescription, InitialPassword);
GO
