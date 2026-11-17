-- Migration: Add IsAlerted/AlertedAt to ka_sytemerrorlogs
-- Date: 2026-07-23
-- Description: Lets the new CriticalErrorAlertService background job track which
-- Critical-severity error logs it has already sent a WhatsApp alert for, so the
-- same error is never re-alerted on every poll.

ALTER TABLE ka_sytemerrorlogs
ADD COLUMN IsAlerted TINYINT(1) NOT NULL DEFAULT 0,
ADD COLUMN AlertedAt DATETIME(6) NULL;

-- Verify migration
SELECT COLUMN_NAME, DATA_TYPE, IS_NULLABLE
FROM INFORMATION_SCHEMA.COLUMNS
WHERE TABLE_NAME = 'ka_sytemerrorlogs'
ORDER BY ORDINAL_POSITION;
