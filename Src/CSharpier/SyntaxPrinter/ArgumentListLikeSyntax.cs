using CSharpier.DocTypes;
using CSharpier.SyntaxPrinter.SyntaxNodePrinters;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace CSharpier.SyntaxPrinter
{
    public static class ArgumentListLike
    {
        public static Doc Print(
            SyntaxToken openParenToken,
            SeparatedSyntaxList<ArgumentSyntax> arguments,
            SyntaxToken closeParenToken
        ) =>
            Doc.Concat(
                Token.Print(openParenToken),
                arguments.Any()
                    ? arguments.Count > 1
                            ? Doc.Indent(
                                    Doc.SoftLine,
                                    SeparatedSyntaxList.Print(arguments, Argument.Print, Doc.Line)
                                )
                            : Argument.Print(arguments[0])
                    : Doc.Null,
                arguments.Count > 1 ? Doc.SoftLine : Doc.Null,
                Token.Print(closeParenToken)
            );
    }
}
