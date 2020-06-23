﻿using CrossMod.Nodes;
using CrossMod.Rendering;
using CrossMod.Rendering.Models;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;

namespace CrossModGui.ViewModels
{
    public class MainWindowViewModel : INotifyPropertyChanged
    {
        public class BoneTreeItem
        {
            public string Name { get; set; }

            public List<BoneTreeItem> Children { get; set;  } = new List<BoneTreeItem>();
        }

        public class MeshListItem
        {
            public string Name { get; set; }

            public bool IsChecked { get; set; }
        }

        public ViewportRenderer Renderer { get; set; }

        public ObservableCollection<FileNode> FileTreeItems { get; } = new ObservableCollection<FileNode>();

        public ObservableCollection<BoneTreeItem> BoneTreeItems { get; } = new ObservableCollection<BoneTreeItem>();

        public ObservableCollection<MeshListItem> MeshListItems { get; } = new ObservableCollection<MeshListItem>();

        public event PropertyChangedEventHandler PropertyChanged;

        public void Clear()
        {
            FileTreeItems.Clear();
            BoneTreeItems.Clear();
            MeshListItems.Clear();
        }

        public void PopulateFileTree(string folderPath)
        {
            // TODO: Populate subnodes after expanding the directory node.
            var rootNode = new DirectoryNode(folderPath);

            // TODO: Combine these two methods?
            rootNode.Open();
            rootNode.OpenChildNodes();

            FileTreeItems.Clear();
            FileTreeItems.Add(rootNode);
        }

        public void UpdateMeshesAndBones(IRenderable newNode)
        {
            if (newNode == null)
                return;

            // Duplicate nodes should still update the mesh list.
            if (newNode is RSkeleton skeleton)
            {
                AddSkeletonToGui(skeleton);
            }
            else if (newNode is IRenderableModel renderableModel)
            {
                AddMeshesToGui(renderableModel.GetModel());
                AddSkeletonToGui(renderableModel.GetSkeleton());
            }
        }

        public void UpdateCurrentRenderableNode(FileNode item)
        {
            BoneTreeItems.Clear();
            MeshListItems.Clear();

            // TODO: Textures?
            if (item is IRenderableNode node)
            {
                // TODO: Why can't these lines be switched?
                UpdateCurrentViewportRenderables(item.AbsolutePath, node);
                UpdateMeshesAndBones(node.GetRenderableNode());
            }
        }

        private void UpdateCurrentViewportRenderables(string name, IRenderableNode renderableNode)
        {
            if (renderableNode is NutexNode node)
                Renderer.UpdateTexture(node);
            else
                Renderer.UpdateTexture(null);

            Renderer.AddRenderableNode(name, renderableNode);
        }

        private void AddSkeletonToGui(RSkeleton skeleton)
        {
            if (skeleton == null)
                return;

            var root = CreateBoneTreeGetRoot(skeleton.Bones);
            BoneTreeItems.Add(root);
        }

        private void AddMeshesToGui(RModel model)
        {
            if (model == null)
                return;

            foreach (var mesh in model.subMeshes)
            {
                MeshListItems.Add(new MainWindowViewModel.MeshListItem
                {
                    Name = mesh.Name,
                    IsChecked = mesh.Visible
                });
            }
        }

        private static BoneTreeItem CreateBoneTreeGetRoot(IEnumerable<RBone> bones)
        {
            // The root bone has no parent.
            var boneItemById = bones.ToDictionary(b => b.Id, b => new BoneTreeItem { Name = b.Name });

            // Add each bone to its parent.
            BoneTreeItem root = null;
            foreach (var bone in bones)
            {
                if (bone.ParentId != -1)
                    boneItemById[bone.ParentId].Children.Add(boneItemById[bone.Id]);
                else
                    root = boneItemById[bone.Id];
            }

            return root;
        }
    }
}
