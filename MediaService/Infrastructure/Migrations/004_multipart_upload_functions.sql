create or replace function create_multipart_upload_session(
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
        'multipart',
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
        'Multipart upload session created',
        jsonb_build_object(
            'uploadMode', 'multipart',
            'objectKey', btrim(p_object_key),
            'mimeType', btrim(p_mime_type),
            'size', p_size,
            'expiresAt', p_expires_at
        )
    );

    return v_session_id;
end;
$$;

-- //

create or replace function attach_multipart_upload(
    p_session_id uuid,
    p_storage_upload_id text,
    p_part_size bigint,
    p_parts_count integer
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
    if p_storage_upload_id is null or length(btrim(p_storage_upload_id)) = 0 then
        raise exception 'Storage upload id cannot be blank';
    end if;

    if p_part_size <= 0 then
        raise exception 'Part size must be greater than zero';
    end if;

    if p_parts_count <= 0 then
        raise exception 'Parts count must be greater than zero';
    end if;

    if p_parts_count > 10000 then
        raise exception 'Parts count cannot exceed 10000';
    end if;

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

    if v_upload_mode <> 'multipart' then
        raise exception 'Upload session is not multipart';
    end if;

    if v_session_status <> 'pending' then
        raise exception 'Multipart upload can only be attached from pending status';
    end if;

    if v_file_status <> 'pending' then
        raise exception 'File must be pending before attaching multipart upload';
    end if;

    if v_expires_at <= now() then
        raise exception 'Upload session has expired';
    end if;

    insert into multipart_uploads (
        session_id,
        storage_upload_id,
        part_size,
        parts_count
    )
    values (
        p_session_id,
        btrim(p_storage_upload_id),
        p_part_size,
        p_parts_count
    );

    update file_upload_sessions
    set
        status = 'uploading',
        updated_at = now()
    where id = p_session_id;

    update files
    set
        status = 'uploading',
        updated_at = now()
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
        'uploading',
        'Multipart upload attached',
        jsonb_build_object(
            'storageUploadId', btrim(p_storage_upload_id),
            'partSize', p_part_size,
            'partsCount', p_parts_count
        )
    );
end;
$$;

-- //

create or replace function register_multipart_upload_part(
    p_session_id uuid,
    p_part_number integer,
    p_etag text,
    p_size bigint
)
returns void
language plpgsql
as $$
declare
    v_session_status upload_status;
    v_file_status upload_status;
    v_upload_mode upload_mode;
    v_expires_at timestamptz;
    v_file_size bigint;
    v_part_size bigint;
    v_parts_count integer;
    v_expected_part_size bigint;
begin
    if p_part_number <= 0 then
        raise exception 'Part number must be greater than zero';
    end if;

    if p_etag is null or length(btrim(p_etag)) = 0 then
        raise exception 'ETag cannot be blank';
    end if;

    if p_size <= 0 then
        raise exception 'Part size must be greater than zero';
    end if;

    select
        sessions.status,
        files.status,
        sessions.upload_mode,
        sessions.expires_at,
        files.size,
        multipart.part_size,
        multipart.parts_count
    into
        v_session_status,
        v_file_status,
        v_upload_mode,
        v_expires_at,
        v_file_size,
        v_part_size,
        v_parts_count
    from file_upload_sessions as sessions
    join files
        on files.node_id = sessions.node_id
    join multipart_uploads as multipart
        on multipart.session_id = sessions.id
    where sessions.id = p_session_id
    for update of sessions;

    if v_parts_count is null then
        raise exception 'Multipart upload not found';
    end if;

    if v_upload_mode <> 'multipart' then
        raise exception 'Upload session is not multipart';
    end if;

    if v_session_status <> 'uploading' then
        raise exception 'Multipart part cannot be registered when session status is %', v_session_status;
    end if;

    if v_file_status <> 'uploading' then
        raise exception 'Multipart part cannot be registered when file status is %', v_file_status;
    end if;

    if v_expires_at <= now() then
        raise exception 'Upload session has expired';
    end if;

    if p_part_number > v_parts_count then
        raise exception 'Part number exceeds expected parts count';
    end if;

    if p_part_number < v_parts_count then
        v_expected_part_size := v_part_size;
    else
        v_expected_part_size := v_file_size - (v_part_size * (v_parts_count - 1));
    end if;

    if p_size <> v_expected_part_size then
        raise exception 'Invalid part size. Expected %, got %', v_expected_part_size, p_size;
    end if;

    insert into multipart_upload_parts (
        session_id,
        part_number,
        etag,
        size
    )
    values (
        p_session_id,
        p_part_number,
        btrim(p_etag),
        p_size
    )
    on conflict (session_id, part_number)
    do update set
        etag = excluded.etag,
        size = excluded.size,
        uploaded_at = now();
end;
$$;

-- //


create or replace function validate_multipart_upload_parts(
    p_session_id uuid
)
returns void
language plpgsql
as $$
declare
    v_session_status upload_status;
    v_file_status upload_status;
    v_upload_mode upload_mode;
    v_expires_at timestamptz;
    v_file_size bigint;
    v_parts_count integer;
    v_uploaded_parts integer;
    v_missing_parts integer;
    v_uploaded_size bigint;
begin
    select
        sessions.status,
        files.status,
        sessions.upload_mode,
        sessions.expires_at,
        files.size,
        multipart.parts_count
    into
        v_session_status,
        v_file_status,
        v_upload_mode,
        v_expires_at,
        v_file_size,
        v_parts_count
    from file_upload_sessions as sessions
    join files
        on files.node_id = sessions.node_id
    join multipart_uploads as multipart
        on multipart.session_id = sessions.id
    where sessions.id = p_session_id;

    if v_parts_count is null then
        raise exception 'Multipart upload not found';
    end if;

    if v_upload_mode <> 'multipart' then
        raise exception 'Upload session is not multipart';
    end if;

    if v_session_status <> 'uploading' then
        raise exception 'Multipart upload cannot be validated from status %', v_session_status;
    end if;

    if v_file_status <> 'uploading' then
        raise exception 'File cannot be validated from status %', v_file_status;
    end if;

    if v_expires_at <= now() then
        raise exception 'Upload session has expired';
    end if;

    select count(*)::integer
    into v_uploaded_parts
    from multipart_upload_parts
    where session_id = p_session_id;

    if v_uploaded_parts <> v_parts_count then
        raise exception 'Multipart upload has % uploaded parts, expected %',
            v_uploaded_parts,
            v_parts_count;
    end if;

    select count(*)::integer
    into v_missing_parts
    from generate_series(1, v_parts_count) as expected(part_number)
    where not exists (
        select 1
        from multipart_upload_parts as actual
        where actual.session_id = p_session_id
          and actual.part_number = expected.part_number
    );

    if v_missing_parts > 0 then
        raise exception 'Multipart upload has missing parts';
    end if;

    select coalesce(sum(size), 0)::bigint
    into v_uploaded_size
    from multipart_upload_parts
    where session_id = p_session_id;

    if v_uploaded_size <> v_file_size then
        raise exception 'Multipart uploaded size mismatch. Expected %, got %',
            v_file_size,
            v_uploaded_size;
    end if;
end;
$$;




create or replace function complete_multipart_upload_session(
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
    v_parts_count integer;
    v_uploaded_parts integer;
begin
    select
        sessions.node_id,
        files.status,
        sessions.status,
        sessions.upload_mode,
        multipart.parts_count
    into
        v_node_id,
        v_file_status,
        v_session_status,
        v_upload_mode,
        v_parts_count
    from file_upload_sessions as sessions
    join files
        on files.node_id = sessions.node_id
    join multipart_uploads as multipart
        on multipart.session_id = sessions.id
    where sessions.id = p_session_id
    for update of sessions, files;

    if v_node_id is null then
        raise exception 'Multipart upload session not found';
    end if;

    if v_upload_mode <> 'multipart' then
        raise exception 'Upload session is not multipart';
    end if;

    if v_session_status <> 'uploading' then
        raise exception 'Multipart upload session cannot be completed from status %', v_session_status;
    end if;

    if v_file_status <> 'uploading' then
        raise exception 'File cannot be completed from status %', v_file_status;
    end if;

    select count(*)::integer
    into v_uploaded_parts
    from multipart_upload_parts
    where session_id = p_session_id;

    if v_uploaded_parts <> v_parts_count then
        raise exception 'Multipart upload has % uploaded parts, expected %',
            v_uploaded_parts,
            v_parts_count;
    end if;

    update file_upload_sessions
    set
        status = 'completed',
        updated_at = now()
    where id = p_session_id;

    update files
    set
        status = 'completed',
        updated_at = now()
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
        'Multipart upload completed',
        p_metadata
    );
end;
$$;


create or replace function register_multipart_upload_parts_batch(
    p_session_id uuid,
    p_parts jsonb
)
returns table (
    part_number integer,
    accepted boolean,
    code text,
    message text
)
language plpgsql
as $$
declare
    v_session_status upload_status;
    v_file_status upload_status;
    v_upload_mode upload_mode;
    v_expires_at timestamptz;
    v_file_size bigint;
    v_part_size bigint;
    v_parts_count integer;
begin
    select
        sessions.status,
        files.status,
        sessions.upload_mode,
        sessions.expires_at,
        files.size,
        multipart.part_size,
        multipart.parts_count
    into
        v_session_status,
        v_file_status,
        v_upload_mode,
        v_expires_at,
        v_file_size,
        v_part_size,
        v_parts_count
    from file_upload_sessions as sessions
    join files
        on files.node_id = sessions.node_id
    join multipart_uploads as multipart
        on multipart.session_id = sessions.id
    where sessions.id = p_session_id;

    if v_parts_count is null then
        raise exception 'Multipart upload not found';
    end if;

    if v_upload_mode <> 'multipart' then
        raise exception 'Upload session is not multipart';
    end if;

    if v_session_status <> 'uploading' then
        raise exception 'Multipart upload is not uploading';
    end if;

    if v_file_status <> 'uploading' then
        raise exception 'File is not uploading';
    end if;

    if v_expires_at <= now() then
        raise exception 'Upload session has expired';
    end if;

    return query
    with input_parts as (
        select
            (part_item->>'partNumber')::integer as part_number,
            part_item->>'etag' as etag,
            (part_item->>'size')::bigint as size
        from jsonb_array_elements(p_parts) as part_item
    ),
    evaluated_parts as (
        select
            input_parts.part_number,
            input_parts.etag,
            input_parts.size,
            case
                when input_parts.part_number is null then false
                when input_parts.part_number <= 0 then false
                when input_parts.part_number > v_parts_count then false
                when input_parts.etag is null or length(btrim(input_parts.etag)) = 0 then false
                when input_parts.size <= 0 then false
                when input_parts.size <>
                    case
                        when input_parts.part_number < v_parts_count
                            then v_part_size
                        else v_file_size - (v_part_size * (v_parts_count - 1))
                    end then false
                else true
            end as accepted,
            case
                when input_parts.part_number is null then 'PART_NUMBER_REQUIRED'
                when input_parts.part_number <= 0 then 'INVALID_PART_NUMBER'
                when input_parts.part_number > v_parts_count then 'PART_NUMBER_OUT_OF_RANGE'
                when input_parts.etag is null or length(btrim(input_parts.etag)) = 0 then 'ETAG_REQUIRED'
                when input_parts.size <= 0 then 'INVALID_PART_SIZE'
                when input_parts.size <>
                    case
                        when input_parts.part_number < v_parts_count
                            then v_part_size
                        else v_file_size - (v_part_size * (v_parts_count - 1))
                    end then 'PART_SIZE_MISMATCH'
                else null
            end as code
        from input_parts
    ),
    inserted_parts as (
        insert into multipart_upload_parts (
            session_id,
            part_number,
            etag,
            size
        )
        select
            p_session_id,
            evaluated_parts.part_number,
            btrim(evaluated_parts.etag),
            evaluated_parts.size
        from evaluated_parts
        where evaluated_parts.accepted = true
        on conflict (session_id, part_number)
        do update set
            etag = excluded.etag,
            size = excluded.size,
            uploaded_at = now()
        returning part_number
    )
    select
        evaluated_parts.part_number,
        evaluated_parts.accepted,
        evaluated_parts.code,
        case evaluated_parts.code
            when 'PART_NUMBER_REQUIRED' then 'Part number is required.'
            when 'INVALID_PART_NUMBER' then 'Part number must be greater than zero.'
            when 'PART_NUMBER_OUT_OF_RANGE' then 'Part number exceeds expected parts count.'
            when 'ETAG_REQUIRED' then 'ETag is required.'
            when 'INVALID_PART_SIZE' then 'Part size must be greater than zero.'
            when 'PART_SIZE_MISMATCH' then 'Part size does not match expected size.'
            else null
        end as message
    from evaluated_parts
    order by evaluated_parts.part_number;
end;
$$;


create or replace function finish_multipart_upload_session(
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
        raise exception 'Invalid terminal status for multipart upload: %', p_status;
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

    if v_upload_mode <> 'multipart' then
        raise exception 'Upload session is not multipart';
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
    set
        status = p_status,
        updated_at = now()
    where id = p_session_id;

    update files
    set
        status = p_status,
        updated_at = now()
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


create or replace function expire_multipart_upload_sessions(
    p_limit integer default 100
)
returns table (
    session_id uuid,
    node_id uuid,
    object_key text,
    storage_upload_id text,
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
            multipart.storage_upload_id,
            files.status as previous_status,
            sessions.expires_at
        from file_upload_sessions as sessions
        join files
            on files.node_id = sessions.node_id
        left join multipart_uploads as multipart
            on multipart.session_id = sessions.id
        where sessions.upload_mode = 'multipart'
          and sessions.status in ('pending', 'uploading')
          and sessions.expires_at <= now()
        order by sessions.expires_at asc
        limit p_limit
        for update of sessions, files skip locked
    ),
    updated_sessions as (
        update file_upload_sessions as sessions
        set
            status = 'expired',
            updated_at = now()
        from expired_sessions
        where sessions.id = expired_sessions.session_id
        returning sessions.id
    ),
    updated_files as (
        update files as files
        set
            status = 'expired',
            updated_at = now()
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
            'Multipart upload session expired',
            jsonb_build_object(
                'expiredAt', now(),
                'objectKey', expired_sessions.object_key,
                'storageUploadId', expired_sessions.storage_upload_id,
                'expiresAt', expired_sessions.expires_at
            )
        from expired_sessions
        returning id
    )
    select
        expired_sessions.session_id,
        expired_sessions.node_id,
        expired_sessions.object_key,
        expired_sessions.storage_upload_id,
        expired_sessions.previous_status,
        expired_sessions.expires_at
    from expired_sessions;
end;
$$;

-- // ======================== \\ --

create or replace function get_multipart_upload_for_abort(
    p_session_id uuid
)
returns table (
    session_id uuid,
    node_id uuid,
    object_key text,
    storage_upload_id text,
    session_status upload_status,
    file_status upload_status
)
language sql
as $$
    select
        sessions.id as session_id,
        files.node_id,
        files.object_key,
        multipart.storage_upload_id,
        sessions.status as session_status,
        files.status as file_status
    from file_upload_sessions as sessions
    join files
        on files.node_id = sessions.node_id
    left join multipart_uploads as multipart
        on multipart.session_id = sessions.id
    where sessions.id = p_session_id
      and sessions.upload_mode = 'multipart';
$$;
