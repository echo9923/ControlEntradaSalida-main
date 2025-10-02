-- MySQL Script for Access Control System
-- 优化后的数据库脚本 - 仅保留项目实际使用的表和结构
-- 系统名称: ControlEntradaSalida 门禁管理系统
-- 生成日期: 2025-10-02
-- 说明: 本脚本已移除未使用的冗余表结构,仅保留项目核心功能所需的数据表

SET @OLD_UNIQUE_CHECKS=@@UNIQUE_CHECKS, UNIQUE_CHECKS=0;
SET @OLD_FOREIGN_KEY_CHECKS=@@FOREIGN_KEY_CHECKS, FOREIGN_KEY_CHECKS=0;
SET @OLD_SQL_MODE=@@SQL_MODE, SQL_MODE='ONLY_FULL_GROUP_BY,STRICT_TRANS_TABLES,NO_ZERO_IN_DATE,NO_ZERO_DATE,ERROR_FOR_DIVISION_BY_ZERO,NO_ENGINE_SUBSTITUTION';

-- -----------------------------------------------------
-- Schema access_control_system
-- -----------------------------------------------------
DROP SCHEMA IF EXISTS `access_control_system`;

CREATE SCHEMA IF NOT EXISTS `access_control_system` DEFAULT CHARACTER SET utf8mb4 COLLATE utf8mb4_unicode_ci;
USE `access_control_system`;

-- -----------------------------------------------------
-- 表: devices (设备表)
-- 用途: 存储门禁设备的基本信息和连接配置
-- 使用位置: DeviceConnectionManager.cs, GestionDispositivos.cs, LoginDevice.cs
-- -----------------------------------------------------
DROP TABLE IF EXISTS `access_control_system`.`devices`;

CREATE TABLE IF NOT EXISTS `access_control_system`.`devices` (
  `device_id` INT NOT NULL AUTO_INCREMENT COMMENT '设备唯一标识',
  `device_name` VARCHAR(255) NOT NULL COMMENT '设备名称',
  `description` VARCHAR(255) NULL DEFAULT NULL COMMENT '设备描述',
  `ip_address` VARCHAR(20) NOT NULL COMMENT '设备IP地址',
  `port` VARCHAR(5) NOT NULL COMMENT '设备端口号',
  `username` VARCHAR(45) NOT NULL COMMENT '设备登录用户名',
  `password` VARCHAR(255) NOT NULL COMMENT '设备登录密码',
  `status` TINYINT NOT NULL DEFAULT 1 COMMENT '设备状态: 1=启用, 0=禁用',
  `is_default` TINYINT NOT NULL DEFAULT 0 COMMENT '是否默认设备: 1=默认, 0=普通',
  `last_used_time` DATETIME NULL DEFAULT NULL COMMENT '最后使用时间',
  `created_at` DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP COMMENT '创建时间',
  `updated_at` DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP COMMENT '最后更新时间',
  PRIMARY KEY (`device_id`),
  INDEX `idx_ip_address` (`ip_address` ASC),
  INDEX `idx_status` (`status` ASC),
  INDEX `idx_is_default` (`is_default` ASC)
) ENGINE=InnoDB AUTO_INCREMENT=2 DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci
COMMENT='门禁设备信息表';

-- -----------------------------------------------------
-- 表: employees (员工表)
-- 用途: 存储员工基本信息,用于门禁权限管理
-- 使用位置: GestionEmpleados.cs (增删改查所有操作)
-- -----------------------------------------------------
DROP TABLE IF EXISTS `access_control_system`.`employees`;

CREATE TABLE IF NOT EXISTS `access_control_system`.`employees` (
  `employee_id` VARCHAR(30) NOT NULL COMMENT '员工编号(主键)',
  `card_number` VARCHAR(20) NOT NULL COMMENT '卡号(与员工编号相同)',
  `full_name` VARCHAR(255) NOT NULL COMMENT '员工完整姓名',
  `photo_path` VARCHAR(255) NOT NULL DEFAULT '' COMMENT '人脸照片存储路径',
  `status` ENUM('ACTIVE', 'INACTIVE') NOT NULL DEFAULT 'ACTIVE' COMMENT '员工状态',
  `created_at` DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP COMMENT '创建时间',
  `updated_at` DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP COMMENT '最后更新时间',
  PRIMARY KEY (`employee_id`),
  UNIQUE KEY `uk_card_number` (`card_number`),
  INDEX `idx_status` (`status` ASC),
  INDEX `idx_full_name` (`full_name` ASC)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci
COMMENT='员工信息表';

-- -----------------------------------------------------
-- 表: access_logs (门禁记录表)
-- 用途: 记录所有门禁事件(人员刷卡、设备状态等)
-- 使用位置: 实时事件接收和日志记录
-- 特性: 支持人员事件和设备事件(employee_number可为NULL)
-- -----------------------------------------------------
DROP TABLE IF EXISTS `access_control_system`.`access_logs`;

CREATE TABLE IF NOT EXISTS `access_control_system`.`access_logs` (
  `sequence_number` BIGINT NOT NULL AUTO_INCREMENT COMMENT '序列号(主键)',
  `employee_number` VARCHAR(30) NULL COMMENT '员工编号(设备事件时为NULL)',
  `employee_name` VARCHAR(255) NULL COMMENT '员工姓名(设备事件时为空)',
  `device_number` INT NOT NULL COMMENT '设备编号',
  `device_name` VARCHAR(255) NOT NULL COMMENT '设备名称',
  `event_type` VARCHAR(128) NOT NULL COMMENT '事件类型',
  `event_time` DATETIME NOT NULL COMMENT '事件发生时间',
  `remote_host_address` VARCHAR(128) NOT NULL COMMENT '远程主机地址',
  PRIMARY KEY (`sequence_number`),
  UNIQUE KEY `uk_device_event_uniqueness` (`device_number`, `event_time`, `event_type`, `employee_number`),
  INDEX `idx_employee_time` (`employee_number` ASC, `event_time` ASC),
  INDEX `idx_device_time` (`device_number` ASC, `event_time` ASC),
  INDEX `idx_event_time` (`event_time` ASC),
  INDEX `idx_event_type` (`event_type` ASC),
  CONSTRAINT `fk_access_logs_employee`
    FOREIGN KEY (`employee_number`)
    REFERENCES `access_control_system`.`employees` (`employee_id`)
    ON DELETE SET NULL
    ON UPDATE CASCADE,
  CONSTRAINT `fk_access_logs_device`
    FOREIGN KEY (`device_number`)
    REFERENCES `access_control_system`.`devices` (`device_id`)
    ON DELETE RESTRICT
    ON UPDATE CASCADE
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci
COMMENT='门禁访问日志表 - 支持人员事件和设备事件';

-- -----------------------------------------------------
-- 表: device_users_backup (设备用户备份表)
-- 用途: 备份从设备下载的用户数据和照片
-- 使用位置: GestionUsuariosDispositivo.cs (设备数据备份功能)
-- -----------------------------------------------------
DROP TABLE IF EXISTS `access_control_system`.`device_users_backup`;

CREATE TABLE IF NOT EXISTS `access_control_system`.`device_users_backup` (
  `backup_id` BIGINT NOT NULL AUTO_INCREMENT COMMENT '备份记录ID',
  `userdata` TEXT NOT NULL COMMENT '用户数据(JSON格式)',
  `image` LONGBLOB NULL DEFAULT NULL COMMENT '用户照片(二进制数据)',
  `created_at` DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP COMMENT '备份时间',
  PRIMARY KEY (`backup_id`),
  INDEX `idx_created_at` (`created_at` ASC)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci
COMMENT='设备用户数据备份表';

-- -----------------------------------------------------
-- 存储过程: delete_employee
-- 用途: 级联删除员工及其相关的门禁记录
-- 使用位置: GestionEmpleados.cs:679 (CALL delete_employee)
-- -----------------------------------------------------
USE `access_control_system`;
DROP PROCEDURE IF EXISTS `access_control_system`.`delete_employee`;

DELIMITER //
USE `access_control_system`//
CREATE DEFINER=`root`@`localhost` PROCEDURE `delete_employee`(IN emp_id VARCHAR(30))
BEGIN
    DECLARE EXIT HANDLER FOR SQLEXCEPTION
    BEGIN
        ROLLBACK;
        RESIGNAL;
    END;

    START TRANSACTION;

    -- 删除员工的门禁记录
    DELETE FROM access_logs WHERE employee_number = emp_id;

    -- 删除员工记录
    DELETE FROM employees WHERE employee_id = emp_id;

    COMMIT;
END//
DELIMITER ;

-- -----------------------------------------------------
-- 示例数据 (可选)
-- -----------------------------------------------------
-- 取消下面注释以插入示例数据用于测试

/*
-- 插入示例设备
INSERT INTO devices (device_name, description, ip_address, port, username, password, status, is_default) VALUES
('主入口设备', '主要门禁控制设备', '192.168.1.100', '8000', 'admin', 'admin123', 1, 1),
('副入口设备', '次要门禁控制设备', '192.168.1.101', '8000', 'admin', 'admin123', 1, 0);

-- 插入示例员工
INSERT INTO employees (employee_id, card_number, full_name, status) VALUES
('EMP001', 'EMP001', '张三', 'ACTIVE'),
('EMP002', 'EMP002', '李四', 'ACTIVE'),
('EMP003', 'EMP003', '王五', 'INACTIVE');
*/

-- -----------------------------------------------------
-- 数据库配置完成
-- -----------------------------------------------------

SET SQL_MODE=@OLD_SQL_MODE;
SET FOREIGN_KEY_CHECKS=@OLD_FOREIGN_KEY_CHECKS;
SET UNIQUE_CHECKS=@OLD_UNIQUE_CHECKS;

-- 脚本执行完成
-- 数据库架构 'access_control_system' 已创建,包含以下核心表:
-- 1. devices - 门禁设备信息表
-- 2. employees - 员工信息表
-- 3. access_logs - 门禁访问日志表(支持人员和设备事件)
-- 4. device_users_backup - 设备用户数据备份表
--
-- 已创建存储过程:
-- 1. delete_employee - 级联删除员工及相关记录
--
-- 优化说明:
-- - 已移除未使用的generate_attendance_report存储过程
-- - access_logs表已优化支持设备事件(employee_number可为NULL)
-- - 所有外键约束已根据实际业务需求调整
-- - 添加完整的中文注释便于维护
