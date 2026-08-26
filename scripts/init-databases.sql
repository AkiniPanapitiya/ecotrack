-- =============================================================================
-- EcoTrack Global Docker Database Initialization Script
-- Provisions 4 isolated microservice databases & initial DDL schemas
-- =============================================================================

-- 1. Identity Database (Port 5001 - Auth Service)
CREATE DATABASE IF NOT EXISTS `ecotrack_identity_db`
    CHARACTER SET utf8mb4
    COLLATE utf8mb4_unicode_ci;

USE `ecotrack_identity_db`;

CREATE TABLE IF NOT EXISTS `Users` (
    `Id` VARCHAR(36) NOT NULL PRIMARY KEY,
    `FullName` VARCHAR(150) NOT NULL,
    `Email` VARCHAR(191) NOT NULL UNIQUE,
    `PasswordHash` VARCHAR(255) NOT NULL,
    `Role` VARCHAR(50) NOT NULL DEFAULT 'User',
    `PhoneNumber` VARCHAR(30) NULL,
    `Address` VARCHAR(255) NULL,
    `IsActive` TINYINT(1) NOT NULL DEFAULT 1,
    `CreatedAt` DATETIME(6) NOT NULL DEFAULT CURRENT_TIMESTAMP(6),
    `UpdatedAt` DATETIME(6) NOT NULL DEFAULT CURRENT_TIMESTAMP(6) ON UPDATE CURRENT_TIMESTAMP(6),
    INDEX `idx_users_email` (`Email`),
    INDEX `idx_users_role` (`Role`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

CREATE TABLE IF NOT EXISTS `RecyclerProfiles` (
    `Id` VARCHAR(36) NOT NULL PRIMARY KEY,
    `UserId` VARCHAR(36) NOT NULL UNIQUE,
    `CompanyName` VARCHAR(200) NOT NULL,
    `BusinessRegistrationNumber` VARCHAR(100) NOT NULL,
    `FacilityAddress` VARCHAR(255) NOT NULL,
    `OperationalCapacityKg` DECIMAL(12, 2) NOT NULL DEFAULT 0.00,
    `VerificationStatus` VARCHAR(50) NOT NULL DEFAULT 'Pending',
    `CreatedAt` DATETIME(6) NOT NULL DEFAULT CURRENT_TIMESTAMP(6),
    `UpdatedAt` DATETIME(6) NOT NULL DEFAULT CURRENT_TIMESTAMP(6) ON UPDATE CURRENT_TIMESTAMP(6),
    CONSTRAINT `fk_recycler_user` FOREIGN KEY (`UserId`) REFERENCES `Users` (`Id`) ON DELETE CASCADE,
    INDEX `idx_recycler_verification_status` (`VerificationStatus`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

CREATE TABLE IF NOT EXISTS `KycDocuments` (
    `Id` VARCHAR(36) NOT NULL PRIMARY KEY,
    `UserId` VARCHAR(36) NOT NULL,
    `DocumentType` VARCHAR(100) NOT NULL,
    `DocumentUrl` VARCHAR(500) NOT NULL,
    `VerificationStatus` VARCHAR(50) NOT NULL DEFAULT 'Pending',
    `UploadedAt` DATETIME(6) NOT NULL DEFAULT CURRENT_TIMESTAMP(6),
    `ReviewedAt` DATETIME(6) NULL,
    CONSTRAINT `fk_kyc_user` FOREIGN KEY (`UserId`) REFERENCES `Users` (`Id`) ON DELETE CASCADE,
    INDEX `idx_kyc_user_status` (`UserId`, `VerificationStatus`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

CREATE TABLE IF NOT EXISTS `UserAuditLogs` (
    `Id` VARCHAR(36) NOT NULL PRIMARY KEY,
    `UserId` VARCHAR(36) NULL,
    `UserEmail` VARCHAR(191) NOT NULL,
    `Action` VARCHAR(100) NOT NULL,
    `Role` VARCHAR(50) NOT NULL,
    `Details` TEXT NULL,
    `IpAddress` VARCHAR(45) NULL,
    `Timestamp` DATETIME(6) NOT NULL DEFAULT CURRENT_TIMESTAMP(6),
    INDEX `idx_audit_timestamp` (`Timestamp`),
    INDEX `idx_audit_action` (`Action`),
    INDEX `idx_audit_role` (`Role`),
    INDEX `idx_audit_user_email` (`UserEmail`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

CREATE TABLE IF NOT EXISTS `UserFeedback` (
    `Id` VARCHAR(36) NOT NULL PRIMARY KEY,
    `UserId` VARCHAR(36) NOT NULL,
    `RecyclerId` VARCHAR(36) NOT NULL,
    `Rating` INT NOT NULL CHECK (`Rating` BETWEEN 1 AND 5),
    `Comments` TEXT NULL,
    `IsFlagged` TINYINT(1) NOT NULL DEFAULT 0,
    `CreatedAt` DATETIME(6) NOT NULL DEFAULT CURRENT_TIMESTAMP(6),
    CONSTRAINT `fk_feedback_user` FOREIGN KEY (`UserId`) REFERENCES `Users` (`Id`) ON DELETE CASCADE,
    CONSTRAINT `fk_feedback_recycler` FOREIGN KEY (`RecyclerId`) REFERENCES `Users` (`Id`) ON DELETE CASCADE,
    INDEX `idx_feedback_recycler` (`RecyclerId`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

-- 2. Logistics Database (Port 5002 - Logistics Service)
CREATE DATABASE IF NOT EXISTS `ecotrack_logistics_db`
    CHARACTER SET utf8mb4
    COLLATE utf8mb4_unicode_ci;

-- 3. Marketplace Database (Port 5003 - Marketplace Service)
CREATE DATABASE IF NOT EXISTS `ecotrack_marketplace_db`
    CHARACTER SET utf8mb4
    COLLATE utf8mb4_unicode_ci;

-- 4. Analytics Database (Port 5004 - Analytics Service)
CREATE DATABASE IF NOT EXISTS `ecotrack_analytics_db`
    CHARACTER SET utf8mb4
    COLLATE utf8mb4_unicode_ci;
