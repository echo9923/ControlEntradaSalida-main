-- =====================================================
-- MySQL Script for Access Control System
-- =====================================================
-- 系统名称: ControlEntradaSalida 门禁管理系统
-- 脚本版本: v2.0
-- 生成日期: 2025-10-03
-- 说明: 精简后的数据库脚本,仅保留项目实际使用的表和字段
--
-- 核心功能:
-- 1. 设备管理 - 门禁设备的注册、配置和连接管理
-- 2. 员工管理 - 员工信息和门禁权限管理(已合并权限信息)
-- 3. 数据备份 - 设备用户数据的备份功能
-- =====================================================

SET @OLD_UNIQUE_CHECKS=@@UNIQUE_CHECKS, UNIQUE_CHECKS=0;
SET @OLD_FOREIGN_KEY_CHECKS=@@FOREIGN_KEY_CHECKS, FOREIGN_KEY_CHECKS=0;
SET @OLD_SQL_MODE=@@SQL_MODE, SQL_MODE='ONLY_FULL_GROUP_BY,STRICT_TRANS_TABLES,NO_ZERO_IN_DATE,NO_ZERO_DATE,ERROR_FOR_DIVISION_BY_ZERO,NO_ENGINE_SUBSTITUTION';

-- =====================================================
-- Schema: access_control_system
-- =====================================================
DROP SCHEMA IF EXISTS `access_control_system`;

CREATE SCHEMA IF NOT EXISTS `access_control_system`
DEFAULT CHARACTER SET utf8mb4
COLLATE utf8mb4_unicode_ci;

USE `access_control_system`;

-- =====================================================
-- 表: devices (门禁设备表)
-- =====================================================
-- 用途: 存储门禁设备的基本信息和连接配置
-- 使用位置:
--   - LoginDevice.cs (设备注册和编辑)
--   - GestionDispositivos.cs (设备列表管理)
--   - DeviceConnectionManager.cs (设备连接管理)
--   - MDIParent.cs (设备状态监控)
-- =====================================================
DROP TABLE IF EXISTS `access_control_system`.`devices`;

CREATE TABLE IF NOT EXISTS `access_control_system`.`devices` (
  `device_id` INT NOT NULL AUTO_INCREMENT
    COMMENT '设备唯一标识(主键)',

  `device_name` VARCHAR(255) NOT NULL
    COMMENT '设备名称',

  `description` VARCHAR(255) NULL DEFAULT NULL
    COMMENT '设备所属区域(生产区域/办公区域)',

  `ip_address` VARCHAR(20) NOT NULL
    COMMENT '设备IP地址',

  `port` VARCHAR(5) NOT NULL DEFAULT '8000'
    COMMENT '设备端口号',

  `username` VARCHAR(45) NOT NULL DEFAULT 'admin'
    COMMENT '设备登录用户名',

  `password` VARCHAR(255) NOT NULL
    COMMENT '设备登录密码',

  `status` TINYINT NOT NULL DEFAULT 1
    COMMENT '设备状态: 1=启用 0=禁用 (新增时默认为1)',

  `is_default` TINYINT NOT NULL DEFAULT 0
    COMMENT '是否默认设备: 1=默认 0=普通 (系统中只能有一个默认设备)',

  `last_used_time` DATETIME NULL DEFAULT NULL
    COMMENT '最后使用时间(设备最后连接或操作的时间)',

  `created_at` DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP
    COMMENT '创建时间',

  `updated_at` DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP
    COMMENT '最后更新时间',

  PRIMARY KEY (`device_id`),
  INDEX `idx_ip_address` (`ip_address` ASC) COMMENT '通过IP地址快速查找设备',
  INDEX `idx_status` (`status` ASC) COMMENT '筛选启用/禁用的设备',
  INDEX `idx_is_default` (`is_default` ASC) COMMENT '快速定位默认设备'
)
ENGINE=InnoDB
AUTO_INCREMENT=1
DEFAULT CHARSET=utf8mb4
COLLATE=utf8mb4_unicode_ci
COMMENT='门禁设备信息表';

-- =====================================================
-- 表: employees (员工信息表)
-- =====================================================
-- 用途: 存储员工基本信息,用于门禁权限管理
-- 使用位置:
--   - GestionEmpleados.cs (员工的增删改查所有操作)
-- =====================================================
DROP TABLE IF EXISTS `access_control_system`.`employees`;

CREATE TABLE IF NOT EXISTS `access_control_system`.`employees` (
  `employee_id` VARCHAR(30) NOT NULL
    COMMENT '员工编号(主键)',

  `card_number` VARCHAR(20) NOT NULL
    COMMENT '卡号(与员工编号相同,用于门禁识别)',

  `full_name` VARCHAR(255) NOT NULL
    COMMENT '员工完整姓名',

  `photo_path` VARCHAR(255) NOT NULL DEFAULT ''
    COMMENT '人脸照片存储路径',

  `status` ENUM('ACTIVE', 'INACTIVE') NOT NULL DEFAULT 'ACTIVE'
    COMMENT '员工状态: ACTIVE=在职 INACTIVE=离职',

  `permission_level` TINYINT NOT NULL DEFAULT 0
    COMMENT '权限级别: 0=无任何权限 1=仅可进入办公区域 2=可进入办公和生产区域',

  `last_synced_level` TINYINT NULL DEFAULT NULL
    COMMENT '最近一次同步到设备的权限级别',

  `permission_updated_at` DATETIME NULL DEFAULT NULL
    COMMENT '权限级别最后更新时间',

  `last_synced_at` DATETIME NULL DEFAULT NULL
    COMMENT '权限最近一次同步到设备的时间',

  `created_at` DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP
    COMMENT '创建时间',

  `updated_at` DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP
    COMMENT '最后更新时间',

  PRIMARY KEY (`employee_id`),
  UNIQUE KEY `uk_card_number` (`card_number`) COMMENT '卡号唯一约束',
  INDEX `idx_status` (`status` ASC) COMMENT '快速筛选在职/离职员工',
  INDEX `idx_full_name` (`full_name` ASC) COMMENT '通过姓名搜索员工',
  INDEX `idx_permission_level` (`permission_level` ASC) COMMENT '按权限级别筛选员工'
)
ENGINE=InnoDB
DEFAULT CHARSET=utf8mb4
COLLATE=utf8mb4_unicode_ci
COMMENT='员工信息表(包含权限信息)';

-- =====================================================
-- 表: device_users_backup (设备用户备份表)
-- =====================================================
-- 用途: 备份从设备下载的用户数据和照片
-- 使用位置:
--   - GestionUsuariosDispositivo.cs (设备数据备份功能)
-- =====================================================
DROP TABLE IF EXISTS `access_control_system`.`device_users_backup`;

CREATE TABLE IF NOT EXISTS `access_control_system`.`device_users_backup` (
  `backup_id` BIGINT NOT NULL AUTO_INCREMENT
    COMMENT '备份记录ID(主键)',

  `userdata` TEXT NOT NULL
    COMMENT '用户数据(JSON格式,包含用户配置信息)',

  `image` LONGBLOB NULL DEFAULT NULL
    COMMENT '用户照片(二进制数据)',

  `created_at` DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP
    COMMENT '备份时间',

  PRIMARY KEY (`backup_id`),
  INDEX `idx_created_at` (`created_at` ASC) COMMENT '按时间查询备份记录'
)
ENGINE=InnoDB
DEFAULT CHARSET=utf8mb4
COLLATE=utf8mb4_unicode_ci
COMMENT='设备用户数据备份表';

-- =====================================================
-- 存储过程: delete_employee
-- =====================================================
-- 用途: 安全删除员工记录
-- 使用位置: GestionEmpleados.cs:679
-- 说明: 使用事务确保数据一致性
-- =====================================================
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

    -- 删除员工记录
    DELETE FROM employees WHERE employee_id = emp_id;

    COMMIT;
END//
DELIMITER ;

-- =====================================================
-- 示例数据 (可选)
-- =====================================================
-- 取消下面注释以插入示例数据用于测试

/*
-- 插入示例设备
INSERT INTO devices (device_name, description, ip_address, port, username, password, status, is_default) VALUES
('主入口设备', '生产区域', '192.168.1.100', '8000', 'admin', 'SXSSF1314te', 1, 1),
('副入口设备', '办公区域', '192.168.1.101', '8000', 'admin', 'SXSSF1314te', 1, 0);

-- 插入示例员工(包含权限信息)
INSERT INTO employees (employee_id, card_number, full_name, status, permission_level) VALUES
('EMP001', 'EMP001', '张三', 'ACTIVE', 2),
('EMP002', 'EMP002', '李四', 'ACTIVE', 1),
('EMP003', 'EMP003', '王五', 'INACTIVE', 0);
*/

-- =====================================================
-- 恢复系统设置
-- =====================================================
SET SQL_MODE=@OLD_SQL_MODE;
SET FOREIGN_KEY_CHECKS=@OLD_FOREIGN_KEY_CHECKS;
SET UNIQUE_CHECKS=@OLD_UNIQUE_CHECKS;

-- =====================================================
-- 脚本执行完成
-- =====================================================
--
-- ✓ 数据库架构 'access_control_system' 已创建
--
-- 已创建核心表:
-- 1. devices (门禁设备表) - 存储设备配置和连接信息
-- 2. employees (员工信息表) - 存储员工基本信息、状态和权限信息
-- 3. device_users_backup (备份表) - 存储设备用户数据备份
--
-- 已创建存储过程:
-- 1. delete_employee - 安全删除员工记录(带事务)
--
-- 优化说明:
-- - 所有表字段与代码实际使用完全一致
-- - 已将 user_permissions 表合并到 employees 表中,简化查询
-- - 移除了未使用的access_logs表和相关存储过程
-- - 字段注释清晰说明了用途和默认值
-- - 索引优化,提高查询性能
-- - 字符集统一使用utf8mb4,支持完整Unicode字符
-- - 示例数据已更新为当前系统使用的默认值
--
-- =====================================================
