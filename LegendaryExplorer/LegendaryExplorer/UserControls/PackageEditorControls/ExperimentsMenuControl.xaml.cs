﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;
using LegendaryExplorer.Dialogs;
using LegendaryExplorer.Misc;
using LegendaryExplorer.Tools.PackageEditor;
using LegendaryExplorer.Tools.PackageEditor.Experiments;
using LegendaryExplorerCore.GameFilesystem;
using LegendaryExplorerCore.Helpers;
using LegendaryExplorerCore.Misc;
using LegendaryExplorerCore.Packages;
using LegendaryExplorerCore.TLK.ME1;
using LegendaryExplorerCore.Unreal;
using LegendaryExplorerCore.Unreal.BinaryConverters;
using LegendaryExplorerCore.Unreal.ObjectInfo;
using LegendaryExplorerCore.UnrealScript;
using LegendaryExplorerCore.UnrealScript.Compiling.Errors;
using LegendaryExplorerCore.UnrealScript.Language.Tree;
using Microsoft.Win32;
using Microsoft.WindowsAPICodePack.Dialogs;
using Newtonsoft.Json;

namespace LegendaryExplorer.UserControls.PackageEditorControls
{
    /// <summary>
    /// Class that holds toolset development experiments. Actual experiment code should be in the Experiments classes
    /// </summary>
    public partial class ExperimentsMenuControl : MenuItem
    {
        public ExperimentsMenuControl()
        {
            InitializeComponent();
        }

        public PackageEditorWindow GetPEWindow()
        {
            if (Window.GetWindow(this) is PackageEditorWindow pew)
            {
                return pew;
            }

            return null;
        }

        // EXPERIMENTS: GENERAL--------------------------------------------------------------------
        #region General Toolset experiments/debug stuff
        private void RefreshProperties_Clicked(object sender, RoutedEventArgs e)
        {
            var exp = GetPEWindow().InterpreterTab_Interpreter.CurrentLoadedExport;
            var properties = exp?.GetProperties();
        }


        private void BuildME1ObjectInfo_Clicked(object sender, RoutedEventArgs e)
        {
            ME1UnrealObjectInfo.generateInfo(Path.Combine(AppDirectories.ExecFolder, "ME1ObjectInfo.json"));
            GetPEWindow().RestoreAndBringToFront();
            MessageBox.Show(GetPEWindow(), "Done");
        }

        private void BuildME2ObjectInfo_Clicked(object sender, RoutedEventArgs e)
        {
            ME2UnrealObjectInfo.generateInfo(Path.Combine(AppDirectories.ExecFolder, "ME2ObjectInfo.json"));
            GetPEWindow().RestoreAndBringToFront();
            MessageBox.Show(GetPEWindow(), "Done");
        }

        private void BuildME3ObjectInfo_Clicked(object sender, RoutedEventArgs e)
        {
            ME3UnrealObjectInfo.generateInfo(Path.Combine(AppDirectories.ExecFolder, "ME3ObjectInfo.json"));
            GetPEWindow().RestoreAndBringToFront();
            MessageBox.Show(GetPEWindow(), "Done");
        }

        private void BuildAllObjectInfoOT_Clicked(object sender, RoutedEventArgs e)
        {
            ME1UnrealObjectInfo.generateInfo(Path.Combine(AppDirectories.ExecFolder, "ME1ObjectInfo.json"));
            ME2UnrealObjectInfo.generateInfo(Path.Combine(AppDirectories.ExecFolder, "ME2ObjectInfo.json"));
            ME3UnrealObjectInfo.generateInfo(Path.Combine(AppDirectories.ExecFolder, "ME3ObjectInfo.json"));
            GetPEWindow().RestoreAndBringToFront();
            MessageBox.Show(GetPEWindow(), "Done");
        }

        private void BuildLE1ObjectInfo_Clicked(object sender, RoutedEventArgs e)
        {
            var pew = GetPEWindow();
            pew.BusyText = "Building LE1 Object Info";
            pew.IsBusy = true;

            void setProgress(int done, int total)
            {
                Application.Current.Dispatcher.InvokeAsync(() =>
                {
                    pew.BusyText = $"Building LE1 Object Info [{done}/{total}]";
                });
            }

            Task.Run(() =>
            {
                LE1UnrealObjectInfo.generateInfo(Path.Combine(AppDirectories.ExecFolder, "LE1ObjectInfo.json"), true, setProgress);
            }).ContinueWithOnUIThread(x =>
            {
                pew.IsBusy = false;
                pew.RestoreAndBringToFront();
                MessageBox.Show(GetPEWindow(), "Done");
            });
        }


        private void BuildLE2ObjectInfo_Clicked(object sender, RoutedEventArgs e)
        {
            var pew = GetPEWindow();
            pew.BusyText = "Building LE2 Object Info";
            pew.IsBusy = true;

            void setProgress(int done, int total)
            {
                Application.Current.Dispatcher.InvokeAsync(() =>
                {
                    pew.BusyText = $"Building LE2 Object Info [{done}/{total}]";
                });
            }

            Task.Run(() =>
            {
                LE2UnrealObjectInfo.generateInfo(Path.Combine(AppDirectories.ExecFolder, "LE2ObjectInfo.json"), true, setProgress);
            }).ContinueWithOnUIThread(x =>
            {
                pew.IsBusy = false;
                pew.RestoreAndBringToFront();
                MessageBox.Show(GetPEWindow(), "Done");
            });
        }

        private void BuildLE3ObjectInfo_Clicked(object sender, RoutedEventArgs e)
        {
            var pew = GetPEWindow();
            pew.BusyText = "Building LE3 Object Info";
            pew.IsBusy = true;

            void setProgress(int done, int total)
            {
                Application.Current.Dispatcher.InvokeAsync(() =>
                {
                    pew.BusyText = $"Building LE3 Object Info [{done}/{total}]";
                });
            }

            Task.Run(() =>
            {
                LE3UnrealObjectInfo.generateInfo(Path.Combine(AppDirectories.ExecFolder, "LE3ObjectInfo.json"), true, setProgress);
            }).ContinueWithOnUIThread(x =>
            {
                pew.IsBusy = false;
                pew.RestoreAndBringToFront();
                MessageBox.Show(GetPEWindow(), "Done");
            });
        }

        private void BuildAllObjectInfoLE_Clicked(object sender, RoutedEventArgs e)
        {
            var pew = GetPEWindow();
            pew.BusyText = "Building LE Object Info";
            pew.IsBusy = true;

            MEGame currentGame = MEGame.LE1;
            void setProgress(int done, int total)
            {
                Application.Current.Dispatcher.InvokeAsync(() =>
                {
                    pew.BusyText = $"Building {currentGame} Object Info [{done}/{total}]";
                });
            }
            Stopwatch sw = new Stopwatch();

            Task.Run(() =>
            {
                sw.Start();
                LE1UnrealObjectInfo.generateInfo(Path.Combine(AppDirectories.ExecFolder, "LE1ObjectInfo.json"), true, setProgress);
                currentGame = MEGame.LE2;
                LE2UnrealObjectInfo.generateInfo(Path.Combine(AppDirectories.ExecFolder, "LE2ObjectInfo.json"), true, setProgress);
                currentGame = MEGame.LE3;
                LE3UnrealObjectInfo.generateInfo(Path.Combine(AppDirectories.ExecFolder, "LE3ObjectInfo.json"), true, setProgress);
                sw.Stop();
            }).ContinueWithOnUIThread(x =>
            {
                pew.IsBusy = false;
                pew.RestoreAndBringToFront();
                MessageBox.Show(GetPEWindow(), $"Done. Took {sw.Elapsed.TotalSeconds} seconds");
            });




        }
        private void ObjectInfosSearch_Click(object sender, RoutedEventArgs e)
        {
            var searchTerm = PromptDialog.Prompt(GetPEWindow(), "Enter key value to search", "ObjectInfos Search");
            if (searchTerm != null)
            {
                string searchResult = "";

                //ME1
                if (ME1UnrealObjectInfo.Classes.TryGetValue(searchTerm, out ClassInfo _))
                {
                    searchResult += "Key found in ME1 Classes\n";
                }

                if (ME1UnrealObjectInfo.Structs.TryGetValue(searchTerm, out ClassInfo _))
                {
                    searchResult += "Key found in ME1 Structs\n";
                }

                if (ME1UnrealObjectInfo.Enums.TryGetValue(searchTerm, out _))
                {
                    searchResult += "Key found in ME1 Enums\n";
                }

                //ME2
                if (ME2UnrealObjectInfo.Classes.TryGetValue(searchTerm, out ClassInfo _))
                {
                    searchResult += "Key found in ME2 Classes\n";
                }

                if (ME2UnrealObjectInfo.Structs.TryGetValue(searchTerm, out ClassInfo _))
                {
                    searchResult += "Key found in ME2 Structs\n";
                }

                if (ME2UnrealObjectInfo.Enums.TryGetValue(searchTerm, out _))
                {
                    searchResult += "Key found in ME2 Enums\n";
                }

                //ME3
                if (ME3UnrealObjectInfo.Classes.TryGetValue(searchTerm, out ClassInfo _))
                {
                    searchResult += "Key found in ME3 Classes\n";
                }

                if (ME3UnrealObjectInfo.Structs.TryGetValue(searchTerm, out ClassInfo _))
                {
                    searchResult += "Key found in ME3 Structs\n";
                }

                if (ME3UnrealObjectInfo.Enums.TryGetValue(searchTerm, out _))
                {
                    searchResult += "Key found in ME3 Enums\n";
                }

                if (searchResult == "")
                {
                    searchResult = "Key " + searchTerm +
                                   " not found in any ObjectInfo Structs/Classes/Enums dictionaries";
                }
                else
                {
                    searchResult = "Key " + searchTerm + " found in the following:\n" + searchResult;
                }

                MessageBox.Show(searchResult);
            }
        }


        private void GenerateObjectInfoDiff_Click(object sender, RoutedEventArgs e)
        {
            // SirC experiment?
            var enumsDiff = new Dictionary<string, (List<NameReference>, List<NameReference>)>();
            var structsDiff = new Dictionary<string, (ClassInfo, ClassInfo)>();
            var classesDiff = new Dictionary<string, (ClassInfo, ClassInfo)>();

            var immutableME1Structs = ME1UnrealObjectInfo.Structs
                .Where(kvp => ME1UnrealObjectInfo.IsImmutableStruct(kvp.Key))
                .ToDictionary(kvp => kvp.Key, kvp => kvp.Value);
            var immutableME2Structs = ME2UnrealObjectInfo.Structs
                .Where(kvp => ME2UnrealObjectInfo.IsImmutableStruct(kvp.Key))
                .ToDictionary(kvp => kvp.Key, kvp => kvp.Value);
            var immutableME3Structs = ME2UnrealObjectInfo.Structs
                .Where(kvp => ME3UnrealObjectInfo.IsImmutableStruct(kvp.Key))
                .ToDictionary(kvp => kvp.Key, kvp => kvp.Value);

            foreach ((string className, ClassInfo classInfo) in immutableME1Structs)
            {
                if (immutableME2Structs.TryGetValue(className, out ClassInfo classInfo2) &&
                    (!classInfo.properties.SequenceEqual(classInfo2.properties) ||
                     classInfo.baseClass != classInfo2.baseClass))
                {
                    structsDiff.Add(className, (classInfo, classInfo2));
                }

                if (immutableME3Structs.TryGetValue(className, out ClassInfo classInfo3) &&
                    (!classInfo.properties.SequenceEqual(classInfo3.properties) ||
                     classInfo.baseClass != classInfo3.baseClass))
                {
                    structsDiff.Add(className, (classInfo, classInfo3));
                }
            }

            foreach ((string className, ClassInfo classInfo) in immutableME2Structs)
            {
                if (immutableME3Structs.TryGetValue(className, out ClassInfo classInfo3) &&
                    (!classInfo.properties.SequenceEqual(classInfo3.properties) ||
                     classInfo.baseClass != classInfo3.baseClass))
                {
                    structsDiff.Add(className, (classInfo, classInfo3));
                }
            }

            File.WriteAllText(System.IO.Path.Combine(AppDirectories.ExecFolder, "Diff.json"),
                JsonConvert.SerializeObject((immutableME1Structs, immutableME2Structs, immutableME3Structs),
                    Formatting.Indented));
            return;

            var srcEnums = ME2UnrealObjectInfo.Enums;
            var compareEnums = ME3UnrealObjectInfo.Enums;
            var srcStructs = ME2UnrealObjectInfo.Structs;
            var compareStructs = ME3UnrealObjectInfo.Structs;
            var srcClasses = ME2UnrealObjectInfo.Classes;
            var compareClasses = ME3UnrealObjectInfo.Classes;

            foreach ((string enumName, List<NameReference> values) in srcEnums)
            {
                if (!compareEnums.TryGetValue(enumName, out var values2) || !values.SubsetOf(values2))
                {
                    enumsDiff.Add(enumName, (values, values2));
                }
            }

            foreach ((string className, ClassInfo classInfo) in srcStructs)
            {
                if (!compareStructs.TryGetValue(className, out var classInfo2) ||
                    !classInfo.properties.SubsetOf(classInfo2.properties) ||
                    classInfo.baseClass != classInfo2.baseClass)
                {
                    structsDiff.Add(className, (classInfo, classInfo2));
                }
            }

            foreach ((string className, ClassInfo classInfo) in srcClasses)
            {
                if (!compareClasses.TryGetValue(className, out var classInfo2) ||
                    !classInfo.properties.SubsetOf(classInfo2.properties) ||
                    classInfo.baseClass != classInfo2.baseClass)
                {
                    classesDiff.Add(className, (classInfo, classInfo2));
                }
            }

            File.WriteAllText(System.IO.Path.Combine(AppDirectories.ExecFolder, "Diff.json"),
                JsonConvert.SerializeObject(new { enumsDiff, structsDiff, classesDiff }, Formatting.Indented));
        }
        #endregion

        // EXPERIMENTS: MGAMERZ---------------------------------------------------
        #region Mgamerz's Experiments

        private void FindEmptyMips_Clicked(object sender, RoutedEventArgs e)
        {
            PackageEditorExperimentsM.FindEmptyMips(GetPEWindow());
        }

        private void StartPackageBytecodeScan_Click(object sender, RoutedEventArgs e)
        {
            PackageEditorExperimentsM.EnumerateAllFunctions(GetPEWindow());
        }

        private void LODBiasTest_Clicked(object sender, RoutedEventArgs e)
        {
            PackageEditorExperimentsM.TestLODBias(GetPEWindow());
        }

        private void BuildME1TLKDB_Clicked(object sender, RoutedEventArgs e)
        {
            var pew = GetPEWindow();
            string myBasePath = ME1Directory.DefaultGamePath;
            string[] extensions = { ".u", ".upk" };
            FileInfo[] files = new DirectoryInfo(ME1Directory.CookedPCPath)
                .EnumerateFiles("*", SearchOption.AllDirectories)
                .Where(f => extensions.Contains(f.Extension.ToLower()))
                .ToArray();
            int i = 1;
            var stringMapping = new SortedDictionary<int, KeyValuePair<string, List<string>>>();
            foreach (FileInfo f in files)
            {
                pew.StatusBar_LeftMostText.Text = $"[{i}/{files.Length}] Scanning {f.FullName}";
                Dispatcher.Invoke(new Action(() => { }), DispatcherPriority.ContextIdle, null);
                int basePathLen = myBasePath.Length;
                using (IMEPackage pack = MEPackageHandler.OpenMEPackage(f.FullName))
                {
                    List<ExportEntry> tlkExports = pack.Exports.Where(x =>
                        (x.ObjectName == "tlk" || x.ObjectName == "tlk_M") && x.ClassName == "BioTlkFile").ToList();
                    if (tlkExports.Count > 0)
                    {
                        string subPath = f.FullName.Substring(basePathLen);
                        Debug.WriteLine("Found exports in " + f.FullName.Substring(basePathLen));
                        foreach (ExportEntry exp in tlkExports)
                        {
                            var talkFile = new ME1TalkFile(exp);
                            foreach (var sref in talkFile.StringRefs)
                            {
                                if (sref.StringID == 0) continue; //skip blank
                                if (sref.Data == null || sref.Data == "-1" || sref.Data == "") continue; //skip blank

                                if (!stringMapping.TryGetValue(sref.StringID, out var dictEntry))
                                {
                                    dictEntry = new KeyValuePair<string, List<string>>(sref.Data, new List<string>());
                                    stringMapping[sref.StringID] = dictEntry;
                                }

                                if (sref.StringID == 158104)
                                {
                                    Debugger.Break();
                                }

                                dictEntry.Value.Add($"{subPath} in uindex {exp.UIndex} \"{exp.ObjectName}\"");
                            }
                        }
                    }

                    i++;
                }
            }

            int total = stringMapping.Count;
            using (StreamWriter file = new StreamWriter(@"C:\Users\Public\SuperTLK.txt"))
            {
                pew.StatusBar_LeftMostText.Text = "Writing... ";
                Dispatcher.Invoke(new Action(() => { }), DispatcherPriority.ContextIdle, null);
                foreach (KeyValuePair<int, KeyValuePair<string, List<string>>> entry in stringMapping)
                {
                    // do something with entry.Value or entry.Key
                    file.WriteLine(entry.Key);
                    file.WriteLine(entry.Value.Key);
                    foreach (string fi in entry.Value.Value)
                    {
                        file.WriteLine(" - " + fi);
                    }

                    file.WriteLine();
                }
            }

            pew.StatusBar_LeftMostText.Text = "Done";
        }

        private void BuildME1NativeFunctionsInfo_Click(object sender, RoutedEventArgs e)
        {
            PackageEditorExperimentsM.BuildME1NativeFunctionsInfo();
        }

        private void ListNetIndexes_Click(object sender, RoutedEventArgs e)
        {
            PackageEditorExperimentsM.ListNetIndexes(GetPEWindow());
        }


        private void PrintNatives(object sender, RoutedEventArgs e)
        {
            PackageEditorExperimentsM.PrintAllNativeFuncsToDebug(GetPEWindow().Pcc);
        }

        private void FindAllFilesWithSpecificName(object sender, RoutedEventArgs e)
        {
            PackageEditorExperimentsM.FindNamedObject(GetPEWindow());
        }

        private void FindME12DATables_Click(object sender, RoutedEventArgs e)
        {
            PackageEditorExperimentsM.FindME1ME22DATables();
        }

        private void FindAllME3PowerCustomAction_Click(object sender, RoutedEventArgs e)
        {
            PackageEditorExperimentsM.FindAllME3PowerCustomActions();
        }
        private void FindAllME2PowerCustomAction_Click(object sender, RoutedEventArgs e)
        {
            PackageEditorExperimentsM.FindAllME2Powers();
        }


        private void GenerateNewGUIDForPackageFile_Clicked(object sender, RoutedEventArgs e)
        {
            PackageEditorExperimentsM.GenerateNewGUIDForFile(GetPEWindow());
        }

        // Probably not useful in legendary since GetPEWindow() only did stuff for MP
        private void GenerateGUIDCacheForFolder_Clicked(object sender, RoutedEventArgs e)
        {
            PackageEditorExperimentsM.GenerateGUIDCacheForFolder(GetPEWindow());
        }


        private void MakeAllGrenadesAmmoRespawn_Click(object sender, RoutedEventArgs e)
        {
            PackageEditorExperimentsM.MakeAllGrenadesAndAmmoRespawn(GetPEWindow());
        }

        private void SetAllWwiseEventDurations_Click(object sender, RoutedEventArgs e)
        {
            var pew = GetPEWindow();
            pew.BusyText = "Scanning audio and updating events";
            pew.IsBusy = true;
            Task.Run(() => PackageEditorExperimentsM.SetAllWwiseEventDurations(pew.Pcc)).ContinueWithOnUIThread(prevTask =>
            {
                pew.IsBusy = false;
                MessageBox.Show("Wwiseevents updated.");
            });
        }

        private void CompactInFile_Click(object sender, RoutedEventArgs e)
        {
            PackageEditorExperimentsM.CompactFileViaExternalFile(GetPEWindow().Pcc);
        }

        private void ResetPackageTextures_Click(object sender, RoutedEventArgs e)
        {
            PackageEditorExperimentsM.ResetTexturesInFile(GetPEWindow().Pcc, GetPEWindow());
        }

        private void ResetVanillaPackagePart_Click(object sender, RoutedEventArgs e)
        {
            PackageEditorExperimentsM.ResetPackageVanillaPart(GetPEWindow().Pcc, GetPEWindow());
        }

        private void ResolveAllImports_Clicked(object sender, RoutedEventArgs e)
        {
            var pew = GetPEWindow();
            Task.Run(() => PackageEditorExperimentsM.CheckImports(pew.Pcc)).ContinueWithOnUIThread(prevTask =>
            {
                pew.IsBusy = false;
            });
        }

        private void CreateTestPatchDelta_Click(object sender, RoutedEventArgs e)
        {
            PackageEditorExperimentsM.BuildTestPatchComparison();
        }

        private void TintAllNormalizedAverageColor_Clicked(object sender, RoutedEventArgs e)
        {
            PackageEditorExperimentsM.TintAllNormalizedAverageColors(GetPEWindow().Pcc);
        }

        private void DumpAllExecFunctionSignatures_Clicked(object sender, RoutedEventArgs e)
        {
            PackageEditorExperimentsM.DumpAllExecFunctionsFromGame();
        }

        private void RebuildLevelNetindexing_Clicked(object sender, RoutedEventArgs e)
        {
            PackageEditorExperimentsM.RebuildFullLevelNetindexes();
        }

        private void BuildNativeTable_OnClick(object sender, RoutedEventArgs e)
        {
            PackageEditorExperimentsS.BuildNativeTable(GetPEWindow());
        }
        private void ExtractPackageTextures_Click(object sender, RoutedEventArgs e)
        {
            PackageEditorExperimentsM.DumpPackageTextures(GetPEWindow().Pcc, GetPEWindow());
        }

        private void ValidateNavpointChain_Clicked(object sender, RoutedEventArgs e)
        {
            PackageEditorExperimentsM.ValidateNavpointChain(GetPEWindow().Pcc);
        }

        private void TriggerObjBinGetNames_Clicked(object sender, RoutedEventArgs e)
        {
            if (GetPEWindow().TryGetSelectedExport(out var exp))
            {
                ObjectBinary bin = ObjectBinary.From(exp);
                var names = bin.GetNames(exp.FileRef.Game);
                foreach (var n in names)
                {
                    Debug.WriteLine($"{n.Item1.Instanced} {n.Item2}");
                }
            }
        }

        private void TriggerObjBinGetUIndexes_Clicked(object sender, RoutedEventArgs e)
        {
            if (GetPEWindow().TryGetSelectedExport(out var exp))
            {
                ObjectBinary bin = ObjectBinary.From(exp);
                var indices = bin.GetUIndexes(exp.FileRef.Game);
                foreach (var n in indices)
                {
                    Debug.WriteLine($"{n.Item1} {n.Item2}");
                }
            }
        }

        private void ShaderCacheResearch_Click(object sender, RoutedEventArgs e)
        {
            PackageEditorExperimentsM.ShaderCacheResearch(GetPEWindow());
        }

        private void PrintLoadedPackages_Clicked(object sender, RoutedEventArgs e)
        {
            MEPackageHandler.PrintOpenPackages();
        }

        private void ShiftME1AnimCutScene(object sender, RoutedEventArgs e)
        {
            var selected = GetPEWindow().TryGetSelectedExport(out var export);
            if (selected)
            {
                PackageEditorExperimentsM.ShiftME1AnimCutscene(export);
            }
        }

        private void ShiftInterpTrackMove(object sender, RoutedEventArgs e)
        {
            var selected = GetPEWindow().TryGetSelectedExport(out var export);
            if (selected)
            {
                PackageEditorExperimentsM.ShiftInterpTrackMove(export);
            }
        }

        private void RandomizeTerrain_Click(object sender, RoutedEventArgs e)
        {
            if (GetPEWindow().Pcc == null) return;
            PackageEditorExperimentsM.RandomizeTerrain(GetPEWindow().Pcc);
        }

        #endregion

        // EXPERIMENTS: SIRCXYRTYX-----------------------------------------------------
        #region SirCxyrtyx's Experiments
        private void MakeME1TextureFileList(object sender, RoutedEventArgs e)
        {
            PackageEditorExperimentsS.MakeME1TextureFileList(GetPEWindow());
        }


        private void DumpAllShaders()
        {
            var pew = GetPEWindow();
            if (pew.Pcc == null) return;
            PackageEditorExperimentsS.DumpAllShaders(pew.Pcc);
        }

        private void DumpMaterialShaders()
        {
            var pew = GetPEWindow();
            if (pew.TryGetSelectedExport(out ExportEntry matExport) && matExport.IsA("MaterialInterface"))
            {
                PackageEditorExperimentsS.DumpMaterialShaders(matExport);
            }
        }



        void OpenMapInGame()
        {
            const string tempMapName = "__ME3EXPDEBUGLOAD";
            var Pcc = GetPEWindow().Pcc;

            if (Pcc.Exports.All(exp => exp.ClassName != "Level"))
            {
                MessageBox.Show(GetPEWindow(), "GetPEWindow() file is not a map file!");
            }

            //only works for ME3?
            string mapName = System.IO.Path.GetFileNameWithoutExtension(Pcc.FilePath);

            string tempDir = MEDirectories.GetCookedPath(Pcc.Game);
            tempDir = Pcc.Game == MEGame.ME1 ? System.IO.Path.Combine(tempDir, "Maps") : tempDir;
            string tempFilePath = System.IO.Path.Combine(tempDir, $"{tempMapName}.{(Pcc.Game == MEGame.ME1 ? "SFM" : "pcc")}");

            Pcc.Save(tempFilePath);

            using (var tempPcc = MEPackageHandler.OpenMEPackage(tempFilePath, forceLoadFromDisk: true))
            {
                //insert PlayerStart if neccesary
                if (!(tempPcc.Exports.FirstOrDefault(exp => exp.ClassName == "PlayerStart") is ExportEntry playerStart))
                {
                    var levelExport = tempPcc.Exports.First(exp => exp.ClassName == "Level");
                    Level level = ObjectBinary.From<Level>(levelExport);
                    float x = 0, y = 0, z = 0;
                    if (tempPcc.TryGetUExport(level.NavListStart, out ExportEntry firstNavPoint))
                    {
                        if (firstNavPoint.GetProperty<StructProperty>("Location") is StructProperty locProp)
                        {
                            (x, y, z) = CommonStructs.GetVector3(locProp);
                        }
                        else if (firstNavPoint.GetProperty<StructProperty>("location") is StructProperty locProp2)
                        {
                            (x, y, z) = CommonStructs.GetVector3(locProp2);
                        }
                    }

                    playerStart = new ExportEntry(tempPcc, properties: new PropertyCollection
                    {
                        CommonStructs.Vector3Prop(x, y, z, "location")
                    })
                    {
                        Parent = levelExport,
                        ObjectName = "PlayerStart",
                        Class = tempPcc.getEntryOrAddImport("Engine.PlayerStart")
                    };
                    tempPcc.AddExport(playerStart);
                    level.Actors.Add(playerStart.UIndex);
                    levelExport.WriteBinary(level);
                }

                tempPcc.Save();
            }


            Process.Start(MEDirectories.GetExecutablePath(Pcc.Game), $"{tempMapName} -nostartupmovies");
        }

        private void ConvertAllDialogueToSkippable_Click(object sender, RoutedEventArgs e)
        {
            PackageEditorExperimentsS.ConvertAllDialogueToSkippable(GetPEWindow());
        }

        private void CreateDynamicLighting(object sender, RoutedEventArgs e)
        {
            if (GetPEWindow().Pcc == null) return;
            PackageEditorExperimentsS.CreateDynamicLighting(GetPEWindow().Pcc);
        }

        private void ConvertToDifferentGameFormat_Click(object sender, RoutedEventArgs e)
        {
            // TODO: IMPLEMENT IN LEX
            //var pew = GetPEWindow();
            //if (pew.Pcc is MEPackage pcc)
            //{
            //    var gameString = InputComboBoxDialog.GetValue(GetPEWindow(), "Which game's format do you want to convert to?",
            //        "Game file converter",
            //        new[] { "ME1", "ME2", "ME3" }, "ME2");
            //    if (Enum.TryParse(gameString, out MEGame game))
            //    {
            //        pew.IsBusy = true;
            //        pew.BusyText = "Converting...";
            //        Task.Run(() => { pcc.ConvertTo(game); }).ContinueWithOnUIThread(prevTask =>
            //        {
            //            pew.IsBusy = false;
            //            pew.SaveFileAs();
            //            pew.Close();
            //        });
            //    }
            //}
            //else
            //{
            //    MessageBox.Show(GetPEWindow(), "Can only convert Mass Effect files!");
            //}
        }

        private void ReSerializeExport_Click(object sender, RoutedEventArgs e)
        {
            if (GetPEWindow().TryGetSelectedExport(out ExportEntry export))
            {
                PackageEditorExperimentsS.ReserializeExport(export);
            }
        }

        private void RunPropertyCollectionTest(object sender, RoutedEventArgs e)
        {
            PackageEditorExperimentsS.RunPropertyCollectionTest(GetPEWindow());
        }

        private void UDKifyTest(object sender, RoutedEventArgs e)
        {
            if (GetPEWindow().Pcc != null)
            {
                PackageEditorExperimentsS.UDKifyTest(GetPEWindow());
            }
        }

        private void CondenseAllArchetypes(object sender, RoutedEventArgs e)
        {
            var pew = GetPEWindow();
            if (pew.Pcc != null && pew.Pcc.Exports.FirstOrDefault(exp => exp.ClassName == "Level") is ExportEntry level)
            {
                pew.IsBusy = true;
                pew.BusyText = "Condensing Archetypes";
                Task.Run(() =>
                {
                    foreach (ExportEntry export in level.GetAllDescendants().OfType<ExportEntry>())
                    {
                        export.CondenseArchetypes(false);
                    }

                    pew.IsBusy = false;
                });
            }
        }

        private void DumptTaggedWwiseStreams_OnClick(object sender, RoutedEventArgs e)
        {
            PackageEditorExperimentsS.DumpSound(GetPEWindow());
        }
        private void ScanStuff_Click(object sender, RoutedEventArgs e)
        {
            PackageEditorExperimentsS.ScanStuff(GetPEWindow());
        }
        private void ConvertFileToME3(object sender, RoutedEventArgs e)
        {
            // TODO: IMPLEMENT IN LEX

            // This should be moved into a specific experiments class
            /*
            var pew = GetPEWindow();
            pew.BusyText = "Converting files";
            pew.IsBusy = true;
            string tfc = PromptDialog.Prompt(GetPEWindow(), "Enter Name of Target Textures File Cache (tfc) without extension",
                "Level Conversion Tool", "Textures_DLC_MOD_", false, PromptDialog.InputType.Text);

            if (pew.Pcc == null || tfc == null || tfc == "Textures_DLC_MOD_")
                return;
            tfc = Path.Combine(Path.GetDirectoryName(pew.Pcc.FilePath), $"{tfc}.tfc");

            if (pew.Pcc is MEPackage tgt && pew.Pcc.Game != MEGame.ME3)
            {
                Task.Run(() => tgt.ConvertTo(MEGame.ME3, tfc, true)).ContinueWithOnUIThread(prevTask =>
                {
                    pew.IsBusy = false;
                });
            }*/
        }

        private void RecompileAll_OnClick(object sender, RoutedEventArgs e)
        {
            var pew = GetPEWindow();
            if (pew.Pcc != null && pew.Pcc.Platform == MEPackage.GamePlatform.PC && pew.Pcc.Game != MEGame.UDK)
            {
                var exportsWithDecompilationErrors = new List<EntryStringPair>();
                var fileLib = new FileLib(GetPEWindow().Pcc);
                foreach (ExportEntry export in pew.Pcc.Exports.Where(exp => exp.IsClass))
                {
                    (_, string script) = UnrealScriptCompiler.DecompileExport(export, fileLib);
                    (ASTNode ast, MessageLog log, _) = UnrealScriptCompiler.CompileAST(script, export.ClassName, export.Game);
                    if (ast == null)
                    {
                        exportsWithDecompilationErrors.Add(new EntryStringPair(export, "Compilation Error!"));
                        break;
                    }
                }

                var dlg = new ListDialog(exportsWithDecompilationErrors, $"Compilation errors", "", GetPEWindow())
                {
                    DoubleClickEntryHandler = pew.GetEntryDoubleClickAction()
                };
                dlg.Show();
            }
        }

        private void FindOpCode_OnClick(object sender, RoutedEventArgs e)
        {
            var pew = GetPEWindow();
            if (pew.Pcc != null)
            {
                if (!short.TryParse(PromptDialog.Prompt(GetPEWindow(), "enter opcode"), out short opCode))
                {
                    return;
                }
                var exportsWithOpcode = new List<EntryStringPair>();
                foreach (ExportEntry export in pew.Pcc.Exports.Where(exp => exp.ClassName == "Function"))
                {
                    if (pew.Pcc.Game is MEGame.ME3)
                    {
                        (List<Token> tokens, _) = Bytecode.ParseBytecode(export.GetBinaryData<UFunction>().ScriptBytes, export);
                        if (tokens.FirstOrDefault(tok => tok.op == opCode) is Token token)
                        {
                            exportsWithOpcode.Add(new EntryStringPair(export, token.posStr));
                        }
                    }
                    else
                    {
                        var func = LegendaryExplorerCore.ME1.Unreal.UnhoodBytecode.UE3FunctionReader.ReadFunction(export);
                        func.Decompile(new LegendaryExplorerCore.ME1.Unreal.UnhoodBytecode.TextBuilder(), false, true);
                        if (func.Statements.statements.Count > 0
                            && func.Statements.statements[0].Reader.ReadTokens.FirstOrDefault(tok => (short)tok.OpCode == opCode) is { })
                        {
                            exportsWithOpcode.Add(new EntryStringPair(export, ""));
                        }
                    }
                }

                var dlg = new ListDialog(exportsWithOpcode, $"functions with opcode 0x{opCode:X}", "", GetPEWindow())
                {
                    DoubleClickEntryHandler = pew.GetEntryDoubleClickAction()
                };
                dlg.Show();
            }
        }

        private void DumpShaderTypes_OnClick(object sender, RoutedEventArgs e)
        {
            PackageEditorExperimentsS.DumpShaderTypes(GetPEWindow());
        }
        #endregion

        // EXPERIMENTS: KINKOJIRO ------------------------------------------------------------
        #region Kinkojiro's Experiments
        public void AutoEnumerateClassNetIndex(object sender, RoutedEventArgs e)
        {
            int baseindex = 0;
            var Pcc = GetPEWindow().Pcc;
            if (GetPEWindow().TryGetSelectedExport(out var classexp) && classexp.IsClass)
            {
                baseindex = classexp.NetIndex;
                var classbin = classexp.GetBinaryData<UClass>();
                ExportEntry defaultxp = classexp.FileRef.GetUExport(classbin.Defaults);
                defaultxp.NetIndex = baseindex + 1;
                EnumerateChildNetIndexes(classbin.Children);
            }

            void EnumerateChildNetIndexes(int child)
            {
                if (child > 0 && child <= Pcc.ExportCount)
                {
                    var childexp = Pcc.GetUExport(child);
                    baseindex--;
                    childexp.NetIndex = baseindex;
                    var childbin = ObjectBinary.From(childexp);
                    if (childbin is UFunction funcbin)
                    {
                        EnumerateChildNetIndexes(funcbin.Children);
                        EnumerateChildNetIndexes(funcbin.Next);
                    }
                    else if (childbin is UProperty propbin)
                    {

                        EnumerateChildNetIndexes(propbin.Next);
                    }
                }

                return;
            }
        }
        private void TransferLevelBetweenGames(object sender, RoutedEventArgs e)
        {
            var pew = GetPEWindow();
            var Pcc = pew.Pcc;
            if (Pcc is MEPackage pcc && Path.GetFileNameWithoutExtension(pcc.FilePath).StartsWith("BioP") &&
                pcc.Game == MEGame.ME2)
            {
                var cdlg = MessageBox.Show(
                    "GetPEWindow() is a highly experimental method to copy the static art and collision from an ME2 level to an ME3 one.  It will not copy materials or design elements.",
                    "Warning", MessageBoxButton.OKCancel);
                if (cdlg == MessageBoxResult.Cancel)
                    return;

                CommonOpenFileDialog o = new CommonOpenFileDialog
                {
                    IsFolderPicker = true,
                    EnsurePathExists = true,
                    Title = "Select output folder"
                };
                if (o.ShowDialog(GetPEWindow()) == CommonFileDialogResult.Ok)
                {
                    string tfc = PromptDialog.Prompt(GetPEWindow(),
                        "Enter Name of Target Textures File Cache (tfc) without extension", "Level Conversion Tool",
                        "Textures_DLC_MOD_", false, PromptDialog.InputType.Text);

                    if (tfc == null || tfc == "Textures_DLC_MOD_")
                        return;

                    pew.BusyText = "Parsing level files";
                    pew.IsBusy = true;
                    Task.Run(() =>
                        PackageEditorExperimentsK.ConvertLevelToGame(MEGame.ME3, pcc, o.FileName, tfc,
                            newText => pew.BusyText = newText)).ContinueWithOnUIThread(prevTask =>
                            {
                                if (Pcc != null)
                                    pew.LoadFile(Pcc.FilePath);
                                pew.IsBusy = false;
                                var dlg = new ListDialog(prevTask.Result, $"Conversion errors: ({prevTask?.Result.Count})", "",
                                    GetPEWindow())
                                {
                                    DoubleClickEntryHandler = pew.GetEntryDoubleClickAction()
                                };
                                dlg.Show();
                            });

                }

            }
            else
            {
                MessageBox.Show(GetPEWindow(),
                    "Load a level's BioP file to start the transfer.\nCurrently can only convert from ME2 to ME3.");
            }
        }

        private void RestartTransferFromJSON(object sender, RoutedEventArgs e)
        {
            PackageEditorExperimentsK.RestartTransferFromJSON(GetPEWindow(), GetPEWindow().GetEntryDoubleClickAction());
        }

        private void RecookLevelToTestFromJSON(object sender, RoutedEventArgs e)
        {
            PackageEditorExperimentsK.RecookLevelToTestFromJSON(GetPEWindow(), GetPEWindow().GetEntryDoubleClickAction());
        }

        #endregion

        // EXPERIMENTS: OTHER PEOPLE ------------------------------------------------------------
        #region Other people's experiments
        private void ExportLevelToT3D_Click(object sender, RoutedEventArgs e)
        {
            PackageEditorExperimentsO.DumpPackageToT3D(GetPEWindow().Pcc);
        }


        private void BuildME1SuperTLK_Clicked(object sender, RoutedEventArgs e)
        {
            PackageEditorExperimentsO.BuildME1SuperTLKFile(GetPEWindow());
        }
        #endregion


        // PLEASE MOVE YOUR EXPERIMENT HANDLER INTO YOUR SECTION ABOVE
    }
}