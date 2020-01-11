// Copyright (c) Samuel Cragg.
//
// Licensed under the MIT license. See LICENSE file in the project root for
// full license information.

namespace Autocrat.Compiler
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using Microsoft.CodeAnalysis;
    using Microsoft.CodeAnalysis.CSharp;
    using Microsoft.CodeAnalysis.CSharp.Syntax;
    using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;

    /// <summary>
    /// Builds a method to register the known worker types.
    /// </summary>
    internal class WorkerRegisterGenerator : MethodGenerator
    {
        // This class goes through every call to IWorkerFactory.GetWorker<Type>()
        // and generates a method that can create the type and registers that
        // method with the native code. This is to allow for dependency
        // injection, for example, this call:
        //
        //// workerFactory.GetWorker<MyClass>()
        //
        // would cause the following to be generated (note that the original
        // call still happens and this is generated in a separate class):
        //
        //// [NativeCallable("RegisterWorkerTypes")]
        //// public static void RegisterWorkerTypes()
        //// {
        ////     WorkerFactory.RegisterConstructor<MyClass>(123);
        //// }
        ////
        //// [NativeCallable("CreateMyClass")]
        //// public static object CreateMyClass()
        //// {
        ////     var dependency = new InjectedDependency();
        ////     return new MyClass(dependency);
        //// }
        //
        // where 123 is the method handle for the CreateMyClass method.
        private static readonly TypeSyntax WorkerFactoryType = ParseTypeName("Autocrat.NativeAdapters.WorkerFactory");
        private readonly IReadOnlyCollection<INamedTypeSymbol> factoryTypes;
        private readonly Func<InstanceBuilder> instanceBuilder;
        private readonly List<(TypeSyntax, int)> methodHandles = new List<(TypeSyntax, int)>();
        private readonly NativeImportGenerator nativeGenerator;

        /// <summary>
        /// Initializes a new instance of the <see cref="WorkerRegisterGenerator"/> class.
        /// </summary>
        /// <param name="instanceBuilder">
        /// Factory that creates the builder to generates code to create objects.
        /// </param>
        /// <param name="factoryVisitor">Used to discover the worker types.</param>
        /// <param name="nativeGenerator">Used to register the managed methods.</param>
        public WorkerRegisterGenerator(
            Func<InstanceBuilder> instanceBuilder,
            WorkerFactoryVisitor factoryVisitor,
            NativeImportGenerator nativeGenerator)
            : base("Workers")
        {
            this.factoryTypes = factoryVisitor.WorkerTypes;
            this.instanceBuilder = instanceBuilder;
            this.nativeGenerator = nativeGenerator;
        }

        /// <inheritdoc />
        public override bool HasCode => true;

        /// <inheritdoc />
        protected override IEnumerable<MemberDeclarationSyntax> GetMethods()
        {
            foreach (INamedTypeSymbol type in this.factoryTypes)
            {
                yield return this.AddCreateMethod(type);
            }

            yield return this.CreateRegisterMethod();
        }

        private MemberDeclarationSyntax AddCreateMethod(INamedTypeSymbol type)
        {
            InstanceBuilder builder = this.instanceBuilder();
            IdentifierNameSyntax instance = builder.GenerateForType(type);

            var statements = new List<StatementSyntax>();
            statements.AddRange(builder.LocalDeclarations);
            statements.Add(ReturnStatement(instance));

            string typeName = type.ToDisplayString();
            string methodName = "Create_" + typeName.Replace('.', '_').Replace('+', '_');
            int handle = this.nativeGenerator.RegisterMethod("void* {0}()", methodName);
            this.methodHandles.Add((ParseTypeName(typeName), handle));

            return CreateMethod(
                methodName,
                Block(statements),
                PredefinedType(Token(SyntaxKind.ObjectKeyword)));
        }

        private MemberDeclarationSyntax CreateRegisterMethod()
        {
            static StatementSyntax CallRegister((TypeSyntax type, int handle) value)
            {
                GenericNameSyntax method = GenericName("RegisterConstructor").WithTypeArgumentList(
                    TypeArgumentList(SingletonSeparatedList(value.type)));

                ArgumentSyntax methodHandle = Argument(
                    LiteralExpression(
                        SyntaxKind.NumericLiteralExpression,
                        Literal(value.handle)));

                return ExpressionStatement(
                    InvocationExpression(
                        MemberAccessExpression(
                            SyntaxKind.SimpleMemberAccessExpression,
                            WorkerFactoryType,
                            method),
                        ArgumentList(SingletonSeparatedList(methodHandle))));
            }

            return CreateMethod(
                "RegisterWorkerTypes",
                Block(this.methodHandles.Select(CallRegister).ToArray()));
        }
    }
}
