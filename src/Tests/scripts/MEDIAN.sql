drop table if exists tbl;
create table tbl (foo);
insert into tbl values (1), (49), (50)
select median(foo) as x from tbl;
--output--
x
49
-
