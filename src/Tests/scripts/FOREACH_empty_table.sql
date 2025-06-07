-- Create an empty test table
CREATE TEMP TABLE empty_table (id INTEGER, name TEXT);

-- Test FOREACH with empty table (should not execute body, variables auto-declared)
PRINT 'Before FOREACH';
FOREACH (@id, @name) IN empty_table BEGIN
    PRINT 'This should not print';
END
PRINT 'After FOREACH';

--output--
Before FOREACH
After FOREACH 