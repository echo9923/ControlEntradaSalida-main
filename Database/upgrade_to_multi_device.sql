-- =============================================
-- 门禁控制系统多设备连接数据库升级脚本
-- 版本: 1.0.0
-- 创建时间: 2025-08-15
-- 功能: 为dispositivos表添加连接状态管理相关字段
-- =============================================

-- 开始事务
START TRANSACTION;

-- 设置字符集
SET NAMES utf8;
SET FOREIGN_KEY_CHECKS = 0;

-- =============================================
-- 1. 升级dispositivos表
-- =============================================

-- 添加连接状态字段
ALTER TABLE `dispositivos` 
ADD COLUMN `connection_status` VARCHAR(20) NOT NULL DEFAULT 'disconnected' COMMENT '连接状态(disconnected,connecting,connected,reconnecting,error)' AFTER `contrasena`;

-- 添加最后连接时间字段
ALTER TABLE `dispositivos` 
ADD COLUMN `last_connection_time` DATETIME NULL DEFAULT NULL COMMENT '最后连接时间' AFTER `connection_status`;

-- 添加最后心跳时间字段
ALTER TABLE `dispositivos` 
ADD COLUMN `last_heartbeat_time` DATETIME NULL DEFAULT NULL COMMENT '最后心跳时间' AFTER `last_connection_time`;

-- 添加自动重连字段
ALTER TABLE `dispositivos` 
ADD COLUMN `auto_connect` BOOLEAN NOT NULL DEFAULT TRUE COMMENT '是否启用自动重连' AFTER `last_heartbeat_time`;

-- 添加连接重试次数字段
ALTER TABLE `dispositivos` 
ADD COLUMN `connection_retries` INT NOT NULL DEFAULT 0 COMMENT '连接重试次数' AFTER `auto_connect`;

-- 添加连接错误信息字段
ALTER TABLE `dispositivos` 
ADD COLUMN `error_message` TEXT NULL DEFAULT NULL COMMENT '连接错误信息' AFTER `connection_retries`;

-- 添加SDK用户ID字段
ALTER TABLE `dispositivos` 
ADD COLUMN `sdk_user_id` INT NOT NULL DEFAULT -1 COMMENT 'SDK用户ID' AFTER `error_message`;

-- 添加连接持续时间字段（秒）
ALTER TABLE `dispositivos` 
ADD COLUMN `connection_duration` INT NOT NULL DEFAULT 0 COMMENT '连接持续时间（秒）' AFTER `sdk_user_id`;

-- 添加设备在线状态字段
ALTER TABLE `dispositivos` 
ADD COLUMN `is_online` BOOLEAN NOT NULL DEFAULT FALSE COMMENT '设备是否在线' AFTER `connection_duration`;

-- 添加设备健康状态字段
ALTER TABLE `dispositivos` 
ADD COLUMN `health_status` VARCHAR(20) NOT NULL DEFAULT 'unknown' COMMENT '设备健康状态(unknown,healthy,unhealthy)' AFTER `is_online`;

-- =============================================
-- 2. 创建设备连接日志表
-- =============================================

CREATE TABLE IF NOT EXISTS `device_connection_logs` (
  `id` INT NOT NULL AUTO_INCREMENT,
  `device_id` INT NOT NULL,
  `connection_status` VARCHAR(20) NOT NULL COMMENT '连接状态',
  `error_message` TEXT NULL DEFAULT NULL COMMENT '错误信息',
  `connection_time` DATETIME NOT NULL COMMENT '连接时间',
  `disconnection_time` DATETIME NULL DEFAULT NULL COMMENT '断开时间',
  `duration_seconds` INT NULL DEFAULT NULL COMMENT '连接持续时间（秒）',
  `retry_count` INT NOT NULL DEFAULT 0 COMMENT '重试次数',
  `created` DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP COMMENT '记录创建时间',
  PRIMARY KEY (`id`),
  INDEX `idx_device_id` (`device_id` ASC),
  INDEX `idx_connection_time` (`connection_time` ASC),
  INDEX `idx_connection_status` (`connection_status` ASC),
  CONSTRAINT `fk_device_connection_logs_device`
    FOREIGN KEY (`device_id`)
    REFERENCES `dispositivos` (`id`)
    ON DELETE CASCADE
    ON UPDATE CASCADE
) ENGINE = InnoDB DEFAULT CHARACTER SET = utf8 COMMENT = '设备连接日志表';

-- =============================================
-- 3. 创建设备心跳日志表
-- =============================================

CREATE TABLE IF NOT EXISTS `device_heartbeat_logs` (
  `id` INT NOT NULL AUTO_INCREMENT,
  `device_id` INT NOT NULL,
  `heartbeat_time` DATETIME NOT NULL COMMENT '心跳时间',
  `response_time_ms` INT NULL DEFAULT NULL COMMENT '响应时间（毫秒）',
  `status` VARCHAR(20) NOT NULL DEFAULT 'success' COMMENT '心跳状态(success,failed,timeout)',
  `error_message` TEXT NULL DEFAULT NULL COMMENT '错误信息',
  `created` DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP COMMENT '记录创建时间',
  PRIMARY KEY (`id`),
  INDEX `idx_device_id` (`device_id` ASC),
  INDEX `idx_heartbeat_time` (`heartbeat_time` ASC),
  INDEX `idx_status` (`status` ASC),
  CONSTRAINT `fk_device_heartbeat_logs_device`
    FOREIGN KEY (`device_id`)
    REFERENCES `dispositivos` (`id`)
    ON DELETE CASCADE
    ON UPDATE CASCADE
) ENGINE = InnoDB DEFAULT CHARACTER SET = utf8 COMMENT = '设备心跳日志表';

-- =============================================
-- 4. 创建设备网络检测日志表
-- =============================================

CREATE TABLE IF NOT EXISTS `device_network_logs` (
  `id` INT NOT NULL AUTO_INCREMENT,
  `device_id` INT NOT NULL,
  `check_time` DATETIME NOT NULL COMMENT '检测时间',
  `ping_success` BOOLEAN NOT NULL DEFAULT FALSE COMMENT 'Ping检测是否成功',
  `ping_response_time_ms` INT NULL DEFAULT NULL COMMENT 'Ping响应时间（毫秒）',
  `port_success` BOOLEAN NOT NULL DEFAULT FALSE COMMENT '端口检测是否成功',
  `port_response_time_ms` INT NULL DEFAULT NULL COMMENT '端口响应时间（毫秒）',
  `network_status` VARCHAR(20) NOT NULL DEFAULT 'unknown' COMMENT '网络状态(unknown,online,offline)',
  `error_message` TEXT NULL DEFAULT NULL COMMENT '错误信息',
  `created` DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP COMMENT '记录创建时间',
  PRIMARY KEY (`id`),
  INDEX `idx_device_id` (`device_id` ASC),
  INDEX `idx_check_time` (`check_time` ASC),
  INDEX `idx_network_status` (`network_status` ASC),
  CONSTRAINT `fk_device_network_logs_device`
    FOREIGN KEY (`device_id`)
    REFERENCES `dispositivos` (`id`)
    ON DELETE CASCADE
    ON UPDATE CASCADE
) ENGINE = InnoDB DEFAULT CHARACTER SET = utf8 COMMENT = '设备网络检测日志表';

-- =============================================
-- 5. 创建设备配置表
-- =============================================

CREATE TABLE IF NOT EXISTS `device_configurations` (
  `id` INT NOT NULL AUTO_INCREMENT,
  `device_id` INT NOT NULL,
  `config_key` VARCHAR(100) NOT NULL COMMENT '配置键',
  `config_value` TEXT NOT NULL COMMENT '配置值',
  `config_type` VARCHAR(20) NOT NULL DEFAULT 'string' COMMENT '配置类型(string,int,bool,datetime)',
  `description` VARCHAR(255) NULL DEFAULT NULL COMMENT '配置描述',
  `created` DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP COMMENT '创建时间',
  `modified` DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP COMMENT '修改时间',
  PRIMARY KEY (`id`),
  UNIQUE INDEX `idx_device_config` (`device_id` ASC, `config_key` ASC),
  INDEX `idx_config_key` (`config_key` ASC),
  CONSTRAINT `fk_device_configurations_device`
    FOREIGN KEY (`device_id`)
    REFERENCES `dispositivos` (`id`)
    ON DELETE CASCADE
    ON UPDATE CASCADE
) ENGINE = InnoDB DEFAULT CHARACTER SET = utf8 COMMENT = '设备配置表';

-- =============================================
-- 6. 创建系统配置表
-- =============================================

CREATE TABLE IF NOT EXISTS `system_configurations` (
  `id` INT NOT NULL AUTO_INCREMENT,
  `config_key` VARCHAR(100) NOT NULL COMMENT '配置键',
  `config_value` TEXT NOT NULL COMMENT '配置值',
  `config_type` VARCHAR(20) NOT NULL DEFAULT 'string' COMMENT '配置类型(string,int,bool,datetime)',
  `description` VARCHAR(255) NULL DEFAULT NULL COMMENT '配置描述',
  `category` VARCHAR(50) NOT NULL DEFAULT 'general' COMMENT '配置类别',
  `created` DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP COMMENT '创建时间',
  `modified` DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP COMMENT '修改时间',
  PRIMARY KEY (`id`),
  UNIQUE INDEX `idx_config_key` (`config_key` ASC),
  INDEX `idx_category` (`category` ASC)
) ENGINE = InnoDB DEFAULT CHARACTER SET = utf8 COMMENT = '系统配置表';

-- =============================================
-- 7. 插入默认系统配置
-- =============================================

-- 插入多设备连接相关配置
INSERT INTO `system_configurations` (`config_key`, `config_value`, `config_type`, `description`, `category`) VALUES
('max_connections', '10', 'int', '最大设备连接数', 'connection'),
('heartbeat_interval', '30', 'int', '心跳检测间隔（秒）', 'connection'),
('retry_interval', '60', 'int', '重连间隔（秒）', 'connection'),
('max_retry_count', '5', 'int', '最大重连次数', 'connection'),
('network_timeout', '3000', 'int', '网络超时时间（毫秒）', 'connection'),
('auto_cleanup_interval', '300', 'int', '自动清理间隔（秒）', 'connection'),
('enable_heartbeat', 'true', 'bool', '是否启用心跳检测', 'connection'),
('enable_auto_reconnect', 'true', 'bool', '是否启用自动重连', 'connection'),
('log_retention_days', '30', 'int', '日志保留天数', 'system'),
('device_status_update_interval', '5', 'int', '设备状态更新间隔（秒）', 'ui');

-- =============================================
-- 8. 创建视图
-- =============================================

-- 设备连接状态视图
CREATE OR REPLACE VIEW `v_device_connection_status` AS
SELECT 
    d.id,
    d.nombre,
    d.direccionip,
    d.puerto,
    d.connection_status,
    d.last_connection_time,
    d.last_heartbeat_time,
    d.auto_connect,
    d.connection_retries,
    d.error_message,
    d.is_online,
    d.health_status,
    d.connection_duration,
    CASE 
        WHEN d.connection_status = 'connected' THEN '已连接'
        WHEN d.connection_status = 'connecting' THEN '连接中'
        WHEN d.connection_status = 'reconnecting' THEN '重连中'
        WHEN d.connection_status = 'error' THEN '连接错误'
        ELSE '未连接'
    END as connection_status_text,
    CASE 
        WHEN d.health_status = 'healthy' THEN '健康'
        WHEN d.health_status = 'unhealthy' THEN '异常'
        ELSE '未知'
    END as health_status_text,
    CASE 
        WHEN d.is_online = 1 THEN '在线'
        ELSE '离线'
    END as online_status_text
FROM dispositivos d;

-- 设备统计视图
CREATE OR REPLACE VIEW `v_device_statistics` AS
SELECT 
    COUNT(*) as total_devices,
    SUM(CASE WHEN connection_status = 'connected' THEN 1 ELSE 0 END) as connected_devices,
    SUM(CASE WHEN connection_status = 'connecting' THEN 1 ELSE 0 END) as connecting_devices,
    SUM(CASE WHEN connection_status = 'reconnecting' THEN 1 ELSE 0 END) as reconnecting_devices,
    SUM(CASE WHEN connection_status = 'error' THEN 1 ELSE 0 END) as error_devices,
    SUM(CASE WHEN connection_status = 'disconnected' THEN 1 ELSE 0 END) as disconnected_devices,
    SUM(CASE WHEN is_online = 1 THEN 1 ELSE 0 END) as online_devices,
    SUM(CASE WHEN health_status = 'healthy' THEN 1 ELSE 0 END) as healthy_devices,
    AVG(connection_retries) as avg_retry_count,
    MAX(connection_duration) as max_connection_duration
FROM dispositivos;

-- =============================================
-- 9. 创建存储过程
-- =============================================

-- 更新设备连接状态
DELIMITER //
CREATE PROCEDURE `UpdateDeviceConnectionStatus`(
    IN p_device_id INT,
    IN p_connection_status VARCHAR(20),
    IN p_error_message TEXT,
    IN p_sdk_user_id INT
)
BEGIN
    DECLARE v_current_status VARCHAR(20);
    
    -- 获取当前状态
    SELECT connection_status INTO v_current_status 
    FROM dispositivos WHERE id = p_device_id;
    
    -- 更新设备状态
    UPDATE dispositivos 
    SET 
        connection_status = p_connection_status,
        error_message = p_error_message,
        sdk_user_id = p_sdk_user_id,
        connection_retries = IF(p_connection_status = 'connected', 0, connection_retries + 1),
        modified = NOW()
    WHERE id = p_device_id;
    
    -- 如果状态变为已连接，更新连接时间
    IF p_connection_status = 'connected' AND v_current_status != 'connected' THEN
        UPDATE dispositivos 
        SET 
            last_connection_time = NOW(),
            last_heartbeat_time = NOW()
        WHERE id = p_device_id;
    END IF;
    
    -- 记录连接日志
    IF p_connection_status = 'connected' THEN
        INSERT INTO device_connection_logs (
            device_id, connection_status, connection_time, retry_count
        ) VALUES (
            p_device_id, p_connection_status, NOW(), 
            (SELECT connection_retries FROM dispositivos WHERE id = p_device_id)
        );
    END IF;
END //
DELIMITER ;

-- 更新设备心跳时间
DELIMITER //
CREATE PROCEDURE `UpdateDeviceHeartbeat`(
    IN p_device_id INT,
    IN p_response_time_ms INT,
    IN p_status VARCHAR(20),
    IN p_error_message TEXT
)
BEGIN
    -- 更新心跳时间
    UPDATE dispositivos 
    SET 
        last_heartbeat_time = NOW(),
        health_status = IF(p_status = 'success', 'healthy', 'unhealthy'),
        is_online = IF(p_status = 'success', TRUE, FALSE),
        connection_duration = TIMESTAMPDIFF(SECOND, last_connection_time, NOW()),
        modified = NOW()
    WHERE id = p_device_id;
    
    -- 记录心跳日志
    INSERT INTO device_heartbeat_logs (
        device_id, heartbeat_time, response_time_ms, status, error_message
    ) VALUES (
        p_device_id, NOW(), p_response_time_ms, p_status, p_error_message
    );
END //
DELIMITER ;

-- 清理过期日志
DELIMITER //
CREATE PROCEDURE `CleanupExpiredLogs`(
    IN p_retention_days INT
)
BEGIN
    DECLARE cutoff_date DATETIME;
    
    SET cutoff_date = DATE_SUB(NOW(), INTERVAL p_retention_days DAY);
    
    -- 清理连接日志
    DELETE FROM device_connection_logs 
    WHERE created < cutoff_date;
    
    -- 清理心跳日志
    DELETE FROM device_heartbeat_logs 
    WHERE created < cutoff_date;
    
    -- 清理网络日志
    DELETE FROM device_network_logs 
    WHERE created < cutoff_date;
    
    -- 返回清理的记录数
    SELECT ROW_COUNT() as cleaned_rows;
END //
DELIMITER ;

-- =============================================
-- 10. 创建触发器
-- =============================================

-- 设备状态变更触发器
DELIMITER //
CREATE TRIGGER `tr_device_status_change` 
AFTER UPDATE ON `dispositivos` 
FOR EACH ROW
BEGIN
    -- 如果连接状态发生变化
    IF OLD.connection_status != NEW.connection_status THEN
        -- 如果状态变为断开，更新断开时间
        IF NEW.connection_status = 'disconnected' AND OLD.connection_status = 'connected' THEN
            UPDATE device_connection_logs 
            SET disconnection_time = NOW(),
                duration_seconds = TIMESTAMPDIFF(SECOND, connection_time, NOW())
            WHERE device_id = NEW.id 
                AND disconnection_time IS NULL
                AND connection_status = 'connected'
            ORDER BY connection_time DESC
            LIMIT 1;
        END IF;
    END IF;
END //
DELIMITER ;

-- =============================================
-- 11. 更新现有设备数据
-- =============================================

-- 初始化现有设备的连接状态
UPDATE dispositivos 
SET 
    connection_status = 'disconnected',
    auto_connect = TRUE,
    connection_retries = 0,
    sdk_user_id = -1,
    connection_duration = 0,
    is_online = FALSE,
    health_status = 'unknown'
WHERE connection_status IS NULL OR connection_status = '';

-- 启用外键检查
SET FOREIGN_KEY_CHECKS = 1;

-- 提交事务
COMMIT;

-- =============================================
-- 升级完成
-- =============================================

SELECT '数据库升级完成！' as message;