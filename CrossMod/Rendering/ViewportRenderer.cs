﻿using CrossMod.Nodes;
using CrossMod.Rendering.GlTools;
using OpenTK.Graphics.OpenGL;
using SFGraphics.Cameras;
using SFGraphics.Controls;
using SFGraphics.GLObjects.Framebuffers;
using SFGraphics.GLObjects.GLObjectManagement;
using System;
using System.Collections.Generic;

namespace CrossMod.Rendering
{
    public class ViewportRenderer
    {
        // TODO: Combine with renderable nodes as a tuple list?
        private readonly HashSet<string> renderableNodeNames = new HashSet<string>();
        private readonly List<IRenderable> renderableNodes = new List<IRenderable>();
     
        private IRenderable renderTexture;

        private readonly GLViewport glViewport;

        public Camera Camera { get; } = new Camera() { FarClipPlane = 500000 };

        public ViewportRenderer(GLViewport viewport)
        {
            glViewport = viewport;
        }

        public void AddRenderableNode(string name, IRenderableNode value)
        {
            if (value == null)
                return;

            SwitchContextToCurrentThreadAndPerformAction(() =>
            {
                var newNode = value.GetRenderableNode();

                // Prevent duplicates. Paths should be unique.
                if (!renderableNodeNames.Contains(name))
                {
                    renderableNodes.Add(newNode);
                    renderableNodeNames.Add(name);
                }

                if (value is NumdlNode)
                {
                    FrameSelection();
                }
            });
        }

        public void ClearRenderableNodes()
        {
            SwitchContextToCurrentThreadAndPerformAction(() =>
            {
                renderableNodeNames.Clear();
                renderableNodes.Clear();
                GC.WaitForPendingFinalizers();
                GLObjectManager.DeleteUnusedGLObjects();
            });
        }

        public void FrameSelection()
        {
            // Bounding spheres will help account for the vastly different model sizes.
            var spheres = new List<OpenTK.Vector4>();
            foreach (var node in renderableNodes)
            {
                // TODO: Make bounding spheres an interface.
                if (node is Rnumdl rnumdl && rnumdl.Model != null)
                {
                    spheres.Add(rnumdl.Model.BoundingSphere);
                }
            }

            var allModelBoundingSphere = SFGraphics.Utils.BoundingSphereGenerator.GenerateBoundingSphere(spheres);
            Camera.FrameBoundingSphere(allModelBoundingSphere, 0);
        }

        public void UpdateTexture(NutexNode texture)
        {
            SwitchContextToCurrentThreadAndPerformAction(() =>
            {
                var node = texture?.GetRenderableNode();
                renderTexture = node;
            });
        }

        public void ReloadShaders()
        {
            SwitchContextToCurrentThreadAndPerformAction(() =>
            {
                ShaderContainer.ReloadShaders();
            });
        }

        public void RenderNodes(ScriptNode scriptNode)
        {
            // Ensure shaders are created before drawing anything.
            if (!ShaderContainer.HasSetUp)
                ShaderContainer.SetUpShaders();

            SetUpViewport();

            if (renderTexture != null)
            {
                renderTexture.Render(Camera);
            }
            else if (renderableNodes != null)
            {
                foreach (var node in renderableNodes)
                    node.Render(Camera);

                ParamNodeContainer.Render(Camera);
                scriptNode?.Render(Camera);
            }
        }

        public System.Drawing.Bitmap GetScreenshot()
        {
            // Make sure the context is current on this thread.
            var wasRendering = glViewport.IsRendering;
            glViewport.PauseRendering();

            var bmp = Framebuffer.ReadDefaultFramebufferImagePixels(glViewport.Width, glViewport.Height, true);

            if (wasRendering)
                glViewport.RestartRendering();

            return bmp;
        }

        public void SwitchContextToCurrentThreadAndPerformAction(Action action)
        {
            // Make sure the context is current on this thread.
            var wasRendering = glViewport.IsRendering;
            glViewport.PauseRendering();

            action();

            if (wasRendering)
                glViewport.RestartRendering();
        }

        private static void SetUpViewport()
        {
            DrawBackgroundClearBuffers();
            SetRenderState();
        }

        private static void DrawBackgroundClearBuffers()
        {
            // TODO: Clearing can be skipped if there is a background to draw.
            GL.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit);
            GL.ClearColor(0.25f, 0.25f, 0.25f, 1);
        }

        private static void SetRenderState()
        {
            GL.Enable(EnableCap.DepthTest);
            GL.DepthFunc(DepthFunction.Lequal);

            GL.Enable(EnableCap.Blend);
            GL.BlendFunc(BlendingFactor.SrcAlpha, BlendingFactor.OneMinusSrcAlpha);

            GL.Enable(EnableCap.CullFace);
            GL.CullFace(CullFaceMode.Back);
        }
    }
}
