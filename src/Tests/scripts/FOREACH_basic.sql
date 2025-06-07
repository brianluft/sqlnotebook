-- Create a test table
CREATE TEMP TABLE test_table (id INTEGER, name TEXT, value INTEGER);
INSERT INTO test_table VALUES (1, 'Alice', 100);
INSERT INTO test_table VALUES (2, 'Bob', 200);
INSERT INTO test_table VALUES (3, 'Charlie', 300);

-- Test basic FOREACH functionality (variables auto-declared)
FOREACH (@id, @name, @value) IN test_table BEGIN
    PRINT CONCAT(@name, ' has ID ', @id, ' and value ', @value);
END

--output--
Alice has ID 1 and value 100
Bob has ID 2 and value 200
Charlie has ID 3 and value 300