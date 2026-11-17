-- =====================================================================================================
-- DOG TRAINING MODULE - LIVE DATABASE SETUP (MySQL / MariaDB)
--
-- Run this ONE file on the live database, then deploy the new API + Portal and restart them.
--   mysql -u <user> -p <database> < DogTraining_LiveSetup.sql
--
--   Part 1  creates the 18 training tables (their own customers, dogs, invoices, payments, etc.)
--   Part 2  adds the "Dog Training" menu, its 13 submenus and 29 permissions
--   Part 3  gives the menu + permissions to the roles (see the role list below)
--
-- Safe to run more than once: every statement checks first, and nothing existing is dropped, changed or deleted.
-- It does not touch ka_dogs, ka_customers or the company Invoices module.
--
-- Roles that are set up automatically (matched by NAME; roles that do not exist are skipped):
--   SystemOwner, Admin ........ the menu + ALL training permissions
--   Dog Handler ............... the menu + log / progress-report permissions only (their own dogs)
-- Any other role (Manager, Staff, ...): tick the training permissions in Configuration > Roles when needed.
--
-- Users must log out and in again to see the new menu.
-- =====================================================================================================

SET NAMES utf8mb4;

-- -----------------------------------------------------------------------------------------------------
-- PART 1 - TABLES
-- -----------------------------------------------------------------------------------------------------

CREATE TABLE IF NOT EXISTS `ka_dogtrainingcustomers` (
    `Id` int NOT NULL AUTO_INCREMENT,
    `FullName` longtext CHARACTER SET utf8mb4 NOT NULL,
    `Email` longtext CHARACTER SET utf8mb4 NULL,
    `Phone` longtext CHARACTER SET utf8mb4 NULL,
    `Address` longtext CHARACTER SET utf8mb4 NULL,
    `IdNumber` longtext CHARACTER SET utf8mb4 NULL,
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
    CONSTRAINT `PK_ka_dogtrainingcustomers` PRIMARY KEY (`Id`),
    CONSTRAINT `FK_ka_dogtrainingcustomers_ka_companies_CompanyId` FOREIGN KEY (`CompanyId`) REFERENCES `ka_companies` (`Id`) ON DELETE CASCADE,
    INDEX `IX_ka_dogtrainingcustomers_CompanyId` (`CompanyId`)
) CHARACTER SET=utf8mb4;

CREATE TABLE IF NOT EXISTS `ka_dogtraininghandlerprofiles` (
    `Id` int NOT NULL AUTO_INCREMENT,
    `UserId` varchar(255) CHARACTER SET utf8mb4 NOT NULL,
    `MaxDogs` int NOT NULL,
    `Specialisation` longtext CHARACTER SET utf8mb4 NULL,
    `IsAvailable` tinyint(1) NOT NULL,
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
    CONSTRAINT `PK_ka_dogtraininghandlerprofiles` PRIMARY KEY (`Id`),
    CONSTRAINT `FK_ka_dogtraininghandlerprofiles_AspNetUsers_UserId` FOREIGN KEY (`UserId`) REFERENCES `AspNetUsers` (`Id`) ON DELETE RESTRICT,
    CONSTRAINT `FK_ka_dogtraininghandlerprofiles_ka_companies_CompanyId` FOREIGN KEY (`CompanyId`) REFERENCES `ka_companies` (`Id`) ON DELETE CASCADE,
    INDEX `IX_ka_dogtraininghandlerprofiles_CompanyId` (`CompanyId`),
    UNIQUE INDEX `IX_ka_dogtraininghandlerprofiles_UserId` (`UserId`)
) CHARACTER SET=utf8mb4;

CREATE TABLE IF NOT EXISTS `ka_dogtrainingprogrammes` (
    `Id` int NOT NULL AUTO_INCREMENT,
    `Name` longtext CHARACTER SET utf8mb4 NOT NULL,
    `Description` longtext CHARACTER SET utf8mb4 NULL,
    `DurationDays` int NOT NULL,
    `Fee` decimal(18,2) NOT NULL,
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
    CONSTRAINT `PK_ka_dogtrainingprogrammes` PRIMARY KEY (`Id`),
    CONSTRAINT `FK_ka_dogtrainingprogrammes_ka_companies_CompanyId` FOREIGN KEY (`CompanyId`) REFERENCES `ka_companies` (`Id`) ON DELETE CASCADE,
    INDEX `IX_ka_dogtrainingprogrammes_CompanyId` (`CompanyId`)
) CHARACTER SET=utf8mb4;

CREATE TABLE IF NOT EXISTS `ka_dogtrainingdogs` (
    `Id` int NOT NULL AUTO_INCREMENT,
    `CustomerId` int NOT NULL,
    `Name` longtext CHARACTER SET utf8mb4 NOT NULL,
    `Breed` longtext CHARACTER SET utf8mb4 NULL,
    `Color` longtext CHARACTER SET utf8mb4 NULL,
    `Gender` longtext CHARACTER SET utf8mb4 NULL,
    `DateOfBirth` datetime(6) NULL,
    `MicrochipNumber` longtext CHARACTER SET utf8mb4 NULL,
    `PhotoUrl` longtext CHARACTER SET utf8mb4 NULL,
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
    CONSTRAINT `PK_ka_dogtrainingdogs` PRIMARY KEY (`Id`),
    CONSTRAINT `FK_ka_dogtrainingdogs_ka_companies_CompanyId` FOREIGN KEY (`CompanyId`) REFERENCES `ka_companies` (`Id`) ON DELETE CASCADE,
    CONSTRAINT `FK_ka_dogtrainingdogs_ka_dogtrainingcustomers_CustomerId` FOREIGN KEY (`CustomerId`) REFERENCES `ka_dogtrainingcustomers` (`Id`) ON DELETE RESTRICT,
    INDEX `IX_ka_dogtrainingdogs_CompanyId` (`CompanyId`),
    INDEX `IX_ka_dogtrainingdogs_CustomerId` (`CustomerId`)
) CHARACTER SET=utf8mb4;

CREATE TABLE IF NOT EXISTS `ka_dogtraininginvoices` (
    `Id` int NOT NULL AUTO_INCREMENT,
    `InvoiceNumber` varchar(40) CHARACTER SET utf8mb4 NOT NULL,
    `CustomerId` int NOT NULL,
    `CustomerName` longtext CHARACTER SET utf8mb4 NULL,
    `CustomerEmail` longtext CHARACTER SET utf8mb4 NULL,
    `CustomerPhone` longtext CHARACTER SET utf8mb4 NULL,
    `InvoiceDate` datetime(6) NOT NULL,
    `DueDate` datetime(6) NOT NULL,
    `TotalAmount` decimal(18,2) NOT NULL,
    `PaidAmount` decimal(18,2) NOT NULL,
    `BalanceAmount` decimal(18,2) NOT NULL,
    `Status` varchar(20) CHARACTER SET utf8mb4 NOT NULL,
    `Notes` longtext CHARACTER SET utf8mb4 NULL,
    `Terms` longtext CHARACTER SET utf8mb4 NULL,
    `CreatedByName` longtext CHARACTER SET utf8mb4 NULL,
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
    CONSTRAINT `PK_ka_dogtraininginvoices` PRIMARY KEY (`Id`),
    CONSTRAINT `FK_ka_dogtraininginvoices_ka_companies_CompanyId` FOREIGN KEY (`CompanyId`) REFERENCES `ka_companies` (`Id`) ON DELETE CASCADE,
    CONSTRAINT `FK_ka_dogtraininginvoices_ka_dogtrainingcustomers_CustomerId` FOREIGN KEY (`CustomerId`) REFERENCES `ka_dogtrainingcustomers` (`Id`) ON DELETE RESTRICT,
    INDEX `IX_ka_dogtraininginvoices_CompanyId` (`CompanyId`),
    INDEX `IX_ka_dogtraininginvoices_CustomerId` (`CustomerId`),
    UNIQUE INDEX `IX_ka_dogtraininginvoices_InvoiceNumber` (`InvoiceNumber`)
) CHARACTER SET=utf8mb4;

CREATE TABLE IF NOT EXISTS `ka_dogtrainingprogrammeactivities` (
    `Id` int NOT NULL AUTO_INCREMENT,
    `ProgrammeId` int NOT NULL,
    `WeekNumber` int NOT NULL,
    `TimeOfDay` varchar(10) CHARACTER SET utf8mb4 NOT NULL,
    `Title` longtext CHARACTER SET utf8mb4 NOT NULL,
    `Purpose` longtext CHARACTER SET utf8mb4 NULL,
    `SortOrder` int NOT NULL,
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
    CONSTRAINT `PK_ka_dogtrainingprogrammeactivities` PRIMARY KEY (`Id`),
    CONSTRAINT `FK_ka_dogtrainingprogrammeactivities_ka_companies_CompanyId` FOREIGN KEY (`CompanyId`) REFERENCES `ka_companies` (`Id`) ON DELETE CASCADE,
    CONSTRAINT `FK_ka_dogtrainingprogrammeactivities_ka_dogtrainingprogrammes_P~` FOREIGN KEY (`ProgrammeId`) REFERENCES `ka_dogtrainingprogrammes` (`Id`) ON DELETE CASCADE,
    INDEX `IX_ka_dogtrainingprogrammeactivities_CompanyId` (`CompanyId`),
    INDEX `IX_ka_dogtrainingprogrammeactivities_ProgrammeId` (`ProgrammeId`)
) CHARACTER SET=utf8mb4;

CREATE TABLE IF NOT EXISTS `ka_dogtrainingprogrammeweeks` (
    `Id` int NOT NULL AUTO_INCREMENT,
    `ProgrammeId` int NOT NULL,
    `WeekNumber` int NOT NULL,
    `Title` longtext CHARACTER SET utf8mb4 NOT NULL,
    `Focus` longtext CHARACTER SET utf8mb4 NULL,
    `Targets` longtext CHARACTER SET utf8mb4 NULL,
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
    CONSTRAINT `PK_ka_dogtrainingprogrammeweeks` PRIMARY KEY (`Id`),
    CONSTRAINT `FK_ka_dogtrainingprogrammeweeks_ka_companies_CompanyId` FOREIGN KEY (`CompanyId`) REFERENCES `ka_companies` (`Id`) ON DELETE CASCADE,
    CONSTRAINT `FK_ka_dogtrainingprogrammeweeks_ka_dogtrainingprogrammes_Progra~` FOREIGN KEY (`ProgrammeId`) REFERENCES `ka_dogtrainingprogrammes` (`Id`) ON DELETE CASCADE,
    INDEX `IX_ka_dogtrainingprogrammeweeks_CompanyId` (`CompanyId`),
    INDEX `IX_ka_dogtrainingprogrammeweeks_ProgrammeId_WeekNumber` (`ProgrammeId`, `WeekNumber`)
) CHARACTER SET=utf8mb4;

CREATE TABLE IF NOT EXISTS `ka_dogtrainingenrollments` (
    `Id` int NOT NULL AUTO_INCREMENT,
    `EnrollmentNumber` varchar(40) CHARACTER SET utf8mb4 NOT NULL,
    `DogId` int NOT NULL,
    `CustomerId` int NULL,
    `ProgrammeId` int NOT NULL,
    `DurationDays` int NOT NULL,
    `Fee` decimal(18,2) NOT NULL,
    `AdditionalCharges` decimal(18,2) NOT NULL,
    `AdditionalChargesNote` longtext CHARACTER SET utf8mb4 NULL,
    `Status` varchar(40) CHARACTER SET utf8mb4 NOT NULL,
    `InvoiceId` int NULL,
    `HandlerUserId` varchar(255) CHARACTER SET utf8mb4 NULL,
    `StartDate` datetime(6) NULL,
    `PlannedEndDate` datetime(6) NULL,
    `ActualEndDate` datetime(6) NULL,
    `ExtensionDays` int NOT NULL,
    `ActivatedByUserId` longtext CHARACTER SET utf8mb4 NULL,
    `ActivatedOn` datetime(6) NULL,
    `Notes` longtext CHARACTER SET utf8mb4 NULL,
    `CancelReason` longtext CHARACTER SET utf8mb4 NULL,
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
    CONSTRAINT `PK_ka_dogtrainingenrollments` PRIMARY KEY (`Id`),
    CONSTRAINT `FK_ka_dogtrainingenrollments_AspNetUsers_HandlerUserId` FOREIGN KEY (`HandlerUserId`) REFERENCES `AspNetUsers` (`Id`) ON DELETE RESTRICT,
    CONSTRAINT `FK_ka_dogtrainingenrollments_ka_companies_CompanyId` FOREIGN KEY (`CompanyId`) REFERENCES `ka_companies` (`Id`) ON DELETE CASCADE,
    CONSTRAINT `FK_ka_dogtrainingenrollments_ka_dogtrainingcustomers_CustomerId` FOREIGN KEY (`CustomerId`) REFERENCES `ka_dogtrainingcustomers` (`Id`) ON DELETE RESTRICT,
    CONSTRAINT `FK_ka_dogtrainingenrollments_ka_dogtrainingdogs_DogId` FOREIGN KEY (`DogId`) REFERENCES `ka_dogtrainingdogs` (`Id`) ON DELETE RESTRICT,
    CONSTRAINT `FK_ka_dogtrainingenrollments_ka_dogtraininginvoices_InvoiceId` FOREIGN KEY (`InvoiceId`) REFERENCES `ka_dogtraininginvoices` (`Id`) ON DELETE SET NULL,
    CONSTRAINT `FK_ka_dogtrainingenrollments_ka_dogtrainingprogrammes_Programme~` FOREIGN KEY (`ProgrammeId`) REFERENCES `ka_dogtrainingprogrammes` (`Id`) ON DELETE RESTRICT,
    INDEX `IX_ka_dogtrainingenrollments_CompanyId` (`CompanyId`),
    INDEX `IX_ka_dogtrainingenrollments_CustomerId` (`CustomerId`),
    INDEX `IX_ka_dogtrainingenrollments_DogId` (`DogId`),
    UNIQUE INDEX `IX_ka_dogtrainingenrollments_EnrollmentNumber` (`EnrollmentNumber`),
    INDEX `IX_ka_dogtrainingenrollments_HandlerUserId` (`HandlerUserId`),
    INDEX `IX_ka_dogtrainingenrollments_InvoiceId` (`InvoiceId`),
    INDEX `IX_ka_dogtrainingenrollments_ProgrammeId` (`ProgrammeId`),
    INDEX `IX_ka_dogtrainingenrollments_Status` (`Status`)
) CHARACTER SET=utf8mb4;

CREATE TABLE IF NOT EXISTS `ka_dogtraininginvoiceitems` (
    `Id` int NOT NULL AUTO_INCREMENT,
    `InvoiceId` int NOT NULL,
    `Description` longtext CHARACTER SET utf8mb4 NOT NULL,
    `Quantity` int NOT NULL,
    `UnitPrice` decimal(18,2) NOT NULL,
    `Amount` decimal(18,2) NOT NULL,
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
    CONSTRAINT `PK_ka_dogtraininginvoiceitems` PRIMARY KEY (`Id`),
    CONSTRAINT `FK_ka_dogtraininginvoiceitems_ka_companies_CompanyId` FOREIGN KEY (`CompanyId`) REFERENCES `ka_companies` (`Id`) ON DELETE CASCADE,
    CONSTRAINT `FK_ka_dogtraininginvoiceitems_ka_dogtraininginvoices_InvoiceId` FOREIGN KEY (`InvoiceId`) REFERENCES `ka_dogtraininginvoices` (`Id`) ON DELETE CASCADE,
    INDEX `IX_ka_dogtraininginvoiceitems_CompanyId` (`CompanyId`),
    INDEX `IX_ka_dogtraininginvoiceitems_InvoiceId` (`InvoiceId`)
) CHARACTER SET=utf8mb4;

CREATE TABLE IF NOT EXISTS `ka_dogtrainingpayments` (
    `Id` int NOT NULL AUTO_INCREMENT,
    `InvoiceId` int NOT NULL,
    `PaymentNumber` varchar(40) CHARACTER SET utf8mb4 NOT NULL,
    `PaymentDate` datetime(6) NOT NULL,
    `Amount` decimal(18,2) NOT NULL,
    `Method` longtext CHARACTER SET utf8mb4 NOT NULL,
    `ReferenceNumber` longtext CHARACTER SET utf8mb4 NULL,
    `Notes` longtext CHARACTER SET utf8mb4 NULL,
    `ReceivedByUserId` longtext CHARACTER SET utf8mb4 NULL,
    `ReceivedByName` longtext CHARACTER SET utf8mb4 NULL,
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
    CONSTRAINT `PK_ka_dogtrainingpayments` PRIMARY KEY (`Id`),
    CONSTRAINT `FK_ka_dogtrainingpayments_ka_companies_CompanyId` FOREIGN KEY (`CompanyId`) REFERENCES `ka_companies` (`Id`) ON DELETE CASCADE,
    CONSTRAINT `FK_ka_dogtrainingpayments_ka_dogtraininginvoices_InvoiceId` FOREIGN KEY (`InvoiceId`) REFERENCES `ka_dogtraininginvoices` (`Id`) ON DELETE RESTRICT,
    INDEX `IX_ka_dogtrainingpayments_CompanyId` (`CompanyId`),
    INDEX `IX_ka_dogtrainingpayments_InvoiceId` (`InvoiceId`),
    UNIQUE INDEX `IX_ka_dogtrainingpayments_PaymentNumber` (`PaymentNumber`)
) CHARACTER SET=utf8mb4;

CREATE TABLE IF NOT EXISTS `ka_doghandlerassignments` (
    `Id` int NOT NULL AUTO_INCREMENT,
    `EnrollmentId` int NOT NULL,
    `HandlerUserId` varchar(255) CHARACTER SET utf8mb4 NOT NULL,
    `AssignedOn` datetime(6) NOT NULL,
    `AssignedByUserId` longtext CHARACTER SET utf8mb4 NULL,
    `Notes` longtext CHARACTER SET utf8mb4 NULL,
    `EndedOn` datetime(6) NULL,
    `IsCurrent` tinyint(1) NOT NULL,
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
    CONSTRAINT `PK_ka_doghandlerassignments` PRIMARY KEY (`Id`),
    CONSTRAINT `FK_ka_doghandlerassignments_AspNetUsers_HandlerUserId` FOREIGN KEY (`HandlerUserId`) REFERENCES `AspNetUsers` (`Id`) ON DELETE RESTRICT,
    CONSTRAINT `FK_ka_doghandlerassignments_ka_companies_CompanyId` FOREIGN KEY (`CompanyId`) REFERENCES `ka_companies` (`Id`) ON DELETE CASCADE,
    CONSTRAINT `FK_ka_doghandlerassignments_ka_dogtrainingenrollments_Enrollmen~` FOREIGN KEY (`EnrollmentId`) REFERENCES `ka_dogtrainingenrollments` (`Id`) ON DELETE CASCADE,
    INDEX `IX_ka_doghandlerassignments_CompanyId` (`CompanyId`),
    INDEX `IX_ka_doghandlerassignments_EnrollmentId` (`EnrollmentId`),
    INDEX `IX_ka_doghandlerassignments_HandlerUserId` (`HandlerUserId`)
) CHARACTER SET=utf8mb4;

CREATE TABLE IF NOT EXISTS `ka_doghealthassessments` (
    `Id` int NOT NULL AUTO_INCREMENT,
    `EnrollmentId` int NOT NULL,
    `DogId` int NOT NULL,
    `AssessmentDate` datetime(6) NOT NULL,
    `ExaminerUserId` longtext CHARACTER SET utf8mb4 NULL,
    `ExaminerName` longtext CHARACTER SET utf8mb4 NULL,
    `ForceNumber` longtext CHARACTER SET utf8mb4 NULL,
    `ExaminerPhone` longtext CHARACTER SET utf8mb4 NULL,
    `DogHandlerName` longtext CHARACTER SET utf8mb4 NULL,
    `Species` longtext CHARACTER SET utf8mb4 NULL,
    `WeightKg` decimal(8,2) NULL,
    `PhysicalCondition` longtext CHARACTER SET utf8mb4 NULL,
    `VaccinationStatus` longtext CHARACTER SET utf8mb4 NULL,
    `DewormingStatus` longtext CHARACTER SET utf8mb4 NULL,
    `MedicalConditions` longtext CHARACTER SET utf8mb4 NULL,
    `FleaTickTreatmentDate` datetime(6) NULL,
    `CertGoodHealth` tinyint(1) NOT NULL,
    `CertVaccinationsCurrent` tinyint(1) NOT NULL,
    `CertNoParasites` tinyint(1) NOT NULL,
    `CertNoBiteHistory` tinyint(1) NOT NULL,
    `Recommendation` longtext CHARACTER SET utf8mb4 NOT NULL,
    `Comments` longtext CHARACTER SET utf8mb4 NULL,
    `InspectorSignatureName` longtext CHARACTER SET utf8mb4 NULL,
    `SignedOn` datetime(6) NULL,
    `Result` varchar(40) CHARACTER SET utf8mb4 NOT NULL,
    `ResultDate` datetime(6) NULL,
    `DecidedByUserId` longtext CHARACTER SET utf8mb4 NULL,
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
    CONSTRAINT `PK_ka_doghealthassessments` PRIMARY KEY (`Id`),
    CONSTRAINT `FK_ka_doghealthassessments_ka_companies_CompanyId` FOREIGN KEY (`CompanyId`) REFERENCES `ka_companies` (`Id`) ON DELETE CASCADE,
    CONSTRAINT `FK_ka_doghealthassessments_ka_dogtrainingdogs_DogId` FOREIGN KEY (`DogId`) REFERENCES `ka_dogtrainingdogs` (`Id`) ON DELETE RESTRICT,
    CONSTRAINT `FK_ka_doghealthassessments_ka_dogtrainingenrollments_Enrollment~` FOREIGN KEY (`EnrollmentId`) REFERENCES `ka_dogtrainingenrollments` (`Id`) ON DELETE CASCADE,
    INDEX `IX_ka_doghealthassessments_CompanyId` (`CompanyId`),
    INDEX `IX_ka_doghealthassessments_DogId` (`DogId`),
    INDEX `IX_ka_doghealthassessments_EnrollmentId` (`EnrollmentId`)
) CHARACTER SET=utf8mb4;

CREATE TABLE IF NOT EXISTS `ka_dogtrainingdailylogs` (
    `Id` int NOT NULL AUTO_INCREMENT,
    `EnrollmentId` int NOT NULL,
    `DogId` int NOT NULL,
    `HandlerUserId` varchar(255) CHARACTER SET utf8mb4 NOT NULL,
    `TrainingDate` datetime(6) NOT NULL,
    `WeekNumber` int NOT NULL,
    `DayNumber` int NOT NULL,
    `ActivitiesPerformed` longtext CHARACTER SET utf8mb4 NULL,
    `CommandsPracticed` longtext CHARACTER SET utf8mb4 NULL,
    `DurationMinutes` int NOT NULL,
    `SlotsJson` longtext CHARACTER SET utf8mb4 NULL,
    `AverageSlotRating` decimal(4,1) NULL,
    `Performance` longtext CHARACTER SET utf8mb4 NOT NULL,
    `BehaviourObservations` longtext CHARACTER SET utf8mb4 NULL,
    `Challenges` longtext CHARACTER SET utf8mb4 NULL,
    `Improvements` longtext CHARACTER SET utf8mb4 NULL,
    `Recommendations` longtext CHARACTER SET utf8mb4 NULL,
    `NextObjectives` longtext CHARACTER SET utf8mb4 NULL,
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
    CONSTRAINT `PK_ka_dogtrainingdailylogs` PRIMARY KEY (`Id`),
    CONSTRAINT `FK_ka_dogtrainingdailylogs_AspNetUsers_HandlerUserId` FOREIGN KEY (`HandlerUserId`) REFERENCES `AspNetUsers` (`Id`) ON DELETE RESTRICT,
    CONSTRAINT `FK_ka_dogtrainingdailylogs_ka_companies_CompanyId` FOREIGN KEY (`CompanyId`) REFERENCES `ka_companies` (`Id`) ON DELETE CASCADE,
    CONSTRAINT `FK_ka_dogtrainingdailylogs_ka_dogtrainingenrollments_Enrollment~` FOREIGN KEY (`EnrollmentId`) REFERENCES `ka_dogtrainingenrollments` (`Id`) ON DELETE CASCADE,
    INDEX `IX_ka_dogtrainingdailylogs_CompanyId` (`CompanyId`),
    INDEX `IX_ka_dogtrainingdailylogs_EnrollmentId_TrainingDate` (`EnrollmentId`, `TrainingDate`),
    INDEX `IX_ka_dogtrainingdailylogs_HandlerUserId` (`HandlerUserId`)
) CHARACTER SET=utf8mb4;

CREATE TABLE IF NOT EXISTS `ka_dogtrainingfinalassessments` (
    `Id` int NOT NULL AUTO_INCREMENT,
    `EnrollmentId` int NOT NULL,
    `AttemptNumber` int NOT NULL,
    `AssessmentDate` datetime(6) NOT NULL,
    `AssessorUserId` longtext CHARACTER SET utf8mb4 NULL,
    `AssessorName` longtext CHARACTER SET utf8mb4 NULL,
    `ItemsJson` longtext CHARACTER SET utf8mb4 NULL,
    `OverallScore` decimal(4,1) NULL,
    `Result` longtext CHARACTER SET utf8mb4 NOT NULL,
    `Comments` longtext CHARACTER SET utf8mb4 NULL,
    `ExtensionDays` int NOT NULL,
    `AdditionalActivities` longtext CHARACTER SET utf8mb4 NULL,
    `ApprovalStatus` longtext CHARACTER SET utf8mb4 NOT NULL,
    `ApprovedByUserId` longtext CHARACTER SET utf8mb4 NULL,
    `ApprovedOn` datetime(6) NULL,
    `ApprovalComments` longtext CHARACTER SET utf8mb4 NULL,
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
    CONSTRAINT `PK_ka_dogtrainingfinalassessments` PRIMARY KEY (`Id`),
    CONSTRAINT `FK_ka_dogtrainingfinalassessments_ka_companies_CompanyId` FOREIGN KEY (`CompanyId`) REFERENCES `ka_companies` (`Id`) ON DELETE CASCADE,
    CONSTRAINT `FK_ka_dogtrainingfinalassessments_ka_dogtrainingenrollments_Enr~` FOREIGN KEY (`EnrollmentId`) REFERENCES `ka_dogtrainingenrollments` (`Id`) ON DELETE CASCADE,
    INDEX `IX_ka_dogtrainingfinalassessments_CompanyId` (`CompanyId`),
    INDEX `IX_ka_dogtrainingfinalassessments_EnrollmentId` (`EnrollmentId`)
) CHARACTER SET=utf8mb4;

CREATE TABLE IF NOT EXISTS `ka_dogtrainingprogressreports` (
    `Id` int NOT NULL AUTO_INCREMENT,
    `EnrollmentId` int NOT NULL,
    `WeekNumber` int NOT NULL,
    `HandlerUserId` varchar(255) CHARACTER SET utf8mb4 NOT NULL,
    `Objectives` longtext CHARACTER SET utf8mb4 NULL,
    `ActivitiesCompleted` longtext CHARACTER SET utf8mb4 NULL,
    `ActivitiesOutstanding` longtext CHARACTER SET utf8mb4 NULL,
    `PerformanceSummary` longtext CHARACTER SET utf8mb4 NULL,
    `Challenges` longtext CHARACTER SET utf8mb4 NULL,
    `Improvements` longtext CHARACTER SET utf8mb4 NULL,
    `Recommendations` longtext CHARACTER SET utf8mb4 NULL,
    `Status` longtext CHARACTER SET utf8mb4 NOT NULL,
    `SubmittedOn` datetime(6) NULL,
    `ReviewedByUserId` longtext CHARACTER SET utf8mb4 NULL,
    `ReviewedOn` datetime(6) NULL,
    `ReviewComments` longtext CHARACTER SET utf8mb4 NULL,
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
    CONSTRAINT `PK_ka_dogtrainingprogressreports` PRIMARY KEY (`Id`),
    CONSTRAINT `FK_ka_dogtrainingprogressreports_AspNetUsers_HandlerUserId` FOREIGN KEY (`HandlerUserId`) REFERENCES `AspNetUsers` (`Id`) ON DELETE RESTRICT,
    CONSTRAINT `FK_ka_dogtrainingprogressreports_ka_companies_CompanyId` FOREIGN KEY (`CompanyId`) REFERENCES `ka_companies` (`Id`) ON DELETE CASCADE,
    CONSTRAINT `FK_ka_dogtrainingprogressreports_ka_dogtrainingenrollments_Enro~` FOREIGN KEY (`EnrollmentId`) REFERENCES `ka_dogtrainingenrollments` (`Id`) ON DELETE CASCADE,
    INDEX `IX_ka_dogtrainingprogressreports_CompanyId` (`CompanyId`),
    INDEX `IX_ka_dogtrainingprogressreports_EnrollmentId_WeekNumber` (`EnrollmentId`, `WeekNumber`),
    INDEX `IX_ka_dogtrainingprogressreports_HandlerUserId` (`HandlerUserId`)
) CHARACTER SET=utf8mb4;

CREATE TABLE IF NOT EXISTS `ka_dogtrainingweeklyplans` (
    `Id` int NOT NULL AUTO_INCREMENT,
    `EnrollmentId` int NOT NULL,
    `WeekNumber` int NOT NULL,
    `Title` longtext CHARACTER SET utf8mb4 NOT NULL,
    `Focus` longtext CHARACTER SET utf8mb4 NULL,
    `Targets` longtext CHARACTER SET utf8mb4 NULL,
    `ScheduleJson` longtext CHARACTER SET utf8mb4 NULL,
    `StartDate` datetime(6) NULL,
    `EndDate` datetime(6) NULL,
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
    CONSTRAINT `PK_ka_dogtrainingweeklyplans` PRIMARY KEY (`Id`),
    CONSTRAINT `FK_ka_dogtrainingweeklyplans_ka_companies_CompanyId` FOREIGN KEY (`CompanyId`) REFERENCES `ka_companies` (`Id`) ON DELETE CASCADE,
    CONSTRAINT `FK_ka_dogtrainingweeklyplans_ka_dogtrainingenrollments_Enrollme~` FOREIGN KEY (`EnrollmentId`) REFERENCES `ka_dogtrainingenrollments` (`Id`) ON DELETE CASCADE,
    INDEX `IX_ka_dogtrainingweeklyplans_CompanyId` (`CompanyId`),
    INDEX `IX_ka_dogtrainingweeklyplans_EnrollmentId_WeekNumber` (`EnrollmentId`, `WeekNumber`)
) CHARACTER SET=utf8mb4;

CREATE TABLE IF NOT EXISTS `ka_doghealthassessmentitems` (
    `Id` int NOT NULL AUTO_INCREMENT,
    `AssessmentId` int NOT NULL,
    `Category` longtext CHARACTER SET utf8mb4 NOT NULL,
    `Name` longtext CHARACTER SET utf8mb4 NOT NULL,
    `Status` longtext CHARACTER SET utf8mb4 NULL,
    `Notes` longtext CHARACTER SET utf8mb4 NULL,
    `Administered` tinyint(1) NOT NULL,
    `TreatmentDate` datetime(6) NULL,
    `Reason` longtext CHARACTER SET utf8mb4 NULL,
    `BesNotified` tinyint(1) NOT NULL,
    `SortOrder` int NOT NULL,
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
    CONSTRAINT `PK_ka_doghealthassessmentitems` PRIMARY KEY (`Id`),
    CONSTRAINT `FK_ka_doghealthassessmentitems_ka_companies_CompanyId` FOREIGN KEY (`CompanyId`) REFERENCES `ka_companies` (`Id`) ON DELETE CASCADE,
    CONSTRAINT `FK_ka_doghealthassessmentitems_ka_doghealthassessments_Assessme~` FOREIGN KEY (`AssessmentId`) REFERENCES `ka_doghealthassessments` (`Id`) ON DELETE CASCADE,
    INDEX `IX_ka_doghealthassessmentitems_AssessmentId` (`AssessmentId`),
    INDEX `IX_ka_doghealthassessmentitems_CompanyId` (`CompanyId`)
) CHARACTER SET=utf8mb4;

CREATE TABLE IF NOT EXISTS `ka_dogtrainingcertificates` (
    `Id` int NOT NULL AUTO_INCREMENT,
    `EnrollmentId` int NOT NULL,
    `FinalAssessmentId` int NOT NULL,
    `CertificateNumber` varchar(40) CHARACTER SET utf8mb4 NOT NULL,
    `VerificationToken` varchar(64) CHARACTER SET utf8mb4 NOT NULL,
    `IssuedOn` datetime(6) NOT NULL,
    `IssuedByUserId` longtext CHARACTER SET utf8mb4 NULL,
    `IsRevoked` tinyint(1) NOT NULL,
    `RevokedReason` longtext CHARACTER SET utf8mb4 NULL,
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
    CONSTRAINT `PK_ka_dogtrainingcertificates` PRIMARY KEY (`Id`),
    CONSTRAINT `FK_ka_dogtrainingcertificates_ka_companies_CompanyId` FOREIGN KEY (`CompanyId`) REFERENCES `ka_companies` (`Id`) ON DELETE CASCADE,
    CONSTRAINT `FK_ka_dogtrainingcertificates_ka_dogtrainingenrollments_Enrollm~` FOREIGN KEY (`EnrollmentId`) REFERENCES `ka_dogtrainingenrollments` (`Id`) ON DELETE RESTRICT,
    CONSTRAINT `FK_ka_dogtrainingcertificates_ka_dogtrainingfinalassessments_Fi~` FOREIGN KEY (`FinalAssessmentId`) REFERENCES `ka_dogtrainingfinalassessments` (`Id`) ON DELETE RESTRICT,
    UNIQUE INDEX `IX_ka_dogtrainingcertificates_CertificateNumber` (`CertificateNumber`),
    INDEX `IX_ka_dogtrainingcertificates_CompanyId` (`CompanyId`),
    INDEX `IX_ka_dogtrainingcertificates_EnrollmentId` (`EnrollmentId`),
    UNIQUE INDEX `IX_ka_dogtrainingcertificates_FinalAssessmentId` (`FinalAssessmentId`),
    UNIQUE INDEX `IX_ka_dogtrainingcertificates_VerificationToken` (`VerificationToken`)
) CHARACTER SET=utf8mb4;

-- -----------------------------------------------------------------------------------------------------
-- PART 2 - MENU, SUBMENUS AND PERMISSIONS
-- -----------------------------------------------------------------------------------------------------

-- The module (left-hand menu group)
INSERT INTO ka_systemmodules (Name, Icon, Description, AccessName, SortOrder, Uuid, CreatedAt, IsDeleted, IsActive)
SELECT 'Dog Training', 'bi-mortarboard-fill', 'Dog training: health assessment, enrolment, handlers, daily logbook, assessments and certificates', 'dogtraining', 15, UUID(), UTC_TIMESTAMP(6), 0, 1
FROM DUAL
WHERE NOT EXISTS (SELECT 1 FROM ka_systemmodules WHERE AccessName = 'dogtraining');

SET @training_module := (SELECT Id FROM ka_systemmodules WHERE AccessName = 'dogtraining' ORDER BY Id LIMIT 1);

-- Submenus (each is only shown to roles that hold its permission)
INSERT INTO ka_submodules (Name, ModuleId, Icon, Route, AccessName, `Order`, Uuid, CreatedAt, IsDeleted, IsActive)
SELECT 'Training Dashboard', @training_module, 'bi-speedometer2', 'Training/Dashboard', 'training.dashboard.view', 1, UUID(), UTC_TIMESTAMP(6), 0, 1
FROM DUAL
WHERE NOT EXISTS (SELECT 1 FROM ka_submodules WHERE AccessName = 'training.dashboard.view' AND ModuleId = @training_module);

INSERT INTO ka_submodules (Name, ModuleId, Icon, Route, AccessName, `Order`, Uuid, CreatedAt, IsDeleted, IsActive)
SELECT 'Training Customers', @training_module, 'bi-people-fill', 'Training/Customers', 'training.customer.view', 2, UUID(), UTC_TIMESTAMP(6), 0, 1
FROM DUAL
WHERE NOT EXISTS (SELECT 1 FROM ka_submodules WHERE AccessName = 'training.customer.view' AND ModuleId = @training_module);

INSERT INTO ka_submodules (Name, ModuleId, Icon, Route, AccessName, `Order`, Uuid, CreatedAt, IsDeleted, IsActive)
SELECT 'Training Dogs', @training_module, 'bi-piggy-bank-fill', 'Training/Dogs', 'training.dog.view', 3, UUID(), UTC_TIMESTAMP(6), 0, 1
FROM DUAL
WHERE NOT EXISTS (SELECT 1 FROM ka_submodules WHERE AccessName = 'training.dog.view' AND ModuleId = @training_module);

INSERT INTO ka_submodules (Name, ModuleId, Icon, Route, AccessName, `Order`, Uuid, CreatedAt, IsDeleted, IsActive)
SELECT 'Health Assessments', @training_module, 'bi-heart-pulse-fill', 'Training/HealthAssessments', 'training.health.view', 4, UUID(), UTC_TIMESTAMP(6), 0, 1
FROM DUAL
WHERE NOT EXISTS (SELECT 1 FROM ka_submodules WHERE AccessName = 'training.health.view' AND ModuleId = @training_module);

INSERT INTO ka_submodules (Name, ModuleId, Icon, Route, AccessName, `Order`, Uuid, CreatedAt, IsDeleted, IsActive)
SELECT 'Training Enrolments', @training_module, 'bi-clipboard2-check', 'Training/Enrolments', 'training.enrolment.view', 5, UUID(), UTC_TIMESTAMP(6), 0, 1
FROM DUAL
WHERE NOT EXISTS (SELECT 1 FROM ka_submodules WHERE AccessName = 'training.enrolment.view' AND ModuleId = @training_module);

INSERT INTO ka_submodules (Name, ModuleId, Icon, Route, AccessName, `Order`, Uuid, CreatedAt, IsDeleted, IsActive)
SELECT 'Invoices & Payments', @training_module, 'bi-receipt', 'Training/Invoices', 'training.invoice.view', 6, UUID(), UTC_TIMESTAMP(6), 0, 1
FROM DUAL
WHERE NOT EXISTS (SELECT 1 FROM ka_submodules WHERE AccessName = 'training.invoice.view' AND ModuleId = @training_module);

INSERT INTO ka_submodules (Name, ModuleId, Icon, Route, AccessName, `Order`, Uuid, CreatedAt, IsDeleted, IsActive)
SELECT 'Handler Allocation', @training_module, 'bi-person-check-fill', 'Training/HandlerAllocation', 'training.handler.view', 7, UUID(), UTC_TIMESTAMP(6), 0, 1
FROM DUAL
WHERE NOT EXISTS (SELECT 1 FROM ka_submodules WHERE AccessName = 'training.handler.view' AND ModuleId = @training_module);

INSERT INTO ka_submodules (Name, ModuleId, Icon, Route, AccessName, `Order`, Uuid, CreatedAt, IsDeleted, IsActive)
SELECT 'Training Programmes', @training_module, 'bi-journal-richtext', 'Training/Programmes', 'training.programme.view', 8, UUID(), UTC_TIMESTAMP(6), 0, 1
FROM DUAL
WHERE NOT EXISTS (SELECT 1 FROM ka_submodules WHERE AccessName = 'training.programme.view' AND ModuleId = @training_module);

INSERT INTO ka_submodules (Name, ModuleId, Icon, Route, AccessName, `Order`, Uuid, CreatedAt, IsDeleted, IsActive)
SELECT 'Daily Training Logbook', @training_module, 'bi-journal-text', 'Training/Logbook', 'training.log.view', 9, UUID(), UTC_TIMESTAMP(6), 0, 1
FROM DUAL
WHERE NOT EXISTS (SELECT 1 FROM ka_submodules WHERE AccessName = 'training.log.view' AND ModuleId = @training_module);

INSERT INTO ka_submodules (Name, ModuleId, Icon, Route, AccessName, `Order`, Uuid, CreatedAt, IsDeleted, IsActive)
SELECT 'Progress Reports', @training_module, 'bi-graph-up-arrow', 'Training/ProgressReports', 'training.progress.view', 10, UUID(), UTC_TIMESTAMP(6), 0, 1
FROM DUAL
WHERE NOT EXISTS (SELECT 1 FROM ka_submodules WHERE AccessName = 'training.progress.view' AND ModuleId = @training_module);

INSERT INTO ka_submodules (Name, ModuleId, Icon, Route, AccessName, `Order`, Uuid, CreatedAt, IsDeleted, IsActive)
SELECT 'Final Assessments', @training_module, 'bi-award', 'Training/FinalAssessments', 'training.assessment.view', 11, UUID(), UTC_TIMESTAMP(6), 0, 1
FROM DUAL
WHERE NOT EXISTS (SELECT 1 FROM ka_submodules WHERE AccessName = 'training.assessment.view' AND ModuleId = @training_module);

INSERT INTO ka_submodules (Name, ModuleId, Icon, Route, AccessName, `Order`, Uuid, CreatedAt, IsDeleted, IsActive)
SELECT 'Certificates', @training_module, 'bi-patch-check-fill', 'Training/Certificates', 'training.certificate.view', 12, UUID(), UTC_TIMESTAMP(6), 0, 1
FROM DUAL
WHERE NOT EXISTS (SELECT 1 FROM ka_submodules WHERE AccessName = 'training.certificate.view' AND ModuleId = @training_module);

INSERT INTO ka_submodules (Name, ModuleId, Icon, Route, AccessName, `Order`, Uuid, CreatedAt, IsDeleted, IsActive)
SELECT 'Training Reports', @training_module, 'bi-bar-chart-line-fill', 'Training/Reports', 'training.reports.view', 13, UUID(), UTC_TIMESTAMP(6), 0, 1
FROM DUAL
WHERE NOT EXISTS (SELECT 1 FROM ka_submodules WHERE AccessName = 'training.reports.view' AND ModuleId = @training_module);

-- Permissions
INSERT INTO ka_permissions (Name, DisplayName, Module, Uuid, CreatedAt, IsDeleted, IsActive)
SELECT 'training.assessment.approve', 'Approve Training Completion', 'Dog Training', UUID(), UTC_TIMESTAMP(6), 0, 1
FROM DUAL
WHERE NOT EXISTS (SELECT 1 FROM ka_permissions WHERE Name = 'training.assessment.approve');

INSERT INTO ka_permissions (Name, DisplayName, Module, Uuid, CreatedAt, IsDeleted, IsActive)
SELECT 'training.assessment.conduct', 'Conduct Final Assessments', 'Dog Training', UUID(), UTC_TIMESTAMP(6), 0, 1
FROM DUAL
WHERE NOT EXISTS (SELECT 1 FROM ka_permissions WHERE Name = 'training.assessment.conduct');

INSERT INTO ka_permissions (Name, DisplayName, Module, Uuid, CreatedAt, IsDeleted, IsActive)
SELECT 'training.assessment.view', 'View Final Assessments', 'Dog Training', UUID(), UTC_TIMESTAMP(6), 0, 1
FROM DUAL
WHERE NOT EXISTS (SELECT 1 FROM ka_permissions WHERE Name = 'training.assessment.view');

INSERT INTO ka_permissions (Name, DisplayName, Module, Uuid, CreatedAt, IsDeleted, IsActive)
SELECT 'training.certificate.issue', 'Issue Training Certificates', 'Dog Training', UUID(), UTC_TIMESTAMP(6), 0, 1
FROM DUAL
WHERE NOT EXISTS (SELECT 1 FROM ka_permissions WHERE Name = 'training.certificate.issue');

INSERT INTO ka_permissions (Name, DisplayName, Module, Uuid, CreatedAt, IsDeleted, IsActive)
SELECT 'training.certificate.view', 'View Training Certificates', 'Dog Training', UUID(), UTC_TIMESTAMP(6), 0, 1
FROM DUAL
WHERE NOT EXISTS (SELECT 1 FROM ka_permissions WHERE Name = 'training.certificate.view');

INSERT INTO ka_permissions (Name, DisplayName, Module, Uuid, CreatedAt, IsDeleted, IsActive)
SELECT 'training.customer.manage', 'Manage Training Customers', 'Dog Training', UUID(), UTC_TIMESTAMP(6), 0, 1
FROM DUAL
WHERE NOT EXISTS (SELECT 1 FROM ka_permissions WHERE Name = 'training.customer.manage');

INSERT INTO ka_permissions (Name, DisplayName, Module, Uuid, CreatedAt, IsDeleted, IsActive)
SELECT 'training.customer.view', 'View Training Customers', 'Dog Training', UUID(), UTC_TIMESTAMP(6), 0, 1
FROM DUAL
WHERE NOT EXISTS (SELECT 1 FROM ka_permissions WHERE Name = 'training.customer.view');

INSERT INTO ka_permissions (Name, DisplayName, Module, Uuid, CreatedAt, IsDeleted, IsActive)
SELECT 'training.dashboard.view', 'View Training Dashboard', 'Dog Training', UUID(), UTC_TIMESTAMP(6), 0, 1
FROM DUAL
WHERE NOT EXISTS (SELECT 1 FROM ka_permissions WHERE Name = 'training.dashboard.view');

INSERT INTO ka_permissions (Name, DisplayName, Module, Uuid, CreatedAt, IsDeleted, IsActive)
SELECT 'training.dog.manage', 'Manage Training Dogs', 'Dog Training', UUID(), UTC_TIMESTAMP(6), 0, 1
FROM DUAL
WHERE NOT EXISTS (SELECT 1 FROM ka_permissions WHERE Name = 'training.dog.manage');

INSERT INTO ka_permissions (Name, DisplayName, Module, Uuid, CreatedAt, IsDeleted, IsActive)
SELECT 'training.dog.view', 'View Training Dogs', 'Dog Training', UUID(), UTC_TIMESTAMP(6), 0, 1
FROM DUAL
WHERE NOT EXISTS (SELECT 1 FROM ka_permissions WHERE Name = 'training.dog.view');

INSERT INTO ka_permissions (Name, DisplayName, Module, Uuid, CreatedAt, IsDeleted, IsActive)
SELECT 'training.enrolment.create', 'Create Training Enrolments', 'Dog Training', UUID(), UTC_TIMESTAMP(6), 0, 1
FROM DUAL
WHERE NOT EXISTS (SELECT 1 FROM ka_permissions WHERE Name = 'training.enrolment.create');

INSERT INTO ka_permissions (Name, DisplayName, Module, Uuid, CreatedAt, IsDeleted, IsActive)
SELECT 'training.enrolment.view', 'View Training Enrolments', 'Dog Training', UUID(), UTC_TIMESTAMP(6), 0, 1
FROM DUAL
WHERE NOT EXISTS (SELECT 1 FROM ka_permissions WHERE Name = 'training.enrolment.view');

INSERT INTO ka_permissions (Name, DisplayName, Module, Uuid, CreatedAt, IsDeleted, IsActive)
SELECT 'training.handler.assign', 'Assign Handlers & Activate Training', 'Dog Training', UUID(), UTC_TIMESTAMP(6), 0, 1
FROM DUAL
WHERE NOT EXISTS (SELECT 1 FROM ka_permissions WHERE Name = 'training.handler.assign');

INSERT INTO ka_permissions (Name, DisplayName, Module, Uuid, CreatedAt, IsDeleted, IsActive)
SELECT 'training.handler.view', 'View Handler Allocation', 'Dog Training', UUID(), UTC_TIMESTAMP(6), 0, 1
FROM DUAL
WHERE NOT EXISTS (SELECT 1 FROM ka_permissions WHERE Name = 'training.handler.view');

INSERT INTO ka_permissions (Name, DisplayName, Module, Uuid, CreatedAt, IsDeleted, IsActive)
SELECT 'training.health.approve', 'Approve Health Assessments', 'Dog Training', UUID(), UTC_TIMESTAMP(6), 0, 1
FROM DUAL
WHERE NOT EXISTS (SELECT 1 FROM ka_permissions WHERE Name = 'training.health.approve');

INSERT INTO ka_permissions (Name, DisplayName, Module, Uuid, CreatedAt, IsDeleted, IsActive)
SELECT 'training.health.create', 'Create Health Assessments', 'Dog Training', UUID(), UTC_TIMESTAMP(6), 0, 1
FROM DUAL
WHERE NOT EXISTS (SELECT 1 FROM ka_permissions WHERE Name = 'training.health.create');

INSERT INTO ka_permissions (Name, DisplayName, Module, Uuid, CreatedAt, IsDeleted, IsActive)
SELECT 'training.health.view', 'View Health Assessments', 'Dog Training', UUID(), UTC_TIMESTAMP(6), 0, 1
FROM DUAL
WHERE NOT EXISTS (SELECT 1 FROM ka_permissions WHERE Name = 'training.health.view');

INSERT INTO ka_permissions (Name, DisplayName, Module, Uuid, CreatedAt, IsDeleted, IsActive)
SELECT 'training.invoice.generate', 'Generate Training Invoices', 'Dog Training', UUID(), UTC_TIMESTAMP(6), 0, 1
FROM DUAL
WHERE NOT EXISTS (SELECT 1 FROM ka_permissions WHERE Name = 'training.invoice.generate');

INSERT INTO ka_permissions (Name, DisplayName, Module, Uuid, CreatedAt, IsDeleted, IsActive)
SELECT 'training.invoice.view', 'View Training Invoices', 'Dog Training', UUID(), UTC_TIMESTAMP(6), 0, 1
FROM DUAL
WHERE NOT EXISTS (SELECT 1 FROM ka_permissions WHERE Name = 'training.invoice.view');

INSERT INTO ka_permissions (Name, DisplayName, Module, Uuid, CreatedAt, IsDeleted, IsActive)
SELECT 'training.log.record', 'Record Daily Training', 'Dog Training', UUID(), UTC_TIMESTAMP(6), 0, 1
FROM DUAL
WHERE NOT EXISTS (SELECT 1 FROM ka_permissions WHERE Name = 'training.log.record');

INSERT INTO ka_permissions (Name, DisplayName, Module, Uuid, CreatedAt, IsDeleted, IsActive)
SELECT 'training.log.view', 'View Daily Training Logbook', 'Dog Training', UUID(), UTC_TIMESTAMP(6), 0, 1
FROM DUAL
WHERE NOT EXISTS (SELECT 1 FROM ka_permissions WHERE Name = 'training.log.view');

INSERT INTO ka_permissions (Name, DisplayName, Module, Uuid, CreatedAt, IsDeleted, IsActive)
SELECT 'training.manage.all', 'Manage All Dogs in Training (not only assigned)', 'Dog Training', UUID(), UTC_TIMESTAMP(6), 0, 1
FROM DUAL
WHERE NOT EXISTS (SELECT 1 FROM ka_permissions WHERE Name = 'training.manage.all');

INSERT INTO ka_permissions (Name, DisplayName, Module, Uuid, CreatedAt, IsDeleted, IsActive)
SELECT 'training.payment.record', 'Record Training Payments', 'Dog Training', UUID(), UTC_TIMESTAMP(6), 0, 1
FROM DUAL
WHERE NOT EXISTS (SELECT 1 FROM ka_permissions WHERE Name = 'training.payment.record');

INSERT INTO ka_permissions (Name, DisplayName, Module, Uuid, CreatedAt, IsDeleted, IsActive)
SELECT 'training.programme.manage', 'Manage Training Programmes & Templates', 'Dog Training', UUID(), UTC_TIMESTAMP(6), 0, 1
FROM DUAL
WHERE NOT EXISTS (SELECT 1 FROM ka_permissions WHERE Name = 'training.programme.manage');

INSERT INTO ka_permissions (Name, DisplayName, Module, Uuid, CreatedAt, IsDeleted, IsActive)
SELECT 'training.programme.view', 'View Training Programmes', 'Dog Training', UUID(), UTC_TIMESTAMP(6), 0, 1
FROM DUAL
WHERE NOT EXISTS (SELECT 1 FROM ka_permissions WHERE Name = 'training.programme.view');

INSERT INTO ka_permissions (Name, DisplayName, Module, Uuid, CreatedAt, IsDeleted, IsActive)
SELECT 'training.progress.review', 'Review & Approve Progress Reports', 'Dog Training', UUID(), UTC_TIMESTAMP(6), 0, 1
FROM DUAL
WHERE NOT EXISTS (SELECT 1 FROM ka_permissions WHERE Name = 'training.progress.review');

INSERT INTO ka_permissions (Name, DisplayName, Module, Uuid, CreatedAt, IsDeleted, IsActive)
SELECT 'training.progress.submit', 'Submit Progress Reports', 'Dog Training', UUID(), UTC_TIMESTAMP(6), 0, 1
FROM DUAL
WHERE NOT EXISTS (SELECT 1 FROM ka_permissions WHERE Name = 'training.progress.submit');

INSERT INTO ka_permissions (Name, DisplayName, Module, Uuid, CreatedAt, IsDeleted, IsActive)
SELECT 'training.progress.view', 'View Progress Reports', 'Dog Training', UUID(), UTC_TIMESTAMP(6), 0, 1
FROM DUAL
WHERE NOT EXISTS (SELECT 1 FROM ka_permissions WHERE Name = 'training.progress.view');

INSERT INTO ka_permissions (Name, DisplayName, Module, Uuid, CreatedAt, IsDeleted, IsActive)
SELECT 'training.reports.view', 'View Training Reports', 'Dog Training', UUID(), UTC_TIMESTAMP(6), 0, 1
FROM DUAL
WHERE NOT EXISTS (SELECT 1 FROM ka_permissions WHERE Name = 'training.reports.view');

-- -----------------------------------------------------------------------------------------------------
-- PART 3 - GIVE THE MENU AND PERMISSIONS TO THE ROLES
-- -----------------------------------------------------------------------------------------------------

-- Menu visible to SystemOwner, Admin and Dog Handler
INSERT INTO ka_rolemenus (RoleId, SystemModuleId, Uuid, CreatedAt, IsDeleted, IsActive)
SELECT r.Id, @training_module, UUID(), UTC_TIMESTAMP(6), 0, 1
FROM AspNetRoles r
WHERE r.Name IN ('SystemOwner', 'Admin', 'Dog Handler')
  AND NOT EXISTS (SELECT 1 FROM ka_rolemenus rm WHERE rm.RoleId = r.Id AND rm.SystemModuleId = @training_module);

-- ALL training permissions for SystemOwner and Admin
INSERT INTO ka_rolepermissions (RoleId, PermissionId, Uuid, CreatedAt, IsDeleted, IsActive)
SELECT r.Id, p.Id, UUID(), UTC_TIMESTAMP(6), 0, 1
FROM AspNetRoles r
JOIN ka_permissions p ON p.Name LIKE 'training.%'
WHERE r.Name IN ('SystemOwner', 'Admin')
  AND NOT EXISTS (SELECT 1 FROM ka_rolepermissions rp WHERE rp.RoleId = r.Id AND rp.PermissionId = p.Id);

-- Dog Handler: only their own dogs - daily logbook and weekly progress reports
INSERT INTO ka_rolepermissions (RoleId, PermissionId, Uuid, CreatedAt, IsDeleted, IsActive)
SELECT r.Id, p.Id, UUID(), UTC_TIMESTAMP(6), 0, 1
FROM AspNetRoles r
JOIN ka_permissions p ON p.Name IN ('training.log.view', 'training.log.record', 'training.progress.view', 'training.progress.submit')
WHERE r.Name = 'Dog Handler'
  AND NOT EXISTS (SELECT 1 FROM ka_rolepermissions rp WHERE rp.RoleId = r.Id AND rp.PermissionId = p.Id);

-- -----------------------------------------------------------------------------------------------------
-- CHECK (optional): should list 18 tables, 1 module, 13 submenus, 29 permissions
-- -----------------------------------------------------------------------------------------------------
SELECT COUNT(*) AS training_tables
  FROM information_schema.tables
 WHERE table_schema = DATABASE() AND (table_name LIKE 'ka\_dogtraining%' OR table_name LIKE 'ka\_doghealth%' OR table_name LIKE 'ka\_doghandler%');
SELECT COUNT(*) AS training_modules FROM ka_systemmodules WHERE AccessName = 'dogtraining';
SELECT COUNT(*) AS training_submenus FROM ka_submodules WHERE ModuleId = @training_module;
SELECT COUNT(*) AS training_permissions FROM ka_permissions WHERE Name LIKE 'training.%';
SELECT r.Name AS role_name, COUNT(rp.Id) AS training_permissions_granted
  FROM AspNetRoles r JOIN ka_rolepermissions rp ON rp.RoleId = r.Id JOIN ka_permissions p ON p.Id = rp.PermissionId AND p.Name LIKE 'training.%'
 GROUP BY r.Name;
