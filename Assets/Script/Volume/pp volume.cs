using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using ProfilingScope = UnityEngine.Rendering.ProfilingScope;


public class ppvolume : ScriptableRendererFeature {

	[System.Serializable]
	public class Setting {
		public string profilingName;
		public string DepthTextureName;
		public Material volumeMaterial;
		public Texture2D noiseTexture2D1;
		public Texture2D noiseTexture2D2;
		public Texture3D noiseTexture3D1;
		public Texture3D noiseTexture3D2;
	}
	public Setting setting = new Setting();
	class CustomRenderPass : ScriptableRenderPass {
		RTHandle _cameraColor;
		RTHandle _cameraDepth;
		RTHandle _cameraDepthTexture;
		RTHandle _cameraDepthTexture1;
		string ColorTextureName;
		string DepthTextureName;

		public Setting setting;
		FilteringSettings filtering;

		PPVolumeCloud ppVolumeCloud;

		public CustomRenderPass( Setting setting ) {
			this.setting = setting;
			DepthTextureName = setting.DepthTextureName;

			//get parameter from volume
			VolumeStack vs = VolumeManager.instance.stack;
			ppVolumeCloud = vs.GetComponent<PPVolumeCloud>();
		}

		public override void OnCameraSetup( CommandBuffer cmd, ref RenderingData renderingData ) {
			//获得相机深度缓冲区，存到_cameraDepth里
			_cameraDepth = renderingData.cameraData.renderer.cameraDepthTargetHandle;
			//获得相机颜色缓冲区，存到_cameraColor里
			_cameraColor = renderingData.cameraData.renderer.cameraColorTargetHandle;
			var m_Descriptor = renderingData.cameraData.cameraTargetDescriptor;
			m_Descriptor.depthBufferBits = 0;
			RenderingUtils.ReAllocateIfNeeded( ref _cameraDepthTexture, m_Descriptor, FilterMode.Bilinear, TextureWrapMode.Clamp, name:"cameraDepthTexture" );
			ConfigureTarget( _cameraColor );
		}

		public override void Execute( ScriptableRenderContext context, ref RenderingData renderingData ) {
			if( renderingData.cameraData.camera.cameraType != CameraType.Game ) return;

			CommandBuffer cmd = CommandBufferPool.Get( setting.profilingName );
			using( new ProfilingScope( cmd, new ProfilingSampler( cmd.name ) ) ) {
				if( setting.DepthTextureName != "" ) {
					//定义云的参数
					Vector3 position = Vector3.up * 10;
					Vector3 localScale = new Vector3( 50, 4, 50 );
					Vector3 boxMin = position - localScale / 2;
					Vector3 boxMax = position + localScale / 2;
					setting.volumeMaterial.SetVector( "_BoxMin", boxMin );
					setting.volumeMaterial.SetVector( "_BoxMax", boxMax );
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


					Vector2 screenSize = new Vector2( _cameraColor.rt.width, _cameraColor.rt.height );
					float width = screenSize.x;
					Blitter.BlitCameraTexture( cmd, _cameraDepth, _cameraDepthTexture );
					Blitter.BlitCameraTexture( cmd, _cameraDepthTexture, _cameraColor, setting.volumeMaterial, 0 );
				}
			}
			context.ExecuteCommandBuffer( cmd );
			cmd.Clear();
			CommandBufferPool.Release( cmd );

		}

	}

	CustomRenderPass m_ScriptablePass;
	public override void Create() {
		m_ScriptablePass = new CustomRenderPass( setting );
		m_ScriptablePass.renderPassEvent = RenderPassEvent.AfterRenderingPostProcessing;
	}
	public override void SetupRenderPasses( ScriptableRenderer renderer, in RenderingData renderingData ) {
		if( renderingData.cameraData.cameraType == CameraType.Game ) {
			//声明要使用的颜色和深度缓冲区
			m_ScriptablePass.ConfigureInput( ScriptableRenderPassInput.Depth );
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

[VolumeComponentMenuForRenderPipeline( "Custom/PPVolumeCloud", typeof( UniversalRenderPipeline ) )]
public class PPVolumeCloud : VolumeComponent, IPostProcessComponent {
	//定义shader要用的参数
	public ClampedIntParameter RaySteps = new ClampedIntParameter( 60, 10, 100, true );
	public ClampedFloatParameter RayStepLength = new ClampedFloatParameter( 0.25f, 0.01f, 1f, true );

	[Tooltip( "相机射线射入云中时候每一步的密度缩放" )]
	public FloatParameter CloudDensityScale = new FloatParameter( 1.0f, true );
	[Tooltip( "相机射线射入云中时候每一步的密度添加" )]
	public FloatParameter CloudDensityAdd = new FloatParameter( 0f, true );

	[Header( "Noise" )]
	[Tooltip( "天空噪波缩放" )]
	public ClampedFloatParameter SkymaskNoiseScale = new ClampedFloatParameter( 1f, 0, 5f, true );
	[Tooltip( "天空噪波偏移" )]
	public Vector2Parameter SkymaskNoiseBias = new Vector2Parameter( new Vector2( 0f, 0f ), true );
	[Tooltip( "密度噪波缩放" )]
	public ClampedFloatParameter DensityNoiseScale = new ClampedFloatParameter( 0.1f, 0, 1f, true );
	[Tooltip( "密度噪波偏移" )]
	public Vector2Parameter DensityNoiseBias = new Vector2Parameter( new Vector2( 0f, 0f ), true );
	[Tooltip( "密度噪波四通道混合" )]
	public Vector4Parameter DensityNoiseWeight = new Vector4Parameter( new Vector4( 0.6f, 0.2f, 0.1f, 0.1f ), true );
	[Tooltip( "细节噪波缩放" )]
	public ClampedFloatParameter DetailNoiseScale = new ClampedFloatParameter( 0.2f, 0, 1f, true );
	[Tooltip( "细节噪波权重" )]
	public ClampedFloatParameter DetailNoiseWeight = new ClampedFloatParameter( 2f, 0f, 5f, true );

	[Header( "Move" )]
	public FloatParameter MoveSpeedShape = new FloatParameter( 5f, true );
	public FloatParameter MoveSpeedDetail = new FloatParameter( 15f, true );
	public Vector4Parameter xy_MoveSpeedSky_z_MoveMask_w_ScaleMask = new Vector4Parameter( new Vector4( 1f, 0f, 2f, 1f ), true );

	[Header( "Light" )]
	[Tooltip( "每次光线向光源步进时的吸收量" )]
	public FloatParameter LightAbsorptionTowardSun = new FloatParameter( 1f, true );
	[Tooltip( "主光源能量缩放" )]
	public FloatParameter LightEnergyScale = new FloatParameter( 1f, true );
	[Header( "Custom Color" )]
	public ColorParameter CloudMidColor = new ColorParameter( Color.cyan, true );
	public ColorParameter CloudDarkColor = new ColorParameter( Color.black, true );
	public FloatParameter ColorOffset1 = new FloatParameter( 1f, true );
	public FloatParameter ColorOffset2 = new FloatParameter( 1f, true );
	public ClampedFloatParameter DarknessThreshold = new ClampedFloatParameter( 0.15f, 0f, 1f, true );
	public ClampedFloatParameter HG = new ClampedFloatParameter( 0.41f, -1f, 1f, true );


	[ExecuteAlways]
	public bool IsActive() {
		return true;
	}
	public bool IsTileCompatible() {
		return false;
	}
}