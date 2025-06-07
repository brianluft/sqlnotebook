CREATE TEMP TABLE test_table (id INTEGER, name TEXT);
INSERT INTO test_table VALUES (1, 'Alice');
INSERT INTO test_table VALUES (2, 'Bob');
-- Variables auto-declared by FOREACH
FOREACH (@id, @name) IN test_table BEGIN
    PRINT CONCAT(@name, ' has ID ', @id);
END

--output--
Alice has ID 1
Bob has ID 2 