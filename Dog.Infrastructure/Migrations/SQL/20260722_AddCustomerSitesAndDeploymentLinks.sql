-- Migration: Add CustomerSite (premises) + link Deployment to Customer/CustomerSite
-- Date: 2026-07-22
-- Description: Lets a customer have multiple sites/premises (name, address, GPS,
-- on-site contact), and lets a Deployment record which customer + which specific
-- site a dog is deployed to, instead of only free-text Location/CompanyDeployedTo.

CREATE TABLE ka_customersites (
    Id INT NOT NULL AUTO_INCREMENT PRIMARY KEY,
    CustomerId INT NOT NULL,
    SiteName LONGTEXT NOT NULL,
    Address LONGTEXT NULL,
    Latitude DOUBLE NULL,
    Longitude DOUBLE NULL,
    ContactPerson LONGTEXT NULL,
    ContactPhone LONGTEXT NULL,
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
    CONSTRAINT FK_ka_customersites_ka_customers
        FOREIGN KEY (CustomerId) REFERENCES ka_customers(Id) ON DELETE CASCADE
) CHARACTER SET utf8mb4;

CREATE INDEX IX_ka_customersites_CustomerId ON ka_customersites(CustomerId);

ALTER TABLE ka_deployments
ADD COLUMN CustomerId INT NULL,
ADD COLUMN CustomerSiteId INT NULL;

ALTER TABLE ka_deployments
ADD CONSTRAINT FK_ka_deployments_ka_customers
    FOREIGN KEY (CustomerId) REFERENCES ka_customers(Id) ON DELETE SET NULL,
ADD CONSTRAINT FK_ka_deployments_ka_customersites
    FOREIGN KEY (CustomerSiteId) REFERENCES ka_customersites(Id) ON DELETE SET NULL;

CREATE INDEX IX_ka_deployments_CustomerId ON ka_deployments(CustomerId);
CREATE INDEX IX_ka_deployments_CustomerSiteId ON ka_deployments(CustomerSiteId);

-- Verify migration
SELECT COLUMN_NAME, DATA_TYPE, IS_NULLABLE
FROM INFORMATION_SCHEMA.COLUMNS
WHERE TABLE_NAME = 'ka_customersites'
ORDER BY ORDINAL_POSITION;

SELECT COLUMN_NAME, DATA_TYPE, IS_NULLABLE
FROM INFORMATION_SCHEMA.COLUMNS
WHERE TABLE_NAME = 'ka_deployments'
ORDER BY ORDINAL_POSITION;
