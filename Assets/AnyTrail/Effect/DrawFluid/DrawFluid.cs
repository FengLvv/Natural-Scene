using AnyTrail.DataAndTools;
using System.Collections.Generic;
using UnityEngine;

namespace AnyTrail.Effect.DrawFluid {
	//新建一个_AnyTrailFluid的全局变量，用来在shader中传递RT
	public sealed class DrawFluid : CanvasComputerAbstract {
		//kernel
		int Advect_Vzw_Kernel;
		int Diffusion1_Vxy_Kernel;
		int Diffusion2_Vzw_Kernel;
		int Force_Vzw_Kernel;
		int Divergence_Pz_Kernel;
		int Presure1_Py_Kernel;
		int Presure2_Px_Kernel;
		int Gradient_Vxy_Kernel;

		//buffer和RT
		//结构体继承自接口，可以在Canvas里面修改数值后传递给ComputeShader

		public ComputeShader NSCompute;
		int WriterCount; //写入的数量
		FluidWriterStructure[] ForceStructArray;
		ComputeBuffer forcePosBuffer;
		int forceBufferStride;
		public Texture2D oriTex;
		RenderTexture veloctiyRT;
		RenderTexture presureRT;
		RenderTexture customRT;


		private FluidParam fluidParam;

		public override void SetComputeParameter( ComputeParaInterface paramater ) {
			fluidParam = (FluidParam)paramater;
			NSCompute.SetFloat( Vscosity, fluidParam.vscosity );
			NSCompute.SetFloat( DisapperaStep, fluidParam.disappearStep / 50f );
			NSCompute.SetFloat( AdvectionSpeed, fluidParam.advectionSpeed );
		}

		//RT数据
		readonly static int Veloctiy = Shader.PropertyToID( "_Veloctiy" );
		readonly static int Presure = Shader.PropertyToID( "_Presure" );
		readonly static int Custom = Shader.PropertyToID( "_CustomTex" );
		readonly static int DT = Shader.PropertyToID( "dt" );
		readonly static int Resolution = Shader.PropertyToID( "resolution" );
		readonly static int AdvectionSpeed = Shader.PropertyToID( "advectionSpeed" );
		readonly static int Vscosity = Shader.PropertyToID( "vscosity" );
		readonly static int ForcePosBuffer = Shader.PropertyToID( "forcePosBuffer" );
		readonly static int ForceCount = Shader.PropertyToID( "forceCount" );
		readonly static int AnyTrailFluid = Shader.PropertyToID( "_AnyTrailFluid" );
		readonly static int DisapperaStep = Shader.PropertyToID( "disapperaStep" );

		void CreateRT() {
			//用来写入速度场的RT
			veloctiyRT = RenderTexture.GetTemporary( rtPara.rtResolution, rtPara.rtResolution, 0, RenderTextureFormat.ARGBFloat );
			veloctiyRT.enableRandomWrite = true; //允许写入
			veloctiyRT.filterMode = FilterMode.Bilinear;
			veloctiyRT.wrapMode = TextureWrapMode.Clamp;
			veloctiyRT.useMipMap = false;
			veloctiyRT.Create();

			//用来写入压强场的RT
			presureRT = RenderTexture.GetTemporary( rtPara.rtResolution, rtPara.rtResolution, 0, RenderTextureFormat.ARGBFloat );
			presureRT.enableRandomWrite = true; //允许写入
			presureRT.filterMode = FilterMode.Bilinear;
			presureRT.wrapMode = TextureWrapMode.Clamp;
			presureRT.useMipMap = false;
			presureRT.Create();

			//用来写入图片
			customRT = RenderTexture.GetTemporary( rtPara.rtResolution, rtPara.rtResolution, 0, RenderTextureFormat.ARGBFloat );
			customRT.enableRandomWrite = true; //允许写入
			customRT.filterMode = FilterMode.Bilinear;
			customRT.wrapMode = TextureWrapMode.Clamp;
			customRT.useMipMap = false;
			customRT.Create();

			//给customTex赋值
			Graphics.Blit( oriTex, customRT );
		}
		private void InitShader() {
			Advect_Vzw_Kernel = NSCompute.FindKernel( "Advection_Vzw" );
			Diffusion1_Vxy_Kernel = NSCompute.FindKernel( "Diffusion1_Vxy" );
			Diffusion2_Vzw_Kernel = NSCompute.FindKernel( "Diffusion2_Vzw" );
			Force_Vzw_Kernel = NSCompute.FindKernel( "Force_Vzw" );
			Divergence_Pz_Kernel = NSCompute.FindKernel( "Divergence_Pz" );
			Presure1_Py_Kernel = NSCompute.FindKernel( "Presure1_Py" );
			Presure2_Px_Kernel = NSCompute.FindKernel( "Presure2_Px" );
			Gradient_Vxy_Kernel = NSCompute.FindKernel( "Gradient_Vxy" );

			//设置纹理
			NSCompute.SetTexture( Advect_Vzw_Kernel, Veloctiy, veloctiyRT );
			NSCompute.SetTexture( Advect_Vzw_Kernel, Custom, customRT );
			NSCompute.SetTexture( Diffusion1_Vxy_Kernel, Veloctiy, veloctiyRT );
			NSCompute.SetTexture( Diffusion2_Vzw_Kernel, Veloctiy, veloctiyRT );
			NSCompute.SetTexture( Force_Vzw_Kernel, Veloctiy, veloctiyRT );
			NSCompute.SetTexture( Divergence_Pz_Kernel, Veloctiy, veloctiyRT );
			NSCompute.SetTexture( Divergence_Pz_Kernel, Presure, presureRT );
			NSCompute.SetTexture( Presure1_Py_Kernel, Presure, presureRT );
			NSCompute.SetTexture( Presure2_Px_Kernel, Presure, presureRT );
			NSCompute.SetTexture( Gradient_Vxy_Kernel, Veloctiy, veloctiyRT );
			NSCompute.SetTexture( Gradient_Vxy_Kernel, Presure, presureRT );

			//设置参数
			//输入fixedDeltaTime,不然这一帧创建RT，会导致dt很大
			NSCompute.SetFloat( DT, 0.005f );
			NSCompute.SetFloat( Resolution, rtPara.rtResolution );
			NSCompute.SetFloat( Vscosity, fluidParam.vscosity );
			NSCompute.SetFloat( AdvectionSpeed, fluidParam.advectionSpeed );

			forceBufferStride = sizeof( float ) * 6;
			forcePosBuffer = new ComputeBuffer( WriterCount, forceBufferStride );
		}

		protected override void Init( int Count, RTPara RTParameter ) {
			//设置当无writer时是否停止计算
			base.Init( Count, RTParameter );
			NSCompute = Resources.Load<ComputeShader>( "NSCompute" );

			WriterCount = Count;
			//初始化结构体数组
			ForceStructArray = new FluidWriterStructure[WriterCount];

			CreateRT();
			InitShader();

			Shader.SetGlobalTexture( AnyTrailFluid, veloctiyRT );
		}

		/// <summary>
		/// 构造函数
		/// </summary>
		/// <param name="Count"></param>
		/// <param name="RTParameter"></param>
		public DrawFluid( int Count, RTPara RTParameter ) {
			//设置当无writer时是否停止计算
			StopWhenNoWriter = false;
			Init( Count, RTParameter );
		}

		public override void GetRTs( ref List<RenderTexture> rt ) {
			rt.Add( veloctiyRT );
			rt.Add( customRT );
		}

		public override void AddWriterPara<T>( T writerPara ) {
			FluidWriterStructure fluidWriterStructure = (FluidWriterStructure)(object)writerPara;
			ForceStructArray[CurrentWriterCount] = fluidWriterStructure;
			//CurrentWriterCount++
			base.AddWriterPara( writerPara );
		}

		protected override void Execute() {
			forcePosBuffer.SetData( ForceStructArray );
			NSCompute.SetBuffer( Force_Vzw_Kernel, ForcePosBuffer, forcePosBuffer );
			NSCompute.SetInt( ForceCount, CurrentWriterCount );
			SimulateNS();
			//清空结构体数组
			ForceStructArray = new FluidWriterStructure[WriterCount];
		}
		void SimulateNS() {
			//advection
			NSCompute.Dispatch( Advect_Vzw_Kernel, rtPara.rtResolution / 8, rtPara.rtResolution / 8, 1 );

			//diffusion
			if( fluidParam.vscosity > 0 ) {
				for( int i = 0; i < fluidParam.diffusionTimes; i++ ) {
					NSCompute.Dispatch( Diffusion1_Vxy_Kernel, rtPara.rtResolution / 8, rtPara.rtResolution / 8, 1 );
					NSCompute.Dispatch( Diffusion2_Vzw_Kernel, rtPara.rtResolution / 8, rtPara.rtResolution / 8, 1 );
				}
			}

			//force
			NSCompute.Dispatch( Force_Vzw_Kernel, rtPara.rtResolution / 8, rtPara.rtResolution / 8, 1 );

			//divergence:输入有散速度，输出散度
			NSCompute.Dispatch( Divergence_Pz_Kernel, rtPara.rtResolution / 8, rtPara.rtResolution / 8, 1 );

			//presure:输入速度的散度，输出压力
			for( int i = 0; i < fluidParam.pressureIterationTimes; i++ ) {
				NSCompute.Dispatch( Presure1_Py_Kernel, rtPara.rtResolution / 8, rtPara.rtResolution / 8, 1 );
				NSCompute.Dispatch( Presure2_Px_Kernel, rtPara.rtResolution / 8, rtPara.rtResolution / 8, 1 );
			}

			// gradient:输入压力，输出速度
			NSCompute.Dispatch( Gradient_Vxy_Kernel, rtPara.rtResolution / 8, rtPara.rtResolution / 8, 1 );
			//这时候速度场已经更新完毕，veloctiyRT.xyz就是最新的速度场

		}

		public override void OnDestroy() {
			forcePosBuffer.Dispose();
			RenderTexture.ReleaseTemporary( veloctiyRT );
			RenderTexture.ReleaseTemporary( presureRT );
			RenderTexture.ReleaseTemporary( customRT );
		}
	}
}