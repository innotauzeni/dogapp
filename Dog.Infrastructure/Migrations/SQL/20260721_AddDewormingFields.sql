-- Migration: Add Deworming fields to DogCareRecord
-- Date: 2026-07-21
-- Description: Lets a "Deworming" care event record which product was used and
-- when the next dose is due (mirrors the existing Vaccine fields).

ALTER TABLE ka_dogcarerecords
ADD COLUMN DewormingProduct VARCHAR(255) NULL,
ADD COLUMN DewormingNextDue DATETIME(6) NULL;

-- Verify migration
SELECT COLUMN_NAME, DATA_TYPE, IS_NULLABLE
FROM INFORMATION_SCHEMA.COLUMNS
WHERE TABLE_NAME = 'ka_dogcarerecords'
ORDER BY ORDINAL_POSITION;
