﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using LegendaryExplorer.Misc;
using LegendaryExplorer.Tools.TlkManagerNS;
using LegendaryExplorerCore.GameFilesystem;
using LegendaryExplorerCore.ME1;
using LegendaryExplorerCore.Misc;
using LegendaryExplorerCore.Packages;
using LegendaryExplorerCore.TLK;
using LegendaryExplorerCore.TLK.ME1;
using Newtonsoft.Json;

namespace LegendaryExplorer.UnrealExtensions
{
    public static class TLKLoader
    {
        public static readonly string LoadedTLKsPathME1 = Path.Combine(AppDirectories.AppDataFolder, "ME1LoadedTLKs.JSON");
        public static readonly string LoadedTLKsPathME2 = Path.Combine(AppDirectories.AppDataFolder, "ME2LoadedTLKs.JSON");
        public static readonly string LoadedTLKsPathME3 = Path.Combine(AppDirectories.AppDataFolder, "ME3LoadedTLKs.JSON");
        public static readonly string LoadedTLKsPathLE1 = Path.Combine(AppDirectories.AppDataFolder, "LE1LoadedTLKs.JSON");
        public static readonly string LoadedTLKsPathLE2 = Path.Combine(AppDirectories.AppDataFolder, "LE2LoadedTLKs.JSON");
        public static readonly string LoadedTLKsPathLE3 = Path.Combine(AppDirectories.AppDataFolder, "LE3LoadedTLKs.JSON");

        public static bool TlkFirstLoadDone { get; private set; } //Set when the TLK loading at startup is finished.

        private static void loadME1Tlk()
        {
            if (File.Exists(LoadedTLKsPathME1))
            {
                var files = JsonConvert.DeserializeObject<List<(int, string)>>(File.ReadAllText(LoadedTLKsPathME1));
                foreach ((int exportnum, string filename) in files)
                {
                    ME1TalkFiles.LoadTlkData(filename, exportnum);
                }
            }
            else
            {
                string path = ME1Directory.CookedPCPath + @"Packages\Dialog\GlobalTlk.upk";
                if (File.Exists(path))
                {
                    try
                    {
                        using IMEPackage pcc = MEPackageHandler.OpenME1Package(path);
                        ME1TalkFiles.tlkList.Add(new ME1TalkFile(pcc, 1));
                    }
                    catch (Exception)
                    {

                    }
                }
            }
        }

        private static void loadME2Tlk()
        {
            if (File.Exists(LoadedTLKsPathME2))
            {
                var files = JsonConvert.DeserializeObject<List<string>>(File.ReadAllText(LoadedTLKsPathME2));
                foreach (string filePath in files)
                {
                    try
                    {
                        ME2TalkFiles.LoadTlkData(filePath);
                    }
                    catch
                    {

                    }
                }
            }
            else
            {
                string tlkPath = ME2Directory.CookedPCPath + "BIOGame_INT.tlk";
                ME2TalkFiles.LoadTlkData(tlkPath);
            }
        }

        private static void loadME3Tlk()
        {
            if (File.Exists(LoadedTLKsPathME3))
            {
                var files = JsonConvert.DeserializeObject<List<string>>(File.ReadAllText(LoadedTLKsPathME3));
                foreach (string filePath in files)
                {
                    try
                    {
                        ME3TalkFiles.LoadTlkData(filePath);
                    }
                    catch
                    {

                    }
                }
            }
            else
            {
                string tlkPath = ME3Directory.CookedPCPath + "BIOGame_INT.tlk";
                ME3TalkFiles.LoadTlkData(tlkPath);
            }
        }

        private static void loadLE1Tlk()
        {
            if (File.Exists(LoadedTLKsPathLE1))
            {
                var files = JsonConvert.DeserializeObject<List<(int, string)>>(File.ReadAllText(LoadedTLKsPathLE1));
                foreach ((int exportnum, string filename) in files)
                {
                    LE1TalkFiles.LoadTlkData(filename, exportnum);
                }
            }
        }

        private static void loadLE2Tlk()
        {
            if (File.Exists(LoadedTLKsPathLE2))
            {
                var files = JsonConvert.DeserializeObject<List<string>>(File.ReadAllText(LoadedTLKsPathLE2));
                foreach (string filePath in files)
                {
                    try
                    {
                        LE2TalkFiles.LoadTlkData(filePath);
                    }
                    catch
                    {

                    }
                }
            }
            else
            {
                string tlkPath = LE2Directory.CookedPCPath + "BIOGame_INT.tlk";
                LE2TalkFiles.LoadTlkData(tlkPath);
            }
        }

        private static void loadLE3Tlk()
        {
            if (File.Exists(LoadedTLKsPathLE3))
            {
                List<string> files = JsonConvert.DeserializeObject<List<string>>(File.ReadAllText(LoadedTLKsPathLE3));
                foreach (string filePath in files)
                {
                    try
                    {
                        LE3TalkFiles.LoadTlkData(filePath);
                    }
                    catch
                    {

                    }
                }
            }
            else
            {
                string tlkPath = LE3Directory.CookedPCPath + "BIOGame_INT.tlk";
                LE3TalkFiles.LoadTlkData(tlkPath);
            }
        }

        //TODO: Call this from somewhere in LEX
        public static void LoadSavedTlkList()
        {
            Action[] loaders =
            {
                loadME1Tlk,
                loadME2Tlk,
                loadME3Tlk,
                loadLE1Tlk,
                loadLE2Tlk,
                loadLE3Tlk
            };
            Parallel.ForEach(loaders, action => action());
            string lastReloaded = $"{DateTime.Now:HH:mm:ss tt}";
            TLKManagerWPF.ME1LastReloaded = lastReloaded;
            TLKManagerWPF.ME2LastReloaded = lastReloaded;
            TLKManagerWPF.ME3LastReloaded = lastReloaded;
            TLKManagerWPF.LE1LastReloaded = lastReloaded;
            TLKManagerWPF.LE2LastReloaded = lastReloaded;
            TLKManagerWPF.LE3LastReloaded = lastReloaded;
            TlkFirstLoadDone = true;
            //Large Object Heap is extraordinarily fragmented after tlks are loaded for some reason.
            MemoryAnalyzer.ForceFullGC(true);
        }

        public static void SaveTLKList(MEGame game = MEGame.Unknown)
        {
            if (game == MEGame.Unknown || game == MEGame.ME1) File.WriteAllText(LoadedTLKsPathME1, JsonConvert.SerializeObject(ME1TalkFiles.tlkList.Select(x => (uindex: x.UIndex, x.FilePath))));
            if (game == MEGame.Unknown || game == MEGame.ME2) File.WriteAllText(LoadedTLKsPathME2, JsonConvert.SerializeObject(ME2TalkFiles.tlkList.Select(x => x.path)));
            if (game == MEGame.Unknown || game == MEGame.ME3) File.WriteAllText(LoadedTLKsPathME3, JsonConvert.SerializeObject(ME3TalkFiles.tlkList.Select(x => x.path)));
            if (game == MEGame.Unknown || game == MEGame.LE1) File.WriteAllText(LoadedTLKsPathLE1, JsonConvert.SerializeObject(LE1TalkFiles.tlkList.Select(x => (uindex: x.UIndex, x.FilePath))));
            if (game == MEGame.Unknown || game == MEGame.LE2) File.WriteAllText(LoadedTLKsPathLE2, JsonConvert.SerializeObject(LE2TalkFiles.tlkList.Select(x => x.path)));
            if (game == MEGame.Unknown || game == MEGame.LE3) File.WriteAllText(LoadedTLKsPathLE3, JsonConvert.SerializeObject(LE3TalkFiles.tlkList.Select(x => x.path)));
        }
    }
}