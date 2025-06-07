-- Create a single column test table
CREATE TEMP TABLE single_col_table (value INTEGER);
INSERT INTO single_col_table VALUES (10);
INSERT INTO single_col_table VALUES (20);
INSERT INTO single_col_table VALUES (30);

-- Test FOREACH with single variable (auto-declared)
FOREACH (@val) IN single_col_table BEGIN
    PRINT @val;
END

--output--
10
20
30 