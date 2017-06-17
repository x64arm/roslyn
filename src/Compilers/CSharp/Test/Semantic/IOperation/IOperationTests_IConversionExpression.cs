﻿using System;
using System.Linq;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.CSharp.Test.Utilities;
using Microsoft.CodeAnalysis.Semantics;
using Microsoft.CodeAnalysis.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.CSharp.UnitTests
{
    // Test list drawn from Microsoft.CodeAnalysis.CSharp.ConversionKind
    public partial class IOperationTests : SemanticModelTestBase
    {
        #region Implicit Conversions

        [Fact]
        public void ConversionExpression_Implicit_IdentityConversionDynamic()
        {
            string source = @"
class Program
{
    static void Main(string[] args)
    {
        object o1 = new object();
        dynamic /*<bind>*/d1 = o1/*</bind>*/;
    }
}
";
            string expectedOperationTree = @"
IVariableDeclarationStatement (1 declarations) (OperationKind.VariableDeclarationStatement) (Syntax: 'dynamic /*< ... *</bind>*/;')
  IVariableDeclaration (1 variables) (OperationKind.VariableDeclaration) (Syntax: 'dynamic /*< ... *</bind>*/;')
    Variables: Local_1: dynamic d1
    Initializer: IConversionExpression (ConversionKind.Cast, Implicit) (OperationKind.ConversionExpression, Type: dynamic) (Syntax: 'o1')
        ILocalReferenceExpression: o1 (OperationKind.LocalReferenceExpression, Type: System.Object) (Syntax: 'o1')
";
            var expectedDiagnostics = DiagnosticDescription.None;

            VerifyOperationTreeAndDiagnosticsForTest<VariableDeclaratorSyntax>(source, expectedOperationTree, expectedDiagnostics,
                AdditionalOperationTreeVerifier: new ExpectedSymbolVerifier().Verify);
        }

        /// <summary>
        /// This test documents the fact that there is no IConversionExpression between two objects of the same type.
        /// </summary>
        [Fact]
        public void ConversionExpression_Implicit_IdentityConversion()
        {
            string source = @"
class Program
{
    static void Main(string[] args)
    {
        object o1 = new object();
        object /*<bind>*/o2 = o1/*</bind>*/;
    }
}
";
            string expectedOperationTree = @"
IVariableDeclarationStatement (1 declarations) (OperationKind.VariableDeclarationStatement) (Syntax: 'object /*<b ... *</bind>*/;')
  IVariableDeclaration (1 variables) (OperationKind.VariableDeclaration) (Syntax: 'object /*<b ... *</bind>*/;')
    Variables: Local_1: System.Object o2
    Initializer: ILocalReferenceExpression: o1 (OperationKind.LocalReferenceExpression, Type: System.Object) (Syntax: 'o1')
";
            var expectedDiagnostics = DiagnosticDescription.None;

            VerifyOperationTreeAndDiagnosticsForTest<VariableDeclaratorSyntax>(source, expectedOperationTree, expectedDiagnostics);
        }

        [Fact]
        public void ConversionExpression_Implicit_NumericConversion_Valid()
        {
            string source = @"
class Program
{
    static void Main(string[] args)
    {
        float f1 = 1.0f;
        double /*<bind>*/d1 = f1/*</bind>*/;
    }
}
";
            string expectedOperationTree = @"
IVariableDeclarationStatement (1 declarations) (OperationKind.VariableDeclarationStatement) (Syntax: 'double /*<b ... *</bind>*/;')
  IVariableDeclaration (1 variables) (OperationKind.VariableDeclaration) (Syntax: 'double /*<b ... *</bind>*/;')
    Variables: Local_1: System.Double d1
    Initializer: IConversionExpression (ConversionKind.CSharp, Implicit) (OperationKind.ConversionExpression, Type: System.Double) (Syntax: 'f1')
        ILocalReferenceExpression: f1 (OperationKind.LocalReferenceExpression, Type: System.Single) (Syntax: 'f1')
";
            var expectedDiagnostics = DiagnosticDescription.None;

            VerifyOperationTreeAndDiagnosticsForTest<VariableDeclaratorSyntax>(source, expectedOperationTree, expectedDiagnostics,
                AdditionalOperationTreeVerifier: new ExpectedSymbolVerifier().Verify);
        }

        [Fact]
        public void ConversionExpression_Implicit_NumericConversion_InvalidIllegalTypes()
        {
            string source = @"
class Program
{
    static void Main(string[] args)
    {
        float f1 = 1.0f;
        int /*<bind>*/i1 = f1/*</bind>*/;
    }
}
";
            string expectedOperationTree = @"
IVariableDeclarationStatement (1 declarations) (OperationKind.VariableDeclarationStatement, IsInvalid) (Syntax: 'int /*<bind ... *</bind>*/;')
  IVariableDeclaration (1 variables) (OperationKind.VariableDeclaration, IsInvalid) (Syntax: 'int /*<bind ... *</bind>*/;')
    Variables: Local_1: System.Int32 i1
    Initializer: IConversionExpression (ConversionKind.CSharp, Implicit) (OperationKind.ConversionExpression, Type: System.Int32, IsInvalid) (Syntax: 'f1')
        ILocalReferenceExpression: f1 (OperationKind.LocalReferenceExpression, Type: System.Single) (Syntax: 'f1')
";
            var expectedDiagnostics = new DiagnosticDescription[] {
                // CS0266: Cannot implicitly convert type 'float' to 'int'. An explicit conversion exists (are you missing a cast?)
                //         int /*<bind>*/i1 = f1/*</bind>*/;
                Diagnostic(ErrorCode.ERR_NoImplicitConvCast, "f1").WithArguments("float", "int").WithLocation(7, 28)
            };

            VerifyOperationTreeAndDiagnosticsForTest<VariableDeclaratorSyntax>(source, expectedOperationTree, expectedDiagnostics,
                AdditionalOperationTreeVerifier: new ExpectedSymbolVerifier().Verify);
        }

        [Fact(Skip = "https://github.com/dotnet/roslyn/issues/20175")]
        public void ConversionExpression_Implicit_NumericConversion_InvalidNoInitializer()
        {
            string source = @"
using System;

class Program
{
    static void Main(string[] args)
    {
        int /*<bind>*/i1 =/*</bind>*/;
    }
}
";
            string expectedOperationTree = @"
IVariableDeclarationStatement (1 declarations) (OperationKind.VariableDeclarationStatement, IsInvalid) (Syntax: 'int /*<bind ... *</bind>*/;')
  IVariableDeclaration (1 variables) (OperationKind.VariableDeclaration, IsInvalid) (Syntax: 'int /*<bind ... *</bind>*/;')
    Variables: Local_1: System.Int32 i1
    Initializer: IConversionExpression (ConversionKind.Invalid, Implicit) (OperationKind.ConversionExpression, Type: System.Int32, IsInvalid) (Syntax: '')
        IInvalidExpression (OperationKind.InvalidExpression, Type: ?, IsInvalid) (Syntax: '')
";
            var expectedDiagnostics = new DiagnosticDescription[] {
                // CS1525: Invalid expression term ';'
                //         int /*<bind>*/i1 =/*</bind>*/;
                Diagnostic(ErrorCode.ERR_InvalidExprTerm, ";").WithArguments(";").WithLocation(8, 38)
            };

            VerifyOperationTreeAndDiagnosticsForTest<VariableDeclaratorSyntax>(source, expectedOperationTree, expectedDiagnostics,
                AdditionalOperationTreeVerifier: new ExpectedSymbolVerifier().Verify);
        }

        [Fact]
        public void ConversionExpression_Implicit_EnumConversion_ZeroToEnum()
        {
            string source = @"
class Program
{    static void Main(string[] args)
    {
        Enum1 /*<bind>*/e1 = 0/*</bind>*/;
    }
}
enum Enum1
{
    Option1, Option2
}
";
            string expectedOperationTree = @"
IVariableDeclarationStatement (1 declarations) (OperationKind.VariableDeclarationStatement) (Syntax: 'Enum1 /*<bi ... *</bind>*/;')
  IVariableDeclaration (1 variables) (OperationKind.VariableDeclaration) (Syntax: 'Enum1 /*<bi ... *</bind>*/;')
    Variables: Local_1: Enum1 e1
    Initializer: IConversionExpression (ConversionKind.CSharp, Implicit) (OperationKind.ConversionExpression, Type: Enum1, Constant: 0) (Syntax: '0')
        ILiteralExpression (Text: 0) (OperationKind.LiteralExpression, Type: System.Int32, Constant: 0) (Syntax: '0')
";
            var expectedDiagnostics = new DiagnosticDescription[] {
                // CS0219: The variable 'e1' is assigned but its value is never used
                //         Enum1 /*<bind>*/e1 = 0/*</bind>*/;
                Diagnostic(ErrorCode.WRN_UnreferencedVarAssg, "e1").WithArguments("e1").WithLocation(5, 25)
            };

            VerifyOperationTreeAndDiagnosticsForTest<VariableDeclaratorSyntax>(source, expectedOperationTree, expectedDiagnostics,
                AdditionalOperationTreeVerifier: new ExpectedSymbolVerifier().Verify);
        }

        [Fact]
        public void ConversionExpression_Implicit_EnumConversion_IntToEnum_Invalid()
        {
            string source = @"
class Program
{
    static void Main(string[] args)
    {
        int i1 = 1;
        Enum1 /*<bind>*/e1 = i1/*</bind>*/;
    }
}
enum Enum1
{
    Option1, Option2
}
";
            string expectedOperationTree = @"
IVariableDeclarationStatement (1 declarations) (OperationKind.VariableDeclarationStatement, IsInvalid) (Syntax: 'Enum1 /*<bi ... *</bind>*/;')
  IVariableDeclaration (1 variables) (OperationKind.VariableDeclaration, IsInvalid) (Syntax: 'Enum1 /*<bi ... *</bind>*/;')
    Variables: Local_1: Enum1 e1
    Initializer: IConversionExpression (ConversionKind.CSharp, Implicit) (OperationKind.ConversionExpression, Type: Enum1, IsInvalid) (Syntax: 'i1')
        ILocalReferenceExpression: i1 (OperationKind.LocalReferenceExpression, Type: System.Int32) (Syntax: 'i1')
";
            var expectedDiagnostics = new DiagnosticDescription[] {
                // CS0266: Cannot implicitly convert type 'int' to 'Program.Enum1'. An explicit conversion exists (are you missing a cast?)
                //         Enum1 /*<bind>*/e1 = i1/*</bind>*/;
                Diagnostic(ErrorCode.ERR_NoImplicitConvCast, "i1").WithArguments("int", "Enum1").WithLocation(7, 30)
            };

            VerifyOperationTreeAndDiagnosticsForTest<VariableDeclaratorSyntax>(source, expectedOperationTree, expectedDiagnostics,
                AdditionalOperationTreeVerifier: new ExpectedSymbolVerifier().Verify);
        }

        [Fact]
        public void ConversionExpression_Implicit_EnumConversion_OneToEnum_Invalid()
        {
            string source = @"
class Program
{    static void Main(string[] args)
    {
        Enum1 /*<bind>*/e1 = 1/*</bind>*/;
    }
}
enum Enum1
{
    Option1, Option2
}
";
            string expectedOperationTree = @"
IVariableDeclarationStatement (1 declarations) (OperationKind.VariableDeclarationStatement, IsInvalid) (Syntax: 'Enum1 /*<bi ... *</bind>*/;')
  IVariableDeclaration (1 variables) (OperationKind.VariableDeclaration, IsInvalid) (Syntax: 'Enum1 /*<bi ... *</bind>*/;')
    Variables: Local_1: Enum1 e1
    Initializer: IConversionExpression (ConversionKind.CSharp, Implicit) (OperationKind.ConversionExpression, Type: Enum1, IsInvalid) (Syntax: '1')
        ILiteralExpression (Text: 1) (OperationKind.LiteralExpression, Type: System.Int32, Constant: 1) (Syntax: '1')
";
            var expectedDiagnostics = new DiagnosticDescription[] {
                // CS0266: Cannot implicitly convert type 'int' to 'Program.Enum1'. An explicit conversion exists (are you missing a cast?)
                //         Enum1 /*<bind>*/e1 = 1/*</bind>*/;
                Diagnostic(ErrorCode.ERR_NoImplicitConvCast, "1").WithArguments("int", "Enum1").WithLocation(5, 30)
            };

            VerifyOperationTreeAndDiagnosticsForTest<VariableDeclaratorSyntax>(source, expectedOperationTree, expectedDiagnostics,
                AdditionalOperationTreeVerifier: new ExpectedSymbolVerifier().Verify);
        }

        [Fact(Skip = "https://github.com/dotnet/roslyn/issues/20175")]
        public void ConversionExpression_Implicit_EnumConversion_NoInitalizer_Invalid()
        {
            string source = @"
class Program
{
    static void Main(string[] args)
    {
        Enum1 /*<bind>*/e1 =/*</bind>*/;
    }
}
enum Enum1
{
    Option1, Option2
}
";
            string expectedOperationTree = @"
IVariableDeclarationStatement (1 declarations) (OperationKind.VariableDeclarationStatement, IsInvalid) (Syntax: 'Enum1 /*<bi ... *</bind>*/;')
  IVariableDeclaration (1 variables) (OperationKind.VariableDeclaration, IsInvalid) (Syntax: 'Enum1 /*<bi ... *</bind>*/;')
    Variables: Local_1: Enum1 e1
    Initializer: IConversionExpression (ConversionKind.Invalid, Implicit) (OperationKind.ConversionExpression, Type: Enum1, IsInvalid) (Syntax: '')
        IInvalidExpression (OperationKind.InvalidExpression, Type: ?, IsInvalid) (Syntax: '')
";
            var expectedDiagnostics = new DiagnosticDescription[] {
                // CS1525: Invalid expression term ';'
                //         Enum1 /*<bind>*/e1 =/*</bind>*/;
                Diagnostic(ErrorCode.ERR_InvalidExprTerm, ";").WithArguments(";").WithLocation(6, 40)
            };

            VerifyOperationTreeAndDiagnosticsForTest<VariableDeclaratorSyntax>(source, expectedOperationTree, expectedDiagnostics,
                AdditionalOperationTreeVerifier: new ExpectedSymbolVerifier().Verify);
        }

        [Fact]
        public void ConversionExpression_Implicit_ThrowExpressionConversion()
        {
            string source = @"
using System;

class Program
{
    static void Main(string[] args)
    {
        object /*<bind>*/o = new object() ?? throw new Exception()/*</bind>*/;
    }
}
";
            string expectedOperationTree = @"
IVariableDeclarationStatement (1 declarations) (OperationKind.VariableDeclarationStatement) (Syntax: 'object /*<b ... *</bind>*/;')
  IVariableDeclaration (1 variables) (OperationKind.VariableDeclaration) (Syntax: 'object /*<b ... *</bind>*/;')
    Variables: Local_1: System.Object o
    Initializer: INullCoalescingExpression (OperationKind.NullCoalescingExpression, Type: System.Object) (Syntax: 'new object( ... Exception()')
        Left: IObjectCreationExpression (Constructor: System.Object..ctor()) (OperationKind.ObjectCreationExpression, Type: System.Object) (Syntax: 'new object()')
        Right: IConversionExpression (ConversionKind.CSharp, Implicit) (OperationKind.ConversionExpression, Type: System.Object) (Syntax: 'throw new Exception()')
            IOperation:  (OperationKind.None) (Syntax: 'throw new Exception()')
              Children(1): IObjectCreationExpression (Constructor: System.Exception..ctor()) (OperationKind.ObjectCreationExpression, Type: System.Exception) (Syntax: 'new Exception()')
";
            var expectedDiagnostics = DiagnosticDescription.None;

            VerifyOperationTreeAndDiagnosticsForTest<VariableDeclaratorSyntax>(source, expectedOperationTree, expectedDiagnostics,
                AdditionalOperationTreeVerifier: new ExpectedSymbolVerifier()
                {
                    SyntaxSelector = (syntax) =>
                    {
                        var initializer = (BinaryExpressionSyntax)((VariableDeclaratorSyntax)syntax).Initializer.Value;
                        return initializer.Right;
                    },
                    OperationSelector = (operation) =>
                    {
                        var initializer = ((IVariableDeclarationStatement)operation).Declarations.Single().Initializer;
                        return (IConversionExpression)((INullCoalescingExpression)initializer).SecondaryOperand;
                    }
                }.Verify);
        }

        [Fact(Skip = "https://github.com/dotnet/roslyn/issues/20175")]
        public void ConversionExpression_Implicit_ThrowExpressionConversion_InvalidSyntax()
        {
            string source = @"
using System;

class Program
{
    static void Main(string[] args)
    {
        object /*<bind>*/o = throw new Exception()/*</bind>*/;
    }
}
";
            string expectedOperationTree = @"
IVariableDeclarationStatement (1 declarations) (OperationKind.VariableDeclarationStatement, IsInvalid) (Syntax: 'object /*<b ... *</bind>*/;')
  IVariableDeclaration (1 variables) (OperationKind.VariableDeclaration, IsInvalid) (Syntax: 'object /*<b ... *</bind>*/;')
    Variables: Local_1: System.Object o
    Initializer: IConversionExpression (ConversionKind.Invalid, Implicit) (OperationKind.ConversionExpression, Type: System.Object, IsInvalid) (Syntax: 'throw new Exception()')
        IInvalidExpression (OperationKind.InvalidExpression, Type: ?, IsInvalid) (Syntax: 'throw new Exception()')
          Children(1): IOperation:  (OperationKind.None, IsInvalid) (Syntax: 'throw new Exception()')
              Children(1): IObjectCreationExpression (Constructor: System.Exception..ctor()) (OperationKind.ObjectCreationExpression, Type: System.Exception) (Syntax: 'new Exception()')
";
            var expectedDiagnostics = new DiagnosticDescription[] {
                // CS8115: A throw expression is not allowed in this context.
                //         object /*<bind>*/o = throw new Exception()/*</bind>*/;
                Diagnostic(ErrorCode.ERR_ThrowMisplaced, "throw").WithLocation(8, 30)
            };

            VerifyOperationTreeAndDiagnosticsForTest<VariableDeclaratorSyntax>(source, expectedOperationTree, expectedDiagnostics,
                AdditionalOperationTreeVerifier: new ExpectedSymbolVerifier().Verify);
        }

        [Fact]
        public void ConversionExpression_Implicit_NullToClassConversion()
        {
            string source = @"
class Program
{
    static void Main(string[] args)
    {
        string /*<bind>*/s1 = null/*</bind>*/;
    }
}
";
            string expectedOperationTree = @"
IVariableDeclarationStatement (1 declarations) (OperationKind.VariableDeclarationStatement) (Syntax: 'string /*<b ... *</bind>*/;')
  IVariableDeclaration (1 variables) (OperationKind.VariableDeclaration) (Syntax: 'string /*<b ... *</bind>*/;')
    Variables: Local_1: System.String s1
    Initializer: IConversionExpression (ConversionKind.Cast, Implicit) (OperationKind.ConversionExpression, Type: System.String, Constant: null) (Syntax: 'null')
        ILiteralExpression (Text: null) (OperationKind.LiteralExpression, Type: null, Constant: null) (Syntax: 'null')
";
            var expectedDiagnostics = new DiagnosticDescription[] {
                // CS0219: The variable 's1' is assigned but its value is never used
                //         string /*<bind>*/s1 = null/*</bind>*/;
                Diagnostic(ErrorCode.WRN_UnreferencedVarAssg, "s1").WithArguments("s1").WithLocation(6, 26)
            };

            VerifyOperationTreeAndDiagnosticsForTest<VariableDeclaratorSyntax>(source, expectedOperationTree, expectedDiagnostics,
                AdditionalOperationTreeVerifier: new ExpectedSymbolVerifier().Verify);
        }

        [Fact]
        public void ConversionExpression_Implicit_NullToNullableValueConversion()
        {
            string source = @"
interface I1
{
}

struct S1
{
    void M1()
    {
        S1? /*<bind>*/s1 = null/*</bind>*/;
    }
}
";
            string expectedOperationTree = @"
IVariableDeclarationStatement (1 declarations) (OperationKind.VariableDeclarationStatement) (Syntax: 'S1? /*<bind ... *</bind>*/;')
  IVariableDeclaration (1 variables) (OperationKind.VariableDeclaration) (Syntax: 'S1? /*<bind ... *</bind>*/;')
    Variables: Local_1: S1? s1
    Initializer: IConversionExpression (ConversionKind.CSharp, Implicit) (OperationKind.ConversionExpression, Type: S1?, Constant: null) (Syntax: 'null')
        ILiteralExpression (Text: null) (OperationKind.LiteralExpression, Type: null, Constant: null) (Syntax: 'null')
";
            var expectedDiagnostics = new DiagnosticDescription[] {
                // CS0219: The variable 's1' is assigned but its value is never used
                //         S1? /*<bind>*/s1 = null/*</bind>*/;
                Diagnostic(ErrorCode.WRN_UnreferencedVarAssg, "s1").WithArguments("s1").WithLocation(10, 23)
            };

            VerifyOperationTreeAndDiagnosticsForTest<VariableDeclaratorSyntax>(source, expectedOperationTree, expectedDiagnostics,
                AdditionalOperationTreeVerifier: new ExpectedSymbolVerifier().Verify);
        }

        [Fact]
        public void ConversionExpression_Implicit_NullToNonNullableConversion_Invalid()
        {
            string source = @"
class Program
{
    static void Main(string[] args)
    {
        int /*<bind>*/i1 = null/*</bind>*/;
    }
}
";
            string expectedOperationTree = @"
IVariableDeclarationStatement (1 declarations) (OperationKind.VariableDeclarationStatement, IsInvalid) (Syntax: 'int /*<bind ... *</bind>*/;')
  IVariableDeclaration (1 variables) (OperationKind.VariableDeclaration, IsInvalid) (Syntax: 'int /*<bind ... *</bind>*/;')
    Variables: Local_1: System.Int32 i1
    Initializer: IConversionExpression (ConversionKind.Invalid, Implicit) (OperationKind.ConversionExpression, Type: System.Int32, IsInvalid) (Syntax: 'null')
        ILiteralExpression (Text: null) (OperationKind.LiteralExpression, Type: null, Constant: null) (Syntax: 'null')
";
            var expectedDiagnostics = new DiagnosticDescription[] {
                // CS0037: Cannot convert null to 'int' because it is a non-nullable value type
                //         int /*<bind>*/i1 = null/*</bind>*/;
                Diagnostic(ErrorCode.ERR_ValueCantBeNull, "null").WithArguments("int").WithLocation(6, 28)
            };

            VerifyOperationTreeAndDiagnosticsForTest<VariableDeclaratorSyntax>(source, expectedOperationTree, expectedDiagnostics,
                AdditionalOperationTreeVerifier: new ExpectedSymbolVerifier().Verify);
        }

        [Fact]
        public void ConversionExpression_Implicit_DefaultToValueConversion()
        {
            string source = @"
using System;

class S1
{
    void M1()
    {
        long /*<bind>*/i1 = default(int)/*</bind>*/;
    }
}
";
            string expectedOperationTree = @"
IVariableDeclarationStatement (1 declarations) (OperationKind.VariableDeclarationStatement) (Syntax: 'long /*<bin ... *</bind>*/;')
  IVariableDeclaration (1 variables) (OperationKind.VariableDeclaration) (Syntax: 'long /*<bin ... *</bind>*/;')
    Variables: Local_1: System.Int64 i1
    Initializer: IConversionExpression (ConversionKind.CSharp, Implicit) (OperationKind.ConversionExpression, Type: System.Int64, Constant: 0) (Syntax: 'default(int)')
        IDefaultValueExpression (OperationKind.DefaultValueExpression, Type: System.Int32, Constant: 0) (Syntax: 'default(int)')
";
            var expectedDiagnostics = new DiagnosticDescription[] {
                // CS0219: The variable 'i1' is assigned but its value is never used
                //         long /*<bind>*/i1 = default(int)/*</bind>*/;
                Diagnostic(ErrorCode.WRN_UnreferencedVarAssg, "i1").WithArguments("i1").WithLocation(8, 24)
            };

            VerifyOperationTreeAndDiagnosticsForTest<VariableDeclaratorSyntax>(source, expectedOperationTree, expectedDiagnostics,
                AdditionalOperationTreeVerifier: new ExpectedSymbolVerifier().Verify);
        }

        /// <summary>
        /// This test documents the fact that `default(T)` is already T, and does not introduce a conversion
        /// </summary>
        [Fact]
        public void ConversionExpression_Implicit_DefaultToClassNoConversion()
        {
            string source = @"
using System;

class S1
{
    void M1()
    {
        string /*<bind>*/i1 = default(string)/*</bind>*/;
    }
}
";
            string expectedOperationTree = @"
IVariableDeclarationStatement (1 declarations) (OperationKind.VariableDeclarationStatement) (Syntax: 'string /*<b ... *</bind>*/;')
  IVariableDeclaration (1 variables) (OperationKind.VariableDeclaration) (Syntax: 'string /*<b ... *</bind>*/;')
    Variables: Local_1: System.String i1
    Initializer: IDefaultValueExpression (OperationKind.DefaultValueExpression, Type: System.String, Constant: null) (Syntax: 'default(string)')
";
            var expectedDiagnostics = new DiagnosticDescription[] {
                // CS0219: The variable 'i1' is assigned but its value is never used
                //         string /*<bind>*/i1 = default(string)/*</bind>*/;
                Diagnostic(ErrorCode.WRN_UnreferencedVarAssg, "i1").WithArguments("i1").WithLocation(8, 26)
            };

            VerifyOperationTreeAndDiagnosticsForTest<VariableDeclaratorSyntax>(source, expectedOperationTree, expectedDiagnostics);
        }

        [Fact]
        public void ConversionExpression_Implicit_NullableFromConstantConversion()
        {
            string source = @"
class Program
{
    static void Main(string[] args)
    {
        int? /*<bind>*/i1 = 1/*</bind>*/;
    }
}
";
            string expectedOperationTree = @"
IVariableDeclarationStatement (1 declarations) (OperationKind.VariableDeclarationStatement) (Syntax: 'int? /*<bin ... *</bind>*/;')
  IVariableDeclaration (1 variables) (OperationKind.VariableDeclaration) (Syntax: 'int? /*<bin ... *</bind>*/;')
    Variables: Local_1: System.Int32? i1
    Initializer: IConversionExpression (ConversionKind.CSharp, Implicit) (OperationKind.ConversionExpression, Type: System.Int32?) (Syntax: '1')
        ILiteralExpression (Text: 1) (OperationKind.LiteralExpression, Type: System.Int32, Constant: 1) (Syntax: '1')
";
            var expectedDiagnostics = new DiagnosticDescription[] {
                // CS0219: The variable 'i1' is assigned but its value is never used
                //         int? /*<bind>*/i1 = 1/*</bind>*/;
                Diagnostic(ErrorCode.WRN_UnreferencedVarAssg, "i1").WithArguments("i1").WithLocation(6, 24)
            };

            VerifyOperationTreeAndDiagnosticsForTest<VariableDeclaratorSyntax>(source, expectedOperationTree, expectedDiagnostics,
                AdditionalOperationTreeVerifier: new ExpectedSymbolVerifier().Verify);
        }

        [Fact]
        public void ConversionExpression_Implicit_NullableToNullableConversion()
        {
            string source = @"
class Program
{
    static void Main(string[] args)
    {
        int? i1 = 1;
        long? /*<bind>*/l1 = i1/*</bind>*/;
    }
}
";
            string expectedOperationTree = @"
IVariableDeclarationStatement (1 declarations) (OperationKind.VariableDeclarationStatement) (Syntax: 'long? /*<bi ... *</bind>*/;')
  IVariableDeclaration (1 variables) (OperationKind.VariableDeclaration) (Syntax: 'long? /*<bi ... *</bind>*/;')
    Variables: Local_1: System.Int64? l1
    Initializer: IConversionExpression (ConversionKind.CSharp, Implicit) (OperationKind.ConversionExpression, Type: System.Int64?) (Syntax: 'i1')
        ILocalReferenceExpression: i1 (OperationKind.LocalReferenceExpression, Type: System.Int32?) (Syntax: 'i1')
";
            var expectedDiagnostics = DiagnosticDescription.None;

            VerifyOperationTreeAndDiagnosticsForTest<VariableDeclaratorSyntax>(source, expectedOperationTree, expectedDiagnostics,
                AdditionalOperationTreeVerifier: new ExpectedSymbolVerifier().Verify);
        }

        [Fact]
        public void ConversionExpression_Implicit_NullableFromNonNullableConversion()
        {
            string source = @"
class Program
{
    static void Main(string[] args)
    {
        int i1 = 1;
        int? /*<bind>*/i2 = i1/*</bind>*/;
    }
}
";
            string expectedOperationTree = @"
IVariableDeclarationStatement (1 declarations) (OperationKind.VariableDeclarationStatement) (Syntax: 'int? /*<bin ... *</bind>*/;')
  IVariableDeclaration (1 variables) (OperationKind.VariableDeclaration) (Syntax: 'int? /*<bin ... *</bind>*/;')
    Variables: Local_1: System.Int32? i2
    Initializer: IConversionExpression (ConversionKind.CSharp, Implicit) (OperationKind.ConversionExpression, Type: System.Int32?) (Syntax: 'i1')
        ILocalReferenceExpression: i1 (OperationKind.LocalReferenceExpression, Type: System.Int32) (Syntax: 'i1')
";
            var expectedDiagnostics = DiagnosticDescription.None;

            VerifyOperationTreeAndDiagnosticsForTest<VariableDeclaratorSyntax>(source, expectedOperationTree, expectedDiagnostics,
                AdditionalOperationTreeVerifier: new ExpectedSymbolVerifier().Verify);
        }

        [Fact]
        public void ConversionExpression_Implicit_NullableToNonNullableConversion_Invalid()
        {
            string source = @"
class Program
{
    static void Main(string[] args)
    {
        int? i1 = 1;
        int /*<bind>*/i2 = i1/*</bind>*/;
    }
}
";
            string expectedOperationTree = @"
IVariableDeclarationStatement (1 declarations) (OperationKind.VariableDeclarationStatement, IsInvalid) (Syntax: 'int /*<bind ... *</bind>*/;')
  IVariableDeclaration (1 variables) (OperationKind.VariableDeclaration, IsInvalid) (Syntax: 'int /*<bind ... *</bind>*/;')
    Variables: Local_1: System.Int32 i2
    Initializer: IConversionExpression (ConversionKind.CSharp, Implicit) (OperationKind.ConversionExpression, Type: System.Int32, IsInvalid) (Syntax: 'i1')
        ILocalReferenceExpression: i1 (OperationKind.LocalReferenceExpression, Type: System.Int32?) (Syntax: 'i1')
";
            var expectedDiagnostics = new DiagnosticDescription[] {
                // CS0266: Cannot implicitly convert type 'int?' to 'int'. An explicit conversion exists (are you missing a cast?)
                //         int /*<bind>*/i2 = i1/*</bind>*/;
                Diagnostic(ErrorCode.ERR_NoImplicitConvCast, "i1").WithArguments("int?", "int").WithLocation(7, 28)
            };

            VerifyOperationTreeAndDiagnosticsForTest<VariableDeclaratorSyntax>(source, expectedOperationTree, expectedDiagnostics,
                AdditionalOperationTreeVerifier: new ExpectedSymbolVerifier().Verify);
        }

        [Fact]
        public void ConversionExpression_Implicit_InterpolatedStringToIFormattableExpression()
        {
            // This needs to be updated once https://github.com/dotnet/roslyn/issues/20046 is addressed.
            string source = @"
using System;

class Program
{
    static void Main(string[] args)
    {
        IFormattable /*<bind>*/f1 = $""{1}""/*</bind>*/;
    }
}
";
            string expectedOperationTree = @"
IVariableDeclarationStatement (1 declarations) (OperationKind.VariableDeclarationStatement) (Syntax: 'IFormattabl ... *</bind>*/;')
  IVariableDeclaration (1 variables) (OperationKind.VariableDeclaration) (Syntax: 'IFormattabl ... *</bind>*/;')
    Variables: Local_1: System.IFormattable f1
    Initializer: IConversionExpression (ConversionKind.Invalid, Implicit) (OperationKind.ConversionExpression, Type: System.IFormattable) (Syntax: '$""{1}""')
        IInterpolatedStringExpression (OperationKind.InterpolatedStringExpression, Type: System.String) (Syntax: '$""{1}""')
          Parts(1): IInterpolation (OperationKind.Interpolation) (Syntax: '{1}')
              Expression: ILiteralExpression (Text: 1) (OperationKind.LiteralExpression, Type: System.Int32, Constant: 1) (Syntax: '1')
";
            var expectedDiagnostics = DiagnosticDescription.None;

            VerifyOperationTreeAndDiagnosticsForTest<VariableDeclaratorSyntax>(source, expectedOperationTree, expectedDiagnostics,
                AdditionalOperationTreeVerifier: new ExpectedSymbolVerifier().Verify);
        }

        [Fact]
        public void ConversionExpression_Implicit_ReferenceToObjectConversion()
        {
            string source = @"
using System;

class C1
{
    static void Main(string[] args)
    {
        object /*<bind>*/o1 = new C1()/*</bind>*/;
    }
}
";
            string expectedOperationTree = @"
IVariableDeclarationStatement (1 declarations) (OperationKind.VariableDeclarationStatement) (Syntax: 'object /*<b ... *</bind>*/;')
  IVariableDeclaration (1 variables) (OperationKind.VariableDeclaration) (Syntax: 'object /*<b ... *</bind>*/;')
    Variables: Local_1: System.Object o1
    Initializer: IConversionExpression (ConversionKind.Cast, Implicit) (OperationKind.ConversionExpression, Type: System.Object) (Syntax: 'new C1()')
        IObjectCreationExpression (Constructor: C1..ctor()) (OperationKind.ObjectCreationExpression, Type: C1) (Syntax: 'new C1()')
";
            var expectedDiagnostics = DiagnosticDescription.None;

            VerifyOperationTreeAndDiagnosticsForTest<VariableDeclaratorSyntax>(source, expectedOperationTree, expectedDiagnostics,
                AdditionalOperationTreeVerifier: new ExpectedSymbolVerifier().Verify);
        }

        [Fact]
        public void ConversionExpression_Implicit_ReferenceToDynamicConversion()
        {
            string source = @"
using System;

class C1
{
    static void Main(string[] args)
    {
        dynamic /*<bind>*/d1 = new C1()/*</bind>*/;
    }
}
";
            string expectedOperationTree = @"
IVariableDeclarationStatement (1 declarations) (OperationKind.VariableDeclarationStatement) (Syntax: 'dynamic /*< ... *</bind>*/;')
  IVariableDeclaration (1 variables) (OperationKind.VariableDeclaration) (Syntax: 'dynamic /*< ... *</bind>*/;')
    Variables: Local_1: dynamic d1
    Initializer: IConversionExpression (ConversionKind.Cast, Implicit) (OperationKind.ConversionExpression, Type: dynamic) (Syntax: 'new C1()')
        IObjectCreationExpression (Constructor: C1..ctor()) (OperationKind.ObjectCreationExpression, Type: C1) (Syntax: 'new C1()')
";
            var expectedDiagnostics = DiagnosticDescription.None;

            VerifyOperationTreeAndDiagnosticsForTest<VariableDeclaratorSyntax>(source, expectedOperationTree, expectedDiagnostics,
                AdditionalOperationTreeVerifier: new ExpectedSymbolVerifier().Verify);
        }

        [Fact]
        public void ConversionExpression_Implicit_ReferenceClassToClassConversion()
        {
            string source = @"
using System;

class C1
{
    static void Main(string[] args)
    {
        C1 /*<bind>*/c1 = new C2()/*</bind>*/;
    }
}

class C2 : C1
{
}
";
            string expectedOperationTree = @"
IVariableDeclarationStatement (1 declarations) (OperationKind.VariableDeclarationStatement) (Syntax: 'C1 /*<bind> ... *</bind>*/;')
  IVariableDeclaration (1 variables) (OperationKind.VariableDeclaration) (Syntax: 'C1 /*<bind> ... *</bind>*/;')
    Variables: Local_1: C1 c1
    Initializer: IConversionExpression (ConversionKind.Cast, Implicit) (OperationKind.ConversionExpression, Type: C1) (Syntax: 'new C2()')
        IObjectCreationExpression (Constructor: C2..ctor()) (OperationKind.ObjectCreationExpression, Type: C2) (Syntax: 'new C2()')
";
            var expectedDiagnostics = DiagnosticDescription.None;

            VerifyOperationTreeAndDiagnosticsForTest<VariableDeclaratorSyntax>(source, expectedOperationTree, expectedDiagnostics,
                AdditionalOperationTreeVerifier: new ExpectedSymbolVerifier().Verify);
        }

        [Fact]
        public void ConversionExpression_Implicit_ReferenceClassToClassConversion_Invalid()
        {
            string source = @"
using System;

class C1
{
    static void Main(string[] args)
    {
        C1 /*<bind>*/c1 = new C2()/*</bind>*/;
    }
}

class C2
{
}
";
            string expectedOperationTree = @"
IVariableDeclarationStatement (1 declarations) (OperationKind.VariableDeclarationStatement, IsInvalid) (Syntax: 'C1 /*<bind> ... *</bind>*/;')
  IVariableDeclaration (1 variables) (OperationKind.VariableDeclaration, IsInvalid) (Syntax: 'C1 /*<bind> ... *</bind>*/;')
    Variables: Local_1: C1 c1
    Initializer: IConversionExpression (ConversionKind.Invalid, Implicit) (OperationKind.ConversionExpression, Type: C1, IsInvalid) (Syntax: 'new C2()')
        IObjectCreationExpression (Constructor: C2..ctor()) (OperationKind.ObjectCreationExpression, Type: C2) (Syntax: 'new C2()')
";
            var expectedDiagnostics = new DiagnosticDescription[] {
                // CS0029: Cannot implicitly convert type 'C2' to 'C1'
                //         C1 /*<bind>*/c1 = new C2()/*</bind>*/;
                Diagnostic(ErrorCode.ERR_NoImplicitConv, "new C2()").WithArguments("C2", "C1").WithLocation(8, 27)
            };

            VerifyOperationTreeAndDiagnosticsForTest<VariableDeclaratorSyntax>(source, expectedOperationTree, expectedDiagnostics,
                AdditionalOperationTreeVerifier: new ExpectedSymbolVerifier().Verify);
        }

        [Fact]
        public void ConversionExpression_Implicit_ReferenceConversion_InvalidSyntax()
        {
            string source = @"
using System;

class C1
{
    static void Main(string[] args)
    {
        C1 /*<bind>*/c1 = new/*</bind>*/;
    }
}
";
            string expectedOperationTree = @"
IVariableDeclarationStatement (1 declarations) (OperationKind.VariableDeclarationStatement, IsInvalid) (Syntax: 'C1 /*<bind> ... *</bind>*/;')
  IVariableDeclaration (1 variables) (OperationKind.VariableDeclaration, IsInvalid) (Syntax: 'C1 /*<bind> ... *</bind>*/;')
    Variables: Local_1: C1 c1
    Initializer: IConversionExpression (ConversionKind.Invalid, Implicit) (OperationKind.ConversionExpression, Type: C1, IsInvalid) (Syntax: 'new/*</bind>*/')
        IInvalidExpression (OperationKind.InvalidExpression, Type: ?, IsInvalid) (Syntax: 'new/*</bind>*/')
";
            var expectedDiagnostics = new DiagnosticDescription[] {
                // CS1031: Type expected
                //         C1 /*<bind>*/c1 = new/*</bind>*/;
                Diagnostic(ErrorCode.ERR_TypeExpected, ";").WithLocation(8, 41),
                // CS1526: A new expression requires (), [], or {} after type
                //         C1 /*<bind>*/c1 = new/*</bind>*/;
                Diagnostic(ErrorCode.ERR_BadNewExpr, ";").WithLocation(8, 41)
            };

            VerifyOperationTreeAndDiagnosticsForTest<VariableDeclaratorSyntax>(source, expectedOperationTree, expectedDiagnostics,
                AdditionalOperationTreeVerifier: new ExpectedSymbolVerifier().Verify);
        }

        [Fact]
        public void ConversionExpression_Implicit_ReferenceClassToInterfaceConversion()
        {
            string source = @"
using System;

interface I1
{
}

class C1 : I1
{
    static void Main(string[] args)
    {
        I1 /*<bind>*/i1 = new C1()/*</bind>*/;
    }
}
";
            string expectedOperationTree = @"
IVariableDeclarationStatement (1 declarations) (OperationKind.VariableDeclarationStatement) (Syntax: 'I1 /*<bind> ... *</bind>*/;')
  IVariableDeclaration (1 variables) (OperationKind.VariableDeclaration) (Syntax: 'I1 /*<bind> ... *</bind>*/;')
    Variables: Local_1: I1 i1
    Initializer: IConversionExpression (ConversionKind.Cast, Implicit) (OperationKind.ConversionExpression, Type: I1) (Syntax: 'new C1()')
        IObjectCreationExpression (Constructor: C1..ctor()) (OperationKind.ObjectCreationExpression, Type: C1) (Syntax: 'new C1()')
";
            var expectedDiagnostics = DiagnosticDescription.None;

            VerifyOperationTreeAndDiagnosticsForTest<VariableDeclaratorSyntax>(source, expectedOperationTree, expectedDiagnostics,
                AdditionalOperationTreeVerifier: new ExpectedSymbolVerifier().Verify);
        }

        [Fact]
        public void ConversionExpression_Implicit_ReferenceClassToInterfaceConversion_Invalid()
        {
            string source = @"
using System;

interface I1
{
}

class C1
{
    static void Main(string[] args)
    {
        I1 /*<bind>*/i1 = new C1()/*</bind>*/;
    }
}
";
            string expectedOperationTree = @"
IVariableDeclarationStatement (1 declarations) (OperationKind.VariableDeclarationStatement, IsInvalid) (Syntax: 'I1 /*<bind> ... *</bind>*/;')
  IVariableDeclaration (1 variables) (OperationKind.VariableDeclaration, IsInvalid) (Syntax: 'I1 /*<bind> ... *</bind>*/;')
    Variables: Local_1: I1 i1
    Initializer: IConversionExpression (ConversionKind.Cast, Implicit) (OperationKind.ConversionExpression, Type: I1, IsInvalid) (Syntax: 'new C1()')
        IObjectCreationExpression (Constructor: C1..ctor()) (OperationKind.ObjectCreationExpression, Type: C1) (Syntax: 'new C1()')
";
            var expectedDiagnostics = new DiagnosticDescription[] {
                // CS0266: Cannot implicitly convert type 'C1' to 'I1'. An explicit conversion exists (are you missing a cast?)
                //         I1 /*<bind>*/i1 = new C1()/*</bind>*/;
                Diagnostic(ErrorCode.ERR_NoImplicitConvCast, "new C1()").WithArguments("C1", "I1").WithLocation(12, 27)
            };

            VerifyOperationTreeAndDiagnosticsForTest<VariableDeclaratorSyntax>(source, expectedOperationTree, expectedDiagnostics,
                AdditionalOperationTreeVerifier: new ExpectedSymbolVerifier().Verify);
        }

        [Fact]
        public void ConversionExpression_Implicit_ReferenceInterfaceToClassConversion_Invalid()
        {
            string source = @"
using System;

interface I1
{
}

class C1
{
    static void Main(string[] args)
    {
        C1 /*<bind>*/i1 = new I1()/*</bind>*/;
    }
}
";
            string expectedOperationTree = @"
IVariableDeclarationStatement (1 declarations) (OperationKind.VariableDeclarationStatement, IsInvalid) (Syntax: 'C1 /*<bind> ... *</bind>*/;')
  IVariableDeclaration (1 variables) (OperationKind.VariableDeclaration, IsInvalid) (Syntax: 'C1 /*<bind> ... *</bind>*/;')
    Variables: Local_1: C1 i1
    Initializer: IConversionExpression (ConversionKind.Cast, Implicit) (OperationKind.ConversionExpression, Type: C1, IsInvalid) (Syntax: 'new I1()')
        IInvalidExpression (OperationKind.InvalidExpression, Type: I1, IsInvalid) (Syntax: 'new I1()')
";
            var expectedDiagnostics = new DiagnosticDescription[] {
                // CS0144: Cannot create an instance of the abstract class or interface 'I1'
                //         C1 /*<bind>*/i1 = new I1()/*</bind>*/;
                Diagnostic(ErrorCode.ERR_NoNewAbstract, "new I1()").WithArguments("I1").WithLocation(12, 27)
            };

            VerifyOperationTreeAndDiagnosticsForTest<VariableDeclaratorSyntax>(source, expectedOperationTree, expectedDiagnostics,
                AdditionalOperationTreeVerifier: new ExpectedSymbolVerifier().Verify);
        }

        [Fact]
        public void ConversionExpression_Implicit_ReferenceInterfaceToInterfaceConversion()
        {
            string source = @"
using System;

interface I1
{
}

interface I2 : I1
{
}

class C1 : I2
{
    static void Main(string[] args)
    {
        I2 i2 = new C1();
        I1 /*<bind>*/i1 = i2/*</bind>*/;
    }
}
";
            string expectedOperationTree = @"
IVariableDeclarationStatement (1 declarations) (OperationKind.VariableDeclarationStatement) (Syntax: 'I1 /*<bind> ... *</bind>*/;')
  IVariableDeclaration (1 variables) (OperationKind.VariableDeclaration) (Syntax: 'I1 /*<bind> ... *</bind>*/;')
    Variables: Local_1: I1 i1
    Initializer: IConversionExpression (ConversionKind.Cast, Implicit) (OperationKind.ConversionExpression, Type: I1) (Syntax: 'i2')
        ILocalReferenceExpression: i2 (OperationKind.LocalReferenceExpression, Type: I2) (Syntax: 'i2')
";
            var expectedDiagnostics = DiagnosticDescription.None;

            VerifyOperationTreeAndDiagnosticsForTest<VariableDeclaratorSyntax>(source, expectedOperationTree, expectedDiagnostics,
                AdditionalOperationTreeVerifier: new ExpectedSymbolVerifier().Verify);
        }

        [Fact]
        public void ConversionExpression_Implicit_ReferenceInterfaceToInterfaceConversion_Invalid()
        {
            string source = @"
using System;

interface I1
{
}

interface I2
{
}

class C1 : I2
{
    static void Main(string[] args)
    {
        I2 i2 = new C1();
        I1 /*<bind>*/i1 = i2/*</bind>*/;
    }
}
";
            string expectedOperationTree = @"
IVariableDeclarationStatement (1 declarations) (OperationKind.VariableDeclarationStatement, IsInvalid) (Syntax: 'I1 /*<bind> ... *</bind>*/;')
  IVariableDeclaration (1 variables) (OperationKind.VariableDeclaration, IsInvalid) (Syntax: 'I1 /*<bind> ... *</bind>*/;')
    Variables: Local_1: I1 i1
    Initializer: IConversionExpression (ConversionKind.Cast, Implicit) (OperationKind.ConversionExpression, Type: I1, IsInvalid) (Syntax: 'i2')
        ILocalReferenceExpression: i2 (OperationKind.LocalReferenceExpression, Type: I2) (Syntax: 'i2')
";
            var expectedDiagnostics = new DiagnosticDescription[] {
                // CS0266: Cannot implicitly convert type 'I2' to 'I1'. An explicit conversion exists (are you missing a cast?)
                //         I1 /*<bind>*/i1 = i2/*</bind>*/;
                Diagnostic(ErrorCode.ERR_NoImplicitConvCast, "i2").WithArguments("I2", "I1").WithLocation(17, 27)
            };

            VerifyOperationTreeAndDiagnosticsForTest<VariableDeclaratorSyntax>(source, expectedOperationTree, expectedDiagnostics,
                AdditionalOperationTreeVerifier: new ExpectedSymbolVerifier().Verify);
        }

        [Fact]
        public void ConversionExpression_Implicit_ReferenceArrayToArrayConversion()
        {
            string source = @"
using System;

class C1
{
    static void Main(string[] args)
    {
        C2[] c2arr = new C2[10];
        C1[] /*<bind>*/c1arr = c2arr/*</bind>*/;
    }
}

class C2 : C1
{
}
";
            string expectedOperationTree = @"
IVariableDeclarationStatement (1 declarations) (OperationKind.VariableDeclarationStatement) (Syntax: 'C1[] /*<bin ... *</bind>*/;')
  IVariableDeclaration (1 variables) (OperationKind.VariableDeclaration) (Syntax: 'C1[] /*<bin ... *</bind>*/;')
    Variables: Local_1: C1[] c1arr
    Initializer: IConversionExpression (ConversionKind.Cast, Implicit) (OperationKind.ConversionExpression, Type: C1[]) (Syntax: 'c2arr')
        ILocalReferenceExpression: c2arr (OperationKind.LocalReferenceExpression, Type: C2[]) (Syntax: 'c2arr')
";
            var expectedDiagnostics = DiagnosticDescription.None;

            VerifyOperationTreeAndDiagnosticsForTest<VariableDeclaratorSyntax>(source, expectedOperationTree, expectedDiagnostics,
                AdditionalOperationTreeVerifier: new ExpectedSymbolVerifier().Verify);
        }

        [Fact]
        public void ConversionExpression_Implicit_ReferenceArrayToArrayConversion_InvalidDimenionMismatch()
        {
            string source = @"
using System;

class C1
{
    static void Main(string[] args)
    {
        C2[] c2arr = new C2[10];
        C1[][] /*<bind>*/c1arr = c2arr/*</bind>*/;
    }
}

class C2 : C1
{
}
";
            string expectedOperationTree = @"
IVariableDeclarationStatement (1 declarations) (OperationKind.VariableDeclarationStatement, IsInvalid) (Syntax: 'C1[][] /*<b ... *</bind>*/;')
  IVariableDeclaration (1 variables) (OperationKind.VariableDeclaration, IsInvalid) (Syntax: 'C1[][] /*<b ... *</bind>*/;')
    Variables: Local_1: C1[][] c1arr
    Initializer: IConversionExpression (ConversionKind.Invalid, Implicit) (OperationKind.ConversionExpression, Type: C1[][], IsInvalid) (Syntax: 'c2arr')
        ILocalReferenceExpression: c2arr (OperationKind.LocalReferenceExpression, Type: C2[]) (Syntax: 'c2arr')
";
            var expectedDiagnostics = new DiagnosticDescription[] {
                // CS0029: Cannot implicitly convert type 'C2[]' to 'C1[][]'
                //         C1[][] /*<bind>*/c1arr = c2arr/*</bind>*/;
                Diagnostic(ErrorCode.ERR_NoImplicitConv, "c2arr").WithArguments("C2[]", "C1[][]").WithLocation(9, 34)
            };

            VerifyOperationTreeAndDiagnosticsForTest<VariableDeclaratorSyntax>(source, expectedOperationTree, expectedDiagnostics,
                AdditionalOperationTreeVerifier: new ExpectedSymbolVerifier().Verify);
        }

        [Fact]
        public void ConversionExpression_Implicit_ReferenceArrayToArrayConversion_InvalidNoReferenceConversion()
        {
            string source = @"
using System;

class C1
{
    static void Main(string[] args)
    {
        C2[] c2arr = new C2[10];
        C1[] /*<bind>*/c1arr = c2arr/*</bind>*/;
    }
}

class C2
{
}
";
            string expectedOperationTree = @"
IVariableDeclarationStatement (1 declarations) (OperationKind.VariableDeclarationStatement, IsInvalid) (Syntax: 'C1[] /*<bin ... *</bind>*/;')
  IVariableDeclaration (1 variables) (OperationKind.VariableDeclaration, IsInvalid) (Syntax: 'C1[] /*<bin ... *</bind>*/;')
    Variables: Local_1: C1[] c1arr
    Initializer: IConversionExpression (ConversionKind.Invalid, Implicit) (OperationKind.ConversionExpression, Type: C1[], IsInvalid) (Syntax: 'c2arr')
        ILocalReferenceExpression: c2arr (OperationKind.LocalReferenceExpression, Type: C2[]) (Syntax: 'c2arr')
";
            var expectedDiagnostics = new DiagnosticDescription[] {
                // CS0029: Cannot implicitly convert type 'C2[]' to 'C1[]'
                //         C1[] /*<bind>*/c1arr = c2arr/*</bind>*/;
                Diagnostic(ErrorCode.ERR_NoImplicitConv, "c2arr").WithArguments("C2[]", "C1[]").WithLocation(9, 32)
            };

            VerifyOperationTreeAndDiagnosticsForTest<VariableDeclaratorSyntax>(source, expectedOperationTree, expectedDiagnostics,
                AdditionalOperationTreeVerifier: new ExpectedSymbolVerifier().Verify);
        }

        [Fact]
        public void ConversionExpression_Implicit_ReferenceArrayToArrayConversion_InvalidValueTypeToReferenceType()
        {
            string source = @"
using System;

class C1
{
    static void Main(string[] args)
    {
        I1[] /*<bind>*/i1arr = new S1[10]/*</bind>*/;
    }
}

interface I1
{
}

struct S1 : I1
{
}
";
            string expectedOperationTree = @"
IVariableDeclarationStatement (1 declarations) (OperationKind.VariableDeclarationStatement, IsInvalid) (Syntax: 'I1[] /*<bin ... *</bind>*/;')
  IVariableDeclaration (1 variables) (OperationKind.VariableDeclaration, IsInvalid) (Syntax: 'I1[] /*<bin ... *</bind>*/;')
    Variables: Local_1: I1[] i1arr
    Initializer: IConversionExpression (ConversionKind.Invalid, Implicit) (OperationKind.ConversionExpression, Type: I1[], IsInvalid) (Syntax: 'new S1[10]')
        IArrayCreationExpression (Element Type: S1) (OperationKind.ArrayCreationExpression, Type: S1[]) (Syntax: 'new S1[10]')
          Dimension Sizes(1): ILiteralExpression (Text: 10) (OperationKind.LiteralExpression, Type: System.Int32, Constant: 10) (Syntax: '10')
";
            var expectedDiagnostics = new DiagnosticDescription[] {
                // CS0029: Cannot implicitly convert type 'S1[]' to 'I1[]'
                //         I1[] /*<bind>*/i1arr = new S1[10]/*</bind>*/;
                Diagnostic(ErrorCode.ERR_NoImplicitConv, "new S1[10]").WithArguments("S1[]", "I1[]").WithLocation(8, 32)
            };

            VerifyOperationTreeAndDiagnosticsForTest<VariableDeclaratorSyntax>(source, expectedOperationTree, expectedDiagnostics,
                AdditionalOperationTreeVerifier: new ExpectedSymbolVerifier().Verify);
        }

        [Fact]
        public void ConversionExpression_Implicit_ReferenceArrayToSystemArrayConversion()
        {
            string source = @"
using System;

class C1
{
    static void Main(string[] args)
    {
        Array /*<bind>*/a1 = new object[10]/*</bind>*/;
    }
}
";
            string expectedOperationTree = @"
IVariableDeclarationStatement (1 declarations) (OperationKind.VariableDeclarationStatement) (Syntax: 'Array /*<bi ... *</bind>*/;')
  IVariableDeclaration (1 variables) (OperationKind.VariableDeclaration) (Syntax: 'Array /*<bi ... *</bind>*/;')
    Variables: Local_1: System.Array a1
    Initializer: IConversionExpression (ConversionKind.Cast, Implicit) (OperationKind.ConversionExpression, Type: System.Array) (Syntax: 'new object[10]')
        IArrayCreationExpression (Element Type: System.Object) (OperationKind.ArrayCreationExpression, Type: System.Object[]) (Syntax: 'new object[10]')
          Dimension Sizes(1): ILiteralExpression (Text: 10) (OperationKind.LiteralExpression, Type: System.Int32, Constant: 10) (Syntax: '10')
";
            var expectedDiagnostics = DiagnosticDescription.None;

            VerifyOperationTreeAndDiagnosticsForTest<VariableDeclaratorSyntax>(source, expectedOperationTree, expectedDiagnostics,
                AdditionalOperationTreeVerifier: new ExpectedSymbolVerifier().Verify);
        }

        [Fact]
        public void ConversionExpression_Implicit_ReferenceArrayToSystemArrayConversion_MultiDimensionalArray()
        {
            string source = @"
using System;

class C1
{
    static void Main(string[] args)
    {
        Array /*<bind>*/a1 = new int[10][]/*</bind>*/;
    }
}
";
            string expectedOperationTree = @"
IVariableDeclarationStatement (1 declarations) (OperationKind.VariableDeclarationStatement) (Syntax: 'Array /*<bi ... *</bind>*/;')
  IVariableDeclaration (1 variables) (OperationKind.VariableDeclaration) (Syntax: 'Array /*<bi ... *</bind>*/;')
    Variables: Local_1: System.Array a1
    Initializer: IConversionExpression (ConversionKind.Cast, Implicit) (OperationKind.ConversionExpression, Type: System.Array) (Syntax: 'new int[10][]')
        IArrayCreationExpression (Element Type: System.Int32[]) (OperationKind.ArrayCreationExpression, Type: System.Int32[][]) (Syntax: 'new int[10][]')
          Dimension Sizes(1): ILiteralExpression (Text: 10) (OperationKind.LiteralExpression, Type: System.Int32, Constant: 10) (Syntax: '10')
";
            var expectedDiagnostics = DiagnosticDescription.None;

            VerifyOperationTreeAndDiagnosticsForTest<VariableDeclaratorSyntax>(source, expectedOperationTree, expectedDiagnostics,
                AdditionalOperationTreeVerifier: new ExpectedSymbolVerifier().Verify);
        }

        [Fact]
        public void ConversionExpression_Implicit_ReferenceArrayToSystemArrayConversion_InvalidNotArrayType()
        {
            string source = @"
using System;

class C1
{
    static void Main(string[] args)
    {
        Array /*<bind>*/a1 = new object()/*</bind>*/;
    }
}
";
            string expectedOperationTree = @"
IVariableDeclarationStatement (1 declarations) (OperationKind.VariableDeclarationStatement, IsInvalid) (Syntax: 'Array /*<bi ... *</bind>*/;')
  IVariableDeclaration (1 variables) (OperationKind.VariableDeclaration, IsInvalid) (Syntax: 'Array /*<bi ... *</bind>*/;')
    Variables: Local_1: System.Array a1
    Initializer: IConversionExpression (ConversionKind.Cast, Implicit) (OperationKind.ConversionExpression, Type: System.Array, IsInvalid) (Syntax: 'new object()')
        IObjectCreationExpression (Constructor: System.Object..ctor()) (OperationKind.ObjectCreationExpression, Type: System.Object) (Syntax: 'new object()')
";
            var expectedDiagnostics = new DiagnosticDescription[] {
                // CS0266: Cannot implicitly convert type 'object' to 'System.Array'. An explicit conversion exists (are you missing a cast?)
                //         Array /*<bind>*/a1 = new object()/*</bind>*/;
                Diagnostic(ErrorCode.ERR_NoImplicitConvCast, "new object()").WithArguments("object", "System.Array").WithLocation(8, 30)
            };

            VerifyOperationTreeAndDiagnosticsForTest<VariableDeclaratorSyntax>(source, expectedOperationTree, expectedDiagnostics,
                AdditionalOperationTreeVerifier: new ExpectedSymbolVerifier().Verify);
        }

        [Fact]
        public void ConversionExpression_Implicit_ReferenceArrayToIListTConversion()
        {
            string source = @"
using System.Collections.Generic;

class C1
{
    static void Main(string[] args)
    {
        IList<int> /*<bind>*/a1 = new int[10]/*</bind>*/;
    }
}
";
            string expectedOperationTree = @"
IVariableDeclarationStatement (1 declarations) (OperationKind.VariableDeclarationStatement) (Syntax: 'IList<int>  ... *</bind>*/;')
  IVariableDeclaration (1 variables) (OperationKind.VariableDeclaration) (Syntax: 'IList<int>  ... *</bind>*/;')
    Variables: Local_1: System.Collections.Generic.IList<System.Int32> a1
    Initializer: IConversionExpression (ConversionKind.Cast, Implicit) (OperationKind.ConversionExpression, Type: System.Collections.Generic.IList<System.Int32>) (Syntax: 'new int[10]')
        IArrayCreationExpression (Element Type: System.Int32) (OperationKind.ArrayCreationExpression, Type: System.Int32[]) (Syntax: 'new int[10]')
          Dimension Sizes(1): ILiteralExpression (Text: 10) (OperationKind.LiteralExpression, Type: System.Int32, Constant: 10) (Syntax: '10')
";
            var expectedDiagnostics = DiagnosticDescription.None;

            VerifyOperationTreeAndDiagnosticsForTest<VariableDeclaratorSyntax>(source, expectedOperationTree, expectedDiagnostics,
                AdditionalOperationTreeVerifier: new ExpectedSymbolVerifier().Verify);
        }

        [Fact]
        public void ConversionExpression_Implicit_ReferenceArrayToIListTConversion_InvalidNonArrayType()
        {
            string source = @"
using System.Collections.Generic;

class C1
{
    static void Main(string[] args)
    {
        IList<int> /*<bind>*/a1 = new object()/*</bind>*/;
    }
}
";
            string expectedOperationTree = @"
IVariableDeclarationStatement (1 declarations) (OperationKind.VariableDeclarationStatement, IsInvalid) (Syntax: 'IList<int>  ... *</bind>*/;')
  IVariableDeclaration (1 variables) (OperationKind.VariableDeclaration, IsInvalid) (Syntax: 'IList<int>  ... *</bind>*/;')
    Variables: Local_1: System.Collections.Generic.IList<System.Int32> a1
    Initializer: IConversionExpression (ConversionKind.Cast, Implicit) (OperationKind.ConversionExpression, Type: System.Collections.Generic.IList<System.Int32>, IsInvalid) (Syntax: 'new object()')
        IObjectCreationExpression (Constructor: System.Object..ctor()) (OperationKind.ObjectCreationExpression, Type: System.Object) (Syntax: 'new object()')
";
            var expectedDiagnostics = new DiagnosticDescription[] {
                // CS0266: Cannot implicitly convert type 'object' to 'System.Collections.Generic.IList<int>'. An explicit conversion exists (are you missing a cast?)
                //         IList<int> /*<bind>*/a1 = new object()/*</bind>*/;
                Diagnostic(ErrorCode.ERR_NoImplicitConvCast, "new object()").WithArguments("object", "System.Collections.Generic.IList<int>").WithLocation(8, 35)
            };

            VerifyOperationTreeAndDiagnosticsForTest<VariableDeclaratorSyntax>(source, expectedOperationTree, expectedDiagnostics,
                AdditionalOperationTreeVerifier: new ExpectedSymbolVerifier().Verify);
        }

        [Fact]
        public void ConversionExpression_Implicit_ReferenceDelegateTypeToSystemDelegateConversion()
        {
            string source = @"
using System;

class C1
{
    delegate void DType();
    void M1()
    {
        DType d1 = M2;
        Delegate /*<bind>*/d2 = d1/*</bind>*/;
    }

    void M2()
    {
    }
}
";
            string expectedOperationTree = @"
IVariableDeclarationStatement (1 declarations) (OperationKind.VariableDeclarationStatement) (Syntax: 'Delegate /* ... *</bind>*/;')
  IVariableDeclaration (1 variables) (OperationKind.VariableDeclaration) (Syntax: 'Delegate /* ... *</bind>*/;')
    Variables: Local_1: System.Delegate d2
    Initializer: IConversionExpression (ConversionKind.Cast, Implicit) (OperationKind.ConversionExpression, Type: System.Delegate) (Syntax: 'd1')
        ILocalReferenceExpression: d1 (OperationKind.LocalReferenceExpression, Type: C1.DType) (Syntax: 'd1')
";
            var expectedDiagnostics = DiagnosticDescription.None;

            VerifyOperationTreeAndDiagnosticsForTest<VariableDeclaratorSyntax>(source, expectedOperationTree, expectedDiagnostics,
                AdditionalOperationTreeVerifier: new ExpectedSymbolVerifier().Verify);
        }

        [Fact]
        public void ConversionExpression_Implicit_ReferenceDelegateTypeToSystemDelegateConversion_InvalidNonDelegateType()
        {
            string source = @"
using System;

class C1
{
    delegate void DType();
    void M1()
    {
        DType d1 = M2;
        Delegate /*<bind>*/d2 = d1()/*</bind>*/;
    }

    void M2()
    {
    }
}
";
            string expectedOperationTree = @"
IVariableDeclarationStatement (1 declarations) (OperationKind.VariableDeclarationStatement, IsInvalid) (Syntax: 'Delegate /* ... *</bind>*/;')
  IVariableDeclaration (1 variables) (OperationKind.VariableDeclaration, IsInvalid) (Syntax: 'Delegate /* ... *</bind>*/;')
    Variables: Local_1: System.Delegate d2
    Initializer: IConversionExpression (ConversionKind.Invalid, Implicit) (OperationKind.ConversionExpression, Type: System.Delegate, IsInvalid) (Syntax: 'd1()')
        IInvocationExpression (virtual void C1.DType.Invoke()) (OperationKind.InvocationExpression, Type: System.Void) (Syntax: 'd1()')
          Instance Receiver: ILocalReferenceExpression: d1 (OperationKind.LocalReferenceExpression, Type: C1.DType) (Syntax: 'd1')
";
            var expectedDiagnostics = new DiagnosticDescription[] {
                // CS0029: Cannot implicitly convert type 'void' to 'System.Delegate'
                //         Delegate /*<bind>*/d2 = d1()/*</bind>*/;
                Diagnostic(ErrorCode.ERR_NoImplicitConv, "d1()").WithArguments("void", "System.Delegate").WithLocation(10, 33)
            };

            VerifyOperationTreeAndDiagnosticsForTest<VariableDeclaratorSyntax>(source, expectedOperationTree, expectedDiagnostics,
                AdditionalOperationTreeVerifier: new ExpectedSymbolVerifier().Verify);
        }

        [Fact(Skip = "https://github.com/dotnet/roslyn/issues/20175")]
        public void ConversionExpression_Implicit_ReferenceDelegateTypeToSystemDelegateConversion_InvalidSyntax()
        {
            string source = @"
using System;

class C1
{
    delegate void DType();
    void M1()
    {
        Delegate /*<bind>*/d2 =/*</bind>*/;
    }

    void M2()
    {
    }
}
";
            string expectedOperationTree = @"
IVariableDeclarationStatement (1 declarations) (OperationKind.VariableDeclarationStatement, IsInvalid) (Syntax: 'Delegate /* ... *</bind>*/;')
  IVariableDeclaration (1 variables) (OperationKind.VariableDeclaration, IsInvalid) (Syntax: 'Delegate /* ... *</bind>*/;')
    Variables: Local_1: System.Delegate d2
    Initializer: IConversionExpression (ConversionKind.Invalid, Implicit) (OperationKind.ConversionExpression, Type: System.Delegate, IsInvalid) (Syntax: '')
        IInvalidExpression (OperationKind.InvalidExpression, Type: ?, IsInvalid) (Syntax: '')
";
            var expectedDiagnostics = new DiagnosticDescription[] {
                // CS1525: Invalid expression term ';'
                //         Delegate /*<bind>*/d2 =/*</bind>*/;
                Diagnostic(ErrorCode.ERR_InvalidExprTerm, ";").WithArguments(";").WithLocation(9, 43)
            };

            VerifyOperationTreeAndDiagnosticsForTest<VariableDeclaratorSyntax>(source, expectedOperationTree, expectedDiagnostics,
                AdditionalOperationTreeVerifier: new ExpectedSymbolVerifier().Verify);
        }

        /// <summary>
        /// This method is documenting the fact that there is no conversion expression here.
        /// </summary>
        [Fact]
        public void ConversionExpression_Implicit_ReferenceMethodToDelegateConversion_NoConversion()
        {
            string source = @"
class Program
{
    delegate void DType();
    void Main()
    {
        DType /*<bind>*/d1 = M1/*</bind>*/;
    }
    void M1()
    { }
}
";
            string expectedOperationTree = @"
IVariableDeclarationStatement (1 declarations) (OperationKind.VariableDeclarationStatement) (Syntax: 'DType /*<bi ... *</bind>*/;')
  IVariableDeclaration (1 variables) (OperationKind.VariableDeclaration) (Syntax: 'DType /*<bi ... *</bind>*/;')
    Variables: Local_1: Program.DType d1
    Initializer: IMethodBindingExpression: void Program.M1() (OperationKind.MethodBindingExpression, Type: Program.DType) (Syntax: 'M1')
        Instance Receiver: IInstanceReferenceExpression (InstanceReferenceKind.Implicit) (OperationKind.InstanceReferenceExpression, Type: Program) (Syntax: 'M1')
";
            var expectedDiagnostics = DiagnosticDescription.None;

            VerifyOperationTreeAndDiagnosticsForTest<VariableDeclaratorSyntax>(source, expectedOperationTree, expectedDiagnostics);
        }

        [Fact]
        public void ConversionExpression_Implicit_ReferenceMethodToDelegateConversion_InvalidIdentifier()
        {
            string source = @"
class Program
{
    delegate void DType();
    void Main()
    {
        DType /*<bind>*/d1 = M1/*</bind>*/;
    }
}
";
            string expectedOperationTree = @"
IVariableDeclarationStatement (1 declarations) (OperationKind.VariableDeclarationStatement, IsInvalid) (Syntax: 'DType /*<bi ... *</bind>*/;')
  IVariableDeclaration (1 variables) (OperationKind.VariableDeclaration, IsInvalid) (Syntax: 'DType /*<bi ... *</bind>*/;')
    Variables: Local_1: Program.DType d1
    Initializer: IConversionExpression (ConversionKind.Invalid, Implicit) (OperationKind.ConversionExpression, Type: Program.DType, IsInvalid) (Syntax: 'M1')
        IInvalidExpression (OperationKind.InvalidExpression, Type: ?, IsInvalid) (Syntax: 'M1')
";
            var expectedDiagnostics = new DiagnosticDescription[] {
                // CS0103: The name 'M1' does not exist in the current context
                //         DType /*<bind>*/d1 = M1/*</bind>*/;
                Diagnostic(ErrorCode.ERR_NameNotInContext, "M1").WithArguments("M1").WithLocation(7, 30)
            };

            VerifyOperationTreeAndDiagnosticsForTest<VariableDeclaratorSyntax>(source, expectedOperationTree, expectedDiagnostics,
                AdditionalOperationTreeVerifier: new ExpectedSymbolVerifier().Verify);
        }

        [Fact]
        public void ConversionExpression_Implicit_ReferenceLambdaToDelegateConversion()
        {
            // TODO: There should be an IDelegateCreationExpression (or something like it) here, wrapping the IConversionExpression.
            // See https://github.com/dotnet/roslyn/issues/20095.
            string source = @"
class Program
{
    delegate void DType();
    void Main()
    {
        DType /*<bind>*/d1 = () => { }/*</bind>*/;
    }
}
";
            string expectedOperationTree = @"
IVariableDeclarationStatement (1 declarations) (OperationKind.VariableDeclarationStatement) (Syntax: 'DType /*<bi ... *</bind>*/;')
  IVariableDeclaration (1 variables) (OperationKind.VariableDeclaration) (Syntax: 'DType /*<bi ... *</bind>*/;')
    Variables: Local_1: Program.DType d1
    Initializer: IConversionExpression (ConversionKind.CSharp, Implicit) (OperationKind.ConversionExpression, Type: Program.DType) (Syntax: '() => { }')
        ILambdaExpression (Signature: lambda expression) (OperationKind.LambdaExpression, Type: null) (Syntax: '() => { }')
          IBlockStatement (1 statements) (OperationKind.BlockStatement) (Syntax: '{ }')
            IReturnStatement (OperationKind.ReturnStatement) (Syntax: '{ }')
";
            var expectedDiagnostics = DiagnosticDescription.None;

            VerifyOperationTreeAndDiagnosticsForTest<VariableDeclaratorSyntax>(source, expectedOperationTree, expectedDiagnostics,
                AdditionalOperationTreeVerifier: new ExpectedSymbolVerifier().Verify);
        }

        [Fact]
        public void ConversionExpression_Implicit_ReferenceLambdaToDelegateConversion_InvalidMismatchedTypes()
        {
            string source = @"
class Program
{
    delegate void DType();
    void Main()
    {
        DType /*<bind>*/d1 = (string s) => { }/*</bind>*/;
    }
}
";
            string expectedOperationTree = @"
IVariableDeclarationStatement (1 declarations) (OperationKind.VariableDeclarationStatement, IsInvalid) (Syntax: 'DType /*<bi ... *</bind>*/;')
  IVariableDeclaration (1 variables) (OperationKind.VariableDeclaration, IsInvalid) (Syntax: 'DType /*<bi ... *</bind>*/;')
    Variables: Local_1: Program.DType d1
    Initializer: IConversionExpression (ConversionKind.Invalid, Implicit) (OperationKind.ConversionExpression, Type: Program.DType, IsInvalid) (Syntax: '(string s) => { }')
        ILambdaExpression (Signature: lambda expression) (OperationKind.LambdaExpression, Type: null) (Syntax: '(string s) => { }')
          IBlockStatement (0 statements) (OperationKind.BlockStatement) (Syntax: '{ }')
";
            var expectedDiagnostics = new DiagnosticDescription[] {
                // CS1593: Delegate 'Program.DType' does not take 1 arguments
                //         DType /*<bind>*/d1 = (string s) => { }/*</bind>*/;
                Diagnostic(ErrorCode.ERR_BadDelArgCount, "(string s) => { }").WithArguments("Program.DType", "1").WithLocation(7, 30)
            };

            VerifyOperationTreeAndDiagnosticsForTest<VariableDeclaratorSyntax>(source, expectedOperationTree, expectedDiagnostics,
                AdditionalOperationTreeVerifier: new ExpectedSymbolVerifier().Verify);
        }

        [Fact]
        public void ConversionExpression_Implicit_ReferenceLambdaToDelegateConversion_InvalidSyntax()
        {
            string source = @"
class Program
{
    delegate void DType();
    void Main()
    {
        DType /*<bind>*/d1 = () =>/*</bind>*/;
    }
}
";
            string expectedOperationTree = @"
IVariableDeclarationStatement (1 declarations) (OperationKind.VariableDeclarationStatement, IsInvalid) (Syntax: 'DType /*<bi ... *</bind>*/;')
  IVariableDeclaration (1 variables) (OperationKind.VariableDeclaration, IsInvalid) (Syntax: 'DType /*<bi ... *</bind>*/;')
    Variables: Local_1: Program.DType d1
    Initializer: IConversionExpression (ConversionKind.CSharp, Implicit) (OperationKind.ConversionExpression, Type: Program.DType, IsInvalid) (Syntax: '() =>/*</bind>*/')
        ILambdaExpression (Signature: lambda expression) (OperationKind.LambdaExpression, Type: null, IsInvalid) (Syntax: '() =>/*</bind>*/')
          IBlockStatement (2 statements) (OperationKind.BlockStatement, IsInvalid) (Syntax: '')
            IExpressionStatement (OperationKind.ExpressionStatement, IsInvalid) (Syntax: '')
              IInvalidExpression (OperationKind.InvalidExpression, Type: ?, IsInvalid) (Syntax: '')
            IReturnStatement (OperationKind.ReturnStatement) (Syntax: '')
";
            var expectedDiagnostics = new DiagnosticDescription[] {
                // CS1525: Invalid expression term ';'
                //         DType /*<bind>*/d1 = () =>/*</bind>*/;
                Diagnostic(ErrorCode.ERR_InvalidExprTerm, ";").WithArguments(";").WithLocation(7, 46)
            };

            VerifyOperationTreeAndDiagnosticsForTest<VariableDeclaratorSyntax>(source, expectedOperationTree, expectedDiagnostics,
                AdditionalOperationTreeVerifier: new ExpectedSymbolVerifier().Verify);
        }

        /// <summary>
        /// This is documenting the fact that there are currently not conversion expressions in this tree. Once
        /// https://github.com/dotnet/roslyn/issues/18839 is addressed, there should be.
        /// </summary>
        [Fact]
        public void ConversionExpression_Implicit_ReferenceLambdaToDelegateConstructor_NoConversion()
        {
            string source = @"
using System;

class Program
{
    static void Main(string[] args)
    {
        Action a = /*<bind>*/new Action(() => { })/*</bind>*/;
    }
}
";
            string expectedOperationTree = @"
IOperation:  (OperationKind.None) (Syntax: 'new Action(() => { })')
  Children(1): ILambdaExpression (Signature: lambda expression) (OperationKind.LambdaExpression, Type: null) (Syntax: '() => { }')
      IBlockStatement (1 statements) (OperationKind.BlockStatement) (Syntax: '{ }')
        IReturnStatement (OperationKind.ReturnStatement) (Syntax: '{ }')
";
            var expectedDiagnostics = DiagnosticDescription.None;
            var a = new Action(() => { });

            VerifyOperationTreeAndDiagnosticsForTest<ObjectCreationExpressionSyntax>(source, expectedOperationTree, expectedDiagnostics);
        }

        [Fact]
        public void ConversionExpression_Implicit_ReferenceTransitiveConversion()
        {
            string source = @"
class C1
{
    void M1()
    {
        C1 /*<bind>*/c1 = new C3()/*</bind>*/;
    }
}

class C2 : C1
{
}

class C3 : C2
{
}
";
            string expectedOperationTree = @"
IVariableDeclarationStatement (1 declarations) (OperationKind.VariableDeclarationStatement) (Syntax: 'C1 /*<bind> ... *</bind>*/;')
  IVariableDeclaration (1 variables) (OperationKind.VariableDeclaration) (Syntax: 'C1 /*<bind> ... *</bind>*/;')
    Variables: Local_1: C1 c1
    Initializer: IConversionExpression (ConversionKind.Cast, Implicit) (OperationKind.ConversionExpression, Type: C1) (Syntax: 'new C3()')
        IObjectCreationExpression (Constructor: C3..ctor()) (OperationKind.ObjectCreationExpression, Type: C3) (Syntax: 'new C3()')
";
            var expectedDiagnostics = DiagnosticDescription.None;

            VerifyOperationTreeAndDiagnosticsForTest<VariableDeclaratorSyntax>(source, expectedOperationTree, expectedDiagnostics,
                AdditionalOperationTreeVerifier: new ExpectedSymbolVerifier().Verify);
        }

        [Fact]
        public void ConversionExpression_Implicit_ReferenceCovarianceTransitiveConversion()
        {
            string source = @"
interface I1<in T>
{
}

class C1<T> : I1<T>
{
    void M1()
    {
        C2<C3> c2 = new C2<C3>();
        I1<C4> /*<bind>*/c1 = c2/*</bind>*/;
    }
}

class C2<T> : C1<T>
{
}

class C3
{
}

class C4 : C3
{
}
";
            string expectedOperationTree = @"
IVariableDeclarationStatement (1 declarations) (OperationKind.VariableDeclarationStatement) (Syntax: 'I1<C4> /*<b ... *</bind>*/;')
  IVariableDeclaration (1 variables) (OperationKind.VariableDeclaration) (Syntax: 'I1<C4> /*<b ... *</bind>*/;')
    Variables: Local_1: I1<C4> c1
    Initializer: IConversionExpression (ConversionKind.Cast, Implicit) (OperationKind.ConversionExpression, Type: I1<C4>) (Syntax: 'c2')
        ILocalReferenceExpression: c2 (OperationKind.LocalReferenceExpression, Type: C2<C3>) (Syntax: 'c2')
";
            var expectedDiagnostics = DiagnosticDescription.None;

            VerifyOperationTreeAndDiagnosticsForTest<VariableDeclaratorSyntax>(source, expectedOperationTree, expectedDiagnostics,
                AdditionalOperationTreeVerifier: new ExpectedSymbolVerifier().Verify);
        }

        [Fact]
        public void ConversionExpression_Implicit_ReferenceCovarianceTransitiveConversion_Invalid()
        {
            string source = @"
interface I1<in T>
{
}

class C1<T> : I1<T>
{
    void M1()
    {
        C2<C4> c2 = new C2<C4>();
        I1<C3> /*<bind>*/c1 = c2/*</bind>*/;
    }
}

class C2<T> : C1<T>
{
}

class C3
{
}

class C4 : C3
{
}
";
            string expectedOperationTree = @"
IVariableDeclarationStatement (1 declarations) (OperationKind.VariableDeclarationStatement, IsInvalid) (Syntax: 'I1<C3> /*<b ... *</bind>*/;')
  IVariableDeclaration (1 variables) (OperationKind.VariableDeclaration, IsInvalid) (Syntax: 'I1<C3> /*<b ... *</bind>*/;')
    Variables: Local_1: I1<C3> c1
    Initializer: IConversionExpression (ConversionKind.Cast, Implicit) (OperationKind.ConversionExpression, Type: I1<C3>, IsInvalid) (Syntax: 'c2')
        ILocalReferenceExpression: c2 (OperationKind.LocalReferenceExpression, Type: C2<C4>) (Syntax: 'c2')
";
            var expectedDiagnostics = new DiagnosticDescription[] {
                // CS0266: Cannot implicitly convert type 'C2<C4>' to 'I1<C3>'. An explicit conversion exists (are you missing a cast?)
                //         I1<C3> /*<bind>*/c1 = c2/*</bind>*/;
                Diagnostic(ErrorCode.ERR_NoImplicitConvCast, "c2").WithArguments("C2<C4>", "I1<C3>").WithLocation(11, 31)
            };

            VerifyOperationTreeAndDiagnosticsForTest<VariableDeclaratorSyntax>(source, expectedOperationTree, expectedDiagnostics,
                AdditionalOperationTreeVerifier: new ExpectedSymbolVerifier().Verify);
        }

        [Fact]
        public void ConversionExpression_Implicit_ReferenceContravarianceTransitiveConversion()
        {
            string source = @"
interface I1<out T>
{
}

class C1<T> : I1<T>
{
    void M1()
    {
        C2<C4> c2 = new C2<C4>();
        I1<C3> /*<bind>*/c1 = c2/*</bind>*/;
    }
}

class C2<T> : C1<T>
{
}

class C3
{
}

class C4 : C3
{
}
";
            string expectedOperationTree = @"
IVariableDeclarationStatement (1 declarations) (OperationKind.VariableDeclarationStatement) (Syntax: 'I1<C3> /*<b ... *</bind>*/;')
  IVariableDeclaration (1 variables) (OperationKind.VariableDeclaration) (Syntax: 'I1<C3> /*<b ... *</bind>*/;')
    Variables: Local_1: I1<C3> c1
    Initializer: IConversionExpression (ConversionKind.Cast, Implicit) (OperationKind.ConversionExpression, Type: I1<C3>) (Syntax: 'c2')
        ILocalReferenceExpression: c2 (OperationKind.LocalReferenceExpression, Type: C2<C4>) (Syntax: 'c2')
";
            var expectedDiagnostics = DiagnosticDescription.None;

            VerifyOperationTreeAndDiagnosticsForTest<VariableDeclaratorSyntax>(source, expectedOperationTree, expectedDiagnostics,
                AdditionalOperationTreeVerifier: new ExpectedSymbolVerifier().Verify);
        }

        [Fact]
        public void ConversionExpression_Implicit_ReferenceContravarianceTransitiveConversion_Invalid()
        {
            string source = @"
interface I1<out T>
{
}

class C1<T> : I1<T>
{
    void M1()
    {
        C2<C3> c2 = new C2<C3>();
        I1<C4> /*<bind>*/c1 = c2/*</bind>*/;
    }
}

class C2<T> : C1<T>
{
}

class C3
{
}

class C4 : C3
{
}
";
            string expectedOperationTree = @"
IVariableDeclarationStatement (1 declarations) (OperationKind.VariableDeclarationStatement, IsInvalid) (Syntax: 'I1<C4> /*<b ... *</bind>*/;')
  IVariableDeclaration (1 variables) (OperationKind.VariableDeclaration, IsInvalid) (Syntax: 'I1<C4> /*<b ... *</bind>*/;')
    Variables: Local_1: I1<C4> c1
    Initializer: IConversionExpression (ConversionKind.Cast, Implicit) (OperationKind.ConversionExpression, Type: I1<C4>, IsInvalid) (Syntax: 'c2')
        ILocalReferenceExpression: c2 (OperationKind.LocalReferenceExpression, Type: C2<C3>) (Syntax: 'c2')
";
            var expectedDiagnostics = new DiagnosticDescription[] {
                // CS0266: Cannot implicitly convert type 'C2<C3>' to 'I1<C4>'. An explicit conversion exists (are you missing a cast?)
                //         I1<C4> /*<bind>*/c1 = c2/*</bind>*/;
                Diagnostic(ErrorCode.ERR_NoImplicitConvCast, "c2").WithArguments("C2<C3>", "I1<C4>").WithLocation(11, 31)
            };

            VerifyOperationTreeAndDiagnosticsForTest<VariableDeclaratorSyntax>(source, expectedOperationTree, expectedDiagnostics,
                AdditionalOperationTreeVerifier: new ExpectedSymbolVerifier().Verify);
        }

        [Fact]
        public void ConversionExpression_Implicit_ReferenceInvariantTransitiveConversion()
        {
            string source = @"
using System.Collections.Generic;

class C1
{
    static void M1()
    {
        IList<string> /*<bind>*/list = new List<string>()/*</bind>*/;
    }
}
";
            string expectedOperationTree = @"
IVariableDeclarationStatement (1 declarations) (OperationKind.VariableDeclarationStatement) (Syntax: 'IList<strin ... *</bind>*/;')
  IVariableDeclaration (1 variables) (OperationKind.VariableDeclaration) (Syntax: 'IList<strin ... *</bind>*/;')
    Variables: Local_1: System.Collections.Generic.IList<System.String> list
    Initializer: IConversionExpression (ConversionKind.Cast, Implicit) (OperationKind.ConversionExpression, Type: System.Collections.Generic.IList<System.String>) (Syntax: 'new List<string>()')
        IObjectCreationExpression (Constructor: System.Collections.Generic.List<System.String>..ctor()) (OperationKind.ObjectCreationExpression, Type: System.Collections.Generic.List<System.String>) (Syntax: 'new List<string>()')
";
            var expectedDiagnostics = DiagnosticDescription.None;

            VerifyOperationTreeAndDiagnosticsForTest<VariableDeclaratorSyntax>(source, expectedOperationTree, expectedDiagnostics,
                AdditionalOperationTreeVerifier: new ExpectedSymbolVerifier().Verify);
        }

        [Fact]
        public void ConversionExpression_Implicit_ReferenceTypeParameterClassConversion()
        {
            string source = @"
class C1
{
    static void M1<T>()
        where T : C2, new()
    {
        C1 /*<bind>*/c1 = new T()/*</bind>*/;
    }
}

class C2 : C1
{

}
";
            string expectedOperationTree = @"
IVariableDeclarationStatement (1 declarations) (OperationKind.VariableDeclarationStatement) (Syntax: 'C1 /*<bind> ... *</bind>*/;')
  IVariableDeclaration (1 variables) (OperationKind.VariableDeclaration) (Syntax: 'C1 /*<bind> ... *</bind>*/;')
    Variables: Local_1: C1 c1
    Initializer: IConversionExpression (ConversionKind.Cast, Implicit) (OperationKind.ConversionExpression, Type: C1) (Syntax: 'new T()')
        ITypeParameterObjectCreationExpression (OperationKind.TypeParameterObjectCreationExpression, Type: T) (Syntax: 'new T()')
";
            var expectedDiagnostics = DiagnosticDescription.None;

            VerifyOperationTreeAndDiagnosticsForTest<VariableDeclaratorSyntax>(source, expectedOperationTree, expectedDiagnostics,
                AdditionalOperationTreeVerifier: new ExpectedSymbolVerifier().Verify);
        }

        [Fact]
        public void ConversionExpression_Implicit_ReferenceTypeParameterClassConversion_InvalidConversion()
        {
            string source = @"
class C1
{
    static void M1<T>()
        where T : class, new()
    {
        C1 /*<bind>*/c1 = new T()/*</bind>*/;
    }
}

class C2 : C1
{

}
";
            string expectedOperationTree = @"
IVariableDeclarationStatement (1 declarations) (OperationKind.VariableDeclarationStatement, IsInvalid) (Syntax: 'C1 /*<bind> ... *</bind>*/;')
  IVariableDeclaration (1 variables) (OperationKind.VariableDeclaration, IsInvalid) (Syntax: 'C1 /*<bind> ... *</bind>*/;')
    Variables: Local_1: C1 c1
    Initializer: IConversionExpression (ConversionKind.Invalid, Implicit) (OperationKind.ConversionExpression, Type: C1, IsInvalid) (Syntax: 'new T()')
        ITypeParameterObjectCreationExpression (OperationKind.TypeParameterObjectCreationExpression, Type: T) (Syntax: 'new T()')
";
            var expectedDiagnostics = new DiagnosticDescription[] {
                // CS0029: Cannot implicitly convert type 'T' to 'C1'
                //         C1 /*<bind>*/c1 = new T()/*</bind>*/;
                Diagnostic(ErrorCode.ERR_NoImplicitConv, "new T()").WithArguments("T", "C1").WithLocation(7, 27)
            };

            VerifyOperationTreeAndDiagnosticsForTest<VariableDeclaratorSyntax>(source, expectedOperationTree, expectedDiagnostics,
                AdditionalOperationTreeVerifier: new ExpectedSymbolVerifier().Verify);
        }

        [Fact]
        public void ConversionExpression_Implicit_ReferenceTypeParameterInterfaceConversion()
        {
            string source = @"
interface I1
{
}

class C1 : I1
{
    static void M1<T>()
        where T : C1, new()
    {
        I1 /*<bind>*/i1 = new T()/*</bind>*/;
    }
}
";
            string expectedOperationTree = @"
IVariableDeclarationStatement (1 declarations) (OperationKind.VariableDeclarationStatement) (Syntax: 'I1 /*<bind> ... *</bind>*/;')
  IVariableDeclaration (1 variables) (OperationKind.VariableDeclaration) (Syntax: 'I1 /*<bind> ... *</bind>*/;')
    Variables: Local_1: I1 i1
    Initializer: IConversionExpression (ConversionKind.Cast, Implicit) (OperationKind.ConversionExpression, Type: I1) (Syntax: 'new T()')
        ITypeParameterObjectCreationExpression (OperationKind.TypeParameterObjectCreationExpression, Type: T) (Syntax: 'new T()')
";
            var expectedDiagnostics = DiagnosticDescription.None;

            VerifyOperationTreeAndDiagnosticsForTest<VariableDeclaratorSyntax>(source, expectedOperationTree, expectedDiagnostics,
                AdditionalOperationTreeVerifier: new ExpectedSymbolVerifier().Verify);
        }

        [Fact]
        public void ConversionExpression_Implicit_ReferenceTypeParameterToInterfaceConversion_InvalidConversion()
        {
            string source = @"
interface I1
{
}

class C1
{
    static void M1<T>()
        where T : C1, new()
    {
        I1 /*<bind>*/i1 = new T()/*</bind>*/;
    }
}
";
            string expectedOperationTree = @"
IVariableDeclarationStatement (1 declarations) (OperationKind.VariableDeclarationStatement, IsInvalid) (Syntax: 'I1 /*<bind> ... *</bind>*/;')
  IVariableDeclaration (1 variables) (OperationKind.VariableDeclaration, IsInvalid) (Syntax: 'I1 /*<bind> ... *</bind>*/;')
    Variables: Local_1: I1 i1
    Initializer: IConversionExpression (ConversionKind.Cast, Implicit) (OperationKind.ConversionExpression, Type: I1, IsInvalid) (Syntax: 'new T()')
        ITypeParameterObjectCreationExpression (OperationKind.TypeParameterObjectCreationExpression, Type: T) (Syntax: 'new T()')
";
            var expectedDiagnostics = new DiagnosticDescription[] {
                // CS0266: Cannot implicitly convert type 'T' to 'I1'. An explicit conversion exists (are you missing a cast?)
                //         I1 /*<bind>*/i1 = new T()/*</bind>*/;
                Diagnostic(ErrorCode.ERR_NoImplicitConvCast, "new T()").WithArguments("T", "I1").WithLocation(11, 27)
            };

            VerifyOperationTreeAndDiagnosticsForTest<VariableDeclaratorSyntax>(source, expectedOperationTree, expectedDiagnostics,
                AdditionalOperationTreeVerifier: new ExpectedSymbolVerifier().Verify);
        }

        [Fact]
        public void ConversionExpression_Implicit_ReferenceTypeParameterToConstraintParameterConversion()
        {
            string source = @"
interface I1
{
}

class C1
{
    static void M1<T, U>()
        where T : U, new()
        where U : class
    {
        U /*<bind>*/u = new T()/*</bind>*/;
    }
}
";
            string expectedOperationTree = @"
IVariableDeclarationStatement (1 declarations) (OperationKind.VariableDeclarationStatement) (Syntax: 'U /*<bind>* ... *</bind>*/;')
  IVariableDeclaration (1 variables) (OperationKind.VariableDeclaration) (Syntax: 'U /*<bind>* ... *</bind>*/;')
    Variables: Local_1: U u
    Initializer: IConversionExpression (ConversionKind.Cast, Implicit) (OperationKind.ConversionExpression, Type: U) (Syntax: 'new T()')
        ITypeParameterObjectCreationExpression (OperationKind.TypeParameterObjectCreationExpression, Type: T) (Syntax: 'new T()')
";
            var expectedDiagnostics = DiagnosticDescription.None;

            VerifyOperationTreeAndDiagnosticsForTest<VariableDeclaratorSyntax>(source, expectedOperationTree, expectedDiagnostics,
                AdditionalOperationTreeVerifier: new ExpectedSymbolVerifier().Verify);
        }

        [Fact]
        public void ConversionExpression_Implicit_ReferenceTypeParameterToConstraintParameter_InvalidConversion()
        {
            string source = @"
interface I1
{
}

class C1
{
    static void M1<T, U>()
        where T : class, new()
        where U : class
    {
        U /*<bind>*/u = new T()/*</bind>*/;
    }
}
";
            string expectedOperationTree = @"
IVariableDeclarationStatement (1 declarations) (OperationKind.VariableDeclarationStatement, IsInvalid) (Syntax: 'U /*<bind>* ... *</bind>*/;')
  IVariableDeclaration (1 variables) (OperationKind.VariableDeclaration, IsInvalid) (Syntax: 'U /*<bind>* ... *</bind>*/;')
    Variables: Local_1: U u
    Initializer: IConversionExpression (ConversionKind.Invalid, Implicit) (OperationKind.ConversionExpression, Type: U, IsInvalid) (Syntax: 'new T()')
        ITypeParameterObjectCreationExpression (OperationKind.TypeParameterObjectCreationExpression, Type: T) (Syntax: 'new T()')
";
            var expectedDiagnostics = new DiagnosticDescription[] {
                // CS0029: Cannot implicitly convert type 'T' to 'U'
                //         U /*<bind>*/u = new T()/*</bind>*/;
                Diagnostic(ErrorCode.ERR_NoImplicitConv, "new T()").WithArguments("T", "U").WithLocation(12, 25)
            };

            VerifyOperationTreeAndDiagnosticsForTest<VariableDeclaratorSyntax>(source, expectedOperationTree, expectedDiagnostics,
                AdditionalOperationTreeVerifier: new ExpectedSymbolVerifier().Verify);
        }

        [Fact]
        public void ConversionExpression_Implicit_ReferenceTypeParameterFromNull()
        {
            string source = @"
interface I1
{
}

class C1
{
    static void M1<T, U>()
        where T : class, new()
    {
        T /*<bind>*/t = null/*</bind>*/;
    }
}
";
            string expectedOperationTree = @"
IVariableDeclarationStatement (1 declarations) (OperationKind.VariableDeclarationStatement) (Syntax: 'T /*<bind>* ... *</bind>*/;')
  IVariableDeclaration (1 variables) (OperationKind.VariableDeclaration) (Syntax: 'T /*<bind>* ... *</bind>*/;')
    Variables: Local_1: T t
    Initializer: IConversionExpression (ConversionKind.Cast, Implicit) (OperationKind.ConversionExpression, Type: T, Constant: null) (Syntax: 'null')
        ILiteralExpression (Text: null) (OperationKind.LiteralExpression, Type: null, Constant: null) (Syntax: 'null')
";
            var expectedDiagnostics = new DiagnosticDescription[] {
                // CS0219: The variable 't' is assigned but its value is never used
                //         T /*<bind>*/t = null/*</bind>*/;
                Diagnostic(ErrorCode.WRN_UnreferencedVarAssg, "t").WithArguments("t").WithLocation(11, 21)
            };

            VerifyOperationTreeAndDiagnosticsForTest<VariableDeclaratorSyntax>(source, expectedOperationTree, expectedDiagnostics,
                AdditionalOperationTreeVerifier: new ExpectedSymbolVerifier().Verify);
        }

        [Fact]
        public void ConversionExpression_Implicit_ReferenceTypeParameterFromNull_InvalidNoReferenceConstraint()
        {
            string source = @"
interface I1
{
}

class C1
{
    static void M1<T, U>()
        where T : new()
    {
        T /*<bind>*/t = null/*</bind>*/;
    }
}
";
            string expectedOperationTree = @"
IVariableDeclarationStatement (1 declarations) (OperationKind.VariableDeclarationStatement, IsInvalid) (Syntax: 'T /*<bind>* ... *</bind>*/;')
  IVariableDeclaration (1 variables) (OperationKind.VariableDeclaration, IsInvalid) (Syntax: 'T /*<bind>* ... *</bind>*/;')
    Variables: Local_1: T t
    Initializer: IConversionExpression (ConversionKind.Invalid, Implicit) (OperationKind.ConversionExpression, Type: T, IsInvalid) (Syntax: 'null')
        ILiteralExpression (Text: null) (OperationKind.LiteralExpression, Type: null, Constant: null) (Syntax: 'null')
";
            var expectedDiagnostics = new DiagnosticDescription[] {
                // CS0403: Cannot convert null to type parameter 'T' because it could be a non-nullable value type. Consider using 'default(T)' instead.
                //         T /*<bind>*/t = null/*</bind>*/;
                Diagnostic(ErrorCode.ERR_TypeVarCantBeNull, "null").WithArguments("T").WithLocation(11, 25)
            };

            VerifyOperationTreeAndDiagnosticsForTest<VariableDeclaratorSyntax>(source, expectedOperationTree, expectedDiagnostics,
                AdditionalOperationTreeVerifier: new ExpectedSymbolVerifier().Verify);
        }

        [Fact]
        public void ConversionExpression_Implicit_BoxingNonNullableValueToObjectConversion()
        {
            string source = @"

class C1
{
    static void M1()
    {
        int i = 1;
        object /*<bind>*/o = i/*</bind>*/;
    }
}
";
            string expectedOperationTree = @"
IVariableDeclarationStatement (1 declarations) (OperationKind.VariableDeclarationStatement) (Syntax: 'object /*<b ... *</bind>*/;')
  IVariableDeclaration (1 variables) (OperationKind.VariableDeclaration) (Syntax: 'object /*<b ... *</bind>*/;')
    Variables: Local_1: System.Object o
    Initializer: IConversionExpression (ConversionKind.Cast, Implicit) (OperationKind.ConversionExpression, Type: System.Object) (Syntax: 'i')
        ILocalReferenceExpression: i (OperationKind.LocalReferenceExpression, Type: System.Int32) (Syntax: 'i')
";
            var expectedDiagnostics = DiagnosticDescription.None;

            VerifyOperationTreeAndDiagnosticsForTest<VariableDeclaratorSyntax>(source, expectedOperationTree, expectedDiagnostics,
                AdditionalOperationTreeVerifier: new ExpectedSymbolVerifier().Verify);
        }

        [Fact]
        public void ConversionExpression_Implicit_BoxingNonNullableValueToDynamicConversion()
        {
            string source = @"

class C1
{
    static void M1()
    {
        int i = 1;
        dynamic /*<bind>*/d = i/*</bind>*/;
    }
}
";
            string expectedOperationTree = @"
IVariableDeclarationStatement (1 declarations) (OperationKind.VariableDeclarationStatement) (Syntax: 'dynamic /*< ... *</bind>*/;')
  IVariableDeclaration (1 variables) (OperationKind.VariableDeclaration) (Syntax: 'dynamic /*< ... *</bind>*/;')
    Variables: Local_1: dynamic d
    Initializer: IConversionExpression (ConversionKind.Cast, Implicit) (OperationKind.ConversionExpression, Type: dynamic) (Syntax: 'i')
        ILocalReferenceExpression: i (OperationKind.LocalReferenceExpression, Type: System.Int32) (Syntax: 'i')
";
            var expectedDiagnostics = DiagnosticDescription.None;

            VerifyOperationTreeAndDiagnosticsForTest<VariableDeclaratorSyntax>(source, expectedOperationTree, expectedDiagnostics,
                AdditionalOperationTreeVerifier: new ExpectedSymbolVerifier().Verify);
        }

        [Fact]
        public void ConversionExpression_Implicit_BoxingValueToSystemValueTypeConversion()
        {
            string source = @"
using System;

struct S1
{
    void M1()
    {
        ValueType /*<bind>*/v1 = new S1()/*</bind>*/;
    }
}

";
            string expectedOperationTree = @"
IVariableDeclarationStatement (1 declarations) (OperationKind.VariableDeclarationStatement) (Syntax: 'ValueType / ... *</bind>*/;')
  IVariableDeclaration (1 variables) (OperationKind.VariableDeclaration) (Syntax: 'ValueType / ... *</bind>*/;')
    Variables: Local_1: System.ValueType v1
    Initializer: IConversionExpression (ConversionKind.Cast, Implicit) (OperationKind.ConversionExpression, Type: System.ValueType) (Syntax: 'new S1()')
        IObjectCreationExpression (Constructor: S1..ctor()) (OperationKind.ObjectCreationExpression, Type: S1) (Syntax: 'new S1()')
";
            var expectedDiagnostics = DiagnosticDescription.None;

            VerifyOperationTreeAndDiagnosticsForTest<VariableDeclaratorSyntax>(source, expectedOperationTree, expectedDiagnostics,
                AdditionalOperationTreeVerifier: new ExpectedSymbolVerifier().Verify);
        }

        [Fact]
        public void ConversionExpression_Implicit_BoxingNonNullableValueToSystemValueTypeConversion_InvalidNonValueType()
        {
            string source = @"
using System;

class C1
{
    void M1()
    {
        ValueType /*<bind>*/v1 = new C1()/*</bind>*/;
    }
}

";
            string expectedOperationTree = @"
IVariableDeclarationStatement (1 declarations) (OperationKind.VariableDeclarationStatement, IsInvalid) (Syntax: 'ValueType / ... *</bind>*/;')
  IVariableDeclaration (1 variables) (OperationKind.VariableDeclaration, IsInvalid) (Syntax: 'ValueType / ... *</bind>*/;')
    Variables: Local_1: System.ValueType v1
    Initializer: IConversionExpression (ConversionKind.Invalid, Implicit) (OperationKind.ConversionExpression, Type: System.ValueType, IsInvalid) (Syntax: 'new C1()')
        IObjectCreationExpression (Constructor: C1..ctor()) (OperationKind.ObjectCreationExpression, Type: C1) (Syntax: 'new C1()')
";
            var expectedDiagnostics = new DiagnosticDescription[] {
                // CS0029: Cannot implicitly convert type 'C1' to 'System.ValueType'
                //         ValueType /*<bind>*/v1 = new C1()/*</bind>*/;
                Diagnostic(ErrorCode.ERR_NoImplicitConv, "new C1()").WithArguments("C1", "System.ValueType").WithLocation(8, 34)
            };

            VerifyOperationTreeAndDiagnosticsForTest<VariableDeclaratorSyntax>(source, expectedOperationTree, expectedDiagnostics,
                AdditionalOperationTreeVerifier: new ExpectedSymbolVerifier().Verify);
        }

        [Fact]
        public void ConversionExpression_Implicit_BoxingNonNullableValueToImplementingInterfaceConversion()
        {
            string source = @"
interface I1
{
}

struct S1 : I1
{
    void M1()
    {
        I1 /*<bind>*/i1 = new S1()/*</bind>*/;
    }
}
";
            string expectedOperationTree = @"
IVariableDeclarationStatement (1 declarations) (OperationKind.VariableDeclarationStatement) (Syntax: 'I1 /*<bind> ... *</bind>*/;')
  IVariableDeclaration (1 variables) (OperationKind.VariableDeclaration) (Syntax: 'I1 /*<bind> ... *</bind>*/;')
    Variables: Local_1: I1 i1
    Initializer: IConversionExpression (ConversionKind.Cast, Implicit) (OperationKind.ConversionExpression, Type: I1) (Syntax: 'new S1()')
        IObjectCreationExpression (Constructor: S1..ctor()) (OperationKind.ObjectCreationExpression, Type: S1) (Syntax: 'new S1()')
";
            var expectedDiagnostics = DiagnosticDescription.None;

            VerifyOperationTreeAndDiagnosticsForTest<VariableDeclaratorSyntax>(source, expectedOperationTree, expectedDiagnostics,
                AdditionalOperationTreeVerifier: new ExpectedSymbolVerifier().Verify);
        }

        [Fact]
        public void ConversionExpression_Implicit_BoxingNonNullableValueToImplementingInterfaceConversion_InvalidNotImplementing()
        {
            string source = @"
interface I1
{
}

struct S1
{
    void M1()
    {
        I1 /*<bind>*/i1 = new S1()/*</bind>*/;
    }
}
";
            string expectedOperationTree = @"
IVariableDeclarationStatement (1 declarations) (OperationKind.VariableDeclarationStatement, IsInvalid) (Syntax: 'I1 /*<bind> ... *</bind>*/;')
  IVariableDeclaration (1 variables) (OperationKind.VariableDeclaration, IsInvalid) (Syntax: 'I1 /*<bind> ... *</bind>*/;')
    Variables: Local_1: I1 i1
    Initializer: IConversionExpression (ConversionKind.Invalid, Implicit) (OperationKind.ConversionExpression, Type: I1, IsInvalid) (Syntax: 'new S1()')
        IObjectCreationExpression (Constructor: S1..ctor()) (OperationKind.ObjectCreationExpression, Type: S1) (Syntax: 'new S1()')
";
            var expectedDiagnostics = new DiagnosticDescription[] {
                // CS0029: Cannot implicitly convert type 'S1' to 'I1'
                //         I1 /*<bind>*/i1 = new S1()/*</bind>*/;
                Diagnostic(ErrorCode.ERR_NoImplicitConv, "new S1()").WithArguments("S1", "I1").WithLocation(10, 27)
            };

            VerifyOperationTreeAndDiagnosticsForTest<VariableDeclaratorSyntax>(source, expectedOperationTree, expectedDiagnostics,
                AdditionalOperationTreeVerifier: new ExpectedSymbolVerifier().Verify);
        }

        [Fact]
        public void ConversionExpression_Implicit_BoxingNullableValueToImplementingInterfaceConversion()
        {
            string source = @"
interface I1
{
}

struct S1 : I1
{
    void M1()
    {
        S1? s1 = null;
        I1 /*<bind>*/i1 = s1/*</bind>*/;
    }
}
";
            string expectedOperationTree = @"
IVariableDeclarationStatement (1 declarations) (OperationKind.VariableDeclarationStatement) (Syntax: 'I1 /*<bind> ... *</bind>*/;')
  IVariableDeclaration (1 variables) (OperationKind.VariableDeclaration) (Syntax: 'I1 /*<bind> ... *</bind>*/;')
    Variables: Local_1: I1 i1
    Initializer: IConversionExpression (ConversionKind.Cast, Implicit) (OperationKind.ConversionExpression, Type: I1) (Syntax: 's1')
        ILocalReferenceExpression: s1 (OperationKind.LocalReferenceExpression, Type: S1?) (Syntax: 's1')
";
            var expectedDiagnostics = DiagnosticDescription.None;

            VerifyOperationTreeAndDiagnosticsForTest<VariableDeclaratorSyntax>(source, expectedOperationTree, expectedDiagnostics,
                AdditionalOperationTreeVerifier: new ExpectedSymbolVerifier().Verify);
        }

        [Fact]
        public void ConversionExpression_Implicit_BoxingNullableValueToImplementingInterfaceConversion_InvalidNotImplementing()
        {
            string source = @"
interface I1
{
}

struct S1
{
    void M1()
    {
        S1? s1 = null;
        I1 /*<bind>*/i1 = s1/*</bind>*/;
    }
}
";
            string expectedOperationTree = @"
IVariableDeclarationStatement (1 declarations) (OperationKind.VariableDeclarationStatement, IsInvalid) (Syntax: 'I1 /*<bind> ... *</bind>*/;')
  IVariableDeclaration (1 variables) (OperationKind.VariableDeclaration, IsInvalid) (Syntax: 'I1 /*<bind> ... *</bind>*/;')
    Variables: Local_1: I1 i1
    Initializer: IConversionExpression (ConversionKind.Invalid, Implicit) (OperationKind.ConversionExpression, Type: I1, IsInvalid) (Syntax: 's1')
        ILocalReferenceExpression: s1 (OperationKind.LocalReferenceExpression, Type: S1?) (Syntax: 's1')
";
            var expectedDiagnostics = new DiagnosticDescription[] {
                // CS0029: Cannot implicitly convert type 'S1?' to 'I1'
                //         I1 /*<bind>*/i1 = s1/*</bind>*/;
                Diagnostic(ErrorCode.ERR_NoImplicitConv, "s1").WithArguments("S1?", "I1").WithLocation(11, 27)
            };

            VerifyOperationTreeAndDiagnosticsForTest<VariableDeclaratorSyntax>(source, expectedOperationTree, expectedDiagnostics,
                AdditionalOperationTreeVerifier: new ExpectedSymbolVerifier().Verify);
        }

        [Fact]
        public void ConversionExpression_Implicit_BoxingEnumToSystemEnumConversion()
        {
            string source = @"
using System;

enum E1
{
    E
}

struct S1
{
    void M1()
    {
        Enum /*<bind>*/e = E1.E/*</bind>*/;
    }
}

";
            string expectedOperationTree = @"
IVariableDeclarationStatement (1 declarations) (OperationKind.VariableDeclarationStatement) (Syntax: 'Enum /*<bin ... *</bind>*/;')
  IVariableDeclaration (1 variables) (OperationKind.VariableDeclaration) (Syntax: 'Enum /*<bin ... *</bind>*/;')
    Variables: Local_1: System.Enum e
    Initializer: IConversionExpression (ConversionKind.Cast, Implicit) (OperationKind.ConversionExpression, Type: System.Enum) (Syntax: 'E1.E')
        IFieldReferenceExpression: E1.E (Static) (OperationKind.FieldReferenceExpression, Type: E1, Constant: 0) (Syntax: 'E1.E')
";
            var expectedDiagnostics = DiagnosticDescription.None;

            VerifyOperationTreeAndDiagnosticsForTest<VariableDeclaratorSyntax>(source, expectedOperationTree, expectedDiagnostics,
                AdditionalOperationTreeVerifier: new ExpectedSymbolVerifier().Verify);
        }

        [Fact]
        public void ConversionExpression_Implicit_BoxingEnumToSystemEnumConversion_InvalidNotEnum()
        {
            string source = @"
using System;

enum E1
{
    E
}

struct S1
{
    void M1()
    {
        Enum /*<bind>*/e = 1/*</bind>*/;
    }
}

";
            string expectedOperationTree = @"
IVariableDeclarationStatement (1 declarations) (OperationKind.VariableDeclarationStatement, IsInvalid) (Syntax: 'Enum /*<bin ... *</bind>*/;')
  IVariableDeclaration (1 variables) (OperationKind.VariableDeclaration, IsInvalid) (Syntax: 'Enum /*<bin ... *</bind>*/;')
    Variables: Local_1: System.Enum e
    Initializer: IConversionExpression (ConversionKind.Invalid, Implicit) (OperationKind.ConversionExpression, Type: System.Enum, IsInvalid) (Syntax: '1')
        ILiteralExpression (Text: 1) (OperationKind.LiteralExpression, Type: System.Int32, Constant: 1) (Syntax: '1')
";
            var expectedDiagnostics = new DiagnosticDescription[] {
                // CS0029: Cannot implicitly convert type 'int' to 'System.Enum'
                //         Enum /*<bind>*/e = 1/*</bind>*/;
                Diagnostic(ErrorCode.ERR_NoImplicitConv, "1").WithArguments("int", "System.Enum").WithLocation(13, 28)
            };

            VerifyOperationTreeAndDiagnosticsForTest<VariableDeclaratorSyntax>(source, expectedOperationTree, expectedDiagnostics,
                AdditionalOperationTreeVerifier: new ExpectedSymbolVerifier().Verify);
        }

        [Fact]
        public void ConversionExpression_Implicit_DynamicConversionToClass()
        {
            string source = @"
class S1
{
    void M1()
    {
        dynamic d1 = 1;
        string /*<bind>*/s1 = d1/*</bind>*/;
    }
}
";
            string expectedOperationTree = @"
IVariableDeclarationStatement (1 declarations) (OperationKind.VariableDeclarationStatement) (Syntax: 'string /*<b ... *</bind>*/;')
  IVariableDeclaration (1 variables) (OperationKind.VariableDeclaration) (Syntax: 'string /*<b ... *</bind>*/;')
    Variables: Local_1: System.String s1
    Initializer: IConversionExpression (ConversionKind.CSharp, Implicit) (OperationKind.ConversionExpression, Type: System.String) (Syntax: 'd1')
        ILocalReferenceExpression: d1 (OperationKind.LocalReferenceExpression, Type: dynamic) (Syntax: 'd1')
";
            var expectedDiagnostics = DiagnosticDescription.None;

            VerifyOperationTreeAndDiagnosticsForTest<VariableDeclaratorSyntax>(source, expectedOperationTree, expectedDiagnostics,
                AdditionalOperationTreeVerifier: new ExpectedSymbolVerifier().Verify);
        }

        [Fact]
        public void ConversionExpression_Implicit_DynamicConversionToValueType()
        {
            string source = @"
class S1
{
    void M1()
    {
        dynamic d1 = null;
        int /*<bind>*/i1 = d1/*</bind>*/;
    }
}
";
            string expectedOperationTree = @"
IVariableDeclarationStatement (1 declarations) (OperationKind.VariableDeclarationStatement) (Syntax: 'int /*<bind ... *</bind>*/;')
  IVariableDeclaration (1 variables) (OperationKind.VariableDeclaration) (Syntax: 'int /*<bind ... *</bind>*/;')
    Variables: Local_1: System.Int32 i1
    Initializer: IConversionExpression (ConversionKind.CSharp, Implicit) (OperationKind.ConversionExpression, Type: System.Int32) (Syntax: 'd1')
        ILocalReferenceExpression: d1 (OperationKind.LocalReferenceExpression, Type: dynamic) (Syntax: 'd1')
";
            var expectedDiagnostics = DiagnosticDescription.None;

            VerifyOperationTreeAndDiagnosticsForTest<VariableDeclaratorSyntax>(source, expectedOperationTree, expectedDiagnostics,
                AdditionalOperationTreeVerifier: new ExpectedSymbolVerifier().Verify);
        }

        [Fact]
        public void ConversionExpression_Implicit_ConstantExpressionConversion()
        {
            string source = @"
class S1
{
    void M1()
    {
        const int i1 = 1;
        const sbyte /*<bind>*/s1 = i1/*</bind>*/;
    }
}
";
            string expectedOperationTree = @"
IVariableDeclarationStatement (1 declarations) (OperationKind.VariableDeclarationStatement) (Syntax: 'const sbyte ... *</bind>*/;')
  IVariableDeclaration (1 variables) (OperationKind.VariableDeclaration) (Syntax: 'const sbyte ... *</bind>*/;')
    Variables: Local_1: System.SByte s1
    Initializer: IConversionExpression (ConversionKind.CSharp, Implicit) (OperationKind.ConversionExpression, Type: System.SByte, Constant: 1) (Syntax: 'i1')
        ILocalReferenceExpression: i1 (OperationKind.LocalReferenceExpression, Type: System.Int32, Constant: 1) (Syntax: 'i1')
";
            var expectedDiagnostics = new DiagnosticDescription[] {
                // CS0219: The variable 's1' is assigned but its value is never used
                //         const sbyte /*<bind>*/s1 = i1/*</bind>*/;
                Diagnostic(ErrorCode.WRN_UnreferencedVarAssg, "s1").WithArguments("s1").WithLocation(7, 31)
            };

            VerifyOperationTreeAndDiagnosticsForTest<VariableDeclaratorSyntax>(source, expectedOperationTree, expectedDiagnostics,
                    AdditionalOperationTreeVerifier: new ExpectedSymbolVerifier().Verify);
        }

        [Fact]
        public void ConversionExpression_Implicit_ConstantExpressionConversion_InvalidValueTooLarge()
        {
            string source = @"
class S1
{
    void M1()
    {
        const int i1 = 0x1000;
        const sbyte /*<bind>*/s1 = i1/*</bind>*/;
    }
}
";
            string expectedOperationTree = @"
IVariableDeclarationStatement (1 declarations) (OperationKind.VariableDeclarationStatement, IsInvalid) (Syntax: 'const sbyte ... *</bind>*/;')
  IVariableDeclaration (1 variables) (OperationKind.VariableDeclaration, IsInvalid) (Syntax: 'const sbyte ... *</bind>*/;')
    Variables: Local_1: System.SByte s1
    Initializer: IConversionExpression (ConversionKind.CSharp, Implicit) (OperationKind.ConversionExpression, Type: System.SByte, IsInvalid) (Syntax: 'i1')
        ILocalReferenceExpression: i1 (OperationKind.LocalReferenceExpression, Type: System.Int32, Constant: 4096) (Syntax: 'i1')
";
            var expectedDiagnostics = new DiagnosticDescription[] {
                // CS0031: Constant value '4096' cannot be converted to a 'sbyte'
                //         const sbyte /*<bind>*/s1 = i1/*</bind>*/;
                Diagnostic(ErrorCode.ERR_ConstOutOfRange, "i1").WithArguments("4096", "sbyte").WithLocation(7, 36)
            };

            VerifyOperationTreeAndDiagnosticsForTest<VariableDeclaratorSyntax>(source, expectedOperationTree, expectedDiagnostics,
                AdditionalOperationTreeVerifier: new ExpectedSymbolVerifier().Verify);
        }

        [Fact]
        public void ConversionExpression_Implicit_ConstantExpressionConversion_InvalidNonConstantExpression()
        {
            string source = @"
class S1
{
    void M1()
    {
        int i1 = 0;
        const sbyte /*<bind>*/s1 = i1/*</bind>*/;
    }
}
";
            string expectedOperationTree = @"
IVariableDeclarationStatement (1 declarations) (OperationKind.VariableDeclarationStatement, IsInvalid) (Syntax: 'const sbyte ... *</bind>*/;')
  IVariableDeclaration (1 variables) (OperationKind.VariableDeclaration, IsInvalid) (Syntax: 'const sbyte ... *</bind>*/;')
    Variables: Local_1: System.SByte s1
    Initializer: IConversionExpression (ConversionKind.CSharp, Implicit) (OperationKind.ConversionExpression, Type: System.SByte, IsInvalid) (Syntax: 'i1')
        ILocalReferenceExpression: i1 (OperationKind.LocalReferenceExpression, Type: System.Int32) (Syntax: 'i1')
";
            var expectedDiagnostics = new DiagnosticDescription[] {
                // CS0266: Cannot implicitly convert type 'int' to 'sbyte'. An explicit conversion exists (are you missing a cast?)
                //         const sbyte /*<bind>*/s1 = i1/*</bind>*/;
                Diagnostic(ErrorCode.ERR_NoImplicitConvCast, "i1").WithArguments("int", "sbyte").WithLocation(7, 36)
            };

            VerifyOperationTreeAndDiagnosticsForTest<VariableDeclaratorSyntax>(source, expectedOperationTree, expectedDiagnostics,
                AdditionalOperationTreeVerifier: new ExpectedSymbolVerifier().Verify);
        }

        [Fact]
        public void ConversionExpression_Implicit_UserDefinedConversion()
        {
            string source = @"
class C1
{
    void M1()
    {
        C2 /*<bind>*/c2 = this/*</bind>*/;
    }
}

class C2
{
    public static implicit operator C2(C1 c1)
    {
        return null;
    }
}
";
            string expectedOperationTree = @"
IVariableDeclarationStatement (1 declarations) (OperationKind.VariableDeclarationStatement) (Syntax: 'C2 /*<bind> ... *</bind>*/;')
  IVariableDeclaration (1 variables) (OperationKind.VariableDeclaration) (Syntax: 'C2 /*<bind> ... *</bind>*/;')
    Variables: Local_1: C2 c2
    Initializer: IConversionExpression (ConversionKind.OperatorMethod, Implicit) (OperatorMethod: C2 C2.op_Implicit(C1 c1)) (OperationKind.ConversionExpression, Type: C2) (Syntax: 'this')
        IInstanceReferenceExpression (InstanceReferenceKind.Explicit) (OperationKind.InstanceReferenceExpression, Type: C1) (Syntax: 'this')
";
            var expectedDiagnostics = DiagnosticDescription.None;

            VerifyOperationTreeAndDiagnosticsForTest<VariableDeclaratorSyntax>(source, expectedOperationTree, expectedDiagnostics,
                AdditionalOperationTreeVerifier: new ExpectedSymbolVerifier().Verify);
        }

        [Fact]
        public void ConversionExpression_Implicit_UserDefinedMultiImplicitStepConversion()
        {
            string source = @"
class C1
{
    void M1()
    {
        int i1 = 1;
        C2 /*<bind>*/c2 = i1/*</bind>*/;
    }
}

class C2
{
    public static implicit operator C2(long c1)
    {
        return null;
    }
}
";
            string expectedOperationTree = @"
IVariableDeclarationStatement (1 declarations) (OperationKind.VariableDeclarationStatement) (Syntax: 'C2 /*<bind> ... *</bind>*/;')
  IVariableDeclaration (1 variables) (OperationKind.VariableDeclaration) (Syntax: 'C2 /*<bind> ... *</bind>*/;')
    Variables: Local_1: C2 c2
    Initializer: IConversionExpression (ConversionKind.OperatorMethod, Implicit) (OperatorMethod: C2 C2.op_Implicit(System.Int64 c1)) (OperationKind.ConversionExpression, Type: C2) (Syntax: 'i1')
        IConversionExpression (ConversionKind.CSharp, Implicit) (OperationKind.ConversionExpression, Type: System.Int64) (Syntax: 'i1')
          ILocalReferenceExpression: i1 (OperationKind.LocalReferenceExpression, Type: System.Int32) (Syntax: 'i1')
";
            var expectedDiagnostics = DiagnosticDescription.None;

            VerifyOperationTreeAndDiagnosticsForTest<VariableDeclaratorSyntax>(source, expectedOperationTree, expectedDiagnostics,
                AdditionalOperationTreeVerifier: new ExpectedSymbolVerifier()
                {
                    ConversionChildSelector = (conversion) => ((IConversionExpression)conversion.Operand).Operand
                }.Verify);
        }

        [Fact]
        public void ConversionExpression_Implicit_UserDefinedMultiImplicitAndExplicitStepConversion()
        {
            string source = @"
class C1
{
    void M1()
    {
        int i1 = 1;
        C2 /*<bind>*/c2 = (int)this/*</bind>*/;
    }

    public static implicit operator int(C1 c1)
    {
        return 1;
    }
}

class C2
{
    public static implicit operator C2(long c1)
    {
        return null;
    }
}
";
            string expectedOperationTree = @"
IVariableDeclarationStatement (1 declarations) (OperationKind.VariableDeclarationStatement) (Syntax: 'C2 /*<bind> ... *</bind>*/;')
  IVariableDeclaration (1 variables) (OperationKind.VariableDeclaration) (Syntax: 'C2 /*<bind> ... *</bind>*/;')
    Variables: Local_1: C2 c2
    Initializer: IConversionExpression (ConversionKind.OperatorMethod, Implicit) (OperatorMethod: C2 C2.op_Implicit(System.Int64 c1)) (OperationKind.ConversionExpression, Type: C2) (Syntax: '(int)this')
        IConversionExpression (ConversionKind.CSharp, Implicit) (OperationKind.ConversionExpression, Type: System.Int64) (Syntax: '(int)this')
          IConversionExpression (ConversionKind.OperatorMethod, Explicit) (OperatorMethod: System.Int32 C1.op_Implicit(C1 c1)) (OperationKind.ConversionExpression, Type: System.Int32) (Syntax: '(int)this')
            IInstanceReferenceExpression (InstanceReferenceKind.Explicit) (OperationKind.InstanceReferenceExpression, Type: C1) (Syntax: 'this')
";
            var expectedDiagnostics = new DiagnosticDescription[] {
                // CS0219: The variable 'i1' is assigned but its value is never used
                //         int i1 = 1;
                Diagnostic(ErrorCode.WRN_UnreferencedVarAssg, "i1").WithArguments("i1").WithLocation(6, 13)
            };

            VerifyOperationTreeAndDiagnosticsForTest<VariableDeclaratorSyntax>(source, expectedOperationTree, expectedDiagnostics);
        }

        [Fact]
        public void ConversionExpression_Implicit_UserDefinedMultiImplicitAndExplicitStepConversion_InvalidMissingExplicitConversion()
        {
            string source = @"
class C1
{
    void M1()
    {
        int i1 = 1;
        C2 /*<bind>*/c2 = this/*</bind>*/;
    }

    public static implicit operator int(C1 c1)
    {
        return 1;
    }
}

class C2
{
    public static implicit operator C2(long c1)
    {
        return null;
    }
}
";
            string expectedOperationTree = @"
IVariableDeclarationStatement (1 declarations) (OperationKind.VariableDeclarationStatement, IsInvalid) (Syntax: 'C2 /*<bind> ... *</bind>*/;')
  IVariableDeclaration (1 variables) (OperationKind.VariableDeclaration, IsInvalid) (Syntax: 'C2 /*<bind> ... *</bind>*/;')
    Variables: Local_1: C2 c2
    Initializer: IConversionExpression (ConversionKind.Invalid, Implicit) (OperationKind.ConversionExpression, Type: C2, IsInvalid) (Syntax: 'this')
        IInstanceReferenceExpression (InstanceReferenceKind.Explicit) (OperationKind.InstanceReferenceExpression, Type: C1) (Syntax: 'this')
";
            var expectedDiagnostics = new DiagnosticDescription[] {
                // CS0029: Cannot implicitly convert type 'C1' to 'C2'
                //         C2 /*<bind>*/c2 = this/*</bind>*/;
                Diagnostic(ErrorCode.ERR_NoImplicitConv, "this").WithArguments("C1", "C2").WithLocation(7, 27),
                // CS0219: The variable 'i1' is assigned but its value is never used
                //         int i1 = 1;
                Diagnostic(ErrorCode.WRN_UnreferencedVarAssg, "i1").WithArguments("i1").WithLocation(6, 13)
            };

            VerifyOperationTreeAndDiagnosticsForTest<VariableDeclaratorSyntax>(source, expectedOperationTree, expectedDiagnostics);
        }

        [Fact]
        public void ConversionExpression_Implicit_UserDefinedMultipleCandidateConversion()
        {
            string source = @"
class C1
{
}

class C2 : C1
{
    void M1()
    {
        C3 /*<bind>*/c3 = this/*</bind>*/;
    }
}

class C3
{
    public static implicit operator C3(C1 c1)
    {
        return null;
    }

    public static implicit operator C3(C2 c2)
    {
        return null;
    }
}
";
            string expectedOperationTree = @"
IVariableDeclarationStatement (1 declarations) (OperationKind.VariableDeclarationStatement) (Syntax: 'C3 /*<bind> ... *</bind>*/;')
  IVariableDeclaration (1 variables) (OperationKind.VariableDeclaration) (Syntax: 'C3 /*<bind> ... *</bind>*/;')
    Variables: Local_1: C3 c3
    Initializer: IConversionExpression (ConversionKind.OperatorMethod, Implicit) (OperatorMethod: C3 C3.op_Implicit(C2 c2)) (OperationKind.ConversionExpression, Type: C3) (Syntax: 'this')
        IInstanceReferenceExpression (InstanceReferenceKind.Explicit) (OperationKind.InstanceReferenceExpression, Type: C2) (Syntax: 'this')
";
            var expectedDiagnostics = DiagnosticDescription.None;

            VerifyOperationTreeAndDiagnosticsForTest<VariableDeclaratorSyntax>(source, expectedOperationTree, expectedDiagnostics,
                AdditionalOperationTreeVerifier: new ExpectedSymbolVerifier().Verify);
        }

        [Fact]
        public void ConversionExpression_Implicit_DelegateExpressionWithoutParamsToDelegateConversion()
        {
            string source = @"
using System;

class S1
{
    void M1()
    {
        Action /*<bind>*/a = delegate { }/*</bind>*/;
    }
}
";
            string expectedOperationTree = @"
IVariableDeclarationStatement (1 declarations) (OperationKind.VariableDeclarationStatement) (Syntax: 'Action /*<b ... *</bind>*/;')
  IVariableDeclaration (1 variables) (OperationKind.VariableDeclaration) (Syntax: 'Action /*<b ... *</bind>*/;')
    Variables: Local_1: System.Action a
    Initializer: IConversionExpression (ConversionKind.CSharp, Implicit) (OperationKind.ConversionExpression, Type: System.Action) (Syntax: 'delegate { }')
        ILambdaExpression (Signature: lambda expression) (OperationKind.LambdaExpression, Type: null) (Syntax: 'delegate { }')
          IBlockStatement (1 statements) (OperationKind.BlockStatement) (Syntax: '{ }')
            IReturnStatement (OperationKind.ReturnStatement) (Syntax: '{ }')
";
            var expectedDiagnostics = DiagnosticDescription.None;

            VerifyOperationTreeAndDiagnosticsForTest<VariableDeclaratorSyntax>(source, expectedOperationTree, expectedDiagnostics,
                AdditionalOperationTreeVerifier: new ExpectedSymbolVerifier().Verify);
        }

        [Fact]
        public void ConversionExpression_Implicit_DelegateExpressionWithParamsToDelegateConversion()
        {
            string source = @"
using System;

class S1
{
    void M1()
    {
        Action<int> /*<bind>*/a = delegate(int i) { }/*</bind>*/;
    }
}
";
            string expectedOperationTree = @"
IVariableDeclarationStatement (1 declarations) (OperationKind.VariableDeclarationStatement) (Syntax: 'Action<int> ... *</bind>*/;')
  IVariableDeclaration (1 variables) (OperationKind.VariableDeclaration) (Syntax: 'Action<int> ... *</bind>*/;')
    Variables: Local_1: System.Action<System.Int32> a
    Initializer: IConversionExpression (ConversionKind.CSharp, Implicit) (OperationKind.ConversionExpression, Type: System.Action<System.Int32>) (Syntax: 'delegate(int i) { }')
        ILambdaExpression (Signature: lambda expression) (OperationKind.LambdaExpression, Type: null) (Syntax: 'delegate(int i) { }')
          IBlockStatement (1 statements) (OperationKind.BlockStatement) (Syntax: '{ }')
            IReturnStatement (OperationKind.ReturnStatement) (Syntax: '{ }')
";
            var expectedDiagnostics = DiagnosticDescription.None;

            VerifyOperationTreeAndDiagnosticsForTest<VariableDeclaratorSyntax>(source, expectedOperationTree, expectedDiagnostics,
                AdditionalOperationTreeVerifier: new ExpectedSymbolVerifier().Verify);
        }

        [Fact]
        public void ConversionExpression_Implicit_DelegateExpressionWithParamsToDelegateConversion_InvalidMismatchedTypes()
        {
            string source = @"
using System;

class S1
{
    void M1()
    {
        Action<int> /*<bind>*/a = delegate() { }/*</bind>*/;
    }
}
";
            string expectedOperationTree = @"
IVariableDeclarationStatement (1 declarations) (OperationKind.VariableDeclarationStatement, IsInvalid) (Syntax: 'Action<int> ... *</bind>*/;')
  IVariableDeclaration (1 variables) (OperationKind.VariableDeclaration, IsInvalid) (Syntax: 'Action<int> ... *</bind>*/;')
    Variables: Local_1: System.Action<System.Int32> a
    Initializer: IConversionExpression (ConversionKind.Invalid, Implicit) (OperationKind.ConversionExpression, Type: System.Action<System.Int32>, IsInvalid) (Syntax: 'delegate() { }')
        ILambdaExpression (Signature: lambda expression) (OperationKind.LambdaExpression, Type: null) (Syntax: 'delegate() { }')
          IBlockStatement (0 statements) (OperationKind.BlockStatement) (Syntax: '{ }')
";
            var expectedDiagnostics = new DiagnosticDescription[] {
                // CS1593: Delegate 'Action<int>' does not take 0 arguments
                //         Action<int> /*<bind>*/a = delegate() { }/*</bind>*/;
                Diagnostic(ErrorCode.ERR_BadDelArgCount, "delegate() { }").WithArguments("System.Action<int>", "0").WithLocation(8, 35)
            };

            VerifyOperationTreeAndDiagnosticsForTest<VariableDeclaratorSyntax>(source, expectedOperationTree, expectedDiagnostics,
                AdditionalOperationTreeVerifier: new ExpectedSymbolVerifier().Verify);
        }

        [Fact]
        public void ConversionExpression_Implicit_PointerFromNullConversion()
        {
            string source = @"
using System;

class S1
{
    unsafe void M1()
    {
        void* /*<bind>*/v1 = null/*</bind>*/;
    }
}
";
            string expectedOperationTree = @"
IVariableDeclarationStatement (1 declarations) (OperationKind.VariableDeclarationStatement) (Syntax: 'void* /*<bi ... *</bind>*/;')
  IVariableDeclaration (1 variables) (OperationKind.VariableDeclaration) (Syntax: 'void* /*<bi ... *</bind>*/;')
    Variables: Local_1: System.Void* v1
    Initializer: IConversionExpression (ConversionKind.CSharp, Implicit) (OperationKind.ConversionExpression, Type: System.Void*) (Syntax: 'null')
        ILiteralExpression (Text: null) (OperationKind.LiteralExpression, Type: null, Constant: null) (Syntax: 'null')
";
            var expectedDiagnostics = DiagnosticDescription.None;
            VerifyOperationTreeAndDiagnosticsForTest<VariableDeclaratorSyntax>(source, expectedOperationTree, expectedDiagnostics,
                compilationOptions: TestOptions.UnsafeReleaseDll,
                AdditionalOperationTreeVerifier: new ExpectedSymbolVerifier().Verify);
        }

        [Fact]
        public void ConversionExpression_Implicit_PointerToVoidConversion()
        {
            string source = @"
using System;

class S1
{
    unsafe void M1()
    {
        int* i1 = null;
        void* /*<bind>*/v1 = i1/*</bind>*/;
    }
}
";
            string expectedOperationTree = @"
IVariableDeclarationStatement (1 declarations) (OperationKind.VariableDeclarationStatement) (Syntax: 'void* /*<bi ... *</bind>*/;')
  IVariableDeclaration (1 variables) (OperationKind.VariableDeclaration) (Syntax: 'void* /*<bi ... *</bind>*/;')
    Variables: Local_1: System.Void* v1
    Initializer: IConversionExpression (ConversionKind.CSharp, Implicit) (OperationKind.ConversionExpression, Type: System.Void*) (Syntax: 'i1')
        ILocalReferenceExpression: i1 (OperationKind.LocalReferenceExpression, Type: System.Int32*) (Syntax: 'i1')
";
            var expectedDiagnostics = DiagnosticDescription.None;

            VerifyOperationTreeAndDiagnosticsForTest<VariableDeclaratorSyntax>(source, expectedOperationTree, expectedDiagnostics,
                compilationOptions: TestOptions.UnsafeReleaseDll,
                AdditionalOperationTreeVerifier: new ExpectedSymbolVerifier().Verify);
        }

        [Fact]
        public void ConversionExpression_Implicit_PointerFromVoidConversion_Invalid()
        {
            string source = @"
using System;

class S1
{
    unsafe void M1()
    {
        void* v1 = null;
        int* /*<bind>*/i1 = v1/*</bind>*/;
    }
}
";
            string expectedOperationTree = @"
IVariableDeclarationStatement (1 declarations) (OperationKind.VariableDeclarationStatement, IsInvalid) (Syntax: 'int* /*<bin ... *</bind>*/;')
  IVariableDeclaration (1 variables) (OperationKind.VariableDeclaration, IsInvalid) (Syntax: 'int* /*<bin ... *</bind>*/;')
    Variables: Local_1: System.Int32* i1
    Initializer: IConversionExpression (ConversionKind.CSharp, Implicit) (OperationKind.ConversionExpression, Type: System.Int32*, IsInvalid) (Syntax: 'v1')
        ILocalReferenceExpression: v1 (OperationKind.LocalReferenceExpression, Type: System.Void*) (Syntax: 'v1')
";
            var expectedDiagnostics = new DiagnosticDescription[] {
                // CS0266: Cannot implicitly convert type 'void*' to 'int*'. An explicit conversion exists (are you missing a cast?)
                //         int* /*<bind>*/i1 = v1/*</bind>*/;
                Diagnostic(ErrorCode.ERR_NoImplicitConvCast, "v1").WithArguments("void*", "int*").WithLocation(9, 29)
            };

            VerifyOperationTreeAndDiagnosticsForTest<VariableDeclaratorSyntax>(source, expectedOperationTree, expectedDiagnostics,
                compilationOptions: TestOptions.UnsafeReleaseDll,
                AdditionalOperationTreeVerifier: new ExpectedSymbolVerifier().Verify);
        }

        [Fact]
        public void ConversionExpression_Implicit_PointerFromIntegerConversion_Invalid()
        {
            string source = @"
using System;

class S1
{
    unsafe void M1()
    {
        void* /*<bind>*/v1 = 0/*</bind>*/;
    }
}
";
            string expectedOperationTree = @"
IVariableDeclarationStatement (1 declarations) (OperationKind.VariableDeclarationStatement, IsInvalid) (Syntax: 'void* /*<bi ... *</bind>*/;')
  IVariableDeclaration (1 variables) (OperationKind.VariableDeclaration, IsInvalid) (Syntax: 'void* /*<bi ... *</bind>*/;')
    Variables: Local_1: System.Void* v1
    Initializer: IConversionExpression (ConversionKind.CSharp, Implicit) (OperationKind.ConversionExpression, Type: System.Void*, IsInvalid) (Syntax: '0')
        ILiteralExpression (Text: 0) (OperationKind.LiteralExpression, Type: System.Int32, Constant: 0) (Syntax: '0')
";
            var expectedDiagnostics = new DiagnosticDescription[] {
                // CS0266: Cannot implicitly convert type 'int' to 'void*'. An explicit conversion exists (are you missing a cast?)
                //         void* /*<bind>*/v1 = 0/*</bind>*/;
                Diagnostic(ErrorCode.ERR_NoImplicitConvCast, "0").WithArguments("int", "void*").WithLocation(8, 30),
            };

            VerifyOperationTreeAndDiagnosticsForTest<VariableDeclaratorSyntax>(source, expectedOperationTree, expectedDiagnostics,
                compilationOptions: TestOptions.UnsafeReleaseDll,
                AdditionalOperationTreeVerifier: new ExpectedSymbolVerifier().Verify);
        }

        [Fact]
        public void ConversionExpression_Implicit_ExpressionTreeConversion()
        {
            string source = @"
using System;
using System.Linq.Expressions;

class Program
{
    static void Main(string[] args)
    {
        Expression<Func<int, bool>> /*<bind>*/exp = num => num < 5/*</bind>*/;
    }
}
";
            string expectedOperationTree = @"
IVariableDeclarationStatement (1 declarations) (OperationKind.VariableDeclarationStatement) (Syntax: 'Expression< ... *</bind>*/;')
  IVariableDeclaration (1 variables) (OperationKind.VariableDeclaration) (Syntax: 'Expression< ... *</bind>*/;')
    Variables: Local_1: System.Linq.Expressions.Expression<System.Func<System.Int32, System.Boolean>> exp
    Initializer: IConversionExpression (ConversionKind.CSharp, Implicit) (OperationKind.ConversionExpression, Type: System.Linq.Expressions.Expression<System.Func<System.Int32, System.Boolean>>) (Syntax: 'num => num < 5')
        ILambdaExpression (Signature: lambda expression) (OperationKind.LambdaExpression, Type: null) (Syntax: 'num => num < 5')
          IBlockStatement (1 statements) (OperationKind.BlockStatement) (Syntax: 'num < 5')
            IReturnStatement (OperationKind.ReturnStatement) (Syntax: 'num < 5')
              IBinaryOperatorExpression (BinaryOperationKind.IntegerLessThan) (OperationKind.BinaryOperatorExpression, Type: System.Boolean) (Syntax: 'num < 5')
                Left: IParameterReferenceExpression: num (OperationKind.ParameterReferenceExpression, Type: System.Int32) (Syntax: 'num')
                Right: ILiteralExpression (Text: 5) (OperationKind.LiteralExpression, Type: System.Int32, Constant: 5) (Syntax: '5')
";
            var expectedDiagnostics = DiagnosticDescription.None;

            VerifyOperationTreeAndDiagnosticsForTest<VariableDeclaratorSyntax>(source, expectedOperationTree, expectedDiagnostics,
                AdditionalOperationTreeVerifier: new ExpectedSymbolVerifier().Verify);
        }

        [Fact]
        public void ConversionExpression_Implicit_ExpressionTreeConversion_InvalidIncorrectLambdaType()
        {
            string source = @"
using System;
using System.Linq.Expressions;

class Program
{
    static void Main(string[] args)
    {
        Expression<Func<int, bool>> /*<bind>*/exp = num => num/*</bind>*/;
    }
}
";
            string expectedOperationTree = @"
IVariableDeclarationStatement (1 declarations) (OperationKind.VariableDeclarationStatement, IsInvalid) (Syntax: 'Expression< ... *</bind>*/;')
  IVariableDeclaration (1 variables) (OperationKind.VariableDeclaration, IsInvalid) (Syntax: 'Expression< ... *</bind>*/;')
    Variables: Local_1: System.Linq.Expressions.Expression<System.Func<System.Int32, System.Boolean>> exp
    Initializer: IConversionExpression (ConversionKind.Invalid, Implicit) (OperationKind.ConversionExpression, Type: System.Linq.Expressions.Expression<System.Func<System.Int32, System.Boolean>>, IsInvalid) (Syntax: 'num => num')
        ILambdaExpression (Signature: lambda expression) (OperationKind.LambdaExpression, Type: null, IsInvalid) (Syntax: 'num => num')
          IBlockStatement (1 statements) (OperationKind.BlockStatement, IsInvalid) (Syntax: 'num')
            IReturnStatement (OperationKind.ReturnStatement, IsInvalid) (Syntax: 'num')
              IConversionExpression (ConversionKind.Invalid, Implicit) (OperationKind.ConversionExpression, Type: System.Boolean, IsInvalid) (Syntax: 'num')
                IParameterReferenceExpression: num (OperationKind.ParameterReferenceExpression, Type: System.Int32) (Syntax: 'num')
";
            var expectedDiagnostics = new DiagnosticDescription[] {
                // CS0029: Cannot implicitly convert type 'int' to 'bool'
                //         Expression<Func<int, bool>> /*<bind>*/exp = num => num/*</bind>*/;
                Diagnostic(ErrorCode.ERR_NoImplicitConv, "num").WithArguments("int", "bool").WithLocation(9, 60),
                // CS1662: Cannot convert lambda expression to intended delegate type because some of the return types in the block are not implicitly convertible to the delegate return type
                //         Expression<Func<int, bool>> /*<bind>*/exp = num => num/*</bind>*/;
                Diagnostic(ErrorCode.ERR_CantConvAnonMethReturns, "num").WithArguments("lambda expression").WithLocation(9, 60)
            };

            // Due to https://github.com/dotnet/roslyn/issues/20291, we cannot verify that the types of the ioperation tree and the sematic model
            // match, as they do not actually match.
            VerifyOperationTreeAndDiagnosticsForTest<VariableDeclaratorSyntax>(source, expectedOperationTree, expectedDiagnostics); //,
                //AdditionalOperationTreeVerifier: new ExpectedSymbolVerifier().Verify);
        }

        [Fact]
        public void ConversionExpression_Implicit_ExpressionTreeConversion_InvalidSyntax()
        {
            string source = @"
using System;
using System.Linq.Expressions;

class Program
{
    static void Main(string[] args)
    {
        Expression<Func<int, bool>> /*<bind>*/exp = num =>/*</bind>*/;
    }
}
";
            string expectedOperationTree = @"
IVariableDeclarationStatement (1 declarations) (OperationKind.VariableDeclarationStatement, IsInvalid) (Syntax: 'Expression< ... *</bind>*/;')
  IVariableDeclaration (1 variables) (OperationKind.VariableDeclaration, IsInvalid) (Syntax: 'Expression< ... *</bind>*/;')
    Variables: Local_1: System.Linq.Expressions.Expression<System.Func<System.Int32, System.Boolean>> exp
    Initializer: IConversionExpression (ConversionKind.CSharp, Implicit) (OperationKind.ConversionExpression, Type: System.Linq.Expressions.Expression<System.Func<System.Int32, System.Boolean>>, IsInvalid) (Syntax: 'num =>/*</bind>*/')
        ILambdaExpression (Signature: lambda expression) (OperationKind.LambdaExpression, Type: null, IsInvalid) (Syntax: 'num =>/*</bind>*/')
          IBlockStatement (1 statements) (OperationKind.BlockStatement, IsInvalid) (Syntax: '')
            IReturnStatement (OperationKind.ReturnStatement, IsInvalid) (Syntax: '')
              IConversionExpression (ConversionKind.Invalid, Implicit) (OperationKind.ConversionExpression, Type: System.Boolean, IsInvalid) (Syntax: '')
                IInvalidExpression (OperationKind.InvalidExpression, Type: ?, IsInvalid) (Syntax: '')
";
            var expectedDiagnostics = new DiagnosticDescription[] {
                // CS1525: Invalid expression term ';'
                //         Expression<Func<int, bool>> /*<bind>*/exp = num =>/*</bind>*/;
                Diagnostic(ErrorCode.ERR_InvalidExprTerm, ";").WithArguments(";").WithLocation(9, 70)
            };

            VerifyOperationTreeAndDiagnosticsForTest<VariableDeclaratorSyntax>(source, expectedOperationTree, expectedDiagnostics,
                AdditionalOperationTreeVerifier: new ExpectedSymbolVerifier().Verify);
        }

        #endregion

        private class ExpectedSymbolVerifier
        {
            public static SyntaxNode VariableDeclaratorSelector(SyntaxNode syntaxNode) =>
                ((VariableDeclaratorSyntax)syntaxNode).Initializer.Value;

            public static SyntaxNode IdentitySelector(SyntaxNode syntaxNode) => syntaxNode;

            public static IConversionExpression IVariableDeclarationStatementSelector(IOperation operation) =>
                (IConversionExpression)((IVariableDeclarationStatement)operation).Declarations.Single().Initializer;

            public Func<IOperation, IConversionExpression> OperationSelector { get; set; } = IVariableDeclarationStatementSelector;

            public Func<IConversionExpression, IOperation> ConversionChildSelector { get; set; } = (conversion) => conversion.Operand;

            public Func<SyntaxNode, SyntaxNode> SyntaxSelector { get; set; } = VariableDeclaratorSelector;

            /// <summary>
            /// Verifies that the given operation has the type information that the semantic model has for the given
            /// syntax node. A selector is used to walk the operation tree and syntax tree for the final
            /// nodes to compare type info for.
            ///
            /// <see cref="SyntaxSelector"/> is used to to select the syntax node to test.
            /// <see cref="OperationSelector"/> is used to select the IConversion node to test.
            /// <see cref="ConversionChildSelector"/> is used to select what child node of the IConversion to compare original types to.
            /// this is useful for multiple conversion scenarios where we end up with multiple IConversion nodes in the tree.
            /// </summary>
            public void Verify(IOperation operation, Compilation compilation, SyntaxNode syntaxNode)
            {
                switch (operation.Kind)
                {
                    case OperationKind.VariableDeclarationStatement:
                        VerifyVariableDeclarationStatement((IVariableDeclarationStatement)operation, compilation, syntaxNode);
                        break;
                    default:
                        Assert.False(true, $"Unexpected kind of statement {operation.Kind}");
                        break;
                }
            }

            private void VerifyVariableDeclarationStatement(IVariableDeclarationStatement variableDeclaration, Compilation compilation, SyntaxNode syntaxNode)
            {
                var finalSyntax = SyntaxSelector(syntaxNode);
                var semanticModel = compilation.GetSemanticModel(finalSyntax.SyntaxTree);
                var typeInfo = semanticModel.GetTypeInfo(finalSyntax);

                var initializer = OperationSelector(variableDeclaration);

                var conversion = initializer;
                Assert.Equal(conversion.Type, typeInfo.ConvertedType);
                Assert.Equal(ConversionChildSelector(conversion).Type, typeInfo.Type);
            }
        }
    }
}
