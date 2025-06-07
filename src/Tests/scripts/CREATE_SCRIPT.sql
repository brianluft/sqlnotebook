-- Test CREATE SCRIPT statement
CREATE SCRIPT TestScript AS 'PRINT ''Hello from created script'';';
EXECUTE TestScript;

-- Test creating script that already exists (should fail)
BEGIN TRY
    CREATE SCRIPT TestScript AS 'PRINT ''This should fail'';';
END TRY
BEGIN CATCH
    PRINT ERROR_MESSAGE();
END CATCH

--output--
Hello from created script
A script named "TestScript" already exists. 