// Copyright (c) Samuel Cragg.
//
// Licensed under the MIT license. See LICENSE file in the project root for
// full license information.

namespace Autocrat.Transform.Managed.CodeGeneration
{
    using System;
    using Mono.Cecil.Cil;

    /// <content>
    /// Contains the nested <see cref="Block"/> class.
    /// </content>
    internal sealed partial class SwitchOnStringEmitter
    {
        private class Block
        {
            public Block(string key, Action<ILProcessor> writeBlock)
            {
                this.Key = key;
                this.WriteBlock = writeBlock;
            }

            public string Key { get; }

            public int KeyHash { get; set; }

            public Block? Next { get; set; }

            public Action<ILProcessor> WriteBlock { get; }
        }
    }
}
