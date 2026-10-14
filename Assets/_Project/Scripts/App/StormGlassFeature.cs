using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.RenderGraphModule;
using UnityEngine.Rendering.RenderGraphModule.Util;
using UnityEngine.Rendering.Universal;

namespace FlockFive
{
    // Copies the garden color, then draws each bead as a lens on top.
    // Installed for the play session only. The renderer asset stays as shipped.
    public sealed class StormGlassFeature : ScriptableRendererFeature
    {
        static StormGlassFeature _feature;
        static bool _added;

        Material _mat;
        Pass _pass;
        readonly Vector4[] _rects = new Vector4[StormGlass.Cap];
        readonly Vector4[] _beads = new Vector4[StormGlass.Cap];

        public static void Install()
        {
            var data = RendererData();
            if (data == null) return;
            if (data.TryGetRendererFeature<StormGlassFeature>(out var have) && have != null)
                return;
            if (_feature == null)
            {
                _feature = CreateInstance<StormGlassFeature>();
                _feature.name = "StormGlass";
                _feature.hideFlags = HideFlags.HideAndDontSave;
            }
            data.rendererFeatures.Add(_feature);
            data.SetDirty();
            _added = true;
        }

        public static void Uninstall()
        {
            if (!_added) return;
            var data = RendererData();
            if (data != null && _feature != null)
            {
                data.rendererFeatures.Remove(_feature);
                data.SetDirty();
            }
            _added = false;
        }

        static ScriptableRendererData RendererData()
        {
            var pipe = GraphicsSettings.defaultRenderPipeline as UniversalRenderPipelineAsset;
            if (pipe == null) return null;
            var list = pipe.rendererDataList;
            if (list.Length == 0) return null;
            return list[0];
        }

        public override void Create()
        {
            if (_mat == null)
            {
                var shader = Resources.Load<Shader>("Shaders/StormGlass");
                if (shader == null) shader = Shader.Find("FlockFive/StormGlass");
                if (shader == null) return;
                _mat = CoreUtils.CreateEngineMaterial(shader);
            }
            _pass = new Pass(_mat, _rects, _beads);
            _pass.renderPassEvent = RenderPassEvent.AfterRenderingPostProcessing;
            _pass.requiresIntermediateTexture = true;
        }

        public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
        {
            if (_pass == null || _mat == null) return;
            if (!StormGlassRig.WantDraw) return;
            var cam = renderingData.cameraData.camera;
            if (cam == null || cam.cameraType != CameraType.Game) return;
            if (cam.GetComponent<PortraitLock>() == null) return;
            renderer.EnqueuePass(_pass);
        }

        protected override void Dispose(bool disposing)
        {
            _pass = null;
            CoreUtils.Destroy(_mat);
            _mat = null;
        }

        sealed class Pass : ScriptableRenderPass
        {
            static readonly int BeadId = Shader.PropertyToID("_Bead");
            static readonly int RadId = Shader.PropertyToID("_Rad");
            static readonly int BlitTexId = Shader.PropertyToID("_BlitTexture");
            static readonly int BlitScaleId = Shader.PropertyToID("_BlitScaleBias");
            static readonly int BlitRtId = Shader.PropertyToID("_BlitScaleBiasRt");
            static readonly int BlitMipId = Shader.PropertyToID("_BlitMipLevel");
            static readonly int BlitTexelId = Shader.PropertyToID("_BlitTexture_TexelSize");
            static readonly MaterialPropertyBlock Block = new MaterialPropertyBlock();

            readonly Material _mat;
            readonly Vector4[] _rects;
            readonly Vector4[] _beads;

            public Pass(Material mat, Vector4[] rects, Vector4[] beads)
            {
                _mat = mat;
                _rects = rects;
                _beads = beads;
            }

            class BeadData
            {
                public TextureHandle Source;
                public TextureHandle Destination;
                public Material Material;
                public int Count;
                public Vector4 R0, R1, R2, R3;
                public Vector4 B0, B1, B2, B3;
            }

            public override void RecordRenderGraph(RenderGraph renderGraph, ContextContainer frameData)
            {
                if (!StormGlassRig.WantDraw || _mat == null) return;
                var resource = frameData.Get<UniversalResourceData>();
                var camera = frameData.Get<UniversalCameraData>();
                if (resource.isActiveTargetBackBuffer) return;
                if (camera.camera.cameraType != CameraType.Game) return;
                var source = resource.activeColorTexture;
                if (!source.IsValid()) return;
                var desc = renderGraph.GetTextureDesc(source);
                int texW = desc.width;
                int texH = desc.height;
                if (texW < 2 || texH < 2) return;
                int n = StormGlassRig.Fill(camera.camera, texW, texH, _rects, _beads);
                if (n <= 0) return;

                desc.clearBuffer = false;
                desc.msaaSamples = MSAASamples.None;
                desc.depthBufferBits = 0;
                desc.name = "StormGlassSample";
                var sample = renderGraph.CreateTexture(desc);
                desc.name = "StormGlassShown";
                var shown = renderGraph.CreateTexture(desc);
                renderGraph.AddBlitPass(source, sample, Vector2.one, Vector2.zero,
                    filterMode: RenderGraphUtils.BlitFilterMode.ClampBilinear,
                    passName: "StormGlassSample");
                // Point copy so the garden outside the beads stays as sharp as the camera left it.
                renderGraph.AddBlitPass(source, shown, Vector2.one, Vector2.zero,
                    filterMode: RenderGraphUtils.BlitFilterMode.ClampNearest,
                    passName: "StormGlassShown");

                using (var builder = renderGraph.AddUnsafePass<BeadData>("StormGlassBeads", out var pass))
                {
                    // Sample one copy and blend the beads onto the other.
                    // The final blit reads cameraColor, so that becomes the shown copy.
                    pass.Source = sample;
                    pass.Destination = shown;
                    pass.Material = _mat;
                    pass.Count = n;
                    pass.R0 = n > 0 ? _rects[0] : default;
                    pass.R1 = n > 1 ? _rects[1] : default;
                    pass.R2 = n > 2 ? _rects[2] : default;
                    pass.R3 = n > 3 ? _rects[3] : default;
                    pass.B0 = n > 0 ? _beads[0] : default;
                    pass.B1 = n > 1 ? _beads[1] : default;
                    pass.B2 = n > 2 ? _beads[2] : default;
                    pass.B3 = n > 3 ? _beads[3] : default;
                    builder.UseTexture(sample, AccessFlags.Read);
                    builder.UseTexture(shown, AccessFlags.ReadWrite);
                    builder.AllowGlobalStateModification(true);
                    builder.AllowPassCulling(false);
                    builder.SetRenderFunc(static (BeadData data, UnsafeGraphContext ctx) => Draw(data, ctx));
                }

                resource.cameraColor = shown;
            }

            static void Draw(BeadData data, UnsafeGraphContext ctx)
            {
                if (data.Material == null || data.Count <= 0) return;
                var cmd = CommandBufferHelpers.GetNativeCommandBuffer(ctx.cmd);
                ctx.cmd.SetRenderTarget(data.Destination);
                var srcTex = (Texture)data.Source;
                if (srcTex != null && srcTex.width > 0 && srcTex.height > 0)
                {
                    var texel = new Vector4(1f / srcTex.width, 1f / srcTex.height, srcTex.width, srcTex.height);
                    cmd.SetGlobalVector(BlitTexelId, texel);
                }
                for (int i = 0; i < data.Count; i++)
                {
                    Vector4 rect = i == 0 ? data.R0 : i == 1 ? data.R1 : i == 2 ? data.R2 : data.R3;
                    Vector4 bead = i == 0 ? data.B0 : i == 1 ? data.B1 : i == 2 ? data.B2 : data.B3;
                    Place(rect, bead, out Vector4 quad, out Vector4 center, out Vector4 rad);
                    // Globals, not only the property block: a pass that never
                    // compiled last capture was drawing with every uniform at 0.
                    cmd.SetGlobalTexture(BlitTexId, data.Source);
                    cmd.SetGlobalVector(BlitScaleId, quad);
                    cmd.SetGlobalVector(BlitRtId, quad);
                    cmd.SetGlobalFloat(BlitMipId, 0f);
                    cmd.SetGlobalVector(BeadId, center);
                    cmd.SetGlobalVector(RadId, rad);
                    Block.Clear();
                    Block.SetTexture(BlitTexId, data.Source);
                    Block.SetVector(BlitScaleId, quad);
                    Block.SetVector(BlitRtId, quad);
                    Block.SetFloat(BlitMipId, 0f);
                    Block.SetVector(BeadId, center);
                    Block.SetVector(RadId, rad);
                    cmd.DrawProcedural(Matrix4x4.identity, data.Material, 0, MeshTopology.Quads, 4, 1, Block);
                }
            }

            // rect is the circle. bead.xy is the trail offset behind the slide.
            static void Place(Vector4 rect, Vector4 bead, out Vector4 quad, out Vector4 center, out Vector4 rad)
            {
                float cx = rect.z + rect.x * 0.5f;
                float cy = rect.w + rect.y * 0.5f;
                float rx = rect.x * 0.5f;
                float ry = rect.y * 0.5f;
                float x0 = rect.z;
                float y0 = rect.w;
                float x1 = rect.z + rect.x;
                float y1 = rect.w + rect.y;
                float ex = cx + bead.x;
                float ey = cy + bead.y;
                float px = rx * 0.5f;
                float py = ry * 0.5f;
                if (ex - px < x0) x0 = ex - px;
                if (ey - py < y0) y0 = ey - py;
                if (ex + px > x1) x1 = ex + px;
                if (ey + py > y1) y1 = ey + py;
                quad = new Vector4(x1 - x0, y1 - y0, x0, y0);
                center = new Vector4(cx, cy, bead.z, 0f);
                rad = new Vector4(rx, ry, bead.x, bead.y);
            }
        }
    }
}
