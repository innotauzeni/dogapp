-- Migration: Add Location and Conditional Fields to DogCareRecord
-- Date: 2026-07-19
-- Description: Add kennel/location tracking and detailed event type fields to care records

BEGIN TRANSACTION;

-- Add Location field for kennel tracking
ALTER TABLE ka_dogcarerecords
ADD Location NVARCHAR(255) NULL;

-- Add Vaccination fields
ALTER TABLE ka_dogcarerecords
ADD VaccineName NVARCHAR(255) NULL,
    VaccineBatch NVARCHAR(255) NULL,
    VaccineNextDue DATETIME2(7) NULL,
    VaccineNotes NVARCHAR(MAX) NULL;

-- Add Mating fields
ALTER TABLE ka_dogcarerecords
ADD MatingPartnerName NVARCHAR(255) NULL,
    MatingDuration NVARCHAR(255) NULL;

-- Add Injury fields
ALTER TABLE ka_dogcarerecords
ADD InjurySeverity NVARCHAR(50) NULL,
    InjuryDetails NVARCHAR(MAX) NULL,
    InjuryTreated BIT NULL DEFAULT 0;

-- Add Bite fields
ALTER TABLE ka_dogcarerecords
ADD BiteSeverity NVARCHAR(50) NULL,
    BiteDetails NVARCHAR(MAX) NULL,
    BiteReportedTo NVARCHAR(255) NULL;

-- Create indexes for frequently searched fields
CREATE INDEX idx_care_location ON ka_dogcarerecords(Location);
CREATE INDEX idx_care_vaccine ON ka_dogcarerecords(VaccineName);
CREATE INDEX idx_care_mating ON ka_dogcarerecords(MatingPartnerName);

-- Add check constraint for severity values
ALTER TABLE ka_dogcarerecords
ADD CONSTRAINT chk_injury_severity
CHECK (InjurySeverity IS NULL OR InjurySeverity IN ('Minor', 'Moderate', 'Severe'));

ALTER TABLE ka_dogcarerecords
ADD CONSTRAINT chk_bite_severity
CHECK (BiteSeverity IS NULL OR BiteSeverity IN ('Minor', 'Moderate', 'Severe'));

COMMIT TRANSACTION;

-- Verify migration
SELECT COLUMN_NAME, DATA_TYPE, IS_NULLABLE
FROM INFORMATION_SCHEMA.COLUMNS
WHERE TABLE_NAME = 'ka_dogcarerecords'
ORDER BY ORDINAL_POSITION;
