-- ─────────────────────────────────────────────────────────────────────────────
-- Adds dog → customer ownership (Dogtbl.CustomerId → Customer).
--
-- The project's EF migration history is out of sync with the live database
-- (only InitialCreate exists, but the model has many more tables), so an
-- auto-generated `dotnet ef` migration would try to recreate existing tables.
-- Run THIS targeted script directly against the MySQL database instead.
--
-- Safe to run once. MySQL 8 has no "ADD COLUMN IF NOT EXISTS", so only run it
-- if the column isn't already present.
-- ─────────────────────────────────────────────────────────────────────────────

ALTER TABLE ka_dogs
    ADD COLUMN CustomerId INT NULL;

CREATE INDEX IX_ka_dogs_CustomerId
    ON ka_dogs (CustomerId);

ALTER TABLE ka_dogs
    ADD CONSTRAINT FK_ka_dogs_ka_customers_CustomerId
    FOREIGN KEY (CustomerId) REFERENCES ka_customers (Id)
    ON DELETE SET NULL;
