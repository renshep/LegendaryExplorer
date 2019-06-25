﻿//This class was generated by ME3Explorer
//Author: Warranty Voider
//URL: http://sourceforge.net/projects/me3explorer/
//URL: http://me3explorer.freeforums.org/
//URL: http://www.facebook.com/pages/Creating-new-end-for-Mass-Effect-3/145902408865659
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using ME3Explorer.Unreal;
using ME3Explorer.Packages;

namespace ME3Explorer.Unreal.Classes
{
    public class AnimNodeSlot
    {
        #region Unreal Props

        //Bool Properties

        public bool bSkipTickWhenZeroWeight;
        //Name Properties

        public string NodeName;
        //Float Properties

        public float NodeTotalWeight;
        //Array Properties

        public List<ChildrenEntry> Children;

        public struct ChildrenEntry
        {
            public string Name;
            public float Weight;
            public int Anim;
            public bool bMirrorSkeleton;
            public bool bIsAdditive;
        }

        #endregion

        public ExportEntry Export;
        public IMEPackage pcc;
        public byte[] data;

        public AnimNodeSlot(ExportEntry export)
        {
            pcc = export.FileRef;
            Export = export;
            data = export.Data;

            PropertyCollection props = export.GetProperties();

            NodeName = props.GetPropOrDefault<NameProperty>("NodeName").Value.InstancedString;
            bSkipTickWhenZeroWeight = props.GetPropOrDefault<BoolProperty>("bSkipTickWhenZeroWeight").Value;
            NodeTotalWeight = props.GetPropOrDefault<FloatProperty>("NodeTotalWeight").Value;
            Children = props.GetPropOrDefault<ArrayProperty<StructProperty>>("Children").Select(prop => new ChildrenEntry
            {
                Name = prop.GetPropOrDefault<NameProperty>("Name").Value.InstancedString,
                Weight = prop.GetPropOrDefault<FloatProperty>("Weight").Value,
                Anim = prop.GetPropOrDefault<ObjectProperty>("Anim").Value,
                bIsAdditive = prop.GetPropOrDefault<BoolProperty>("bIsAdditive").Value,
                bMirrorSkeleton = prop.GetPropOrDefault<BoolProperty>("bMirrorSkeleton").Value
            }).ToList();
        }

        public int GetArrayCount(byte[] raw)
        {
            return BitConverter.ToInt32(raw, 24);
        }

        public byte[] GetArrayContent(byte[] raw)
        {
            byte[] buff = new byte[raw.Length - 28];
            for (int i = 0; i < raw.Length - 28; i++)
                buff[i] = raw[i + 28];
            return buff;
        }

        public TreeNode ToTree()
        {
            TreeNode res = new TreeNode($"{Export.ObjectName}(#{Export.UIndex})");
            res.Nodes.Add("bSkipTickWhenZeroWeight : " + bSkipTickWhenZeroWeight);
            res.Nodes.Add("NodeName : " + NodeName);
            res.Nodes.Add("NodeTotalWeight : " + NodeTotalWeight);
            res.Nodes.Add(ChildrenToTree());
            return res;
        }

        public TreeNode ChildrenToTree()
        {
            TreeNode res = new TreeNode("Children");
            for (int i = 0; i < Children.Count; i++)
            {
                int idx = Children[i].Anim;
                TreeNode t = new TreeNode(i.ToString());
                t.Nodes.Add("Name : " + Children[i].Name);
                t.Nodes.Add("Weight : " + Children[i].Weight);
                t.Nodes.Add("Anim : " + Children[i].Anim);
                if (pcc.isUExport(idx))
                    switch (pcc.getUExport(idx).ClassName)
                    {
                        case "AnimNodeSlot":
                            AnimNodeSlot ans = new AnimNodeSlot(pcc.getUExport(idx));
                            t.Nodes.Add(ans.ToTree());
                            break;
                    }
                t.Nodes.Add("bIsMirrorSkeleton : " + Children[i].bMirrorSkeleton);
                t.Nodes.Add("bIsAdditive : " + Children[i].bIsAdditive);
                res.Nodes.Add(t);
            }
            return res;
        }

    }
}