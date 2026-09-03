-- =============================================================================
-- EcoTrack Identity Database Schema Migration
-- Database: ecotrack_identity_db
-- Microservice: Identity & Access Management Service (Auth Service)
-- Lead: Akini Panapitiya (IT24610790)
-- Target Engine: MySQL 8.0+
-- =============================================================================

CREATE DATABASE IF NOT EXISTS `ecotrack_identity_db`
    CHARACTER SET utf8mb4
    COLLATE utf8mb4_unicode_ci;

USE `ecotrack_identity_db`;

-- 1. Users Table
CREATE TABLE IF NOT EXISTS `Users` (
    `Id` VARCHAR(36) NOT NULL PRIMARY KEY,
    `FullName` VARCHAR(150) NOT NULL,
    `Email` VARCHAR(191) NOT NULL UNIQUE,
    `PasswordHash` VARCHAR(255) NOT NULL,
    `Role` VARCHAR(50) NOT NULL DEFAULT 'User', -- 'User', 'Recycler', 'Driver', 'Admin'
    `PhoneNumber` VARCHAR(30) NULL,
    `Address` VARCHAR(255) NULL,
    `IsActive` TINYINT(1) NOT NULL DEFAULT 1,
    `CreatedAt` DATETIME(6) NOT NULL DEFAULT CURRENT_TIMESTAMP(6),
    `UpdatedAt` DATETIME(6) NOT NULL DEFAULT CURRENT_TIMESTAMP(6) ON UPDATE CURRENT_TIMESTAMP(6),
    INDEX `idx_users_email` (`Email`),
    INDEX `idx_users_role` (`Role`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

-- 2. Recycler Profiles Table
CREATE TABLE IF NOT EXISTS `RecyclerProfiles` (
    `Id` VARCHAR(36) NOT NULL PRIMARY KEY,
    `UserId` VARCHAR(36) NOT NULL UNIQUE,
    `CompanyName` VARCHAR(200) NOT NULL,
    `BusinessRegistrationNumber` VARCHAR(100) NOT NULL,
    `FacilityAddress` VARCHAR(255) NOT NULL,
    `OperationalCapacityKg` DECIMAL(12, 2) NOT NULL DEFAULT 0.00,
    `VerificationStatus` VARCHAR(50) NOT NULL DEFAULT 'Pending', -- 'Pending', 'Approved', 'Rejected', 'Suspended'
    `CreatedAt` DATETIME(6) NOT NULL DEFAULT CURRENT_TIMESTAMP(6),
    `UpdatedAt` DATETIME(6) NOT NULL DEFAULT CURRENT_TIMESTAMP(6) ON UPDATE CURRENT_TIMESTAMP(6),
    CONSTRAINT `fk_recycler_user` FOREIGN KEY (`UserId`) REFERENCES `Users` (`Id`) ON DELETE CASCADE,
    INDEX `idx_recycler_verification_status` (`VerificationStatus`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

-- 3. KYC Documents Table (for regulatory licensing compliance)
CREATE TABLE IF NOT EXISTS `KycDocuments` (
    `Id` VARCHAR(36) NOT NULL PRIMARY KEY,
    `UserId` VARCHAR(36) NOT NULL,
    `DocumentType` VARCHAR(100) NOT NULL, -- 'BusinessLicense', 'EnvironmentalPermit', 'TaxRegistration'
    `DocumentUrl` VARCHAR(500) NOT NULL,
    `VerificationStatus` VARCHAR(50) NOT NULL DEFAULT 'Pending', -- 'Pending', 'Verified', 'Rejected'
    `UploadedAt` DATETIME(6) NOT NULL DEFAULT CURRENT_TIMESTAMP(6),
    `ReviewedAt` DATETIME(6) NULL,
    CONSTRAINT `fk_kyc_user` FOREIGN KEY (`UserId`) REFERENCES `Users` (`Id`) ON DELETE CASCADE,
    INDEX `idx_kyc_user_status` (`UserId`, `VerificationStatus`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

-- 4. User Audit & Registration Activity Logs Table (Dynamic Report Source)
CREATE TABLE IF NOT EXISTS `UserAuditLogs` (
    `Id` VARCHAR(36) NOT NULL PRIMARY KEY,
    `UserId` VARCHAR(36) NULL,
    `UserEmail` VARCHAR(191) NOT NULL,
    `Action` VARCHAR(100) NOT NULL, -- 'REGISTER', 'LOGIN_SUCCESS', 'LOGIN_FAILED', 'PROFILE_UPDATE', 'STATUS_CHANGE'
    `Role` VARCHAR(50) NOT NULL,
    `Details` TEXT NULL,
    `IpAddress` VARCHAR(45) NULL,
    `Timestamp` DATETIME(6) NOT NULL DEFAULT CURRENT_TIMESTAMP(6),
    INDEX `idx_audit_timestamp` (`Timestamp`),
    INDEX `idx_audit_action` (`Action`),
    INDEX `idx_audit_role` (`Role`),
    INDEX `idx_audit_user_email` (`UserEmail`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

-- 5. User Feedback / Ratings Table
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

--6. Backlisted Tokens table
CREATE TABLE IF NOT EXISTS `BlacklistedTokens` (
    `Id` VARCHAR(36) NOT NULL PRIMARY KEY,
    `Jti` VARCHAR(100) NOT NULL UNIQUE,  
    `UserId` VARCHAR(36) NOT NULL,
    `ExpiresAt` DATETIME(6) NOT NULL,
    `CreatedAt` DATETIME(6) NOT NULL DEFAULT CURRENT_TIMESTAMP(6),
    INDEX `idx_blacklist_jti` (`Jti`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;