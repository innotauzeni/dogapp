-- ============================================================================
-- Dog Section — manual schema update for LIVE (MySQL)
-- Apply these instead of running EF migrations on the live server.
-- Covers the two pending migrations:
--   20260717091029_AllowExternalMatingPartner
--   20260717124846_AddPuppyRegisteredDogId
-- Take a backup first. Run the whole script in one transaction if you like.
-- ============================================================================

-- 1) Flexible mating: the partner may be an UNREGISTERED dog (name only) ------
ALTER TABLE `ka_matingrecords` MODIFY COLUMN `SireId` int NULL;
ALTER TABLE `ka_matingrecords` MODIFY COLUMN `DamId`  int NULL;
ALTER TABLE `ka_matingrecords` ADD COLUMN `SireName` longtext CHARACTER SET utf8mb4 NULL;
ALTER TABLE `ka_matingrecords` ADD COLUMN `DamName`  longtext CHARACTER SET utf8mb4 NULL;

-- 2) Puppy auto-registration: link a puppy to the dog it was promoted into ----
ALTER TABLE `ka_puppies` ADD COLUMN `RegisteredDogId` int NULL;

-- 3) Puppy vaccination tracking: DogId nullable + PuppyId FK for recording
--    vaccines on puppies before they're promoted to full dogs. ----------------
ALTER TABLE `ka_vaccinationrecords` MODIFY COLUMN `DogId` int NULL;
ALTER TABLE `ka_vaccinationrecords` ADD COLUMN `PuppyId` int NULL;
ALTER TABLE `ka_vaccinationrecords` ADD CONSTRAINT `FK_ka_vaccinationrecords_ka_puppies_PuppyId`
  FOREIGN KEY (`PuppyId`) REFERENCES `ka_puppies` (`Id`) ON DELETE RESTRICT;
CREATE INDEX `IX_ka_vaccinationrecords_PuppyId` ON `ka_vaccinationrecords` (`PuppyId`);

-- 4) IMPORTANT: mark these migrations as already applied so the app's
--    startup auto-migrate (DbSeeder.MigrateAsync) does NOT try to run them
--    again and fail with "duplicate column". ----------------------------------
INSERT INTO `__EFMigrationsHistory` (`MigrationId`, `ProductVersion`) VALUES
  ('20260717091029_AllowExternalMatingPartner',  '8.0.4'),
  ('20260717124846_AddPuppyRegisteredDogId',     '8.0.4'),
  ('20260717135000_PuppyVaccinationTracking',    '8.0.4');
