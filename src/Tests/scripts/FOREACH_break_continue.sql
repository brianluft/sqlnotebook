-- Create a test table
CREATE TEMP TABLE control_table (value INTEGER);
INSERT INTO control_table VALUES (1);
INSERT INTO control_table VALUES (2);
INSERT INTO control_table VALUES (3);
INSERT INTO control_table VALUES (4);
INSERT INTO control_table VALUES (5);

-- Test CONTINUE (skip printing 3), variable auto-declared
PRINT 'Testing CONTINUE:';
FOREACH (@val) IN control_table BEGIN
    IF @val = 3 CONTINUE;
    PRINT @val;
END

-- Test BREAK (stop at 3)
PRINT 'Testing BREAK:';
FOREACH (@val) IN control_table BEGIN
    IF @val = 3 BREAK;
    PRINT @val;
END

--output--
Testing CONTINUE:
1
2
4
5
Testing BREAK:
1
2 