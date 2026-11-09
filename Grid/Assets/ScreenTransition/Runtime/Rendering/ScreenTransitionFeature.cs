using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.RenderGraphModule;
using UnityEngine.Rendering.Universal;

namespace ScreenTransitions
{
    // Composite only the explicitly selected game camera, never the door camera.
    public sealed class ScreenTransitionFeature : ScriptableRendererFeature
    {
        public Material compositeMaterial;
        private DoorPass pass;
        public static Camera TargetCamera { get; set; }
        public static RenderTexture Overlay { get; set; }
        public static float ClosedCoverage { get; set; }

        public override void Create()
        {
            pass?.Dispose();
            pass = new DoorPass { renderPassEvent = RenderPassEvent.AfterRenderingPostProcessing };
        }

        public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
        {
            if (Overlay == null || compositeMaterial == null || renderingData.cameraData.camera != TargetCamera)
                return;
            pass.Prepare(Overlay, compositeMaterial);
            renderer.EnqueuePass(pass);
        }

        protected override void Dispose(bool disposing) => pass?.Dispose();

        private sealed class DoorPass : ScriptableRenderPass
        {
            private RTHandle overlay;
            private Material material;
            private RenderTexture current;
            private sealed class PassData
            {
                public TextureHandle texture;
                public RenderTexture source;
                public Material material;
                public float closedCoverage;
            }

            public void Prepare(RenderTexture texture, Material composite)
            {
                if (current != texture)
                {
                    overlay?.Release();
                    // The camera target also contains depth. Import only its color view;
                    // importing the RenderTexture directly rejects the combined descriptor.
                    overlay = RTHandles.Alloc(new RenderTargetIdentifier(texture), "Shelter Door Color View");
                    current = texture;
                }
                material = composite;
            }

            public override void RecordRenderGraph(RenderGraph graph, ContextContainer frameData)
            {
                var resources = frameData.Get<UniversalResourceData>();
                using (var builder = graph.AddRasterRenderPass<PassData>("Shelter Door Composite", out var data))
                {
                    var colorInfo = new RenderTargetInfo
                    {
                        width = current.width,
                        height = current.height,
                        volumeDepth = current.volumeDepth,
                        msaaSamples = current.antiAliasing,
                        format = current.graphicsFormat,
                        bindMS = current.bindTextureMS
                    };
                    data.texture = graph.ImportTexture(overlay, colorInfo);
                    data.source = current;
                    data.material = material;
                    data.closedCoverage = ClosedCoverage;
                    builder.UseTexture(data.texture, AccessFlags.Read);
                    builder.SetRenderAttachment(resources.activeColorTexture, 0, AccessFlags.ReadWrite);
                    builder.SetRenderFunc(static (PassData d, RasterGraphContext context) =>
                    {
                        // An identifier-only RTHandle cannot be converted to a Texture for
                        // Blitter's MaterialPropertyBlock. Bind the original color texture.
                        var properties = new MaterialPropertyBlock();
                        properties.SetTexture("_BlitTexture", d.source);
                        properties.SetVector("_BlitScaleBias", new Vector4(1, 1, 0, 0));
                        properties.SetFloat("_ClosedCoverage", d.closedCoverage);
                        context.cmd.DrawProcedural(Matrix4x4.identity, d.material, 0,
                            MeshTopology.Triangles, 3, 1, properties);
                    });
                }
            }

            public void Dispose() { overlay?.Release(); overlay = null; current = null; }
        }
    }
}
