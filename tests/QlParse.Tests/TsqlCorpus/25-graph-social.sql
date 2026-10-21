-- expect: Batch(TsqlDropObjectsStatement,CreateTableStatement,CreateTableStatement,CreateTableStatement,CreateTableStatement,TsqlInsertStatement,TsqlInsertStatement,TsqlInsertStatement,TsqlInsertStatement,SelectStatement,SelectStatement,TsqlDeleteStatement)
DROP TABLE IF EXISTS dbo.FriendOf, dbo.LivesIn, dbo.Person, dbo.City;

CREATE TABLE dbo.Person (PersonID int PRIMARY KEY, name nvarchar(100) NOT NULL) AS NODE;
CREATE TABLE dbo.City (CityID int PRIMARY KEY, name nvarchar(100) NOT NULL, country char(2) NULL) AS NODE;
CREATE TABLE dbo.FriendOf (since date NULL, CONSTRAINT EC_FriendOf CONNECTION (dbo.Person TO dbo.Person)) AS EDGE;
CREATE TABLE dbo.LivesIn AS EDGE;

INSERT INTO dbo.Person (PersonID, name) VALUES (1, N'Alice'), (2, N'Bob'), (3, N'Carol'), (4, N'Dan');
INSERT INTO dbo.City (CityID, name, country) VALUES (1, N'Seattle', 'US'), (2, N'Warsaw', 'PL');

INSERT INTO dbo.FriendOf ($from_id, $to_id, since)
SELECT p1.$node_id, p2.$node_id, '2019-05-01'
FROM dbo.Person AS p1, dbo.Person AS p2
WHERE (p1.PersonID = 1 AND p2.PersonID = 2)
   OR (p1.PersonID = 2 AND p2.PersonID = 3)
   OR (p1.PersonID = 3 AND p2.PersonID = 4);

INSERT INTO dbo.LivesIn ($from_id, $to_id)
VALUES ((SELECT $node_id FROM dbo.Person WHERE PersonID = 1), (SELECT $node_id FROM dbo.City WHERE CityID = 1)),
       ((SELECT $node_id FROM dbo.Person WHERE PersonID = 3), (SELECT $node_id FROM dbo.City WHERE CityID = 2));

-- friends of friends of Alice and where they live
SELECT p1.name AS person, p3.name AS friend_of_friend, c.name AS city
FROM dbo.Person AS p1, dbo.FriendOf AS f1, dbo.Person AS p2, dbo.FriendOf AS f2, dbo.Person AS p3, dbo.LivesIn AS l, dbo.City AS c
WHERE MATCH(p1-(f1)->p2-(f2)->p3 AND p3-(l)->c)
  AND p1.name = N'Alice';

-- shortest path (SQL Server 2019+)
SELECT p1.name,
       STRING_AGG(p2.name, '->') WITHIN GROUP (GRAPH PATH) AS friends,
       LAST_VALUE(p2.name) WITHIN GROUP (GRAPH PATH) AS last_friend
FROM dbo.Person AS p1, dbo.FriendOf FOR PATH AS fo, dbo.Person FOR PATH AS p2
WHERE MATCH(SHORTEST_PATH(p1(-(fo)->p2)+))
  AND p1.name = N'Alice';

DELETE FROM dbo.FriendOf
WHERE $from_id = (SELECT $node_id FROM dbo.Person WHERE PersonID = 3);
