-- Table structure for system_users
-- ----------------------------
IF OBJECT_ID(N'[dbo].[system_users]', 'U') IS NOT NULL DROP TABLE [dbo].[system_users];
CREATE TABLE [dbo].[system_users] (
    [id] BIGINT NOT NULL IDENTITY(1,1),
    [username] VARCHAR(30) NOT NULL,
    [password] VARCHAR(100) NOT NULL DEFAULT '',
    [nickname] VARCHAR(30) NOT NULL,
    [remark] VARCHAR(500) NULL,
    [dept_id] BIGINT NULL,
    [post_ids] VARCHAR(255) NULL,
    [email] VARCHAR(50) NULL DEFAULT '',
    [mobile] VARCHAR(11) NULL DEFAULT '',
    [sex] TINYINT NULL DEFAULT 0,
    [avatar] VARCHAR(512) NULL DEFAULT '',
    [status] TINYINT NOT NULL DEFAULT 0,
    [access_permission] TINYINT NOT NULL DEFAULT 2,
    [login_ip] VARCHAR(50) NULL DEFAULT '',
    [login_date] DATETIME2(3) NULL,
    [creator] VARCHAR(64) NULL DEFAULT '',
    [create_time] DATETIME2(3) NOT NULL DEFAULT SYSDATETIME(),
    [updater] VARCHAR(64) NULL DEFAULT '',
    [update_time] DATETIME2(3) NOT NULL DEFAULT SYSDATETIME(),
    [deleted] BIT NOT NULL DEFAULT 0,
    [tenant_id] BIGINT NOT NULL DEFAULT 0,
    CONSTRAINT [PK_system_users] PRIMARY KEY CLUSTERED ([id])
);