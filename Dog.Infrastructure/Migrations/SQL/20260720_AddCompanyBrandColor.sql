-- Migration: Add BrandColor to Company
-- Date: 2026-07-20
-- Description: Lets a company customize the mobile app's welcome screen color.
-- Provider: MySQL (matches ka_companies' existing schema).

ALTER TABLE ka_companies
ADD COLUMN BrandColor VARCHAR(20) NULL;

-- Verify migration
SELECT COLUMN_NAME, DATA_TYPE, IS_NULLABLE
FROM INFORMATION_SCHEMA.COLUMNS
WHERE TABLE_NAME = 'ka_companies'
ORDER BY ORDINAL_POSITION;
