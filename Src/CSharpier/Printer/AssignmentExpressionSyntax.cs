using CSharpier.DocTypes;
using CSharpier.SyntaxPrinter;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace CSharpier
{
    public partial class Printer
    {
        private Doc PrintAssignmentExpressionSyntax(
            AssignmentExpressionSyntax node
        ) {
            return Docs.Concat(
                this.Print(node.Left),
                " ",
                SyntaxTokens.Print(node.OperatorToken),
                node.Right is QueryExpressionSyntax ? Docs.Null : " ",
                this.Print(node.Right)
            );
        }
    }
}
