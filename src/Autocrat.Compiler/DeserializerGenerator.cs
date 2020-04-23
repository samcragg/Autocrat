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

        private readonly List<PropertyInfo> properties = new List<PropertyInfo>();
        private readonly SimpleNameSyntax classType;
        private readonly IdentifierNameSyntax instanceField;

        /// <summary>
        /// Initializes a new instance of the <see cref="DeserializerGenerator"/> class.
        /// </summary>
        /// <param name="classType">The name of the class to deserialize.</param>
        public DeserializerGenerator(SimpleNameSyntax classType)
        {
            this.classType = classType;
            this.instanceField = IdentifierName("instance");
        }

        /// <summary>
        /// Adds a property to be deserialized by the generated class.
        /// </summary>
        /// <param name="name">The name of the JSON property.</param>
        /// <param name="type">The type of the object property.</param>
        /// <param name="allowNulls">
        /// Determines whether <c>null</c> values are valid or not.
        /// </param>
        public void AddProperty(string name, ITypeSymbol type, bool allowNulls)
        {
            this.properties.Add(new PropertyInfo
            {
                AllowNulls = allowNulls,
                Name = name,
                Type = type,
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

        private static StatementSyntax CreateThrowFormatException(string message)
        {
            return ThrowStatement(
                ObjectCreationExpression(IdentifierName(nameof(FormatException)))
                .WithArgumentList(ArgumentList(SingletonSeparatedList(
                    Argument(CreateLiteral(message))))));
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
            if ((typeSymbol as IArrayTypeSymbol)?.ElementType?.SpecialType == SpecialType.System_Byte)
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
            var body = new List<StatementSyntax>();
            IdentifierNameSyntax reader = IdentifierName("reader");
            IdentifierNameSyntax name = IdentifierName("name");

            SwitchSectionSyntax CreateSwitchSection(PropertyInfo property)
            {
                //// case 123 when CaseInsensitiveStringHelper.Equals("ABC", name):
                int hashCode = CaseInsensitiveStringHelper.GetHashCode(
                    Encoding.UTF8.GetBytes(property.Name));

                SwitchLabelSyntax switchLabel = CasePatternSwitchLabel(
                    ConstantPattern(CreateLiteral(hashCode)),
                    WhenClause(
                        InvokeMethod(
                            IdentifierName(nameof(CaseInsensitiveStringHelper)),
                            nameof(CaseInsensitiveStringHelper.Equals),
                            CreateLiteral(property.Name.ToUpperInvariant()),
                            name)),
                    Token(SyntaxKind.ColonToken));

                //// this.Read_Abc(ref reader)
                //// return
                var switchStatements = new StatementSyntax[]
                {
                    ExpressionStatement(InvocationExpression(IdentifierName("Read_" + property.Name))
                        .WithArgumentList(ArgumentList(SingletonSeparatedList(
                            Argument(reader).WithRefKindKeyword(Token(SyntaxKind.RefKeyword)))))),
                    ReturnStatement(),
                };

                return SwitchSection(SingletonList(switchLabel), List(switchStatements));
            }

            //// var name = reader.ValueSpan
            ExpressionSyntax valueSpan = MemberAccessExpression(
                SyntaxKind.SimpleMemberAccessExpression,
                reader,
                IdentifierName(nameof(Utf8JsonReader.ValueSpan)));

            body.Add(
                LocalDeclarationStatement(
                    VariableDeclaration(
                        IdentifierName("var"),
                        SingletonSeparatedList(
                            VariableDeclarator(name.Identifier)
                            .WithInitializer(EqualsValueClause(valueSpan))))));

            //// switch (CaseInsensitiveStringHelper.GetHashCode(name))
            body.Add(
                SwitchStatement(
                    InvokeMethod(
                        IdentifierName(nameof(CaseInsensitiveStringHelper)),
                        nameof(CaseInsensitiveStringHelper.GetHashCode),
                        name),
                    List(this.properties.Select(CreateSwitchSection))));

            // Skip unknown properties
            //// reader.Read()
            //// reader.TrySkip()
            body.Add(ExpressionStatement(InvokeMethod(reader, nameof(Utf8JsonReader.Read))));
            body.Add(ExpressionStatement(InvokeMethod(reader, nameof(Utf8JsonReader.TrySkip))));

            return CreateMethod(SyntaxKind.PrivateKeyword, "ReadProperty", reader, body);
        }

        private MethodDeclarationSyntax CreateReadPropertyValue(PropertyInfo property)
        {
            // The call to this.classType is just there to stop the warning over instance fields
            string? readerMethod = GetJsonReaderMethod(property.Type);
            if (readerMethod == null || this.classType == null)
            {
                throw new NotImplementedException("Need support for arrays, enums and nested serializers");
            }

            var body = new List<StatementSyntax>();
            IdentifierNameSyntax reader = IdentifierName("reader");

            //// reader.Read()
            body.Add(ExpressionStatement(InvokeMethod(reader, nameof(Utf8JsonReader.Read))));

            //// this.instance.Value = reader.GetXxx()
            ExpressionSyntax accessProperty = MemberAccessExpression(
                SyntaxKind.SimpleMemberAccessExpression,
                IdentifierName("instance"),
                IdentifierName(property.Name));

            StatementSyntax setProperty = ExpressionStatement(AssignmentExpression(
                SyntaxKind.SimpleAssignmentExpression,
                accessProperty,
                InvokeMethod(reader, readerMethod)));

            if (property.AllowNulls)
            {
                //// if (reader.TokenType == JsonTokenType.Null)
                ////     this.instance.Value = null
                //// else
                ////     this.instance.Value = reader.GetXxx()
                body.Add(IfStatement(
                    CheckTokenType(reader, SyntaxKind.EqualsExpression, JsonTokenType.Null),
                    ExpressionStatement(
                        AssignmentExpression(
                            SyntaxKind.SimpleAssignmentExpression,
                            accessProperty,
                            LiteralExpression(SyntaxKind.NullLiteralExpression))),
                    ElseClause(setProperty)));
            }
            else
            {
                body.Add(setProperty);
            }

            return CreateMethod(SyntaxKind.PrivateKeyword, "Read_" + property.Name, reader, body);
        }

        private IEnumerable<MemberDeclarationSyntax> GetMethods()
        {
            MethodDeclarationSyntax readProperty = this.CreateReadPropertyMethod();
            yield return this.CreateReadMethod(readProperty.Identifier);
            yield return readProperty;

            foreach (PropertyInfo property in this.properties)
            {
                yield return this.CreateReadPropertyValue(property);
            }
        }

        private struct PropertyInfo
        {
            public bool AllowNulls;
            public string Name;
            public ITypeSymbol Type;
        }
    }
}
