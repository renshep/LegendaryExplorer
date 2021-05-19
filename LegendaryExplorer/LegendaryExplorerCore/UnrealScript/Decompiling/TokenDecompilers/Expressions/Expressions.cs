﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using LegendaryExplorerCore.Helpers;
using LegendaryExplorerCore.Packages;
using LegendaryExplorerCore.Unreal.BinaryConverters;
using LegendaryExplorerCore.UnrealScript.Language.ByteCode;
using LegendaryExplorerCore.UnrealScript.Language.Tree;
using LegendaryExplorerCore.UnrealScript.Utilities;
using static LegendaryExplorerCore.UnrealScript.Utilities.Keywords;

namespace LegendaryExplorerCore.UnrealScript.Decompiling
{
    public partial class ByteCodeDecompiler
    {

        public Expression DecompileExpression()
        {
            StartPositions.Push((ushort)Position);
            var token = PeekByte;

            if (token >= extNativeIndex)
            {
                PopByte();
                int nativeIndex;
                if ((token & 0xF0) == extNativeIndex) //Extended Native
                {
                    byte b2 = ReadByte();
                    nativeIndex = ((token - extNativeIndex) << 8) + b2;
                }
                else
                {
                    nativeIndex = token; //Native
                }
                return DecompileNativeFunction(nativeIndex);
            }

            switch (token)
            {
                // variable lookups
                case (byte)OpCodes.DefaultVariable:

                    PopByte();
                    return DecompileDefaultReference();
                case (byte)OpCodes.LocalVariable:
                case (byte)OpCodes.InstanceVariable:
                case (byte)OpCodes.LocalOutVariable:
                case (byte)OpCodes.LocalFloatVariable:
                case (byte)OpCodes.LocalIntVariable:
                case (byte)OpCodes.LocalByteVariable:
                case (byte)OpCodes.LocalObjectVariable: 
                case (byte)OpCodes.InstanceFloatVariable:
                case (byte)OpCodes.InstanceIntVariable:
                case (byte)OpCodes.InstanceByteVariable:
                case (byte)OpCodes.InstanceObjectVariable:
                    PopByte();
                    return DecompileObjectLookup();

                case (byte)OpCodes.Nothing:
                    PopByte();
                    StartPositions.Pop();
                    return DecompileExpression(); // TODO, solve this better? What about variable assignments etc?

                // array[index]
                case (byte)OpCodes.DynArrayElement: //TODO: possibly separate this
                case (byte)OpCodes.ArrayElement:
                    return DecompileArrayRef();

                // new (...) class (.)
                case (byte)OpCodes.New:
                    return DecompileNew();

                // (class|object|struct).member
                case (byte)OpCodes.ClassContext: // TODO: support in AST
                    return DecompileContext(isClass:true);

                case (byte)OpCodes.Context:
                    return DecompileContext();

                case (byte)OpCodes.StructMember:
                    return DecompileStructMember();

                // unknown, interface
                case (byte)OpCodes.InterfaceContext:
                    PopByte();
                    StartPositions.Pop();
                    return DecompileExpression(); // TODO: research this

                // class<Name>(Obj)
                case(byte)OpCodes.Metacast:
                    return DecompileCast(meta:true); // TODO: ugly hack to make this qork quickly

                // Self
                case (byte)OpCodes.Self:
                    PopByte();
                    StartPositions.Pop();
                    return new SymbolReference(null, SELF, null, null); // TODO: solve better

                // Skip(numBytes)
                case (byte)OpCodes.Skip: // handles skips in operator arguments
                    PopByte();
                    ReadInt16(); // MemSize
                    StartPositions.Pop();
                    return DecompileExpression();

                // Function calls
                case (byte)OpCodes.FinalFunction:
                    return DecompileFunctionCall();

                case (byte)OpCodes.GlobalFunction:
                    return DecompileFunctionCall(byName: true, global: true); // TODO: is this correct?

                case (byte)OpCodes.VirtualFunction:
                    return DecompileFunctionCall(byName: true);

                // int, eg. 5
                case (byte)OpCodes.IntConst:
                    return DecompileIntConst();

                // float, eg. 5.5
                case (byte)OpCodes.FloatConst:
                    return DecompileFloatConst();

                // "string"
                case (byte)OpCodes.StringConst:
                    return DecompileStringConst();

                // Object
                case (byte)OpCodes.ObjectConst:
                    return DecompileObjectConst();

                // 'name'
                case (byte)OpCodes.NameConst:
                    return DecompileNameConst();

                // rot(1, 2, 3)
                case (byte)OpCodes.RotationConst:
                    return DecompileRotationConst();

                // vect(1.0, 2.0, 3.0)
                case (byte)OpCodes.VectorConst:
                    return DecompileVectorConst();

                // byte, eg. 0B
                case (byte)OpCodes.ByteConst:
                    return DecompileByteConst(BYTE);
                case (byte)OpCodes.IntConstByte:
                    return DecompileByteConst(INT);

                // 0
                case (byte)OpCodes.IntZero:
                    return DecompileIntConstVal(0);

                // 1
                case (byte)OpCodes.IntOne:
                    return DecompileIntConstVal(1);

                // true
                case (byte)OpCodes.True:
                    return DecompileBoolConstVal(true);

                // false
                case (byte)OpCodes.False:
                    return DecompileBoolConstVal(false);

                // None    (object literal)
                case (byte)OpCodes.NoObject:
                case (byte)OpCodes.EmptyDelegate:
                    PopByte();
                    StartPositions.Pop();
                    return new NoneLiteral();

                // (bool expression)
                case (byte)OpCodes.BoolVariable:
                    return DecompileBoolExprValue();

                // ClassName(Obj)
                case (byte)OpCodes.InterfaceCast:
                case (byte)OpCodes.DynamicCast:
                    return DecompileCast();

                // struct == struct 
                case (byte)OpCodes.StructCmpEq:
                    return DecompileStructComparison(true);

                // struct != struct 
                case (byte)OpCodes.StructCmpNe:
                    return DecompileStructComparison(false);

                // delegate == delegate 
                case (byte)OpCodes.EqualEqual_DelDel:
                    return DecompileDelegateComparison(true);

                // delegate != delegate 
                case (byte)OpCodes.NotEqual_DelDel:
                    return DecompileDelegateComparison(false);

                // delegate == Function
                case (byte)OpCodes.EqualEqual_DelFunc:
                    return DecompileDelegateComparison(true);

                // delegate != Function 
                case (byte)OpCodes.NotEqual_DelFunc:
                    return DecompileDelegateComparison(false);

                // primitiveType(expr)
                case (byte)OpCodes.PrimitiveCast:
                    return DecompilePrimitiveCast();

                // (bool expr) ? expr : expr
                case (byte)OpCodes.Conditional:
                    return DecompileConditionalExpression();

                // end of script
                case (byte)OpCodes.EndOfScript:
                    return null; // ERROR?

                // (empty function param)
                case (byte)OpCodes.EmptyParmValue:
                    PopByte();
                    StartPositions.Pop();
                    return new SymbolReference(null, ""); // TODO: solve better

                // arrayName.Length
                case (byte)OpCodes.DynArrayLength:
                    return DecompileDynArrLength();

                // arrayName.Find(value)
                case (byte)OpCodes.DynArrayFind:
                    return DecompileDynArrayFind();

                // arrayName.Find(StructProperty, value)
                case (byte)OpCodes.DynArrayFindStruct:
                    return DecompileDynArrayFindStructMember();

                // arrayName.Insert(Index, Count)
                case (byte)OpCodes.DynArrayInsert:
                    return DecompileDynArrayInsert();

                // arrayName.Remove(Index, Count)
                case (byte)OpCodes.DynArrayRemove:
                    return DecompileDynArrayRemove();

                // arrayName.Add(value)
                case (byte)OpCodes.DynArrayAdd:
                    return DecompileDynArrayAdd();

                // arrayName.AddItem(value)
                case (byte)OpCodes.DynArrayAddItem:
                    return DecompileDynArrayAddItem();

                // arrayName.RemoveItem(value)
                case (byte)OpCodes.DynArrayRemoveItem:
                    return DecompileDynArrayRemoveItem();

                // arrayName.InsertItem(StructProperty, value)
                case (byte)OpCodes.DynArrayInsertItem:
                    return DecompileDynArrayInsertItem();

                // arrayName.Sort(value)
                case (byte)OpCodes.DynArraySort:
                    return DecompileDynArraySort();

                // TODO: temporary delegate handling, probably wrong:
                case (byte)OpCodes.DelegateFunction:
                    return DecompileDelegateFunction();

                case (byte)OpCodes.DelegateProperty:
                    return DecompileDelegateProperty();

                case (byte)OpCodes.NamedFunction:
                    return DecompileFunctionCall(byName: true, withFuncListIdx: true);

                case (byte)OpCodes.StringRefConst:
                    return DecompileStringRefConst();



                /*****
                 * TODO: all of these needs changes, see functions below.
                 * */
                #region Unsupported

                case (byte)OpCodes.NativeParm: // is this even present anywhere?
                    return DecompileNativeParm();

                case (byte)OpCodes.GoW_DefaultValue:
                    return DecompileGoW_DefaultValue();

                case (byte)OpCodes.InstanceDelegate:
                    return DecompileInstanceDelegate();

                #endregion


                // TODO: 41, debugInfo

                // TODO: 0x3B - 0x3E native calls

                default:

                    // ERROR!
                    break;
            }

            return null;
        }

        #region Decompilers

        public Expression DecompileObjectLookup()
        {
            var obj = ReadObject();
            if (obj == null)
                return null; // ERROR

            StartPositions.Pop();
            ASTNode node = null;

            //attempt to resolve Enum references so that byte constants can be converted to enum values
            if (obj.ClassName == "ByteProperty")
            {
                if (LibInitialized)
                {
                    IEntry typeExp = obj.Parent;
                    string scope = null;
                    while (typeExp.ClassName != "Class" && typeExp.ClassName != "ScriptStruct")
                    {
                        scope = scope is null ? typeExp.ObjectName.Name : $"{typeExp.ObjectName}.{scope}";
                        typeExp = typeExp.Parent;
                    }

                    if (ReadOnlySymbolTable.TryGetType(typeExp.ObjectName, out VariableType cls))
                    {
                        scope = scope is null ? cls.GetScope() : $"{cls.GetScope()}.{scope}";
                        if (ReadOnlySymbolTable.TryGetSymbolFromSpecificScope(obj.ObjectName, out ASTNode astNode, scope)
                         && astNode is VariableDeclaration {VarType: Enumeration enumeration})
                        {
                            node = enumeration;
                        }
                    }
                }
                if (node is null && obj is ExportEntry exp && PCC.GetEntry(exp.GetBinaryData<UByteProperty>().Enum) is IEntry enumEntry)
                {
                    if (enumEntry is ExportEntry enumExp)
                    {
                        node = ScriptObjectToASTConverter.ConvertEnum(enumExp.GetBinaryData<UEnum>());
                    }
                    else if (LibInitialized && ReadOnlySymbolTable.TryGetType(enumEntry.ObjectName, out Enumeration enumeration))
                    {
                        node = enumeration;
                    }
                }
            }

            return new SymbolReference(node, obj.ObjectName.Instanced);
        }

        public Expression DecompileDefaultReference()
        {
            var obj = ReadObject();
            if (obj == null)
                return null; // ERROR

            StartPositions.Pop();
            return new DefaultReference(null, obj.ObjectName.Instanced);
        }

        public Expression DecompileContext(bool isClass = false)
        {
            PopByte();

            var left = DecompileExpression();
            if (left == null)
                return null; // ERROR

            ReadInt16(); // discard MemSize value. (memory size of expr-right)
            if (Game >= MEGame.ME3)
            {
                ReadObject(); // discard RetValRef.
            }
            var propType = ReadByte(); // discard propType.

            isInContextExpression = true;
            var right = DecompileExpression();
            if (right == null)
                return null; // ERROR
            isInContextExpression = false;

            //testing code TODO: remove when done testing
            if (false)//Game <= MEGame.ME2)
            {
                if (right is not PrimitiveCast && propType is not (0 or 1 or 4 or 8 or 12 or 0x24))
                {
                    string message = $"proptype: {propType} for expression of type {right.GetType()}";
                    Debugger.Log(0, "", $"{message}\n");
                }
            }

            StartPositions.Pop();
            switch (right)
            {
                //A const accessed by an instance of another class can be compiled as EX_Context(instancevar, literal),
                //which, if decompiled naively, can lead to nonsense like: Weapon.-1
                //in such a case, the context expression can be safely replaced with the literal
                case IntegerLiteral _:
                case FloatLiteral _:
                case BooleanLiteral _:
                case StringLiteral _:
                case NameLiteral _:
                case StringRefLiteral _:
                    return right;
                default:
                    return new CompositeSymbolRef(left, right, isClass);
            }
        }

        public Expression DecompileStructMember()
        {
            PopByte();

            var member = DecompileObjectLookup();
            var StructRef = ReadObject();

            bool needsCopy = ReadByte() > 0; // is accessed through rvalue
            bool isModified = ReadByte() > 0;
            //if (needsCopy || isModified)
            //{
            //    Debugger.Log(0,"", $"#{DataContainer.Export.UIndex} {Path.GetFileNameWithoutExtension(PCC.FilePath)}\n");
            //}
            var expr = DecompileExpression(); // get the expression for struct instance
            if (expr == null)
                return null; // ERROR

            //StartPositions.Pop(); Occurs in DecompileObjectLookup
            return new CompositeSymbolRef(expr, member);
        }

        public Expression DecompileArrayRef()
        {
            PopByte();

            var index = DecompileExpression();
            if (index == null)
                return null; // ERROR

            var arrayExpr = DecompileExpression();
            if (arrayExpr == null)
                return null; // ERROR

            StartPositions.Pop();
            return new ArraySymbolRef(arrayExpr, index, null, null);
        }

        public Expression DecompileBoolExprValue()
        {
            PopByte(); 

            var value = DecompileExpression();
            if (value == null)
                return null; // ERROR

            StartPositions.Pop();
            return value; 
        }

        public DelegateComparison DecompileDelegateComparison(bool isEqual)
        {
            PopByte();

            var left = DecompileExpression();
            if (left == null)
                return null; // ERROR

            var right = DecompileExpression();
            if (right == null)
                return null; // ERROR

            PopByte();//EndOfParms

            StartPositions.Pop();
            return new DelegateComparison(isEqual, left, right);
        }

        public StructComparison DecompileStructComparison(bool isEqual)
        {
            PopByte();

            ReadObject(); // struct type?

            var left = DecompileExpression();
            if (left == null)
                return null; // ERROR

            var right = DecompileExpression();
            if (right == null)
                return null; // ERROR

            StartPositions.Pop();
            return new StructComparison(isEqual, left, right);
        }

        public Expression DecompileConditionalExpression()
        {
            PopByte();

            var cond = DecompileExpression();
            if (cond == null)
                return null; // ERROR

            ReadInt16(); // MemSizeA

            var trueExpr = DecompileExpression();
            if (trueExpr == null)
                return null; // ERROR

            ReadInt16(); // MemSizeB

            var falseExpr = DecompileExpression();
            if (falseExpr == null)
                return null; // ERROR

            StartPositions.Pop();
            return new ConditionalExpression(cond, trueExpr, falseExpr, null, null);
        }

        public Expression DecompileNativeFunction(int index)
        {
            var parameters = new List<Expression>();
            while (!CurrentIs(OpCodes.EndFunctionParms))
            {
                var param = DecompileExpression();
                if (param == null)
                    return null; // ERROR

                parameters.Add(param);
            }
            PopByte();

            var entry = NativeTable[index];
            Expression call = null;

            switch (entry.Type)
            {
                case NativeType.Function:
                    var func = new SymbolReference(null, entry.Name);
                    call = new FunctionCall(func, parameters, null, null);
                    break;

                case NativeType.Operator:
                    var op = new InOpDeclaration(entry.Name, entry.Precedence, index, null, null, null);
                    var opRef = new InOpReference(op, parameters[0], parameters[1]);
                    DecompileEnumOperatorComparisons(opRef);
                    call = opRef;
                    break;

                case NativeType.PreOperator:
                    var preOp = new PreOpDeclaration(entry.Name, null, index, null);
                    call = new PreOpReference(preOp, parameters[0]);
                    break;

                case NativeType.PostOperator:
                    var postOp = new PostOpDeclaration(entry.Name, null, index, null);
                    call = new PostOpReference(postOp, parameters[0], null, null);
                    break;
            }

            StartPositions.Pop();
            return call;
        }

        private static void DecompileEnumOperatorComparisons(InOpReference opRef)
        {
            switch (opRef.Operator.OperatorKeyword)
            {
                case "==":
                case "!=":
                case ">":
                case "<":
                case ">=":
                case "<=":
                    if (!ResolveEnumValues(ref opRef.LeftOperand, ref opRef.RightOperand))
                    {
                        ResolveEnumValues(ref opRef.RightOperand, ref opRef.LeftOperand);
                    }
                    break;
            }

        }
        static bool ResolveEnumValues(ref Expression a, ref Expression b)
        {
            SymbolReference symRef = a as SymbolReference;
            if (symRef is null && a is CastExpression cast && cast.CastType.Name == INT)
            {
                symRef = cast.CastTarget as SymbolReference;
            }

            if (symRef?.Node is Enumeration enm && b is IntegerLiteral intLit && intLit.Value >= 0 && intLit.Value < enm.Values.Count)
            {
                a = symRef;
                b = new SymbolReference(enm.Values[intLit.Value], enm.Values[intLit.Value].Name);
                return true;
            }

            return false;
        }

        public Expression DecompileCast(bool meta = false)
        {
            PopByte();
            var objRef = ReadObject();
            var expr = DecompileExpression();
            if (expr == null)
                return null; // ERROR


            StartPositions.Pop();
            if (expr is NoneLiteral)
            {
                return expr;
            }
            VariableType type = new VariableType(objRef.ObjectName.Instanced);
            if (meta)
            {
                type = new ClassType(type);
            }
            return new CastExpression(type, expr);
        }

        public Expression DecompilePrimitiveCast()
        {
            PopByte();
            var typeToken = ReadByte();

            var expr = DecompileExpression();
            if (expr == null)
                return null; // ERROR
            
            string type = PrimitiveCastTable[typeToken];

            StartPositions.Pop();
            if (typeToken == (byte)ECast.ByteToInt && expr is IntegerLiteral || typeToken == (byte)ECast.InterfaceToObject)
            {
                return expr;
            }

            //re-enable this once testing is complete
            //if (typeToken == (byte)ECast.IntToFloat && expr is IntegerLiteral shouldBeFloat)
            //{
            //    return new FloatLiteral(shouldBeFloat.Value);
            //}
            return new PrimitiveCast((ECast)typeToken, new VariableType(type), expr, null, null);
        }

        public Expression DecompileFunctionCall(bool byName = false, bool withFuncListIdx = false, bool global = false)
        {
            PopByte();
            string funcName;
            bool isSuper = false;
            VariableType superSpecifier = null;
            if (byName)
            {
                funcName = ReadNameReference();
            }
            else
            {
                var funcObj = ReadObject();
                funcName = funcObj.ObjectName.Instanced;

                if (Decompiling.ByteCodeDecompiler.NonNativeOperators.TryGetValue(funcName, out InOpDeclaration opDecl))
                {
                    Expression parm1 = DecompileExpression();
                    if (parm1 is null)
                    {
                        return null;
                    }
                    Expression parm2 = DecompileExpression();
                    if (parm2 is null)
                    {
                        return null;
                    }
                    PopByte();//EndFunctionParms

                    StartPositions.Pop();
                    return new InOpReference(opDecl, parm1, parm2);
                }
                
                if (IsSuper(funcObj)) // If we're calling ourself, it's a super call
                {
                    var classExp = DataContainer.Export.Parent;
                    while (classExp != null && classExp.ClassName != "Class")
                    {
                        classExp = classExp.Parent;
                    }
                    var currentClass = (classExp as ExportEntry).GetBinaryData<UClass>();
                    classExp = funcObj;
                    while (classExp != null && classExp.ClassName != "Class")
                    {
                        classExp = classExp.Parent;
                    }
                    var funcOuterClass = classExp?.ObjectName.Instanced;
                    isSuper = true;
                    if (currentClass == null || currentClass.SuperClass == 0 || currentClass.SuperClass.GetEntry(PCC).ObjectName.Instanced != funcOuterClass)
                    {
                        superSpecifier = new VariableType(funcOuterClass);
                    }
                }
            }

            if (withFuncListIdx)
                ReadInt16();

            List<Expression> parameters = DecompileArgumentList();
            if (parameters is null)
            {
                return null;
            }

            StartPositions.Pop();
            var func = new SymbolReference(null, funcName)
            {
                IsGlobal = global,
                IsSuper = isSuper,
                SuperSpecifier = superSpecifier
            };
            return new FunctionCall(func, parameters, null, null);
        }

        private List<Expression> DecompileArgumentList()
        {
            var parameters = new List<Expression>();
            while (!CurrentIs(OpCodes.EndFunctionParms))
            {
                if (CurrentIs(OpCodes.Nothing) || CurrentIs(OpCodes.EmptyParmValue))
                {
                    PopByte(); // TODO: is this reasonable? what does it mean?
                    parameters.Add(null);
                    continue;
                }

                var param = DecompileExpression();
                if (param == null)
                    return null;

                parameters.Add(param);
            }

            PopByte();
            return parameters;
        }

        bool IsSuper(IEntry funcObj)
        {
            //can't be a super call if it's being called on a specific object
            if (isInContextExpression)
            {
                return false;
            }

            string funcName = funcObj.ObjectName.Instanced;
            ExportEntry parentContextExport = funcObj.Parent as ExportEntry;

            //if the function being called is in the current context, it's not a super call
            if (parentContextExport == DataContainer.Export.Parent)
            {
                return false;
            }

            //if it's in a parent context, it's a super call if there is a function with the same name in the current context
            return IEntryExtensions.GetChildren(DataContainer.Export.Parent).Any(child => child.ObjectName.Instanced.CaseInsensitiveEquals(funcName));
        }

        public Expression DecompileNew()
        {
            PopByte();
            var parms = new List<Expression>();
            for (int n = 0; n < 5; n++)
            {
                if (CurrentIs(OpCodes.Nothing))
                {
                    PopByte();
                    parms.Add(null);
                    continue;
                }

                var param = DecompileExpression();
                if (param == null)
                    return null; // ERROR

                parms.Add(param);
            }

            StartPositions.Pop();
            return new NewOperator(parms[0], parms[1], parms[2], parms[3], parms.Count > 4 ? parms[4] : null);
        }

        public Expression DecompileDelegateFunction()
        {
            PopByte();
            var delegateProp = DecompileExpression();
            if (!(delegateProp is SymbolReference symRef)) return null;

            var delegateTypeName = ReadNameReference();

            var args = DecompileArgumentList();
            if (args == null) return null;

            StartPositions.Pop();
            return new DelegateCall(symRef, args);
        }

        public Expression DecompileDelegateProperty()
        {
            PopByte();
            var name = ReadNameReference();
            if (Game >= MEGame.ME3)
            {
                var obj = ReadObject();
            } 

            StartPositions.Pop();
            if (string.Equals(name.Name, "None", StringComparison.OrdinalIgnoreCase))
            {
                return new NoneLiteral();
            }
            return new SymbolReference(null, name);
        }

#endregion

        /*
         * TODO: All of these need verification and changes
         * */
        #region UnsuportedDecompilers

        public Expression DecompileGoW_DefaultValue()
        {
            PopByte();
            var unkn = ReadByte();
            var expr = DecompileExpression();

            StartPositions.Pop();
            var op = new InOpDeclaration("", 0, 0, null, null, null);
            var objRef = new SymbolReference(null, "UNSUPPORTED: GoW_DefaultValue: Byte:" + unkn + " - ", null, null);
            return new InOpReference(op, objRef, expr, null, null);
        }

        public Expression DecompileNativeParm() // TODO: see code
        {
            PopByte();
            var obj = ReadObject();

            StartPositions.Pop();
            return new SymbolReference(null, "UNSUPPORTED: NativeParm: " + obj.ObjectName.Instanced + " : " + obj.ClassName, null, null);
        }

        public Expression DecompileInstanceDelegate() // TODO: check code, seems ok?
        {
            PopByte();
            var name = ReadNameReference();

            StartPositions.Pop();
            return new SymbolReference(null, name);
        }

        #endregion

    }
}