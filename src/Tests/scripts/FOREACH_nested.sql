-- Create test tables for nesting
CREATE TEMP TABLE outer_table (letter TEXT);
INSERT INTO outer_table VALUES ('A');
INSERT INTO outer_table VALUES ('B');

CREATE TEMP TABLE inner_table (number INTEGER);
INSERT INTO inner_table VALUES (1);
INSERT INTO inner_table VALUES (2);

-- Test nested FOREACH loops (variables auto-declared)
FOREACH (@letter) IN outer_table BEGIN
    FOREACH (@number) IN inner_table BEGIN
        PRINT CONCAT(@letter, @number);
    END
END

--output--
A1
A2
B1
B2 