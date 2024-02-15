using AnyTrail.DataAndTools;
using System.Collections.Generic;
using UnityEngine;

namespace AnyTrail.Effect.DrawTrail {
	//新建一个_AnyTrailTrail的全局变量，用来在shader中传递RT
	public sealed class DrawTrail : CanvasComputerAbstract {
		//kernel
		int DrawTrailKernel;

		//buffer和RT
		//结构体继承自接口，可以在Canvas里面修改数值后传递给ComputeShader
		public ComputeShader TrailCompute;
		int WriterCount; //写入的数量
		TrailWriterStructure[] ForceStructArray;
		ComputeBuffer forcePosBuffer;
		int forceBufferStride;
		RenderTexture trailRT;

		private TrailParam trailParam;

		//RT数据
		readonly static int TrailDisappearStep = Shader.PropertyToID( "trailDisappearStep" );
		readonly static int ForcePosBuffer = Shader.PropertyToID( "forcePosBuffer" );
		readonly static int ForceCount = Shader.PropertyToID( "forceCount" );
		readonly static int TrailRT = Shader.PropertyToID( "_TrailRT" );
		readonly static int AnyTrailTrail = Shader.PropertyToID( "_AnyTrailTrail" );

		public override void SetComputeParameter( ComputeParaInterface paramater ) {
			trailParam = (TrailParam)paramater;
			TrailCompute.SetFloat( TrailDisappearStep, trailParam.disappearStep/50f );
			if( TrailDisappearStep>0 ) {
				StopWhenNoWriter = false;
			} else {
				StopWhenNoWriter = true;
			}
		}

		void CreateRT() {
			//用来写入速度场的RT
			trailRT = RenderTexture.GetTemporary( rtPara.rtResolution, rtPara.rtResolution, 0, RenderTextureFormat.RFloat );
			trailRT.enableRandomWrite = true; //允许写入
			trailRT.filterMode = FilterMode.Bilinear;
			trailRT.wrapMode = TextureWrapMode.Clamp;
			trailRT.useMipMap = false;
			trailRT.Create();
		}

		private void InitShader() {
			DrawTrailKernel = TrailCompute.FindKernel( "DrawTrail" );

			//设置纹理
			TrailCompute.SetTexture( DrawTrailKernel, TrailRT, trailRT );

			//设置参数
			forceBufferStride = sizeof( float ) * 6;
			forcePosBuffer = new ComputeBuffer( WriterCount, forceBufferStride );
		}

		protected override void Init( int Count, RTPara RTParameter ) {
			base.Init( Count, RTParameter );
			TrailCompute = Resources.Load<ComputeShader>( "TrailCompute" );
			WriterCount = Count;

			//初始化结构体数组
			ForceStructArray = new TrailWriterStructure[WriterCount];

			CreateRT();
			InitShader();

			Shader.SetGlobalTexture( AnyTrailTrail, trailRT );
		}

		/// <summary>
		/// 构造函数
		/// </summary>
		/// <param name="Count"></param>
		/// <param name="RTParameter"></param>
		public DrawTrail( int Count, RTPara RTParameter ) {
			//设置当无writer时是否停止计算
			StopWhenNoWriter = true;
			Init( Count, RTParameter );
		}

		public override void GetRTs( ref List<RenderTexture> rt ) {
			rt.Add( trailRT );
		}

		public override void AddWriterPara<T>( T writerPara ) {
			ForceStructArray[CurrentWriterCount] = (TrailWriterStructure)(object)writerPara;
			//CurrentWriterCount++
			base.AddWriterPara( writerPara );
		}

		protected override void Execute() {
			forcePosBuffer.SetData( ForceStructArray );
			TrailCompute.SetBuffer( DrawTrailKernel, ForcePosBuffer, forcePosBuffer );
			TrailCompute.SetInt( ForceCount, forcePosBuffer.count );
			SimulateWave();
			//清空结构体数组
			ForceStructArray = new TrailWriterStructure[WriterCount];
		}
		void SimulateWave() {
			//advection
			TrailCompute.Dispatch( DrawTrailKernel, rtPara.rtResolution / 8, rtPara.rtResolution / 8, 1 );
		}

		public override void OnDestroy() {
			forcePosBuffer.Dispose();
			RenderTexture.ReleaseTemporary( trailRT );
		}
	}
}