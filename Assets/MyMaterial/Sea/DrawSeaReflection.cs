using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

public class DrawSeaReflection : ScriptableRendererFeature {

	[System.Serializable]
	public class Setting {
		public string name;
		public RenderPassEvent passEvent = RenderPassEvent.AfterRenderingTransparents;
		public int bias;
		public string ColorTextureName;
		public string DepthTextureName;
	}
	public Setting setting = new Setting();
	class CustomRenderPass : ScriptableRenderPass {
		RTHandle _cameraDepth;
		RTHandle _cameraColor;
		RTHandle _cameraDepthTexture;
		RTHandle _cameraColorTexture;
		string ColorTextureName;
		string DepthTextureName;

		public Setting setting;
		FilteringSettings filtering;

		
		public CustomRenderPass( Setting setting ) {
			this.setting = setting;
			ColorTextureName = setting.ColorTextureName;
			DepthTextureName = setting.DepthTextureName;
		}

		public override void OnCameraSetup( CommandBuffer cmd, ref RenderingData renderingData ) {
			_cameraColor = renderingData.cameraData.renderer.cameraColorTargetHandle;
			_cameraDepth = renderingData.cameraData.renderer.cameraDepthTargetHandle;
			//获得相机颜色缓冲区，存到_cameraColor里
			RenderTextureDescriptor m_DescriptorCol = new RenderTextureDescriptor( Screen.width, Screen.height, RenderTextureFormat.ARGBHalf, 8 );
			RenderTextureDescriptor m_DescriptorDep = new RenderTextureDescriptor( Screen.width, Screen.height, RenderTextureFormat.ARGBHalf, 8 );
			var m_Descriptor = renderingData.cameraData.cameraTargetDescriptor;
			m_Descriptor.depthBufferBits = 0;
			m_DescriptorDep= m_Descriptor;
			m_DescriptorCol = m_Descriptor;
			RenderingUtils.ReAllocateIfNeeded( ref _cameraDepthTexture, m_DescriptorDep, FilterMode.Bilinear, TextureWrapMode.Clamp, name:"cameraDepthTexture" );
			RenderingUtils.ReAllocateIfNeeded( ref _cameraColorTexture, m_DescriptorCol, FilterMode.Bilinear, TextureWrapMode.Clamp, name:"cameraColorTexture" );
			// ConfigureTarget( _cameraColorTexture );
		}

		public override void Execute( ScriptableRenderContext context, ref RenderingData renderingData ) {
			if( renderingData.cameraData.camera.cameraType != CameraType.Game ) return;
			if( setting.ColorTextureName != "" || setting.DepthTextureName != "" ) {
				CommandBuffer cmd = CommandBufferPool.Get( setting.name );
				using( new ProfilingScope( cmd, new ProfilingSampler( cmd.name ) ) ) {
					if( setting.ColorTextureName != "" ) {
						//渲染到相机颜色缓冲区
						Blitter.BlitCameraTexture( cmd, _cameraColor, _cameraColorTexture );
						cmd.SetGlobalTexture( ColorTextureName, _cameraColorTexture );
					}
					if( setting.DepthTextureName != "" ) {
						Blitter.BlitCameraTexture( cmd, _cameraDepth, _cameraDepthTexture );
						cmd.SetGlobalTexture( DepthTextureName, _cameraDepthTexture );
					}
				}
				context.ExecuteCommandBuffer( cmd );
				cmd.Clear();
				CommandBufferPool.Release( cmd );
			}
		}

		//清除任何分配的临时RT,reallocate的不要清除
		// public override void OnCameraCleanup( CommandBuffer cmd ) {
		// }
	}

	CustomRenderPass m_ScriptablePass;

	public override void Create() {
		m_ScriptablePass = new CustomRenderPass( setting );
		m_ScriptablePass.renderPassEvent = setting.passEvent + setting.bias;
	}
	public override void SetupRenderPasses( ScriptableRenderer renderer, in RenderingData renderingData ) {
		if( renderingData.cameraData.cameraType == CameraType.Game ) {
			//声明要使用的颜色和深度缓冲区
			if( setting.ColorTextureName != "" ) {
				m_ScriptablePass.ConfigureInput( ScriptableRenderPassInput.Color );
			}
			if( setting.DepthTextureName != "" ) {
				m_ScriptablePass.ConfigureInput( ScriptableRenderPassInput.Depth );
			}
		}
	}
	public override void AddRenderPasses( ScriptableRenderer renderer, ref RenderingData renderingData ) {
		if( renderingData.cameraData.cameraType == CameraType.Game ) {
			renderer.EnqueuePass( m_ScriptablePass );
			
		}
	}
}