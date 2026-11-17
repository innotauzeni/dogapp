-- Add PuppyId to ka_vaccinationrecords for puppy vaccination tracking
ALTER TABLE `ka_vaccinationrecords` MODIFY COLUMN `DogId` int NULL;
ALTER TABLE `ka_vaccinationrecords` ADD COLUMN `PuppyId` int NULL;
CREATE INDEX `IX_ka_vaccinationrecords_PuppyId` ON `ka_vaccinationrecords` (`PuppyId`);
ALTER TABLE `ka_vaccinationrecords` ADD CONSTRAINT `FK_ka_vaccinationrecords_ka_puppies_PuppyId` FOREIGN KEY (`PuppyId`) REFERENCES `ka_puppies` (`Id`) ON DELETE RESTRICT;
INSERT INTO `__EFMigrationsHistory` (`MigrationId`, `ProductVersion`) VALUES ('20260717135000_PuppyVaccinationTracking', '8.0.4');
