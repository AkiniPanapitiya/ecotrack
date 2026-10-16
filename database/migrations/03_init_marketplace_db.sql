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
    `IsDeleted` TINYINT(1) NOT NULL DEFAULT 0,
    `CreatedAt` DATETIME(6) NOT NULL DEFAULT CURRENT_TIMESTAMP(6),
    `UpdatedAt` DATETIME(6) NOT NULL DEFAULT CURRENT_TIMESTAMP(6) ON UPDATE CURRENT_TIMESTAMP(6),
    UNIQUE KEY `uq_listing_valuation` (`ValuationId`),
    INDEX `idx_listing_status` (`Status`),
    INDEX `idx_listing_recycler` (`RecyclerId`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

-- 3. Orders Table
-- Records a buyer's purchase of a listed item.
-- One order per listing (unique constraint on ListingId).
-- ListingId -> Listings in same DB (enforced by FK)
-- BuyerId    -> Users in ecotrack_identity_db (validated in app code)
-- Status flow: Placed -> Completed (or Cancelled)
-- An order is Cancelled when its payment record is Refunded (Story 20).
-- PriceAtPurchase is snapshot of listing price at order time.
CREATE TABLE IF NOT EXISTS `Orders` (
    `Id` VARCHAR(36) NOT NULL PRIMARY KEY,
    `ListingId` VARCHAR(36) NOT NULL,
    `BuyerId` VARCHAR(36) NOT NULL,
    `PriceAtPurchase` DECIMAL(12, 2) NOT NULL,
    `Status` VARCHAR(20) NOT NULL DEFAULT 'Placed',
    `CreatedAt` DATETIME(6) NOT NULL DEFAULT CURRENT_TIMESTAMP(6),
    `UpdatedAt` DATETIME(6) NOT NULL DEFAULT CURRENT_TIMESTAMP(6) ON UPDATE CURRENT_TIMESTAMP(6),
    UNIQUE KEY `uq_order_listing` (`ListingId`),
    INDEX `idx_order_buyer` (`BuyerId`),
    INDEX `idx_order_status` (`Status`),
    CONSTRAINT `fk_order_listing` FOREIGN KEY (`ListingId`) REFERENCES `Listings`(`Id`) ON DELETE RESTRICT
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

-- 4. Payment Records Table
-- Story 20: Escrow & Payment Record (E6.2)
-- Record-only escrow ledger: tracks the payment status of every order.
-- No real payment gateway is used - this table is a record only.
-- One payment record per order (unique constraint on OrderId).
-- OrderId    -> Orders in same DB (enforced by FK)
-- BuyerId    -> Users in ecotrack_identity_db (validated in app code)
-- RecyclerId -> seller of the listing, copied from Listings.RecyclerId
-- Status flow: Pending -> Paid -> Refunded
-- A record is created as Pending in the same transaction that places the order.
-- Refunding a Paid record also sets the linked order to Cancelled.
CREATE TABLE IF NOT EXISTS `PaymentRecords` (
    `Id` VARCHAR(36) NOT NULL PRIMARY KEY,
    `OrderId` VARCHAR(36) NOT NULL,
    `BuyerId` VARCHAR(36) NOT NULL,
    `RecyclerId` VARCHAR(36) NOT NULL,
    `Amount` DECIMAL(12, 2) NOT NULL,
    `Status` VARCHAR(20) NOT NULL DEFAULT 'Pending',
    `PaidAt` DATETIME(6) NULL,
    `RefundedAt` DATETIME(6) NULL,
    `UpdatedBy` VARCHAR(36) NULL,
    `CreatedAt` DATETIME(6) NOT NULL DEFAULT CURRENT_TIMESTAMP(6),
    `UpdatedAt` DATETIME(6) NOT NULL DEFAULT CURRENT_TIMESTAMP(6) ON UPDATE CURRENT_TIMESTAMP(6),
    UNIQUE KEY `uq_payment_order` (`OrderId`),
    INDEX `idx_payment_status` (`Status`),
    INDEX `idx_payment_buyer` (`BuyerId`),
    INDEX `idx_payment_recycler` (`RecyclerId`),
    CONSTRAINT `fk_payment_order` FOREIGN KEY (`OrderId`) REFERENCES `Orders`(`Id`) ON DELETE RESTRICT
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

-- Backfill: give every existing order a linked Pending payment record.
-- Safe to re-run: orders that already have a record are skipped.
INSERT INTO `PaymentRecords` (`Id`, `OrderId`, `BuyerId`, `RecyclerId`, `Amount`, `Status`)
SELECT UUID(), o.`Id`, o.`BuyerId`, l.`RecyclerId`, o.`PriceAtPurchase`, 'Pending'
FROM `Orders` o
INNER JOIN `Listings` l ON l.`Id` = o.`ListingId`
WHERE NOT EXISTS (SELECT 1 FROM `PaymentRecords` p WHERE p.`OrderId` = o.`Id`);
