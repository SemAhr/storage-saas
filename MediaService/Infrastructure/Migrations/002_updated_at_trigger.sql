create or replace function set_updated_at()
returns trigger as $$
begin
    if to_jsonb(new) - 'updated_at' is distinct from to_jsonb(old) - 'updated_at' then
        new.updated_at = now();
    end if;

    return new;
end;
$$ language plpgsql;


do $$
declare
    table_record record;
begin
    for table_record in
        select
            columns.table_schema,
            columns.table_name
        from information_schema.columns as columns
        join information_schema.tables as tables
            on tables.table_schema = columns.table_schema
           and tables.table_name = columns.table_name
        where columns.column_name = 'updated_at'
          and columns.data_type in ('timestamp with time zone', 'timestamp without time zone')
          and tables.table_type = 'BASE TABLE'
          and columns.table_schema not in ('pg_catalog', 'information_schema')
    loop
        execute format(
            'drop trigger if exists trg_set_updated_at on %I.%I;',
            table_record.table_schema,
            table_record.table_name
        );

        execute format(
            'create trigger trg_set_updated_at
             before update on %I.%I
             for each row
             execute function set_updated_at();',
            table_record.table_schema,
            table_record.table_name
        );
    end loop;
end;
$$;
