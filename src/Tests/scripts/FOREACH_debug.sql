CREATE TEMP TABLE test_table (value INTEGER);
INSERT INTO test_table VALUES (42);
DECLARE @x;
FOREACH (@x) IN test_table BEGIN
    PRINT @x;
END

--output--
42 