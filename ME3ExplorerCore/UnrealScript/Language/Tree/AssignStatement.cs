﻿using System.Collections.Generic;
using Unrealscript.Analysis.Visitors;
using Unrealscript.Utilities;

namespace Unrealscript.Language.Tree
{
    public class AssignStatement : Statement
    {
        public Expression Target;
        public Expression Value;
        public AssignStatement(Expression target, Expression value,
            SourcePosition start = null, SourcePosition end = null) 
            : base(ASTNodeType.AssignStatement, start, end) 
        {
            Target = target;
            Value = value;
        }

        public override bool AcceptVisitor(IASTVisitor visitor)
        {
            return visitor.VisitNode(this);
        }
        public override IEnumerable<ASTNode> ChildNodes
        {
            get
            {
                yield return Target;
                yield return Value;
            }
        }
    }
}