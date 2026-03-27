-- 新增进出记录表（取消同一用户同一秒唯一限制，写入/查询平衡方案）
-- 如需重复执行，可先手动 DROP 该表后再执行本脚本。
IF OBJECT_ID(N'[dbo].[attendance_gate_v2]', N'U') IS NULL
BEGIN
    CREATE TABLE [dbo].[attendance_gate_v2] (
      [auto_id] bigint IDENTITY(1,1) NOT NULL,
      [id] bigint NOT NULL,
      [username] nvarchar(30) COLLATE Chinese_PRC_CI_AS NOT NULL,
      [nickname] nvarchar(50) COLLATE Chinese_PRC_CI_AS NULL,
      [record_datetime] datetime2(7) NOT NULL,
      [record_date] date NOT NULL,
      [record_time] time(7) NOT NULL,
      [direction] tinyint NOT NULL,
      [device_name] nvarchar(100) COLLATE Chinese_PRC_CI_AS NULL,
      [device_sn] nvarchar(100) COLLATE Chinese_PRC_CI_AS NULL,
      [card_no] nvarchar(64) COLLATE Chinese_PRC_CI_AS NULL,
      [snapshot_path] nvarchar(255) COLLATE Chinese_PRC_CI_AS NULL,
      [raw_payload] varchar(max) COLLATE Chinese_PRC_CI_AS NULL,
      [event_type] tinyint NULL,
      [process_status] tinyint NOT NULL,
      [process_message] nvarchar(255) COLLATE Chinese_PRC_CI_AS NULL,
      [processed_at] datetime2(7) NULL,
      [creator] nvarchar(64) COLLATE Chinese_PRC_CI_AS NULL,
      [create_time] datetime2(7) NOT NULL,
      [updater] nvarchar(64) COLLATE Chinese_PRC_CI_AS NULL,
      [update_time] datetime2(7) NOT NULL,
      [deleted] varchar(1) COLLATE Chinese_PRC_CI_AS NOT NULL,
      [tenant_id] bigint NOT NULL
    )
    ON [PRIMARY];

    ALTER TABLE [dbo].[attendance_gate_v2]
        ADD CONSTRAINT [PK_attendance_gate_v2] PRIMARY KEY CLUSTERED ([auto_id])
        WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON)
        ON [PRIMARY];

    CREATE UNIQUE NONCLUSTERED INDEX [ux_gate_v2_id]
    ON [dbo].[attendance_gate_v2] ([id] ASC)
    ON [PRIMARY];

    CREATE NONCLUSTERED INDEX [idx_gate_v2_user_time]
    ON [dbo].[attendance_gate_v2] ([username] ASC, [record_datetime] ASC)
    ON [PRIMARY];

    CREATE NONCLUSTERED INDEX [idx_gate_v2_status_date]
    ON [dbo].[attendance_gate_v2] ([process_status] ASC, [record_date] ASC)
    ON [PRIMARY];
END
GO
