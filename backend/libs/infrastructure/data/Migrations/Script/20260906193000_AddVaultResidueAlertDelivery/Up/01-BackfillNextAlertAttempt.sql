UPDATE altivy.vault_residue_occurrences
SET next_alert_attempt_at = last_detected_at
WHERE alert_pending = true;
