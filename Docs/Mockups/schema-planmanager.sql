-- Erzeugt von SchemaSnapshotTests aus einer frischen, leeren Datenbank - nicht von Hand aendern.
CREATE TABLE import_action_files (
    id TEXT PRIMARY KEY,
    action_id TEXT NOT NULL,
    file_id TEXT,
    file_name TEXT NOT NULL,
    original_file_name TEXT,
    final_file_name TEXT,
    file_type TEXT NOT NULL,
    source_path TEXT NOT NULL,
    destination_path TEXT NOT NULL,
    md5_hash TEXT NOT NULL,
    file_size INTEGER,
    FOREIGN KEY (action_id) REFERENCES import_actions(id)
);
CREATE TABLE import_actions (
    id TEXT PRIMARY KEY,
    import_id TEXT NOT NULL,
    action_order INTEGER NOT NULL,
    action_type TEXT NOT NULL,
    action_status TEXT NOT NULL,
    document_key TEXT,
    plan_number TEXT NOT NULL,
    plan_index TEXT,
    old_index TEXT,
    source_path TEXT NOT NULL,
    destination_path TEXT,
    archive_path TEXT,
    md5 TEXT,
    file_size INTEGER,
    document_type_id TEXT,
    document_type TEXT,                 -- Anzeigename (R01-006, ADR-064-Addendum P.2)
    building_part_id TEXT,              -- SoftRef bpm.db.building_parts(id) (R01-006)
    building_level_id TEXT,             -- SoftRef bpm.db.building_levels(id) (R01-006)
    error_message TEXT,
    FOREIGN KEY (import_id) REFERENCES import_journal(id)
);
CREATE TABLE import_journal (
    id TEXT PRIMARY KEY,
    timestamp TEXT NOT NULL,
    completed_at TEXT,
    status TEXT NOT NULL,
    source_path TEXT NOT NULL,
    file_count INTEGER NOT NULL,
    profile_id TEXT,
    machine_name TEXT,
    error_message TEXT
);
CREATE TABLE plan_context_links (
    id TEXT PRIMARY KEY,
    source_module TEXT NOT NULL,
    source_id TEXT NOT NULL,
    target_document_id TEXT NOT NULL,
    target_revision_id TEXT,
    resolution_mode TEXT NOT NULL
        CHECK (resolution_mode IN ('fixed_revision')),
    context_time TEXT NOT NULL,
    link_type TEXT NOT NULL
        CHECK (link_type IN ('auto_reference', 'manual_reference', 'attachment')),
    created_at TEXT NOT NULL,
    created_by TEXT,
    last_modified_at TEXT NOT NULL,
    last_modified_by TEXT,
    sync_version INTEGER NOT NULL DEFAULT 0,
    is_deleted INTEGER NOT NULL DEFAULT 0,
    FOREIGN KEY (target_document_id) REFERENCES plan_documents(id),
    FOREIGN KEY (target_revision_id) REFERENCES plan_revisions(id)
);
CREATE TABLE plan_document_segments (
    id TEXT PRIMARY KEY,
    document_id TEXT NOT NULL,
    segment_type_id TEXT NOT NULL,      -- SoftRef bpm.db.segment_types(id), kein FK (Cross-DB)
    segment_key TEXT NOT NULL,
    raw_value TEXT NOT NULL,
    normalized_value TEXT NOT NULL,
    created_at TEXT NOT NULL,
    created_by TEXT,
    last_modified_at TEXT NOT NULL,
    last_modified_by TEXT,
    sync_version INTEGER NOT NULL DEFAULT 0,
    is_deleted INTEGER NOT NULL DEFAULT 0,
    FOREIGN KEY (document_id) REFERENCES plan_documents(id),
    -- Keine FK auf segment_types: Cross-DB Soft Reference (ADR-058-Addendum)
    UNIQUE (document_id, segment_type_id)
);
CREATE TABLE plan_document_tags (
    id TEXT PRIMARY KEY,
    document_id TEXT NOT NULL,
    tag TEXT NOT NULL,                  -- Anzeigetext wie eingegeben
    normalized_tag TEXT NOT NULL,       -- lowercase/getrimmt fuer Vergleich + Vorschlaege
    created_at TEXT NOT NULL,
    created_by TEXT,
    sync_version INTEGER NOT NULL DEFAULT 0,
    is_deleted INTEGER NOT NULL DEFAULT 0,
    FOREIGN KEY (document_id) REFERENCES plan_documents(id),
    UNIQUE (document_id, normalized_tag)
);
CREATE TABLE plan_documents (
    id TEXT PRIMARY KEY,
    project_id TEXT NOT NULL,
    document_key TEXT NOT NULL UNIQUE,
    document_type_id TEXT NOT NULL,
    plan_number TEXT NOT NULL,
    document_type TEXT NOT NULL,
    title TEXT NOT NULL DEFAULT '',
    target_folder TEXT NOT NULL,
    relative_directory TEXT NOT NULL,
    building_part_id TEXT,              -- SoftRef bpm.db.building_parts(id), kein FK (Cross-DB)
    building_level_id TEXT,             -- SoftRef bpm.db.building_levels(id), kein FK (Cross-DB)
    created_at TEXT NOT NULL,
    created_by TEXT,
    last_modified_at TEXT NOT NULL,
    last_modified_by TEXT,
    sync_version INTEGER NOT NULL DEFAULT 0,
    is_deleted INTEGER NOT NULL DEFAULT 0
    -- Keine FK auf building_parts/building_levels: Cross-DB Soft Reference (ADR-058-Addendum)
);
CREATE TABLE plan_files (
    id TEXT PRIMARY KEY,
    file_name TEXT NOT NULL,
    relative_path TEXT NOT NULL,
    file_type TEXT NOT NULL,
    md5_hash TEXT NOT NULL,
    file_size INTEGER NOT NULL,
    origin_mode TEXT NOT NULL,
    created_at TEXT NOT NULL,
    updated_at TEXT NOT NULL
);
CREATE TABLE plan_revision_events (
    id TEXT PRIMARY KEY,
    revision_id TEXT NOT NULL,
    import_id TEXT,
    event_type TEXT NOT NULL
        CHECK (event_type IN ('created', 'made_current', 'superseded', 'file_linked', 'manual_override')),
    event_at TEXT NOT NULL,
    event_by TEXT,
    note TEXT NOT NULL DEFAULT '',
    created_at TEXT NOT NULL,
    created_by TEXT,
    last_modified_at TEXT NOT NULL,
    last_modified_by TEXT,
    sync_version INTEGER NOT NULL DEFAULT 0,
    is_deleted INTEGER NOT NULL DEFAULT 0,
    FOREIGN KEY (revision_id) REFERENCES plan_revisions(id),
    FOREIGN KEY (import_id) REFERENCES import_journal(id)
);
CREATE TABLE plan_revisions (
    id TEXT PRIMARY KEY,
    document_id TEXT NOT NULL,
    plan_index TEXT,
    index_source TEXT NOT NULL,
    revision_status TEXT NOT NULL
        CHECK (revision_status IN ('current', 'superseded', 'rejected')),
    current_from TEXT NOT NULL,
    superseded_at TEXT,
    received_at TEXT NOT NULL,
    released_at TEXT,                   -- Freigabedatum d. Index (BPM-109.04b); NULL bis OCR/manuell (post-V1)
    change_note TEXT NOT NULL DEFAULT '', -- Änderungshinweis d. Revision (ADR-063, befüllt via BPM-118 Text-Zuweisung)
    last_import_id TEXT,
    created_at TEXT NOT NULL,
    created_by TEXT,
    last_modified_at TEXT NOT NULL,
    last_modified_by TEXT,
    sync_version INTEGER NOT NULL DEFAULT 0,
    is_deleted INTEGER NOT NULL DEFAULT 0,
    FOREIGN KEY (document_id) REFERENCES plan_documents(id),
    FOREIGN KEY (last_import_id) REFERENCES import_journal(id)
);
CREATE TABLE revision_file_links (
    revision_id TEXT NOT NULL,
    file_id TEXT NOT NULL,
    link_mode TEXT NOT NULL,
    is_primary INTEGER NOT NULL DEFAULT 0,
    PRIMARY KEY (revision_id, file_id),
    FOREIGN KEY (revision_id) REFERENCES plan_revisions(id),
    FOREIGN KEY (file_id) REFERENCES plan_files(id)
);
CREATE TABLE schema_version (
    version TEXT NOT NULL
);
