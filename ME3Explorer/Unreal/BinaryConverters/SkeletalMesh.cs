﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ME3Explorer.Packages;
using SharpDX;
using StreamHelpers;

namespace ME3Explorer.Unreal.BinaryConverters
{
    public class SkeletalMesh : ObjectBinary
    {
        public BoxSphereBounds Bounds;
        public UIndex[] Materials;
        public Vector3 Origin;
        public Rotator RotOrigin;
        public MeshBone[] RefSkeleton;
        public int SkeletalDepth;
        public StaticLODModel[] LODModels;
        public OrderedMultiValueDictionary<NameReference, int> NameIndexMap;
        public PerPolyBoneCollisionData[] PerPolyBoneKDOPs;
        public string[] BoneBreakNames; //ME3 and UDK
        public UIndex[] ClothingAssets; //ME3 and UDK

        protected override void Serialize(SerializingContainer2 sc)
        {
            sc.Serialize(ref Bounds);
            sc.Serialize(ref Materials, SCExt.Serialize);
            sc.Serialize(ref Origin);
            sc.Serialize(ref RotOrigin);
            sc.Serialize(ref RefSkeleton, SCExt.Serialize);
            sc.Serialize(ref SkeletalDepth);
            sc.Serialize(ref LODModels, SCExt.Serialize);
            sc.Serialize(ref NameIndexMap, SCExt.Serialize, SCExt.Serialize);
            sc.Serialize(ref PerPolyBoneKDOPs, SCExt.Serialize);

            if (sc.Game >= MEGame.ME3)
            {
                sc.Serialize(ref BoneBreakNames, SCExt.Serialize);
                sc.Serialize(ref ClothingAssets, SCExt.Serialize);
            }
            else
            {
                BoneBreakNames = Array.Empty<string>();
                ClothingAssets = Array.Empty<UIndex>();
            }
        }

        public override List<(UIndex, string)> GetUIndexes(MEGame game)
        {
            List<(UIndex t, string)> uIndexes = Materials.Select((t, i) => (t, $"Materials[{i}]")).ToList();

            if (game == MEGame.ME3)
            {
                uIndexes.AddRange(ClothingAssets.Select((t, i) => (t, $"ClothingAssets[{i}]")));
            }
            return uIndexes;
        }
    }

    public class MeshBone
    {
        public NameReference Name;
        public uint Flags;
        public Quaternion Orientation;
        public Vector3 Position;
        public int NumChildren;
        public int ParentIndex;
        public Color BoneColor; //ME3 and UDK
    }

    public class StaticLODModel
    {
        public SkelMeshSection[] Sections;
        public bool NeedsCPUAccess; //UDK
        public byte DataTypeSize; //UDK
        public ushort[] IndexBuffer; //BulkSerialized
        public ushort[] ShadowIndices; //not in UDK
        public ushort[] ActiveBoneIndices;
        public byte[] ShadowTriangleDoubleSided; //not in UDK
        public SkelMeshChunk[] Chunks;
        public uint Size;
        public uint NumVertices;
        public MeshEdge[] Edges; //Not in UDK
        public byte[] RequiredBones;
        public ushort[] RawPointIndices; //BulkData
        public uint NumTexCoords; //UDK
        public SoftSkinVertex[] ME1VertexBufferGPUSkin; //BulkSerialized
        public SkeletalMeshVertexBuffer VertexBufferGPUSkin;
    }

    public class SkelMeshSection
    {
        public ushort MaterialIndex;
        public ushort ChunkIndex;
        public uint BaseIndex;
        public int NumTriangles; //ushort in ME1 and ME2
        public byte TriangleSorting; //UDK
    }

    public class SkelMeshChunk
    {
        public uint BaseVertexIndex;
        public RigidSkinVertex[] RigidVertices;
        public SoftSkinVertex[] SoftVertices;
        public ushort[] BoneMap;
        public int NumRigidVertices;
        public int NumSoftVertices;
        public int MaxBoneInfluences;
    }

    public class RigidSkinVertex
    {
        public Vector3 Position;
        public PackedNormal TangentX;
        public PackedNormal TangentY;
        public PackedNormal TangentZ;
        public Vector2 UV;
        public Vector2 UV2; //UDK
        public Vector2 UV3; //UDK
        public Vector2 UV4; //UDK
        public Color BoneColor; //UDK
        public byte Bone;
    }

    public class SoftSkinVertex
    {
        public Vector3 Position;
        public PackedNormal TangentX;
        public PackedNormal TangentY;
        public PackedNormal TangentZ;
        public Vector2 UV;
        public Vector2 UV2; //UDK
        public Vector2 UV3; //UDK
        public Vector2 UV4; //UDK
        public Color BoneColor; //UDK
        public byte[] InfluenceBones = new byte[4];
        public byte[] InfluenceWeights = new byte[4];
    }

    public class SkeletalMeshVertexBuffer
    {
        public int NumTexCoords; //UDK
        public bool bUseFullPrecisionUVs; //should always be false
        public bool bUsePackedPosition; //ME3 or UDK
        public Vector3 MeshExtension; //ME3 or UDK
        public Vector3 MeshOrigin; //ME3 or UDK
        public GPUSkinVertex[] VertexData; //BulkSerialized
    }

    public class GPUSkinVertex
    {
        public PackedNormal TangentX;
        public PackedNormal TangentZ;
        public byte[] InfluenceBones = new byte[4];
        public byte[] InfluenceWeights = new byte[4];
        public Vector3 Position; //serialized first in ME2
        public Vector2DHalf UV;
    }

    public class PerPolyBoneCollisionData
    {
        public kDOPTree kDOPTreeME1ME2;
        public kDOPTreeCompact kDOPTreeME3UDK;
        public Vector3[] CollisionVerts;
    }
}

namespace ME3Explorer
{
    using Unreal.BinaryConverters;

    public static partial class SCExt
    {
        public static void Serialize(this SerializingContainer2 sc, ref MeshBone mb)
        {
            if (sc.IsLoading)
            {
                mb = new MeshBone();
            }
            sc.Serialize(ref mb.Name);
            sc.Serialize(ref mb.Flags);
            sc.Serialize(ref mb.Orientation);
            sc.Serialize(ref mb.Position);
            sc.Serialize(ref mb.NumChildren);
            sc.Serialize(ref mb.ParentIndex);
            if (sc.Game >= MEGame.ME3)
            {
                sc.Serialize(ref mb.BoneColor);
            }
        }
        public static void Serialize(this SerializingContainer2 sc, ref SkelMeshSection sms)
        {
            if (sc.IsLoading)
            {
                sms = new SkelMeshSection();
            }
            sc.Serialize(ref sms.MaterialIndex);
            sc.Serialize(ref sms.ChunkIndex);
            sc.Serialize(ref sms.BaseIndex);
            if (sc.Game >= MEGame.ME3)
            {
                sc.Serialize(ref sms.NumTriangles);
            }
            else
            {
                ushort tmp = (ushort)sms.NumTriangles;
                sc.Serialize(ref tmp);
                sms.NumTriangles = tmp;
            }

            if (sc.Game == MEGame.UDK)
            {
                sc.Serialize(ref sms.TriangleSorting);
            }
        }
        public static void Serialize(this SerializingContainer2 sc, ref RigidSkinVertex rsv)
        {
            if (sc.IsLoading)
            {
                rsv = new RigidSkinVertex();
            }
            sc.Serialize(ref rsv.Position);
            sc.Serialize(ref rsv.TangentX);
            sc.Serialize(ref rsv.TangentY);
            sc.Serialize(ref rsv.TangentZ);
            sc.Serialize(ref rsv.UV);
            if (sc.Game == MEGame.UDK)
            {
                sc.Serialize(ref rsv.UV2);
                sc.Serialize(ref rsv.UV3);
                sc.Serialize(ref rsv.UV4);
                sc.Serialize(ref rsv.BoneColor);
            }
            sc.Serialize(ref rsv.Bone);
        }
        public static void Serialize(this SerializingContainer2 sc, ref SoftSkinVertex ssv)
        {
            if (sc.IsLoading)
            {
                ssv = new SoftSkinVertex();
            }
            sc.Serialize(ref ssv.Position);
            sc.Serialize(ref ssv.TangentX);
            sc.Serialize(ref ssv.TangentY);
            sc.Serialize(ref ssv.TangentZ);
            sc.Serialize(ref ssv.UV);
            if (sc.Game == MEGame.UDK)
            {
                sc.Serialize(ref ssv.UV2);
                sc.Serialize(ref ssv.UV3);
                sc.Serialize(ref ssv.UV4);
                sc.Serialize(ref ssv.BoneColor);
            }
            for (int i = 0; i < 4; i++)
            {
                sc.Serialize(ref ssv.InfluenceBones[i]);
            }
            for (int i = 0; i < 4; i++)
            {
                sc.Serialize(ref ssv.InfluenceWeights[i]);
            }
        }
        public static void Serialize(this SerializingContainer2 sc, ref SkelMeshChunk smc)
        {
            if (sc.IsLoading)
            {
                smc = new SkelMeshChunk();
            }
            sc.Serialize(ref smc.BaseVertexIndex);
            sc.Serialize(ref smc.RigidVertices, Serialize);
            sc.Serialize(ref smc.SoftVertices, Serialize);
            sc.Serialize(ref smc.BoneMap, Serialize);
            sc.Serialize(ref smc.NumRigidVertices);
            sc.Serialize(ref smc.NumSoftVertices);
            sc.Serialize(ref smc.MaxBoneInfluences);
        }
        public static void Serialize(this SerializingContainer2 sc, ref GPUSkinVertex gsv)
        {
            if (sc.IsLoading)
            {
                gsv = new GPUSkinVertex();
            }

            if (sc.Game == MEGame.ME2)
            {
                sc.Serialize(ref gsv.Position);
            }
            sc.Serialize(ref gsv.TangentX);
            sc.Serialize(ref gsv.TangentZ);
            for (int i = 0; i < 4; i++)
            {
                sc.Serialize(ref gsv.InfluenceBones[i]);
            }
            for (int i = 0; i < 4; i++)
            {
                sc.Serialize(ref gsv.InfluenceWeights[i]);
            }
            if (sc.Game >= MEGame.ME3)
            {
                sc.Serialize(ref gsv.Position);
            }
            sc.Serialize(ref gsv.UV);
        }
        public static void Serialize(this SerializingContainer2 sc, ref SkeletalMeshVertexBuffer svb)
        {
            if (sc.IsLoading)
            {
                svb = new SkeletalMeshVertexBuffer();
            }

            if (sc.Game == MEGame.UDK)
            {
                sc.Serialize(ref svb.NumTexCoords);
            }
            sc.Serialize(ref svb.bUseFullPrecisionUVs);
            if (svb.bUseFullPrecisionUVs)
            {
                throw new Exception($"SkeletalMesh is using Full precision UVs! Mesh in: {sc.Pcc.FilePath}");
            }
            if (sc.Game >= MEGame.ME3)
            {
                sc.Serialize(ref svb.bUsePackedPosition);
                if (svb.bUseFullPrecisionUVs)
                {
                    throw new Exception($"SkeletalMesh is using PackedPosition vertices! Mesh in: {sc.Pcc.FilePath}");
                }
                sc.Serialize(ref svb.MeshExtension);
                sc.Serialize(ref svb.MeshOrigin);
            }
            int elementSize = 32;
            sc.Serialize(ref elementSize);
            sc.Serialize(ref svb.VertexData, Serialize);
        }
        public static void Serialize(this SerializingContainer2 sc, ref StaticLODModel slm)
        {
            if (sc.IsLoading)
            {
                slm = new StaticLODModel();
            }
            sc.Serialize(ref slm.Sections, Serialize);
            if (sc.Game == MEGame.UDK)
            {
                sc.Serialize(ref slm.NeedsCPUAccess);
                sc.Serialize(ref slm.DataTypeSize);
            }
            int ushortSize = 2;
            sc.Serialize(ref ushortSize);
            sc.Serialize(ref slm.IndexBuffer, Serialize);
            if (sc.Game != MEGame.UDK)
            {
                sc.Serialize(ref slm.ShadowIndices, Serialize);
            }
            sc.Serialize(ref slm.ActiveBoneIndices, Serialize);
            if (sc.Game != MEGame.UDK)
            {
                sc.Serialize(ref slm.ShadowTriangleDoubleSided, Serialize);
            }
            sc.Serialize(ref slm.Chunks, Serialize);
            sc.Serialize(ref slm.Size);
            sc.Serialize(ref slm.NumVertices);
            if (sc.Game != MEGame.UDK)
            {
                sc.Serialize(ref slm.Edges, Serialize);
            }
            sc.Serialize(ref slm.RequiredBones, Serialize);
            if (sc.Game == MEGame.UDK)
            {
                int[] UDKRawPointIndices = sc.IsSaving ? Array.ConvertAll(slm.RawPointIndices, u => (int)u) : Array.Empty<int>();
                sc.SerializeBulkData(ref UDKRawPointIndices, Serialize);
                slm.RawPointIndices = Array.ConvertAll(UDKRawPointIndices, i => (ushort)i);
            }
            else
            {
                sc.SerializeBulkData(ref slm.RawPointIndices, Serialize);
            }
            if (sc.Game == MEGame.UDK)
            {
                sc.Serialize(ref slm.NumTexCoords);
            }
            if (sc.Game == MEGame.ME1)
            {
                if (sc.IsSaving && slm.ME1VertexBufferGPUSkin == null)
                {
                    GPUSkinVertex[] vertexData = slm.VertexBufferGPUSkin.VertexData;
                    slm.ME1VertexBufferGPUSkin = new SoftSkinVertex[vertexData.Length];
                    for (int i = 0; i < vertexData.Length; i++)
                    {
                        GPUSkinVertex vert = vertexData[i];
                        slm.ME1VertexBufferGPUSkin[i] = new SoftSkinVertex
                        {
                            Position = vert.Position,
                            TangentX = vert.TangentX,
                            TangentY = new PackedNormal(0, 1, 0, 0), //¯\_(ツ)_/¯
                            TangentZ = vert.TangentZ,
                            UV = new Vector2(vert.UV.X, vert.UV.Y),
                            InfluenceBones = vert.InfluenceBones.TypedClone(),
                            InfluenceWeights = vert.InfluenceWeights.TypedClone()
                        };
                    }
                }

                int softSkinVertexSize = 40;
                sc.Serialize(ref softSkinVertexSize);
                sc.Serialize(ref slm.ME1VertexBufferGPUSkin, Serialize);
            }
            else
            {
                if (sc.IsSaving && slm.VertexBufferGPUSkin == null)
                {
                    slm.VertexBufferGPUSkin = new SkeletalMeshVertexBuffer
                    {
                        MeshExtension = new Vector3(1, 1, 1),
                        NumTexCoords = 1,
                        VertexData = new GPUSkinVertex[slm.ME1VertexBufferGPUSkin.Length]
                    };
                    for (int i = 0; i < slm.ME1VertexBufferGPUSkin.Length; i++)
                    {
                        SoftSkinVertex vert = slm.ME1VertexBufferGPUSkin[i];
                        slm.VertexBufferGPUSkin.VertexData[i] = new GPUSkinVertex
                        {
                            Position = vert.Position,
                            TangentX = vert.TangentX,
                            TangentZ = vert.TangentZ,
                            UV = new Vector2DHalf(vert.UV.X, vert.UV.Y),
                            InfluenceBones = vert.InfluenceBones.TypedClone(),
                            InfluenceWeights = vert.InfluenceWeights.TypedClone()
                        };
                    }
                }
                sc.Serialize(ref slm.VertexBufferGPUSkin);
            }

            if (sc.Game >= MEGame.ME3)
            {
                int vertexInfluencesCount = 0;
                sc.Serialize(ref vertexInfluencesCount);
                if (vertexInfluencesCount != 0)
                {
                    throw new Exception($"VertexInfluences exist on this SkeletalMesh! Mesh in: {sc.Pcc.FilePath}");
                }
            }

            if (sc.Game == MEGame.UDK)
            {
                sc.Serialize(ref slm.NeedsCPUAccess);
                sc.Serialize(ref slm.DataTypeSize);
                int elementSize = 2;
                sc.Serialize(ref elementSize);
                ushort[] secondIndexBuffer = new ushort[0];
                sc.Serialize(ref secondIndexBuffer, Serialize);
            }
        }
        public static void Serialize(this SerializingContainer2 sc, ref PerPolyBoneCollisionData bcd)
        {
            if (sc.IsLoading)
            {
                bcd = new PerPolyBoneCollisionData();
            }
            if (sc.IsSaving)
            {
                if (sc.Game >= MEGame.ME3 && bcd.kDOPTreeME3UDK == null)
                {
                    bcd.kDOPTreeME3UDK = KDOPTreeBuilder.ToCompact(bcd.kDOPTreeME1ME2.Triangles, bcd.CollisionVerts);
                }
                else if (sc.Game <= MEGame.ME2 && bcd.kDOPTreeME1ME2 == null)
                {
                    //todo: need to convert kDOPTreeCompact to kDOPTree
                    throw new NotImplementedException("Cannot convert this SkeletalMesh to ME1 or ME2 format :(");
                }
            }
            if (sc.Game >= MEGame.ME3)
            {
                sc.Serialize(ref bcd.kDOPTreeME3UDK);
            }
            else
            {
                sc.Serialize(ref bcd.kDOPTreeME1ME2);
            }

            sc.Serialize(ref bcd.CollisionVerts, Serialize);
        }
    }
}