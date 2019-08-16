﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ME3Explorer.Packages;

namespace ME3Explorer.Unreal.BinaryConverters
{
    class Polys
    {
        public class Poly
        {
            public Vector Base;
            public Vector Normal;
            public Vector TextureU;
            public Vector TextureV;
            public Vector[] Vertices;
            public int PolyFlags;
            public int Actor;
            public NameReference ItemName;
            public int Material;
            public int iLink;
            public int iBrushPoly;
            public float ShadowMapScale;
            public int LightingChannels;
            public LightmassPrimitiveSettings LightmassSettings; //ME3 only
            public NameReference RulesetVariation; //ME3 only
        }

        public int PolyCount;
        public int PolyMax;
        public int Owner;
        public Poly[] Elements;

        public Polys(ExportEntry export)
        {
            Serialize(new SerializingContainer2(new MemoryStream(export.getBinaryData()), true), export.FileRef, export.Game);
        }

        public static Polys From(ExportEntry export)
        {
            return new Polys(export);
        }

        private void Serialize(SerializingContainer2 sc, IMEPackage pcc, MEGame game)
        {
            if (!sc.IsLoading)
            {
                PolyCount = Elements.Length;
            }
            sc.Serialize(ref PolyCount);
            sc.Serialize(ref PolyMax);
            sc.Serialize(ref Owner);
            if (sc.IsLoading)
            {
                Elements = new Poly[PolyCount];
            }

            for (int i = 0; i < PolyCount; i++)
            {
                if (sc.IsLoading)
                {
                    Elements[i] = new Poly();
                }
                var poly = Elements[i];

                sc.Serialize(ref poly.Base);
                sc.Serialize(ref poly.Normal);
                sc.Serialize(ref poly.TextureU);
                sc.Serialize(ref poly.TextureV);
                sc.Serialize(ref poly.Vertices);
                sc.Serialize(ref poly.PolyFlags);
                sc.Serialize(ref poly.Actor);
                sc.Serialize(ref poly.ItemName, pcc);
                sc.Serialize(ref poly.Material);
                sc.Serialize(ref poly.iLink);
                sc.Serialize(ref poly.iBrushPoly);
                sc.Serialize(ref poly.ShadowMapScale);
                sc.Serialize(ref poly.LightingChannels);
                if (game == MEGame.ME3)
                {
                    sc.Serialize(ref poly.LightmassSettings);
                    sc.Serialize(ref poly.RulesetVariation, pcc);
                }
                else if(sc.IsLoading)
                {
                    //defaults that won't break the lighting completely
                    poly.LightmassSettings = new LightmassPrimitiveSettings
                    {
                        FullyOccludedSamplesFraction = 1,
                        EmissiveLightFalloffExponent = 2,
                        DiffuseBoost = 1,
                        EmissiveBoost = 1,
                        SpecularBoost = 1,
                    };
                    poly.RulesetVariation = "None";
                }
            }
        }

        public byte[] Write(IMEPackage pcc, MEGame game)
        {
            var ms = new MemoryStream();
            Serialize(new SerializingContainer2(ms), pcc, game);
            return ms.ToArray();
        }
    }
}
