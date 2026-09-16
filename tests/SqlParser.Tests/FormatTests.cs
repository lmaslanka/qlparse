namespace SqlParser.Tests;

public sealed class FormatTests
{
    [Fact]
    public void Formats_select_from()
    {
        AssertFormatted(
            "select a from t",
            """
            SELECT
                a
            FROM t
            """);
    }

    [Fact]
    public void Formats_multiple_select_columns()
    {
        AssertFormatted(
            "select a,b from t",
            """
            SELECT
                a,
                b
            FROM t
            """);
    }

    [Fact]
    public void Formats_where_equality()
    {
        AssertFormatted(
            "select a from t where x=1",
            """
            SELECT
                a
            FROM t
            WHERE x = 1
            """);
    }

    [Fact]
    public void Formats_where_and_or_with_precedence()
    {
        AssertFormatted(
            "select a from t where x=1 or y=2 and z=3",
            """
            SELECT
                a
            FROM t
            WHERE x = 1 OR y = 2 AND z = 3
            """);
    }

    [Fact]
    public void Preserves_line_and_block_comments()
    {
        AssertFormatted(
            """
            select a -- col
            from /* table */ t
            """,
            """
            SELECT
                a -- col
            FROM /* table */ t
            """);
    }

    [Fact]
    public void Uppercases_keywords_and_lowercases_identifiers()
    {
        AssertFormatted(
            "Select A From T Where X=1",
            """
            SELECT
                a
            FROM t
            WHERE x = 1
            """);
    }

    [Fact]
    public void Formats_qualified_names_alias_and_drops_semicolon()
    {
        AssertFormatted(
            "SELECT p.first_name, p.last_name, p.age, p.eye_color FROM person As p WHERE p.record_id = 42;",
            """
            SELECT
                p.first_name,
                p.last_name,
                p.age,
                p.eye_color
            FROM person AS p
            WHERE p.record_id = 42
            """);
    }

    [Fact]
    public void Formats_bare_table_alias_with_as()
    {
        AssertFormatted(
            "select a from person p",
            """
            SELECT
                a
            FROM person AS p
            """);
    }

    [Fact]
    public void Formats_sample_query()
    {
        AssertFormatted(
            File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "sample.sql")),
            """
            -- person directory extract
            SELECT DISTINCT
                *,
                p.*,
                p.record_id,
                p.first_name AS given_name,
                p.last_name,
                p.first_name || ' ' || p.last_name AS full_name,
                ARRAY[p.record_id::TEXT],
                p.age,
                p.eye_color,
                p.email,
                p.phone_number,
                public.person.status,
                count(*),
                coalesce(o.job_title, 'none') AS title,
                CASE /* age band */
                    WHEN p.age < 18 THEN 'minor'
                    WHEN p.age >= 65 THEN 'senior'
                    ELSE 'adult'
                END AS age_group,
                a.city,
                a.province,
                a.country,
                a.postal_code,
                o.department,
                o.salary,
                CAST(o.salary AS NUMERIC),
                o.salary::NUMERIC(10, 2),
                o.salary * 1.1 AS raised,
                (
                    SELECT
                        max(salary)
                    FROM occupation
                ) AS cap
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
            JOIN (
                SELECT
                    record_id
                FROM watchlist
            ) AS w
                ON p.record_id = w.record_id
            WHERE p.record_id != 0
                AND p.age BETWEEN 18 AND 65
                AND p.eye_color IN ('blue', 'green', 'hazel', 'brown')
                AND p.status = 'active'
                AND p.last_name LIKE 'S%'
                AND p.email NOT LIKE '%test%'
                AND a.country = 'Canada'
                AND p.age > 17
                AND p.age >= 18
                AND p.age < 66
                AND p.age <= 65
                AND p.record_id != 1
                AND NOT a.city IS NULL
                AND (o.salary IS NULL OR o.salary >= 50000)
                AND o.job_title IS NOT NULL
                AND p.record_id IN (
                    SELECT
                        record_id
                    FROM watchlist
                )
                AND p.record_id NOT IN (
                    SELECT
                        person_id
                    FROM ban
                )
                AND EXISTS (
                    SELECT
                        1
                    FROM login
                    WHERE login.person_id = p.record_id
                )
                AND NOT EXISTS (
                    SELECT
                        1
                    FROM ban
                    WHERE ban.person_id = p.record_id
                )
                AND o.salary > ALL (
                    SELECT
                        min_salary
                    FROM pay_band
                )
                AND o.salary >= ANY (
                    SELECT
                        mid_salary
                    FROM pay_band
                )
                AND p.age < SOME (
                    SELECT
                        max_age
                    FROM age_limit
                )
            GROUP BY p.record_id, p.first_name, p.last_name, p.age, p.eye_color, p.email, p.phone_number, public.person.status, a.city, a.province, a.country, a.postal_code, o.department, o.salary, o.job_title
            HAVING count(*) != 0
                AND NOT p.age IS NOT NULL
            UNION ALL
            SELECT
                p.record_id,
                p.first_name,
                p.last_name
            FROM public.person AS p
            WHERE p.status = 'alumni'
            INTERSECT
            SELECT
                p.record_id,
                p.first_name,
                p.last_name
            FROM public.person AS p
            WHERE p.record_id IN (
                SELECT
                    person_id
                FROM alumni
            )
            EXCEPT
            SELECT
                p.record_id,
                p.first_name,
                p.last_name
            FROM public.person AS p
            WHERE EXISTS (
                SELECT
                    1
                FROM suppress
                WHERE suppress.person_id = p.record_id
            )
            ORDER BY p.last_name DESC, p.first_name ASC, p.record_id
            LIMIT 10
            OFFSET 5
            """);
    }

    [Fact]
    public void Formats_all_join_kinds()
    {
        AssertFormatted(
            "select a from t join u on t.id=u.id right outer join v as v on t.id=v.id full join w on t.id=w.id cross join x natural left join y natural join z inner join q using (id, name)",
            """
            SELECT
                a
            FROM t
            JOIN u
                ON t.id = u.id
            RIGHT OUTER JOIN v AS v
                ON t.id = v.id
            FULL JOIN w
                ON t.id = w.id
            CROSS JOIN x
            NATURAL LEFT JOIN y
            NATURAL JOIN z
            INNER JOIN q
                USING (id, name)
            """);
    }

    [Fact]
    public void Formats_star_column_alias_schema_group_order_limit()
    {
        AssertFormatted(
            "select *, p.name as n, public.person.x from public.person p group by p.name, p.id order by p.name desc, p.id asc limit 10",
            """
            SELECT
                *,
                p.name AS n,
                public.person.x
            FROM public.person AS p
            GROUP BY p.name, p.id
            ORDER BY p.name DESC, p.id ASC
            LIMIT 10
            """);
    }

    [Fact]
    public void Formats_qualified_star()
    {
        AssertFormatted(
            "select t.* from t",
            """
            SELECT
                t.*
            FROM t
            """);
    }

    [Fact]
    public void Formats_functions_not_is_not_null_not_equals_having_offset()
    {
        AssertFormatted(
            "select count(*), coalesce(p.name,'x') from p group by p.id having count(*)!=0 and not p.id is not null offset 5 limit 10",
            """
            SELECT
                count(*),
                coalesce(p.name, 'x')
            FROM p
            GROUP BY p.id
            HAVING count(*) != 0
                AND NOT p.id IS NOT NULL
            LIMIT 10
            OFFSET 5
            """);
    }

    [Fact]
    public void Formats_case_when()
    {
        AssertFormatted(
            "select case when p.age < 18 then 'minor' when p.age >= 65 then 'senior' else 'adult' end as age_group from p",
            """
            SELECT
                CASE
                    WHEN p.age < 18 THEN 'minor'
                    WHEN p.age >= 65 THEN 'senior'
                    ELSE 'adult'
                END AS age_group
            FROM p
            """);
    }

    [Fact]
    public void Formats_arithmetic_and_unary_minus()
    {
        AssertFormatted(
            "select a+b*c, -p.age, o.salary * 1.1 as raised from t",
            """
            SELECT
                a + b * c,
                -p.age,
                o.salary * 1.1 AS raised
            FROM t
            """);
    }

    [Fact]
    public void Formats_derived_table_in_from()
    {
        AssertFormatted(
            "select x.a from (select a from t) as x",
            """
            SELECT
                x.a
            FROM (
                SELECT
                    a
                FROM t
            ) AS x
            """);
    }

    [Fact]
    public void Formats_derived_table_in_join()
    {
        AssertFormatted(
            "select t.a from t join (select id from u) x on t.id=x.id",
            """
            SELECT
                t.a
            FROM t
            JOIN (
                SELECT
                    id
                FROM u
            ) AS x
                ON t.id = x.id
            """);
    }

    [Fact]
    public void Formats_nested_subqueries()
    {
        AssertFormatted(
            "select x.a from (select (select b from t) as a from u) x where x.a in (select id from v)",
            """
            SELECT
                x.a
            FROM (
                SELECT
                    (
                        SELECT
                            b
                        FROM t
                    ) AS a
                FROM u
            ) AS x
            WHERE x.a IN (
                SELECT
                    id
                FROM v
            )
            """);
    }

    [Fact]
    public void Formats_parenthesized_scalar_subquery()
    {
        AssertFormatted(
            "select ((select a from t)) from u",
            """
            SELECT
                ((
                    SELECT
                        a
                    FROM t
                ))
            FROM u
            """);
    }

    [Fact]
    public void Formats_union()
    {
        AssertFormatted(
            "select a from t union select b from u",
            """
            SELECT
                a
            FROM t
            UNION
            SELECT
                b
            FROM u
            """);
    }

    [Fact]
    public void Formats_with_cte()
    {
        AssertFormatted(
            "with x as (select a from t) select a from x",
            """
            WITH
                x AS (
                    SELECT
                        a
                    FROM t
                )
            SELECT
                a
            FROM x
            """);
    }

    [Fact]
    public void Formats_multiple_ctes_with_column_list()
    {
        AssertFormatted(
            "with x as (select a from t), y(a, b) as (select 1, 2 from u) select a from y",
            """
            WITH
                x AS (
                    SELECT
                        a
                    FROM t
                ),
                y(a, b) AS (
                    SELECT
                        1,
                        2
                    FROM u
                )
            SELECT
                a
            FROM y
            """);
    }

    [Fact]
    public void Formats_recursive_cte_with_union_body()
    {
        AssertFormatted(
            "with recursive x as (select a from t union select a from x) select a from x union select a from y",
            """
            WITH RECURSIVE
                x AS (
                    SELECT
                        a
                    FROM t
                    UNION
                    SELECT
                        a
                    FROM x
                )
            SELECT
                a
            FROM x
            UNION
            SELECT
                a
            FROM y
            """);
    }

    [Fact]
    public void Formats_with_scalar_subquery()
    {
        AssertFormatted(
            "select (with x as (select a from t) select a from x) from u",
            """
            SELECT
                (
                    WITH
                        x AS (
                            SELECT
                                a
                            FROM t
                        )
                    SELECT
                        a
                    FROM x
                )
            FROM u
            """);
    }

    [Fact]
    public void Formats_with_in_derived_table_and_in_subquery()
    {
        AssertFormatted(
            "select d.a from (with x as (select a from t) select a from x) d where d.a in (with y as (select a from u) select a from y)",
            """
            SELECT
                d.a
            FROM (
                WITH
                    x AS (
                        SELECT
                            a
                        FROM t
                    )
                SELECT
                    a
                FROM x
            ) AS d
            WHERE d.a IN (
                WITH
                    y AS (
                        SELECT
                            a
                        FROM u
                    )
                SELECT
                    a
                FROM y
            )
            """);
    }

    [Fact]
    public void Formats_parenthesized_union_operand()
    {
        AssertFormatted(
            "select a from t union (select b from u)",
            """
            SELECT
                a
            FROM t
            UNION
            (
                SELECT
                    b
                FROM u
            )
            """);
    }

    [Fact]
    public void Formats_union_in_derived_table()
    {
        AssertFormatted(
            "select x.a from (select a from t union select a from u) x",
            """
            SELECT
                x.a
            FROM (
                SELECT
                    a
                FROM t
                UNION
                SELECT
                    a
                FROM u
            ) AS x
            """);
    }

    [Fact]
    public void Formats_union_order_by_limit()
    {
        AssertFormatted(
            "select a from t union select b from u order by a desc limit 10",
            """
            SELECT
                a
            FROM t
            UNION
            SELECT
                b
            FROM u
            ORDER BY a DESC
            LIMIT 10
            """);
    }

    [Fact]
    public void Formats_intersect_and_except()
    {
        AssertFormatted(
            "select a from t intersect select b from u except select c from v",
            """
            SELECT
                a
            FROM t
            INTERSECT
            SELECT
                b
            FROM u
            EXCEPT
            SELECT
                c
            FROM v
            """);
    }

    [Fact]
    public void Formats_union_all()
    {
        AssertFormatted(
            "select a from t union all select b from u",
            """
            SELECT
                a
            FROM t
            UNION ALL
            SELECT
                b
            FROM u
            """);
    }

    [Fact]
    public void Formats_quantified_subqueries()
    {
        AssertFormatted(
            "select a from t where n>all (select n from lim) and n=any (select n from ok) and n<some (select n from cap)",
            """
            SELECT
                a
            FROM t
            WHERE n > ALL (
                SELECT
                    n
                FROM lim
            )
                AND n = ANY (
                    SELECT
                        n
                    FROM ok
                )
                AND n < SOME (
                    SELECT
                        n
                    FROM cap
                )
            """);
    }

    [Fact]
    public void Formats_exists_and_not_exists()
    {
        AssertFormatted(
            "select a from t where exists (select 1 from u where u.id=t.id) and not exists (select 1 from v where v.id=t.id)",
            """
            SELECT
                a
            FROM t
            WHERE EXISTS (
                SELECT
                    1
                FROM u
                WHERE u.id = t.id
            )
                AND NOT EXISTS (
                    SELECT
                        1
                    FROM v
                    WHERE v.id = t.id
                )
            """);
    }

    [Fact]
    public void Formats_in_subquery()
    {
        AssertFormatted(
            "select a from t where id in (select id from u)",
            """
            SELECT
                a
            FROM t
            WHERE id IN (
                SELECT
                    id
                FROM u
            )
            """);
    }

    [Fact]
    public void Formats_not_in_list_and_subquery()
    {
        AssertFormatted(
            "select a from t where x not in (1,2) and id not in (select id from u)",
            """
            SELECT
                a
            FROM t
            WHERE x NOT IN (1, 2)
                AND id NOT IN (
                    SELECT
                        id
                    FROM u
                )
            """);
    }

    [Fact]
    public void Formats_scalar_subquery()
    {
        AssertFormatted(
            "select (select a from t) from u",
            """
            SELECT
                (
                    SELECT
                        a
                    FROM t
                )
            FROM u
            """);
    }

    [Fact]
    public void Rejects_derived_table_without_alias()
    {
        var error = Assert.Throws<SqlParseException>(() => Sql.Format("select a from (select a from t)"));
        Assert.Equal("Expected alias after derived table", error.Message);
    }

    [Fact]
    public void Formats_between_in_is_null_and_parens()
    {
        AssertFormatted(
            "select a from t where x between 1 and 2 and y in ('a','b') and (z is null or z>=3)",
            """
            SELECT
                a
            FROM t
            WHERE x BETWEEN 1 AND 2
                AND y IN ('a', 'b')
                AND (z IS NULL OR z >= 3)
            """);
    }



    [Fact]
    public void Formats_array()
    {
        AssertFormatted(
            "select array[1, 2] from t",
            """
            SELECT
                ARRAY[1, 2]
            FROM t
            """);
    }

    [Fact]
    public void Formats_empty_array()
    {
        AssertFormatted(
            "select array[] from t",
            """
            SELECT
                ARRAY[]
            FROM t
            """);
    }

    [Fact]
    public void Formats_array_with_colon_cast()
    {
        AssertFormatted(
            "select array[id::text] from t",
            """
            SELECT
                ARRAY[id::TEXT]
            FROM t
            """);
    }

    [Fact]
    public void Preserves_comment_inside_array()
    {
        AssertFormatted(
            "select array[ /* n */ 1] from t",
            """
            SELECT
                ARRAY[/* n */ 1]
            FROM t
            """);
    }

    [Fact]
    public void Formats_concat()
    {
        AssertFormatted(
            "select a || b from t",
            """
            SELECT
                a || b
            FROM t
            """);
    }

    [Fact]
    public void Formats_concat_chain()
    {
        AssertFormatted(
            "select p.first_name || ' ' || p.last_name from t",
            """
            SELECT
                p.first_name || ' ' || p.last_name
            FROM t
            """);
    }

    [Fact]
    public void Formats_cast()
    {
        AssertFormatted(
            "select cast(x as numeric) from t",
            """
            SELECT
                CAST(x AS NUMERIC)
            FROM t
            """);
    }

    [Fact]
    public void Formats_cast_in_where()
    {
        AssertFormatted(
            "select a from t where cast(x as int) = 1",
            """
            SELECT
                a
            FROM t
            WHERE CAST(x AS INT) = 1
            """);
    }

    [Fact]
    public void Preserves_comment_between_as_and_type()
    {
        AssertFormatted(
            "select cast(x as /* t */ numeric) from t",
            """
            SELECT
                CAST(x AS /* t */ NUMERIC)
            FROM t
            """);
    }

    [Fact]
    public void Formats_colon_cast()
    {
        AssertFormatted(
            "select x::numeric from t",
            """
            SELECT
                x::NUMERIC
            FROM t
            """);
    }

    [Fact]
    public void Formats_cast_with_precision_and_scale()
    {
        AssertFormatted(
            "select cast(x as numeric(10, 2)), y::numeric(10,2) from t",
            """
            SELECT
                CAST(x AS NUMERIC(10, 2)),
                y::NUMERIC(10, 2)
            FROM t
            """);
    }

    [Fact]
    public void Formats_like()
    {
        AssertFormatted(
            "select a from t where x like 'a%'",
            """
            SELECT
                a
            FROM t
            WHERE x LIKE 'a%'
            """);
    }

    [Fact]
    public void Formats_not_like()
    {
        AssertFormatted(
            "select a from t where x not like 'a%'",
            """
            SELECT
                a
            FROM t
            WHERE x NOT LIKE 'a%'
            """);
    }

    [Fact]
    public void Formats_like_with_and_or()
    {
        AssertFormatted(
            "select a from t where x like 'a%' or y not like '%z' and z like '_'",
            """
            SELECT
                a
            FROM t
            WHERE x LIKE 'a%' OR y NOT LIKE '%z' AND z LIKE '_'
            """);
    }

    [Fact]
    public void Preserves_comment_between_like_and_pattern()
    {
        AssertFormatted(
            "select a from t where x like /* pat */ 'a%'",
            """
            SELECT
                a
            FROM t
            WHERE x LIKE /* pat */ 'a%'
            """);
    }

    [Fact]
    public void Formats_select_without_from()
    {
        AssertFormatted(
            "select 1",
            """
            SELECT
                1
            """);
    }

    [Fact]
    public void Formats_select_distinct_without_from()
    {
        AssertFormatted(
            "select distinct a, 2",
            """
            SELECT DISTINCT
                a,
                2
            """);
    }

    [Fact]
    public void Formats_select_without_from_with_where()
    {
        AssertFormatted(
            "select 1 where x = 1",
            """
            SELECT
                1
            WHERE x = 1
            """);
    }

    [Fact]
    public void Formats_scalar_subquery_without_from()
    {
        AssertFormatted(
            "select (select 1)",
            """
            SELECT
                (
                    SELECT
                        1
                )
            """);
    }

    [Fact]
    public void Formats_select_distinct()
    {
        AssertFormatted(
            "select distinct a from t",
            """
            SELECT DISTINCT
                a
            FROM t
            """);
    }

    [Fact]
    public void Formats_select_distinct_columns_and_where()
    {
        AssertFormatted(
            "select distinct a, b from t where x=1",
            """
            SELECT DISTINCT
                a,
                b
            FROM t
            WHERE x = 1
            """);
    }

    [Fact]
    public void Formats_distinct_in_subquery_and_cte()
    {
        AssertFormatted(
            "with c as (select distinct a from t) select distinct b from (select distinct b from c) as s",
            """
            WITH
                c AS (
                    SELECT DISTINCT
                        a
                    FROM t
                )
            SELECT DISTINCT
                b
            FROM (
                SELECT DISTINCT
                    b
                FROM c
            ) AS s
            """);
    }

    [Fact]
    public void Preserves_comment_between_select_and_distinct()
    {
        AssertFormatted(
            "select /* keep */ distinct a from t",
            """
            SELECT /* keep */ DISTINCT
                a
            FROM t
            """);
    }

    [Fact]
    public void Colors_keywords_literals_and_comments()
    {
        var sql = """
            select a -- c
            from t where x=1 and y='z'
            """;
        var plain = Sql.Format(sql);
        var colored = Sql.Format(sql, color: true);

        Assert.DoesNotContain('\u001b', plain);
        Assert.Contains('\u001b', colored);
        Assert.Equal(plain, StripAnsi(colored));
    }

    private static void AssertFormatted(string sql, string expected) =>
        Assert.Equal(expected, Sql.Format(sql));

    private const int AnsiCsiPrefixLength = 2;

    private static string StripAnsi(string text)
    {
        var stripped = new System.Text.StringBuilder(text.Length);
        for (var i = 0; i < text.Length; i++)
        {
            if (text[i] == '\u001b' && i + 1 < text.Length && text[i + 1] == '[')
            {
                i += AnsiCsiPrefixLength;
                while (i < text.Length && text[i] is not 'm')
                {
                    i++;
                }

                continue;
            }

            stripped.Append(text[i]);
        }

        return stripped.ToString();
    }
}
