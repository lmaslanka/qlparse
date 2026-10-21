namespace QlParse.Tests;

// SQL Server full-text DDL (stream G): catalogs, indexes, stoplists and search property lists.
public sealed partial class TsqlAdminTests
{
    private const string OffWord = "off";
    private const string DboOwner = "dbo";
    private const string FulltextIndexSql = "create fulltext index on Production.Document "
        + "(Title language 1033, Document type column FileExtension language 'English' statistical_semantics) "
        + "key index PK_Document on (ftcat, filegroup ftfg) with (change_tracking = auto, stoplist = system)";

    [Fact]
    public void Parses_create_fulltext_catalog()
    {
        const string sql = "create fulltext catalog ftcat on filegroup ftfg in path 'C:\\ft' "
            + "with accent_sensitivity = off as default authorization dbo";
        var catalog = Whole<TsqlCreateFulltextCatalogStatement>(sql);
        Assert.Equal("ftfg", Text(sql, catalog.FileGroup));
        Assert.Equal("'C:\\ft'", Text(sql, catalog.Path));
        Assert.Equal(OffWord, Value(sql, catalog.Options!.Options[0]));
        Assert.NotNull(catalog.DefaultKeyword);
        Assert.Equal(DboOwner, Text(sql, catalog.Owner));
        Assert.Null(Whole<TsqlCreateFulltextCatalogStatement>("create fulltext catalog c").OnKeyword);
        SqlAssert.ParseFails("create fulltext catalog c");
    }

    [Theory]
    [InlineData("alter fulltext catalog c rebuild with accent_sensitivity = on", "rebuild", true)]
    [InlineData("alter fulltext catalog c reorganize", "reorganize", false)]
    [InlineData("alter fulltext catalog c as default", "as default", false)]
    public void Parses_alter_fulltext_catalog(string sql, string action, bool withOptions)
    {
        var catalog = Whole<TsqlAlterFulltextCatalogStatement>(sql);
        Assert.Equal(action, Words(sql, catalog.Action));
        Assert.Equal(withOptions, catalog.Options is not null);
    }

    [Fact]
    public void Parses_create_fulltext_index()
    {
        var index = Whole<TsqlCreateFulltextIndexStatement>(FulltextIndexSql);
        Assert.Equal(Two, index.Table.Count);
        Assert.Equal(Two, index.Columns.Count);
        Assert.Equal("1033", Text(FulltextIndexSql, index.Columns[0].Language));
        var document = index.Columns[1];
        Assert.Equal("FileExtension", Text(FulltextIndexSql, document.TypeColumn));
        Assert.NotNull(document.StatisticalSemanticsKeyword);
        Assert.Equal("PK_Document", Text(FulltextIndexSql, index.KeyIndex));
        Assert.Equal(Two, index.Storage!.Options.Count);
        Assert.Equal("filegroup", Name(FulltextIndexSql, index.Storage.Options[1]));
        Assert.Equal(Two, index.Options!.Options.Count);
        SqlAssert.ParseFails(FulltextIndexSql);
    }

    [Fact]
    public void Parses_fulltext_index_bare_with_options()
    {
        const string sql = "create fulltext index on t (c) key index ix on ftcat "
            + "with change_tracking off, no population, stoplist = OurStoplist, search property list = plist";
        var index = Whole<TsqlCreateFulltextIndexStatement>(sql);
        Assert.Equal("ftcat", Name(sql, Assert.Single(index.Storage!.Options)));
        var options = index.Options!.Options;
        Assert.Equal(Four, options.Count);
        Assert.Equal(OffWord, Value(sql, options[0]));
        Assert.Equal("no population", Name(sql, options[1]));
        Assert.Empty(options[1].Value);
        Assert.Equal("search property list", Name(sql, options[Three]));
        Assert.Equal("plist", Value(sql, options[Three]));

        const string minimal = "create fulltext index on t key index ix";
        Assert.Empty(Whole<TsqlCreateFulltextIndexStatement>(minimal).Columns);
    }

    [Fact]
    public void Parses_fulltext_stoplists()
    {
        const string sql = "create fulltext stoplist sl from db.OtherList authorization dbo";
        var stoplist = Whole<TsqlCreateFulltextStoplistStatement>(sql);
        Assert.Equal(Two, stoplist.Source.Count);
        Assert.Equal(DboOwner, Text(sql, stoplist.Owner));
        var system = Whole<TsqlCreateFulltextStoplistStatement>("create fulltext stoplist sl from system stoplist");
        Assert.NotNull(system.SystemStoplistKeyword);
        Assert.Empty(system.Source);
    }

    [Theory]
    [InlineData("alter fulltext stoplist sl add 'en' language 'Spanish'", "add", false)]
    [InlineData("alter fulltext stoplist sl drop 'en' language 1033", "drop", false)]
    [InlineData("alter fulltext stoplist sl drop all language 0x0C", "drop", true)]
    [InlineData("alter fulltext stoplist sl drop all", "drop", true)]
    public void Parses_alter_fulltext_stoplist(string sql, string action, bool all)
    {
        var stoplist = Whole<TsqlAlterFulltextStoplistStatement>(sql);
        Assert.Equal(action, Text(sql, stoplist.Action));
        Assert.Equal(all, stoplist.AllKeyword is not null);
    }

    [Fact]
    public void Parses_search_property_lists()
    {
        const string sql = "create search property list pl from db.other authorization dbo";
        var list = Whole<TsqlCreateSearchPropertyListStatement>(sql);
        Assert.Equal(Two, list.Source.Count);

        const string add = "alter search property list pl add 'Title' with "
            + "(property_set_guid = 'F29F85E0-4FF9-1068-AB91-08002B27B3D9', property_int_id = 2, property_description = 'Title')";
        var altered = Whole<TsqlAlterSearchPropertyListStatement>(add);
        Assert.Equal(Three, altered.Options!.Options.Count);
        Assert.Null(Whole<TsqlAlterSearchPropertyListStatement>("alter search property list pl drop 'Title'").Options);
        SqlAssert.ParseFails("create search property list pl");
    }

    [Fact]
    public void Visitor_walks_fulltext_statements()
    {
        var counter = new OptionCounter();
        counter.Visit(AnyTsql(FulltextIndexSql));
        Assert.Equal(Four, counter.Options);
        foreach (var sql in FulltextSamples)
        {
            new SqlVisitor().Visit(AnyTsql(sql));
        }
    }

    private static readonly string[] FulltextSamples =
    [
        "create fulltext catalog c with accent_sensitivity = on",
        "alter fulltext catalog c rebuild",
        "create fulltext stoplist sl",
        "alter fulltext stoplist sl drop all",
        "create search property list pl",
        "alter search property list pl drop 'p'",
    ];
}
