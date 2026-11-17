-- ============================================================================
-- Add PuppyId column to ka_vaccinationrecords table (MySQL)
-- Allows tracking vaccinations for unregistered puppies before promotion to dogs
--
-- Safe to run multiple times - checks for column existence
-- ============================================================================

-- Check if PuppyId column already exists
SET @dbname = DATABASE();
SET @tablename = 'ka_vaccinationrecords';
SET @columnname = 'PuppyId';
SET @preparedStatement = (SELECT IF(
  (SELECT COUNT(*) FROM INFORMATION_SCHEMA.COLUMNS
   WHERE (table_name = @tablename)
   AND (table_schema = @dbname)
   AND (column_name = @columnname)) > 0,
  "SELECT 'Column PuppyId already exists'",
  CONCAT("ALTER TABLE ", @tablename, " ADD COLUMN ", @columnname, " INT NULL")
));

PREPARE stmt FROM @preparedStatement;
EXECUTE stmt;
DEALLOCATE PREPARE stmt;

-- Add index on PuppyId if it doesn't exist
-- (MySQL doesn't have CREATE INDEX IF NOT EXISTS, so we check manually)
SET @indexExists = (SELECT COUNT(*) FROM INFORMATION_SCHEMA.STATISTICS
  WHERE table_name = 'ka_vaccinationrecords'
  AND index_name = 'IX_ka_vaccinationrecords_PuppyId'
  AND table_schema = @dbname);

SET @addIndex = (SELECT IF(@indexExists > 0,
  "SELECT 'Index IX_ka_vaccinationrecords_PuppyId already exists'",
  "CREATE INDEX IX_ka_vaccinationrecords_PuppyId ON ka_vaccinationrecords (PuppyId)"));

PREPARE stmt FROM @addIndex;
EXECUTE stmt;
DEALLOCATE PREPARE stmt;

-- Add foreign key constraint if it doesn't exist
SET @fkExists = (SELECT COUNT(*) FROM INFORMATION_SCHEMA.REFERENTIAL_CONSTRAINTS
  WHERE constraint_name = 'FK_ka_vaccinationrecords_ka_puppies_PuppyId'
  AND table_schema = @dbname);

SET @addFK = (SELECT IF(@fkExists > 0,
  "SELECT 'Foreign key FK_ka_vaccinationrecords_ka_puppies_PuppyId already exists'",
  "ALTER TABLE ka_vaccinationrecords ADD CONSTRAINT FK_ka_vaccinationrecords_ka_puppies_PuppyId FOREIGN KEY (PuppyId) REFERENCES ka_puppies (Id) ON DELETE RESTRICT"));

PREPARE stmt FROM @addFK;
EXECUTE stmt;
DEALLOCATE PREPARE stmt;

SELECT 'PuppyId column, index, and foreign key constraint added/verified successfully' AS status;
