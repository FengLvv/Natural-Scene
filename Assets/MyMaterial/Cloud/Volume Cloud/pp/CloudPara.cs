
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
[VolumeComponentMenuForRenderPipeline( "Custom/PPVolumeCloud", typeof( UniversalRenderPipeline ) )]
public class PPVolumeCloud : VolumeComponent, IPostProcessComponent {
	//定义shader要用的参数
	public ClampedIntParameter RaySteps = new ClampedIntParameter( 60, 10, 100, true );
	public ClampedFloatParameter RayStepLength = new ClampedFloatParameter( 0.25f, 0.01f, 50f, true );

	
	
	
	
	[Tooltip( "相机射线射入云中时候每一步的密度缩放" )]
	public FloatParameter CloudDensityScale = new FloatParameter( 1.0f, true );
	[Tooltip( "相机射线射入云中时候每一步的密度添加" )]
	public FloatParameter CloudDensityAdd = new FloatParameter( 0f, true );

	[Header( "Noise" )]
	[Tooltip("Box Position")]
	public Vector3Parameter BoxMin = new Vector3Parameter( new Vector3( 0f, 0f, 0f ), true );
	public Vector3Parameter BoxMax = new Vector3Parameter( new Vector3( 1f, 1f, 1f ), true );
	
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
	public ColorParameter CloudMidColor = new ColorParameter( Color.cyan, true,true,true );
	public ColorParameter CloudDarkColor = new ColorParameter( Color.black, true,true,true );
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