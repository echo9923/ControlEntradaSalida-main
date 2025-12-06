-- 人脸事件与补偿检查点表结构（SQL Server）
IF OBJECT_ID(N'dbo.face_event_log', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.face_event_log
    (
        Id            BIGINT IDENTITY(1,1) PRIMARY KEY,
        EventType     TINYINT       NOT NULL,          -- 0=失败，1=通过
        EventTime     DATETIME2(3)  NOT NULL,
        UserId        NVARCHAR(64)  NULL,
        CardNo        NVARCHAR(32)  NULL,
        DeviceName    NVARCHAR(128) NULL,
        DeviceIP      NVARCHAR(45)  NOT NULL,
        VerifyMode    NVARCHAR(32)  NULL,
        SerialNo      BIGINT        NOT NULL,
        Snapshot      VARBINARY(MAX) NULL,
        CreatedAt     DATETIME2(3)  NOT NULL CONSTRAINT DF_face_event_log_CreatedAt DEFAULT (SYSUTCDATETIME())
    );

    CREATE UNIQUE INDEX UX_face_event_log_device_serial ON dbo.face_event_log(DeviceIP, SerialNo);
    CREATE INDEX IX_face_event_log_time ON dbo.face_event_log(DeviceName, EventTime);
    CREATE INDEX IX_face_event_log_user ON dbo.face_event_log(EventType, UserId);
END;
GO

IF OBJECT_ID(N'dbo.face_event_checkpoint', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.face_event_checkpoint
    (
        DeviceIP      NVARCHAR(45)  NOT NULL PRIMARY KEY,
        LastSerialNo  BIGINT        NOT NULL,
        LastEventTime DATETIME2(3)  NOT NULL,
        UpdatedAt     DATETIME2(3)  NOT NULL CONSTRAINT DF_face_event_checkpoint_UpdatedAt DEFAULT (SYSUTCDATETIME())
    );
END;
GO
