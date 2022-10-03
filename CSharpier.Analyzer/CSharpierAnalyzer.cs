namespace Insite.Analyzers.Net6Rules
{
    using System.Collections.Immutable;
    using CSharpier;
    using Microsoft.CodeAnalysis;
    using Microsoft.CodeAnalysis.CSharp;
    using Microsoft.CodeAnalysis.CSharp.Syntax;
    using Microsoft.CodeAnalysis.Diagnostics;

    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public class CSharpierAnalyzer : DiagnosticAnalyzer
    {
        public const string DiagnosticId = "CSHARPIER";

        private static readonly string Title = "CSHARPIER!";
        private static readonly string MessageFormat = Title;
        private static readonly string Description = Title;
        private const string Category = "Net6";

        private static readonly DiagnosticDescriptor Rule =
            new(
                DiagnosticId,
                Title,
                MessageFormat,
                Category,
                DiagnosticSeverity.Warning,
                isEnabledByDefault: true,
                description: Description
            );

        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics =>
            ImmutableArray.Create(Rule);

        public override void Initialize(AnalysisContext context)
        {
            context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
            context.EnableConcurrentExecution();

            context.RegisterSyntaxNodeAction(
                this.HandleCompilationUnitExpression,
                SyntaxKind.CompilationUnit
            );
        }

        private void HandleCompilationUnitExpression(SyntaxNodeAnalysisContext context)
        {
            var compilationUnit = context.Node as CompilationUnitSyntax;

            var result = CodeFormatter.Format(compilationUnit.SyntaxTree);
            if (result != compilationUnit.SyntaxTree.GetText().ToString())
            {
                context.ReportDiagnostic(Diagnostic.Create(Rule, context.Node.GetLocation()));
            }
        }
    }
}
