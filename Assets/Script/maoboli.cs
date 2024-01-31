using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

public class maoboli : ScriptableRendererFeature {
	//在ScriptableRendererFeature类里写一个class Settings，并实例化它
	[System.Serializable]
	public class Settings {
		public RenderPassEvent renderEvent = RenderPassEvent.BeforeRenderingTransparents;
		public Material blurMat;
		public Material attachPic;
		public Texture2D pic;
	}

	public Settings settings = new Settings();
	//----------------------------------------------------------------
	public class FrostedGlassRenderPass : ScriptableRenderPass {

		CommandBuffer cmd;
		string m_ProfilerTag; //cmd name
		Material m_blurMat;
		RTHandle source;
		RTHandle rth_tempTex;
		RTHandle rth_Blur01;
		RTHandle rth_Blur02;
		Material _attachMat;
		Texture2D pic;
		//首先写这个ScriptableRenderPass的构造函数
		public FrostedGlassRenderPass( Settings param ) {
			m_ProfilerTag = "FrostedGalss";
			this.renderPassEvent = param.renderEvent;
			m_blurMat = param.blurMat;
			_attachMat = param.attachPic;
			pic = param.pic;
		}
		//----------------------------------------------------------------
		public override void OnCameraSetup( CommandBuffer cmd, ref RenderingData renderingData ) {
			var renderer = renderingData.cameraData.renderer;
			this.source = renderer.cameraColorTargetHandle;
			RenderTextureDescriptor opaqueDesc = renderingData.cameraData.cameraTargetDescriptor;
			//RenderTextureDescriptor opaqueDesc = new RenderTextureDescriptor( Screen.width, Screen.height, RenderTextureFormat.Default, 0 );
			opaqueDesc.depthBufferBits = 0; //为什么加这句？// Color and depth cannot be combined in RTHandles
			/*下面这个错误的……就当是记录下黑历史……
			// int id_tempTex = Shader.PropertyToID("_TempTex");
			// cmd.GetTemporaryRT(id_tempTex, opaqueDesc, FilterMode.Bilinear);
			// rti_tempTex = new RenderTargetIdentifier(id_tempTex);
			// rth_tempTex = RTHandles.Alloc(rti_tempTex);
			*/
			RenderingUtils.ReAllocateIfNeeded( ref rth_tempTex, opaqueDesc, FilterMode.Bilinear, TextureWrapMode.Clamp, name:"_TempTex" );
			RenderingUtils.ReAllocateIfNeeded( ref rth_Blur01, opaqueDesc, FilterMode.Bilinear, TextureWrapMode.Clamp, name:"_Blur01" );
			RenderingUtils.ReAllocateIfNeeded( ref rth_Blur02, opaqueDesc, FilterMode.Bilinear, TextureWrapMode.Clamp, name:"_Blur02" );
			// rth_tempTex = RTHandles.Alloc(in opaqueDesc,FilterMode.Bilinear,TextureWrapMode.Clamp,name: "_TempTex");
			// 上面这个也可以正确运行，但是记得要在OnCameraCleanup()里执行Release
			_attachMat.SetTexture( "_Pic", pic );
		}
		//----------------------------------------------------------------
		// Here you can implement the rendering logic. 在Execute方法里执行CommandBuffer
		public override void Execute( ScriptableRenderContext context, ref RenderingData renderingData ) {
			CommandBuffer cmd = CommandBufferPool.Get();
			using( new ProfilingScope( cmd, new ProfilingSampler( m_ProfilerTag ) ) ) {
				Vector2[] sizes ={
					new Vector2( Screen.width, Screen.height ),
					new Vector2( Screen.width / 2, Screen.height / 2 ),
					new Vector2( Screen.width / 4, Screen.height / 4 ),
					new Vector2( Screen.width / 8, Screen.height / 8 ),
				};
				int numIterations = 2; //3
				Blitter.BlitCameraTexture( cmd, source, rth_tempTex ); //表格里的类型|C|
				// Blitter.BlitCameraTexture( cmd, source, rth_tempTex,_attachMat,0 ); //表格里的类型|C|
				for( int i = 0; i < numIterations; ++i ) {
					Blitter.BlitCameraTexture( cmd, rth_tempTex, rth_Blur01 ); //表格里的类型|C|
					//
					cmd.SetGlobalVector( "_Offset", new Vector4( 2.0f / sizes[i].x, 0, 0, 0 ) );
					Blitter.BlitCameraTexture( cmd, rth_Blur01, rth_Blur02, m_blurMat, 0 ); //表格里的类型|A|

					cmd.SetGlobalVector( "_Offset", new Vector4( 0, 2.0f / sizes[i].y, 0, 0 ) );
					Blitter.BlitCameraTexture( cmd, rth_Blur02, rth_Blur01, m_blurMat, 0 ); //表格里的类型|A|

					Blitter.BlitCameraTexture( cmd, rth_Blur01, rth_tempTex ); //表格里的类型|C|
				}
				// Blitter.BlitCameraTexture( cmd, rth_Blur01, source ); //表格里的类型|C|
				// 把最终内容Blit到一个RenderTexture上。
				cmd.SetGlobalTexture( "_GrabBlurTexture", rth_Blur01 );
			}
			//---------------------------------------------
			context.ExecuteCommandBuffer( cmd ); // Use ScriptableRenderContext to issue drawing commands or execute command buffers
			cmd.Clear();
			CommandBufferPool.Release( cmd );
		}
		//----------------------------------------------------------------
		// Cleanup any allocated resources that were created during the execution of this render pass.
		public override void OnCameraCleanup( CommandBuffer cmd ) { }
		public override void FrameCleanup( CommandBuffer cmd ) { }
		public void Dispose() {
			rth_tempTex?.Release();
			rth_Blur01?.Release();
			rth_Blur02?.Release();
		}

	}
//----------------------------------------------------------------
	maoboli.FrostedGlassRenderPass m_ScriptablePass;
	public override void Create() { //在Create方法里实例化ScriptableRenderPass类
		m_ScriptablePass = new maoboli.FrostedGlassRenderPass( settings ); //实例化包含了renderPassEvent的设置
	}
	public override void AddRenderPasses( ScriptableRenderer renderer, ref RenderingData renderingData ) {
		//m_ScriptablePass.Setup(renderer.cameraColorTargetHandle); 
		//上面这行放在AddRenderPasses()方法里会报错：
		//可以放到RenderPass的OnCameraSetup()里；也可以放到RendererFeature的SetupRenderPasses()里；
		m_ScriptablePass.ConfigureInput( ScriptableRenderPassInput.Color );
		if( renderingData.cameraData.cameraType == CameraType.Game ) {
			renderer.EnqueuePass( m_ScriptablePass );
		}
	}
}



/*
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

public class maoboli : ScriptableRendererFeature {
	//在ScriptableRendererFeature类里写一个class Settings，并实例化它
	[System.Serializable]
	public class Settings {
		public RenderPassEvent renderEvent = RenderPassEvent.BeforeRenderingTransparents;
		public Material blurMat;
	}

	public Settings settings = new Settings();
	//----------------------------------------------------------------
	public class FrostedGlassRenderPass : ScriptableRenderPass {

		CommandBuffer cmd;
		string m_ProfilerTag; //cmd name
		Material m_blurMat;
		RTHandle source;
		RTHandle rth_tempTex;
		RTHandle rth_Blur01;
		RTHandle rth_Blur02;
		//首先写这个ScriptableRenderPass的构造函数
		public FrostedGlassRenderPass( Settings param ) {
			m_ProfilerTag = "FrostedGalss";
			this.renderPassEvent = param.renderEvent;
			m_blurMat = param.blurMat;
		}
		//----------------------------------------------------------------
		// This method is called before executing the render pass.
		// It can be used to configure render targets and their clear state. Also to create temporary render target textures.
		// When empty this render pass will render to the active camera render target.
		public override void OnCameraSetup( CommandBuffer cmd, ref RenderingData renderingData ) {
			var renderer = renderingData.cameraData.renderer;
			this.source = renderer.cameraColorTargetHandle;
			RenderTextureDescriptor opaqueDesc = renderingData.cameraData.cameraTargetDescriptor;
			opaqueDesc.depthBufferBits = 0; //为什么加这句？// Color and depth cannot be combined in RTHandles
			/*下面这个错误的……就当是记录下黑历史……
			// int id_tempTex = Shader.PropertyToID("_TempTex");
			// cmd.GetTemporaryRT(id_tempTex, opaqueDesc, FilterMode.Bilinear);
			// rti_tempTex = new RenderTargetIdentifier(id_tempTex);
			// rth_tempTex = RTHandles.Alloc(rti_tempTex);
			#1#
			RenderingUtils.ReAllocateIfNeeded( ref rth_tempTex, opaqueDesc, FilterMode.Bilinear, TextureWrapMode.Clamp, name:"_TempTex" );
			RenderingUtils.ReAllocateIfNeeded( ref rth_Blur01, opaqueDesc, FilterMode.Bilinear, TextureWrapMode.Clamp, name:"_Blur01" );
			RenderingUtils.ReAllocateIfNeeded( ref rth_Blur02, opaqueDesc, FilterMode.Bilinear, TextureWrapMode.Clamp, name:"_Blur02" );
			// rth_tempTex = RTHandles.Alloc(in opaqueDesc,FilterMode.Bilinear,TextureWrapMode.Clamp,name: "_TempTex");
			// 上面这个也可以正确运行，但是记得要在OnCameraCleanup()里执行Release
		}
		//----------------------------------------------------------------
		// Here you can implement the rendering logic. 在Execute方法里执行CommandBuffer
		public override void Execute( ScriptableRenderContext context, ref RenderingData renderingData ) {
			CommandBuffer cmd = CommandBufferPool.Get();
			using( new ProfilingScope( cmd, new ProfilingSampler( m_ProfilerTag ) ) ) {
				Vector2[] sizes ={
					new Vector2( Screen.width, Screen.height ),
					new Vector2( Screen.width / 2, Screen.height / 2 ),
					new Vector2( Screen.width / 4, Screen.height / 4 ),
					new Vector2( Screen.width / 8, Screen.height / 8 ),
				};
				int numIterations = 2; //3
				Blitter.BlitCameraTexture( cmd, source, rth_tempTex ); //表格里的类型|C|
				for( int i = 0; i < numIterations; ++i ) {
					Blitter.BlitCameraTexture( cmd, rth_tempTex, rth_Blur01 ); //表格里的类型|C|

					cmd.SetGlobalVector( "_Offset", new Vector4( 2.0f / sizes[i].x, 0, 0, 0 ) );
					Blitter.BlitCameraTexture( cmd, rth_Blur01, rth_Blur02, m_blurMat, 0 ); //表格里的类型|A|

					cmd.SetGlobalVector( "_Offset", new Vector4( 0, 2.0f / sizes[i].y, 0, 0 ) );
					Blitter.BlitCameraTexture( cmd, rth_Blur02, rth_Blur01, m_blurMat, 0 ); //表格里的类型|A|

					Blitter.BlitCameraTexture( cmd, rth_Blur01, rth_tempTex ); //表格里的类型|C|
				}
				//把最终内容Blit到一个RenderTexture上。
				cmd.SetGlobalTexture( "_GrabBlurTexture", (RenderTargetIdentifier)rth_Blur01 );
			}
			//---------------------------------------------
			context.ExecuteCommandBuffer( cmd ); // Use ScriptableRenderContext to issue drawing commands or execute command buffers
			cmd.Clear();
			CommandBufferPool.Release( cmd );
		}
		//----------------------------------------------------------------
		// Cleanup any allocated resources that were created during the execution of this render pass.
		public override void OnCameraCleanup( CommandBuffer cmd ) { }
		public override void FrameCleanup( CommandBuffer cmd ) { }
		public void Dispose() {
			rth_tempTex?.Release();
			rth_Blur01?.Release();
			rth_Blur02?.Release();
		}

	}
//----------------------------------------------------------------
	maoboli.FrostedGlassRenderPass m_ScriptablePass;
	public override void Create() { //在Create方法里实例化ScriptableRenderPass类
		m_ScriptablePass = new maoboli.FrostedGlassRenderPass( settings ); //实例化包含了renderPassEvent的设置
	}
	public override void AddRenderPasses( ScriptableRenderer renderer, ref RenderingData renderingData ) {
		//m_ScriptablePass.Setup(renderer.cameraColorTargetHandle);
		//上面这行放在AddRenderPasses()方法里会报错：
		//可以放到RenderPass的OnCameraSetup()里；也可以放到RendererFeature的SetupRenderPasses()里；
		m_ScriptablePass.ConfigureInput( ScriptableRenderPassInput.Color );
		if( renderingData.cameraData.cameraType == CameraType.Game ) {
			renderer.EnqueuePass( m_ScriptablePass );
		}
	}
}
*/