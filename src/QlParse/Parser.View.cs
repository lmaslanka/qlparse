namespace QlParse;

internal sealed partial class Parser
{
    private CreateViewStatement ParseCreateView()
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
        else if (IsAsColumnList())
        {
            columnsOpen = Advance();
            columns = ParseNameList();
            columnsClose = Expect(SyntaxKind.CloseParen);
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

    private AlterViewStatement ParseAlterView()
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
        else if (IdentifierEquals(Keyword.Alter))
        {
            var column = ParseAlterColumn();
            action = new AlterViewColumnAction { Span = column.Span, Column = column };
        }
        else
        {
            throw new SqlParseException($"Expected AS or ALTER, found {_current.Kind}", _current.Position);
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

    private DropViewStatement ParseDropView()
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

    private CreateDomainStatement ParseCreateDomain()
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

    private AlterDomainStatement ParseAlterDomain()
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

    private DropDomainStatement ParseDropDomain()
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

    private CreateTypeStatement ParseCreateType()
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
            if (_current.Kind == SyntaxKind.OpenParen)
            {
                ParseAttributes(out var open, out var members, out var close);
                membersOpen = open;
                attributes = members;
                membersClose = close;
            }
            else
            {
                representation = ParseDataType();
            }
        }
        else if (underKeyword is null)
        {
            throw new SqlParseException($"Expected AS or UNDER, found {_current.Kind}", _current.Position);
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

        var end = name[^1].Span;
        if (supertype is not null)
        {
            end = supertype[^1].Span;
        }

        if (representation is not null)
        {
            end = representation.Span;
        }

        if (membersClose is SyntaxToken membersCloseToken)
        {
            end = membersCloseToken.Span;
        }

        if (optionsEnd is SourceSpan optionSpan)
        {
            end = optionSpan;
        }

        if (methods.Count > 0)
        {
            end = methods[^1].Span;
        }

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

    private DropTypeStatement ParseDropType()
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

    private CreateOrderingStatement ParseCreateOrdering()
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
        else if (_current.Kind == SyntaxKind.OrderKeyword)
        {
            orderKeyword = Advance();
            form = Expect(SyntaxKind.FullKeyword);
        }
        else
        {
            throw new SqlParseException($"Expected EQUALS ONLY or ORDER FULL, found {_current.Kind}", _current.Position);
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
        else if (IdentifierEquals(Keyword.State))
        {
            category = Advance();
            end = category.Span;
        }
        else
        {
            throw new SqlParseException($"Expected RELATIVE, MAP, or STATE, found {_current.Kind}", _current.Position);
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

    private CreateCastStatement ParseCreateCast()
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

    private CreateTransformStatement ParseCreateTransform()
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

    private void ParseAttributes(
        out SyntaxToken openParen,
        out IReadOnlyList<AttributeDefinition> attributes,
        out SyntaxToken closeParen)
    {
        openParen = Expect(SyntaxKind.OpenParen);
        var items = new List<AttributeDefinition> { ParseAttribute() };
        while (_current.Kind == SyntaxKind.Comma)
        {
            Advance();
            items.Add(ParseAttribute());
        }

        attributes = items;
        closeParen = Expect(SyntaxKind.CloseParen);
    }

    private AttributeDefinition ParseAttribute()
    {
        var name = Expect(SyntaxKind.Identifier);
        var type = ParseDataType();
        SyntaxToken? referencesKeyword = null;
        SyntaxToken? areKeyword = null;
        SyntaxToken? notKeyword = null;
        SyntaxToken? checkedKeyword = null;
        ReferentialAction? onDelete = null;
        DefaultClause? defaultClause = null;
        SyntaxToken? collateKeyword = null;
        IReadOnlyList<SyntaxToken>? collation = null;
        var end = type.Span;
        while (true)
        {
            if (IdentifierEquals(Keyword.References))
            {
                if (referencesKeyword is not null)
                {
                    throw new SqlParseException($"Unexpected {_current.Kind}", _current.Position);
                }

                referencesKeyword = Advance();
                areKeyword = ExpectIdent(Keyword.Are);
                if (_current.Kind == SyntaxKind.NotKeyword)
                {
                    notKeyword = Advance();
                }

                var checkedToken = ExpectIdent(Keyword.Checked);
                checkedKeyword = checkedToken;
                end = checkedToken.Span;
                if (_current.Kind == SyntaxKind.OnKeyword)
                {
                    onDelete = ParseOnDelete();
                    end = onDelete.Span;
                }

                continue;
            }

            if (IdentifierEquals(Keyword.Default))
            {
                if (defaultClause is not null)
                {
                    throw new SqlParseException($"Unexpected {_current.Kind}", _current.Position);
                }

                defaultClause = ParseDefaultClause();
                end = defaultClause.Span;
                continue;
            }

            if (_current.Kind != SyntaxKind.CollateKeyword)
            {
                break;
            }

            if (collateKeyword is not null)
            {
                throw new SqlParseException($"Unexpected {_current.Kind}", _current.Position);
            }

            collateKeyword = Advance();
            collation = ParseQualifiedName();
            end = collation[^1].Span;
        }

        return new AttributeDefinition
        {
            Span = SourceSpan.From(name, end),
            Name = name,
            Type = type,
            ReferencesKeyword = referencesKeyword,
            AreKeyword = areKeyword,
            NotKeyword = notKeyword,
            CheckedKeyword = checkedKeyword,
            OnDelete = onDelete,
            Default = defaultClause,
            CollateKeyword = collateKeyword,
            Collation = collation,
        };
    }

    private ReferentialAction ParseOnDelete()
    {
        var onKeyword = Expect(SyntaxKind.OnKeyword);
        var deleteKeyword = ExpectIdent(Keyword.Delete);
        SyntaxToken? noKeyword = null;
        SyntaxToken? setKeyword = null;
        SyntaxToken action;
        if (_current.Kind == SyntaxKind.SetKeyword)
        {
            setKeyword = Advance();
            if (_current.Kind != SyntaxKind.NullKeyword && !IdentifierEquals(Keyword.Default))
            {
                throw new SqlParseException($"Expected NULL or DEFAULT, found {_current.Kind}", _current.Position);
            }

            action = Advance();
        }
        else if (IdentifierEquals(Keyword.No))
        {
            noKeyword = Advance();
            action = ExpectIdent(Keyword.Action);
        }
        else if (IdentifierEquals(Keyword.Cascade) || IdentifierEquals(Keyword.Restrict))
        {
            action = Advance();
        }
        else
        {
            throw new SqlParseException($"Expected referential action, found {_current.Kind}", _current.Position);
        }

        return new ReferentialAction
        {
            Span = SourceSpan.From(onKeyword, action),
            OnKeyword = onKeyword,
            Event = deleteKeyword,
            NoKeyword = noKeyword,
            SetKeyword = setKeyword,
            Action = action,
        };
    }

    private SourceSpan? ParseTypeOptions(
        out SyntaxToken? notInstantiable,
        out SyntaxToken? instantiable,
        out SyntaxToken? notFinal,
        out SyntaxToken? finalKeyword,
        out ReferenceGeneration? reference,
        out IReadOnlyList<TypeCastOption> casts)
    {
        notInstantiable = null;
        instantiable = null;
        notFinal = null;
        finalKeyword = null;
        reference = null;
        var castItems = new List<TypeCastOption>();
        SourceSpan? end = null;
        while (IsTypeOptionStart())
        {
            if (IsInstantiableOption())
            {
                if (instantiable is not null)
                {
                    throw new SqlParseException($"Unexpected {_current.Kind}", _current.Position);
                }

                if (_current.Kind == SyntaxKind.NotKeyword)
                {
                    notInstantiable = Advance();
                }

                var instantiableToken = ExpectIdent(Keyword.Instantiable);
                instantiable = instantiableToken;
                end = instantiableToken.Span;
                continue;
            }

            if (IsFinalOption())
            {
                if (finalKeyword is not null)
                {
                    throw new SqlParseException($"Unexpected {_current.Kind}", _current.Position);
                }

                if (_current.Kind == SyntaxKind.NotKeyword)
                {
                    notFinal = Advance();
                }

                var finalToken = ExpectIdent(Keyword.Final);
                finalKeyword = finalToken;
                end = finalToken.Span;
                continue;
            }

            if (IdentifierEquals(Keyword.Ref))
            {
                if (reference is not null)
                {
                    throw new SqlParseException($"Unexpected {_current.Kind}", _current.Position);
                }

                reference = ParseReferenceGeneration();
                end = reference.Span;
                continue;
            }

            var cast = ParseTypeCastOption();
            castItems.Add(cast);
            end = cast.Span;
        }

        casts = castItems;
        return end;
    }

    private ReferenceGeneration ParseReferenceGeneration()
    {
        var refKeyword = Advance();
        if (_current.Kind == SyntaxKind.IsKeyword)
        {
            var isKeyword = Advance();
            var system = ExpectIdent(Keyword.System);
            var generated = ExpectIdent(Keyword.Generated);
            return new ReferenceGeneration
            {
                Span = SourceSpan.From(refKeyword, generated),
                RefKeyword = refKeyword,
                IsKeyword = isKeyword,
                SystemKeyword = system,
                GeneratedKeyword = generated,
            };
        }

        if (_current.Kind == SyntaxKind.UsingKeyword)
        {
            var usingKeyword = Advance();
            var type = ParseDataType();
            return new ReferenceGeneration
            {
                Span = SourceSpan.From(refKeyword, type.Span),
                RefKeyword = refKeyword,
                UsingKeyword = usingKeyword,
                PredefinedType = type,
            };
        }

        if (_current.Kind != SyntaxKind.FromKeyword)
        {
            throw new SqlParseException($"Expected IS, USING, or FROM, found {_current.Kind}", _current.Position);
        }

        var fromKeyword = Advance();
        ParseRequiredNameList(out var openParen, out var attributes, out var closeParen);
        return new ReferenceGeneration
        {
            Span = SourceSpan.From(refKeyword, closeParen),
            RefKeyword = refKeyword,
            FromKeyword = fromKeyword,
            OpenParen = openParen,
            Attributes = attributes,
            CloseParen = closeParen,
        };
    }

    private TypeCastOption ParseTypeCastOption()
    {
        var castKeyword = Expect(SyntaxKind.CastKeyword);
        var openParen = Expect(SyntaxKind.OpenParen);
        if (!IdentifierEquals(Keyword.Source) && !IdentifierEquals(Keyword.Ref))
        {
            throw new SqlParseException($"Expected SOURCE or REF, found {_current.Kind}", _current.Position);
        }

        var source = Advance();
        var asKeyword = Expect(SyntaxKind.AsKeyword);
        if (!IdentifierEquals(Keyword.Source) && !IdentifierEquals(Keyword.Ref))
        {
            throw new SqlParseException($"Expected SOURCE or REF, found {_current.Kind}", _current.Position);
        }

        var target = Advance();
        if (TokenEquals(source, Keyword.Ref) == TokenEquals(target, Keyword.Ref))
        {
            throw new SqlParseException($"Expected SOURCE AS REF or REF AS SOURCE, found {_current.Kind}", _current.Position);
        }

        var closeParen = Expect(SyntaxKind.CloseParen);
        var withKeyword = Expect(SyntaxKind.WithKeyword);
        var function = Expect(SyntaxKind.Identifier);
        return new TypeCastOption
        {
            Span = SourceSpan.From(castKeyword, function),
            CastKeyword = castKeyword,
            OpenParen = openParen,
            Source = source,
            AsKeyword = asKeyword,
            Target = target,
            CloseParen = closeParen,
            WithKeyword = withKeyword,
            Function = function,
        };
    }

    private List<MethodSpecification> ParseMethodList()
    {
        var methods = new List<MethodSpecification> { ParseMethod() };
        while (_current.Kind == SyntaxKind.Comma)
        {
            Advance();
            methods.Add(ParseMethod());
        }

        return methods;
    }

    private MethodSpecification ParseMethod()
    {
        var start = _current;
        SyntaxToken? overriding = null;
        if (IdentifierEquals(Keyword.Overriding))
        {
            overriding = Advance();
        }

        SyntaxToken? kind = null;
        if (IdentifierEquals(Keyword.Instance) || IdentifierEquals(Keyword.Static) || IdentifierEquals(Keyword.Constructor))
        {
            kind = Advance();
        }

        var methodKeyword = ExpectIdent(Keyword.Method);
        var name = Expect(SyntaxKind.Identifier);
        var openParen = Expect(SyntaxKind.OpenParen);
        var parameters = new List<RoutineParameter>();
        if (_current.Kind != SyntaxKind.CloseParen)
        {
            parameters.Add(ParseRoutineParameter());
            while (_current.Kind == SyntaxKind.Comma)
            {
                Advance();
                parameters.Add(ParseRoutineParameter());
            }
        }

        var closeParen = Expect(SyntaxKind.CloseParen);
        var returnsKeyword = ExpectIdent(Keyword.Returns);
        var returnsType = ParseDataType();
        SyntaxToken? returnsAs = null;
        SyntaxToken? returnsLocator = null;
        if (_current.Kind == SyntaxKind.AsKeyword && NextEquals(Keyword.Locator))
        {
            returnsAs = Advance();
            returnsLocator = Advance();
        }

        SyntaxToken? specific = null;
        IReadOnlyList<SyntaxToken>? specificName = null;
        SyntaxToken? selfResult = null;
        SyntaxToken? selfResultAs = null;
        SyntaxToken? result = null;
        SyntaxToken? selfLocator = null;
        SyntaxToken? selfLocatorAs = null;
        SyntaxToken? locator = null;
        var characteristics = new List<RoutineCharacteristic>();
        var end = returnsLocator?.Span ?? returnsType.Span;
        while (true)
        {
            if (overriding is null && specific is null && IdentifierEquals(Keyword.Specific))
            {
                specific = Advance();
                specificName = ParseQualifiedName();
                end = specificName[^1].Span;
                continue;
            }

            if (IdentifierEquals(Keyword.Self))
            {
                var selfToken = Advance();
                var asToken = Expect(SyntaxKind.AsKeyword);
                if (IdentifierEquals(Keyword.Result))
                {
                    if (result is not null)
                    {
                        throw new SqlParseException($"Unexpected {_current.Kind}", _current.Position);
                    }

                    selfResult = selfToken;
                    selfResultAs = asToken;
                    var resultToken = Advance();
                    result = resultToken;
                    end = resultToken.Span;
                    continue;
                }

                if (!IdentifierEquals(Keyword.Locator))
                {
                    throw new SqlParseException($"Expected RESULT or LOCATOR, found {_current.Kind}", _current.Position);
                }

                if (locator is not null)
                {
                    throw new SqlParseException($"Unexpected {_current.Kind}", _current.Position);
                }

                selfLocator = selfToken;
                selfLocatorAs = asToken;
                var locatorToken = Advance();
                locator = locatorToken;
                end = locatorToken.Span;
                continue;
            }

            if (!IsCharacteristicStart())
            {
                break;
            }

            var characteristic = ParseCharacteristic();
            characteristics.Add(characteristic);
            end = characteristic.Span;
        }

        return new MethodSpecification
        {
            Span = SourceSpan.From(start, end),
            OverridingKeyword = overriding,
            Kind = kind,
            MethodKeyword = methodKeyword,
            Name = name,
            OpenParen = openParen,
            Parameters = parameters,
            CloseParen = closeParen,
            ReturnsKeyword = returnsKeyword,
            ReturnsType = returnsType,
            ReturnsAsKeyword = returnsAs,
            ReturnsLocator = returnsLocator,
            SpecificKeyword = specific,
            SpecificName = specificName,
            SelfResultKeyword = selfResult,
            SelfResultAsKeyword = selfResultAs,
            ResultKeyword = result,
            SelfLocatorKeyword = selfLocator,
            SelfLocatorAsKeyword = selfLocatorAs,
            LocatorKeyword = locator,
            Characteristics = characteristics,
        };
    }

    private RoutineParameter ParseRoutineParameter()
    {
        var start = _current;
        SyntaxToken? mode = null;
        if (_current.Kind == SyntaxKind.InKeyword || IdentifierEquals(Keyword.Out) || IdentifierEquals(Keyword.Inout))
        {
            mode = Advance();
        }

        SyntaxToken? name = null;
        if (IsParameterName())
        {
            name = Advance();
        }

        var type = ParseDataType();
        SyntaxToken? asKeyword = null;
        SyntaxToken? locator = null;
        if (_current.Kind == SyntaxKind.AsKeyword && NextEquals(Keyword.Locator))
        {
            asKeyword = Advance();
            locator = Advance();
        }

        SyntaxToken? result = null;
        if (IdentifierEquals(Keyword.Result))
        {
            result = Advance();
        }

        var end = result?.Span ?? locator?.Span ?? type.Span;
        return new RoutineParameter
        {
            Span = SourceSpan.From(start, end),
            Mode = mode,
            Name = name,
            Type = type,
            AsKeyword = asKeyword,
            LocatorKeyword = locator,
            ResultKeyword = result,
        };
    }

    private RoutineCharacteristic ParseCharacteristic()
    {
        if (_current.Kind == SyntaxKind.NotKeyword)
        {
            var notKeyword = Advance();
            var name = ExpectIdent(Keyword.Deterministic);
            return new RoutineCharacteristic
            {
                Span = SourceSpan.From(notKeyword, name),
                NotKeyword = notKeyword,
                Name = name,
            };
        }

        if (IdentifierEquals(Keyword.Deterministic))
        {
            var name = Advance();
            return new RoutineCharacteristic { Span = name.Span, Name = name };
        }

        if (IdentifierEquals(Keyword.Language))
        {
            var name = Advance();
            var language = Expect(SyntaxKind.Identifier);
            return new RoutineCharacteristic
            {
                Span = SourceSpan.From(name, language),
                Name = name,
                Tail = [language],
            };
        }

        if (IdentifierEquals(Keyword.Parameter))
        {
            var name = Advance();
            var style = ExpectIdent(Keyword.Style);
            if (!IdentifierEquals(Keyword.Sql) && !IdentifierEquals(Keyword.General))
            {
                throw new SqlParseException($"Expected SQL or GENERAL, found {_current.Kind}", _current.Position);
            }

            var kind = Advance();
            return new RoutineCharacteristic
            {
                Span = SourceSpan.From(name, kind),
                Name = name,
                Tail = [style, kind],
            };
        }

        if (IdentifierEquals(Keyword.No) || IdentifierEquals(Keyword.Contains))
        {
            var name = Advance();
            var sql = ExpectIdent(Keyword.Sql);
            return new RoutineCharacteristic
            {
                Span = SourceSpan.From(name, sql),
                Name = name,
                Tail = [sql],
            };
        }

        if (IdentifierEquals(Keyword.Reads) || IdentifierEquals(Keyword.Modifies))
        {
            var name = Advance();
            var sql = ExpectIdent(Keyword.Sql);
            var data = ExpectIdent(Keyword.Data);
            return new RoutineCharacteristic
            {
                Span = SourceSpan.From(name, data),
                Name = name,
                Tail = [sql, data],
            };
        }

        if (IdentifierEquals(Keyword.Returns))
        {
            var name = Advance();
            var nullKeyword = Expect(SyntaxKind.NullKeyword);
            var onKeyword = Expect(SyntaxKind.OnKeyword);
            var secondNull = Expect(SyntaxKind.NullKeyword);
            var input = ExpectIdent(Keyword.Input);
            return new RoutineCharacteristic
            {
                Span = SourceSpan.From(name, input),
                Name = name,
                Tail = [nullKeyword, onKeyword, secondNull, input],
            };
        }

        var called = ExpectIdent(Keyword.Called);
        var calledOn = Expect(SyntaxKind.OnKeyword);
        var calledNull = Expect(SyntaxKind.NullKeyword);
        var calledInput = ExpectIdent(Keyword.Input);
        return new RoutineCharacteristic
        {
            Span = SourceSpan.From(called, calledInput),
            Name = called,
            Tail = [calledOn, calledNull, calledInput],
        };
    }

    private RoutineDesignator ParseRoutineDesignator()
    {
        var start = _current;
        SyntaxToken? specific = null;
        if (IdentifierEquals(Keyword.Specific))
        {
            specific = Advance();
        }

        SyntaxToken? kind = null;
        if (IdentifierEquals(Keyword.Instance) || IdentifierEquals(Keyword.Static) || IdentifierEquals(Keyword.Constructor))
        {
            kind = Advance();
        }

        if (!IdentifierEquals(Keyword.Routine) && !IdentifierEquals(Keyword.Function)
            && !IdentifierEquals(Keyword.Procedure) && !IdentifierEquals(Keyword.Method))
        {
            throw new SqlParseException($"Expected routine designator, found {_current.Kind}", _current.Position);
        }

        var routineType = Advance();
        if (kind is not null && !TokenEquals(routineType, Keyword.Method))
        {
            throw new SqlParseException($"Expected METHOD, found {_current.Kind}", _current.Position);
        }

        var name = ParseQualifiedName();
        SyntaxToken? forKeyword = null;
        IReadOnlyList<SyntaxToken>? forType = null;
        var end = name[^1].Span;
        if (specific is null && _current.Kind == SyntaxKind.ForKeyword)
        {
            forKeyword = Advance();
            forType = ParseQualifiedName();
            end = forType[^1].Span;
        }

        return new RoutineDesignator
        {
            Span = SourceSpan.From(start, end),
            SpecificKeyword = specific,
            Kind = kind,
            RoutineType = routineType,
            Name = name,
            ForKeyword = forKeyword,
            ForType = forType,
        };
    }

    private TransformGroup ParseTransformGroup()
    {
        var name = Expect(SyntaxKind.Identifier);
        var openParen = Expect(SyntaxKind.OpenParen);
        var elements = new List<TransformElement> { ParseTransformElement() };
        while (_current.Kind == SyntaxKind.Comma)
        {
            Advance();
            elements.Add(ParseTransformElement());
        }

        var closeParen = Expect(SyntaxKind.CloseParen);
        return new TransformGroup
        {
            Span = SourceSpan.From(name, closeParen),
            Name = name,
            OpenParen = openParen,
            Elements = elements,
            CloseParen = closeParen,
        };
    }

    private TransformElement ParseTransformElement()
    {
        SyntaxToken direction;
        if (IdentifierEquals(Keyword.To) || _current.Kind == SyntaxKind.FromKeyword)
        {
            direction = Advance();
        }
        else
        {
            throw new SqlParseException($"Expected TO or FROM, found {_current.Kind}", _current.Position);
        }

        var sql = ExpectIdent(Keyword.Sql);
        var withKeyword = Expect(SyntaxKind.WithKeyword);
        var routine = ParseRoutineDesignator();
        return new TransformElement
        {
            Span = SourceSpan.From(direction, routine.Span),
            Direction = direction,
            SqlKeyword = sql,
            WithKeyword = withKeyword,
            Routine = routine,
        };
    }

    private bool IsParameterName()
    {
        if (_current.Kind != SyntaxKind.Identifier || IsTypeContinuation())
        {
            return false;
        }

        if (_index >= _tokens.Count)
        {
            return false;
        }

        var next = _tokens[_index];
        return next.Kind is SyntaxKind.Identifier or SyntaxKind.DateKeyword or SyntaxKind.TimeKeyword
            or SyntaxKind.TimestampKeyword or SyntaxKind.IntervalKeyword;
    }

    private bool IsTypeContinuation()
    {
        if (TokenEquals(_current, Keyword.Double) && NextEquals(Keyword.Precision))
        {
            return true;
        }

        if ((TokenEquals(_current, Keyword.Character) || TokenEquals(_current, Keyword.Char)
                || TokenEquals(_current, Keyword.Nchar) || TokenEquals(_current, Keyword.Binary))
            && (NextEquals(Keyword.Varying) || NextEquals(Keyword.Large)))
        {
            return true;
        }

        return TokenEquals(_current, Keyword.National)
            && (NextEquals(Keyword.Character) || NextEquals(Keyword.Char));
    }

    private bool IsTypeOptionStart() =>
        IsInstantiableOption() || IsFinalOption() || IdentifierEquals(Keyword.Ref) || _current.Kind == SyntaxKind.CastKeyword;

    private bool IsInstantiableOption() =>
        IdentifierEquals(Keyword.Instantiable)
        || (_current.Kind == SyntaxKind.NotKeyword && NextEquals(Keyword.Instantiable));

    private bool IsFinalOption() =>
        IdentifierEquals(Keyword.Final) || (_current.Kind == SyntaxKind.NotKeyword && NextEquals(Keyword.Final));

    private bool IsMethodStart() =>
        IdentifierEquals(Keyword.Method)
        || IdentifierEquals(Keyword.Overriding)
        || IdentifierEquals(Keyword.Static)
        || IdentifierEquals(Keyword.Constructor)
        || IdentifierEquals(Keyword.Instance);

    private bool IsCharacteristicStart() =>
        IdentifierEquals(Keyword.Language)
        || IdentifierEquals(Keyword.Parameter)
        || IdentifierEquals(Keyword.Deterministic)
        || (_current.Kind == SyntaxKind.NotKeyword && NextEquals(Keyword.Deterministic))
        || IdentifierEquals(Keyword.No)
        || IdentifierEquals(Keyword.Contains)
        || IdentifierEquals(Keyword.Reads)
        || IdentifierEquals(Keyword.Modifies)
        || IdentifierEquals(Keyword.Returns)
        || IdentifierEquals(Keyword.Called);
}
