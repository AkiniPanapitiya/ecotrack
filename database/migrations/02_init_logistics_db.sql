-- =============================================================================
-- EcoTrack Logistics Database Migration Script
-- Sprint 1: E-Waste Pickup Scheduling (ECO-50)
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
