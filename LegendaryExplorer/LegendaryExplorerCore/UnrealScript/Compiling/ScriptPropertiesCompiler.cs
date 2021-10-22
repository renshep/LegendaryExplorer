﻿using System;
using System.Collections.Generic;
using System.Linq;
using LegendaryExplorerCore.Helpers;
using LegendaryExplorerCore.Packages;
using LegendaryExplorerCore.Packages.CloningImportingAndRelinking;
using LegendaryExplorerCore.Unreal;
using LegendaryExplorerCore.Unreal.BinaryConverters;
using LegendaryExplorerCore.Unreal.ObjectInfo;
using LegendaryExplorerCore.UnrealScript.Language.Tree;
using LegendaryExplorerCore.UnrealScript.Utilities;
using static LegendaryExplorerCore.Unreal.UnrealFlags;

namespace LegendaryExplorerCore.UnrealScript.Compiling
{
    public class ScriptPropertiesCompiler
    {
        private readonly IMEPackage Pcc;
        private ExportEntry Default__Export;
        private ExportEntry Default__Archetype;
        private bool ShouldStripTransients;
        private readonly PackageCache packageCache;
        private bool IsStructDefaults;

        private ScriptPropertiesCompiler(IMEPackage pcc, PackageCache packageCache = null)
        {
            this.packageCache = packageCache;
            Pcc = pcc;
        }

        public static void CompileStructDefaults(Struct structAST, PropertyCollection props, IMEPackage pcc, PackageCache packageCache = null)
        {
            var compiler = new ScriptPropertiesCompiler(pcc, packageCache)
            {
                IsStructDefaults = true
            };

            foreach (Statement statement in structAST.DefaultProperties.Statements)
            {
                props.AddOrReplaceProp(compiler.ConvertToProperty((AssignStatement)statement));
            }
        }

        public static void CompileDefault__Object(DefaultPropertiesBlock defaultsAST, ExportEntry classExport, ref ExportEntry defaultsExport, PackageCache packageCache = null)
        {
            IMEPackage pcc = classExport.FileRef;
            var defaultsExportObjectName = new NameReference($"Default__{classExport.ObjectNameString}", classExport.indexValue);
            if (defaultsExport is null)
            {
                if (pcc.TryGetTrash(out defaultsExport))
                {
                    defaultsExport.Parent = classExport.Parent;
                }
                else
                {
                    defaultsExport = new ExportEntry(pcc, classExport.Parent, defaultsExportObjectName);
                    pcc.AddExport(defaultsExport);
                }
            }
            var cls = (Class)defaultsAST.Outer;

            var compiler = new ScriptPropertiesCompiler(pcc, packageCache)
            {
                Default__Export = defaultsExport
            };

            defaultsExport.SuperClass = null;
            defaultsExport.Class = classExport;
            defaultsExport.ObjectName = defaultsExportObjectName;
            var defaultsExportObjectFlags = EObjectFlags.ClassDefaultObject | EObjectFlags.Public | EObjectFlags.LoadForClient | EObjectFlags.LoadForServer | EObjectFlags.LoadForEdit;
            if (cls.Flags.Has(EClassFlags.PerObjectLocalized) || cls.Flags.Has(EClassFlags.PerObjectConfig) && cls.Flags.Has(EClassFlags.Localized))
            {
                defaultsExportObjectFlags |= EObjectFlags.PerObjectLocalized;
            }
            if ((pcc.Game.IsGame1() || pcc.Game.IsGame2()) && cls.Name is "BioConversation" or "Font" or "MultiFont" or "FaceFXAnimSet" or "BioCreatureSoundSet")
            {
                defaultsExportObjectFlags |= EObjectFlags.LocalizedResource;
            }
            defaultsExport.ObjectFlags = defaultsExportObjectFlags;
            defaultsExport.Archetype = classExport.SuperClass is not null ? compiler.GetClassDefaultObject(classExport.SuperClass) : null;

            compiler.Default__Archetype = defaultsExport.Archetype switch
            {
                ImportEntry defaultArchetypeImport => EntryImporter.ResolveImport(defaultArchetypeImport, packageCache, null, "INT"),
                ExportEntry defaultArchetypeExport => defaultArchetypeExport,
                _ => null
            };

            var props = compiler.ConvertStatementsToPropertyCollection(defaultsAST.Statements, defaultsExport, new Dictionary<NameReference, ExportEntry>());
            
            defaultsExport.WriteProperties(props);
        }

        private PropertyCollection ConvertStatementsToPropertyCollection(List<Statement> statements, ExportEntry export, Dictionary<NameReference, ExportEntry> subObjectDict)
        {
            var existingSubObjects = export.GetChildren<ExportEntry>().ToList();
            var props = new PropertyCollection();
            var subObjectsToFinish = new List<(Subobject, ExportEntry, int)>();
            foreach (Statement statement in statements)
            {
                switch (statement)
                {
                    case Subobject subObj:
                        var subObjName = NameReference.FromInstancedString(subObj.Name.Name);
                        existingSubObjects.TryRemove(exp => exp.ObjectName == subObjName, out ExportEntry existingSubObject);
                        int netIndex = existingSubObject?.NetIndex ?? 0;
                        CreateSubObject(subObj, export, ref existingSubObject);
                        subObjectDict[subObjName] = existingSubObject;
                        subObjectsToFinish.Add(subObj, existingSubObject, netIndex);
                        break;
                    case AssignStatement assignStatement:
                        Property prop = ConvertToProperty(assignStatement, subObjectDict);
                        props.AddOrReplaceProp(prop);
                        break;
                    default:
                        throw new Exception($"Unexpected statement type: {statement.GetType().Name}");
                }
            }
            //Default__ objects and struct defaults serialize transients, but subobjects dont 
            ShouldStripTransients = true;
            foreach ((Subobject subobject, ExportEntry subExport, int netIndex) in subObjectsToFinish)
            {
                WriteSubObjectData(subobject, subExport, netIndex, subObjectDict);
            }

            if (existingSubObjects.Any())
            {
                EntryPruner.TrashEntriesAndDescendants(existingSubObjects);
            }
            return props;
        }

        private void CreateSubObject(Subobject subObject, ExportEntry parent, ref ExportEntry subExport)
        {
            var objName = NameReference.FromInstancedString(subObject.Name.Name);
            IEntry classEntry = EntryImporter.EnsureClassIsInFile(Pcc, subObject.Class.Name);
            if (subExport is null)
            {
                if (Pcc.TryGetTrash(out subExport))
                {
                    subExport.Parent = parent;
                }
                else
                {
                    subExport = new ExportEntry(Pcc, parent, objName);
                    Pcc.AddExport(subExport);
                }
            }
            subExport.SuperClass = null;
            subExport.Class = classEntry;
            subExport.ObjectName = objName;
            subExport.Class = classEntry;

            if (subObject.IsTemplate && Default__Archetype is not null)
            {
                string defaultArchetypePath = Default__Archetype.InstancedFullPath;
                string defaultObjectPath = Default__Export.InstancedFullPath;
                string subObjPath = subExport.InstancedFullPath;
                string subPath = subObjPath[(defaultObjectPath.Length + 1)..];
                ExportEntry subObjArchetype = Default__Archetype.FileRef.FindExport($"{defaultArchetypePath}.{subPath}");
                if (subObjArchetype is null)
                {
                    //sometimes the subobjects have a flat structure under the Default__
                    subPath = objName.Instanced;
                    subObjArchetype = Default__Archetype.FileRef.FindExport($"{defaultArchetypePath}.{subPath}");
                }

                if (subObjArchetype is not null && subObjArchetype.ClassName.CaseInsensitiveEquals(subExport.ClassName))
                {
                    subExport.Archetype = ReferenceEquals(Default__Archetype.FileRef, Pcc)
                        ? subObjArchetype
                        : Pcc.getEntryOrAddImport($"{Default__Export.Archetype.InstancedFullPath}.{subPath}", classEntry.ObjectName.Instanced, classEntry.ParentName);
                    return;
                }
                //sometimes the archetype is a subobject of the Default__ for a parent suboject's class.
                if (parent != Default__Export)
                {
                    var archetypeRoot = parent;
                    while (archetypeRoot.Archetype is not null)
                    {
                        archetypeRoot = (ExportEntry)archetypeRoot.Parent;
                    }
                    subPath = subObjPath[(archetypeRoot.InstancedFullPath.Length + 1)..];
                    IEntry classDefaultObject = GetClassDefaultObject(archetypeRoot.Class);
                    var baseDefault = classDefaultObject switch
                    {
                        ExportEntry exp => exp,
                        ImportEntry imp => EntryImporter.ResolveImport(imp, packageCache, null, "INT"),
                        _ => null
                    };
                    if (baseDefault is not null)
                    {
                        subObjArchetype = baseDefault.FileRef.FindExport($"{baseDefault.InstancedFullPath}.{subPath}");
                        if (subObjArchetype is null)
                        {
                            subPath = objName.Instanced;
                            subObjArchetype = baseDefault.FileRef.FindExport($"{baseDefault.InstancedFullPath}.{subPath}");
                        }
                        if (subObjArchetype is not null && subObjArchetype.ClassName.CaseInsensitiveEquals(subExport.ClassName))
                        {
                            subExport.Archetype = ReferenceEquals(baseDefault.FileRef, Pcc)
                                ? subObjArchetype
                                : Pcc.getEntryOrAddImport($"{classDefaultObject.InstancedFullPath}.{subPath}", classEntry.ObjectName.Instanced, classEntry.ParentName);
                            return;
                        }
                    }
                }

            }
            subExport.Archetype = null;
        }

        private Property ConvertToProperty(AssignStatement assignStatement, Dictionary<NameReference, ExportEntry> subObjectDict = null)
        {
            var nameRef = (SymbolReference) assignStatement.Target;
            int staticArrayIndex = 0;
            if (nameRef is ArraySymbolRef staticArrayRef)
            {
                staticArrayIndex = ((IntegerLiteral) staticArrayRef.Index).Value;
                nameRef = (SymbolReference) staticArrayRef.Array;
            }

            var type = nameRef.ResolveType();
            var literal = assignStatement.Value;
            var propName = NameReference.FromInstancedString(nameRef.Name);
            Property prop = MakeProperty(propName, type, literal, subObjectDict);
            prop.StaticArrayIndex = staticArrayIndex;
            return prop;
        }

        private Property MakeProperty(NameReference propName, VariableType type, Expression literal, Dictionary<NameReference, ExportEntry> subObjectDict = null)
        {
            Property prop;
            if (type is StaticArrayType sat)
            {
                type = sat.ElementType;
            }
            switch (type)
            {
                case ClassType classType:
                    prop = new ObjectProperty(literal is NoneLiteral ? null : CompilerUtils.ResolveClass((Class)((ClassType)((ObjectLiteral)literal).Class).ClassLimiter, Pcc), propName);
                    break;
                case Class cls:
                    IEntry entry = literal switch
                    {
                        NoneLiteral => null,
                        SymbolReference {Node: Subobject subobject} => subObjectDict?[NameReference.FromInstancedString(subobject.Name.Name)],
                        _ => Pcc.FindEntry(((ObjectLiteral)literal).Name.Value)
                    };

                    prop = new ObjectProperty(entry, propName)
                    {
                        InternalPropType = cls.IsInterface ? PropertyType.InterfaceProperty : PropertyType.ObjectProperty
                    };
                    break;
                case DelegateType:
                    int objUIndex = 0;
                    NameReference funcName;
                    if (literal is NoneLiteral)
                    {
                        funcName = "None";
                    }
                    else
                    {
                        if (literal is CompositeSymbolRef csf)
                        {
                            objUIndex = GetClassDefaultObject(CompilerUtils.ResolveClass((Class)((ClassType)((ObjectLiteral)csf.OuterSymbol).Class).ClassLimiter, Pcc)).UIndex;
                            literal = csf.InnerSymbol;
                        }

                        funcName = NameReference.FromInstancedString(((SymbolReference) literal).Name);
                    }
                    prop = new DelegateProperty(objUIndex, funcName, propName);
                    break;
                case DynamicArrayType dynamicArrayType:
                    VariableType elementType = dynamicArrayType.ElementType;
                    var properties = ((DynamicArrayLiteral)literal).Values.Select(lit => MakeProperty(null, elementType, lit, subObjectDict));
                    switch (elementType)
                    {
                        case ClassType:
                        case Class:
                            prop = new ArrayProperty<ObjectProperty>(properties.Cast<ObjectProperty>().ToList(), propName);
                            break;
                        case DelegateType:
                            prop = new ArrayProperty<DelegateProperty>(properties.Cast<DelegateProperty>().ToList(), propName);
                            break;
                        case Enumeration:
                            prop = new ArrayProperty<EnumProperty>(properties.Cast<EnumProperty>().ToList(), propName);
                            break;
                        case Struct:
                            prop = new ArrayProperty<StructProperty>(properties.Cast<StructProperty>().ToList(), propName);
                            break;
                        default:
                            switch (elementType.PropertyType)
                            {
                                case EPropertyType.Byte:
                                    prop = new ArrayProperty<ByteProperty>(properties.Cast<ByteProperty>().ToList(), propName);
                                    break;
                                case EPropertyType.Int:
                                    prop = new ArrayProperty<IntProperty>(properties.Cast<IntProperty>().ToList(), propName);
                                    break;
                                case EPropertyType.Bool:
                                    prop = new ArrayProperty<BoolProperty>(properties.Cast<BoolProperty>().ToList(), propName);
                                    break;
                                case EPropertyType.Float:
                                    prop = new ArrayProperty<FloatProperty>(properties.Cast<FloatProperty>().ToList(), propName);
                                    break;
                                case EPropertyType.Name:
                                    prop = new ArrayProperty<NameProperty>(properties.Cast<NameProperty>().ToList(), propName);
                                    break;
                                case EPropertyType.String:
                                    prop = new ArrayProperty<StrProperty>(properties.Cast<StrProperty>().ToList(), propName);
                                    break;
                                case EPropertyType.StringRef:
                                    prop = new ArrayProperty<StringRefProperty>(properties.Cast<StringRefProperty>().ToList(), propName);
                                    break;
                                default:
                                    throw new ArgumentOutOfRangeException(nameof(elementType.PropertyType), elementType.PropertyType, $"{elementType.PropertyType} is not a valid array element type!");
                            }
                            break;
                    }
                    break;
                case Enumeration enumeration:
                    NameReference value;
                    if (literal is NoneLiteral)
                    {
                        value = "None";
                    }
                    else
                    {
                        value = NameReference.FromInstancedString(((EnumValue) ((SymbolReference) literal).Node).Name);
                    }
                    prop = new EnumProperty(value, NameReference.FromInstancedString(enumeration.Name), Pcc.Game, propName);
                    break;
                case Struct @struct:
                    //todo: Spec says that unspecified properties on a struct value should be inherited from base class's default for that property
                    var structProps = (IsStructDefaults || @struct.IsAtomic) ? @struct.GetDefaultPropertyCollection(Pcc, ShouldStripTransients, packageCache) : new PropertyCollection();
                    foreach (Statement statement in ((StructLiteral)literal).Statements)
                    {
                        structProps.AddOrReplaceProp(ConvertToProperty((AssignStatement)statement, subObjectDict));
                    }
                    prop = new StructProperty(@struct.Name, structProps, propName, @struct.IsImmutable);
                    break;
                default:
                    switch (type.PropertyType)
                    {
                        case EPropertyType.Byte:
                            prop = new ByteProperty((byte) ((IntegerLiteral) literal).Value, propName);
                            break;
                        case EPropertyType.Int:
                            prop = new IntProperty(((IntegerLiteral) literal).Value, propName);
                            break;
                        case EPropertyType.Bool:
                            prop = new BoolProperty(((BooleanLiteral) literal).Value, propName);
                            break;
                        case EPropertyType.Float:
                            prop = new FloatProperty(((FloatLiteral) literal).Value, propName);
                            break;
                        case EPropertyType.Name:
                            prop = new NameProperty(NameReference.FromInstancedString(((NameLiteral) literal).Value), propName);
                            break;
                        case EPropertyType.String:
                            prop = new StrProperty(((StringLiteral) literal).Value, propName);
                            break;
                        case EPropertyType.StringRef:
                            prop = new StringRefProperty(((StringRefLiteral) literal).Value, propName);
                            break;
                        default:
                            throw new ArgumentOutOfRangeException();
                    }

                    break;
            }

            return prop;
        }

        private void WriteSubObjectData(Subobject subObject, ExportEntry subExport, int netIndex, Dictionary<NameReference, ExportEntry> parentSubObjectDict)
        {
            PropertyCollection props = ConvertStatementsToPropertyCollection(subObject.Statements, subExport, new(parentSubObjectDict));
            var binary = ObjectBinary.Create(subExport.ClassName, subExport.Game, props);
            
            //this code should probably be somewhere else, perhaps integrated into ObjectBinary.Create somehow?
            if (binary is BioDynamicAnimSet dynAnimSet && props.GetProp<ArrayProperty<ObjectProperty>>("Sequences") is {} sequences)
            {
                var setName = props.GetProp<NameProperty>("m_nmOrigSetName");
                foreach (ObjectProperty objProp in sequences)
                {
                    switch (objProp.ResolveToEntry(Pcc))
                    {
                        case ExportEntry exportEntry when exportEntry.GetProperty<NameProperty>("SequenceName") is {} seqNameProperty:
                            dynAnimSet.SequenceNamesToUnkMap.Add(seqNameProperty.Value, 1);
                            break;
                        case IEntry entry:
                            if (setName is null)
                            {
                                throw new Exception($"{nameof(BioDynamicAnimSet)} must have m_nmOrigSetName property defined!");
                            }
                            dynAnimSet.SequenceNamesToUnkMap.Add(entry.ObjectName.Instanced[(setName.Value.Instanced.Length + 1)..], 1);
                            break;
                    }
                }
            }


            if (subExport.ClassName is "DominantDirectionalLightComponent" or "DominantSpotLightComponent")
            {
                Span<byte> preProps = stackalloc byte[20];
                const int templateOwnerClass = 0; //todo: When is this not 0?

                EndianBitConverter.WriteAsBytes(0, preProps, Pcc.Endian);
                EndianBitConverter.WriteAsBytes(templateOwnerClass, preProps[4..], Pcc.Endian);
                EndianBitConverter.WriteAsBytes(Pcc.FindNameOrAdd(subExport.ObjectName.Name), preProps[8..], Pcc.Endian);
                EndianBitConverter.WriteAsBytes(subExport.ObjectName.Number, preProps[12..], Pcc.Endian);
                EndianBitConverter.WriteAsBytes(netIndex, preProps[16..], Pcc.Endian);
                subExport.WritePrePropsAndPropertiesAndBinary(preProps.ToArray(), props, binary);
            }
            else if (subObject.Class.IsComponent)
            {
                Span<byte> preProps = stackalloc byte[16];
                const int templateOwnerClass = 0; //todo: When is this not 0?

                EndianBitConverter.WriteAsBytes(templateOwnerClass, preProps, Pcc.Endian);
                EndianBitConverter.WriteAsBytes(Pcc.FindNameOrAdd(subExport.ObjectName.Name), preProps[4..], Pcc.Endian);
                EndianBitConverter.WriteAsBytes(subExport.ObjectName.Number, preProps[8..], Pcc.Endian);
                EndianBitConverter.WriteAsBytes(netIndex, preProps[12..], Pcc.Endian);
                subExport.WritePrePropsAndPropertiesAndBinary(preProps.ToArray(), props, binary);
            }
            else
            {
                subExport.WritePropertiesAndBinary(props, binary);
            }
        }

        private IEntry GetClassDefaultObject(IEntry classEntry)
        {
            if (classEntry is ExportEntry export)
            {
                var classObj = export.GetBinaryData<UClass>(packageCache);
                return classObj.Defaults.GetEntry(Pcc);
            }
            string parentPath = classEntry.ParentInstancedFullPath;
            return Pcc.getEntryOrAddImport($"{parentPath}.Default__{classEntry.ObjectName.Instanced}", classEntry.ObjectName.Instanced, classEntry.ParentName);
        }
    }
}