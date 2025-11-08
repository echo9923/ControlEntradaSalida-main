USE [ruoyi-vue-pro];
GO

/* ========== 设备表 devices ========== */
IF OBJECT_ID(''dbo.devices'', ''U'') IS NOT NULL
    DROP TABLE dbo.devices;
GO

CREATE TABLE dbo.devices (
    device_id        INT           NOT NULL,
    device_name      NVARCHAR(255) NOT NULL,
    description      NVARCHAR(255) NULL,
    ip_address       NVARCHAR(20)  NOT NULL,
    port             NVARCHAR(5)   NOT NULL DEFAULT ''8000'',
    username         NVARCHAR(45)  NOT NULL DEFAULT ''admin'',
    password         NVARCHAR(255) NOT NULL,
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
    UPDATE dbo.devices
    SET updated_at = SYSDATETIME()
    FROM dbo.devices d
    INNER JOIN inserted i ON d.device_id = i.device_id;
END;
GO

EXEC sys.sp_addextendedproperty
    @name = N''MS_Description'',
    @value = N''门禁设备信息表'',
    @level0type = N''SCHEMA'', @level0name = N''dbo'',
    @level1type = N''TABLE'',  @level1name = N''devices'';
GO

/* ========== 人员表 system_users ========== */
IF OBJECT_ID(N''dbo.system_users'', ''U'') IS NOT NULL
    DROP TABLE dbo.system_users;
GO

CREATE TABLE dbo.system_users (
    id                    BIGINT        IDENTITY(1,1) NOT NULL,
    username              NVARCHAR(30)  NOT NULL,
    [password]            NVARCHAR(100) NOT NULL DEFAULT N'''',
    nickname              NVARCHAR(30)  NOT NULL,
    remark                NVARCHAR(500) NULL,
    dept_id               BIGINT        NULL,
    post_ids              NVARCHAR(255) NULL,
    email                 NVARCHAR(50)  NULL DEFAULT N'''',
    mobile                NVARCHAR(11)  NULL DEFAULT N'''',
    sex                   TINYINT       NULL DEFAULT 0,
    avatar                NVARCHAR(512) NULL DEFAULT N'''',
    status                TINYINT       NOT NULL DEFAULT 0,
    access_permission     TINYINT       NOT NULL DEFAULT 2,
    last_synced_level     TINYINT       NULL,
    permission_updated_at DATETIME2(3)  NULL,
    last_synced_at        DATETIME2(3)  NULL,
    login_ip              NVARCHAR(50)  NULL DEFAULT N'''',
    login_date            DATETIME2(3)  NULL,
    creator               NVARCHAR(64)  NULL DEFAULT N'''',
    create_time           DATETIME2(3)  NOT NULL DEFAULT SYSDATETIME(),
    updater               NVARCHAR(64)  NULL DEFAULT N'''',
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
    UPDATE dbo.system_users
    SET update_time = SYSDATETIME()
    FROM dbo.system_users su
    INNER JOIN inserted i ON su.id = i.id;
END;
GO

EXEC sys.sp_addextendedproperty
    @name = N''MS_Description'',
    @value = N''统一账号体系人员信息表（含门禁扩展字段）'',
    @level0type = N''SCHEMA'', @level0name = N''dbo'',
    @level1type = N''TABLE'',  @level1name = N''system_users'';
GO
