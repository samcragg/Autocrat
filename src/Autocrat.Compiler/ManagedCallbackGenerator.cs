// Copyright (c) Samuel Cragg.
//
// Licensed under the MIT license. See LICENSE file in the project root for
// full license information.

namespace Autocrat.Compiler
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Runtime.InteropServices;
    using Microsoft.CodeAnalysis;
    using Microsoft.CodeAnalysis.CSharp;
    using Microsoft.CodeAnalysis.CSharp.Syntax;
    using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;

    /// <summary>
    /// Creates methods that are exported to native code.
    /// </summary>
    internal class ManagedCallbackGenerator
    {
        // This class goes through the methods in a type and generates static
        // methods that can be called from native code. The arguments passed to
        // the constructor will be created as local variables in the method,
        // i.e.
        //
        //// class Example
        //// {
        ////     private readonly object injected;
        ////
        ////     public Example(object injected)
        ////     {
        ////         this.injected = injected;
        ////     }
        ////
        ////     public object Method()
        ////     {
        ////         this.injected;
        ////     }
        //// }
        //
        // would be transformed to:
        //
        //// public static class NativeCallableMethods
        //// {
        ////     [NativeMethod("Example_Method")]
        ////     public static object Method()
        ////     {
        ////         object injected = new object();
        ////         var instance = new Example(injected);
        ////         return instance.Method();
        ////     }
        //// }
        private const string AdapterClassName = "NativeCallableMethods";
        private readonly Func<InstanceBuilder> instanceBuilder;
        private readonly NativeImportGenerator nativeGenerator;
        private ClassDeclarationSyntax nativeClass;

        /// <summary>
        /// Initializes a new instance of the <see cref="ManagedCallbackGenerator"/> class.
        /// </summary>
        /// <param name="instanceBuilder">
        /// Factory that creates the builder to generates code to create objects.
        /// </param>
        /// <param name="nativeGenerator">Used to register the managed methods.</param>
        public ManagedCallbackGenerator(
            Func<InstanceBuilder> instanceBuilder,
            NativeImportGenerator nativeGenerator)
        {
            this.instanceBuilder = instanceBuilder;
            this.nativeClass = ClassDeclaration(AdapterClassName);
            this.nativeGenerator = nativeGenerator;
        }

        /// <summary>
        /// Generates a native callable method for the specified type.
        /// </summary>
        /// <param name="nativeSignature">The format of the native signature.</param>
        /// <param name="method">The method in the type to generate.</param>
        /// <returns>The name of the generated method.</returns>
        public virtual int CreateMethod(string nativeSignature, IMethodSymbol method)
        {
            TypeSyntax returnType = ParseTypeName(method.ReturnType.ToDisplayString());
            string methodName = method.ContainingType.Name + "_" + method.Name;
            MethodDeclarationSyntax declaration = MethodDeclaration(returnType, methodName)
                .WithAttributeLists(SingletonList(AttributeList(SingletonSeparatedList(
                    CreateNativeCallableAttribute(methodName)))))
                .WithBody(this.CreateBody(method))
                .WithParameterList(ParameterList(SeparatedList(
                    CreateParameters(method))));

            this.nativeClass = this.nativeClass.AddMembers(declaration);
            return this.nativeGenerator.RegisterMethod(nativeSignature, methodName);
        }

        /// <summary>
        /// Creates a compilation unit with a class containing the generated
        /// adapter methods.
        /// </summary>
        /// <returns>A new compilation unit.</returns>
        public virtual CompilationUnitSyntax GetCompilationUnit()
        {
            UsingDirectiveSyntax nativeInterop = UsingDirective(
                ParseName("System.Runtime.InteropServices"));

            return CompilationUnit()
                .WithUsings(SingletonList(nativeInterop))
                .WithMembers(SingletonList<MemberDeclarationSyntax>(this.nativeClass));
        }

        /// <summary>
        /// Generates an attribute indicating that the method can be called by
        /// native code.
        /// </summary>
        /// <param name="method">The native name of the method.</param>
        /// <returns>An attribute syntax.</returns>
        internal static AttributeSyntax CreateNativeCallableAttribute(string method)
        {
            AttributeArgumentSyntax callingConvention = AttributeArgument(
                NameEquals(IdentifierName("CallingConvention")),
                null,
                MemberAccessExpression(
                    SyntaxKind.SimpleMemberAccessExpression,
                    IdentifierName(nameof(CallingConvention)),
                    IdentifierName(nameof(CallingConvention.Cdecl))));

            AttributeArgumentSyntax entryPoint = AttributeArgument(
                NameEquals(IdentifierName("EntryPoint")),
                null,
                LiteralExpression(SyntaxKind.StringLiteralExpression, Literal(method)));

            var arguments = new AttributeArgumentSyntax[]
            {
                entryPoint,
                callingConvention,
            };

            return Attribute(IdentifierName("NativeCallable"))
                .WithArgumentList(AttributeArgumentList(SeparatedList(arguments)));
        }

        private static IEnumerable<ParameterSyntax> CreateParameters(IMethodSymbol method)
        {
            foreach (IParameterSymbol parameter in method.Parameters)
            {
                TypeSyntax type = ParseTypeName(parameter.Type.ToDisplayString());
                SyntaxToken name = Identifier(parameter.Name);
                yield return Parameter(name).WithType(type);
            }
        }

        private BlockSyntax CreateBody(IMethodSymbol method)
        {
            ArgumentSyntax CreateArgument(IParameterSymbol symbol)
            {
                return Argument(IdentifierName(symbol.Name));
            }

            ArgumentListSyntax arguments = ArgumentList(SeparatedList(
                    method.Parameters.Select(CreateArgument)));

            InstanceBuilder builder = this.instanceBuilder();
            IdentifierNameSyntax instance = builder.GenerateForType(
                method.ContainingType);

            InvocationExpressionSyntax invocation = InvocationExpression(
                MemberAccessExpression(
                    SyntaxKind.SimpleMemberAccessExpression,
                    instance,
                    IdentifierName(method.Name)))
                .WithArgumentList(arguments);

            var statements = new List<StatementSyntax>();
            statements.AddRange(builder.LocalDeclarations);
            if (method.ReturnsVoid)
            {
                statements.Add(ExpressionStatement(invocation));
            }
            else
            {
                statements.Add(ReturnStatement(invocation));
            }

            return Block(statements);
        }
    }
}
