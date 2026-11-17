-- ============================================================================
-- Add PuppyId to ka_vaccinationrecords table (SIMPLE VERSION)
-- Run this ONCE on the live database to enable puppy vaccination tracking
--
-- This SQL:
-- 1. Makes DogId nullable (if not already)
-- 2. Adds PuppyId column for tracking puppy vaccines
-- 3. Creates index on PuppyId for query performance
-- 4. Adds foreign key to ka_puppies table
-- ============================================================================

-- 1. Modify DogId to nullable (if needed)
ALTER TABLE `ka_vaccinationrecords`
MODIFY COLUMN `DogId` int NULL;

-- 2. Add PuppyId column (nullable int)
ALTER TABLE `ka_vaccinationrecords`
ADD COLUMN `PuppyId` int NULL;

-- 3. Create index on PuppyId for performance
CREATE INDEX `IX_ka_vaccinationrecords_PuppyId`
ON `ka_vaccinationrecords` (`PuppyId`);

-- 4. Add foreign key constraint to ka_puppies
ALTER TABLE `ka_vaccinationrecords`
ADD CONSTRAINT `FK_ka_vaccinationrecords_ka_puppies_PuppyId`
FOREIGN KEY (`PuppyId`) REFERENCES `ka_puppies` (`Id`)
ON DELETE RESTRICT;

-- 5. Mark migration as applied (so EF doesn't try to run it again)
INSERT INTO `__EFMigrationsHistory` (`MigrationId`, `ProductVersion`)
VALUES ('20260717135000_PuppyVaccinationTracking', '8.0.4');

-- Verify the changes
SELECT
    COLUMN_NAME,
    IS_NULLABLE,
    COLUMN_TYPE
FROM INFORMATION_SCHEMA.COLUMNS
WHERE TABLE_NAME = 'ka_vaccinationrecords'
AND COLUMN_NAME IN ('DogId', 'PuppyId')
ORDER BY COLUMN_NAME;
