-- expect: Batch(TsqlDeclareStatement,TsqlDeclareStatement,SelectStatement,WithQuery,SelectStatement)
DECLARE @tz sysname = N'Central European Standard Time';
DECLARE @from datetime2(0) = '2024-03-01', @to datetime2(0) = '2024-04-01';

SELECT o.OrderID,
       o.CreatedAtUtc,
       o.CreatedAtUtc AT TIME ZONE 'UTC' AT TIME ZONE @tz AS CreatedLocal,
       CONVERT(date, SWITCHOFFSET(o.CreatedAtOffset, '+01:00')) AS CreatedDateCET,
       TODATETIMEOFFSET(o.CreatedAtUtc, 0) AS CreatedOffset
FROM dbo.Orders AS o
WHERE o.CreatedAtUtc >= @from AND o.CreatedAtUtc < @to;

-- hourly buckets including empty hours (SQL Server 2022)
WITH hours AS
(
    SELECT DATEADD(HOUR, gs.value, @from) AS bucket_start
    FROM GENERATE_SERIES(0, DATEDIFF(HOUR, @from, @to) - 1) AS gs
),
orders AS
(
    SELECT DATE_BUCKET(HOUR, 1, o.CreatedAtUtc) AS bucket_start, COUNT(*) AS cnt, SUM(o.Amount) AS amount
    FROM dbo.Orders AS o
    WHERE o.CreatedAtUtc >= @from AND o.CreatedAtUtc < @to
    GROUP BY DATE_BUCKET(HOUR, 1, o.CreatedAtUtc)
)
SELECT h.bucket_start,
       DATETRUNC(day, h.bucket_start) AS [day],
       ISNULL(o.cnt, 0) AS order_count,
       ISNULL(o.amount, 0) AS amount,
       AVG(ISNULL(o.amount, 0)) OVER w AS moving_avg_24h,
       MAX(ISNULL(o.cnt, 0)) OVER (PARTITION BY DATETRUNC(day, h.bucket_start)) AS peak_in_day
FROM hours AS h
LEFT JOIN orders AS o ON o.bucket_start = h.bucket_start
WINDOW w AS (ORDER BY h.bucket_start ROWS BETWEEN 23 PRECEDING AND CURRENT ROW)
ORDER BY h.bucket_start;

SELECT c.CustomerID,
       GREATEST(c.LastOrderDate, c.LastLoginDate, c.LastContactDate) AS LastActivity,
       LEAST(c.CreditLimit, 10000) AS CappedLimit,
       EOMONTH(c.LastOrderDate) AS LastOrderMonthEnd,
       DATEDIFF(MONTH, c.LastOrderDate, GETDATE()) AS months_since_order
FROM dbo.Customers AS c
WHERE c.Region IS DISTINCT FROM c.BillingRegion;
