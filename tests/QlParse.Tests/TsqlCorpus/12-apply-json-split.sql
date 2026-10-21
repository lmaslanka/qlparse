-- expect: Batch(TsqlDeclareStatement,TsqlDeclareStatement,SelectStatement,SelectStatement,SelectStatement,TsqlIfStatement,SelectStatement)
declare @tags nvarchar(400) = N'red,blue,  green ,yellow';
declare @payload nvarchar(max) = N'{"orderId":5001,"customer":{"id":42,"name":"Contoso"},"lines":[{"sku":"BK-M68B-42","qty":2,"price":1229.99},{"sku":"HL-U509","qty":1,"price":34.99}]}';

select ltrim(rtrim(value)) as tag
from string_split(@tags, ',')
where rtrim(value) <> '';

-- latest order per customer
select c.CustomerID, c.AccountNumber, lo.SalesOrderID, lo.OrderDate, lo.TotalDue
from Sales.Customer c
cross apply (
    select top (1) h.SalesOrderID, h.OrderDate, h.TotalDue
    from Sales.SalesOrderHeader h
    where h.CustomerID = c.CustomerID
    order by h.OrderDate desc, h.SalesOrderID desc
) lo;

-- customers with their most recent note, if any
select c.CustomerID, n.NoteText, n.CreatedAt
from Sales.Customer c
outer apply (
    select top (1) cn.NoteText, cn.CreatedAt
    from dbo.CustomerNote cn
    where cn.CustomerID = c.CustomerID
    order by cn.CreatedAt desc
) n
where c.TerritoryID = 4;

if isjson(@payload) = 1
begin
    select json_value(@payload, '$.orderId') as OrderId,
           json_value(@payload, '$.customer.name') as CustomerName,
           json_query(@payload, '$.customer') as CustomerJson,
           l.sku, l.qty, l.price, l.qty * l.price as LineTotal
    from openjson(@payload, '$.lines')
         with (
             sku   varchar(25)   '$.sku',
             qty   int           '$.qty',
             price decimal(10,2) '$.price'
         ) as l;
end

select p.ProductID, p.Name, s.value as Keyword, s.ordinal
from Production.Product p
cross apply string_split(p.Keywords, ';', 1) s
where p.Keywords is not null;
