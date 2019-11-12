﻿//This class was generated by ME3Explorer
//Author: Warranty Voider
//URL: http://sourceforge.net/projects/me3explorer/
//URL: http://me3explorer.freeforums.org/
//URL: http://www.facebook.com/pages/Creating-new-end-for-Mass-Effect-3/145902408865659
using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using ME3Explorer;
using ME3Explorer.Unreal;
using ME3Explorer.Packages;
using SharpDX;
using StreamHelpers;

namespace ME3Explorer.Unreal.Classes
{
    public class AnimSequence
    {
        #region Unreal Props

        //Byte Properties

        public string RotationCompressionFormat;
        public string KeyEncodingFormat;
        //Bool Properties

        public bool bIsAdditive;
        public bool bNoLoopingInterpolation;
        //Name Properties

        public string SequenceName;
        //Object Properties

        public int m_pBioAnimSetData;
        //Float Properties

        public float SequenceLength;
        public float RateScale;
        //Integer Properties

        public int NumFrames;
        //Array Properties

        public List<TrackOffsets> CompressedTrackOffsets;

        public struct TrackOffsets
        {
            public int TransOffset;
            public int TransNumKeys;
            public Vector3 Trans;
            public int RotOffset;
            public int RotNumKeys;
            public Vector4 Rot;
        }

        #endregion

        public float Rate;

        public IMEPackage pcc;
        private readonly ExportEntry Export;
        private readonly PropertyCollection Props;
        public byte[] data;
        public byte[] CompressedBlob;

        public AnimSequence(ExportEntry export)
        {
            pcc = export.FileRef;
            Export = export;
            data = export.Data;

            Props = export.GetProperties();

            RotationCompressionFormat = Props.GetPropOrDefault<EnumProperty>("RotationCompressionFormat").Value.Instanced;
            KeyEncodingFormat = Props.GetPropOrDefault<EnumProperty>("KeyEncodingFormat").Value.Instanced;
            bIsAdditive = Props.GetPropOrDefault<BoolProperty>("bIsAdditive").Value;
            bNoLoopingInterpolation = Props.GetPropOrDefault<BoolProperty>("bNoLoopingInterpolation").Value;
            SequenceName = Props.GetPropOrDefault<NameProperty>("SequenceName").Value.Instanced;
            m_pBioAnimSetData = Props.GetPropOrDefault<ObjectProperty>("m_pBioAnimSetData").Value;
            SequenceLength = Props.GetPropOrDefault<FloatProperty>("SequenceLength").Value;
            RateScale = Props.GetProp<FloatProperty>("RateScale")?.Value ?? 1;
            NumFrames = Props.GetPropOrDefault<IntProperty>("NumFrames").Value;
            ReadTrackOffsets(Props.GetPropOrDefault<ArrayProperty<IntProperty>>("CompressedTrackOffsets").Select(p => p.Value).ToArray());
            ReadCompressedBlob();
            Rate = NumFrames / SequenceLength * RateScale;
        }

        public void ReadTrackOffsets(int[] raw)
        {
            CompressedTrackOffsets = new List<TrackOffsets>();
            for (int i = 0; i < raw.Length / 4; i++)
            {
                CompressedTrackOffsets.Add(new TrackOffsets
                {
                    TransOffset = raw[i],
                    TransNumKeys = raw[i + 1],
                    RotOffset = raw[i + 2],
                    RotNumKeys = raw[i + 3]
                });
            }
        }

        public void ReadCompressedBlob()
        {
            int pos = Props.endOffset;
            int size = BitConverter.ToInt32(data, pos);
            CompressedBlob = new byte[size];
            pos += 4;
            for (int i = 0; i < size; i++)
                CompressedBlob[i] = data[pos + i];
            for (int i = 0; i < CompressedTrackOffsets.Count; i++)
            {
                TrackOffsets t = CompressedTrackOffsets[i];
                t.Trans = ReadVector3(t.TransOffset);
                Vector3 CRot = ReadVector3(t.RotOffset);
                t.Rot = DecompressVector3(CRot);
                CompressedTrackOffsets[i] = t;
            }
        }

        public Vector4 DecompressVector3(Vector3 v)
        {
            Vector4 r = new Vector4(v.X, v.Y, v.Z, 0);
            float l = r.LengthSquared();
            if (l == 0)
                r.W = -1;
            else if (l > 0 && l < 1.0f)
                r.W = (float)Math.Sqrt(1f - r.X * r.X - r.Y * r.Y - r.Z * r.Z) * -1f;
            return r;
        }

        public Vector4 ReadVector4(int pos)
        {
            Vector4 q = new Vector4();
            q.X = BitConverter.ToSingle(CompressedBlob, pos);
            q.Y = BitConverter.ToSingle(CompressedBlob, pos + 4) * -1f;
            q.Z = BitConverter.ToSingle(CompressedBlob, pos + 8);
            q.W = BitConverter.ToSingle(CompressedBlob, pos + 12);
            return q;
        }

        public Vector3 ReadVector3(int pos)
        {
            Vector3 q = new Vector3
            {
                X = BitConverter.ToSingle(CompressedBlob, pos),
                Y = BitConverter.ToSingle(CompressedBlob, pos + 4) * -1f,
                Z = BitConverter.ToSingle(CompressedBlob, pos + 8)
            };
            return q;
        }

        public TreeNode ToTree()
        {
            TreeNode res = new TreeNode($"{Export.ObjectName.Instanced}(#{Export.UIndex})");
            res.Nodes.Add("RotationCompressionFormat : " + RotationCompressionFormat);
            res.Nodes.Add("KeyEncodingFormat : " + KeyEncodingFormat);
            res.Nodes.Add("bIsAdditive : " + bIsAdditive);
            res.Nodes.Add("bNoLoopingInterpolation : " + bNoLoopingInterpolation);
            res.Nodes.Add("SequenceName : " + SequenceName);
            res.Nodes.Add("m_pBioAnimSetData : " + m_pBioAnimSetData);
            res.Nodes.Add("SequenceLength : " + SequenceLength);
            res.Nodes.Add("RateScale : " + RateScale);
            res.Nodes.Add("NumFrames : " + NumFrames);
            res.Nodes.Add(TracksToTree());
            return res;
        }

        public TreeNode TracksToTree()
        {
            TreeNode res = new TreeNode("CompressedTracks");
            for (int i = 0; i < CompressedTrackOffsets.Count; i++)
            {
                TrackOffsets t = CompressedTrackOffsets[i];
                string s = $"{i} :  Location( {t.Trans.X}, {t.Trans.Y}, {t.Trans.Z}) Trans.Numkeys({t.TransNumKeys}) ";
                s += $"Rotation( {t.Rot.X}, {t.Rot.Y}, {t.Rot.Z}, {t.Rot.W}) Rot.Numkeys({t.RotNumKeys})";
                res.Nodes.Add(s);
            }
            return res;
        }

        public void ImportKeys(Vector3[] loc, Vector4[] rot, int time)
        {
            MemoryStream m = new MemoryStream();
            int pos = 0;
            var temp = new int[CompressedTrackOffsets.Count];
            for (int i = 0; i < CompressedTrackOffsets.Count; i++)
            {
                m.Write(BitConverter.GetBytes(loc[i].X), 0, 4);
                m.Write(BitConverter.GetBytes(loc[i].Y * -1f), 0, 4);
                m.Write(BitConverter.GetBytes(loc[i].Z), 0, 4);
                m.Write(BitConverter.GetBytes(rot[i].X), 0, 4);
                m.Write(BitConverter.GetBytes(rot[i].Y * -1f), 0, 4);
                m.Write(BitConverter.GetBytes(rot[i].Z), 0, 4);
                TrackOffsets t = CompressedTrackOffsets[i];
                t.Trans = loc[i];
                t.TransOffset = pos;
                t.TransNumKeys = 0;
                t.Rot = rot[i];
                t.RotOffset = pos + 12;
                t.RotNumKeys = time;
                CompressedTrackOffsets[i] = t;
                pos += 24;

                temp[i * 4] = t.TransOffset;
                temp[i * 4 + 1] = t.TransNumKeys;
                temp[i * 4 + 2] = t.RotOffset;
                temp[i * 4 + 3] = t.RotNumKeys;
            }

            Props.AddOrReplaceProp(new ArrayProperty<IntProperty>(temp.Select(i => new IntProperty(i)), "CompressedTrackOffsets"));
            Props.AddOrReplaceProp(new IntProperty(time, "NumFrames"));
            CompressedBlob = m.ToArray();
        }

        public void SaveChanges()
        {
            Export.WriteProperties(Props);
            MemoryStream m = new MemoryStream();
            m.WriteInt32(CompressedBlob.Length);
            m.WriteFromBuffer(CompressedBlob);
            Export.SetBinaryData(m.ToArray());
        }

    }
}