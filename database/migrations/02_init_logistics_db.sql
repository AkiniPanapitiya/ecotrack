-- =============================================================================
-- EcoTrack Logistics Database Migration Script
-- Sprint 1: E-Waste Pickup Scheduling (ECO-50)
-- Sprint 3: Disposal Certification (ECO-71/E7.1)
-- Target Database: ecotrack_logistics_db
-- =============================================================================

CREATE DATABASE IF NOT EXISTS `ecotrack_logistics_db`
    CHARACTER SET utf8mb4
    COLLATE utf8mb4_unicode_ci;

USE `ecotrack_logistics_db`;

-- 1. Pickup Requests Table
CREATE TABLE IF NOT EXISTS `PickupRequests` (
    `Id` VARCHAR(36) NOT NULL PRIMARY KEY,
    `UserId` VARCHAR(36) NOT NULL,
    `Category` VARCHAR(100) NOT NULL,
    `EstimatedWeightKg` DECIMAL(10, 2) NOT NULL,
    `PickupAddress` VARCHAR(255) NOT NULL,
    `ContactPhone` VARCHAR(30) NOT NULL,
    `PreferredDate` DATE NOT NULL,
    `TimeSlot` VARCHAR(50) NOT NULL,
    `SpecialInstructions` TEXT NULL,
    `Status` VARCHAR(50) NOT NULL DEFAULT 'Pending',
    `CreatedAt` DATETIME(6) NOT NULL DEFAULT CURRENT_TIMESTAMP(6),
    `UpdatedAt` DATETIME(6) NOT NULL DEFAULT CURRENT_TIMESTAMP(6) ON UPDATE CURRENT_TIMESTAMP(6),
    INDEX `idx_pickup_user` (`UserId`),
    INDEX `idx_pickup_status` (`Status`),
    INDEX `idx_pickup_date` (`PreferredDate`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

-- 2. Pickup Items Breakdown Table
CREATE TABLE IF NOT EXISTS `PickupItems` (
    `Id` VARCHAR(36) NOT NULL PRIMARY KEY,
    `PickupRequestId` VARCHAR(36) NOT NULL,
    `ItemName` VARCHAR(150) NOT NULL,
    `Quantity` INT NOT NULL DEFAULT 1,
    `ItemCondition` VARCHAR(50) NOT NULL DEFAULT 'Used',
    `EstimatedWeightKg` DECIMAL(10, 2) NOT NULL DEFAULT 0.00,
    CONSTRAINT `fk_pickup_item_request` FOREIGN KEY (`PickupRequestId`) REFERENCES `PickupRequests` (`Id`) ON DELETE CASCADE,
    INDEX `idx_item_request` (`PickupRequestId`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

-- 3. Add recycler scheduling columns to PickupRequests (Sprint 2)
-- Uses a stored procedure + information_schema check instead of
-- "ADD COLUMN IF NOT EXISTS", since that syntax is MariaDB-only and
-- fails with a syntax error on standard MySQL.
DELIMITER $$
DROP PROCEDURE IF EXISTS `sp_eco95_add_scheduling_columns` $$
CREATE PROCEDURE `sp_eco95_add_scheduling_columns`()
BEGIN
    IF NOT EXISTS (
        SELECT 1 FROM information_schema.COLUMNS
        WHERE TABLE_SCHEMA = DATABASE() AND TABLE_NAME = 'PickupRequests' AND COLUMN_NAME = 'RecyclerId'
    ) THEN
        ALTER TABLE `PickupRequests` ADD COLUMN `RecyclerId` VARCHAR(36) NULL;
    END IF;

    IF NOT EXISTS (
        SELECT 1 FROM information_schema.COLUMNS
        WHERE TABLE_SCHEMA = DATABASE() AND TABLE_NAME = 'PickupRequests' AND COLUMN_NAME = 'ScheduledDate'
    ) THEN
        ALTER TABLE `PickupRequests` ADD COLUMN `ScheduledDate` DATE NULL;
    END IF;

    IF NOT EXISTS (
        SELECT 1 FROM information_schema.COLUMNS
        WHERE TABLE_SCHEMA = DATABASE() AND TABLE_NAME = 'PickupRequests' AND COLUMN_NAME = 'ScheduledTimeSlot'
    ) THEN
        ALTER TABLE `PickupRequests` ADD COLUMN `ScheduledTimeSlot` VARCHAR(50) NULL;
    END IF;

    IF NOT EXISTS (
        SELECT 1 FROM information_schema.STATISTICS
        WHERE TABLE_SCHEMA = DATABASE() AND TABLE_NAME = 'PickupRequests' AND INDEX_NAME = 'idx_recycler_schedule'
    ) THEN
        ALTER TABLE `PickupRequests` ADD INDEX `idx_recycler_schedule` (`RecyclerId`, `ScheduledDate`, `ScheduledTimeSlot`);
    END IF;
END $$
DELIMITER ;

CALL `sp_eco95_add_scheduling_columns`();
DROP PROCEDURE IF EXISTS `sp_eco95_add_scheduling_columns`;

-- 4. Disposal Certificates Table (Sprint 3 - E7.1)
-- Records proof that a pickup item was properly recycled/disposed of.
-- PickupItemId -> PickupItems in same DB (enforced by FK + UNIQUE).
-- One certificate per item. Recycler name stored at creation time.
CREATE TABLE IF NOT EXISTS `DisposalCertificates` (
    `Id` VARCHAR(36) NOT NULL PRIMARY KEY,
    `PickupItemId` VARCHAR(36) NOT NULL,
    `RecyclerId` VARCHAR(36) NOT NULL,
    `RecyclerName` VARCHAR(150) NOT NULL,
    `DisposalMethod` VARCHAR(100) NOT NULL,
    `DisposedAt` DATETIME(6) NOT NULL DEFAULT CURRENT_TIMESTAMP(6),
    `CreatedAt` DATETIME(6) NOT NULL DEFAULT CURRENT_TIMESTAMP(6),
    CONSTRAINT `fk_cert_item` FOREIGN KEY (`PickupItemId`) REFERENCES `PickupItems`(`Id`) ON DELETE RESTRICT,
    CONSTRAINT `uq_cert_item` UNIQUE KEY (`PickupItemId`),
    INDEX `idx_cert_recycler` (`RecyclerId`),
    INDEX `idx_cert_method` (`DisposalMethod`),
    INDEX `idx_cert_date` (`DisposedAt`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

-- 5. Add Status column to PickupItems (Sprint 3 - E7.1)
-- Tracks item lifecycle: Pending -> Collected -> Disposed.
-- Uses stored procedure + information_schema for idempotency.
DELIMITER $$
DROP PROCEDURE IF EXISTS `sp_eco71_add_item_status` $$
CREATE PROCEDURE `sp_eco71_add_item_status`()
BEGIN
    IF NOT EXISTS (
        SELECT 1 FROM information_schema.COLUMNS
        WHERE TABLE_SCHEMA = DATABASE() AND TABLE_NAME = 'PickupItems' AND COLUMN_NAME = 'Status'
    ) THEN
        ALTER TABLE `PickupItems` ADD COLUMN `Status` VARCHAR(50) NOT NULL DEFAULT 'Pending' AFTER `EstimatedWeightKg`;
    END IF;

    IF NOT EXISTS (
        SELECT 1 FROM information_schema.STATISTICS
        WHERE TABLE_SCHEMA = DATABASE() AND TABLE_NAME = 'PickupItems' AND INDEX_NAME = 'idx_item_status'
    ) THEN
        ALTER TABLE `PickupItems` ADD INDEX `idx_item_status` (`Status`);
    END IF;
END $$
DELIMITER ;

CALL `sp_eco71_add_item_status`();
DROP PROCEDURE IF EXISTS `sp_eco71_add_item_status`;
