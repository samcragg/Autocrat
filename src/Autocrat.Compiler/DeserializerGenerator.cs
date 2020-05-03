// Copyright (c) Samuel Cragg.
//
// Licensed under the MIT license. See LICENSE file in the project root for
// full license information.

namespace Autocrat.Compiler
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using System.Text.Json;
    using Autocrat.Abstractions;
    using Microsoft.CodeAnalysis;
    using Microsoft.CodeAnalysis.CSharp;
    using Microsoft.CodeAnalysis.CSharp.Syntax;
    using static System.Diagnostics.Debug;
    using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;

    /// <summary>
    /// Generates classes to deserialize JSON into objects.
    /// </summary>
    internal class DeserializerGenerator
    {
        /// <summary>
        /// The suffix added to the class name used to create the name of the
        /// generated deserializer class.
        /// </summary>
        internal const string GeneratedClassSuffix = "_Deserializer";

        /// <summary>
        /// Gets the name of the public method to call in the generated class
        /// to read the JSON data.
        /// </summary>
        internal const string ReadMethodName = "Read";

        private readonly SimpleNameSyntax classType;
        private readonly SymbolDisplayFormat displayFormat = SymbolDisplayFormat.CSharpErrorMessageFormat;
        private readonly IdentifierNameSyntax instanceField;
        private readonly SemanticModel model;
        private readonly List<PropertyInfo> properties = new List<PropertyInfo>();

        /// <summary>
        /// Initializes a new instance of the <see cref="DeserializerGenerator"/> class.
        /// </summary>
        /// <param name="model">Contains the semantic information.</param>
        /// <param name="classType">The name of the class to deserialize.</param>
        public DeserializerGenerator(SemanticModel model, SimpleNameSyntax classType)
        {
            this.classType = classType;
            this.instanceField = IdentifierName("instance");
            this.model = model;
        }

        /// <summary>
        /// Adds a property to be deserialized by the generated class.
        /// </summary>
        /// <param name="property">Contains the property information.</param>
        public void AddProperty(PropertyDeclarationSyntax property)
        {
            TypeInfo typeInfo = this.model.GetTypeInfo(property.Type);
            ITypeSymbol typeSymbol = typeInfo.Type ??
                throw new InvalidOperationException("Unable to get type information for " + property.Type);

            if (property.Type is NullableTypeSyntax)
            {
                typeSymbol = typeSymbol.WithNullableAnnotation(NullableAnnotation.Annotated);
            }

            this.properties.Add(new PropertyInfo
            {
                Name = property.Identifier.ValueText,
                Type = typeSymbol,
            });
        }

        /// <summary>
        /// Generates the class to deserialize JSON data.
        /// </summary>
        /// <returns>A new compilation unit.</returns>
        public CompilationUnitSyntax Generate()
        {
            UsingDirectiveSyntax[] usings = new[]
            {
                UsingDirective(IdentifierName("System")),
                UsingDirective(ParseName("System.Text.Json")),
                UsingDirective(ParseName("Autocrat.Abstractions")),
            };
            return CompilationUnit()
                .WithUsings(List(usings))
                .WithMembers(SingletonList(this.CreateClass()));
        }

        private static ExpressionSyntax CheckTokenType(
            IdentifierNameSyntax reader,
            SyntaxKind compare,
            JsonTokenType tokenType)
        {
            return BinaryExpression(
                compare,
                MemberAccessExpression(
                    SyntaxKind.SimpleMemberAccessExpression,
                    reader,
                    IdentifierName(nameof(Utf8JsonReader.TokenType))),
                MemberAccessExpression(
                    SyntaxKind.SimpleMemberAccessExpression,
                    IdentifierName(nameof(JsonTokenType)),
                    IdentifierName(tokenType.ToString())));
        }

        private static ExpressionSyntax CreateLiteral(int value)
        {
            return LiteralExpression(SyntaxKind.NumericLiteralExpression, Literal(value));
        }

        private static ExpressionSyntax CreateLiteral(string value)
        {
            return LiteralExpression(SyntaxKind.StringLiteralExpression, Literal(value));
        }

        private static StatementSyntax CreateLocal(
            IdentifierNameSyntax identifier,
            ExpressionSyntax? initialValue = null,
            TypeSyntax? type = null)
        {
            VariableDeclaratorSyntax variable = VariableDeclarator(identifier.Identifier);
            if (initialValue != null)
            {
                variable = variable.WithInitializer(EqualsValueClause(initialValue));
            }

            return LocalDeclarationStatement(
                VariableDeclaration(
                    type ?? IdentifierName("var"),
                    SingletonSeparatedList(variable)));
        }

        private static MethodDeclarationSyntax CreateMethod(
            SyntaxKind modifier,
            string name,
            IdentifierNameSyntax reader,
            IEnumerable<StatementSyntax> body,
            TypeSyntax? returnType = null)
        {
            returnType ??= PredefinedType(Token(SyntaxKind.VoidKeyword));
            return MethodDeclaration(returnType, Identifier(name))
                .WithModifiers(TokenList(Token(modifier)))
                .WithParameterList(
                    ParameterList(
                        SingletonSeparatedList(
                            Parameter(reader.Identifier)
                            .WithModifiers(TokenList(Token(SyntaxKind.RefKeyword)))
                            .WithType(IdentifierName(nameof(Utf8JsonReader))))))
                .WithBody(Block(body));
        }

        private static StatementSyntax CreateSwitchOnString(
            ExpressionSyntax value,
            IEnumerable<string> cases,
            Func<string, StatementSyntax> caseStatment,
            IEnumerable<StatementSyntax> defaultStatements)
        {
            IdentifierNameSyntax switchValue = IdentifierName("switchValue");
            var switchSections = new List<SwitchSectionSyntax>();
            foreach (string caseValue in cases)
            {
                //// case 123 when CaseInsensitiveStringHelper.Equals("ABC", name):
                int hashCode = CaseInsensitiveStringHelper.GetHashCode(
                    Encoding.UTF8.GetBytes(caseValue));

                SwitchLabelSyntax switchLabel = CasePatternSwitchLabel(
                    ConstantPattern(CreateLiteral(hashCode)),
                    WhenClause(
                        InvokeMethod(
                            IdentifierName(nameof(CaseInsensitiveStringHelper)),
                            nameof(CaseInsensitiveStringHelper.Equals),
                            CreateLiteral(caseValue.ToUpperInvariant()),
                            switchValue)),
                    Token(SyntaxKind.ColonToken));

                switchSections.Add(SwitchSection(
                    SingletonList(switchLabel),
                    List(new[]
                    {
                        caseStatment(caseValue),
                        BreakStatement(),
                    })));
            }

            //// default:
            switchSections.Add(SwitchSection(
                SingletonList<SwitchLabelSyntax>(DefaultSwitchLabel()),
                List(defaultStatements)));

            //// var switchValue = ...
            //// switch (CaseInsensitiveStringHelper.GetHashCode(switchValue))
            return Block(
                CreateLocal(switchValue, value),
                SwitchStatement(
                    InvokeMethod(
                        IdentifierName(nameof(CaseInsensitiveStringHelper)),
                        nameof(CaseInsensitiveStringHelper.GetHashCode),
                        switchValue),
                    List(switchSections)));
        }

        private static StatementSyntax CreateThrowFormatException(string message)
        {
            return ThrowStatement(
                ObjectCreationExpression(IdentifierName(nameof(FormatException)))
                .WithArgumentList(ArgumentList(SingletonSeparatedList(
                    Argument(CreateLiteral(message))))));
        }

        private static StatementSyntax GenerateReadValue(
            ExpressionSyntax target,
            IdentifierNameSyntax reader,
            ITypeSymbol type)
        {
            string? readerMethod = GetJsonReaderMethod(type);
            if (readerMethod == null)
            {
                throw new NotImplementedException("Need support for nested serializers");
            }

            //// target = reader.GetXxx()
            return ExpressionStatement(AssignmentExpression(
                SyntaxKind.SimpleAssignmentExpression,
                target,
                InvokeMethod(reader, readerMethod)));
        }

        private static string? GetJsonReaderMethod(ITypeSymbol typeSymbol)
        {
            return typeSymbol.SpecialType switch
            {
                SpecialType.System_Boolean => nameof(Utf8JsonReader.GetBoolean),
                SpecialType.System_Byte => nameof(Utf8JsonReader.GetByte),
                SpecialType.System_DateTime => nameof(Utf8JsonReader.GetDateTime),
                SpecialType.System_Decimal => nameof(Utf8JsonReader.GetDecimal),
                SpecialType.System_Double => nameof(Utf8JsonReader.GetDouble),
                SpecialType.System_Int16 => nameof(Utf8JsonReader.GetInt16),
                SpecialType.System_Int32 => nameof(Utf8JsonReader.GetInt32),
                SpecialType.System_Int64 => nameof(Utf8JsonReader.GetInt64),
                SpecialType.System_SByte => nameof(Utf8JsonReader.GetSByte),
                SpecialType.System_Single => nameof(Utf8JsonReader.GetSingle),
                SpecialType.System_String => nameof(Utf8JsonReader.GetString),
                SpecialType.System_UInt16 => nameof(Utf8JsonReader.GetUInt16),
                SpecialType.System_UInt32 => nameof(Utf8JsonReader.GetUInt32),
                SpecialType.System_UInt64 => nameof(Utf8JsonReader.GetUInt64),
                _ => GetJsonReadMethodForUnknownSpecialType(typeSymbol),
            };
        }

        private static string? GetJsonReadMethodForUnknownSpecialType(ITypeSymbol typeSymbol)
        {
            // Must check for the array _before_ the check for System types
            if (IsByteArray(typeSymbol))
            {
                return nameof(Utf8JsonReader.GetBytesFromBase64);
            }

            if (string.Equals(
                typeSymbol.ContainingNamespace.Name,
                nameof(System),
                StringComparison.Ordinal))
            {
                switch (typeSymbol.Name)
                {
                    case nameof(Guid):
                        return nameof(Utf8JsonReader.GetGuid);

                    case nameof(DateTimeOffset):
                        return nameof(Utf8JsonReader.GetDateTimeOffset);
                }
            }

            return null;
        }

        private static ExpressionSyntax InvokeMethod(
            ExpressionSyntax expression,
            string name,
            params ExpressionSyntax[] parameters)
        {
            InvocationExpressionSyntax invoke = InvocationExpression(
                MemberAccessExpression(
                    SyntaxKind.SimpleMemberAccessExpression,
                    expression,
                    IdentifierName(name)));

            if (parameters.Length > 0)
            {
                invoke = invoke.WithArgumentList(
                    ArgumentList(
                        SeparatedList(
                            Array.ConvertAll(parameters, Argument))));
            }

            return invoke;
        }

        private static bool IsByteArray(ITypeSymbol type)
        {
            if (type is IArrayTypeSymbol arrayType)
            {
                return arrayType.ElementType.SpecialType == SpecialType.System_Byte;
            }
            else
            {
                return false;
            }
        }

        private static ITypeSymbol UnwrapNullableTypes(ITypeSymbol symbol)
        {
            if ((symbol.NullableAnnotation == NullableAnnotation.Annotated) &&
                symbol.IsValueType &&
                (symbol.OriginalDefinition.SpecialType == SpecialType.System_Nullable_T))
            {
                return ((INamedTypeSymbol)symbol).TypeArguments[0]
                    .WithNullableAnnotation(NullableAnnotation.Annotated);
            }
            else
            {
                return symbol;
            }
        }

        private MemberDeclarationSyntax CreateClass()
        {
            FieldDeclarationSyntax field =
                FieldDeclaration(VariableDeclaration(
                    this.classType,
                    SingletonSeparatedList(VariableDeclarator(this.instanceField.Identifier))))
                .WithModifiers(TokenList(Token(SyntaxKind.PrivateKeyword)));

            return ClassDeclaration(this.classType.Identifier.ValueText + GeneratedClassSuffix)
                .AddModifiers(Token(SyntaxKind.PublicKeyword))
                .AddMembers(
                    new[] { field }
                    .Concat(this.GetMethods())
                    .ToArray());
        }

        private MethodDeclarationSyntax CreateReadMethod(SyntaxToken readPropertyMethod)
        {
            var body = new List<StatementSyntax>();
            IdentifierNameSyntax reader = IdentifierName("reader");

            //// if (!reader.Read() || (reader.TokenType != JsonTokenType.StartObject))
            ////     throw new FormatException("Missing start object token")
            ExpressionSyntax callReaderRead = InvokeMethod(reader, nameof(Utf8JsonReader.Read));
            body.Add(
                IfStatement(
                    BinaryExpression(
                        SyntaxKind.LogicalOrExpression,
                        PrefixUnaryExpression(SyntaxKind.LogicalNotExpression, callReaderRead),
                        CheckTokenType(
                            reader,
                            SyntaxKind.NotEqualsExpression,
                            JsonTokenType.StartObject)),
                    CreateThrowFormatException("Missing start object token")));

            //// this.instance = new ClassType()
            body.Add(
                ExpressionStatement(
                    AssignmentExpression(
                        SyntaxKind.SimpleAssignmentExpression,
                        this.instanceField,
                        ObjectCreationExpression(
                            this.classType,
                            ArgumentList(),
                            null))));

            //// while (reader.Read() && reader.TokenType == JsonTokenType.PropertyName)
            ////     this.ReadProperty(ref reader)
            ExpressionSyntax whileCondition = BinaryExpression(
                SyntaxKind.LogicalAndExpression,
                callReaderRead,
                CheckTokenType(
                    reader,
                    SyntaxKind.EqualsExpression,
                    JsonTokenType.PropertyName));

            body.Add(
                WhileStatement(
                    whileCondition,
                    ExpressionStatement(
                        InvocationExpression(IdentifierName(readPropertyMethod))
                        .WithArgumentList(ArgumentList(SingletonSeparatedList(
                            Argument(reader)
                            .WithRefKindKeyword(Token(SyntaxKind.RefKeyword))))))));

            //// if (reader.TokenType != JsonTokenType.EndObject)
            ////     throw new FormatException("Missing end object token")
            body.Add(
                IfStatement(
                    CheckTokenType(reader, SyntaxKind.NotEqualsExpression, JsonTokenType.EndObject),
                    CreateThrowFormatException("Missing end object token")));

            //// return this.instance
            body.Add(ReturnStatement(this.instanceField));

            return CreateMethod(
                SyntaxKind.PublicKeyword,
                ReadMethodName,
                reader,
                body,
                this.classType);
        }

        private MethodDeclarationSyntax CreateReadPropertyMethod()
        {
            IdentifierNameSyntax reader = IdentifierName("reader");
            StatementSyntax CallReadPropertyValueMethod(string property)
            {
                //// this.Read_Abc(ref reader)
                return ExpressionStatement(
                    InvocationExpression(
                        IdentifierName("Read_" + property),
                        ArgumentList(SingletonSeparatedList(
                            Argument(reader)
                            .WithRefKindKeyword(Token(SyntaxKind.RefKeyword))))));
            }

            //// default: // Skip unknown properties
            ////     reader.Read()
            ////     reader.TrySkip()
            var defaultStatements = new StatementSyntax[]
            {
                ExpressionStatement(InvokeMethod(reader, nameof(Utf8JsonReader.Read))),
                ExpressionStatement(InvokeMethod(reader, nameof(Utf8JsonReader.TrySkip))),
                BreakStatement(),
            };

            //// switch (reader.ValueSpan)
            StatementSyntax switchStatment = CreateSwitchOnString(
                MemberAccessExpression(
                    SyntaxKind.SimpleMemberAccessExpression,
                    reader,
                    IdentifierName(nameof(Utf8JsonReader.ValueSpan))),
                this.properties.Select(pi => pi.Name),
                CallReadPropertyValueMethod,
                defaultStatements);

            return CreateMethod(
                SyntaxKind.PrivateKeyword,
                "ReadProperty",
                reader,
                new[] { switchStatment });
        }

        private StatementSyntax GenerateReadArray(
            ExpressionSyntax target,
            IdentifierNameSyntax reader,
            IArrayTypeSymbol arrayType)
        {
            IdentifierNameSyntax buffer = IdentifierName("buffer");
            IdentifierNameSyntax value = IdentifierName("value");

            // We're not compiling in a nullable context, therefore, we need
            // to remove the nullable annotation from reference types (we need
            // to leave them on for value types as they're actually
            // System.Nullable<T>)
            ITypeSymbol elementTypeSymbol = arrayType.ElementType;
            if (!elementTypeSymbol.IsValueType)
            {
                elementTypeSymbol = elementTypeSymbol.WithNullableAnnotation(NullableAnnotation.None);
            }

            TypeSyntax elementType = ParseTypeName(
                elementTypeSymbol.ToDisplayString(this.displayFormat));

            //// if (reader.TokenType != JsonTokenType.StartArray)
            ////     throw new FormatException("Missing start array token")
            StatementSyntax assertStartArray = IfStatement(
                CheckTokenType(reader, SyntaxKind.NotEqualsExpression, JsonTokenType.StartArray),
                CreateThrowFormatException("Missing start array token"));

            //// var buffer = new List<type>();
            NameSyntax listType = GenericName(
                Identifier("System.Collections.Generic.List"),
                TypeArgumentList(SingletonSeparatedList(elementType)));

            StatementSyntax createBuffer = CreateLocal(
                buffer,
                ObjectCreationExpression(listType, ArgumentList(), null));

            //// while (reader.Read() && reader.TokenType != JsonTokenType.EndArray)
            ExpressionSyntax whileCondition = BinaryExpression(
                SyntaxKind.LogicalAndExpression,
                InvokeMethod(reader, nameof(Utf8JsonReader.Read)),
                CheckTokenType(
                    reader,
                    SyntaxKind.NotEqualsExpression,
                    JsonTokenType.EndArray));

            ////     var value = reader.ReadXxx()
            ////     buffer.Add(value)
            StatementSyntax whileBody = Block(
                CreateLocal(value, type: elementType),
                this.GenerateReadValueWithNullCheck(value, reader, arrayType.ElementType),
                ExpressionStatement(InvokeMethod(buffer, "Add", value)));

            return Block(
                assertStartArray,
                createBuffer,
                WhileStatement(whileCondition, whileBody),
                ExpressionStatement(
                    AssignmentExpression(
                        SyntaxKind.SimpleAssignmentExpression,
                        target,
                        InvokeMethod(buffer, "ToArray"))));
        }

        private StatementSyntax GenerateReadEnum(
            ExpressionSyntax target,
            IdentifierNameSyntax reader,
            INamedTypeSymbol propertyType)
        {
            TypeSyntax enumType = ParseTypeName(
                propertyType.ToDisplayString(this.displayFormat));

            StatementSyntax ConvertEnumValue(string member)
            {
                //// target = EnumType.Member
                return ExpressionStatement(
                    AssignmentExpression(
                        SyntaxKind.SimpleAssignmentExpression,
                        target,
                        MemberAccessExpression(
                            SyntaxKind.SimpleMemberAccessExpression,
                            ParseName(propertyType.ToDisplayString(this.displayFormat)),
                            IdentifierName(member))));
            }

            //// switch (reader.GetString())
            ////     default: throw FormatException(...)
            StatementSyntax readEnumAsString = CreateSwitchOnString(
                InvokeMethod(reader, nameof(Utf8JsonReader.GetString)),
                propertyType.MemberNames,
                ConvertEnumValue,
                new[] { CreateThrowFormatException("Invalid enum value") });

            //// target = (EnumType)reader.GetIntXx()
            Assert(propertyType.EnumUnderlyingType != null, "Method should only be called for enum types.");
            string? readerMethod = GetJsonReaderMethod(propertyType.EnumUnderlyingType);
            Assert(readerMethod != null, "Missing read integer mapping");
            StatementSyntax readEnumAsNumber = ExpressionStatement(AssignmentExpression(
                SyntaxKind.SimpleAssignmentExpression,
                target,
                CastExpression(
                    enumType,
                    InvokeMethod(reader, readerMethod))));

            //// if (reader.TokenType == JsonTokenType.Number)
            ////    target = (EnumType)reader.GetIntXx()
            //// else
            ////    switch ...
            return IfStatement(
                CheckTokenType(reader, SyntaxKind.EqualsExpression, JsonTokenType.Number),
                readEnumAsNumber,
                ElseClause(readEnumAsString));
        }

        private MethodDeclarationSyntax GenerateReadValueMethod(PropertyInfo property)
        {
            var body = new List<StatementSyntax>();
            IdentifierNameSyntax reader = IdentifierName("reader");

            //// reader.Read()
            body.Add(ExpressionStatement(InvokeMethod(reader, nameof(Utf8JsonReader.Read))));

            //// this.instance.Value = ...
            ExpressionSyntax accessProperty = MemberAccessExpression(
                SyntaxKind.SimpleMemberAccessExpression,
                IdentifierName("instance"),
                IdentifierName(property.Name));

            body.Add(this.GenerateReadValueWithNullCheck(
                accessProperty,
                reader,
                property.Type));

            return CreateMethod(
                SyntaxKind.PrivateKeyword,
                "Read_" + property.Name,
                reader,
                body);
        }

        private StatementSyntax GenerateReadValueWithNullCheck(
            ExpressionSyntax target,
            IdentifierNameSyntax reader,
            ITypeSymbol type)
        {
            type = UnwrapNullableTypes(type);
            StatementSyntax readValue = type.TypeKind switch
            {
                TypeKind.Array when !IsByteArray(type) =>
                    this.GenerateReadArray(target, reader, (IArrayTypeSymbol)type),

                TypeKind.Enum =>
                    this.GenerateReadEnum(target, reader, (INamedTypeSymbol)type),

                _ => GenerateReadValue(target, reader, type),
            };

            if (type.NullableAnnotation == NullableAnnotation.Annotated)
            {
                //// if (reader.TokenType == JsonTokenType.Null)
                ////     this.instance.Value = null
                //// else
                ////     this.instance.Value = ...
                return IfStatement(
                    CheckTokenType(reader, SyntaxKind.EqualsExpression, JsonTokenType.Null),
                    ExpressionStatement(
                        AssignmentExpression(
                            SyntaxKind.SimpleAssignmentExpression,
                            target,
                            LiteralExpression(SyntaxKind.NullLiteralExpression))),
                    ElseClause(readValue));
            }
            else
            {
                return readValue;
            }
        }

        private IEnumerable<MemberDeclarationSyntax> GetMethods()
        {
            MethodDeclarationSyntax readProperty = this.CreateReadPropertyMethod();
            yield return this.CreateReadMethod(readProperty.Identifier);
            yield return readProperty;

            foreach (PropertyInfo property in this.properties)
            {
                yield return this.GenerateReadValueMethod(property);
            }
        }

        private struct PropertyInfo
        {
            public string Name;
            public ITypeSymbol Type;
        }
    }
}
