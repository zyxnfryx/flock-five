using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.RenderGraphModule;
using UnityEngine.Rendering.RenderGraphModule.Util;
using UnityEngine.Rendering.Universal;

namespace FlockFive
{
    // Copies the garden color, then draws each bead as a lens on top.
    // Installed for the play session only. The renderer asset stays as shipped.
    // Draw lists are fixed pools. A frame does not allocate a new one.
    public sealed class StormGlassFeature : ScriptableRendererFeature
    {
        static StormGlassFeature _feature;
        static bool _added;

        Material _mat;
        Pass _pass;
        readonly Vector4[] _rects = new Vector4[StormGlass.DrawCap];
        readonly Vector4[] _beads = new Vector4[StormGlass.DrawCap];

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
            readonly Vector4[] _snapR = new Vector4[StormGlass.DrawCap];
            readonly Vector4[] _snapB = new Vector4[StormGlass.DrawCap];

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
                public Vector4[] Rects;
                public Vector4[] Beads;
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
                if (n > StormGlass.DrawCap) n = StormGlass.DrawCap;
                for (int i = 0; i < n; i++)
                {
                    _snapR[i] = _rects[i];
                    _snapB[i] = _beads[i];
                }

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
                    pass.Rects = _snapR;
                    pass.Beads = _snapB;
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
                if (data.Rects == null || data.Beads == null) return;
                var cmd = CommandBufferHelpers.GetNativeCommandBuffer(ctx.cmd);
                ctx.cmd.SetRenderTarget(data.Destination);
                var srcTex = (Texture)data.Source;
                if (srcTex != null && srcTex.width > 0 && srcTex.height > 0)
                {
                    var texel = new Vector4(1f / srcTex.width, 1f / srcTex.height, srcTex.width, srcTex.height);
                    cmd.SetGlobalVector(BlitTexelId, texel);
                }
                int n = data.Count;
                if (n > data.Rects.Length) n = data.Rects.Length;
                if (n > data.Beads.Length) n = data.Beads.Length;
                for (int i = 0; i < n; i++)
                {
                    Place(data.Rects[i], data.Beads[i], out Vector4 quad, out Vector4 center, out Vector4 rad);
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
            // bead.z is alpha. bead.w is splat (1 at the impact, 0 once gathered).
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
                float px = rx * 0.45f;
                float py = ry * 0.45f;
                if (ex - px < x0) x0 = ex - px;
                if (ey - py < y0) y0 = ey - py;
                if (ex + px > x1) x1 = ex + px;
                if (ey + py > y1) y1 = ey + py;
                quad = new Vector4(x1 - x0, y1 - y0, x0, y0);
                center = new Vector4(cx, cy, bead.z, bead.w);
                rad = new Vector4(rx, ry, bead.x, bead.y);
            }
        }
    }
}
