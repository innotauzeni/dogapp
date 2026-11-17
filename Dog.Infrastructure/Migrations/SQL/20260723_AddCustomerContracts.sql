-- Migration: Add CustomerContract (date of contract + contract details)
-- Date: 2026-07-23
-- Description: Lets a customer have one or more signed contracts/agreements
-- recorded against them (contract date + free-text contract details).

CREATE TABLE ka_customercontracts (
    Id INT NOT NULL AUTO_INCREMENT PRIMARY KEY,
    CustomerId INT NOT NULL,
    ContractDate DATETIME(6) NOT NULL,
    ContractDetails LONGTEXT NULL,
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
    CONSTRAINT FK_ka_customercontracts_ka_customers
        FOREIGN KEY (CustomerId) REFERENCES ka_customers(Id) ON DELETE CASCADE
) CHARACTER SET utf8mb4;

CREATE INDEX IX_ka_customercontracts_CustomerId ON ka_customercontracts(CustomerId);

-- Verify migration
SELECT COLUMN_NAME, DATA_TYPE, IS_NULLABLE
FROM INFORMATION_SCHEMA.COLUMNS
WHERE TABLE_NAME = 'ka_customercontracts'
ORDER BY ORDINAL_POSITION;
