IMPORT CSV '<FILES>\missing-header.csv' INTO foo;

SELECT * FROM foo;

--output--
A,column2,B,column4,C
111,222,333,444,555
666,777,888,999,0
-
