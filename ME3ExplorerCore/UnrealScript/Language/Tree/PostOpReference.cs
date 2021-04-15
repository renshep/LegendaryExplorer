﻿using System.Collections.Generic;
using Unrealscript.Analysis.Visitors;
using Unrealscript.Utilities;

namespace Unrealscript.Language.Tree
{
    public class PostOpReference : Expression
    {
        public PostOpDeclaration Operator;
        public Expression Operand;

        public PostOpReference(PostOpDeclaration op, Expression oper, SourcePosition start, SourcePosition end)
            : base(ASTNodeType.InOpRef, start, end)
        {
            Operator = op;
            Operand = oper;
        }

        public override bool AcceptVisitor(IASTVisitor visitor)
        {
            return visitor.VisitNode(this);
        }

        public override VariableType ResolveType()
        {
            return Operator.ReturnType;
        }
        public override IEnumerable<ASTNode> ChildNodes
        {
            get
            {
                yield return Operand;
            }
        }
    }
}