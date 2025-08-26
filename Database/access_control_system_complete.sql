-- MySQL Script for Access Control System
-- Database Refactoring - English Naming Convention
-- Generated for ControlEntradaSalida System
-- Author: System Database Refactoring
-- Date: 2025-08-26

SET @OLD_UNIQUE_CHECKS=@@UNIQUE_CHECKS, UNIQUE_CHECKS=0;
SET @OLD_FOREIGN_KEY_CHECKS=@@FOREIGN_KEY_CHECKS, FOREIGN_KEY_CHECKS=0;
SET @OLD_SQL_MODE=@@SQL_MODE, SQL_MODE='ONLY_FULL_GROUP_BY,STRICT_TRANS_TABLES,NO_ZERO_IN_DATE,NO_ZERO_DATE,ERROR_FOR_DIVISION_BY_ZERO,NO_ENGINE_SUBSTITUTION';

-- -----------------------------------------------------
-- Schema access_control_system
-- -----------------------------------------------------
DROP SCHEMA IF EXISTS `access_control_system`;

-- -----------------------------------------------------
-- Schema access_control_system
-- -----------------------------------------------------
CREATE SCHEMA IF NOT EXISTS `access_control_system` DEFAULT CHARACTER SET utf8mb4 COLLATE utf8mb4_unicode_ci;
USE `access_control_system`;

-- -----------------------------------------------------
-- Table `access_control_system`.`devices`
-- -----------------------------------------------------
DROP TABLE IF EXISTS `access_control_system`.`devices`;

CREATE TABLE IF NOT EXISTS `access_control_system`.`devices` (
  `device_id` INT NOT NULL AUTO_INCREMENT,
  `device_name` VARCHAR(255) NOT NULL,
  `description` VARCHAR(255) NULL DEFAULT NULL,
  `ip_address` VARCHAR(20) NOT NULL,
  `port` VARCHAR(5) NOT NULL,
  `username` VARCHAR(45) NOT NULL,
  `password` VARCHAR(255) NOT NULL,
  `status` TINYINT NOT NULL DEFAULT 1 COMMENT '1=Active, 0=Inactive',
  `is_default` TINYINT NOT NULL DEFAULT 0 COMMENT '1=Default device, 0=Regular device',
  `last_used_time` DATETIME NULL DEFAULT NULL,
  `created_at` DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
  `updated_at` DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP,
  PRIMARY KEY (`device_id`),
  INDEX `idx_ip_address` (`ip_address` ASC),
  INDEX `idx_status` (`status` ASC),
  INDEX `idx_is_default` (`is_default` ASC)
) ENGINE=InnoDB AUTO_INCREMENT=2 DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

-- -----------------------------------------------------
-- Table `access_control_system`.`employees`
-- -----------------------------------------------------
DROP TABLE IF EXISTS `access_control_system`.`employees`;

CREATE TABLE IF NOT EXISTS `access_control_system`.`employees` (
  `employee_id` VARCHAR(30) NOT NULL,
  `card_number` VARCHAR(20) NOT NULL,
  `first_name` VARCHAR(255) NOT NULL,
  `last_name` VARCHAR(255) NOT NULL,
  `photo_path` VARCHAR(255) NOT NULL DEFAULT '',
  `status` ENUM('ACTIVE', 'INACTIVE') NOT NULL DEFAULT 'ACTIVE',
  `created_at` DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
  `updated_at` DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP,
  PRIMARY KEY (`employee_id`),
  UNIQUE KEY `uk_card_number` (`card_number`),
  INDEX `idx_status` (`status` ASC),
  INDEX `idx_name` (`first_name` ASC, `last_name` ASC)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

-- -----------------------------------------------------
-- Table `access_control_system`.`access_logs`
-- -----------------------------------------------------
DROP TABLE IF EXISTS `access_control_system`.`access_logs`;

CREATE TABLE IF NOT EXISTS `access_control_system`.`access_logs` (
  `log_id` BIGINT NOT NULL AUTO_INCREMENT,
  `log_number` INT NOT NULL,
  `log_date` DATE NOT NULL,
  `log_time` TIME NOT NULL,
  `employee_id` VARCHAR(30) NOT NULL,
  `device_id` INT NOT NULL,
  `event_type` VARCHAR(255) NOT NULL,
  `created_at` DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
  PRIMARY KEY (`log_id`),
  UNIQUE KEY `uk_composite` (`log_number`, `log_date`, `log_time`, `device_id`),
  INDEX `idx_employee_date` (`employee_id` ASC, `log_date` ASC),
  INDEX `idx_device_date` (`device_id` ASC, `log_date` ASC),
  INDEX `idx_log_date` (`log_date` ASC),
  INDEX `idx_event_type` (`event_type` ASC),
  CONSTRAINT `fk_access_logs_employee`
    FOREIGN KEY (`employee_id`)
    REFERENCES `access_control_system`.`employees` (`employee_id`)
    ON DELETE CASCADE
    ON UPDATE CASCADE,
  CONSTRAINT `fk_access_logs_device`
    FOREIGN KEY (`device_id`)
    REFERENCES `access_control_system`.`devices` (`device_id`)
    ON DELETE RESTRICT
    ON UPDATE CASCADE
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

-- -----------------------------------------------------
-- Table `access_control_system`.`device_users_backup`
-- -----------------------------------------------------
DROP TABLE IF EXISTS `access_control_system`.`device_users_backup`;

CREATE TABLE IF NOT EXISTS `access_control_system`.`device_users_backup` (
  `backup_id` BIGINT NOT NULL AUTO_INCREMENT,
  `userdata` TEXT NOT NULL,
  `image` LONGBLOB NULL DEFAULT NULL,
  `created_at` DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
  PRIMARY KEY (`backup_id`),
  INDEX `idx_created_at` (`created_at` ASC)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

USE `access_control_system`;

-- -----------------------------------------------------
-- Procedure delete_employee
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
    
    -- Delete employee access logs
    DELETE FROM access_logs WHERE employee_id = emp_id;
    
    -- Delete employee record
    DELETE FROM employees WHERE employee_id = emp_id;
    
    COMMIT;
END//
DELIMITER ;

-- -----------------------------------------------------
-- Procedure generate_attendance_report
-- -----------------------------------------------------
USE `access_control_system`;
DROP PROCEDURE IF EXISTS `access_control_system`.`generate_attendance_report`;

DELIMITER //
USE `access_control_system`//
CREATE DEFINER=`root`@`localhost` PROCEDURE `generate_attendance_report`()
BEGIN
    DECLARE var_employee_id VARCHAR(30);
    DECLARE var_previous_employee_id VARCHAR(30);
    DECLARE var_log_date DATE;
    DECLARE var_previous_log_date DATE;
    DECLARE var_log_time TIME;
    DECLARE var_device_id INT;
    DECLARE var_finished INTEGER DEFAULT 0;
    DECLARE var_updated BOOLEAN;
    DECLARE var_last_id INTEGER;
    
    DECLARE attendance_cursor CURSOR FOR 
        SELECT log_date, log_time, employee_id, device_id 
        FROM access_logs 
        ORDER BY employee_id, log_date, log_time ASC;
    
    DECLARE CONTINUE HANDLER FOR NOT FOUND SET var_finished = 1;

    -- Drop temporary table if exists
    DROP TABLE IF EXISTS temp_attendance_report;
    
    -- Create temporary report table
    CREATE TABLE temp_attendance_report (
        id INTEGER NOT NULL PRIMARY KEY AUTO_INCREMENT,
        report_date DATE NOT NULL,
        check_in_time TIME,
        check_out_time TIME,
        employee_id VARCHAR(30) NOT NULL,
        device_id INT,
        INDEX idx_employee_date (employee_id, report_date)
    );

    OPEN attendance_cursor;
    
    SET var_previous_employee_id = "";
    SET var_previous_log_date = "1900-01-01";
    SET var_updated = 0;
    
    START TRANSACTION;
    
    attendance_loop: LOOP
        FETCH attendance_cursor INTO var_log_date, var_log_time, var_employee_id, var_device_id;
        
        IF var_finished = 1 THEN
            LEAVE attendance_loop;
        END IF;

        IF var_previous_log_date != var_log_date THEN
            -- New date, insert new record
            INSERT INTO temp_attendance_report (report_date, check_in_time, employee_id, device_id) 
            VALUES (var_log_date, var_log_time, var_employee_id, var_device_id);
            SET var_updated = 0;
        ELSE
            IF var_previous_employee_id = var_employee_id AND var_updated = 1 THEN
                -- Same employee, new day, insert new record
                INSERT INTO temp_attendance_report (report_date, check_in_time, employee_id, device_id) 
                VALUES (var_log_date, var_log_time, var_employee_id, var_device_id);
                SET var_updated = 0;
            ELSE
                -- Update existing record with check out time
                UPDATE temp_attendance_report 
                SET check_out_time = var_log_time 
                WHERE employee_id = var_employee_id 
                  AND report_date = var_log_date 
                  AND id = var_last_id;
                SET var_updated = 1;
            END IF;
        END IF;

        SET var_last_id = (SELECT MAX(id) FROM temp_attendance_report);
        SET var_previous_employee_id = var_employee_id;
        SET var_previous_log_date = var_log_date;
    END LOOP attendance_loop;
    
    COMMIT;
    CLOSE attendance_cursor;
END//
DELIMITER ;

-- -----------------------------------------------------
-- Sample Data (Optional)
-- -----------------------------------------------------
-- Uncomment the following lines to insert sample data

/*
-- Insert sample devices
INSERT INTO devices (device_name, description, ip_address, port, username, password, status, is_default) VALUES
('Main Entrance Device', 'Primary access control device at main entrance', '192.168.1.100', '8000', 'admin', 'admin123', 1, 1),
('Secondary Entrance Device', 'Secondary access control device', '192.168.1.101', '8000', 'admin', 'admin123', 1, 0);

-- Insert sample employees
INSERT INTO employees (employee_id, card_number, first_name, last_name, status) VALUES
('EMP001', 'CARD001', 'John', 'Doe', 'ACTIVE'),
('EMP002', 'CARD002', 'Jane', 'Smith', 'ACTIVE'),
('EMP003', 'CARD003', 'Bob', 'Johnson', 'INACTIVE');
*/

-- -----------------------------------------------------
-- Database Configuration Complete
-- -----------------------------------------------------

SET SQL_MODE=@OLD_SQL_MODE;
SET FOREIGN_KEY_CHECKS=@OLD_FOREIGN_KEY_CHECKS;
SET UNIQUE_CHECKS=@OLD_UNIQUE_CHECKS;

-- Script execution completed successfully
-- Database schema 'access_control_system' has been created with the following tables:
-- 1. devices (formerly dispositivos)
-- 2. employees (formerly empleados) 
-- 3. device_users_backup (for device user data backup)
--
-- Stored procedures created:
-- 1. delete_employee (formerly ELIMINAR_EMPLEADO)
-- 2. generate_attendance_report (formerly CREAR_TABLA_INFORME_ES)