CREATE OR REPLACE FUNCTION fn_queue_get_top(p_channel int)
RETURNS SETOF queues AS $$
BEGIN
    RETURN QUERY
    UPDATE queues
    SET status = 2 --- InProcess
    WHERE id = (
        SELECT id
        FROM queues
        WHERE status = 1 --- Pending
            AND channel = p_channel
            AND (process_at IS NULL OR process_at <= now())
        ORDER BY priority DESC, created_at ASC, id ASC
        LIMIT 1
        FOR UPDATE SKIP LOCKED
    )
    RETURNING *;
END;
$$ LANGUAGE plpgsql;
