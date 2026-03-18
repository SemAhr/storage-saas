create extension if not exists "pgcrypto";

create type node_type as enum ('file', 'folder');

create type upload_status as enum ('pending', 'completed', 'failed');

-- it is needed a worker to chance the file status to 'failed' if the upload is not completed within a certain time frame and also delete the node

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
);

create table files (
    node_id uuid primary key,
    mime_type text not null,
    size bigint not null,
    storage_url text null unique,
    status upload_status not null default 'pending',
    created_at timestamptz not null default now(),
    updated_at timestamptz not null default now(),

    constraint fk_files_node
        foreign key (node_id)
        references nodes(id),

    constraint chk_files_size
        check (size >= 0),

    constraint chk_files_status_storage_url
        check (
            (status = 'completed' and storage_url is not null) or
            (status in ('pending', 'failed') and storage_url is null)
        )
);

create unique index uq_nodes_root_name_active
on nodes(name)
where parent_id is null and deleted_at is null;

create unique index uq_nodes_parent_name_active
on nodes(parent_id, name)
where parent_id is not null and deleted_at is null;

create index idx_nodes_parent_id on nodes(parent_id);
create index idx_nodes_type on nodes(type);
create index idx_nodes_deleted_at on nodes(deleted_at);
create index idx_files_status on files(status);
