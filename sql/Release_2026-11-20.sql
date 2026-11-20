-- =====================================================================================================
-- RELEASE 2026-11-20 - LIVE DATABASE (MySQL 8 / MariaDB)
--
--   1. Announcements: new tables used to send the "download the new mobile app" notice on WhatsApp for
--      4 days and to show a banner in the web portal for 3 days (it disappears by itself).
--   2. Dog Handler role: its own Dog Training menu (dashboard, enrolments, logbook, progress reports,
--      assessments - each handler only ever sees the dogs allocated to them).
--
--   mysql -u <user> -p <database> < Release_2026-11-20.sql
--
-- Run it BEFORE deploying the new API (the API creates nothing by itself). Safe to run more than once.
-- Users must log out and in again to see new menus.
-- =====================================================================================================

SET NAMES utf8mb4;

CREATE TABLE IF NOT EXISTS `ka_appannouncements` (
    `Id` int NOT NULL AUTO_INCREMENT,
    `Key` varchar(100) CHARACTER SET utf8mb4 NOT NULL,
    `Title` longtext CHARACTER SET utf8mb4 NOT NULL,
    `Message` longtext CHARACTER SET utf8mb4 NOT NULL,
    `PortalMessage` longtext CHARACTER SET utf8mb4 NOT NULL,
    `LinkUrl` longtext CHARACTER SET utf8mb4 NULL,
    `LinkText` longtext CHARACTER SET utf8mb4 NULL,
    `StartDate` datetime(6) NOT NULL,
    `WhatsAppDays` int NOT NULL,
    `PortalDays` int NOT NULL,
    `SendFromHour` int NOT NULL,
    `SendToHour` int NOT NULL,
    `Uuid` longtext CHARACTER SET utf8mb4 NULL,
    `CreatedAt` datetime(6) NOT NULL,
    `UpdatedAt` datetime(6) NULL,
    `DeletedAt` datetime(6) NULL,
    `CreatedBy` longtext CHARACTER SET utf8mb4 NULL,
    `UpdatedBy` longtext CHARACTER SET utf8mb4 NULL,
    `DeletedBy` longtext CHARACTER SET utf8mb4 NULL,
    `IsDeleted` tinyint(1) NOT NULL,
    `IsActive` tinyint(1) NOT NULL,
    `CompanyId` int NOT NULL,
    CONSTRAINT `PK_ka_appannouncements` PRIMARY KEY (`Id`),
    CONSTRAINT `FK_ka_appannouncements_ka_companies_CompanyId` FOREIGN KEY (`CompanyId`) REFERENCES `ka_companies` (`Id`) ON DELETE CASCADE
) CHARACTER SET=utf8mb4;

CREATE TABLE IF NOT EXISTS `ka_appannouncementdeliveries` (
    `Id` int NOT NULL AUTO_INCREMENT,
    `AnnouncementId` int NOT NULL,
    `Phone` varchar(30) CHARACTER SET utf8mb4 NOT NULL,
    `UserId` longtext CHARACTER SET utf8mb4 NULL,
    `SentOn` datetime(6) NOT NULL,
    `Success` tinyint(1) NOT NULL,
    `Error` longtext CHARACTER SET utf8mb4 NULL,
    `Uuid` longtext CHARACTER SET utf8mb4 NULL,
    `CreatedAt` datetime(6) NOT NULL,
    `UpdatedAt` datetime(6) NULL,
    `DeletedAt` datetime(6) NULL,
    `CreatedBy` longtext CHARACTER SET utf8mb4 NULL,
    `UpdatedBy` longtext CHARACTER SET utf8mb4 NULL,
    `DeletedBy` longtext CHARACTER SET utf8mb4 NULL,
    `IsDeleted` tinyint(1) NOT NULL,
    `IsActive` tinyint(1) NOT NULL,
    `CompanyId` int NOT NULL,
    CONSTRAINT `PK_ka_appannouncementdeliveries` PRIMARY KEY (`Id`),
    CONSTRAINT `FK_ka_appannouncementdeliveries_ka_companies_CompanyId` FOREIGN KEY (`CompanyId`) REFERENCES `ka_companies` (`Id`) ON DELETE CASCADE
) CHARACTER SET=utf8mb4;

DROP PROCEDURE IF EXISTS _dog_add_idx;
DELIMITER $$
CREATE PROCEDURE _dog_add_idx(IN t VARCHAR(64), IN i VARCHAR(64), IN cols TEXT, IN uniq INT)
BEGIN
  IF NOT EXISTS (SELECT 1 FROM information_schema.statistics WHERE table_schema = DATABASE() AND table_name = t AND index_name = i) THEN
    SET @s = CONCAT('CREATE ', IF(uniq = 1, 'UNIQUE ', ''), 'INDEX `', i, '` ON `', t, '` (', cols, ')');
    PREPARE st FROM @s; EXECUTE st; DEALLOCATE PREPARE st;
  END IF;
END$$
DELIMITER ;
CALL _dog_add_idx('ka_appannouncements', 'IX_ka_appannouncements_CompanyId', '`CompanyId`', 0);
CALL _dog_add_idx('ka_appannouncements', 'IX_ka_appannouncements_Key', '`Key`', 1);
CALL _dog_add_idx('ka_appannouncementdeliveries', 'IX_ka_appannouncementdeliveries_CompanyId', '`CompanyId`', 0);
CALL _dog_add_idx('ka_appannouncementdeliveries', 'IX_ka_appannouncementdeliveries_AnnouncementId_Phone_SentOn', '`AnnouncementId`, `Phone`, `SentOn`', 0);
DROP PROCEDURE IF EXISTS _dog_add_idx;

-- -----------------------------------------------------------------------------------------------------
-- The announcement: WhatsApp once a day for 4 days (09:00-18:00 Harare time), portal banner for 3 days.
-- It starts today if it is before 16:00 in Harare, otherwise tomorrow. To change the dates later:
--   UPDATE ka_appannouncements SET StartDate = '2026-11-21' WHERE `Key` = 'mobile-app-apkpure-1.1.0';
-- To stop it at once:  UPDATE ka_appannouncements SET IsActive = 0 WHERE `Key` = 'mobile-app-apkpure-1.1.0';
-- -----------------------------------------------------------------------------------------------------
SET @local_now := DATE_ADD(UTC_TIMESTAMP(6), INTERVAL 2 HOUR);
INSERT INTO ka_appannouncements (`Key`, Title, Message, PortalMessage, LinkUrl, LinkText, StartDate, WhatsAppDays, PortalDays, SendFromHour, SendToHour,
                                 Uuid, CreatedAt, IsDeleted, IsActive, CompanyId)
SELECT 'mobile-app-apkpure-1.1.0',
       'New Black Eagle mobile app',
       CONCAT('📱 *Black Eagle – new mobile app*', CHAR(10), CHAR(10),
              'Please download the latest Black Eagle app from APKPure:', CHAR(10),
              'https://apkpure.com/p/com.blackeagle.black_eagle', CHAR(10), CHAR(10),
              '⚠️ *Before you install it, uninstall the version that is on your phone now* (Settings → Apps → Black Eagle → Uninstall). ',
              'Then install the new one and sign in with your usual login.', CHAR(10), CHAR(10), 'Thank you.'),
       'A new Black Eagle mobile app is available. Uninstall the version on your phone first, then install the new one from APKPure.',
       'https://apkpure.com/p/com.blackeagle.black_eagle', 'Download on APKPure',
       IF(HOUR(@local_now) < 16, DATE(@local_now), DATE_ADD(DATE(@local_now), INTERVAL 1 DAY)),
       4, 3, 9, 18, UUID(), UTC_TIMESTAMP(6), 0, 1, (SELECT MIN(Id) FROM ka_companies)
FROM DUAL
WHERE NOT EXISTS (SELECT 1 FROM ka_appannouncements WHERE `Key` = 'mobile-app-apkpure-1.1.0')
  AND EXISTS (SELECT 1 FROM ka_companies);

-- -----------------------------------------------------------------------------------------------------
-- Dog Handler role: Dog Training menu + the permissions a handler needs
-- -----------------------------------------------------------------------------------------------------
INSERT INTO ka_rolemenus (RoleId, SystemModuleId, Uuid, CreatedAt, IsDeleted, IsActive)
SELECT r.Id, m.Id, UUID(), UTC_TIMESTAMP(6), 0, 1
FROM AspNetRoles r JOIN ka_systemmodules m ON m.AccessName = 'dogtraining'
WHERE r.Name = 'Dog Handler'
  AND NOT EXISTS (SELECT 1 FROM ka_rolemenus rm WHERE rm.RoleId = r.Id AND rm.SystemModuleId = m.Id);

INSERT INTO ka_rolepermissions (RoleId, PermissionId, Uuid, CreatedAt, IsDeleted, IsActive)
SELECT r.Id, MIN(p.Id), UUID(), UTC_TIMESTAMP(6), 0, 1
FROM AspNetRoles r
JOIN ka_permissions p ON p.Name IN ('training.dashboard.view', 'training.enrolment.view', 'training.log.view', 'training.log.record',
                                    'training.progress.view', 'training.progress.submit', 'training.assessment.view')
WHERE r.Name = 'Dog Handler'
  AND NOT EXISTS (SELECT 1 FROM ka_rolepermissions rp WHERE rp.RoleId = r.Id AND rp.PermissionId = p.Id)
GROUP BY r.Id, p.Name;

-- CHECK (optional)
SELECT `Key`, StartDate, WhatsAppDays, PortalDays, IsActive FROM ka_appannouncements;
SELECT p.Name AS handler_permission FROM ka_rolepermissions rp JOIN AspNetRoles r ON r.Id = rp.RoleId AND r.Name = 'Dog Handler'
  JOIN ka_permissions p ON p.Id = rp.PermissionId WHERE p.Name LIKE 'training.%' ORDER BY p.Name;
