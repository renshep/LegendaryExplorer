﻿using System;
using LegendaryExplorerCore.UnrealScript.Analysis.Visitors;
using LegendaryExplorerCore.UnrealScript.Utilities;

namespace LegendaryExplorerCore.UnrealScript.Language.Tree
{
    public abstract class Expression : ASTNode
    {
        protected Expression(ASTNodeType type, int start, int end) 
            : base(type, start, end) { }

        public override bool AcceptVisitor(IASTVisitor visitor)
        {
            throw new NotImplementedException();
        }

        public abstract VariableType ResolveType();
    }
}
