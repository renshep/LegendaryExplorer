﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using LegendaryExplorer.Misc;
using LegendaryExplorerCore.GameFilesystem;
using LegendaryExplorerCore.Helpers;
using LegendaryExplorerCore.Misc;
using LegendaryExplorerCore.Packages;
using LegendaryExplorerCore.Packages.CloningImportingAndRelinking;
using LegendaryExplorerCore.Unreal;
using LegendaryExplorerCore.Unreal.BinaryConverters;
using LegendaryExplorerCore.Unreal.ObjectInfo;
using Newtonsoft.Json;

namespace LegendaryExplorer.Tools.PackageEditor.Experiments
{
    public class VTestExperiment
    {

        class VTestOptions
        {
            #region Configurable options
            /// <summary>
            /// List of levels to port. DO NOT INCLUDE BIOA_.
            /// </summary>
            public string[] vTestLevels = new[]
            {
                // Comment/uncomment these to select which files to run on
                "PRC2",
                "PRC2AA"
            };
            public bool useDynamicLighting = true;
            #endregion

            #region Autoset options - Do not change these
            public PackageEditorWindow packageEditorWindow;
            public IMEPackage vTestHelperPackage;
            public ObjectInstanceDB objectDB;
            public PackageCache cache = new PackageCache();
            #endregion
        }


        #region Vars
        /// <summary>
        /// List of things to port when porting a level with VTest
        /// </summary>
        private static string[] ClassesToVTestPort = new[]
        {
            "InterpActor",
            "BioInert",
            "BioUsable",
            "BioPawn",
            "SkeletalMeshActor",
            "PostProcessVolume",
            "BioMapNote",
            "Note",
            "BioTrigger",
            "BioSunActor",
            "BlockingVolume",
            "BioDoor",
            "StaticMeshCollectionActor",
            "StaticLightCollectionActor",
            "ReverbVolume",
            "BioAudioVolume",
            "AmbientSound",
            "BioLedgeMeshActor",
            "BioStage",
            "HeightFog",
            "PrefabInstance",
            "CameraActor",
            "Terrain", // OH BOY

            // Pass 2
            "StaticMeshActor",
            "TriggerVolume",
            "BioSquadCombat",
            "PhysicsVolume",
            "BioWp_ActionStation",
            "BioLookAtTarget",
            "BioUsable",
            "BioContainer",

            // Pass 3
            "Brush",
            "PathNode",
            "BioCoverVolume",
            "BioTriggerVolume",
            "BioWp_AssaultPoint",
            "BioSquadPlayer",
            "BioUseable",
            "BioSquadSitAndShoot",
            "CoverLink",
            "BioWaypointSet",
            "BioPathPoint",
            "Emitter"

        };

        /// <summary>
        /// Classes to port only for master level files
        /// </summary>
        private static string[] ClassesToVTestPortMasterOnly = new[]
        {
            "PlayerStart",
            "BioTriggerStream"
        };

        // Files we know are referenced by name but do not exist
        private static string[] VTest_NonExistentBTSFiles =
        {
            "bioa_prc2_ccahern_l",
            "bioa_prc2_cccave01",
            "bioa_prc2_cccave02",
            "bioa_prc2_cccave03",
            "bioa_prc2_cccave04",
            "bioa_prc2_cccrate01",
            "bioa_prc2_cccrate02",
            "bioa_prc2_cclobby01",
            "bioa_prc2_cclobby02",
            "bioa_prc2_ccmid01",
            "bioa_prc2_ccmid02",
            "bioa_prc2_ccmid03",
            "bioa_prc2_ccmid04",
            "bioa_prc2_ccscoreboard",
            "bioa_prc2_ccsim01",
            "bioa_prc2_ccsim02",
            "bioa_prc2_ccsim03",
            "bioa_prc2_ccsim04",
            "bioa_prc2_ccspace02",
            "bioa_prc2_ccspace03",
            "bioa_prc2_ccthai01",
            "bioa_prc2_ccthai02",
            "bioa_prc2_ccthai03",
            "bioa_prc2_ccthai04",
            "bioa_prc2_ccthai05",
            "bioa_prc2_ccthai06",
        };

        // This is list of materials to run a conversion to a MaterialInstanceConstant
        // List is not long cause not a lot of materials support this...
        private static string[] vtest_DonorMaterials = new[]
        {
            "BIOA_MAR10_T.UNC_HORIZON_MAT_Dup",
        };

        #endregion

        #region Kinda Hacky Vars
        /// <summary>
        /// List of all actor classes that were encountered during the last VTest session. Resets at the start of every VTest.
        /// </summary>
        internal static List<string> actorTypesNotPorted;

        #endregion

        #region Main porting methods

        /// <summary>
        /// Runs the main VTest
        /// </summary>
        /// <param name="pe"></param>
        /// <param name="installAndBootGame"></param>
        public static async void VTest(PackageEditorWindow pe, bool? installAndBootGame = null)
        {
            // Prep
            EntryImporter.NonDonorMaterials.Clear();
            actorTypesNotPorted = new List<string>();

            if (installAndBootGame == null)
            {
                var result = MessageBox.Show(pe, "Install VTest and run the game when VTest completes? PAEMPaths must be set.", "Auto install and boot", MessageBoxButton.YesNo, MessageBoxImage.Question, MessageBoxResult.Yes);
                installAndBootGame = result == MessageBoxResult.Yes;
            }

            // This object is passed through to all the methods so we don't have to constantly update the signatures
            var vTestOptions = new VTestOptions()
            {
                packageEditorWindow = pe
            };

            pe.SetBusy("Performing VTest");
            await Task.Run(() =>
            {
                RunVTest(vTestOptions);
            }).ContinueWithOnUIThread(result =>
            {
                if (result.Exception != null)
                    throw result.Exception;
                pe.EndBusy();
                if (installAndBootGame != null && installAndBootGame.Value)
                {
                    var moddesc = Path.Combine(Directory.GetParent(PAEMPaths.VTest_DonorsDir).FullName, "moddesc.ini");
                    if (File.Exists(moddesc))
                    {
                        ProcessStartInfo psi = new ProcessStartInfo(PAEMPaths.VTest_ModManagerPath, $"--installmod \"{moddesc}\" --bootgame LE1");
                        Process.Start(psi);
                    }
                }
            });
        }

        /// <summary>
        /// Internal single-thread VTest session
        /// </summary>
        /// <param name="vTestOptions"></param>
        private static void RunVTest(VTestOptions vTestOptions)
        {
            string dbPath = AppDirectories.GetObjectDatabasePath(MEGame.LE1);
            //string matPath = AppDirectories.GetMaterialGuidMapPath(MEGame.ME1);
            //Dictionary<Guid, string> me1MaterialMap = null;
            vTestOptions.packageEditorWindow.BusyText = "Loading databases";

            if (File.Exists(dbPath))
            {
                vTestOptions.objectDB = ObjectInstanceDB.DeserializeDB(File.ReadAllText(dbPath));
                vTestOptions.objectDB.BuildLookupTable(); // Lookup table is required as we are going to compile things
                vTestOptions.packageEditorWindow.BusyText = "Inventorying donors";

                // Add extra donors and VTestHelper package
                foreach (var file in Directory.GetFiles(PAEMPaths.VTest_DonorsDir))
                {
                    if (file.RepresentsPackageFilePath())
                    {
                        if (Path.GetFileNameWithoutExtension(file) == "VTestHelper")
                        {
                            // Load the VTestHelper
                            vTestOptions.vTestHelperPackage = MEPackageHandler.OpenMEPackage(file, forceLoadFromDisk: true); // Do not put into cache
                        }
                        else
                        {
                            // Inventory
                            using var p = MEPackageHandler.OpenMEPackage(file);
                            PackageEditorExperimentsM.IndexFileForObjDB(vTestOptions.objectDB, MEGame.LE1, p);
                        }

                    }
                }
            }
            else
            {
                return;
            }


            // Unused for now, maybe forever
            //if (File.Exists(matPath))
            //{
            //    me1MaterialMap = JsonConvert.DeserializeObject<Dictionary<Guid, string>>(File.ReadAllText(matPath));
            //}hel
            //else
            //{
            //    return;
            //}

            vTestOptions.packageEditorWindow.BusyText = "Clearing mod folder";
            // Clear out dest dir
            foreach (var f in Directory.GetFiles(PAEMPaths.VTest_FinalDestDir))
            {
                File.Delete(f);
            }

            // Copy in precomputed files
            vTestOptions.packageEditorWindow.BusyText = "Copying precomputed files";
            foreach (var f in Directory.GetFiles(PAEMPaths.VTest_PrecomputedDir))
            {
                File.Copy(f, Path.Combine(PAEMPaths.VTest_FinalDestDir, Path.GetFileName(f)));
            }

            vTestOptions.packageEditorWindow.BusyText = "Running VTest";

            // VTest Level Loop ---------------------------------------
            foreach (var vTestLevel in vTestOptions.vTestLevels)
            {
                var levelFiles = Directory.GetFiles(Path.Combine(PAEMPaths.VTest_SourceDir, vTestLevel));
                foreach (var f in levelFiles)
                {
                    if (f.Contains("_LOC_", StringComparison.InvariantCultureIgnoreCase))
                    {
                        PortLOCFile(f, vTestOptions);
                    }
                    else
                    {
                        var levelName = Path.GetFileNameWithoutExtension(f);
                        PortVTestLevel(vTestLevel, levelName, vTestOptions, levelName == "BIOA_" + vTestLevel, true);
                    }
                }
            }

            vTestOptions.cache.ReleasePackages(true); // Dump everything out of memory

            Debug.WriteLine("Non donated materials: ");
            foreach (var nonDonorMaterial in EntryImporter.NonDonorMaterials)
            {
                Debug.WriteLine(nonDonorMaterial);
            }

            Debug.WriteLine("Actor classes that were not ported:");
            foreach (var ac in actorTypesNotPorted)
            {
                Debug.WriteLine(ac);
            }

            // VTest post QA
            vTestOptions.packageEditorWindow.BusyText = "Performing checks";


            foreach (var f in Directory.GetFiles(PAEMPaths.VTest_FinalDestDir))
            {
                if (f.RepresentsPackageFilePath())
                {
                    using var p = MEPackageHandler.OpenMEPackage(f);

                    VTestCheckImports(p, vTestOptions);
                }
            }

        }

        private static void VTestCheckImports(IMEPackage p, VTestOptions vTestOptions)
        {
            foreach (var import in p.Imports)
            {
                if (import.IsAKnownNativeClass())
                    continue; //skip
                var resolvedExp = EntryImporter.ResolveImport(import, null, vTestOptions.cache, clipRootLevelPackage: false);
                if (resolvedExp == null)
                {
                    // Look in DB for objects that have same suffix
                    // This is going to be VERY slow

                    var instancedNameSuffix = "." + import.ObjectName.Instanced;
                    string similar = "";
                    foreach (var name in vTestOptions.objectDB.NameTable)
                    {
                        if (name.EndsWith(instancedNameSuffix, StringComparison.InvariantCultureIgnoreCase))
                        {
                            similar += ", " + name;
                        }
                    }

                    Debug.WriteLine($"Import not resolved: {import.InstancedFullPath}, may be these ones instead: {similar}");
                }
            }
        }

        /// <summary>
        /// Ports a level file for VTest. Saves package at the end.
        /// </summary>
        /// <param name="mapName">Overarching map name</param>
        /// <param name="sourceName">Full map file name</param>
        /// <param name="finalDestDir"></param>
        /// <param name="sourceDir"></param>
        /// <param name="db"></param>
        /// <param name="pe"></param>
        /// <param name="syncBioWorldInfo"></param>
        /// <param name="portMainSequence"></param>
        private static void PortVTestLevel(string mapName, string sourceName, VTestOptions vTestOptions, bool syncBioWorldInfo = false, bool portMainSequence = false)
        {
            vTestOptions.cache.ReleasePackages(x => Path.GetFileNameWithoutExtension(x) != "SFXGame" && Path.GetFileNameWithoutExtension(x) != "Engine"); //Reduce memory overhead
            var outputFile = $@"{PAEMPaths.VTest_FinalDestDir}\{sourceName.ToUpper()}.pcc";
            CreateEmptyLevel(outputFile, MEGame.LE1);

            using var le1File = MEPackageHandler.OpenMEPackage(outputFile);
            using var me1File = MEPackageHandler.OpenMEPackage($@"{PAEMPaths.VTest_SourceDir}\{mapName}\{sourceName}.SFM");

            // BIOC_BASE -> SFXGame
            var bcBaseIdx = me1File.findName("BIOC_Base");
            me1File.replaceName(bcBaseIdx, "SFXGame");

            // BIOG_StrategicAI -> SFXStrategicAI
            var bgsaiBaseIdx = me1File.findName("BIOG_StrategicAI");
            if (bgsaiBaseIdx >= 0)
                me1File.replaceName(bgsaiBaseIdx, "SFXStrategicAI");

            // Once we are confident in porting we will just take the actor list from PersistentLevel
            // For now just port these
            var itemsToPort = new List<ExportEntry>();

            var me1PersistentLevel = ObjectBinary.From<Level>(me1File.FindExport(@"TheWorld.PersistentLevel"));
            itemsToPort.AddRange(me1PersistentLevel.Actors.Where(x => x.value != 0) // Skip blanks
                .Select(x => me1File.GetUExport(x.value))
                .Where(x => ClassesToVTestPort.Contains(x.ClassName) || (syncBioWorldInfo && ClassesToVTestPortMasterOnly.Contains(x.ClassName))));

            // WIP: Find which classes we have yet to port
            // BioWorldInfo is not ported except on the level master. Might need to see if there's things
            // like scene desaturation in it worth porting.
            foreach (var v in me1PersistentLevel.Actors)
            {
                var entry = v.value != 0 ? v.GetEntry(me1File) : null;
                if (entry != null && !actorTypesNotPorted.Contains(entry.ClassName) && !ClassesToVTestPort.Contains(entry.ClassName) && !ClassesToVTestPortMasterOnly.Contains(entry.ClassName) && entry.ClassName != "BioWorldInfo")
                {
                    actorTypesNotPorted.Add(entry.ClassName);
                }
            }

            // End WIP


            VTestFilePorting(me1File, le1File, itemsToPort, vTestOptions);

            // Replace BioWorldInfo if requested
            if (syncBioWorldInfo)
            {
                var me1BWI = me1File.Exports.FirstOrDefault(x => x.ClassName == "BioWorldInfo");
                if (me1BWI != null)
                {
                    me1BWI.indexValue = 1;
                    EntryImporter.ImportAndRelinkEntries(EntryImporter.PortingOption.ReplaceSingular, me1BWI, le1File, le1File.FindExport(@"TheWorld.PersistentLevel.BioWorldInfo_0"), true, out _, importExportDependencies: true, targetGameDonorDB: vTestOptions.objectDB);
                }
            }

            // Replace Main_Sequence if requested
            if (portMainSequence)
            {
                vTestOptions.packageEditorWindow.BusyText = "Porting sequencing...";
                var dest = le1File.FindExport(@"TheWorld.PersistentLevel.Main_Sequence");
                var source = me1File.FindExport(@"TheWorld.PersistentLevel.Main_Sequence");
                if (source != null && dest != null)
                {
                    EntryImporter.ImportAndRelinkEntries(EntryImporter.PortingOption.ReplaceSingular, source, le1File, dest, true, out _, importExportDependencies: true, targetGameDonorDB: vTestOptions.objectDB);
                }
                else
                {
                    Debug.WriteLine($"No sequence to port in {sourceName}");
                }

            }

            PostPortingCorrections(me1File, le1File, vTestOptions);

            if (vTestOptions.useDynamicLighting)
            {
                PackageEditorExperimentsS.CreateDynamicLighting(le1File, true);
            }

            //if (le1File.Exports.Any(x => x.IsA("PathNode")))
            //{
            //    Debugger.Break();
            //}

            le1File.Save();

            Debug.WriteLine($"RCP CHECK FOR {Path.GetFileNameWithoutExtension(le1File.FilePath)} -------------------------");
            ReferenceCheckPackage rcp = new ReferenceCheckPackage();
            EntryChecker.CheckReferences(rcp, le1File, EntryChecker.NonLocalizedStringConverter);

            foreach (var err in rcp.GetBlockingErrors())
            {
                Debug.WriteLine($"RCP: [ERROR] {err.Entry.InstancedFullPath} {err.Message}");
            }

            foreach (var err in rcp.GetSignificantIssues())
            {
                Debug.WriteLine($"RCP: [WARN] {err.Entry.InstancedFullPath} {err.Message}");
            }
        }

        /// <summary>
        /// Ports a list of actors between levels with VTest
        /// </summary>
        /// <param name="sourcePackage"></param>
        /// <param name="destPackage"></param>
        /// <param name="itemsToPort"></param>
        /// <param name="db"></param>
        /// <param name="pe"></param>
        private static void VTestFilePorting(IMEPackage sourcePackage, IMEPackage destPackage, IEnumerable<ExportEntry> itemsToPort, VTestOptions vTestOptions)
        {
            // PRECORRECTION - CORRECTIONS TO THE SOURCE FILE BEFORE PORTING
            PrePortingCorrections(sourcePackage);

            // PORTING ACTORS
            var le1PL = destPackage.FindExport("TheWorld.PersistentLevel");
            foreach (var e in itemsToPort)
            {
                vTestOptions.packageEditorWindow.BusyText = $"Porting {e.ObjectName}";
                var report = EntryImporter.ImportAndRelinkEntries(EntryImporter.PortingOption.CloneAllDependencies, e, destPackage,
                    le1PL, true, out _, targetGameDonorDB: vTestOptions.objectDB);
            }
        }

        /// <summary>
        /// Ports a LOC file by porting the ObjectReferencer within it
        /// </summary>
        /// <param name="sourceFile"></param>
        /// <param name="db"></param>
        /// <param name="pc"></param>
        /// <param name="pe"></param>
        private static void PortLOCFile(string sourceFile, VTestOptions vTestOptions)
        {
            var packName = Path.GetFileNameWithoutExtension(sourceFile);
            vTestOptions.packageEditorWindow.BusyText = $"Porting {packName}";

            var destPackagePath = Path.Combine(PAEMPaths.VTest_FinalDestDir, $"{packName.ToUpper()}.pcc");
            MEPackageHandler.CreateAndSavePackage(destPackagePath, MEGame.LE1);
            using var package = MEPackageHandler.OpenMEPackage(destPackagePath);
            using var sourcePackage = MEPackageHandler.OpenMEPackage(sourceFile);

            var bcBaseIdx = sourcePackage.findName("BIOC_Base");
            sourcePackage.replaceName(bcBaseIdx, "SFXGame");

            foreach (var e in sourcePackage.Exports.Where(x => x.ClassName == "ObjectReferencer"))
            {
                var report = EntryImporter.ImportAndRelinkEntries(EntryImporter.PortingOption.CloneAllDependencies, e, package, null, true, out _, targetGameDonorDB: vTestOptions.objectDB);
                if (report.Any())
                {
                    //Debugger.Break();
                }
            }

            CorrectSequences(package, vTestOptions);
            vTestOptions.packageEditorWindow.BusyText = $"Saving {packName}";
            package.Save();
        }
        #endregion

        #region Utility methods
        private static void CreateEmptyLevel(string outpath, MEGame game)
        {
            var emptyLevelName = $"{game}EmptyLevel";
            File.Copy(Path.Combine(AppDirectories.ExecFolder, $"{emptyLevelName}.pcc"), outpath, true);
            using var Pcc = MEPackageHandler.OpenMEPackage(outpath);
            for (int i = 0; i < Pcc.Names.Count; i++)
            {
                string name = Pcc.Names[i];
                if (name.Equals(emptyLevelName))
                {
                    var newName = name.Replace(emptyLevelName, Path.GetFileNameWithoutExtension(outpath));
                    Pcc.replaceName(i, newName);
                }
            }

            var packguid = Guid.NewGuid();
            var package = Pcc.GetUExport(game switch
            {
                MEGame.LE1 => 4,
                MEGame.LE3 => 6,
                MEGame.ME2 => 7,
                _ => 1
            });
            package.PackageGUID = packguid;
            Pcc.PackageGuid = packguid;
            Pcc.Save();
        }

        private static StructProperty MakeLinearColorStruct(string propertyName, float r, float g, float b, float a)
        {
            PropertyCollection p = new PropertyCollection();
            p.AddOrReplaceProp(new FloatProperty(r, "R"));
            p.AddOrReplaceProp(new FloatProperty(g, "G"));
            p.AddOrReplaceProp(new FloatProperty(b, "B"));
            p.AddOrReplaceProp(new FloatProperty(a, "A"));
            return new StructProperty("LinearColor", p, propertyName, true);
        }
        #endregion

        #region Correction methods
        private static void PrePortingCorrections(IMEPackage sourcePackage)
        {
            // Strip static mesh light maps since they don't work crossgen. Strip them from
            // the source so they don't port
            foreach (var exp in sourcePackage.Exports)
            {
                #region Remove Light and Shadow Maps
                if (exp.ClassName == "StaticMeshComponent")
                {
                    var b = ObjectBinary.From<StaticMeshComponent>(exp);
                    foreach (var lod in b.LODData)
                    {
                        // Clear light and shadowmaps
                        lod.ShadowMaps = new UIndex[0];
                        lod.LightMap = new LightMap() { LightMapType = ELightMapType.LMT_None };
                    }

                    exp.WriteBinary(b);
                }
                else if (exp.ClassName == "TerrainComponent")
                {
                    // Strip Lightmap
                    var b = ObjectBinary.From<TerrainComponent>(exp);
                    b.LightMap = new LightMap() { LightMapType = ELightMapType.LMT_None };
                    // Correct collision vertices as they've changed from local to world in LE

                    float scaleX = 1;
                    float scaleY = 1;
                    float scaleZ = 1;

                    float basex = 0;
                    float basey = 0;
                    float basez = 0;

                    var ds3d = (exp.Parent as ExportEntry).GetProperty<StructProperty>("DrawScale3D");
                    if (ds3d != null)
                    {
                        scaleX = ds3d.GetProp<FloatProperty>("X").Value;
                        scaleY = ds3d.GetProp<FloatProperty>("Y").Value;
                        scaleZ = ds3d.GetProp<FloatProperty>("Z").Value;
                    }

                    var loc = (exp.Parent as ExportEntry).GetProperty<StructProperty>("Location");
                    if (loc != null)
                    {
                        basex = loc.GetProp<FloatProperty>("X").Value;
                        basey = loc.GetProp<FloatProperty>("Y").Value;
                        basez = loc.GetProp<FloatProperty>("Z").Value;
                    }

                    // COLLISION VERTICES
                    for (int i = 0; i < b.CollisionVertices.Length; i++)
                    {
                        var cv = b.CollisionVertices[i];
                        Vector3 newV = new Vector3();

                        newV.X = (cv.X * scaleX) + basex;
                        newV.Y = (cv.Y * scaleY) + basey;
                        newV.Z = (cv.Z * scaleZ) + basez;
                        b.CollisionVertices[i] = newV;
                    }

                    // Bounding Volume Tree
                    for (int i = 0; i < b.BVTree.Length; i++)
                    {
                        var box = b.BVTree[i].BoundingVolume;
                        box.Min = new Vector3 { X = (box.Min.X * scaleX) + basex, Y = (box.Min.Y * scaleY) + basey, Z = (box.Min.Z * scaleZ) + basez };
                        box.Max = new Vector3 { X = (box.Max.X * scaleX) + basex, Y = (box.Max.Y * scaleY) + basey, Z = (box.Max.Z * scaleZ) + basez };
                    }

                    exp.WriteBinary(b);

                    // Make dynamic lighting
                    var props = exp.GetProperties();
                    props.RemoveNamedProperty("ShadowMaps");
                    props.AddOrReplaceProp(new BoolProperty(false, "bForceDirectLightMap"));
                    props.AddOrReplaceProp(new BoolProperty(true, "bCastDynamicShadow"));
                    props.AddOrReplaceProp(new BoolProperty(true, "bAcceptDynamicLights"));

                    var lightingChannels = props.GetProp<StructProperty>("LightingChannels") ??
                                           new StructProperty("LightingChannelContainer", false,
                                               new BoolProperty(true, "bIsInitialized"))
                                           {
                                               Name = "LightingChannels"
                                           };
                    lightingChannels.Properties.AddOrReplaceProp(new BoolProperty(true, "Static"));
                    lightingChannels.Properties.AddOrReplaceProp(new BoolProperty(true, "Dynamic"));
                    lightingChannels.Properties.AddOrReplaceProp(new BoolProperty(true, "CompositeDynamic"));
                    props.AddOrReplaceProp(lightingChannels);

                    exp.WriteProperties(props);
                }
                #endregion
                else if (exp.ClassName == "BioTriggerStream")
                {
                    PreCorrectBioTriggerStream(exp);
                }
                else if (exp.ClassName == "BioWorldInfo")
                {
                    // Remove streaminglevels that don't do anything
                    //PreCorrectBioWorldInfoStreamingLevels(exp);
                }

                if (exp.IsA("Actor"))
                {
                    //exp.RemoveProperty("m_oAreaMap"); // Remove this when stuff is NOT borked up
                    //exp.RemoveProperty("Base"); // No bases
                    //exp.RemoveProperty("nextNavigationPoint"); // No bases
                }
            }
        }

        private static void PreCorrectBioWorldInfoStreamingLevels(ExportEntry exp)
        {
            // Older games (ME1 at least) can reference levels that don't exist. This didn't breka game. Later games this does break
            // has a bunch of level references that don't exist

            //if (triggerStream.ObjectName.Instanced == "BioTriggerStream_0")
            //    Debugger.Break();
            var streamingLevels = exp.GetProperty<ArrayProperty<ObjectProperty>>("StreamingLevels");
            if (streamingLevels != null)
            {
                for (int i = streamingLevels.Count - 1; i >= 0; i--)
                {
                    var lsk = streamingLevels[i].ResolveToEntry(exp.FileRef) as ExportEntry;
                    var packageName = lsk.GetProperty<NameProperty>("PackageName");
                    if (VTest_NonExistentBTSFiles.Contains(packageName.Value.Instanced.ToLower()))
                    {
                        // Do not port this
                        Debug.WriteLine($@"Removed non-existent LSK package: {packageName.Value.Instanced} in {Path.GetFileNameWithoutExtension(exp.FileRef.FilePath)}");
                        streamingLevels.RemoveAt(i);
                    }
                    else
                    {
                        Debug.WriteLine($@"LSK package exists: {packageName.Value.Instanced} in {Path.GetFileNameWithoutExtension(exp.FileRef.FilePath)}");
                    }
                }

                exp.WriteProperty(streamingLevels);
            }
        }

        private static void PostCorrectMaterialsToInstanceConstants(IMEPackage me1Package, IMEPackage le1Package, VTestOptions vTestOptions)
        {
            // Oh lordy this is gonna suck

            // Donor materials need tweaks to behave like the originals
            // So we make a new MaterialInstanceConstant, copy in the relevant(?) values,
            // and then repoint all incoming references to the Material to use this MaterialInstanceConstant instead.
            // This is going to be slow and ugly code
            // Technically this could be done in the relinker but I don't want to stuff
            // something this ugly in there
            foreach (var le1Material in le1Package.Exports.Where(x => vtest_DonorMaterials.Contains(x.InstancedFullPath)).ToList())
            {
                Debug.WriteLine($"Correcting material inputs for donor material: {le1Material.InstancedFullPath}");
                var donorinputs = new List<string>();
                var expressions = le1Material.GetProperty<ArrayProperty<ObjectProperty>>("Expressions");
                foreach (var express in expressions.Select(x => x.ResolveToEntry(le1Package) as ExportEntry))
                {
                    if (express.ClassName == "MaterialExpressionVectorParameter")
                    {
                        donorinputs.Add(express.GetProperty<NameProperty>("ParameterName").Value.Name);
                    }
                }

                Debug.WriteLine(@"Donor has the following inputs:");
                foreach (var di in donorinputs)
                {
                    Debug.WriteLine(di);
                }

                var me1Material = me1Package.FindExport(le1Material.InstancedFullPath);

                var sourceMatInst = vTestOptions.vTestHelperPackage.Exports.First(x => x.ClassName == "MaterialInstanceConstant"); // cause it can change names here
                sourceMatInst.ObjectName = $"{le1Material.ObjectName}_MatInst";
                EntryImporter.ImportAndRelinkEntries(EntryImporter.PortingOption.CloneAllDependencies, sourceMatInst, le1Package, le1Material.Parent, true, out var le1MatInstEntryt);

                var le1MatInst = le1MatInstEntryt as ExportEntry;
                var le1MatInstProps = le1MatInst.GetProperties();

                le1MatInstProps.AddOrReplaceProp(new ObjectProperty(le1Material, "Parent")); // Update the parent

                // VECTOR EXPRESSIONS
                var vectorExpressions = new ArrayProperty<StructProperty>("VectorParameterValues");
                foreach (var v in me1Material.GetProperty<ArrayProperty<ObjectProperty>>("Expressions").Select(x => x.ResolveToEntry(me1Package) as ExportEntry))
                {
                    if (v.ClassName == "MaterialExpressionVectorParameter")
                    {
                        var exprInput = v.GetProperty<NameProperty>("ParameterName").Value.Name;
                        if (donorinputs.Contains(exprInput))
                        {
                            var vpv = v.GetProperty<StructProperty>("DefaultValue");
                            PropertyCollection pc = new PropertyCollection();
                            pc.AddOrReplaceProp(MakeLinearColorStruct("ParameterValue", vpv.GetProp<FloatProperty>("R"), vpv.GetProp<FloatProperty>("G"), vpv.GetProp<FloatProperty>("B"), vpv.GetProp<FloatProperty>("A")));
                            pc.AddOrReplaceProp(new FGuid(Guid.Empty).ToStructProperty("ExpressionGUID"));
                            pc.AddOrReplaceProp(new NameProperty(exprInput, "ParameterName"));
                            vectorExpressions.Add(new StructProperty("VectorParameterValue", pc));
                            donorinputs.Remove(exprInput);
                        }
                    }
                    else
                    {
                        //Debugger.Break();
                    }
                }

                if (vectorExpressions.Any())
                {
                    le1MatInstProps.AddOrReplaceProp(vectorExpressions);
                }

                // SCALAR EXPRESSIONS
                var me1MatInfo = ObjectBinary.From<Material>(me1Material);
                var scalarExpressions = new ArrayProperty<StructProperty>("ScalarParameterValues");
                foreach (var v in me1MatInfo.SM3MaterialResource.UniformPixelScalarExpressions)
                {
                    if (v is MaterialUniformExpressionScalarParameter spv)
                    {
                        PropertyCollection pc = new PropertyCollection();
                        pc.AddOrReplaceProp(new FGuid(Guid.Empty).ToStructProperty("ExpressionGUID"));
                        pc.AddOrReplaceProp(new NameProperty(spv.ParameterName, "ParameterName"));
                        pc.AddOrReplaceProp(new FloatProperty(spv.DefaultValue, "ParameterValue"));
                        scalarExpressions.Add(new StructProperty("ScalarParameterValue", pc));
                    }
                }

                if (scalarExpressions.Any())
                {
                    le1MatInstProps.AddOrReplaceProp(scalarExpressions);
                }

                le1MatInst.WriteProperties(le1MatInstProps);

                // Find things that reference this material and repoint them
                var entriesToUpdate = le1Material.GetEntriesThatReferenceThisOne();
                foreach (var entry in entriesToUpdate.Keys)
                {
                    if (entry == le1MatInst)
                        continue;
                    le1MatInst.GetProperties();
                    var relinkDict = new Dictionary<IEntry, IEntry>();
                    relinkDict[le1Material] = le1MatInst; // This is a ridiculous hack
                    Relinker.Relink(entry as ExportEntry, entry as ExportEntry, relinkDict, new List<EntryStringPair>(0));
                    le1MatInst.GetProperties();
                }
            }
        }

        private static void PreCorrectBioTriggerStream(ExportEntry triggerStream)
        {
            // Older games (ME1 at least) can reference levels that don't exist. This didn't break game. Later games this does break. Maybe. IDK.

            //if (triggerStream.ObjectName.Instanced == "BioTriggerStream_0")
            //    Debugger.Break();
            // triggerStream.RemoveProperty("m_oAreaMapOverride"); // Remove this when stuff is NOT borked up
            //
            // return;
            var streamingStates = triggerStream.GetProperty<ArrayProperty<StructProperty>>("StreamingStates");
            if (streamingStates != null)
            {
                foreach (var ss in streamingStates)
                {
                    var inChunkName = ss.GetProp<NameProperty>("InChunkName").Value.Name.ToLower();

                    if (inChunkName != "none" && VTest_NonExistentBTSFiles.Contains(inChunkName))
                        Debugger.Break(); // Hmm....

                    var visibleChunks = ss.GetProp<ArrayProperty<NameProperty>>("VisibleChunkNames");
                    for (int i = visibleChunks.Count - 1; i >= 0; i--)
                    {
                        if (VTest_NonExistentBTSFiles.Contains(visibleChunks[i].Value.Name.ToLower()))
                        {
                            Debug.WriteLine($"PreCorrect: VS Remove BTS level {visibleChunks[i].Value}");
                            //visibleChunks.RemoveAt(i);
                        }
                    }

                    var loadChunks = ss.GetProp<ArrayProperty<NameProperty>>("LoadChunkNames");
                    for (int i = loadChunks.Count - 1; i >= 0; i--)
                    {
                        if (VTest_NonExistentBTSFiles.Contains(loadChunks[i].Value.Name.ToLower()))
                        {
                            Debug.WriteLine($"PreCorrect: LC Remove BTS level {loadChunks[i].Value}");
                            //loadChunks.RemoveAt(i);
                        }
                    }
                }

                triggerStream.WriteProperty(streamingStates);
            }
            else
            {
                //yDebug.WriteLine($"{triggerStream.InstancedFullPath} in {triggerStream} has NO StreamingStates!!");
            }
        }

        private static void PostPortingCorrections(IMEPackage me1File, IMEPackage le1File, VTestOptions vTestOptions)
        {
            // Corrections to run AFTER porting is done
            CorrectNeverStream(le1File);
            CorrectPrefabSequenceClass(le1File);
            CorrectSequences(le1File, vTestOptions);
            CorrectPathfindingNetwork(me1File, le1File);
            PostCorrectMaterialsToInstanceConstants(me1File, le1File, vTestOptions);
            RebuildPersistentLevelChildren(le1File.FindExport("TheWorld.PersistentLevel"));
            CorrectTerrainMaterials(le1File);
            //CorrectTriggerStreamsMaybe(me1File, le1File);
        }

        private static void CorrectNeverStream(IMEPackage package)
        {
            foreach (var exp in package.Exports.Where(x => x.IsTexture()))
            {
                var props = exp.GetProperties();
                var texinfo = ObjectBinary.From<UTexture2D>(exp);
                var numMips = texinfo.Mips.Count;
                var ns = props.GetProp<BoolProperty>("NeverStream");
                int lowMipCount = 0;
                for (int i = numMips - 1; i >= 0; i--)
                {
                    if (lowMipCount > 6 && (ns == null || ns.Value == false) && texinfo.Mips[i].IsLocallyStored && texinfo.Mips[i].StorageType != StorageTypes.empty)
                    {
                        exp.WriteProperty(new BoolProperty(true, "NeverStream"));
                        break;
                    }
                    lowMipCount++;
                }
            }
        }

        private static void CorrectSequences(IMEPackage le1File, VTestOptions vTestOptions)
        {
            // Find sequences that aren't in other sequences
            foreach (var seq in le1File.Exports.Where(e => e is { ClassName: "Sequence" } && !e.Parent.IsA("SequenceObject")))
            {
                CorrectSequenceObjects(seq, vTestOptions);
            }
        }


        private static void RebuildPersistentLevelChildren(ExportEntry pl)
        {
            ExportEntry[] actorsToAdd = pl.FileRef.Exports.Where(exp => exp.Parent == pl && exp.IsA("Actor")).ToArray();
            Level level = ObjectBinary.From<Level>(pl);
            level.Actors.Clear();
            foreach (var actor in actorsToAdd)
            {
                // Don't add things that are in collection actors

                var lc = actor.GetProperty<ObjectProperty>("LightComponent");
                if (lc != null && pl.FileRef.TryGetUExport(lc.Value, out var lightComp))
                {
                    if (lightComp.Parent != null && lightComp.Parent.ClassName == "StaticLightCollectionActor")
                        continue; // don't add this one
                }

                //var mc = actor.GetProperty<ObjectProperty>("MeshComponent");
                //if (mc != null && pl.FileRef.TryGetUExport(mc.Value, out var meshComp))
                //{
                //    if (meshComp.Parent != null && meshComp.Parent.ClassName == "StaticMeshCollectionActor")
                //        continue; // don't add this one
                //}

                level.Actors.Add(new UIndex(actor.UIndex));
            }

            //if (level.Actors.Count > 1)
            //{

            // BioWorldInfo will always be present
            // or at least, it better be!
            // Slot 2 has to be blank in LE. In ME1 i guess it was a brush.
            level.Actors.Insert(1, new UIndex(0)); // This is stupid
            //}

            pl.WriteBinary(level);
        }




        private static void CorrectSequenceObjects(ExportEntry seq, VTestOptions vTestOptions)
        {
            // Set ObjInstanceVersions to LE value
            if (seq.IsA("SequenceObject"))
            {
                if (LE1UnrealObjectInfo.SequenceObjects.TryGetValue(seq.ClassName, out var soi))
                {
                    seq.WriteProperty(new IntProperty(soi.ObjInstanceVersion, "ObjInstanceVersion"));
                }
                else
                {
                    Debug.WriteLine($"SequenceCorrection: Didn't correct {seq.UIndex} {seq.ObjectName}, not in LE1 ObjectInfo SequenceObjects");
                }

                var children = seq.GetChildren();
                foreach (var child in children)
                {
                    if (child is ExportEntry chExp)
                    {
                        CorrectSequenceObjects(chExp, vTestOptions);
                    }
                }
            }

            // Fix extra four bytes after SeqAct_Interp
            if (seq.ClassName == "SeqAct_Interp")
            {
                seq.WriteBinary(Array.Empty<byte>());
            }

            if (seq.ClassName == "SeqAct_SetInt")
            {
                seq.WriteProperty(new BoolProperty(true, "bIsUpdated"));
            }


            // Fix missing PropertyNames on VariableLinks
            if (seq.IsA("SequenceOp"))
            {
                var varLinks = seq.GetProperty<ArrayProperty<StructProperty>>("VariableLinks");
                if (varLinks is null) return;
                foreach (var t in varLinks.Values)
                {
                    string desc = t.GetProp<StrProperty>("LinkDesc").Value;

                    if (desc == "Target" && seq.ClassName == "SeqAct_SetBool")
                    {
                        t.Properties.AddOrReplaceProp(new NameProperty("Target", "PropertyName"));
                    }

                    if (desc == "Value" && seq.ClassName == "SeqAct_SetInt")
                    {
                        t.Properties.AddOrReplaceProp(new NameProperty("Values", "PropertyName"));
                    }
                }

                seq.WriteProperty(varLinks);
            }
        }

        private static Guid? tempDonorGuid = null;
        private static void CorrectTerrainMaterials(IMEPackage le1File)
        {
            if (tempDonorGuid == null)
            {
                using var donorMatP = MEPackageHandler.OpenMEPackage(Path.Combine(LE1Directory.CookedPCPath, "BIOA_PRO10_11_LAY.pcc"));
                var terrain = donorMatP.FindExport("TheWorld.PersistentLevel.Terrain_0");
                var terrbinD = ObjectBinary.From<Terrain>(terrain);
                tempDonorGuid = terrbinD.CachedTerrainMaterials[0].ID;
            }

            var fname = Path.GetFileNameWithoutExtension(le1File.FilePath);
            var terrains = le1File.Exports.Where(x => x.ClassName == "Terrain").ToList();
            foreach (var terrain in terrains)
            {
                var terrbin = ObjectBinary.From<Terrain>(terrain);

                foreach (var terrainMat in terrbin.CachedTerrainMaterials)
                {
                    terrainMat.ID = tempDonorGuid.Value;
                }

                terrain.WriteBinary(terrbin);
            }
        }

        // ME1 -> LE1 Prefab's Sequence class was changed to a subclass. No different props though.P
        private static void CorrectPrefabSequenceClass(IMEPackage le1File)
        {
            foreach (var le1Exp in le1File.Exports.Where(x => x.IsA("Prefab")))
            {
                var prefabSeqObj = le1Exp.GetProperty<ObjectProperty>("PrefabSequence");
                if (prefabSeqObj != null && prefabSeqObj.ResolveToEntry(le1File) is ExportEntry export)
                {
                    var prefabSeqClass = le1File.FindImport("Engine.PrefabSequence");
                    if (prefabSeqClass == null)
                    {
                        var seqClass = le1File.FindImport("Engine.Sequence");
                        prefabSeqClass = new ImportEntry(le1File, seqClass.Parent?.UIndex ?? 0, "PrefabSequence") { PackageFile = seqClass.PackageFile, ClassName = "Class" };
                        le1File.AddImport(prefabSeqClass);
                    }
                    Debug.WriteLine($"Corrected Sequence -> PrefabSequence class type for {le1Exp.InstancedFullPath}");
                    export.Class = prefabSeqClass;
                }
            }
        }

        private static void CorrectPathfindingNetwork(IMEPackage me1File, IMEPackage le1File)
        {
            var le1PL = le1File.FindExport("TheWorld.PersistentLevel");
            Level me1L = ObjectBinary.From<Level>(me1File.FindExport("TheWorld.PersistentLevel"));
            Level le1L = ObjectBinary.From<Level>(le1PL);

            PropertyCollection mcs = new PropertyCollection();
            mcs.AddOrReplaceProp(new FloatProperty(400, "Radius"));
            mcs.AddOrReplaceProp(new FloatProperty(400, "Height"));
            StructProperty maxPathSize = new StructProperty("Cylinder", mcs, "MaxPathSize");

            // Chain start and end
            if (me1L.NavListEnd.value != 0 && le1File.FindExport(me1File.GetUExport(me1L.NavListEnd.value).InstancedFullPath) is { } matchingNavEnd)
            {
                le1L.NavListEnd = new UIndex(matchingNavEnd.UIndex);

                if (me1L.NavListStart.value != 0 && le1File.FindExport(me1File.GetUExport(me1L.NavListStart.value).InstancedFullPath) is { } matchingNavStart)
                {
                    le1L.NavListStart = new UIndex(matchingNavStart.UIndex);

                    // TEST: Widen the size of each node to see if that's why BioActorFactory fires Cancelled
                    while (matchingNavStart != null)
                    {
                        int uindex = matchingNavStart.UIndex;
                        var props = matchingNavStart.GetProperties();
                        props.AddOrReplaceProp(maxPathSize);
                        var next = props.GetProp<ObjectProperty>("nextNavigationPoint");
                        matchingNavStart.WriteProperties(props);
                        matchingNavStart = next?.ResolveToEntry(le1File) as ExportEntry;
                        if (matchingNavStart == null && uindex != matchingNavEnd.UIndex)
                        {
                            Debugger.Break();
                        }
                    }
                }


            }



            // Cross level actors
            foreach (var exportIdx in me1L.CrossLevelActors)
            {
                var me1E = me1File.GetUExport(exportIdx.value);
                if (le1File.FindExport(me1E.InstancedFullPath) is { } crossLevelActor)
                {
                    le1L.CrossLevelActors.Add(new UIndex(crossLevelActor.UIndex));
                }
            }

            // Regenerate the 'End' struct cause it will have ported wrong
            #region ReachSpecs

            // Have to do LE1 -> ME1 for references as not all reachspecs may have been ported
            foreach (var le1Exp in le1File.Exports.Where(x => x.IsA("ReachSpec")))
            {
                var le1End = le1Exp.GetProperty<StructProperty>("End");
                if (le1End != null)
                {
                    var me1Exp = me1File.FindExport(le1Exp.InstancedFullPath);
                    var me1End = me1Exp.GetProperty<StructProperty>("End");
                    var le1Props = le1Exp.GetProperties();
                    le1Props.RemoveNamedProperty("End");

                    PropertyCollection newEnd = new PropertyCollection();
                    newEnd.Add(me1End.GetProp<StructProperty>("Guid"));

                    var me1EndEntry = me1End.GetProp<ObjectProperty>("Nav");
                    if (me1EndEntry != null)
                    {
                        newEnd.Add(new ObjectProperty(le1File.FindExport(me1File.GetUExport(me1EndEntry.Value).InstancedFullPath).UIndex, "Actor"));
                    }
                    else
                    {
                        newEnd.Add(new ObjectProperty(0, "Actor")); // This is probably cross level or end of chain
                    }

                    StructProperty nes = new StructProperty("ActorReference", newEnd, "End", true);
                    le1Props.AddOrReplaceProp(nes);
                    le1Exp.WriteProperties(le1Props);

                    // Test properties
                    le1Exp.GetProperties();
                }
            }
            #endregion

            le1PL.WriteBinary(le1L);
        }
        #endregion

        #region QA Methods
        public static void VTest_Check()
        {
            var vtestFinalFiles = Directory.GetFiles(PAEMPaths.VTest_FinalDestDir);
            var vtestFinalFilesAvailable = vtestFinalFiles.Select(x => Path.GetFileNameWithoutExtension(x).ToLower()).ToList();
            foreach (var v in vtestFinalFiles)
            {
                using var package = MEPackageHandler.OpenMEPackage(v);

                #region Check BioTriggerStream files exists
                var triggerStraems = package.Exports.Where(x => x.ClassName == "BioTriggerStream").ToList();
                foreach (var triggerStream in triggerStraems)
                {
                    var streamingStates = triggerStream.GetProperty<ArrayProperty<StructProperty>>("StreamingStates");
                    if (streamingStates != null)
                    {
                        foreach (var ss in streamingStates)
                        {
                            List<NameProperty> namesToCheck = new List<NameProperty>();
                            var inChunkName = ss.GetProp<NameProperty>("InChunkName");

                            if (inChunkName.Value.Name != "None" && !vtestFinalFilesAvailable.Contains(inChunkName.Value.Name.ToLower()))
                            {
                                Debug.WriteLine($"LEVEL MISSING (ICN): {inChunkName} in {triggerStream.UIndex} {triggerStream.ObjectName.Instanced}");
                            }

                            foreach (var levelNameProperty in ss.GetProp<ArrayProperty<NameProperty>>("VisibleChunkNames"))
                            {
                                var levelName = levelNameProperty.Value.Name;
                                if (levelName != "None" && !vtestFinalFilesAvailable.Contains(levelName.ToLower()))
                                {
                                    Debug.WriteLine($"LEVEL MISSING (VC): {levelName} in {triggerStream.UIndex} {triggerStream.ObjectName.Instanced}");
                                }
                            }

                            foreach (var levelNameProperty in ss.GetProp<ArrayProperty<NameProperty>>("LoadChunkNames"))
                            {
                                var levelName = levelNameProperty.Value.Name;
                                if (levelName != "None" && !vtestFinalFilesAvailable.Contains(levelName.ToLower()))
                                {
                                    Debug.WriteLine($"LEVEL MISSING (LC): {levelName} in {triggerStream.UIndex} {triggerStream.ObjectName.Instanced}");
                                }
                            }
                        }
                    }
                    else
                    {
                        Debug.WriteLine($"{triggerStream.InstancedFullPath} in {v} has NO StreamingStates!!");
                    }
                }
                #endregion

                #region Check Level has at least 2 actors

                var level = package.FindExport("TheWorld.PersistentLevel");
                {
                    if (level != null)
                    {
                        var levelBin = ObjectBinary.From<Level>(level);
                        Debug.WriteLine($"{Path.GetFileName(v)} actor list count: {levelBin.Actors.Count}");
                    }
                }

                #endregion

            }
        }
        #endregion
    }
}