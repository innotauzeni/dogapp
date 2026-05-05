-- Create BreedGroup table
CREATE TABLE ka_breedgroups (
    Id INT IDENTITY(1,1) PRIMARY KEY,
    Name NVARCHAR(255) NULL,
    CompanyId INT NOT NULL,
    IsActive BIT NOT NULL DEFAULT 1,
    IsDeleted BIT NOT NULL DEFAULT 0,
    CreatedAt DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
    UpdatedAt DATETIME2 NULL,
    DeletedAt DATETIME2 NULL,
    CreatedBy NVARCHAR(255) NULL,
    UpdatedBy NVARCHAR(255) NULL,
    DeletedBy NVARCHAR(255) NULL,
    CONSTRAINT FK_BreedGroups_Companies FOREIGN KEY (CompanyId) REFERENCES ka_companies(Id)
);

-- Create Breed table
CREATE TABLE ka_breeds (
    Id INT IDENTITY(1,1) PRIMARY KEY,
    BreedGroupId INT NOT NULL,
    Name NVARCHAR(255) NULL,
    CompanyId INT NOT NULL,
    IsActive BIT NOT NULL DEFAULT 1,
    IsDeleted BIT NOT NULL DEFAULT 0,
    CreatedAt DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
    UpdatedAt DATETIME2 NULL,
    DeletedAt DATETIME2 NULL,
    CreatedBy NVARCHAR(255) NULL,
    UpdatedBy NVARCHAR(255) NULL,
    DeletedBy NVARCHAR(255) NULL,
    CONSTRAINT FK_Breeds_BreedGroups FOREIGN KEY (BreedGroupId) REFERENCES ka_breedgroups(Id),
    CONSTRAINT FK_Breeds_Companies FOREIGN KEY (CompanyId) REFERENCES ka_companies(Id)
);

-- Insert default breed groups (these are standard AKC breed groups)
INSERT INTO ka_breedgroups (Name, CompanyId, IsActive, CreatedAt) VALUES
('Sporting', 1, 1, GETUTCDATE()),
('Hound', 1, 1, GETUTCDATE()),
('Working', 1, 1, GETUTCDATE()),
('Terrier', 1, 1, GETUTCDATE()),
('Toy', 1, 1, GETUTCDATE()),
('Non-Sporting', 1, 1, GETUTCDATE()),
('Herding', 1, 1, GETUTCDATE()),
('Miscellaneous', 1, 1, GETUTCDATE());

-- Insert some popular breeds for each group
DECLARE @SportingId INT = (SELECT Id FROM ka_breedgroups WHERE Name = 'Sporting' AND CompanyId = 1);
DECLARE @HoundId INT = (SELECT Id FROM ka_breedgroups WHERE Name = 'Hound' AND CompanyId = 1);
DECLARE @WorkingId INT = (SELECT Id FROM ka_breedgroups WHERE Name = 'Working' AND CompanyId = 1);
DECLARE @TerrierID INT = (SELECT Id FROM ka_breedgroups WHERE Name = 'Terrier' AND CompanyId = 1);
DECLARE @ToyId INT = (SELECT Id FROM ka_breedgroups WHERE Name = 'Toy' AND CompanyId = 1);
DECLARE @NonSportingId INT = (SELECT Id FROM ka_breedgroups WHERE Name = 'Non-Sporting' AND CompanyId = 1);
DECLARE @HerdingId INT = (SELECT Id FROM ka_breedgroups WHERE Name = 'Herding' AND CompanyId = 1);
DECLARE @MiscId INT = (SELECT Id FROM ka_breedgroups WHERE Name = 'Miscellaneous' AND CompanyId = 1);

-- Sporting breeds
INSERT INTO ka_breeds (BreedGroupId, Name, CompanyId, IsActive, CreatedAt) VALUES
(@SportingId, 'Golden Retriever', 1, 1, GETUTCDATE()),
(@SportingId, 'Labrador Retriever', 1, 1, GETUTCDATE()),
(@SportingId, 'German Shorthaired Pointer', 1, 1, GETUTCDATE()),
(@SportingId, 'English Springer Spaniel', 1, 1, GETUTCDATE()),
(@SportingId, 'Brittany', 1, 1, GETUTCDATE()),
(@SportingId, 'Cocker Spaniel', 1, 1, GETUTCDATE()),
(@SportingId, 'Weimaraner', 1, 1, GETUTCDATE()),
(@SportingId, 'Vizsla', 1, 1, GETUTCDATE());

-- Hound breeds
INSERT INTO ka_breeds (BreedGroupId, Name, CompanyId, IsActive, CreatedAt) VALUES
(@HoundId, 'Beagle', 1, 1, GETUTCDATE()),
(@HoundId, 'Bloodhound', 1, 1, GETUTCDATE()),
(@HoundId, 'Dachshund', 1, 1, GETUTCDATE()),
(@HoundId, 'Greyhound', 1, 1, GETUTCDATE()),
(@HoundId, 'Basset Hound', 1, 1, GETUTCDATE()),
(@HoundId, 'Afghan Hound', 1, 1, GETUTCDATE()),
(@HoundId, 'Irish Wolfhound', 1, 1, GETUTCDATE()),
(@HoundId, 'Whippet', 1, 1, GETUTCDATE());

-- Working breeds
INSERT INTO ka_breeds (BreedGroupId, Name, CompanyId, IsActive, CreatedAt) VALUES
(@WorkingId, 'Siberian Husky', 1, 1, GETUTCDATE()),
(@WorkingId, 'Rottweiler', 1, 1, GETUTCDATE()),
(@WorkingId, 'Doberman Pinscher', 1, 1, GETUTCDATE()),
(@WorkingId, 'Great Dane', 1, 1, GETUTCDATE()),
(@WorkingId, 'Boxer', 1, 1, GETUTCDATE()),
(@WorkingId, 'Mastiff', 1, 1, GETUTCDATE()),
(@WorkingId, 'Saint Bernard', 1, 1, GETUTCDATE()),
(@WorkingId, 'Newfoundland', 1, 1, GETUTCDATE());

-- Terrier breeds
INSERT INTO ka_breeds (BreedGroupId, Name, CompanyId, IsActive, CreatedAt) VALUES
(@TerrierID, 'Bull Terrier', 1, 1, GETUTCDATE()),
(@TerrierID, 'Scottish Terrier', 1, 1, GETUTCDATE()),
(@TerrierID, 'West Highland White Terrier', 1, 1, GETUTCDATE()),
(@TerrierID, 'Airedale Terrier', 1, 1, GETUTCDATE()),
(@TerrierID, 'Jack Russell Terrier', 1, 1, GETUTCDATE()),
(@TerrierID, 'Miniature Schnauzer', 1, 1, GETUTCDATE()),
(@TerrierID, 'American Staffordshire Terrier', 1, 1, GETUTCDATE()),
(@TerrierID, 'Border Terrier', 1, 1, GETUTCDATE());

-- Toy breeds
INSERT INTO ka_breeds (BreedGroupId, Name, CompanyId, IsActive, CreatedAt) VALUES
(@ToyId, 'Chihuahua', 1, 1, GETUTCDATE()),
(@ToyId, 'Pomeranian', 1, 1, GETUTCDATE()),
(@ToyId, 'Yorkshire Terrier', 1, 1, GETUTCDATE()),
(@ToyId, 'Maltese', 1, 1, GETUTCDATE()),
(@ToyId, 'Pug', 1, 1, GETUTCDATE()),
(@ToyId, 'Shih Tzu', 1, 1, GETUTCDATE()),
(@ToyId, 'Cavalier King Charles Spaniel', 1, 1, GETUTCDATE()),
(@ToyId, 'Papillon', 1, 1, GETUTCDATE());

-- Non-Sporting breeds
INSERT INTO ka_breeds (BreedGroupId, Name, CompanyId, IsActive, CreatedAt) VALUES
(@NonSportingId, 'Bulldog', 1, 1, GETUTCDATE()),
(@NonSportingId, 'Poodle', 1, 1, GETUTCDATE()),
(@NonSportingId, 'Boston Terrier', 1, 1, GETUTCDATE()),
(@NonSportingId, 'French Bulldog', 1, 1, GETUTCDATE()),
(@NonSportingId, 'Dalmatian', 1, 1, GETUTCDATE()),
(@NonSportingId, 'Bichon Frise', 1, 1, GETUTCDATE()),
(@NonSportingId, 'Chow Chow', 1, 1, GETUTCDATE()),
(@NonSportingId, 'Shiba Inu', 1, 1, GETUTCDATE());

-- Herding breeds
INSERT INTO ka_breeds (BreedGroupId, Name, CompanyId, IsActive, CreatedAt) VALUES
(@HerdingId, 'German Shepherd Dog', 1, 1, GETUTCDATE()),
(@HerdingId, 'Border Collie', 1, 1, GETUTCDATE()),
(@HerdingId, 'Australian Shepherd', 1, 1, GETUTCDATE()),
(@HerdingId, 'Pembroke Welsh Corgi', 1, 1, GETUTCDATE()),
(@HerdingId, 'Australian Cattle Dog', 1, 1, GETUTCDATE()),
(@HerdingId, 'Collie', 1, 1, GETUTCDATE()),
(@HerdingId, 'Shetland Sheepdog', 1, 1, GETUTCDATE()),
(@HerdingId, 'Belgian Malinois', 1, 1, GETUTCDATE());

-- Miscellaneous breeds
INSERT INTO ka_breeds (BreedGroupId, Name, CompanyId, IsActive, CreatedAt) VALUES
(@MiscId, 'Mixed Breed', 1, 1, GETUTCDATE()),
(@MiscId, 'Unknown', 1, 1, GETUTCDATE()),
(@MiscId, 'Other', 1, 1, GETUTCDATE());

-- Create indexes for better performance
CREATE INDEX IX_BreedGroups_CompanyId ON ka_breedgroups(CompanyId);
CREATE INDEX IX_BreedGroups_IsActive ON ka_breedgroups(IsActive);
CREATE INDEX IX_Breeds_BreedGroupId ON ka_breeds(BreedGroupId);
CREATE INDEX IX_Breeds_CompanyId ON ka_breeds(CompanyId);
CREATE INDEX IX_Breeds_IsActive ON ka_breeds(IsActive);