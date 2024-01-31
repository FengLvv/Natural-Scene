using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Experimental.Rendering;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.XR;
/*
public class CustomRenderFeature : ScriptableRendererFeature {
	/// <summary>
	/// 这里是自定义的渲染pass
	/// </summary>
	class CustomRenderPass : ScriptableRenderPass {
		private Material _material;
		float _intensity ;
		//创建RTHandle,用来存储相机的颜色和深度缓冲区
		RTHandle _cameraColor;
		RTHandle _tempTex;

		//纹理描述器
		RenderTextureDescriptor m_Descriptor;

		//初始类的时候传入材质
		public CustomRenderPass( Material material ) {
			_material = material;
			renderPassEvent = RenderPassEvent.BeforeRenderingTransparents;
			//从Volume里面把参数拿到
			VolumeStack vs = VolumeManager.instance.stack;
			CustomRP paramaters = vs.GetComponent<CustomRP>();
			_intensity = paramaters.intensity.value;
		}

		public void SetTarget(RTHandle rtHandle){
			_cameraColor = rtHandle;
		}
		//在执行pass前执行，用来构造渲染目标和清除状态
		//同样用来创建临时RT
		//如果为空，则会渲染到激活的RT上
		public override void OnCameraSetup( CommandBuffer cmd, ref RenderingData renderingData ) {
			//获取屏幕纹理的描述器
			m_Descriptor = renderingData.cameraData.cameraTargetDescriptor;

			//_tempTex = RTHandles.Alloc( in m_Descriptor, FilterMode.Bilinear, TextureWrapMode.Clamp, name:"_TempTex" );
			//新建纹理
			RenderingUtils.ReAllocateIfNeeded( ref _tempTex, m_Descriptor, FilterMode.Bilinear, TextureWrapMode.Clamp, name:"_TempTex" );
		}

		//每帧会调用一次，应用Pass
		public override void Execute( ScriptableRenderContext context, ref RenderingData renderingData ) {
			//如果不是Game视图，就不执行
			if( renderingData.cameraData.camera.cameraType != CameraType.Game ) return;
			//如果没有材质，就不执行
			if( _material == null ) return;

			//新建一个CommandBuffer
			//CommandBufferPool.Get()会从一个池子里获取CommandBuffer，如果池子里没有可用的CommandBuffer，就会新建一个
			CommandBuffer cmd = CommandBufferPool.Get( name:"myPass" );

			//创建一个frame debugger的作用域
			using( new ProfilingScope( cmd, new ProfilingSampler( cmd.name ) ) ) {
				_material.SetFloat( "_Intensity", _intensity );
				Blitter.BlitCameraTexture(cmd,_cameraColor,_tempTex);
				cmd.SetGlobalTexture( "_ScreenCopyTexture", _cameraColor );
				//执行CommandBuffer
				context.ExecuteCommandBuffer( cmd );
				cmd.Clear();
				//释放CommandBuffer
				CommandBufferPool.Release( cmd );
			}
		}
		//清除任何分配的临时RT
		public override void OnCameraCleanup( CommandBuffer cmd ) {
		//	_tempTex?.Release();
		}
		public override void FrameCleanup(CommandBuffer cmd){}

		public void Dispose(){
			_tempTex?.Release();
		}
	}

	/************************************************************************#1#
	//新建一个CustomRenderPass
	CustomRenderPass _myPass;
	[SerializeField]
	private Shader shader;
	private Material material;

	//当RendererFeature被创建、激活、改变参数时调用
	public override void Create() {
		//新建一个材质
		material = CoreUtils.CreateEngineMaterial( shader );
		//初始化CustomRenderPass
		_myPass = new CustomRenderPass( material );
	}

	//每帧对每个相机调用一次，用来注入ScriptableRenderPass
	public override void AddRenderPasses( ScriptableRenderer renderer,
		ref RenderingData renderingData ) {
		//注入CustomRenderPass，这样每帧就会调用CustomRenderPass的Execute()方法
		renderer.EnqueuePass( _myPass );
	}

	public override void SetupRenderPasses( ScriptableRenderer renderer, in RenderingData renderingData ) {
		if( renderingData.cameraData.cameraType == CameraType.Game ) {
			_myPass.SetTarget(renderer.cameraColorTargetHandle);
			//声明要使用的颜色和深度缓冲区
			_myPass.ConfigureInput( ScriptableRenderPassInput.Color );
		}
	}
	override protected void Dispose( bool disposing ) {
		_myPass.Dispose();
		CoreUtils.Destroy( material );
	}

}
}*/
