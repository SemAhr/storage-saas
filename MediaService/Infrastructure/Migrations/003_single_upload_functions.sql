create or replace function create_single_upload_session(
    p_node_id uuid,
    p_parent_id uuid,
    p_name text,
    p_mime_type text,
    p_size bigint,
    p_object_key text,
    p_expires_at timestamptz
)
returns uuid
language plpgsql
as $$
declare
    v_session_id uuid;
begin
    if p_node_id is null then
        raise exception 'Node id cannot be null';
    end if;

    if p_name is null or length(btrim(p_name)) = 0 then
        raise exception 'Node name cannot be blank';
    end if;

    if p_mime_type is null or length(btrim(p_mime_type)) = 0 then
        raise exception 'Mime type cannot be blank';
    end if;

    if p_object_key is null or length(btrim(p_object_key)) = 0 then
        raise exception 'Object key cannot be blank';
    end if;

    if p_size <= 0 then
        raise exception 'File size must be greater than zero';
    end if;

    if p_expires_at <= now() then
        raise exception 'Expiration must be in the future';
    end if;

    if p_parent_id is not null and not exists (
        select 1
        from nodes
        where id = p_parent_id
          and type = 'folder'
          and deleted_at is null
    ) then
        raise exception 'Parent node does not exist or is not an active folder';
    end if;

    insert into nodes (
        id,
        parent_id,
        name,
        type
    )
    values (
        p_node_id,
        p_parent_id,
        btrim(p_name),
        'file'
    );

    insert into files (
        node_id,
        mime_type,
        size,
        object_key,
        status
    )
    values (
        p_node_id,
        btrim(p_mime_type),
        p_size,
        btrim(p_object_key),
        'pending'
    );

    insert into file_upload_sessions (
        node_id,
        upload_mode,
        status,
        expires_at
    )
    values (
        p_node_id,
        'single',
        'pending',
        p_expires_at
    )
    returning id into v_session_id;

    insert into file_upload_events (
        node_id,
        session_id,
        from_status,
        to_status,
        reason,
        metadata
    )
    values (
        p_node_id,
        v_session_id,
        null,
        'pending',
        'Single upload session created',
        jsonb_build_object(
            'uploadMode', 'single',
            'objectKey', btrim(p_object_key),
            'mimeType', btrim(p_mime_type),
            'size', p_size,
            'expiresAt', p_expires_at
        )
    );

    return v_session_id;
end;
$$;


create or replace function get_single_upload_session_for_confirmation(
    p_session_id uuid
)
returns table (
    session_id uuid,
    node_id uuid,
    object_key text,
    mime_type text,
    size bigint,
    expires_at timestamptz,
    session_status upload_status,
    file_status upload_status
)
language sql
as $$
    select
        sessions.id as session_id,
        files.node_id,
        files.object_key,
        files.mime_type,
        files.size,
        sessions.expires_at,
        sessions.status as session_status,
        files.status as file_status
    from file_upload_sessions as sessions
    join files
        on files.node_id = sessions.node_id
    where sessions.id = p_session_id
      and sessions.upload_mode = 'single';
$$;


create or replace function complete_single_upload_session(
    p_session_id uuid,
    p_metadata jsonb default null
)
returns void
language plpgsql
as $$
declare
    v_node_id uuid;
    v_file_status upload_status;
    v_session_status upload_status;
    v_upload_mode upload_mode;
    v_expires_at timestamptz;
begin
    select
        sessions.node_id,
        files.status,
        sessions.status,
        sessions.upload_mode,
        sessions.expires_at
    into
        v_node_id,
        v_file_status,
        v_session_status,
        v_upload_mode,
        v_expires_at
    from file_upload_sessions as sessions
    join files
        on files.node_id = sessions.node_id
    where sessions.id = p_session_id
    for update of sessions, files;

    if v_node_id is null then
        raise exception 'Upload session not found';
    end if;

    if v_upload_mode <> 'single' then
        raise exception 'Upload session is not single upload';
    end if;

    if v_session_status <> 'pending' then
        raise exception 'Single upload session cannot be completed from status %', v_session_status;
    end if;

    if v_file_status <> 'pending' then
        raise exception 'File cannot be completed from status %', v_file_status;
    end if;

    if v_expires_at <= now() then
        raise exception 'Upload session has expired';
    end if;

    update file_upload_sessions
    set status = 'completed'
    where id = p_session_id;

    update files
    set status = 'completed'
    where node_id = v_node_id;

    insert into file_upload_events (
        node_id,
        session_id,
        from_status,
        to_status,
        reason,
        metadata
    )
    values (
        v_node_id,
        p_session_id,
        v_file_status,
        'completed',
        'Single upload completed',
        p_metadata
    );
end;
$$;


create or replace function finish_single_upload_session(
    p_session_id uuid,
    p_status upload_status,
    p_reason text default null,
    p_metadata jsonb default null
)
returns void
language plpgsql
as $$
declare
    v_node_id uuid;
    v_file_status upload_status;
    v_session_status upload_status;
    v_upload_mode upload_mode;
begin
    if p_status not in ('failed', 'canceled', 'expired') then
        raise exception 'Invalid terminal status for single upload: %', p_status;
    end if;

    select
        sessions.node_id,
        files.status,
        sessions.status,
        sessions.upload_mode
    into
        v_node_id,
        v_file_status,
        v_session_status,
        v_upload_mode
    from file_upload_sessions as sessions
    join files
        on files.node_id = sessions.node_id
    where sessions.id = p_session_id
    for update of sessions, files;

    if v_node_id is null then
        raise exception 'Upload session not found';
    end if;

    if v_upload_mode <> 'single' then
        raise exception 'Upload session is not single upload';
    end if;

    if v_session_status = p_status then
        return;
    end if;

    if v_session_status in ('completed', 'failed', 'canceled', 'expired') then
        raise exception 'Upload session is already terminal with status %', v_session_status;
    end if;

    if v_file_status in ('completed', 'failed', 'canceled', 'expired') then
        raise exception 'File is already terminal with status %', v_file_status;
    end if;

    update file_upload_sessions
    set status = p_status
    where id = p_session_id;

    update files
    set status = p_status
    where node_id = v_node_id;

    insert into file_upload_events (
        node_id,
        session_id,
        from_status,
        to_status,
        reason,
        metadata
    )
    values (
        v_node_id,
        p_session_id,
        v_file_status,
        p_status,
        p_reason,
        p_metadata
    );
end;
$$;


create or replace function expire_single_upload_sessions(
    p_limit integer default 100
)
returns table (
    session_id uuid,
    node_id uuid,
    object_key text,
    previous_status upload_status,
    expires_at timestamptz
)
language plpgsql
as $$
begin
    if p_limit <= 0 then
        raise exception 'Limit must be greater than zero';
    end if;

    return query
    with expired_sessions as (
        select
            sessions.id as session_id,
            sessions.node_id,
            files.object_key,
            files.status as previous_status,
            sessions.expires_at
        from file_upload_sessions as sessions
        join files
            on files.node_id = sessions.node_id
        where sessions.upload_mode = 'single'
          and sessions.status = 'pending'
          and sessions.expires_at <= now()
        order by sessions.expires_at asc
        limit p_limit
        for update of sessions, files skip locked
    ),
    updated_sessions as (
        update file_upload_sessions as sessions
        set status = 'expired'
        from expired_sessions
        where sessions.id = expired_sessions.session_id
        returning sessions.id
    ),
    updated_files as (
        update files as files
        set status = 'expired'
        from expired_sessions
        where files.node_id = expired_sessions.node_id
        returning files.node_id
    ),
    inserted_events as (
        insert into file_upload_events (
            node_id,
            session_id,
            from_status,
            to_status,
            reason,
            metadata
        )
        select
            expired_sessions.node_id,
            expired_sessions.session_id,
            expired_sessions.previous_status,
            'expired',
            'Single upload session expired',
            jsonb_build_object(
                'expiredAt', now(),
                'objectKey', expired_sessions.object_key,
                'expiresAt', expired_sessions.expires_at
            )
        from expired_sessions
        returning id
    )
    select
        expired_sessions.session_id,
        expired_sessions.node_id,
        expired_sessions.object_key,
        expired_sessions.previous_status,
        expired_sessions.expires_at
    from expired_sessions;
end;
$$;
