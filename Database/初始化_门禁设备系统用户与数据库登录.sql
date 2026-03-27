-- 切换到 master 数据库，确保可以安全地管理目标数据库
USE master;
GO

IF EXISTS (SELECT name FROM sys.databases WHERE name = N'ruoyi-vue-pro')
BEGIN
    DECLARE @kill_sessions NVARCHAR(MAX) = N'';

    SELECT @kill_sessions = @kill_sessions + N'KILL ' + CAST(session_id AS NVARCHAR(10)) + N';'
    FROM sys.dm_exec_sessions
    WHERE database_id = DB_ID(N'ruoyi-vue-pro')
      AND session_id <> @@SPID;

    IF (@kill_sessions <> N'')
    BEGIN
        EXEC sp_executesql @kill_sessions;
    END

    ALTER DATABASE [ruoyi-vue-pro] SET SINGLE_USER WITH ROLLBACK IMMEDIATE;
    DROP DATABASE [ruoyi-vue-pro];
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

-- 创建数据库, 使用 UTF-8 排序规则
CREATE DATABASE [ruoyi-vue-pro]
COLLATE Chinese_PRC_CI_AS;
GO

USE [ruoyi-vue-pro];
GO

/* ========== 设备表 devices ========== */
IF OBJECT_ID(N'dbo.devices', N'U') IS NOT NULL
    DROP TABLE dbo.devices;
GO

CREATE TABLE dbo.devices (
    device_id        INT           NOT NULL,
    device_name      NVARCHAR(255) NOT NULL,
    description      NVARCHAR(255) NULL,
    ip_address       NVARCHAR(20)  NOT NULL,
    port             NVARCHAR(5)   NOT NULL DEFAULT N'8000',
    username         NVARCHAR(45)  NOT NULL DEFAULT N'admin',
    [password]       NVARCHAR(255) NOT NULL DEFAULT N'SXSSF1314te',
    status           TINYINT       NOT NULL DEFAULT 1,
    last_used_time   DATETIME2(0)  NULL,
    created_at       DATETIME2(0)  NOT NULL DEFAULT SYSDATETIME(),
    updated_at       DATETIME2(0)  NOT NULL DEFAULT SYSDATETIME(),
    CONSTRAINT PK_devices PRIMARY KEY (device_id)
);
GO

CREATE INDEX idx_ip_address ON dbo.devices(ip_address);
CREATE INDEX idx_status     ON dbo.devices(status);
GO

CREATE TRIGGER trg_devices_update
ON dbo.devices
AFTER UPDATE
AS
BEGIN
    SET NOCOUNT ON;

    UPDATE d
    SET updated_at = SYSDATETIME()
    FROM dbo.devices AS d
    INNER JOIN inserted AS i ON d.device_id = i.device_id;
END;
GO

EXEC sys.sp_addextendedproperty
    @name = N'MS_Description',
    @value = N'门禁设备信息表',
    @level0type = N'SCHEMA', @level0name = N'dbo',
    @level1type = N'TABLE',  @level1name = N'devices';
GO

/* ========== 初始化设备数据 ========== */
INSERT INTO dbo.devices (device_id, device_name, ip_address, port, username, [password], description, status, created_at, updated_at) VALUES

(10, N'物资仓库西门门禁进', N'10.98.26.50', N'8000', N'admin', N'xxzx@135', N'生产区域', 1, SYSDATETIME(), SYSDATETIME()),
(11, N'生产区门禁右进', N'10.98.26.56', N'8000', N'admin', N'xxzx@135', N'生产区域', 1, SYSDATETIME(), SYSDATETIME()),
(12, N'物资仓库西门门禁出', N'10.98.26.51', N'8000', N'admin', N'xxzx@135', N'生产区域', 1, SYSDATETIME(), SYSDATETIME()),
(13, N'煤炭路门禁进', N'10.98.26.52', N'8000', N'admin', N'xxzx@135', N'生产区域', 1, SYSDATETIME(), SYSDATETIME()),
(14, N'煤炭路门禁出', N'10.98.26.53', N'8000', N'admin', N'xxzx@135', N'生产区域', 1, SYSDATETIME(), SYSDATETIME()),
(15, N'生产区门禁左进', N'10.98.26.54', N'8000', N'admin', N'xxzx@135', N'生产区域', 1, SYSDATETIME(), SYSDATETIME()),
(16, N'生产区门禁左出', N'10.98.26.55', N'8000', N'admin', N'xxzx@135', N'生产区域', 1, SYSDATETIME(), SYSDATETIME()),
(17, N'生产区门禁右出', N'10.98.26.57', N'8000', N'admin', N'xxzx@135', N'生产区域', 1, SYSDATETIME(), SYSDATETIME()),
(18, N'生产区西门门禁左进', N'10.98.26.70', N'8000', N'admin', N'xxzx@135', N'生产区域', 1, SYSDATETIME(), SYSDATETIME()),
(19, N'生产区西门门禁右进', N'10.98.26.69', N'8000', N'admin', N'xxzx@135', N'生产区域', 1, SYSDATETIME(), SYSDATETIME()),
(20, N'生产区西门门禁左出', N'10.98.26.71', N'8000', N'admin', N'xxzx@135', N'生产区域', 1, SYSDATETIME(), SYSDATETIME()),
(21, N'生产区西门门禁右出', N'10.98.26.72', N'8000', N'admin', N'xxzx@135', N'生产区域', 1, SYSDATETIME(), SYSDATETIME());
GO

/* ========== 人员表 system_users ========== */
IF OBJECT_ID(N'dbo.system_users', N'U') IS NOT NULL
    DROP TABLE dbo.system_users;
GO

CREATE TABLE dbo.system_users (
    id                    BIGINT        IDENTITY(1,1) NOT NULL,
    username              NVARCHAR(30)  NOT NULL,
    [password]            NVARCHAR(100) NOT NULL DEFAULT N'',
    nickname              NVARCHAR(30)  NOT NULL,
    remark                NVARCHAR(500) NULL,
    dept_id               BIGINT        NULL,
    post_ids              NVARCHAR(255) NULL,
    email                 NVARCHAR(50)  NULL DEFAULT N'',
    mobile                NVARCHAR(11)  NULL DEFAULT N'',
    sex                   TINYINT       NULL DEFAULT 0,
    avatar                NVARCHAR(512) NULL DEFAULT N'',
    status                TINYINT       NOT NULL DEFAULT 0,
    access_permission     TINYINT       NOT NULL DEFAULT 2,
    last_synced_level     TINYINT       NULL,
    permission_updated_at DATETIME2(3)  NULL,
    last_synced_at        DATETIME2(3)  NULL,
    login_ip              NVARCHAR(50)  NULL DEFAULT N'',
    login_date            DATETIME2(3)  NULL,
    creator               NVARCHAR(64)  NULL DEFAULT N'',
    create_time           DATETIME2(3)  NOT NULL DEFAULT SYSDATETIME(),
    updater               NVARCHAR(64)  NULL DEFAULT N'',
    update_time           DATETIME2(3)  NOT NULL DEFAULT SYSDATETIME(),
    deleted               BIT           NOT NULL DEFAULT 0,
    tenant_id             BIGINT        NOT NULL DEFAULT 1,
    CONSTRAINT PK_system_users PRIMARY KEY CLUSTERED (id),
    CONSTRAINT UQ_system_users_username UNIQUE (username)
);
GO

CREATE INDEX idx_system_users_username   ON dbo.system_users(username);
CREATE INDEX idx_system_users_status     ON dbo.system_users(status);
CREATE INDEX idx_system_users_permission ON dbo.system_users(access_permission);
CREATE INDEX idx_system_users_deleted    ON dbo.system_users(deleted);
GO

CREATE TRIGGER trg_system_users_update
ON dbo.system_users
AFTER UPDATE
AS
BEGIN
    SET NOCOUNT ON;

    UPDATE su
    SET update_time = SYSDATETIME()
    FROM dbo.system_users AS su
    INNER JOIN inserted AS i ON su.id = i.id;
END;
GO

EXEC sys.sp_addextendedproperty
    @name = N'MS_Description',
    @value = N'统一账号体系人员信息表（含门禁扩展字段）',
    @level0type = N'SCHEMA', @level0name = N'dbo',
    @level1type = N'TABLE',  @level1name = N'system_users';
GO

-- =====================================================
-- 创建数据库级登录与用户
-- =====================================================
USE master;
GO

CREATE LOGIN admin_user
WITH PASSWORD = '123456',
     DEFAULT_DATABASE = [ruoyi-vue-pro],
     CHECK_EXPIRATION = OFF,
     CHECK_POLICY = OFF;
GO

CREATE LOGIN operator_user
WITH PASSWORD = 'Operator@123',
     DEFAULT_DATABASE = [ruoyi-vue-pro],
     CHECK_EXPIRATION = OFF,
     CHECK_POLICY = OFF;
GO

CREATE LOGIN monitor_user
WITH PASSWORD = 'Monitor@123',
     DEFAULT_DATABASE = [ruoyi-vue-pro],
     CHECK_EXPIRATION = OFF,
     CHECK_POLICY = OFF;
GO

CREATE LOGIN integration_user
WITH PASSWORD = 'Integration@123',
     DEFAULT_DATABASE = [ruoyi-vue-pro],
     CHECK_EXPIRATION = OFF,
     CHECK_POLICY = OFF;
GO

USE [ruoyi-vue-pro];
GO

CREATE USER admin_user FOR LOGIN admin_user;
GO
ALTER ROLE db_owner ADD MEMBER admin_user;
GO
EXEC sys.sp_addextendedproperty
    @name = N'MS_Description',
    @value = N'门禁管理系统管理员用户，拥有数据库内全部权限',
    @level0type = N'USER', @level0name = N'admin_user';
GO

CREATE USER operator_user FOR LOGIN operator_user;
GO
ALTER ROLE db_owner ADD MEMBER operator_user;
GO
EXEC sys.sp_addextendedproperty
    @name = N'MS_Description',
    @value = N'门禁系统运营用户，拥有数据库内全部权限',
    @level0type = N'USER', @level0name = N'operator_user';
GO

CREATE USER monitor_user FOR LOGIN monitor_user;
GO
ALTER ROLE db_owner ADD MEMBER monitor_user;
GO
EXEC sys.sp_addextendedproperty
    @name = N'MS_Description',
    @value = N'门禁系统运维用户，拥有数据库内全部权限',
    @level0type = N'USER', @level0name = N'monitor_user';
GO

CREATE USER integration_user FOR LOGIN integration_user;
GO
ALTER ROLE db_owner ADD MEMBER integration_user;
GO
EXEC sys.sp_addextendedproperty
    @name = N'MS_Description',
    @value = N'门禁系统集成用户，拥有数据库内全部权限',
    @level0type = N'USER', @level0name = N'integration_user';
GO

SELECT
    v.UserName             AS 用户名,
    v.RoleName             AS 角色,
    v.PermissionDescription AS 权限描述,
    v.InitialPassword      AS 初始密码
FROM (VALUES
    (N'admin_user',      N'db_owner', N'数据库内全部权限', N'123456'),
    (N'operator_user',   N'db_owner', N'数据库内全部权限', N'Operator@123'),
    (N'monitor_user',    N'db_owner', N'数据库内全部权限', N'Monitor@123'),
    (N'integration_user',N'db_owner', N'数据库内全部权限', N'Integration@123')
) AS v(UserName, RoleName, PermissionDescription, InitialPassword);
GO
