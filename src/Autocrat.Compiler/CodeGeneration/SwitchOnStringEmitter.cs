// Copyright (c) Samuel Cragg.
//
// Licensed under the MIT license. See LICENSE file in the project root for
// full license information.

namespace Autocrat.Compiler.CodeGeneration
{
    using System;
    using System.Collections.Generic;
    using Autocrat.Abstractions;
    using Mono.Cecil;
    using Mono.Cecil.Cil;

    /// <summary>
    /// Emits IL instructions for switching over string values.
    /// </summary>
    /// <remarks>
    /// For small cases, this will emit simple if checks. However, for larger
    /// numbers of cases it will implement a binary search tree over the hash
    /// of the string, similar to how Roslyn does for C# switch statements.
    /// </remarks>
    internal sealed partial class SwitchOnStringEmitter
    {
        private readonly List<Block> blocks = new List<Block>();
        private readonly Instruction defaultBegin = Instruction.Create(OpCodes.Nop);
        private readonly Instruction endSwitch = Instruction.Create(OpCodes.Nop);
        private readonly ILProcessor processor;
        private readonly MethodReference stringEqualsRef;
        private readonly MethodReference stringGetHashCodeRef;
        private readonly VariableDefinition valueVar;

        /// <summary>
        /// Initializes a new instance of the <see cref="SwitchOnStringEmitter"/> class.
        /// </summary>
        /// <param name="module">The instance containing the module information.</param>
        /// <param name="processor">Where to emit the IL instructions to.</param>
        /// <param name="value">The variable to switch over.</param>
        public SwitchOnStringEmitter(
            ModuleDefinition module,
            ILProcessor processor,
            VariableDefinition value)
        {
            this.processor = processor;
            this.valueVar = value;

            Type valueType = (value.VariableType.MetadataType == MetadataType.String) ?
                typeof(string) :
                typeof(ReadOnlySpan<byte>);

            this.stringEqualsRef = module.ImportReference(
                typeof(CaseInsensitiveStringHelper).GetMethod(
                    nameof(CaseInsensitiveStringHelper.Equals),
                    new[] { typeof(string), valueType }));

            this.stringGetHashCodeRef = module.ImportReference(
                typeof(CaseInsensitiveStringHelper).GetMethod(
                    nameof(CaseInsensitiveStringHelper.GetHashCode),
                    new[] { valueType }));
        }

        /// <summary>
        /// Gets or sets the instructions to emit for the default switch case.
        /// </summary>
        public Action<ILProcessor>? DefaultCase { get; set; }

        /// <summary>
        /// Adds a new switch statement with the specified key and instructions.
        /// </summary>
        /// <param name="key">The key value to switch on.</param>
        /// <param name="code">Emits the instructions for the case.</param>
        public void Add(string key, Action<ILProcessor> code)
        {
            this.blocks.Add(new Block(key.ToUpperInvariant(), code));
        }

        /// <summary>
        /// Emits the instructions to the ILProcessor passed into the
        /// constructor.
        /// </summary>
        public void Emit()
        {
            if (this.blocks.Count < 3)
            {
                this.EmitIfChecks(0, this.blocks.Count);
            }
            else
            {
                this.CalculateHashesAndSortBlocks();
                this.EmitHashChecks(this.EmitGetHashCode(), 0, this.blocks.Count);
            }

            this.EmitDefaultCase();
        }

        private void CalculateHashesAndSortBlocks()
        {
            foreach (Block block in this.blocks)
            {
                block.KeyHash = CaseInsensitiveStringHelper.GetHashCode(block.Key);
            }

            this.blocks.Sort((a, b) => a.KeyHash.CompareTo(b.KeyHash));

            // Merge the blocks with the same hash - since they're now sorted
            // we only have to check their neighbours
            for (int i = this.blocks.Count - 1; i > 0; i--)
            {
                Block current = this.blocks[i];
                Block previous = this.blocks[i - 1];
                if (current.KeyHash == previous.KeyHash)
                {
                    previous.Next = current;
                    this.blocks.RemoveAt(i);
                }
            }
        }

        private Instruction[] CreateIfLabels(int count)
        {
            // If the last condition fails we jump to the default case label
            var ifs = new Instruction[count + 1];
            ifs[count] = this.defaultBegin;
            for (int i = 0; i < count; i++)
            {
                ifs[i] = Instruction.Create(OpCodes.Nop);
            }

            return ifs;
        }

        private void EmitDefaultCase()
        {
            this.processor.Append(this.defaultBegin);
            this.DefaultCase?.Invoke(this.processor);
            this.processor.Append(this.endSwitch);
        }

        private VariableDefinition EmitGetHashCode()
        {
            var hashCodeVariable = new VariableDefinition(
                this.processor.Body.Method.Module.TypeSystem.Int32);

            this.processor.Body.Variables.Add(hashCodeVariable);

            //// int hashCode = StringHelper.GetHashCode(value)
            this.processor.Emit(OpCodes.Ldloc, this.valueVar);
            this.processor.Emit(OpCodes.Call, this.stringGetHashCodeRef);
            this.processor.Emit(OpCodes.Stloc, hashCodeVariable);

            return hashCodeVariable;
        }

        private void EmitHashChecks(VariableDefinition hashCode, int start, int count)
        {
            if (count > 3)
            {
                int midPoint = count / 2;
                var upperHalf = Instruction.Create(OpCodes.Nop);

                //// if (hashCode > midpoint) goto upper half
                this.processor.Emit(OpCodes.Ldloc, hashCode);
                this.processor.Emit(OpCodes.Ldc_I4, this.blocks[start + midPoint - 1].KeyHash);
                this.processor.Emit(OpCodes.Bgt, upperHalf);

                this.EmitHashChecks(hashCode, start, midPoint);
                this.processor.Append(upperHalf);
                this.EmitHashChecks(hashCode, start + midPoint, count - midPoint);
            }
            else
            {
                Instruction[] ifs = this.CreateIfLabels(count);
                for (int i = 0; i < count; i++)
                {
                    //// if (hashCode != KeyHash) goto next if
                    this.processor.Append(ifs[i]);
                    this.processor.Emit(OpCodes.Ldloc, hashCode);
                    this.processor.Emit(OpCodes.Ldc_I4, this.blocks[start + i].KeyHash);
                    this.processor.Emit(OpCodes.Bne_Un, ifs[i + 1]);

                    this.EmitIfChecks(start + i, 1);
                }
            }
        }

        private void EmitIfChecks(int start, int count)
        {
            Instruction[] ifs = this.CreateIfLabels(count);

            for (int i = 0; i < count; i++)
            {
                Block? block = this.blocks[start + i];
                do
                {
                    //// if (!StringHelper.Equals("key", value)) goto next if
                    this.processor.Append(ifs[i]);
                    this.processor.Emit(OpCodes.Ldstr, block.Key);
                    this.processor.Emit(OpCodes.Ldloc, this.valueVar);
                    this.processor.Emit(OpCodes.Call, this.stringEqualsRef);
                    this.processor.Emit(OpCodes.Brfalse, ifs[i + 1]);

                    // Key equals the value - write the code to execute then jump to
                    // the end of the switch block (i.e. simulate the break keyword)
                    block.WriteBlock(this.processor);
                    this.processor.Emit(OpCodes.Br, this.endSwitch);

                    block = block.Next;
                }
                while (block != null);
            }
        }
    }
}
