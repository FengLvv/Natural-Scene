using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.Serialization;

public class DrawCaustic : ScriptableRendererFeature {
	//创建一个setting，用来从外部输入材质和参数
	[System.Serializable]
	public class Settings {
		public Material CausticMaterial;
	}
	public Settings settings = new Settings();

	/// <summary>
	/// 这里是自定义的渲染pass
	/// </summary>
	class CustomRenderPass : ScriptableRenderPass {
		FilteringSettings filtering; //在之后设置
		public ShaderTagId shaderTag = new ShaderTagId( "UniversalForward" ); //渲染的tag
		Settings _settings;
		//纹理描述器
		RenderTextureDescriptor m_Descriptor;
		//cmd name


		//初始类的时候传入材质
		public CustomRenderPass( Settings settings ) {
			//传入材质和参数（来自render feature）
			renderPassEvent = RenderPassEvent.AfterRenderingSkybox + 1;
			_settings = settings;
			RenderQueueRange queue = RenderQueueRange.all; //设置渲染队列
			int layerMask = LayerMask.GetMask( "Water" ); //设置filter的layermask
			filtering = new FilteringSettings( queue, layerMask ); //设置filter
		}

		//在执行pass前执行，用来构造渲染目标和清除状态
		//同样用来创建临时RT
		//如果为空，则会渲染到激活的RT上
		public override void OnCameraSetup( CommandBuffer cmd, ref RenderingData renderingData ) {
			//获取屏幕纹理的描述器 
			//m_Descriptor = new RenderTextureDescriptor( Screen.width, Screen.height, RenderTextureFormat.Default, 0 );
			m_Descriptor = renderingData.cameraData.cameraTargetDescriptor;
			m_Descriptor.depthBufferBits = 0; //不需要深度缓冲区
		}

		//每帧会调用一次，应用Pass
		public override void Execute( ScriptableRenderContext context, ref RenderingData renderingData ) {
			//如果不是Game视图，就不执行
			if( renderingData.cameraData.camera.cameraType != CameraType.Game ) return;
			//如果没有材质，就不执行
			if( _settings.CausticMaterial == null ) return;

			//SortingCriteria sortingCriteria = SortingCriteria.RenderQueue;  //也可以自定义渲染顺序
			var draw = CreateDrawingSettings( shaderTag, ref renderingData, renderingData.cameraData.defaultOpaqueSortFlags ); //传入执行的tag，上下文，渲染顺序
			draw.overrideMaterial = _settings.CausticMaterial;
			draw.overrideMaterialPassIndex = 0; //材质覆盖
			context.DrawRenderers( renderingData.cullResults, ref draw, ref filtering ); //绘制，传入filter
		}

		//清除任何分配的临时RT
		public override void OnCameraCleanup( CommandBuffer cmd ) {
			//_tempTex?.Release(); //如果用的RenderingUtils.ReAllocateIfNeeded创建，就不要清除，否则会出bug（纹理传入不了材质）
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
		if( settings.CausticMaterial == null ) {
			Debug.LogError( "CausticMaterial is null" );
			return;
		}
	}

	//对每个相机调用一次，用来注入ScriptableRenderPass 
	public override void AddRenderPasses( ScriptableRenderer renderer,
		ref RenderingData renderingData ) {
		if( settings.CausticMaterial == null ) return;
		if( renderingData.cameraData.cameraType == CameraType.Game ) {
			//注入CustomRenderPass，这样每帧就会调用CustomRenderPass的Execute()方法
			renderer.EnqueuePass( _myPass );
		}
	}
}