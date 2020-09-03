﻿using ME3Script.Analysis.Visitors;
using ME3Script.Utilities;
using System.Collections.Generic;

namespace ME3Script.Language.Tree
{
    public class ExpressionOnlyStatement : Statement
    {
        public Expression Value;

        public ExpressionOnlyStatement(Expression value, SourcePosition start = null, SourcePosition end = null)
            : base(ASTNodeType.ExpressionStatement, start, end)
        {
            Value = value;
        }

        public override bool AcceptVisitor(IASTVisitor visitor)
        {
            return visitor.VisitNode(this);
        }
        public override IEnumerable<ASTNode> ChildNodes
        {
            get { yield return Value; }
        }
    }
}
