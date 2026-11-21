CREATE DATABASE IF NOT EXISTS `commentsdb`
  DEFAULT CHARACTER SET utf8mb4
  COLLATE utf8mb4_unicode_ci;

USE `commentsdb`;

CREATE TABLE `users` (
  `Id` CHAR(36) NOT NULL,
  `UserName` VARCHAR(50) NOT NULL,
  `Email` VARCHAR(254) NOT NULL,
  `HomePage` VARCHAR(2048) NULL,
  `IpAddress` VARCHAR(45) NOT NULL,
  `UserAgent` VARCHAR(512) NOT NULL,
  `CreatedAt` DATETIME(6) NOT NULL,
  PRIMARY KEY (`Id`),
  UNIQUE KEY `UX_users_UserName_Email` (`UserName`, `Email`)
) ENGINE = InnoDB;

CREATE TABLE `comments` (
  `Id` CHAR(36) NOT NULL,
  `Text` VARCHAR(5000) NOT NULL,
  `CreatedAt` DATETIME(6) NOT NULL,
  `UserId` CHAR(36) NOT NULL,
  `ParentId` CHAR(36) NULL,
  PRIMARY KEY (`Id`),
  KEY `IX_comments_UserId` (`UserId`),
  KEY `IX_comments_ParentId` (`ParentId`),
  KEY `IX_comments_CreatedAt` (`CreatedAt`),
  CONSTRAINT `FK_comments_users_UserId`
    FOREIGN KEY (`UserId`) REFERENCES `users` (`Id`) ON DELETE RESTRICT,
  CONSTRAINT `FK_comments_comments_ParentId`
    FOREIGN KEY (`ParentId`) REFERENCES `comments` (`Id`) ON DELETE RESTRICT
) ENGINE = InnoDB;

CREATE TABLE `attachments` (
  `Id` CHAR(36) NOT NULL,
  `Type` INT NOT NULL,
  `OriginalFileName` VARCHAR(255) NOT NULL,
  `StoredFileName` VARCHAR(255) NOT NULL,
  `SizeBytes` BIGINT NOT NULL,
  `CommentId` CHAR(36) NOT NULL,
  PRIMARY KEY (`Id`),
  UNIQUE KEY `UX_attachments_CommentId` (`CommentId`),
  CONSTRAINT `FK_attachments_comments_CommentId`
    FOREIGN KEY (`CommentId`) REFERENCES `comments` (`Id`) ON DELETE CASCADE
) ENGINE = InnoDB;