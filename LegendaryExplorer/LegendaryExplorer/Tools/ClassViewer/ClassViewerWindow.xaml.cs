﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using LegendaryExplorer.Misc;
using LegendaryExplorerCore.Misc;
using LegendaryExplorerCore.Unreal.ObjectInfo;

namespace LegendaryExplorer.Tools.ClassViewer
{
    /// <summary>
    /// Interaction logic for Window1.xaml
    /// </summary>
    public partial class ClassViewerWindow : NotifyPropertyChangedWindowBase
    {
        private ClassObject _selectedObject;
        public ClassObject SelectedObject
        {
            get => _selectedObject;
            set => SetProperty(ref _selectedObject, value);
        }

        public ClassViewerWindow()
        {

            // Pass 1: Build all classes
            Dictionary<string, ClassObject> objMap = new();
            foreach (var v in ME3UnrealObjectInfo.Classes)
            {
                ClassObject co = new ClassObject()
                {
                    ObjectName = v.Value.ClassName,
                    ObjectClass = v.Value.ClassName,
                };

                co.Descendents.AddRange(v.Value.properties.Select(x=>new ClassObject() { ObjectClass = x.Value.Type.ToString(), ObjectName = x.Key, IsProperty = true}));

                objMap[v.Value.ClassName] = co;
            }

            // Pass 2: Link heirarchy
            foreach (var v in ME3UnrealObjectInfo.Classes)
            {
                var classObj = objMap[v.Value.ClassName];
                //if (v.Value.ClassName.Contains(@"SFXWeapon"))
                //    Debugger.Break();
                if (v.Value.baseClass != null)
                {
                    if (objMap.TryGetValue(v.Value.baseClass, out var parent))
                    {
                        classObj.Parent = parent;
                        parent.Descendents.Add(classObj);
                    }
                    else
                    {
                        Debug.WriteLine($"Unknown parent: {v.Value.baseClass}");
                    }
                }
            }

            var roots = objMap.Values.Where(x => x.Parent == null).ToList();

            // Pass 3: Sort
            foreach (var v in roots)
            {
                SortChildren(v);
            }

            // Search for unattached nodes
            foreach (var v in objMap.Values)
            {
                if (v.Parent == null && !v.Descendents.Any())
                {
                    Debug.WriteLine($@"Unattached node: {v.ObjectName}");
                }
            }

            RootObjects.AddRange(roots);
            InitializeComponent();
        }

        private void SortChildren(ClassObject classObject)
        {
            foreach (var v in classObject.Descendents)
            {
                SortChildren(v);
            }

            classObject.Descendents.Sort(x => x.ObjectName);
        }


        public ObservableCollectionExtended<ClassObject> RootObjects { get; } = new();


        public class ClassObject : NotifyPropertyChangedBase
        {
            public ClassObject Parent { get; set; }
            public string ObjectName { get; set; }
            public string ObjectClass { get; set; }
            public ObservableCollectionExtended<ClassObject> Descendents { get; } = new();

            private bool _isExpanded;
            public bool IsExpanded
            {
                get => _isExpanded;
                set => SetProperty(ref _isExpanded, value);
            }

            private bool _isSelected;
            public bool IsSelected
            {
                get => _isSelected;
                set => SetProperty(ref _isSelected, value);
            }

            public bool IsProperty { get; set; }
        }
    }
}
