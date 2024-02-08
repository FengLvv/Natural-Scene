using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using ProfilingScope = UnityEngine.Rendering.ProfilingScope;


public class ppvolume : ScriptableRendererFeature {

	[System.Serializable]
	public class Setting {
		public string profilingName;
		public Material volumeMaterial;
		public Texture2D noiseTexture2D1;
		public Texture2D noiseTexture2D2;
		public Texture3D noiseTexture3D1;
		public Texture3D noiseTexture3D2;
	}
	public Setting setting = new Setting();
	class CustomRenderPass : ScriptableRenderPass {
		RTHandle _cameraColor;
		RTHandle _cameraDepthTexture;
		string ColorTextureName;


		public Setting setting;
		FilteringSettings filtering;

		PPVolumeCloud ppVolumeCloud;

		public CustomRenderPass( Setting setting ) {
			this.setting = setting;
			
			//get parameter from volume
			VolumeStack vs = VolumeManager.instance.stack;
			ppVolumeCloud = vs.GetComponent<PPVolumeCloud>();
		}

		public override void OnCameraSetup( CommandBuffer cmd, ref RenderingData renderingData ) {
			//获得相机颜色缓冲区，存到_cameraColor里
			_cameraColor = renderingData.cameraData.renderer.cameraColorTargetHandle;
			var m_Descriptor = renderingData.cameraData.cameraTargetDescriptor;
			m_Descriptor.depthBufferBits = 0;
			RenderingUtils.ReAllocateIfNeeded( ref _cameraDepthTexture, m_Descriptor, FilterMode.Bilinear, TextureWrapMode.Clamp, name:"cameraDepthTexture" );
		}

		public override void Execute( ScriptableRenderContext context, ref RenderingData renderingData ) {
			if( renderingData.cameraData.camera.cameraType != CameraType.Game ) return;

			CommandBuffer cmd = CommandBufferPool.Get( setting.profilingName );
			using( new ProfilingScope( cmd, new ProfilingSampler( cmd.name ) ) ) {
					//定义云的参数
					setting.volumeMaterial.SetVector( "_BoxMin", ppVolumeCloud.BoxMin.value );
					setting.volumeMaterial.SetVector( "_BoxMax", ppVolumeCloud.BoxMax.value );
					setting.volumeMaterial.SetInt( "_RaySteps", ppVolumeCloud.RaySteps.value );
					setting.volumeMaterial.SetFloat( "_RayStepLength", ppVolumeCloud.RayStepLength.value );
					setting.volumeMaterial.SetTexture( "_NoiseTexture2D1", setting.noiseTexture2D1 );
					setting.volumeMaterial.SetTexture( "_NoiseTexture2D2", setting.noiseTexture2D2 );
					setting.volumeMaterial.SetTexture( "_NoiseTexture3D1", setting.noiseTexture3D1 );
					setting.volumeMaterial.SetTexture( "_NoiseTexture3D2", setting.noiseTexture3D2 );
					setting.volumeMaterial.SetFloat( "_SkymaskNoiseScale", ppVolumeCloud.SkymaskNoiseScale.value );
					setting.volumeMaterial.SetFloat( "_LightAbsorptionTowardSun", ppVolumeCloud.LightAbsorptionTowardSun.value );
					setting.volumeMaterial.SetColor( "_CloudMidColor", ppVolumeCloud.CloudMidColor.value );
					setting.volumeMaterial.SetColor( "_CloudDarkColor", ppVolumeCloud.CloudDarkColor.value );
					setting.volumeMaterial.SetFloat( "_ColorOffset1", ppVolumeCloud.ColorOffset1.value );
					setting.volumeMaterial.SetFloat( "_ColorOffset2", ppVolumeCloud.ColorOffset2.value );
					setting.volumeMaterial.SetFloat( "_DarknessThreshold", ppVolumeCloud.DarknessThreshold.value );
					setting.volumeMaterial.SetFloat( "_CloudDensityScale", ppVolumeCloud.CloudDensityScale.value );
					setting.volumeMaterial.SetFloat( "_CloudDensityAdd", ppVolumeCloud.CloudDensityAdd.value );
					setting.volumeMaterial.SetVector( "_SkymaskNoiseBias", ppVolumeCloud.SkymaskNoiseBias.value );
					setting.volumeMaterial.SetFloat( "_LightEnergyScale", ppVolumeCloud.LightEnergyScale.value );
					setting.volumeMaterial.SetFloat( "_DensityNoiseScale", ppVolumeCloud.DensityNoiseScale.value );
					setting.volumeMaterial.SetVector( "_DensityNoiseBias", ppVolumeCloud.DensityNoiseBias.value );
					setting.volumeMaterial.SetFloat( "_MoveSpeedShape", ppVolumeCloud.MoveSpeedShape.value );
					setting.volumeMaterial.SetFloat( "_MoveSpeedDetail", ppVolumeCloud.MoveSpeedDetail.value );
					setting.volumeMaterial.SetVector( "_DensityNoiseWeight", ppVolumeCloud.DensityNoiseWeight.value );
					setting.volumeMaterial.SetFloat( "_HG", ppVolumeCloud.HG.value );
					setting.volumeMaterial.SetFloat( "_DetailNoiseScale", ppVolumeCloud.DetailNoiseScale.value );
					setting.volumeMaterial.SetFloat( "_DetailNoiseWeight", ppVolumeCloud.DetailNoiseWeight.value );
					setting.volumeMaterial.SetVector( "_MoveSpeedSky", ppVolumeCloud.xy_MoveSpeedSky_z_MoveMask_w_ScaleMask.value );
	
					Blitter.BlitCameraTexture( cmd, _cameraDepthTexture, _cameraColor, setting.volumeMaterial, 0 );
			}
			context.ExecuteCommandBuffer( cmd );
			cmd.Clear();
			CommandBufferPool.Release( cmd );

		}

	}

	CustomRenderPass m_ScriptablePass;
	public override void Create() {
		m_ScriptablePass = new CustomRenderPass( setting );
		m_ScriptablePass.renderPassEvent = RenderPassEvent.BeforeRenderingTransparents;
	}
	public override void SetupRenderPasses( ScriptableRenderer renderer, in RenderingData renderingData ) {
		if( renderingData.cameraData.cameraType == CameraType.Game ) {
			//声明要使用的颜色和深度缓冲区
			m_ScriptablePass.ConfigureInput( ScriptableRenderPassInput.Color );
		}
	}
	public override void AddRenderPasses( ScriptableRenderer renderer, ref RenderingData renderingData ) {
		if( setting.volumeMaterial == null ) {
			Debug.Log( "require cloud material" );
			return;
		}
		if( renderingData.cameraData.cameraType == CameraType.Game ) {
			renderer.EnqueuePass( m_ScriptablePass );
		}
	}
}
