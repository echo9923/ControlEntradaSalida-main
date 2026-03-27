-- 人脸事件补偿检查点表结构（SQL Server）
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
