﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.ComponentModel.Composition;

using SlimDX;
using SlimDX.Direct3D11;

using VVVV.PluginInterfaces.V2;
using VVVV.PluginInterfaces.V1;

using FeralTic.DX11;
using FeralTic.DX11.Resources;

using Microsoft.Kinect;
using System.Runtime.InteropServices;

namespace VVVV.DX11.Nodes.MSKinect
{
    [PluginInfo(Name = "Depth", 
	            Category = "Kinect2", 
	            Version = "Microsoft", 
	            Author = "flateric", 
	            Tags = "DX11, texture",
	            Help = "Returns a 16bit depthmap from the Kinects depth camera.")]
    public class KinectDepthTextureNode : KinectBaseTextureNode
    {
        private IntPtr depthread;
        private IntPtr depthwrite;

        private SlimDX.DXGI.Format format;
        private int width;
        private int height;
        private bool first = true;

        [ImportingConstructor()]
        public KinectDepthTextureNode(IPluginHost host)
        {
            this.InitBuffers();
        }

        private void InitBuffers()
        {
            this.format = SlimDX.DXGI.Format.R16_UInt;
            this.width = 512;
            this.height = 424;

            this.depthread = Marshal.AllocHGlobal(512 * 424 * 2);
            this.depthwrite = Marshal.AllocHGlobal(512 * 424 * 2);
        }

        private void DepthFrameReady(object sender, DepthFrameArrivedEventArgs e)
        {
            var frame = e.FrameReference.AcquireFrame();

            if (frame != null)
            {
                using (frame)
                {
                    lock (m_lock)
                    {
                        frame.CopyFrameDataToIntPtr(this.depthwrite, 512 * 424 * 2);
                        IntPtr swap = this.depthread;
                        this.depthread = this.depthwrite;
                        this.depthwrite = swap;
                    }

                    this.FInvalidate = true;
                    this.frameindex = frame.RelativeTime.Ticks;
                }
            }
        }

        protected override int Width
        {
            get { return this.width; }
        }

        protected override int Height
        {
            get { return this.height; }
        }

        protected override SlimDX.DXGI.Format Format
        {
            get { return SlimDX.DXGI.Format.R16_UNorm; }
        }

        protected override void CopyData(DX11DynamicTexture2D texture)
        {
            lock (m_lock)
            {
                texture.WriteData(this.depthread, 512 * 424 * 2);
            }
        }

        protected override void OnRuntimeConnected()
        {
            this.runtime.DepthFrameReady += DepthFrameReady;
        }

        protected override void OnRuntimeDisconnected()
        {
            this.runtime.DepthFrameReady -= DepthFrameReady;
        }

        protected override void Disposing()
        {
            Marshal.FreeHGlobal(this.depthread);
            Marshal.FreeHGlobal(depthwrite);
        }
    }
}
