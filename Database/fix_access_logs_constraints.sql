-- 修复access_logs表的外键约束问题
-- 允许设备事件（门锁操作等）不需要关联员工信息

USE access_control_system;

-- 先删除外键约束
ALTER TABLE access_logs DROP FOREIGN KEY fk_access_logs_employee;

-- 修改employee_number字段允许NULL
ALTER TABLE access_logs MODIFY COLUMN employee_number VARCHAR(30) NULL;

-- 修改employee_name字段允许NULL，更符合实际情况
ALTER TABLE access_logs MODIFY COLUMN employee_name VARCHAR(255) NULL;

-- 删除原有的唯一约束（因为它包含employee_number）
ALTER TABLE access_logs DROP INDEX uk_event_uniqueness;

-- 重新创建更合适的唯一约束（排除NULL的employee_number）
-- 对于设备事件，使用device_number + event_time + event_type来避免重复
-- 对于人员事件，使用employee_number + device_number + event_time + event_type来避免重复
ALTER TABLE access_logs ADD CONSTRAINT uk_device_event_uniqueness
    UNIQUE (device_number, event_time, event_type, employee_number);

-- 重新添加外键约束，但允许NULL值
ALTER TABLE access_logs ADD CONSTRAINT fk_access_logs_employee
    FOREIGN KEY (employee_number)
    REFERENCES employees(employee_id)
    ON DELETE SET NULL
    ON UPDATE CASCADE;

-- 添加注释说明
ALTER TABLE access_logs COMMENT = '门禁访问日志表 - 支持人员事件和设备事件';
ALTER TABLE access_logs MODIFY COLUMN employee_number VARCHAR(30) NULL COMMENT '员工编号（设备事件时为NULL）';
ALTER TABLE access_logs MODIFY COLUMN employee_name VARCHAR(255) NULL COMMENT '员工姓名（设备事件时为空或NULL）';