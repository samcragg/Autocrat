// Copyright (c) Samuel Cragg.
//
// Licensed under the MIT license. See LICENSE file in the project root for
// full license information.

namespace Autocrat.Compiler.CodeRewriting
{
    using System.Collections.Generic;
    using System.Linq;
    using Mono.Cecil;
    using Mono.Cecil.Cil;

    /// <summary>
    /// Allows the rewriting of instructions inside a module.
    /// </summary>
    internal class ModuleRewriter
    {
        private readonly List<CilVisitor> visitors = new List<CilVisitor>();

        /// <summary>
        /// Adds the specified instance to be called when visiting instructions.
        /// </summary>
        /// <param name="visitor">The instance to add.</param>
        public virtual void AddVisitor(CilVisitor visitor)
        {
            this.visitors.Add(visitor);
        }

        /// <summary>
        /// Visits the metadata and instructions in the specified module.
        /// </summary>
        /// <param name="module">Contains the information to visit.</param>
        public virtual void Visit(ModuleDefinition module)
        {
            IEnumerable<MethodDefinition> methods =
                from type in module.Types
                from method in type.Methods
                where method.HasBody
                select method;

            foreach (MethodDefinition method in methods)
            {
                this.VisitInstructions(method.Body);
            }
        }

        private void VisitInstructions(MethodBody body)
        {
            // Create a copy to allow the handlers to modify the body
            Instruction[] instructions = body.Instructions.ToArray();
            foreach (Instruction instruction in instructions)
            {
                foreach (CilVisitor visitor in this.visitors)
                {
                    visitor.Visit(body, instruction);
                }
            }
        }
    }
}
