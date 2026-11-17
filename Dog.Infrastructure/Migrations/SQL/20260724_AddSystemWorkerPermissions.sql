-- The System Logs page (view/resolve/delete/resolve-all/delete-all/audit trail)
-- has always been gated behind permissions named "systemworker.view",
-- "systemworker.manage" and "systemworker.audit" (Dog.Api/Controllers/SystemLogsController.cs),
-- but those three permission names were never added to the permission seed list
-- (Dog.Infrastructure/Persistence/DbSeeder.cs), so no role could ever be granted
-- "systemworker.manage" specifically -- meaning "Mark All Resolved", "Delete All",
-- single-row Resolve/Delete, and the Audit Trail tab all silently 403 for everyone,
-- even though the log list itself (systemworker.view) may already work if it was
-- granted by hand earlier.
--
-- This script is idempotent (safe to run more than once):
--   1. Inserts the 3 missing permissions if they don't already exist.
--   2. Grants all 3 to the SystemOwner role.
--   3. Also grants systemworker.manage / systemworker.audit to any role that
--      already has systemworker.view (so whichever role you're actually using
--      today gets the missing pieces too, without guessing its name/id).
--
-- A matching fix has been added to DbSeeder.cs so freshly-seeded environments
-- get these permissions automatically going forward; this script is only needed
-- to patch the already-running database.

INSERT INTO ka_permissions (Uuid, Name, DisplayName, Module, CreatedAt, IsDeleted, IsActive)
SELECT UUID(), 'systemworker.view', 'View System Logs', 'System Worker', UTC_TIMESTAMP(), 0, 1
WHERE NOT EXISTS (SELECT 1 FROM ka_permissions WHERE Name = 'systemworker.view');

INSERT INTO ka_permissions (Uuid, Name, DisplayName, Module, CreatedAt, IsDeleted, IsActive)
SELECT UUID(), 'systemworker.manage', 'Resolve/Delete Log Entries', 'System Worker', UTC_TIMESTAMP(), 0, 1
WHERE NOT EXISTS (SELECT 1 FROM ka_permissions WHERE Name = 'systemworker.manage');

INSERT INTO ka_permissions (Uuid, Name, DisplayName, Module, CreatedAt, IsDeleted, IsActive)
SELECT UUID(), 'systemworker.audit', 'View Audit Trail', 'System Worker', UTC_TIMESTAMP(), 0, 1
WHERE NOT EXISTS (SELECT 1 FROM ka_permissions WHERE Name = 'systemworker.audit');

-- Grant all three to SystemOwner.
INSERT INTO ka_rolepermissions (Uuid, RoleId, PermissionId, CreatedAt, IsDeleted, IsActive)
SELECT UUID(), r.Id, p.Id, UTC_TIMESTAMP(), 0, 1
FROM AspNetRoles r
CROSS JOIN ka_permissions p
WHERE r.Name = 'SystemOwner'
  AND p.Name IN ('systemworker.view', 'systemworker.manage', 'systemworker.audit')
  AND NOT EXISTS (
      SELECT 1 FROM ka_rolepermissions rp WHERE rp.RoleId = r.Id AND rp.PermissionId = p.Id
  );

-- Also backfill manage/audit onto any role that already has view (covers whichever
-- non-SystemOwner role was manually granted systemworker.view earlier).
INSERT INTO ka_rolepermissions (Uuid, RoleId, PermissionId, CreatedAt, IsDeleted, IsActive)
SELECT UUID(), existing.RoleId, p.Id, UTC_TIMESTAMP(), 0, 1
FROM (
    SELECT rp.RoleId
    FROM ka_rolepermissions rp
    JOIN ka_permissions vp ON vp.Id = rp.PermissionId AND vp.Name = 'systemworker.view'
) existing
CROSS JOIN ka_permissions p
WHERE p.Name IN ('systemworker.manage', 'systemworker.audit')
  AND NOT EXISTS (
      SELECT 1 FROM ka_rolepermissions rp2 WHERE rp2.RoleId = existing.RoleId AND rp2.PermissionId = p.Id
  );
