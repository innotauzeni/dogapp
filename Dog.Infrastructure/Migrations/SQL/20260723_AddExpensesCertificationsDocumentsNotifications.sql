-- Migration: Add Expenses, DogCertifications, DocumentAttachments, NotificationLogs
-- Date: 2026-07-23
-- Description: Four new modules — operating expenses (money out), per-dog training
-- certifications, generic file attachments (photos/contracts/insurance docs), and a
-- log of reminder notifications the system has sent (vaccine due, contract renewal,
-- invoice overdue). Also adds ExpiryDate to ka_customercontracts for renewal reminders.

CREATE TABLE ka_expenses (
    Id INT NOT NULL AUTO_INCREMENT PRIMARY KEY,
    Category LONGTEXT NOT NULL,
    Description LONGTEXT NOT NULL,
    Amount DECIMAL(18,2) NOT NULL,
    ExpenseDate DATETIME(6) NOT NULL,
    DogId INT NULL,
    Vendor LONGTEXT NULL,
    PaymentMethod LONGTEXT NULL,
    Notes LONGTEXT NULL,
    CompanyId INT NOT NULL,
    CreatedAt DATETIME(6) NOT NULL,
    UpdatedAt DATETIME(6) NULL,
    DeletedAt DATETIME(6) NULL,
    CreatedBy LONGTEXT NULL,
    UpdatedBy LONGTEXT NULL,
    DeletedBy LONGTEXT NULL,
    IsDeleted TINYINT(1) NOT NULL DEFAULT 0,
    IsActive TINYINT(1) NOT NULL DEFAULT 1,
    Uuid LONGTEXT NULL,
    CONSTRAINT FK_ka_expenses_ka_dogs
        FOREIGN KEY (DogId) REFERENCES ka_dogs(Id) ON DELETE SET NULL
) CHARACTER SET utf8mb4;

CREATE INDEX IX_ka_expenses_CompanyId ON ka_expenses(CompanyId);
CREATE INDEX IX_ka_expenses_DogId ON ka_expenses(DogId);

CREATE TABLE ka_dogcertifications (
    Id INT NOT NULL AUTO_INCREMENT PRIMARY KEY,
    DogId INT NOT NULL,
    CertificationName LONGTEXT NOT NULL,
    IssuingBody LONGTEXT NULL,
    IssueDate DATETIME(6) NOT NULL,
    ExpiryDate DATETIME(6) NULL,
    TrainerName LONGTEXT NULL,
    Notes LONGTEXT NULL,
    CertificateFileUrl LONGTEXT NULL,
    CompanyId INT NOT NULL,
    CreatedAt DATETIME(6) NOT NULL,
    UpdatedAt DATETIME(6) NULL,
    DeletedAt DATETIME(6) NULL,
    CreatedBy LONGTEXT NULL,
    UpdatedBy LONGTEXT NULL,
    DeletedBy LONGTEXT NULL,
    IsDeleted TINYINT(1) NOT NULL DEFAULT 0,
    IsActive TINYINT(1) NOT NULL DEFAULT 1,
    Uuid LONGTEXT NULL,
    CONSTRAINT FK_ka_dogcertifications_ka_dogs
        FOREIGN KEY (DogId) REFERENCES ka_dogs(Id) ON DELETE CASCADE
) CHARACTER SET utf8mb4;

CREATE INDEX IX_ka_dogcertifications_CompanyId ON ka_dogcertifications(CompanyId);
CREATE INDEX IX_ka_dogcertifications_DogId ON ka_dogcertifications(DogId);

CREATE TABLE ka_documentattachments (
    Id INT NOT NULL AUTO_INCREMENT PRIMARY KEY,
    EntityType LONGTEXT NOT NULL,
    EntityId INT NOT NULL,
    FileName LONGTEXT NOT NULL,
    FileUrl LONGTEXT NOT NULL,
    ContentType LONGTEXT NULL,
    Notes LONGTEXT NULL,
    UploadedByUserId LONGTEXT NULL,
    CompanyId INT NOT NULL,
    CreatedAt DATETIME(6) NOT NULL,
    UpdatedAt DATETIME(6) NULL,
    DeletedAt DATETIME(6) NULL,
    CreatedBy LONGTEXT NULL,
    UpdatedBy LONGTEXT NULL,
    DeletedBy LONGTEXT NULL,
    IsDeleted TINYINT(1) NOT NULL DEFAULT 0,
    IsActive TINYINT(1) NOT NULL DEFAULT 1,
    Uuid LONGTEXT NULL
) CHARACTER SET utf8mb4;

CREATE INDEX IX_ka_documentattachments_CompanyId ON ka_documentattachments(CompanyId);

CREATE TABLE ka_notificationlogs (
    Id INT NOT NULL AUTO_INCREMENT PRIMARY KEY,
    NotificationType LONGTEXT NOT NULL,
    RecipientPhone LONGTEXT NULL,
    Message LONGTEXT NOT NULL,
    SentAt DATETIME(6) NOT NULL,
    Success TINYINT(1) NOT NULL DEFAULT 0,
    RelatedEntityType LONGTEXT NULL,
    RelatedEntityId INT NULL,
    CompanyId INT NOT NULL,
    CreatedAt DATETIME(6) NOT NULL,
    UpdatedAt DATETIME(6) NULL,
    DeletedAt DATETIME(6) NULL,
    CreatedBy LONGTEXT NULL,
    UpdatedBy LONGTEXT NULL,
    DeletedBy LONGTEXT NULL,
    IsDeleted TINYINT(1) NOT NULL DEFAULT 0,
    IsActive TINYINT(1) NOT NULL DEFAULT 1,
    Uuid LONGTEXT NULL
) CHARACTER SET utf8mb4;

CREATE INDEX IX_ka_notificationlogs_CompanyId ON ka_notificationlogs(CompanyId);

ALTER TABLE ka_customercontracts
ADD COLUMN ExpiryDate DATETIME(6) NULL;

-- Verify migration
SELECT TABLE_NAME, COLUMN_NAME, DATA_TYPE, IS_NULLABLE
FROM INFORMATION_SCHEMA.COLUMNS
WHERE TABLE_NAME IN ('ka_expenses', 'ka_dogcertifications', 'ka_documentattachments', 'ka_notificationlogs')
ORDER BY TABLE_NAME, ORDINAL_POSITION;
