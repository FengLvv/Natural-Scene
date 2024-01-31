using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

public class official : ScriptableRendererFeature
{
    class ColorBlitPass : ScriptableRenderPass
    {
        ProfilingSampler m_ProfilingSampler = new ProfilingSampler("ColorBlit");
        Material m_Material;
        Material m_Material1;
        RTHandle m_CameraColorTarget;
        float m_Intensity;
        RTHandle temp;

        public ColorBlitPass(Material material, Material material1)
        {
            m_Material = material;
            m_Material1=material1;
            renderPassEvent = RenderPassEvent.BeforeRenderingPostProcessing;
        }

        public void SetTarget(RTHandle colorHandle, float intensity)
        {
            m_CameraColorTarget = colorHandle;
            m_Intensity = intensity;
        }

        public override void OnCameraSetup(CommandBuffer cmd, ref RenderingData renderingData)
        {
            RenderTextureDescriptor descriptor = new RenderTextureDescriptor( 1920, 1080, RenderTextureFormat.Default, 0 );
            temp = RTHandles.Alloc(descriptor);
            ConfigureTarget(m_CameraColorTarget);
        }

        public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
        {
            var cameraData = renderingData.cameraData;
            if (cameraData.camera.cameraType != CameraType.Game)
                return;

            if (m_Material == null)
                return;

            CommandBuffer cmd = CommandBufferPool.Get();
            using (new ProfilingScope(cmd, m_ProfilingSampler))
            {
       
                m_Material.SetFloat("_Intensity", m_Intensity);
               //Blitter.BlitCameraTexture(cmd, m_CameraColorTarget, m_CameraColorTarget, m_Material, 0);
              Blitter.BlitCameraTexture(cmd, m_CameraColorTarget, temp,m_Material1,0);
              // Blitter.BlitCameraTexture(cmd, temp, m_CameraColorTarget);
            }
            context.ExecuteCommandBuffer(cmd);
            cmd.Clear();

            CommandBufferPool.Release(cmd);
        }
    }

    public Shader m_Shader;
    public Shader m_Shader1;
    public float m_Intensity;

    Material m_Material;
    Material m_Material1;

    ColorBlitPass m_RenderPass = null;

    public override void AddRenderPasses(ScriptableRenderer renderer,
        ref RenderingData renderingData)
    {
        if (renderingData.cameraData.cameraType == CameraType.Game)
            renderer.EnqueuePass(m_RenderPass);
    }

    public override void SetupRenderPasses(ScriptableRenderer renderer,
        in RenderingData renderingData)
    {
        if (renderingData.cameraData.cameraType == CameraType.Game)
        {
            // Calling ConfigureInput with the ScriptableRenderPassInput.Color argument
            // ensures that the opaque texture is available to the Render Pass.
            m_RenderPass.ConfigureInput(ScriptableRenderPassInput.Color);
            m_RenderPass.SetTarget(renderer.cameraColorTargetHandle, m_Intensity);
        }
    }

    public override void Create()
    {
        m_Material = CoreUtils.CreateEngineMaterial(m_Shader);
        m_Material1 = CoreUtils.CreateEngineMaterial(m_Shader1);
        m_RenderPass = new ColorBlitPass(m_Material,m_Material1);
    }

    protected override void Dispose(bool disposing)
    {
        CoreUtils.Destroy(m_Material);
    }
}


