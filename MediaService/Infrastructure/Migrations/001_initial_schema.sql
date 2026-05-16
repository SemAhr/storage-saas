create extension if not exists "pgcrypto";

create type node_type as enum (
    'folder',
    'file'
);

create type upload_status as enum (
    'pending',
    'uploading',
    'completed',
    'failed',
    'canceled',
    'expired'
);

create type upload_mode as enum (
    'single',
    'multipart'
);

create table nodes (
    id uuid primary key default gen_random_uuid(),
    parent_id uuid null,
    name text not null,
    type node_type not null default 'folder',
    created_at timestamptz not null default now(),
    updated_at timestamptz not null default now(),
    deleted_at timestamptz null,

    constraint fk_nodes_parent
        foreign key (parent_id)
        references nodes(id)
        on delete cascade,

    constraint chk_nodes_name_not_blank
        check (length(btrim(name)) > 0),

    constraint chk_nodes_parent_not_self
        check (parent_id is null or parent_id <> id)
);

create table files (
    node_id uuid primary key,
    mime_type text not null,
    size bigint not null,
    object_key text not null unique,
    status upload_status not null default 'pending',
    created_at timestamptz not null default now(),
    updated_at timestamptz not null default now(),

    constraint fk_files_node
        foreign key (node_id)
        references nodes(id)
        on delete cascade,

    constraint chk_files_mime_type_not_blank
        check (length(btrim(mime_type)) > 0),

    constraint chk_files_object_key_not_blank
        check (length(btrim(object_key)) > 0),

    constraint chk_files_size
        check (size > 0)
);

create table file_upload_sessions (
    id uuid primary key default gen_random_uuid(),
    node_id uuid not null,
    upload_mode upload_mode not null,
    status upload_status not null default 'pending',
    expires_at timestamptz not null,
    created_at timestamptz not null default now(),
    updated_at timestamptz not null default now(),

    constraint fk_file_upload_sessions_file
        foreign key (node_id)
        references files(node_id)
        on delete cascade,

    constraint chk_file_upload_sessions_expires_after_created
        check (expires_at > created_at)
);

create table multipart_uploads (
    session_id uuid primary key,
    storage_upload_id text not null unique,
    part_size bigint not null,
    parts_count integer not null,
    created_at timestamptz not null default now(),
    updated_at timestamptz not null default now(),

    constraint fk_multipart_uploads_session
        foreign key (session_id)
        references file_upload_sessions(id)
        on delete cascade,

    constraint chk_multipart_uploads_storage_upload_id_not_blank
        check (length(btrim(storage_upload_id)) > 0),

    constraint chk_multipart_uploads_part_size
        check (part_size > 0),

    constraint chk_multipart_uploads_parts_count
        check (parts_count > 0)
);

create table multipart_upload_parts (
    session_id uuid not null,
    part_number integer not null,
    etag text not null,
    size bigint not null,
    uploaded_at timestamptz not null default now(),

    primary key (session_id, part_number),

    constraint fk_multipart_upload_parts_multipart_upload
        foreign key (session_id)
        references multipart_uploads(session_id)
        on delete cascade,

    constraint chk_multipart_upload_parts_part_number
        check (part_number > 0),

    constraint chk_multipart_upload_parts_etag_not_blank
        check (length(btrim(etag)) > 0),

    constraint chk_multipart_upload_parts_size
        check (size > 0)
);

create table file_upload_events (
    id uuid primary key default gen_random_uuid(),
    node_id uuid not null,
    session_id uuid null,
    from_status upload_status null,
    to_status upload_status not null,
    reason text null,
    metadata jsonb null,
    created_at timestamptz not null default now(),

    constraint fk_file_upload_events_file
        foreign key (node_id)
        references files(node_id)
        on delete cascade,

    constraint fk_file_upload_events_session
        foreign key (session_id)
        references file_upload_sessions(id)
        on delete set null
);

create unique index uq_nodes_root_name_active
    on nodes ((lower(btrim(name))))
    where parent_id is null
      and deleted_at is null;

create unique index uq_nodes_parent_name_active
    on nodes (parent_id, (lower(btrim(name))))
    where parent_id is not null
      and deleted_at is null;

create index idx_nodes_parent_id_active
    on nodes(parent_id)
    where deleted_at is null;

create index idx_file_upload_sessions_node_id
    on file_upload_sessions(node_id);

create index idx_file_upload_sessions_active_expires_at
    on file_upload_sessions(expires_at)
    where status in ('pending', 'uploading');

create unique index uq_file_upload_sessions_active
    on file_upload_sessions(node_id)
    where status in ('pending', 'uploading');

create index idx_file_upload_events_node_id_created_at
    on file_upload_events(node_id, created_at desc);

create index idx_file_upload_events_session_id_created_at
    on file_upload_events(session_id, created_at desc);
