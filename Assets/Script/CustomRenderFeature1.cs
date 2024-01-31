using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
public class CustomRenderFeature1 : ScriptableRendererFeature {
	//创建一个setting，用来从外部输入材质和参数
	[System.Serializable]
	public class Settings {
		public RenderPassEvent renderEvent = RenderPassEvent.BeforeRenderingTransparents;
		public Shader shaderNeeded;
	}
	public Settings settings = new Settings();

	/// <summary>
	/// 这里是自定义的渲染pass
	/// </summary>
	class CustomRenderPass : ScriptableRenderPass {
		//设置用来后处理的材质和参数
		Material _material;
		float _intensity;
		//创建RTHandle,用来存储相机的颜色和深度缓冲区
		RTHandle _cameraColor;
		RTHandle _tempTex;
		RTHandle _tempTex2;
		//纹理描述器
		RenderTextureDescriptor m_Descriptor;
		//cmd name
		string _passName;

		//初始类的时候传入材质
		public CustomRenderPass( Settings settings ) {
			_passName = "CustomRenderPass";
			//传入材质和参数（来自render feature）
			_material = CoreUtils.CreateEngineMaterial( settings.shaderNeeded );
			renderPassEvent = settings.renderEvent;
			//传入参数（来自volume）
			VolumeStack vs = VolumeManager.instance.stack;
			CustomRP paramaters = vs.GetComponent<CustomRP>();
			_intensity = paramaters.intensity.value;
		}

		//在执行pass前执行，用来构造渲染目标和清除状态
		//同样用来创建临时RT
		//如果为空，则会渲染到激活的RT上
		public override void OnCameraSetup( CommandBuffer cmd, ref RenderingData renderingData ) {
			//获得相机颜色缓冲区，存到_cameraColor里
			_cameraColor = renderingData.cameraData.renderer.cameraColorTargetHandle;
			//获取屏幕纹理的描述器 
			//m_Descriptor = new RenderTextureDescriptor( Screen.width, Screen.height, RenderTextureFormat.Default, 0 );
			m_Descriptor = renderingData.cameraData.cameraTargetDescriptor;
			m_Descriptor.depthBufferBits = 0; //不需要深度缓冲区
			//新建纹理_tempTex
			RenderingUtils.ReAllocateIfNeeded( ref _tempTex, m_Descriptor, FilterMode.Bilinear, TextureWrapMode.Clamp, name:"_TempTex" );
			RenderingUtils.ReAllocateIfNeeded( ref _tempTex2, m_Descriptor, FilterMode.Bilinear, TextureWrapMode.Clamp, name:"_TempTex2" );
			//这个用来在blit的时候指定目标RT（如果不指定，则默认为激活的RT）
			//blit如果不指定目标RT，则为这个RT
			ConfigureTarget( _tempTex ); 
		}

		//每帧会调用一次，应用Pass
		public override void Execute( ScriptableRenderContext context, ref RenderingData renderingData ) {
			//如果不是Game视图，就不执行
			if( renderingData.cameraData.camera.cameraType != CameraType.Game ) return;
			//如果没有材质，就不执行
			if( _material == null ) return;

			//新建一个CommandBuffer
			//CommandBufferPool.Get()会从一个池子里获取CommandBuffer，如果池子里没有可用的CommandBuffer，就会新建一个
			CommandBuffer cmd = CommandBufferPool.Get( name:_passName );

			//创建一个frame debugger的作用域
			using( new ProfilingScope( cmd, new ProfilingSampler( cmd.name ) ) ) {
				Blitter.BlitCameraTexture( cmd, _cameraColor, _tempTex );
				Blitter.BlitCameraTexture( cmd, _tempTex, _tempTex2 );
				cmd.SetGlobalTexture( "_ScreenCopyTexture", _tempTex );
			}
			//执行、清空、释放 CommandBuffer
			context.ExecuteCommandBuffer( cmd );
			cmd.Clear();
			CommandBufferPool.Release( cmd );
		}
		//清除任何分配的临时RT
		public override void OnCameraCleanup( CommandBuffer cmd ) {
			_tempTex?.Release();
		}
	}

	/*************************************************************************/
	//新建一个CustomRenderPass
	CustomRenderPass _myPass;
	Material _material;

	//当RendererFeature被创建、激活、改变参数时调用
	public override void Create() {
		//初始化CustomRenderPass
		_myPass = new CustomRenderPass( settings );
	}

	public override void SetupRenderPasses( ScriptableRenderer renderer, in RenderingData renderingData ) {
		if(settings.shaderNeeded == null) return;
		if( renderingData.cameraData.cameraType == CameraType.Game ) {
			//声明要使用的颜色和深度缓冲区
			_myPass.ConfigureInput( ScriptableRenderPassInput.Color );
		}
	}
	
	//每帧对每个相机调用一次，用来注入ScriptableRenderPass 
	public override void AddRenderPasses( ScriptableRenderer renderer,
		ref RenderingData renderingData ) {
		if(settings.shaderNeeded == null) return;
		if( renderingData.cameraData.cameraType == CameraType.Game ) {
			//注入CustomRenderPass，这样每帧就会调用CustomRenderPass的Execute()方法
			renderer.EnqueuePass( _myPass );
		}
	}
}
	
[VolumeComponentMenuForRenderPipeline( "Custom/CustormRP", typeof( UniversalRenderPipeline ) )]
public class CustomRP : VolumeComponent, IPostProcessComponent {
	[Header( "BLoom Settings" )]
	//定义shader要用的参数
	public FloatParameter intensity = new FloatParameter( 0.9f, true );

	public bool IsActive() {
		return true;
	}
	public bool IsTileCompatible() {
		return false;
	}
}