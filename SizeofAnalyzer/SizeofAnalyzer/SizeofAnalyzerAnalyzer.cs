using System.Collections.Immutable;
using System.Linq;
using System.Runtime.InteropServices;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace SizeofAnalyzer
{
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public class SizeofAnalyzerAnalyzer : DiagnosticAnalyzer
    {
        public const string DiagnosticId = "SizeofAnalyzer";
        private static readonly LocalizableString Title = new LocalizableResourceString(nameof(Resources.AnalyzerTitle), Resources.ResourceManager, typeof(Resources));
        private static readonly LocalizableString MessageFormat = new LocalizableResourceString(nameof(Resources.AnalyzerMessageFormat), Resources.ResourceManager, typeof(Resources));
        private static readonly LocalizableString Description = new LocalizableResourceString(nameof(Resources.AnalyzerDescription), Resources.ResourceManager, typeof(Resources));
        private const string Category = "Performance";

        private static DiagnosticDescriptor Rule = new DiagnosticDescriptor(DiagnosticId, Title, MessageFormat, Category, DiagnosticSeverity.Warning, isEnabledByDefault: true, description: Description);

        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get { return ImmutableArray.Create(Rule); } }

        public override void Initialize(AnalysisContext context)
        {
            context.RegisterSyntaxNodeAction(AnalyzeSymbol, SyntaxKind.InvocationExpression);
        }

        private static void AnalyzeSymbol(SyntaxNodeAnalysisContext context)
        {
            if (context.Compilation.Options is CSharpCompilationOptions cSharpCompilationOptions && !cSharpCompilationOptions.AllowUnsafe)
                return; //Only enabled in unsafe code
            var invocation = context.Node as InvocationExpressionSyntax;
            Marshal.SizeOf<int>();
            var methodSymbol = context.SemanticModel.GetSymbolInfo(invocation.Expression).Symbol as IMethodSymbol;
            if (methodSymbol is null)
                return;
            if (methodSymbol.ContainingType.Name.Equals("Marshal") && methodSymbol.Name.Equals("SizeOf") && methodSymbol.Arity == 1)
            {
                var preDefinedType = ((invocation.Expression as MemberAccessExpressionSyntax).Name as GenericNameSyntax).TypeArgumentList.Arguments.Single() as PredefinedTypeSyntax;
                if (preDefinedType is null)
                    return;//Start out only with known types

                if (preDefinedType.Keyword.IsKind(SyntaxKind.CharKeyword) || preDefinedType.Keyword.IsKind(SyntaxKind.BoolKeyword))
                    return; //Their size differs
                var typeKey = preDefinedType.Keyword.ValueText;
                var diagnostic = Diagnostic.Create(Rule, invocation.GetLocation(), invocation, typeKey);

                context.ReportDiagnostic(diagnostic);
            }
        }
    }
}
