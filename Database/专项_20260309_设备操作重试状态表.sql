-- 设备操作重试状态表专项脚本
-- 本脚本对应历史全量建库脚本末尾新增的设备补偿重试能力，建议后续仅维护本文件，不再在废弃脚本中重复追加。
-- 用途：记录某个员工在某台设备上的权限、人员、人脸下发与删除操作的待重试状态。

IF OBJECT_ID(N'[dbo].[device_operation_retry_states]', N'U') IS NULL
BEGIN
    CREATE TABLE [dbo].[device_operation_retry_states]
    (
        [id] BIGINT IDENTITY(1,1) NOT NULL,
        [device_id] INT NOT NULL,
        [employee_id] NVARCHAR(64) NOT NULL,
        [permission_level] INT NULL,
        [permission_pending] BIT NOT NULL CONSTRAINT [DF_device_operation_retry_states_permission_pending] DEFAULT ((0)),
        [permission_sync_completion_blocked] BIT NOT NULL CONSTRAINT [DF_device_operation_retry_states_permission_sync_completion_blocked] DEFAULT ((1)),
        [person_payload] NVARCHAR(MAX) NULL,
        [person_pending] BIT NOT NULL CONSTRAINT [DF_device_operation_retry_states_person_pending] DEFAULT ((0)),
        [face_payload] NVARCHAR(MAX) NULL,
        [face_pending] BIT NOT NULL CONSTRAINT [DF_device_operation_retry_states_face_pending] DEFAULT ((0)),
        [delete_person_pending] BIT NOT NULL CONSTRAINT [DF_device_operation_retry_states_delete_person_pending] DEFAULT ((0)),
        [delete_face_pending] BIT NOT NULL CONSTRAINT [DF_device_operation_retry_states_delete_face_pending] DEFAULT ((0)),
        [attempt_count] INT NOT NULL CONSTRAINT [DF_device_operation_retry_states_attempt_count] DEFAULT ((0)),
        [next_retry_at] DATETIME2(0) NULL,
        [last_error] NVARCHAR(2000) NULL,
        [last_attempt_at] DATETIME2(0) NULL,
        [exhausted_at] DATETIME2(0) NULL,
        [created_at] DATETIME2(0) NOT NULL CONSTRAINT [DF_device_operation_retry_states_created_at] DEFAULT (SYSDATETIME()),
        [updated_at] DATETIME2(0) NOT NULL CONSTRAINT [DF_device_operation_retry_states_updated_at] DEFAULT (SYSDATETIME()),
        CONSTRAINT [PK_device_operation_retry_states] PRIMARY KEY CLUSTERED ([id] ASC),
        CONSTRAINT [UQ_device_operation_retry_states_device_employee] UNIQUE NONCLUSTERED ([device_id] ASC, [employee_id] ASC)
    );
END
GO

IF COL_LENGTH(N'dbo.device_operation_retry_states', N'permission_sync_completion_blocked') IS NULL
BEGIN
    ALTER TABLE [dbo].[device_operation_retry_states]
        ADD [permission_sync_completion_blocked] BIT NOT NULL
            CONSTRAINT [DF_device_operation_retry_states_permission_sync_completion_blocked] DEFAULT ((1));
END
GO

IF NOT EXISTS (
    SELECT 1
    FROM sys.indexes
    WHERE name = N'IX_device_operation_retry_states_next_retry_at'
      AND object_id = OBJECT_ID(N'[dbo].[device_operation_retry_states]'))
BEGIN
    CREATE NONCLUSTERED INDEX [IX_device_operation_retry_states_next_retry_at]
        ON [dbo].[device_operation_retry_states] ([next_retry_at] ASC)
        INCLUDE ([device_id], [employee_id], [updated_at])
        WHERE [exhausted_at] IS NULL;
END
GO

IF NOT EXISTS (
    SELECT 1
    FROM sys.indexes
    WHERE name = N'IX_device_operation_retry_states_employee_permission'
      AND object_id = OBJECT_ID(N'[dbo].[device_operation_retry_states]'))
BEGIN
    CREATE NONCLUSTERED INDEX [IX_device_operation_retry_states_employee_permission]
        ON [dbo].[device_operation_retry_states] ([employee_id] ASC, [permission_pending] ASC)
        INCLUDE ([device_id], [updated_at]);
END
GO

IF NOT EXISTS (
    SELECT 1
    FROM sys.indexes
    WHERE name = N'IX_device_operation_retry_states_exhausted_at'
      AND object_id = OBJECT_ID(N'[dbo].[device_operation_retry_states]'))
BEGIN
    CREATE NONCLUSTERED INDEX [IX_device_operation_retry_states_exhausted_at]
        ON [dbo].[device_operation_retry_states] ([exhausted_at] ASC)
        INCLUDE ([device_id], [employee_id], [updated_at]);
END
GO
