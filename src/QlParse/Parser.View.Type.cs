namespace QlParse;

internal sealed partial class Parser
{
    internal void ParseAttributes(
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

                var referencesClause = ParseAttributeReferences();
                referencesKeyword = referencesClause.ReferencesKeyword;
                areKeyword = referencesClause.AreKeyword;
                notKeyword = referencesClause.NotKeyword;
                checkedKeyword = referencesClause.CheckedKeyword;
                onDelete = referencesClause.OnDelete;
                end = referencesClause.End;
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

    private readonly record struct AttributeReferences(
        SyntaxToken ReferencesKeyword,
        SyntaxToken AreKeyword,
        SyntaxToken? NotKeyword,
        SyntaxToken CheckedKeyword,
        ReferentialAction? OnDelete,
        SourceSpan End);

    private AttributeReferences ParseAttributeReferences()
    {
        var referencesKeyword = Advance();
        var areKeyword = ExpectIdent(Keyword.Are);
        SyntaxToken? notKeyword = null;
        if (_current.Kind == SyntaxKind.NotKeyword)
        {
            notKeyword = Advance();
        }

        var checkedKeyword = ExpectIdent(Keyword.Checked);
        var end = checkedKeyword.Span;
        ReferentialAction? onDelete = null;
        if (_current.Kind == SyntaxKind.OnKeyword)
        {
            onDelete = ParseOnDelete();
            end = onDelete.Span;
        }

        return new AttributeReferences(referencesKeyword, areKeyword, notKeyword, checkedKeyword, onDelete, end);
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
        else
        {
            if (IdentifierEquals(Keyword.No))
            {
                noKeyword = Advance();
                action = ExpectIdent(Keyword.Action);
            }
            else
            {
                if (IdentifierEquals(Keyword.Cascade) || IdentifierEquals(Keyword.Restrict))
                {
                    action = Advance();
                }
                else
                {
                    throw new SqlParseException($"Expected referential action, found {_current.Kind}", _current.Position);
                }
            }
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

    internal SourceSpan? ParseTypeOptions(
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
                EnsureNotAlreadySet(instantiable);
                var instantiableOption = ParseNotPrefixedOption(Keyword.Instantiable);
                notInstantiable = instantiableOption.NotKeyword;
                instantiable = instantiableOption.Keyword;
                end = instantiableOption.Keyword.Span;
                continue;
            }

            if (IsFinalOption())
            {
                EnsureNotAlreadySet(finalKeyword);
                var finalOption = ParseNotPrefixedOption(Keyword.Final);
                notFinal = finalOption.NotKeyword;
                finalKeyword = finalOption.Keyword;
                end = finalOption.Keyword.Span;
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

    private readonly record struct NotPrefixedOption(SyntaxToken? NotKeyword, SyntaxToken Keyword);

    private NotPrefixedOption ParseNotPrefixedOption(string keyword)
    {
        SyntaxToken? notKeyword = null;
        if (_current.Kind == SyntaxKind.NotKeyword)
        {
            notKeyword = Advance();
        }

        return new NotPrefixedOption(notKeyword, ExpectIdent(keyword));
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
        if ((!IdentifierEquals(Keyword.Source) && !IdentifierEquals(Keyword.Ref)) is true)
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

    internal List<MethodSpecification> ParseMethodList()
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
        var parameters = ParseRoutineParameterList();
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

        var initialEnd = returnsLocator?.Span ?? returnsType.Span;
        var tail = ParseMethodTail(overriding, initialEnd);

        return new MethodSpecification
        {
            Span = SourceSpan.From(start, tail.End),
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
            SpecificKeyword = tail.SpecificKeyword,
            SpecificName = tail.SpecificName,
            SelfResultKeyword = tail.SelfResultKeyword,
            SelfResultAsKeyword = tail.SelfResultAsKeyword,
            ResultKeyword = tail.ResultKeyword,
            SelfLocatorKeyword = tail.SelfLocatorKeyword,
            SelfLocatorAsKeyword = tail.SelfLocatorAsKeyword,
            LocatorKeyword = tail.LocatorKeyword,
            Characteristics = tail.Characteristics,
        };
    }

    private readonly record struct MethodTail(
        SyntaxToken? SpecificKeyword,
        IReadOnlyList<SyntaxToken>? SpecificName,
        SyntaxToken? SelfResultKeyword,
        SyntaxToken? SelfResultAsKeyword,
        SyntaxToken? ResultKeyword,
        SyntaxToken? SelfLocatorKeyword,
        SyntaxToken? SelfLocatorAsKeyword,
        SyntaxToken? LocatorKeyword,
        IReadOnlyList<RoutineCharacteristic> Characteristics,
        SourceSpan End);

    private MethodTail ParseMethodTail(SyntaxToken? overriding, SourceSpan initialEnd)
    {
        SyntaxToken? specific = null;
        IReadOnlyList<SyntaxToken>? specificName = null;
        SyntaxToken? selfResult = null;
        SyntaxToken? selfResultAs = null;
        SyntaxToken? result = null;
        SyntaxToken? selfLocator = null;
        SyntaxToken? selfLocatorAs = null;
        SyntaxToken? locator = null;
        var characteristics = new List<RoutineCharacteristic>();
        var end = initialEnd;
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
                var self = ParseSelfClause(result, locator);
                if (self.ResultKeyword is not null)
                {
                    selfResult = self.SelfResultKeyword;
                    selfResultAs = self.SelfResultAsKeyword;
                    result = self.ResultKeyword;
                }
                else
                {
                    selfLocator = self.SelfLocatorKeyword;
                    selfLocatorAs = self.SelfLocatorAsKeyword;
                    locator = self.LocatorKeyword;
                }

                end = self.End;
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

        return new MethodTail(specific, specificName, selfResult, selfResultAs, result, selfLocator, selfLocatorAs, locator, characteristics, end);
    }

    private readonly record struct SelfClause(
        SyntaxToken? SelfResultKeyword,
        SyntaxToken? SelfResultAsKeyword,
        SyntaxToken? ResultKeyword,
        SyntaxToken? SelfLocatorKeyword,
        SyntaxToken? SelfLocatorAsKeyword,
        SyntaxToken? LocatorKeyword,
        SourceSpan End);

    private SelfClause ParseSelfClause(SyntaxToken? existingResult, SyntaxToken? existingLocator)
    {
        var selfToken = Advance();
        var asToken = Expect(SyntaxKind.AsKeyword);
        if (IdentifierEquals(Keyword.Result))
        {
            if (existingResult is not null)
            {
                throw new SqlParseException($"Unexpected {_current.Kind}", _current.Position);
            }

            var resultToken = Advance();
            return new SelfClause(selfToken, asToken, resultToken, null, null, null, resultToken.Span);
        }

        if (!IdentifierEquals(Keyword.Locator))
        {
            throw new SqlParseException($"Expected RESULT or LOCATOR, found {_current.Kind}", _current.Position);
        }

        if (existingLocator is not null)
        {
            throw new SqlParseException($"Unexpected {_current.Kind}", _current.Position);
        }

        var locatorToken = Advance();
        return new SelfClause(null, null, null, selfToken, asToken, locatorToken, locatorToken.Span);
    }

    private List<RoutineParameter> ParseRoutineParameterList()
    {
        var parameters = new List<RoutineParameter>();
        if (_current.Kind == SyntaxKind.CloseParen)
        {
            return parameters;
        }

        parameters.Add(ParseRoutineParameter());
        while (_current.Kind == SyntaxKind.Comma)
        {
            Advance();
            parameters.Add(ParseRoutineParameter());
        }

        return parameters;
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

        return ParseSqlDataCharacteristic();
    }

    private RoutineCharacteristic ParseSqlDataCharacteristic()
    {
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

        if (!IsRoutineTypeKeyword())
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

    private bool IsRoutineTypeKeyword() =>
        IdentifierEquals(Keyword.Routine) || IdentifierEquals(Keyword.Function)
        || IdentifierEquals(Keyword.Procedure) || IdentifierEquals(Keyword.Method);

    internal TransformGroup ParseTransformGroup()
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

        if (IsCharacterFamilyKeyword() && (NextEquals(Keyword.Varying) || NextEquals(Keyword.Large)))
        {
            return true;
        }

        return TokenEquals(_current, Keyword.National)
            && (NextEquals(Keyword.Character) || NextEquals(Keyword.Char));
    }

    private bool IsCharacterFamilyKeyword() =>
        TokenEquals(_current, Keyword.Character) || TokenEquals(_current, Keyword.Char)
        || TokenEquals(_current, Keyword.Nchar) || TokenEquals(_current, Keyword.Binary);

    private bool IsTypeOptionStart() =>
        IsInstantiableOption() || IsFinalOption() || IdentifierEquals(Keyword.Ref) || _current.Kind == SyntaxKind.CastKeyword;

    private bool IsInstantiableOption() =>
        IdentifierEquals(Keyword.Instantiable)
        || (_current.Kind == SyntaxKind.NotKeyword && NextEquals(Keyword.Instantiable));

    private bool IsFinalOption() =>
        IdentifierEquals(Keyword.Final) || (_current.Kind == SyntaxKind.NotKeyword && NextEquals(Keyword.Final));

    internal bool IsMethodStart() =>
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
        || IsSqlDataCharacteristicStart();

    private bool IsSqlDataCharacteristicStart() =>
        IdentifierEquals(Keyword.No)
        || IdentifierEquals(Keyword.Contains)
        || IdentifierEquals(Keyword.Reads)
        || IdentifierEquals(Keyword.Modifies)
        || IdentifierEquals(Keyword.Returns)
        || IdentifierEquals(Keyword.Called);
}
