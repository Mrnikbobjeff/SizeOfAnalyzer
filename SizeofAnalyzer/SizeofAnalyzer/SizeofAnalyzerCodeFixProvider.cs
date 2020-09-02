using System.Collections.Immutable;
using System.Composition;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace SizeofAnalyzer
{
    [ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(SizeofAnalyzerCodeFixProvider)), Shared]
    public class SizeofAnalyzerCodeFixProvider : CodeFixProvider
    {
        private const string title = "Use sizeof()";

        public sealed override ImmutableArray<string> FixableDiagnosticIds
        {
            get { return ImmutableArray.Create(SizeofAnalyzerAnalyzer.DiagnosticId); }
        }

        public sealed override FixAllProvider GetFixAllProvider()
        {
            return WellKnownFixAllProviders.BatchFixer;
        }

        public sealed override async Task RegisterCodeFixesAsync(CodeFixContext context)
        {
            var root = await context.Document.GetSyntaxRootAsync(context.CancellationToken).ConfigureAwait(false);

            foreach (var diagnostic in context.Diagnostics)
            {
                var diagnosticSpan = diagnostic.Location.SourceSpan;

                var declaration = root.FindToken(diagnosticSpan.Start).Parent.AncestorsAndSelf().OfType<InvocationExpressionSyntax>().First();

                context.RegisterCodeFix(
                    CodeAction.Create(
                        title: title,
                        createChangedSolution: c => UseSizeOfT(context.Document, declaration, c),
                        equivalenceKey: title),
                    diagnostic);
            }
        }

        private async Task<Solution> UseSizeOfT(Document document, InvocationExpressionSyntax marshalInvocation, CancellationToken cancellationToken)
        {
            var sizeofSyntax = SyntaxFactory.SizeOfExpression(((marshalInvocation.Expression as MemberAccessExpressionSyntax).Name as GenericNameSyntax).TypeArgumentList.Arguments.Single());
            
            var documentRoot = await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
            var newRoot = documentRoot.ReplaceNode(marshalInvocation, sizeofSyntax.WithoutTrailingTrivia());
            return document.Project.Solution.WithDocumentSyntaxRoot(document.Id, newRoot);
        }
    }
}
