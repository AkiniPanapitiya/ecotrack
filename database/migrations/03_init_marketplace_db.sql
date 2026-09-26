-- =============================================================================
-- EcoTrack Marketplace Database Migration Script
-- Story 17: E-Waste Item Valuation (E5.1)
-- Target Database: ecotrack_marketplace_db
-- =============================================================================

CREATE DATABASE IF NOT EXISTS `ecotrack_marketplace_db`
    CHARACTER SET utf8mb4
    COLLATE utf8mb4_unicode_ci;

USE `ecotrack_marketplace_db`;

-- 1. Item Valuations Table
-- Links collected pickup items to their resale valuations.
-- One valuation per pickup item (unique constraint).
-- PickupItemId  -> PickupItems in ecotrack_logistics_db  (validated in app code)
-- RecyclerId    -> RecyclerProfiles in ecotrack_identity_db (validated in app code)
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

-- 2. Refurbished Product Listings Table
-- Extends valuations into sellable marketplace listings.
-- Only valued items can be listed (ValuationId references ItemValuations).
-- One listing per valuation (unique constraint).
-- ValuationId  -> ItemValuations in same DB (same database, enforced by FK)
-- RecyclerId   -> RecyclerProfiles in ecotrack_identity_db (validated in app code)
CREATE TABLE IF NOT EXISTS `Listings` (
    `Id` VARCHAR(36) NOT NULL PRIMARY KEY,
    `ValuationId` VARCHAR(36) NOT NULL,
    `RecyclerId` VARCHAR(36) NOT NULL,
    `Title` VARCHAR(200) NOT NULL,
    `Description` TEXT NULL,
    `Price` DECIMAL(12, 2) NOT NULL,
    `PhotoPath` VARCHAR(500) NULL,
    `Status` VARCHAR(20) NOT NULL DEFAULT 'Available',
    `CreatedAt` DATETIME(6) NOT NULL DEFAULT CURRENT_TIMESTAMP(6),
    `UpdatedAt` DATETIME(6) NOT NULL DEFAULT CURRENT_TIMESTAMP(6) ON UPDATE CURRENT_TIMESTAMP(6),
    UNIQUE KEY `uq_listing_valuation` (`ValuationId`),
    INDEX `idx_listing_status` (`Status`),
    INDEX `idx_listing_recycler` (`RecyclerId`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;
