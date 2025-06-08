IMPORT DATABASE 'sqlite'
CONNECTION 'Data Source=<FILES>\example.sqlite3'
TABLE employees;

SELECT * FROM employees ORDER BY id;

--output--
id,name,department,salary,hire_date
1,Alice,Engineering,95000,6/1/2018
2,Bob,Sales,65000,7/15/2019
3,Charlie,HR,60000,3/22/2020
4,Diana,Engineering,98000,11/30/2017
5,Evan,Marketing,72000,1/10/2021
- 