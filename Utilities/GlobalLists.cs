﻿using ME3Script.Lexing.Matching.StringMatchers;
using ME3Script.Lexing.Tokenizing;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ME3Script.Utilities
{
    public static class GlobalLists
    {
        public static List<KeywordMatcher> Delimiters;
        public static List<KeywordMatcher> Keywords;

        static GlobalLists()
        {
            Delimiters = new List<KeywordMatcher>
            {
                new KeywordMatcher("{", TokenType.LeftBracket, null),
                new KeywordMatcher("}", TokenType.RightBracket, null),
                new KeywordMatcher("[", TokenType.LeftSqrBracket, null),
                new KeywordMatcher("]", TokenType.RightSqrBracket, null),
                new KeywordMatcher("==", TokenType.Equals, null),    
                new KeywordMatcher("+=", TokenType.AddAssign, null),   
                new KeywordMatcher("-=", TokenType.SubAssign, null),   
                new KeywordMatcher("*=", TokenType.MulAssign, null),   
                new KeywordMatcher("/=", TokenType.DivAssign, null),      
                new KeywordMatcher("!=", TokenType.NotEquals, null),  
                new KeywordMatcher("~=", TokenType.ApproxEquals, null), 
                new KeywordMatcher(">>", TokenType.RightShift, null),    
                new KeywordMatcher("<<", TokenType.LeftShift, null),
                new KeywordMatcher("<=", TokenType.LessOrEquals, null),
                new KeywordMatcher(">=", TokenType.GreaterOrEquals, null),
                new KeywordMatcher("**", TokenType.Power, null), 
                new KeywordMatcher("&&", TokenType.And, null),   
                new KeywordMatcher("||", TokenType.Or, null),         
                new KeywordMatcher("^^", TokenType.Xor, null),
                new KeywordMatcher("<", TokenType.LessThan, null),    
                new KeywordMatcher(">", TokenType.GreaterThan, null),         
                new KeywordMatcher("%", TokenType.Modulo, null),
                new KeywordMatcher("$=", TokenType.StrConcatAssign, null),
                new KeywordMatcher("$", TokenType.StrConcat, null),
                new KeywordMatcher("@=", TokenType.StrConcAssSpace, null),
                new KeywordMatcher("@", TokenType.StrConcatSpace, null),
                new KeywordMatcher("-", TokenType.Subract, null),      
                new KeywordMatcher("+", TokenType.Add, null),        
                new KeywordMatcher("*", TokenType.Multiply, null),   
                new KeywordMatcher("/", TokenType.Divide, null),  
                new KeywordMatcher("=", TokenType.Assign, null),  
                new KeywordMatcher("~", TokenType.BinaryNegate, null), 
                new KeywordMatcher("&", TokenType.BinaryAnd, null),    
                new KeywordMatcher("|", TokenType.BinaryOr, null),     
                new KeywordMatcher("^", TokenType.BinaryXor, null),     
                new KeywordMatcher("?", TokenType.Conditional, null),   
                new KeywordMatcher(":", TokenType.Colon, null),
                new KeywordMatcher(";", TokenType.SemiColon, null),
                new KeywordMatcher(",", TokenType.Comma, null),
                new KeywordMatcher(".", TokenType.Dot, null)
            };

            Keywords = new List<KeywordMatcher>
            {
                new KeywordMatcher("VectorCross", TokenType.VectorCross, Delimiters, false),
                new KeywordMatcher("VectorDot", TokenType.VectorDot, Delimiters, false),
                new KeywordMatcher("IsClockwiseFrom", TokenType.IsClockwiseFrom, Delimiters, false),
                new KeywordMatcher("var", TokenType.InstanceVariable, Delimiters, false),
                new KeywordMatcher("local", TokenType.LocalVariable, Delimiters, false),
                new KeywordMatcher("GlobalConfig", TokenType.GlobalConfigSpecifier, Delimiters, false),
                new KeywordMatcher("Config", TokenType.ConfigSpecifier, Delimiters, false),
                new KeywordMatcher("Localized", TokenType.LocalizedSpecifier, Delimiters, false),
                new KeywordMatcher("Const", TokenType.ConstSpecifier, Delimiters, false),
                new KeywordMatcher("PrivateWrite", TokenType.PrivateWriteSpecifier, Delimiters, false),
                new KeywordMatcher("ProtectedWrite", TokenType.ProtectedWriteSpecifier, Delimiters, false),
                new KeywordMatcher("Private", TokenType.PrivateSpecifier, Delimiters, false),
                new KeywordMatcher("Protected", TokenType.ProtectedSpecifier, Delimiters, false),
                new KeywordMatcher("RepNotify", TokenType.RepNotifySpecifier, Delimiters, false),
                new KeywordMatcher("Deprecated", TokenType.DeprecatedSpecifier, Delimiters, false),
                new KeywordMatcher("Instanced", TokenType.InstancedSpecifier, Delimiters, false),
                new KeywordMatcher("Databinding", TokenType.DatabindingSpecifier, Delimiters, false),
                new KeywordMatcher("EditorOnly", TokenType.EditorOnlySpecifier, Delimiters, false),
                new KeywordMatcher("NotForConsole", TokenType.NotForConsoleSpecifier, Delimiters, false),
                new KeywordMatcher("EditConst", TokenType.EditConstSpecifier, Delimiters, false),
                new KeywordMatcher("EditFixedSize", TokenType.EditFixedSizeSpecifier, Delimiters, false),
                new KeywordMatcher("EditInline", TokenType.EditInlineSpecifier, Delimiters, false),
                new KeywordMatcher("EditInlineUse", TokenType.EditInlineUseSpecifier, Delimiters, false),
                new KeywordMatcher("NoClear", TokenType.NoClearSpecifier, Delimiters, false),
                new KeywordMatcher("Interp", TokenType.InterpSpecifier, Delimiters, false),
                new KeywordMatcher("Input", TokenType.InputSpecifier, Delimiters, false),
                new KeywordMatcher("Transient", TokenType.TransientSpecifier, Delimiters, false),
                new KeywordMatcher("DuplicateTransient", TokenType.DuplicateTransientSpecifier, Delimiters, false),
                new KeywordMatcher("NoImport", TokenType.NoImportSpecifier, Delimiters, false),
                new KeywordMatcher("Native", TokenType.NativeSpecifier, Delimiters, false),
                new KeywordMatcher("Export", TokenType.ExportSpecifier, Delimiters, false),
                new KeywordMatcher("NoExport", TokenType.NoExportSpecifier, Delimiters, false),
                new KeywordMatcher("NonTransactional", TokenType.NonTransactionalSpecifier, Delimiters, false),
                new KeywordMatcher("Pointer", TokenType.PointerSpecifier, Delimiters, false),
                new KeywordMatcher("Init", TokenType.InitSpecifier, Delimiters, false),
                new KeywordMatcher("RepRetry", TokenType.RepRetrySpecifier, Delimiters, false),
                new KeywordMatcher("AllowAbstract", TokenType.AllowAbstractSpecifier, Delimiters, false),
                new KeywordMatcher("Out", TokenType.OutSpecifier, Delimiters, false),
                new KeywordMatcher("Coerce", TokenType.CoerceSpecifier, Delimiters, false),
                new KeywordMatcher("Optional", TokenType.OptionalSpecifier, Delimiters, false),
                new KeywordMatcher("Skip", TokenType.SkipSpecifier, Delimiters, false),
                //new KeywordMatcher("byte", TokenType.Byte, Delimiters, false),
                //new KeywordMatcher("int", TokenType.Int, Delimiters, false),
                //new KeywordMatcher("bool", TokenType.Bool, Delimiters, false),
                //new KeywordMatcher("float", TokenType.Float, Delimiters, false),
                new KeywordMatcher("string", TokenType.String, Delimiters, false),
                new KeywordMatcher("enum", TokenType.Enumeration, Delimiters, false),
                new KeywordMatcher("array", TokenType.Array, Delimiters, false),
                new KeywordMatcher("struct", TokenType.Struct, Delimiters, false),
                new KeywordMatcher("class", TokenType.Class, Delimiters, false),
                //new KeywordMatcher("Name", TokenType.Name, Delimiters, false),
                //new KeywordMatcher("Object", TokenType.Object, Delimiters, false),
                //new KeywordMatcher("Actor", TokenType.Actor, Delimiters, false),
                new KeywordMatcher("delegate", TokenType.Delegate, Delimiters, false),
                //new KeywordMatcher("Vector", TokenType.Vector, Delimiters, false),
                //new KeywordMatcher("Rotator", TokenType.Rotator, Delimiters, false),
                new KeywordMatcher("constant", TokenType.Constant, Delimiters, false),
                new KeywordMatcher("None", TokenType.None, Delimiters, false),
                new KeywordMatcher("Self", TokenType.Self, Delimiters, false),
                new KeywordMatcher("EnumCount", TokenType.EnumCount, Delimiters, false),
                new KeywordMatcher("ArrayCount", TokenType.ArrayCount, Delimiters, false),
                new KeywordMatcher("extends", TokenType.Extends, Delimiters, false),
                new KeywordMatcher("within", TokenType.Within, Delimiters, false),
                new KeywordMatcher("public", TokenType.PublicSpecifier, Delimiters, false),
                new KeywordMatcher("final", TokenType.FinalSpecifier, Delimiters, false),
                new KeywordMatcher("exec", TokenType.ExecSpecifier, Delimiters, false),
                new KeywordMatcher("K2Call", TokenType.K2CallSpecifier, Delimiters, false),
                new KeywordMatcher("K2Override", TokenType.K2OverrideSpecifier, Delimiters, false),
                new KeywordMatcher("K2Pure", TokenType.K2PureSpecifier, Delimiters, false),
                new KeywordMatcher("static", TokenType.StaticSpecifier, Delimiters, false),
                new KeywordMatcher("simulated", TokenType.SimulatedSpecifier, Delimiters, false),
                new KeywordMatcher("singular", TokenType.SingularSpecifier, Delimiters, false),
                new KeywordMatcher("client", TokenType.ClientSpecifier, Delimiters, false),
                new KeywordMatcher("DemoRecording", TokenType.DemoRecordingSpecifier, Delimiters, false),
                new KeywordMatcher("reliable", TokenType.ReliableSpecifier, Delimiters, false),
                new KeywordMatcher("server", TokenType.ServerSpecifier, Delimiters, false),
                new KeywordMatcher("unreliable", TokenType.UnreliableSpecifier, Delimiters, false),
                new KeywordMatcher("event", TokenType.Event, Delimiters, false),
                new KeywordMatcher("iterator", TokenType.IteratorSpecifier, Delimiters, false),
                new KeywordMatcher("latent", TokenType.LatentSpecifier, Delimiters, false),
                new KeywordMatcher("state", TokenType.State, Delimiters, false),
                new KeywordMatcher("function", TokenType.Function, Delimiters, false),
                new KeywordMatcher("operator", TokenType.Operator, Delimiters, false),
                new KeywordMatcher("ignores", TokenType.Ignores, Delimiters, false),
                new KeywordMatcher("auto", TokenType.AutoSpecifier, Delimiters, false)
            };
        }

    }
}
