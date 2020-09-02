using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using TestHelper;

namespace SizeofAnalyzer.Test
{
    [TestClass]
    public class UnitTest : CodeFixVerifier
    {
        [TestMethod]
        public void EmptyText_NoDiagnostic()
        {
            var test = @"";

            VerifyCSharpDiagnostic(test);
        }

        [TestMethod]
        public void MarshalSizeOf_IntParameter_SingleDiagnostic()
        {
            var test = @"
    using System;
    using System.Runtime.InteropServices;

    namespace ConsoleApplication1
    {
        class TypeName
        {   
            unsafe int Test() {return Marshal.SizeOf<int>()};
        }
    }";
            var expected = new DiagnosticResult
            {
                Id = "SizeofAnalyzer",
                Message = String.Format("'{0}' can be simplified to 'sizeof({1})'", "Marshal.SizeOf<int>()", "int"),
                Severity = DiagnosticSeverity.Warning,
                Locations =
                    new[] {
                            new DiagnosticResultLocation("Test0.cs", 9, 39)
                        }
            };

            VerifyCSharpDiagnostic(test, expected);
        }

        [TestMethod]
        public void MarshalSizeOf_Char_Bool_Parameter_NoDiagnostic()
        {
            var testChar = @"
    using System;
    using System.Runtime.InteropServices;

    namespace ConsoleApplication1
    {
        class TypeName
        {   
            unsafe int Test() {return Marshal.SizeOf<char>()};
        }
    }";

            var testBool = @"
    using System;
    using System.Runtime.InteropServices;

    namespace ConsoleApplication1
    {
        class TypeName
        {   
            unsafe int Test() {return Marshal.SizeOf<bool>()};
        }
    }";

            VerifyCSharpDiagnostic(testChar);
            VerifyCSharpDiagnostic(testBool);
        }

        [TestMethod]
        public void MarshalSizeOf_IntParameter_SingleFix()
        {
            var test = @"
    using System;
    using System.Runtime.InteropServices;

    namespace ConsoleApplication1
    {
        class TypeName
        {   
            unsafe int Test() {return Marshal.SizeOf<int>();}
        }
    }";

            var fixtest = @"
    using System;
    using System.Runtime.InteropServices;

    namespace ConsoleApplication1
    {
        class TypeName
        {   
            unsafe int Test() {return sizeof(int); }
        }
    }";
            VerifyCSharpFix(test, fixtest, allowNewCompilerDiagnostics: true);
        }

        protected override CodeFixProvider GetCSharpCodeFixProvider()
        {
            return new SizeofAnalyzerCodeFixProvider();
        }

        protected override DiagnosticAnalyzer GetCSharpDiagnosticAnalyzer()
        {
            return new SizeofAnalyzerAnalyzer();
        }
    }
}
