== format ==
SELECT
    *,
    p.*,
    p.record_id,
    p.first_name AS given_name,
    p.last_name,
    p.age,
    p.eye_color,
    p.email,
    p.phone_number,
    public.person.status,
    count(*),
    coalesce(o.job_title, 'none') AS title,
    a.city,
    a.province,
    a.country,
    a.postal_code,
    o.department,
    o.salary
FROM public.person AS p
INNER JOIN public.address AS a
    ON p.address_id = a.address_id
LEFT JOIN occupation AS o
    ON p.occupation_id = o.occupation_id
RIGHT OUTER JOIN department AS d
    ON o.department_id = d.department_id
FULL JOIN region AS r
    ON a.region_id = r.region_id
JOIN country AS c
    ON a.country_id = c.country_id
CROSS JOIN calendar AS cal
NATURAL LEFT JOIN status_flag
NATURAL JOIN audit
INNER JOIN extra
    USING (record_id, tenant_id)
WHERE p.record_id != 0
    AND p.age BETWEEN 18 AND 65
    AND p.eye_color IN ('blue', 'green', 'hazel', 'brown')
    AND p.status = 'active'
    AND a.country = 'Canada'
    AND p.age > 17
    AND p.age >= 18
    AND p.age < 66
    AND p.age <= 65
    AND p.record_id != 1
    AND NOT a.city IS NULL
    AND (o.salary IS NULL OR o.salary >= 50000)
    AND o.job_title IS NOT NULL
GROUP BY p.record_id, p.first_name, p.last_name, p.age, p.eye_color, p.email, p.phone_number, public.person.status, a.city, a.province, a.country, a.postal_code, o.department, o.salary, o.job_title
HAVING count(*) != 0
    AND NOT p.age IS NOT NULL
ORDER BY p.last_name DESC, p.first_name ASC, p.record_id
LIMIT 10
OFFSET 5

== stats ==
File: sample.sql
Source: 1350 chars, 2 lines
Tokens: 372
Comments: 0
Output: 1496 chars
Lexer: 2 ms
Parser: 3.47 ms
Formatter: 1.92 ms
Dump: 2.14 ms
Total: 10.38 ms
