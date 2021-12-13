﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using LegendaryExplorerCore.Dialogue;
using LegendaryExplorerCore.Helpers;
using LegendaryExplorerCore.Packages;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace LegendaryExplorerCore.Tests
{
    [TestClass]
    public class BioConversationTests
    {
        [TestMethod]
        public void TestBioConversationReserialization()
        {
            GlobalTest.Init();
            var packagesPath = GlobalTest.GetTestPackagesDirectory();
            var packages = Directory.GetFiles(packagesPath, "*.*", SearchOption.AllDirectories);
            foreach (var p in packages)
            {
                if (p.RepresentsPackageFilePath())
                {
                    // Do not use package caching in tests
                    Console.WriteLine($"Opening package {p}");
                    (var game, var platform) = GlobalTest.GetExpectedTypes(p);
                    if (platform == MEPackage.GamePlatform.PC)
                    {
                        var loadedPackage = MEPackageHandler.OpenMEPackage(p, forceLoadFromDisk: true);
                        foreach (var bioConv in loadedPackage.Exports.Where(x => !x.IsDefaultObject && x.ClassName == "BioConversation"))
                        {
                            Console.WriteLine($"Testing reserialization of {bioConv.InstancedFullPath}");
                            var startData = bioConv.Data;
                            ConversationExtended ce = new ConversationExtended(bioConv);
                            ce.LoadConversation(null, true); // no tlk lookup
                            ce.SerializeNodes();
                            var endData = bioConv.Data;

                            if (!startData.SequenceEqual(endData))
                            {
                                DebugTools.DebugUtilities.CompareByteArrays(startData,endData);
                            }

                        }
                    }
                }
            }
        }
    }
}
