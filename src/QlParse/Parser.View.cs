namespace QlParse;

internal sealed partial class Parser
{
    internal CreateViewStatement ParseCreateView()
    {
        var createKeyword = Advance();
        var recursive = _current.Kind == SyntaxKind.RecursiveKeyword ? Advance() : (SyntaxToken?)null;
        var viewKeyword = ExpectIdent(Keyword.View);
        var name = ParseQualifiedName();
        SyntaxToken? ofKeyword = null;
        IReadOnlyList<SyntaxToken>? typeName = null;
        SyntaxToken? underKeyword = null;
        IReadOnlyList<SyntaxToken>? superview = null;
        SyntaxToken? columnsOpen = null;
        IReadOnlyList<SyntaxToken>? columns = null;
        SyntaxToken? columnsClose = null;
        IReadOnlyList<TableElement>? elements = null;
        if (_current.Kind == SyntaxKind.OfKeyword)
        {
            ofKeyword = Advance();
            typeName = ParseQualifiedName();
            if (IdentifierEquals(Keyword.Under))
            {
                underKeyword = Advance();
                superview = ParseQualifiedName();
            }

            if (_current.Kind == SyntaxKind.OpenParen)
            {
                ParseViewElements(out var elementOpen, out var elementList, out var elementClose);
                columnsOpen = elementOpen;
                elements = elementList;
                columnsClose = elementClose;
            }
        }
        else
        {
            if (IsAsColumnList())
            {
                columnsOpen = Advance();
                columns = ParseNameList();
                columnsClose = Expect(SyntaxKind.CloseParen);
            }
        }

        var asKeyword = Expect(SyntaxKind.AsKeyword);
        var query = ParseQuery();
        ParseCheckOption(out var withKeyword, out var levels, out var checkKeyword, out var optionKeyword);
        var end = optionKeyword?.Span ?? query.Span;
        return new CreateViewStatement
        {
            Span = SourceSpan.From(createKeyword, end),
            CreateKeyword = createKeyword,
            RecursiveKeyword = recursive,
            ViewKeyword = viewKeyword,
            Name = name,
            OfKeyword = ofKeyword,
            TypeName = typeName,
            UnderKeyword = underKeyword,
            Superview = superview,
            ColumnsOpen = columnsOpen,
            Columns = columns,
            ColumnsClose = columnsClose,
            Elements = elements,
            AsKeyword = asKeyword,
            Query = query,
            WithKeyword = withKeyword,
            Levels = levels,
            CheckKeyword = checkKeyword,
            OptionKeyword = optionKeyword,
        };
    }

    internal AlterViewStatement ParseAlterView()
    {
        var alterKeyword = Advance();
        var viewKeyword = ExpectIdent(Keyword.View);
        var name = ParseQualifiedName();
        AlterViewAction action;
        if (_current.Kind == SyntaxKind.AsKeyword)
        {
            var asKeyword = Advance();
            var query = ParseQuery();
            action = new ReplaceViewAction
            {
                Span = SourceSpan.From(asKeyword, query.Span),
                AsKeyword = asKeyword,
                Query = query,
            };
        }
        else
        {
            if (IdentifierEquals(Keyword.Alter))
            {
                var column = ParseAlterColumn();
                action = new AlterViewColumnAction { Span = column.Span, Column = column };
            }
            else
            {
                throw new SqlParseException($"Expected AS or ALTER, found {_current.Kind}", _current.Position);
            }
        }

        return new AlterViewStatement
        {
            Span = SourceSpan.From(alterKeyword, action.Span),
            AlterKeyword = alterKeyword,
            ViewKeyword = viewKeyword,
            Name = name,
            Action = action,
        };
    }

    internal DropViewStatement ParseDropView()
    {
        var dropKeyword = Advance();
        var viewKeyword = ExpectIdent(Keyword.View);
        var name = ParseQualifiedName();
        var behavior = ParseDropBehavior();
        return new DropViewStatement
        {
            Span = SourceSpan.From(dropKeyword, behavior),
            DropKeyword = dropKeyword,
            ViewKeyword = viewKeyword,
            Name = name,
            Behavior = behavior,
        };
    }

    internal CreateDomainStatement ParseCreateDomain()
    {
        var createKeyword = Advance();
        var domainKeyword = ExpectIdent(Keyword.Domain);
        var name = ParseQualifiedName();
        var asKeyword = _current.Kind == SyntaxKind.AsKeyword ? Advance() : (SyntaxToken?)null;
        var type = ParseDataType();
        ParseDomainTail(out var defaultClause, out var constraints, out var collateKeyword, out var collation);
        var end = type.Span;
        if (defaultClause is not null)
        {
            end = defaultClause.Span;
        }

        if (constraints.Count > 0)
        {
            end = constraints[^1].Span;
        }

        if (collation is not null)
        {
            end = collation[^1].Span;
        }

        return new CreateDomainStatement
        {
            Span = SourceSpan.From(createKeyword, end),
            CreateKeyword = createKeyword,
            DomainKeyword = domainKeyword,
            Name = name,
            AsKeyword = asKeyword,
            Type = type,
            Default = defaultClause,
            Constraints = constraints,
            CollateKeyword = collateKeyword,
            Collation = collation,
        };
    }

    internal AlterDomainStatement ParseAlterDomain()
    {
        var alterKeyword = Advance();
        var domainKeyword = ExpectIdent(Keyword.Domain);
        var name = ParseQualifiedName();
        var action = ParseAlterDomainAction();
        return new AlterDomainStatement
        {
            Span = SourceSpan.From(alterKeyword, action.Span),
            AlterKeyword = alterKeyword,
            DomainKeyword = domainKeyword,
            Name = name,
            Action = action,
        };
    }

    internal DropDomainStatement ParseDropDomain()
    {
        var dropKeyword = Advance();
        var domainKeyword = ExpectIdent(Keyword.Domain);
        var name = ParseQualifiedName();
        var behavior = ParseDropBehavior();
        return new DropDomainStatement
        {
            Span = SourceSpan.From(dropKeyword, behavior),
            DropKeyword = dropKeyword,
            DomainKeyword = domainKeyword,
            Name = name,
            Behavior = behavior,
        };
    }

    internal CreateTypeStatement ParseCreateType()
    {
        var createKeyword = Advance();
        var typeKeyword = ExpectIdent(Keyword.Type);
        var name = ParseQualifiedName();
        SyntaxToken? underKeyword = null;
        IReadOnlyList<SyntaxToken>? supertype = null;
        if (IdentifierEquals(Keyword.Under))
        {
            underKeyword = Advance();
            supertype = ParseQualifiedName();
        }

        SyntaxToken? asKeyword = null;
        DataType? representation = null;
        SyntaxToken? membersOpen = null;
        IReadOnlyList<AttributeDefinition>? attributes = null;
        SyntaxToken? membersClose = null;
        if (_current.Kind == SyntaxKind.AsKeyword)
        {
            asKeyword = Advance();
            var typeBody = ParseTypeRepresentation();
            representation = typeBody.Representation;
            membersOpen = typeBody.MembersOpen;
            attributes = typeBody.Attributes;
            membersClose = typeBody.MembersClose;
        }
        else
        {
            if (underKeyword is null)
            {
                throw new SqlParseException($"Expected AS or UNDER, found {_current.Kind}", _current.Position);
            }
        }

        var optionsEnd = ParseTypeOptions(
            out var notInstantiable,
            out var instantiable,
            out var notFinal,
            out var finalKeyword,
            out var reference,
            out var casts);
        IReadOnlyList<MethodSpecification> methods = [];
        if (IsMethodStart())
        {
            methods = ParseMethodList();
        }

        var end = ParseCreateTypeEnd(name, supertype, representation, membersClose, optionsEnd, methods);

        return new CreateTypeStatement
        {
            Span = SourceSpan.From(createKeyword, end),
            CreateKeyword = createKeyword,
            TypeKeyword = typeKeyword,
            Name = name,
            UnderKeyword = underKeyword,
            Supertype = supertype,
            AsKeyword = asKeyword,
            Representation = representation,
            MembersOpen = membersOpen,
            Attributes = attributes,
            MembersClose = membersClose,
            NotInstantiableKeyword = notInstantiable,
            InstantiableKeyword = instantiable,
            NotFinalKeyword = notFinal,
            FinalKeyword = finalKeyword,
            Reference = reference,
            Casts = casts,
            Methods = methods,
        };
    }

    private static SourceSpan ParseCreateTypeEnd(
        IReadOnlyList<SyntaxToken> name,
        IReadOnlyList<SyntaxToken>? supertype,
        DataType? representation,
        SyntaxToken? membersClose,
        SourceSpan? optionsEnd,
        IReadOnlyList<MethodSpecification> methods) =>
        (methods.Count > 0 ? methods[^1].Span : (SourceSpan?)null)
        ?? optionsEnd
        ?? membersClose?.Span
        ?? representation?.Span
        ?? supertype?[^1].Span
        ?? name[^1].Span;

    private readonly record struct TypeRepresentation(
        DataType? Representation,
        SyntaxToken? MembersOpen,
        IReadOnlyList<AttributeDefinition>? Attributes,
        SyntaxToken? MembersClose);

    private TypeRepresentation ParseTypeRepresentation()
    {
        if (_current.Kind != SyntaxKind.OpenParen)
        {
            return new TypeRepresentation(ParseDataType(), null, null, null);
        }

        ParseAttributes(out var open, out var members, out var close);
        return new TypeRepresentation(null, open, members, close);
    }

    internal DropTypeStatement ParseDropType()
    {
        var dropKeyword = Advance();
        var typeKeyword = ExpectIdent(Keyword.Type);
        var name = ParseQualifiedName();
        var behavior = ParseDropBehavior();
        return new DropTypeStatement
        {
            Span = SourceSpan.From(dropKeyword, behavior),
            DropKeyword = dropKeyword,
            TypeKeyword = typeKeyword,
            Name = name,
            Behavior = behavior,
        };
    }

    internal CreateOrderingStatement ParseCreateOrdering()
    {
        var createKeyword = Advance();
        var orderingKeyword = ExpectIdent(Keyword.Ordering);
        var forKeyword = Expect(SyntaxKind.ForKeyword);
        var name = ParseQualifiedName();
        SyntaxToken? equalsKeyword = null;
        SyntaxToken? orderKeyword = null;
        SyntaxToken form;
        if (IdentifierEquals(Keyword.Equals))
        {
            equalsKeyword = Advance();
            form = Expect(SyntaxKind.OnlyKeyword);
        }
        else
        {
            if (_current.Kind == SyntaxKind.OrderKeyword)
            {
                orderKeyword = Advance();
                form = Expect(SyntaxKind.FullKeyword);
            }
            else
            {
                throw new SqlParseException($"Expected EQUALS ONLY or ORDER FULL, found {_current.Kind}", _current.Position);
            }
        }

        var byKeyword = Expect(SyntaxKind.ByKeyword);
        SyntaxToken category;
        SyntaxToken? withKeyword = null;
        RoutineDesignator? routine = null;
        SourceSpan end;
        if (IdentifierEquals(Keyword.Relative) || IdentifierEquals(Keyword.Map))
        {
            category = Advance();
            withKeyword = Expect(SyntaxKind.WithKeyword);
            routine = ParseRoutineDesignator();
            end = routine.Span;
        }
        else
        {
            if (IdentifierEquals(Keyword.State))
            {
                category = Advance();
                end = category.Span;
            }
            else
            {
                throw new SqlParseException($"Expected RELATIVE, MAP, or STATE, found {_current.Kind}", _current.Position);
            }
        }

        return new CreateOrderingStatement
        {
            Span = SourceSpan.From(createKeyword, end),
            CreateKeyword = createKeyword,
            OrderingKeyword = orderingKeyword,
            ForKeyword = forKeyword,
            Name = name,
            EqualsKeyword = equalsKeyword,
            OrderKeyword = orderKeyword,
            Form = form,
            ByKeyword = byKeyword,
            Category = category,
            WithKeyword = withKeyword,
            Routine = routine,
        };
    }

    internal CreateCastStatement ParseCreateCast()
    {
        var createKeyword = Advance();
        var castKeyword = Expect(SyntaxKind.CastKeyword);
        var openParen = Expect(SyntaxKind.OpenParen);
        var source = ParseDataType();
        var asKeyword = Expect(SyntaxKind.AsKeyword);
        var target = ParseDataType();
        var closeParen = Expect(SyntaxKind.CloseParen);
        var withKeyword = Expect(SyntaxKind.WithKeyword);
        var routine = ParseRoutineDesignator();
        SyntaxToken? assignmentAs = null;
        SyntaxToken? assignment = null;
        var end = routine.Span;
        if (_current.Kind == SyntaxKind.AsKeyword)
        {
            assignmentAs = Advance();
            var assignmentToken = ExpectIdent(Keyword.Assignment);
            assignment = assignmentToken;
            end = assignmentToken.Span;
        }

        return new CreateCastStatement
        {
            Span = SourceSpan.From(createKeyword, end),
            CreateKeyword = createKeyword,
            CastKeyword = castKeyword,
            OpenParen = openParen,
            Source = source,
            AsKeyword = asKeyword,
            Target = target,
            CloseParen = closeParen,
            WithKeyword = withKeyword,
            Routine = routine,
            AssignmentAsKeyword = assignmentAs,
            AssignmentKeyword = assignment,
        };
    }

    internal CreateTransformStatement ParseCreateTransform()
    {
        var createKeyword = Advance();
        var transformKeyword = Advance();
        var forKeyword = Expect(SyntaxKind.ForKeyword);
        var name = ParseQualifiedName();
        var groups = new List<TransformGroup> { ParseTransformGroup() };
        while (_current.Kind == SyntaxKind.Identifier)
        {
            groups.Add(ParseTransformGroup());
        }

        return new CreateTransformStatement
        {
            Span = SourceSpan.From(createKeyword, groups[^1].Span),
            CreateKeyword = createKeyword,
            TransformKeyword = transformKeyword,
            ForKeyword = forKeyword,
            Name = name,
            Groups = groups,
        };
    }

    private void ParseCheckOption(
        out SyntaxToken? withKeyword,
        out SyntaxToken? levels,
        out SyntaxToken? checkKeyword,
        out SyntaxToken? optionKeyword)
    {
        withKeyword = null;
        levels = null;
        checkKeyword = null;
        optionKeyword = null;
        if (_current.Kind != SyntaxKind.WithKeyword)
        {
            return;
        }

        withKeyword = Advance();
        if (IdentifierEquals(Keyword.Cascaded) || IdentifierEquals(Keyword.Local))
        {
            levels = Advance();
        }

        checkKeyword = ExpectIdent(Keyword.Check);
        optionKeyword = ExpectIdent(Keyword.Option);
    }

    private void ParseViewElements(
        out SyntaxToken openParen,
        out IReadOnlyList<TableElement> elements,
        out SyntaxToken closeParen)
    {
        openParen = Expect(SyntaxKind.OpenParen);
        var items = new List<TableElement> { ParseViewElement() };
        while (_current.Kind == SyntaxKind.Comma)
        {
            Advance();
            items.Add(ParseViewElement());
        }

        elements = items;
        closeParen = Expect(SyntaxKind.CloseParen);
    }

    private TableElement ParseViewElement()
    {
        if (IsRefIs())
        {
            return ParseRefIs();
        }

        if (IsWithOptions())
        {
            return ParseColumn(withOptions: true);
        }

        throw new SqlParseException($"Expected view element, found {_current.Kind}", _current.Position);
    }

    private void ParseDomainTail(
        out DefaultClause? defaultClause,
        out IReadOnlyList<DomainConstraint> constraints,
        out SyntaxToken? collateKeyword,
        out IReadOnlyList<SyntaxToken>? collation)
    {
        defaultClause = null;
        collateKeyword = null;
        collation = null;
        var items = new List<DomainConstraint>();
        while (true)
        {
            if (IdentifierEquals(Keyword.Default))
            {
                if (defaultClause is not null)
                {
                    throw new SqlParseException($"Unexpected {_current.Kind}", _current.Position);
                }

                defaultClause = ParseDefaultClause();
                continue;
            }

            if (_current.Kind == SyntaxKind.CollateKeyword)
            {
                if (collateKeyword is not null)
                {
                    throw new SqlParseException($"Unexpected {_current.Kind}", _current.Position);
                }

                collateKeyword = Advance();
                collation = ParseQualifiedName();
                continue;
            }

            if (!IdentifierEquals(Keyword.Constraint) && !IdentifierEquals(Keyword.Check))
            {
                break;
            }

            items.Add(ParseDomainConstraint());
        }

        constraints = items;
    }

    private DomainConstraint ParseDomainConstraint()
    {
        var start = _current;
        SyntaxToken? constraintKeyword = null;
        SyntaxToken? constraintName = null;
        if (IdentifierEquals(Keyword.Constraint))
        {
            constraintKeyword = Advance();
            constraintName = Expect(SyntaxKind.Identifier);
        }

        var checkKeyword = ExpectIdent(Keyword.Check);
        var openParen = Expect(SyntaxKind.OpenParen);
        var check = ParseExpression();
        var closeParen = Expect(SyntaxKind.CloseParen);
        ParseConstraintCharacteristics(out var notKeyword, out var deferrable, out var initially, out var initiallyWhen);
        var end = initiallyWhen?.Span ?? deferrable?.Span ?? closeParen.Span;
        return new DomainConstraint
        {
            Span = SourceSpan.From(start, end),
            ConstraintKeyword = constraintKeyword,
            ConstraintName = constraintName,
            CheckKeyword = checkKeyword,
            OpenParen = openParen,
            Check = check,
            CloseParen = closeParen,
            DeferrableNotKeyword = notKeyword,
            DeferrableKeyword = deferrable,
            InitiallyKeyword = initially,
            InitiallyWhen = initiallyWhen,
        };
    }

    private AlterDomainAction ParseAlterDomainAction()
    {
        if (_current.Kind == SyntaxKind.SetKeyword)
        {
            var setKeyword = Advance();
            var defaultClause = ParseDefaultClause();
            return new SetDomainDefaultAction
            {
                Span = SourceSpan.From(setKeyword, defaultClause.Span),
                SetKeyword = setKeyword,
                Default = defaultClause,
            };
        }

        if (IdentifierEquals(Keyword.Add))
        {
            var addKeyword = Advance();
            var constraint = ParseDomainConstraint();
            return new AddDomainConstraintAction
            {
                Span = SourceSpan.From(addKeyword, constraint.Span),
                AddKeyword = addKeyword,
                Constraint = constraint,
            };
        }

        if (!IdentifierEquals(Keyword.Drop))
        {
            throw new SqlParseException($"Expected SET, ADD, or DROP, found {_current.Kind}", _current.Position);
        }

        var dropKeyword = Advance();
        if (IdentifierEquals(Keyword.Default))
        {
            var defaultKeyword = Advance();
            return new DropDomainDefaultAction
            {
                Span = SourceSpan.From(dropKeyword, defaultKeyword),
                DropKeyword = dropKeyword,
                DefaultKeyword = defaultKeyword,
            };
        }

        var constraintKeyword = ExpectIdent(Keyword.Constraint);
        var name = Expect(SyntaxKind.Identifier);
        var behavior = ParseDropBehavior();
        return new DropDomainConstraintAction
        {
            Span = SourceSpan.From(dropKeyword, behavior),
            DropKeyword = dropKeyword,
            ConstraintKeyword = constraintKeyword,
            Name = name,
            Behavior = behavior,
        };
    }

}
