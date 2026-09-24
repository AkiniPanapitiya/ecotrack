-- =============================================================================
-- EcoTrack Marketplace Database Migration Script
-- Target Database: ecotrack_marketplace_db
-- =============================================================================

CREATE DATABASE IF NOT EXISTS `ecotrack_marketplace_db`
    CHARACTER SET utf8mb4
    COLLATE utf8mb4_unicode_ci;

USE `ecotrack_marketplace_db`;

-- 1. Item Valuations Table
CREATE TABLE IF NOT EXISTS `ItemValuations` (
    `Id` VARCHAR(36) NOT NULL PRIMARY KEY,
    `PickupItemId` VARCHAR(36) NOT NULL,
    `RecyclerId` VARCHAR(36) NOT NULL,
    `Price` DECIMAL(12, 2) NOT NULL,
    `Condition` VARCHAR(20) NOT NULL DEFAULT 'Good',
    `CreatedAt` DATETIME(6) NOT NULL DEFAULT CURRENT_TIMESTAMP(6),
    `UpdatedAt` DATETIME(6) NOT NULL DEFAULT CURRENT_TIMESTAMP(6) ON UPDATE CURRENT_TIMESTAMP(6),
    UNIQUE KEY `uq_valuation_pickup_item` (`PickupItemId`),
    INDEX `idx_valuation_recycler` (`RecyclerId`),
    INDEX `idx_valuation_condition` (`Condition`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;
