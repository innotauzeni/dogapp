-- =====================================================================================================
-- BILLING UPGRADE - LIVE DATABASE (MySQL 8 / MariaDB)
--
-- Customer companies, proper payment ledger (cash / EcoCash / bank transfer with traceable references ...),
-- company tax / VAT / fiscal details, email settings + log, invoice item catalogue, new permissions and menus.
--
--   mysql -u <user> -p <database> < Billing_LiveUpgrade.sql
--
-- ORDER: if this database does not have the Dog Training module yet, run DogTraining_LiveSetup.sql FIRST,
-- then this file (the training columns are skipped when the training tables are missing).
--
-- Safe to run more than once: every statement checks first and nothing is dropped, renamed or deleted.
-- Back the database up first anyway. Then deploy the new API + Portal and restart both.
--
-- Existing invoices and training payments keep working: their payments are marked CONFIRMED, and the
-- invoice totals are unchanged.
-- Users must log out and in again to see the new menu entries.
-- =====================================================================================================

SET NAMES utf8mb4;

DROP PROCEDURE IF EXISTS _dog_add_col;
DROP PROCEDURE IF EXISTS _dog_add_idx;
DROP PROCEDURE IF EXISTS _dog_add_fk;
DELIMITER $$
CREATE PROCEDURE _dog_add_col(IN t VARCHAR(64), IN c VARCHAR(64), IN ddl TEXT)
BEGIN
  IF EXISTS (SELECT 1 FROM information_schema.tables WHERE table_schema = DATABASE() AND table_name = t)
     AND NOT EXISTS (SELECT 1 FROM information_schema.columns WHERE table_schema = DATABASE() AND table_name = t AND column_name = c) THEN
    SET @s = CONCAT('ALTER TABLE `', t, '` ADD COLUMN `', c, '` ', ddl);
    PREPARE st FROM @s; EXECUTE st; DEALLOCATE PREPARE st;
  END IF;
END$$
CREATE PROCEDURE _dog_add_idx(IN t VARCHAR(64), IN i VARCHAR(64), IN cols TEXT, IN uniq INT)
BEGIN
  IF NOT EXISTS (SELECT 1 FROM information_schema.statistics WHERE table_schema = DATABASE() AND table_name = t AND index_name = i) THEN
    SET @s = CONCAT('CREATE ', IF(uniq = 1, 'UNIQUE ', ''), 'INDEX `', i, '` ON `', t, '` (', cols, ')');
    PREPARE st FROM @s; EXECUTE st; DEALLOCATE PREPARE st;
  END IF;
END$$
CREATE PROCEDURE _dog_add_fk(IN t VARCHAR(64), IN fk VARCHAR(64), IN def TEXT)
BEGIN
  IF NOT EXISTS (SELECT 1 FROM information_schema.table_constraints WHERE table_schema = DATABASE() AND table_name = t AND constraint_name = fk) THEN
    SET @s = CONCAT('ALTER TABLE `', t, '` ADD CONSTRAINT `', fk, '` ', def);
    PREPARE st FROM @s; EXECUTE st; DEALLOCATE PREPARE st;
  END IF;
END$$
DELIMITER ;

-- -----------------------------------------------------------------------------------------------------
-- PART 1 - NEW TABLES
-- -----------------------------------------------------------------------------------------------------

CREATE TABLE IF NOT EXISTS `ka_customercompanies` (
    `Id` int NOT NULL AUTO_INCREMENT,
    `Name` longtext CHARACTER SET utf8mb4 NOT NULL,
    `TradingName` longtext CHARACTER SET utf8mb4 NULL,
    `RegistrationNumber` longtext CHARACTER SET utf8mb4 NULL,
    `TaxNumber` longtext CHARACTER SET utf8mb4 NULL,
    `VatNumber` longtext CHARACTER SET utf8mb4 NULL,
    `Email` longtext CHARACTER SET utf8mb4 NULL,
    `BillingEmail` longtext CHARACTER SET utf8mb4 NULL,
    `Phone` longtext CHARACTER SET utf8mb4 NULL,
    `Website` longtext CHARACTER SET utf8mb4 NULL,
    `Address` longtext CHARACTER SET utf8mb4 NULL,
    `City` longtext CHARACTER SET utf8mb4 NULL,
    `Country` longtext CHARACTER SET utf8mb4 NULL,
    `ContactPerson` longtext CHARACTER SET utf8mb4 NULL,
    `PaymentTermsDays` int NOT NULL,
    `Notes` longtext CHARACTER SET utf8mb4 NULL,
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
    CONSTRAINT `PK_ka_customercompanies` PRIMARY KEY (`Id`),
    CONSTRAINT `FK_ka_customercompanies_ka_companies_CompanyId` FOREIGN KEY (`CompanyId`) REFERENCES `ka_companies` (`Id`) ON DELETE CASCADE
) CHARACTER SET=utf8mb4;

CREATE TABLE IF NOT EXISTS `ka_invoicepayments` (
    `Id` int NOT NULL AUTO_INCREMENT,
    `InvoiceId` int NOT NULL,
    `PaymentNumber` varchar(40) CHARACTER SET utf8mb4 NOT NULL,
    `PaymentDate` datetime(6) NOT NULL,
    `ValueDate` datetime(6) NULL,
    `Amount` decimal(18,2) NOT NULL,
    `CurrencyCode` longtext CHARACTER SET utf8mb4 NOT NULL,
    `Method` varchar(20) CHARACTER SET utf8mb4 NOT NULL,
    `Status` varchar(20) CHARACTER SET utf8mb4 NOT NULL,
    `PayerName` longtext CHARACTER SET utf8mb4 NULL,
    `PayerContact` longtext CHARACTER SET utf8mb4 NULL,
    `PayerBank` longtext CHARACTER SET utf8mb4 NULL,
    `PayerAccount` longtext CHARACTER SET utf8mb4 NULL,
    `DestinationBankAccountId` int NULL,
    `ReferenceNumber` longtext CHARACTER SET utf8mb4 NULL,
    `ChequeNumber` longtext CHARACTER SET utf8mb4 NULL,
    `ProofUrl` longtext CHARACTER SET utf8mb4 NULL,
    `Notes` longtext CHARACTER SET utf8mb4 NULL,
    `ReceivedByUserId` longtext CHARACTER SET utf8mb4 NULL,
    `ReceivedByName` longtext CHARACTER SET utf8mb4 NULL,
    `ConfirmedByUserId` longtext CHARACTER SET utf8mb4 NULL,
    `ConfirmedByName` longtext CHARACTER SET utf8mb4 NULL,
    `ConfirmedAt` datetime(6) NULL,
    `StatementReference` longtext CHARACTER SET utf8mb4 NULL,
    `RejectReason` longtext CHARACTER SET utf8mb4 NULL,
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
    CONSTRAINT `PK_ka_invoicepayments` PRIMARY KEY (`Id`),
    CONSTRAINT `FK_ka_invoicepayments_ka_companies_CompanyId` FOREIGN KEY (`CompanyId`) REFERENCES `ka_companies` (`Id`) ON DELETE CASCADE,
    CONSTRAINT `FK_ka_invoicepayments_ka_invoices_InvoiceId` FOREIGN KEY (`InvoiceId`) REFERENCES `ka_invoices` (`Id`) ON DELETE RESTRICT
) CHARACTER SET=utf8mb4;

CREATE TABLE IF NOT EXISTS `ka_emailsettings` (
    `Id` int NOT NULL AUTO_INCREMENT,
    `Host` longtext CHARACTER SET utf8mb4 NOT NULL,
    `Port` int NOT NULL,
    `Security` longtext CHARACTER SET utf8mb4 NOT NULL,
    `Username` longtext CHARACTER SET utf8mb4 NULL,
    `PasswordEncrypted` longtext CHARACTER SET utf8mb4 NULL,
    `FromAddress` longtext CHARACTER SET utf8mb4 NOT NULL,
    `FromName` longtext CHARACTER SET utf8mb4 NULL,
    `ReplyTo` longtext CHARACTER SET utf8mb4 NULL,
    `BccAddress` longtext CHARACTER SET utf8mb4 NULL,
    `Signature` longtext CHARACTER SET utf8mb4 NULL,
    `LastTestAt` datetime(6) NULL,
    `LastTestOk` tinyint(1) NULL,
    `LastTestMessage` longtext CHARACTER SET utf8mb4 NULL,
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
    CONSTRAINT `PK_ka_emailsettings` PRIMARY KEY (`Id`),
    CONSTRAINT `FK_ka_emailsettings_ka_companies_CompanyId` FOREIGN KEY (`CompanyId`) REFERENCES `ka_companies` (`Id`) ON DELETE CASCADE
) CHARACTER SET=utf8mb4;

CREATE TABLE IF NOT EXISTS `ka_emaillogs` (
    `Id` int NOT NULL AUTO_INCREMENT,
    `ToAddresses` longtext CHARACTER SET utf8mb4 NOT NULL,
    `CcAddresses` longtext CHARACTER SET utf8mb4 NULL,
    `Subject` longtext CHARACTER SET utf8mb4 NOT NULL,
    `RelatedType` longtext CHARACTER SET utf8mb4 NULL,
    `RelatedId` int NULL,
    `Success` tinyint(1) NOT NULL,
    `Error` longtext CHARACTER SET utf8mb4 NULL,
    `SentByUserId` longtext CHARACTER SET utf8mb4 NULL,
    `SentByName` longtext CHARACTER SET utf8mb4 NULL,
    `HasAttachment` tinyint(1) NOT NULL,
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
    CONSTRAINT `PK_ka_emaillogs` PRIMARY KEY (`Id`),
    CONSTRAINT `FK_ka_emaillogs_ka_companies_CompanyId` FOREIGN KEY (`CompanyId`) REFERENCES `ka_companies` (`Id`) ON DELETE CASCADE
) CHARACTER SET=utf8mb4;

CREATE TABLE IF NOT EXISTS `ka_invoicecatalog` (
    `Id` int NOT NULL AUTO_INCREMENT,
    `Name` longtext CHARACTER SET utf8mb4 NOT NULL,
    `Description` longtext CHARACTER SET utf8mb4 NULL,
    `UnitPrice` decimal(18,2) NOT NULL,
    `TaxRate` decimal(5,2) NOT NULL,
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
    CONSTRAINT `PK_ka_invoicecatalog` PRIMARY KEY (`Id`),
    CONSTRAINT `FK_ka_invoicecatalog_ka_companies_CompanyId` FOREIGN KEY (`CompanyId`) REFERENCES `ka_companies` (`Id`) ON DELETE CASCADE
) CHARACTER SET=utf8mb4;

-- -----------------------------------------------------------------------------------------------------
-- PART 2 - NEW COLUMNS ON EXISTING TABLES
-- -----------------------------------------------------------------------------------------------------

-- ka_companies
CALL _dog_add_col('ka_companies', 'RegistrationNumber', 'longtext NULL');
CALL _dog_add_col('ka_companies', 'TaxNumber', 'longtext NULL');
CALL _dog_add_col('ka_companies', 'VatNumber', 'longtext NULL');
CALL _dog_add_col('ka_companies', 'InvoiceTerms', 'longtext NULL');
CALL _dog_add_col('ka_companies', 'InvoiceFooter', 'longtext NULL');
CALL _dog_add_col('ka_companies', 'DefaultVatRate', 'decimal(5,2) NOT NULL DEFAULT 0');
CALL _dog_add_col('ka_companies', 'FiscalEnabled', 'tinyint(1) NOT NULL DEFAULT 0');
CALL _dog_add_col('ka_companies', 'FiscalDeviceSerial', 'longtext NULL');

-- ka_customers
CALL _dog_add_col('ka_customers', 'JobTitle', 'longtext NULL');
CALL _dog_add_col('ka_customers', 'CustomerCompanyId', 'int NULL');

-- ka_invoices
CALL _dog_add_col('ka_invoices', 'CustomerCompanyId', 'int NULL');
CALL _dog_add_col('ka_invoices', 'BillToName', 'longtext NULL');
CALL _dog_add_col('ka_invoices', 'BillToAddress', 'longtext NULL');
CALL _dog_add_col('ka_invoices', 'BillToTaxNumber', 'longtext NULL');
CALL _dog_add_col('ka_invoices', 'BillToVatNumber', 'longtext NULL');
CALL _dog_add_col('ka_invoices', 'BillToEmail', 'longtext NULL');
CALL _dog_add_col('ka_invoices', 'BillToPhone', 'longtext NULL');
CALL _dog_add_col('ka_invoices', 'ContactPerson', 'longtext NULL');
CALL _dog_add_col('ka_invoices', 'PurchaseOrderNumber', 'longtext NULL');
CALL _dog_add_col('ka_invoices', 'CurrencyCode', 'varchar(10) NOT NULL DEFAULT ''USD''');
CALL _dog_add_col('ka_invoices', 'IsFiscal', 'tinyint(1) NOT NULL DEFAULT 0');
CALL _dog_add_col('ka_invoices', 'FiscalInvoiceNumber', 'longtext NULL');
CALL _dog_add_col('ka_invoices', 'FiscalDeviceSerial', 'longtext NULL');
CALL _dog_add_col('ka_invoices', 'FiscalVerificationCode', 'longtext NULL');
CALL _dog_add_col('ka_invoices', 'FiscalDate', 'datetime(6) NULL');
CALL _dog_add_col('ka_invoices', 'EmailedAt', 'datetime(6) NULL');
CALL _dog_add_col('ka_invoices', 'EmailedTo', 'longtext NULL');

-- ka_dogtraininginvoices
CALL _dog_add_col('ka_dogtraininginvoices', 'EmailedAt', 'datetime(6) NULL');
CALL _dog_add_col('ka_dogtraininginvoices', 'EmailedTo', 'longtext NULL');

-- ka_dogtrainingpayments
CALL _dog_add_col('ka_dogtrainingpayments', 'ValueDate', 'datetime(6) NULL');
CALL _dog_add_col('ka_dogtrainingpayments', 'CurrencyCode', 'varchar(10) NOT NULL DEFAULT ''USD''');
CALL _dog_add_col('ka_dogtrainingpayments', 'Status', 'varchar(20) NOT NULL DEFAULT ''CONFIRMED''');
CALL _dog_add_col('ka_dogtrainingpayments', 'PayerName', 'longtext NULL');
CALL _dog_add_col('ka_dogtrainingpayments', 'PayerContact', 'longtext NULL');
CALL _dog_add_col('ka_dogtrainingpayments', 'PayerBank', 'longtext NULL');
CALL _dog_add_col('ka_dogtrainingpayments', 'PayerAccount', 'longtext NULL');
CALL _dog_add_col('ka_dogtrainingpayments', 'DestinationBankAccountId', 'int NULL');
CALL _dog_add_col('ka_dogtrainingpayments', 'ChequeNumber', 'longtext NULL');
CALL _dog_add_col('ka_dogtrainingpayments', 'ProofUrl', 'longtext NULL');
CALL _dog_add_col('ka_dogtrainingpayments', 'ConfirmedByUserId', 'longtext NULL');
CALL _dog_add_col('ka_dogtrainingpayments', 'ConfirmedByName', 'longtext NULL');
CALL _dog_add_col('ka_dogtrainingpayments', 'ConfirmedAt', 'datetime(6) NULL');
CALL _dog_add_col('ka_dogtrainingpayments', 'StatementReference', 'longtext NULL');
CALL _dog_add_col('ka_dogtrainingpayments', 'RejectReason', 'longtext NULL');

-- Indexes and foreign keys
CALL _dog_add_idx('ka_customercompanies', 'IX_ka_customercompanies_CompanyId', '`CompanyId`', 0);
CALL _dog_add_idx('ka_invoicepayments', 'IX_ka_invoicepayments_CompanyId', '`CompanyId`', 0);
CALL _dog_add_idx('ka_invoicepayments', 'IX_ka_invoicepayments_InvoiceId', '`InvoiceId`', 0);
CALL _dog_add_idx('ka_invoicepayments', 'IX_ka_invoicepayments_PaymentNumber', '`PaymentNumber`', 1);
CALL _dog_add_idx('ka_emailsettings', 'IX_ka_emailsettings_CompanyId', '`CompanyId`', 0);
CALL _dog_add_idx('ka_emaillogs', 'IX_ka_emaillogs_CompanyId', '`CompanyId`', 0);
CALL _dog_add_idx('ka_invoicecatalog', 'IX_ka_invoicecatalog_CompanyId', '`CompanyId`', 0);
CALL _dog_add_idx('ka_customers', 'IX_ka_customers_CustomerCompanyId', '`CustomerCompanyId`', 0);
CALL _dog_add_fk('ka_customers', 'FK_ka_customers_ka_customercompanies_CustomerCompanyId',
  'FOREIGN KEY (`CustomerCompanyId`) REFERENCES `ka_customercompanies` (`Id`) ON DELETE SET NULL');

-- Payments recorded before this upgrade were all received money: mark them CONFIRMED (the column default does this already).
-- (training tables only exist once DogTraining_LiveSetup.sql has been run; the column default already marks old payments CONFIRMED)

DROP PROCEDURE IF EXISTS _dog_add_col;
DROP PROCEDURE IF EXISTS _dog_add_idx;
DROP PROCEDURE IF EXISTS _dog_add_fk;

-- -----------------------------------------------------------------------------------------------------
-- PART 3 - MENUS AND PERMISSIONS
-- -----------------------------------------------------------------------------------------------------

-- Customer Companies: next to the Customers menu (same module)
INSERT INTO ka_submodules (Name, ModuleId, Icon, Route, AccessName, `Order`, Uuid, CreatedAt, IsDeleted, IsActive)
SELECT 'Customer Companies', c.ModuleId, 'bi-buildings', 'CustomerCompanies', 'customercompany.view', c.`Order` + 1, UUID(), UTC_TIMESTAMP(6), 0, 1
FROM ka_submodules c
WHERE c.AccessName = 'customer.view'
  AND NOT EXISTS (SELECT 1 FROM ka_submodules WHERE AccessName = 'customercompany.view')
LIMIT 1;

-- Billing & Email settings: under Configuration
INSERT INTO ka_submodules (Name, ModuleId, Icon, Route, AccessName, `Order`, Uuid, CreatedAt, IsDeleted, IsActive)
SELECT 'Billing & Email', m.Id, 'bi-envelope-paper', 'BillingSettings/Index', 'billingsettings.view', 6, UUID(), UTC_TIMESTAMP(6), 0, 1
FROM ka_systemmodules m
WHERE m.AccessName = 'configuration'
  AND NOT EXISTS (SELECT 1 FROM ka_submodules WHERE AccessName = 'billingsettings.view')
LIMIT 1;

INSERT INTO ka_permissions (Name, DisplayName, Module, Uuid, CreatedAt, IsDeleted, IsActive)
SELECT 'customercompany.view', 'View Customer Companies', 'Managements', UUID(), UTC_TIMESTAMP(6), 0, 1 FROM DUAL
WHERE NOT EXISTS (SELECT 1 FROM ka_permissions WHERE Name = 'customercompany.view');

INSERT INTO ka_permissions (Name, DisplayName, Module, Uuid, CreatedAt, IsDeleted, IsActive)
SELECT 'customercompany.create', 'Create Customer Company', 'Managements', UUID(), UTC_TIMESTAMP(6), 0, 1 FROM DUAL
WHERE NOT EXISTS (SELECT 1 FROM ka_permissions WHERE Name = 'customercompany.create');

INSERT INTO ka_permissions (Name, DisplayName, Module, Uuid, CreatedAt, IsDeleted, IsActive)
SELECT 'customercompany.edit', 'Edit Customer Company', 'Managements', UUID(), UTC_TIMESTAMP(6), 0, 1 FROM DUAL
WHERE NOT EXISTS (SELECT 1 FROM ka_permissions WHERE Name = 'customercompany.edit');

INSERT INTO ka_permissions (Name, DisplayName, Module, Uuid, CreatedAt, IsDeleted, IsActive)
SELECT 'customercompany.delete', 'Delete Customer Company', 'Managements', UUID(), UTC_TIMESTAMP(6), 0, 1 FROM DUAL
WHERE NOT EXISTS (SELECT 1 FROM ka_permissions WHERE Name = 'customercompany.delete');

INSERT INTO ka_permissions (Name, DisplayName, Module, Uuid, CreatedAt, IsDeleted, IsActive)
SELECT 'invoice.verifypayment', 'Confirm / Reject Bank & Cheque Payments', 'Finance', UUID(), UTC_TIMESTAMP(6), 0, 1 FROM DUAL
WHERE NOT EXISTS (SELECT 1 FROM ka_permissions WHERE Name = 'invoice.verifypayment');

INSERT INTO ka_permissions (Name, DisplayName, Module, Uuid, CreatedAt, IsDeleted, IsActive)
SELECT 'invoice.email', 'Email Invoices', 'Finance', UUID(), UTC_TIMESTAMP(6), 0, 1 FROM DUAL
WHERE NOT EXISTS (SELECT 1 FROM ka_permissions WHERE Name = 'invoice.email');

INSERT INTO ka_permissions (Name, DisplayName, Module, Uuid, CreatedAt, IsDeleted, IsActive)
SELECT 'billingsettings.view', 'View Billing & Email Settings', 'Configuration', UUID(), UTC_TIMESTAMP(6), 0, 1 FROM DUAL
WHERE NOT EXISTS (SELECT 1 FROM ka_permissions WHERE Name = 'billingsettings.view');

INSERT INTO ka_permissions (Name, DisplayName, Module, Uuid, CreatedAt, IsDeleted, IsActive)
SELECT 'billingsettings.edit', 'Edit Billing & Email Settings', 'Configuration', UUID(), UTC_TIMESTAMP(6), 0, 1 FROM DUAL
WHERE NOT EXISTS (SELECT 1 FROM ka_permissions WHERE Name = 'billingsettings.edit');

INSERT INTO ka_permissions (Name, DisplayName, Module, Uuid, CreatedAt, IsDeleted, IsActive)
SELECT 'training.payment.verify', 'Confirm / Reject Training Bank & Cheque Payments', 'Dog Training', UUID(), UTC_TIMESTAMP(6), 0, 1 FROM DUAL
WHERE NOT EXISTS (SELECT 1 FROM ka_permissions WHERE Name = 'training.payment.verify');

INSERT INTO ka_permissions (Name, DisplayName, Module, Uuid, CreatedAt, IsDeleted, IsActive)
SELECT 'training.invoice.email', 'Email Training Invoices', 'Dog Training', UUID(), UTC_TIMESTAMP(6), 0, 1 FROM DUAL
WHERE NOT EXISTS (SELECT 1 FROM ka_permissions WHERE Name = 'training.invoice.email');

-- SystemOwner and Admin get every new permission
INSERT INTO ka_rolepermissions (RoleId, PermissionId, Uuid, CreatedAt, IsDeleted, IsActive)
SELECT r.Id, p.Id, UUID(), UTC_TIMESTAMP(6), 0, 1
FROM AspNetRoles r JOIN ka_permissions p ON p.Name IN ('customercompany.view', 'customercompany.create', 'customercompany.edit', 'customercompany.delete', 'invoice.verifypayment', 'invoice.email', 'billingsettings.view', 'billingsettings.edit', 'training.payment.verify', 'training.invoice.email')
WHERE r.Name IN ('SystemOwner', 'Admin')
  AND NOT EXISTS (SELECT 1 FROM ka_rolepermissions rp WHERE rp.RoleId = r.Id AND rp.PermissionId = p.Id);

-- Everyone who could already manage customers / invoices keeps working (bank-payment verification is NOT given automatically)
INSERT INTO ka_rolepermissions (RoleId, PermissionId, Uuid, CreatedAt, IsDeleted, IsActive)
SELECT DISTINCT rp.RoleId, g.Id, UUID(), UTC_TIMESTAMP(6), 0, 1
FROM ka_rolepermissions rp
JOIN ka_permissions h ON h.Id = rp.PermissionId AND h.Name = 'customer.view'
JOIN ka_permissions g ON g.Name = 'customercompany.view'
WHERE NOT EXISTS (SELECT 1 FROM ka_rolepermissions x WHERE x.RoleId = rp.RoleId AND x.PermissionId = g.Id);

INSERT INTO ka_rolepermissions (RoleId, PermissionId, Uuid, CreatedAt, IsDeleted, IsActive)
SELECT DISTINCT rp.RoleId, g.Id, UUID(), UTC_TIMESTAMP(6), 0, 1
FROM ka_rolepermissions rp
JOIN ka_permissions h ON h.Id = rp.PermissionId AND h.Name = 'customer.create'
JOIN ka_permissions g ON g.Name = 'customercompany.create'
WHERE NOT EXISTS (SELECT 1 FROM ka_rolepermissions x WHERE x.RoleId = rp.RoleId AND x.PermissionId = g.Id);

INSERT INTO ka_rolepermissions (RoleId, PermissionId, Uuid, CreatedAt, IsDeleted, IsActive)
SELECT DISTINCT rp.RoleId, g.Id, UUID(), UTC_TIMESTAMP(6), 0, 1
FROM ka_rolepermissions rp
JOIN ka_permissions h ON h.Id = rp.PermissionId AND h.Name = 'customer.edit'
JOIN ka_permissions g ON g.Name = 'customercompany.edit'
WHERE NOT EXISTS (SELECT 1 FROM ka_rolepermissions x WHERE x.RoleId = rp.RoleId AND x.PermissionId = g.Id);

INSERT INTO ka_rolepermissions (RoleId, PermissionId, Uuid, CreatedAt, IsDeleted, IsActive)
SELECT DISTINCT rp.RoleId, g.Id, UUID(), UTC_TIMESTAMP(6), 0, 1
FROM ka_rolepermissions rp
JOIN ka_permissions h ON h.Id = rp.PermissionId AND h.Name = 'customer.delete'
JOIN ka_permissions g ON g.Name = 'customercompany.delete'
WHERE NOT EXISTS (SELECT 1 FROM ka_rolepermissions x WHERE x.RoleId = rp.RoleId AND x.PermissionId = g.Id);

INSERT INTO ka_rolepermissions (RoleId, PermissionId, Uuid, CreatedAt, IsDeleted, IsActive)
SELECT DISTINCT rp.RoleId, g.Id, UUID(), UTC_TIMESTAMP(6), 0, 1
FROM ka_rolepermissions rp
JOIN ka_permissions h ON h.Id = rp.PermissionId AND h.Name = 'invoice.edit'
JOIN ka_permissions g ON g.Name = 'invoice.email'
WHERE NOT EXISTS (SELECT 1 FROM ka_rolepermissions x WHERE x.RoleId = rp.RoleId AND x.PermissionId = g.Id);

INSERT INTO ka_rolepermissions (RoleId, PermissionId, Uuid, CreatedAt, IsDeleted, IsActive)
SELECT DISTINCT rp.RoleId, g.Id, UUID(), UTC_TIMESTAMP(6), 0, 1
FROM ka_rolepermissions rp
JOIN ka_permissions h ON h.Id = rp.PermissionId AND h.Name = 'training.invoice.generate'
JOIN ka_permissions g ON g.Name = 'training.invoice.email'
WHERE NOT EXISTS (SELECT 1 FROM ka_rolepermissions x WHERE x.RoleId = rp.RoleId AND x.PermissionId = g.Id);

-- -----------------------------------------------------------------------------------------------------
-- CHECK (optional)
-- -----------------------------------------------------------------------------------------------------
SELECT COUNT(*) AS new_billing_tables FROM information_schema.tables
 WHERE table_schema = DATABASE() AND table_name IN ('ka_customercompanies','ka_invoicepayments','ka_emailsettings','ka_emaillogs','ka_invoicecatalog');
SELECT COUNT(*) AS new_permissions FROM ka_permissions WHERE Name IN ('customercompany.view', 'customercompany.create', 'customercompany.edit', 'customercompany.delete', 'invoice.verifypayment', 'invoice.email', 'billingsettings.view', 'billingsettings.edit', 'training.payment.verify', 'training.invoice.email');
SELECT s.Name AS new_menu, s.Route, m.Name AS module FROM ka_submodules s JOIN ka_systemmodules m ON m.Id = s.ModuleId
 WHERE s.AccessName IN ('customercompany.view', 'billingsettings.view');
