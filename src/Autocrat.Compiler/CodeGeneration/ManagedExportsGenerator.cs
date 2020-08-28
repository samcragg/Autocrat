// Copyright (c) Samuel Cragg.
//
// Licensed under the MIT license. See LICENSE file in the project root for
// full license information.

namespace Autocrat.Compiler.CodeGeneration
{
    using System.Collections.Generic;
    using Microsoft.CodeAnalysis.CSharp;
    using Microsoft.CodeAnalysis.CSharp.Syntax;
    using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;

    /// <summary>
    /// Builds a method to register the generated managed types to the native
    /// code (such as the worker and configuration classes).
    /// </summary>
    internal class ManagedExportsGenerator : MethodGenerator
    {
        /// <summary>
        /// Represents the name of the generated class.
        /// </summary>
        internal const string GeneratedClassName = "ManagedTypes";

        /// <summary>
        /// Represents the name of the generated method.
        /// </summary>
        internal const string GeneratedMethodName = "RegisterManagedTypes";

        // This class creates a hook for initializing the managed code and for
        // registering types with the native code. It is called at startup from
        // the main thread before any configuration is loaded. This allows us
        // to register the worker class constructors and the generated
        // configuration classes before any other code is called.
        private readonly TypeSyntax configServiceType = ParseTypeName(
            "Autocrat.NativeAdapters." + nameof(NativeAdapters.ConfigService));

        /// <summary>
        /// Initializes a new instance of the <see cref="ManagedExportsGenerator"/> class.
        /// </summary>
        public ManagedExportsGenerator()
            : base(GeneratedClassName)
        {
        }

        /// <inheritdoc />
        public override bool HasCode => true;

        /// <summary>
        /// Gets or sets a value indicating whether to include configuration
        /// registration code or not.
        /// </summary>
        public virtual bool IncludeConfig { get; set; }

        /// <inheritdoc />
        protected override IEnumerable<MemberDeclarationSyntax> GetMethods()
        {
            yield return this.CreateRegisterMethod();
        }

        private MemberDeclarationSyntax CreateRegisterMethod()
        {
            var expressions = new List<ExpressionStatementSyntax>();
            if (this.IncludeConfig)
            {
                expressions.Add(this.RegisterConfiguration());
            }

            expressions.Add(ExpressionStatement(
                InvocationExpression(RoslynHelper.AccessMember(
                    WorkerRegisterGenerator.GeneratedClassName,
                    WorkerRegisterGenerator.GeneratedMethodName))));

            return CreateMethod(
                GeneratedMethodName,
                Block(expressions));
        }

        private ExpressionStatementSyntax RegisterConfiguration()
        {
            return ExpressionStatement(
                InvocationExpression(
                    MemberAccessExpression(
                        SyntaxKind.SimpleMemberAccessExpression,
                        this.configServiceType,
                        IdentifierName(nameof(NativeAdapters.ConfigService.Initialize))),
                    ArgumentList(SingletonSeparatedList(Argument(RoslynHelper.AccessMember(
                        ConfigResolver.ConfigurationClassName,
                        ConfigResolver.ReadConfigurationMethod))))));
        }
    }
}
