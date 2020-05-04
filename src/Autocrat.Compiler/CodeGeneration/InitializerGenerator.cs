// Copyright (c) Samuel Cragg.
//
// Licensed under the MIT license. See LICENSE file in the project root for
// full license information.

namespace Autocrat.Compiler.CodeGeneration
{
    using System.Collections.Generic;
    using System.Linq;
    using Autocrat.Abstractions;
    using Microsoft.CodeAnalysis;
    using Microsoft.CodeAnalysis.CSharp;
    using Microsoft.CodeAnalysis.CSharp.Syntax;
    using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;

    /// <summary>
    /// Rewrites code used to initialize the application.
    /// </summary>
    internal class InitializerGenerator : MethodGenerator
    {
        // This class generates a static method to invoke the
        // IInitialize.OnInitialize method on classes, so given this:
        //
        //// public class Startup : IInitializer
        //// {
        ////     private readonly IUdpEvent udpEvent;
        ////
        ////     public Startup(IUdpEvent udpEvent)
        ////     {
        ////         this.updEvent = updEvent;
        ////     }
        ////
        ////     public void OnInitialize()
        ////     {
        ////         int port = 123;
        ////         udpEvent.Register<MyReceiver>(port);
        ////     }
        //// }
        //
        // then a method like this would be generated:
        //
        //// public static void OnInitialize()
        //// {
        ////     var udpEvent = new UdpEvent();
        ////     var startup = new Startup(udpEvent);
        ////     startup.OnInitialize();
        //// }
        //
        // Note that only a single method is generated, so multiple
        // initializers will be created but the order they are invoked in is
        // not specified.
        private readonly InstanceBuilder instanceBuilder;

        private readonly List<(INamedTypeSymbol type, IMethodSymbol method)> methods =
            new List<(INamedTypeSymbol, IMethodSymbol)>();

        /// <summary>
        /// Initializes a new instance of the <see cref="InitializerGenerator"/> class.
        /// </summary>
        /// <param name="instanceBuilder">Generates code to create objects.</param>
        public InitializerGenerator(InstanceBuilder instanceBuilder)
            : base("Initialization")
        {
            this.instanceBuilder = instanceBuilder;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="InitializerGenerator"/> class.
        /// </summary>
        /// <remarks>
        /// This constructor is to make the class easier to be mocked.
        /// </remarks>
        protected InitializerGenerator()
            : base(string.Empty)
        {
            this.instanceBuilder = null!;
        }

        /// <inheritdoc />
        public override bool HasCode => this.methods.Count != 0;

        /// <summary>
        /// Registers the class for invoking during initialization.
        /// </summary>
        /// <param name="type">The type information.</param>
        public virtual void AddClass(INamedTypeSymbol type)
        {
            IMethodSymbol method = type.GetMembers(nameof(IInitializer.OnConfigurationLoaded))
                .OfType<IMethodSymbol>()
                .FirstOrDefault(m => m.Parameters.Length == 0);

            if (method != null)
            {
                this.methods.Add((type, method));
            }
        }

        /// <inheritdoc />
        protected override IEnumerable<MemberDeclarationSyntax> GetMethods()
        {
            var statements = new List<StatementSyntax>(this.methods.Count);
            foreach ((INamedTypeSymbol type, IMethodSymbol method) in this.methods)
            {
                IdentifierNameSyntax instance =
                    this.instanceBuilder.GenerateForType(type);

                statements.Add(
                    ExpressionStatement(
                        InvocationExpression(
                            MemberAccessExpression(
                                SyntaxKind.SimpleMemberAccessExpression,
                                instance,
                                IdentifierName(method.Name)))));
            }

            statements.InsertRange(0, this.instanceBuilder.LocalDeclarations);
            yield return CreateMethod(
                nameof(IInitializer.OnConfigurationLoaded),
                Block(statements));
        }
    }
}
