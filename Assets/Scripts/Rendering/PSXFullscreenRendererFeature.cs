using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

/// <summary>
/// Applies the final, screen-wide PSX treatment after the camera has rendered scene and UI.
/// </summary>
public class PSXFullscreenRendererFeature : ScriptableRendererFeature
{
    [System.Serializable]
    public class Settings
    {
        public bool enabled = true;
        public Material material;
        public RenderPassEvent renderPassEvent = RenderPassEvent.AfterRendering;
    }

    [SerializeField] private Settings settings = new Settings();

    private PSXFullscreenPass pass;

    public override void Create()
    {
        pass = new PSXFullscreenPass(settings);
        pass.renderPassEvent = settings.renderPassEvent;
    }

    public override void SetupRenderPasses(ScriptableRenderer renderer, in RenderingData renderingData)
    {
        if (settings.enabled && settings.material != null)
        {
            pass.Setup(renderer.cameraColorTargetHandle);
        }
    }

    public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
    {
        if (!settings.enabled || settings.material == null || renderingData.cameraData.cameraType != CameraType.Game)
        {
            return;
        }

        renderer.EnqueuePass(pass);
    }

    protected override void Dispose(bool disposing)
    {
        pass?.Dispose();
    }

    private sealed class PSXFullscreenPass : ScriptableRenderPass
    {
        private readonly Settings settings;
        private readonly ProfilingSampler psxProfilingSampler = new ProfilingSampler("PSX Fullscreen Effect");
        private RTHandle source;
        private RTHandle temporaryColor;

        public PSXFullscreenPass(Settings settings)
        {
            this.settings = settings;
        }

        public void Setup(RTHandle cameraColorTarget)
        {
            source = cameraColorTarget;
        }

        public override void OnCameraSetup(CommandBuffer cmd, ref RenderingData renderingData)
        {
            RenderTextureDescriptor descriptor = renderingData.cameraData.cameraTargetDescriptor;
            descriptor.depthBufferBits = 0;
            descriptor.msaaSamples = 1;

            RenderingUtils.ReAllocateIfNeeded(
                ref temporaryColor,
                descriptor,
                FilterMode.Point,
                TextureWrapMode.Clamp,
                name: "_PSXFullscreenTemporaryColor");
        }

        public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
        {
            if (source == null || temporaryColor == null || settings.material == null)
            {
                return;
            }

            CommandBuffer cmd = CommandBufferPool.Get();
            using (new ProfilingScope(cmd, psxProfilingSampler))
            {
                Blitter.BlitCameraTexture(cmd, source, temporaryColor);
                Blitter.BlitCameraTexture(cmd, temporaryColor, source, settings.material, 0);
            }

            context.ExecuteCommandBuffer(cmd);
            CommandBufferPool.Release(cmd);
        }

        public void Dispose()
        {
            temporaryColor?.Release();
        }
    }
}
