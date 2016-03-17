﻿using HelixToolkit.Wpf;
using Mocap.Core;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media;
using System.Windows.Media.Media3D;

namespace Mocap.VM
{
    public class BoneVM : INotifyPropertyChanged
    {
        public static double LinkThickness = 3;
        public static double SelectedLinkThickness = 5;

        public static Color LinkColor = Colors.DarkGray;
        public static Color SelectedLinkColor = Colors.Black;

        private bool isSelected;

        private CoordinateSystemVisual3D coordinateSystemVisual;

        // visuals to connect to child bones
        private Dictionary<BoneVM, LinesVisual3D> childLinkVisualMap = new Dictionary<BoneVM, LinesVisual3D>();

        /// <summary>
        /// the underlying Bone model instance
        /// </summary>
        public Bone Model { get; set; }

        /// <summary>
        /// this instances parent node. null means that this is the root node
        /// </summary>
        public BoneVM Parent { get; }

        /// <summary>
        /// true if the current bone is selected in UI
        /// </summary>
        public bool IsSelected
        {
            get { return isSelected; }
            set
            {
                if (isSelected != value)
                {
                    isSelected = value;

                    UpdateVisuals();

                    PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(IsSelected)));
                }
            }
        }

        /// <summary>
        /// the name of this bone
        /// </summary>
        public string Name
        {
            get { return Model.Name; }
            set
            {
                if (Model.Name != value)
                {
                    Model.Name = value;
                    PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Name)));
                }
            }
        }

        /// <summary>
        /// the offset to the parent node.
        /// </summary>
        public Vector3D Offset
        {
            get { return Model.Offset; }
            set
            {
                Model.Offset = value;
                Parent?.UpdateLinkVisual(this);
            }
        }

        /// <summary>
        /// the local orientation of this bone
        /// </summary>
        public Quaternion LocalRotation { get { return Model.JointRotation; } set { Model.JointRotation = value; } }

        /// <summary>
        /// this bones associated sensor
        /// </summary>
        public SensorVM Sensor { get; set; }

        public Quaternion SensorToLocalTransform { get; set; }

        public ModelVisual3D Visual { get; }

        public ModelVisual3D WorldVisual { get; }

        public ObservableCollection<BoneVM> Children { get; }

        public event PropertyChangedEventHandler PropertyChanged;

        public BoneVM(Bone model, BoneVM parent)
        {
            Model = model;
            Parent = parent;

            DisplaySettings.Get.PropertyChanged += OnDisplaySettingsPropertyChanged;

            Visual = new ModelVisual3D();
            coordinateSystemVisual = new CoordinateSystemVisual3D();
            coordinateSystemVisual.ArrowLengths = DisplaySettings.Get.CSysSize;

            Visual.Children.Add(coordinateSystemVisual);

            WorldVisual = new ModelVisual3D();

            // create child bones
            Children = new ObservableCollection<BoneVM>();
            Children.CollectionChanged += OnChildrenChanged;
            foreach (var item in model.Children)
            {
                Children.Add(new BoneVM(item, this));
            }
        }

        private void OnChildrenChanged(object sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
        {
            if (e.Action == System.Collections.Specialized.NotifyCollectionChangedAction.Add)
            {
                foreach (BoneVM child in e.NewItems)
                {
                    Visual.Children.Add(child.Visual);
                    LinesVisual3D linkVisual = new LinesVisual3D();
                    linkVisual.Points = new Point3DCollection(new[] { new Point3D(0, 0, 0), child.Offset.ToPoint3D() });
                    childLinkVisualMap.Add(child, linkVisual);

                    Visual.Children.Add(linkVisual);
                    UpdateLinkVisual(child);

                    WorldVisual.Children.Add(child.WorldVisual);
                }
            }
            if (e.Action == System.Collections.Specialized.NotifyCollectionChangedAction.Remove)
            {
                foreach (BoneVM child in e.OldItems)
                {
                    Visual.Children.Remove(child.Visual);
                    Visual.Children.Remove(childLinkVisualMap[child]);
                    WorldVisual.Children.Remove(child.WorldVisual);
                }
            }
        }

        private void OnDisplaySettingsPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            UpdateVisuals();
        }

        private void UpdateLinkVisual(BoneVM child)
        {
            var lineVisual = childLinkVisualMap[child];

            lineVisual.Points[1] = child.Offset.ToPoint3D();

            if (IsSelected)
            {
                lineVisual.Thickness = SelectedLinkThickness;
                lineVisual.Color = SelectedLinkColor;
            }
            else
            {
                lineVisual.Thickness = LinkThickness;
                lineVisual.Color = LinkColor;
            }
        }

        private void UpdateVisuals()
        {
            coordinateSystemVisual.ArrowLengths = DisplaySettings.Get.CSysSize;
            foreach (var item in childLinkVisualMap)
            {
                UpdateLinkVisual(item.Key);
            }
        }

        public void Refresh()
        {
            Visual.Transform = new MatrixTransform3D(Model.LocalTransform);
            foreach (var item in Children)
            {
                item.Refresh();
            }
        }

        public void Traverse(Action<BoneVM> action)
        {
            action(this);

            foreach (var item in Children)
                item.Traverse(action);
        }
    }
}
