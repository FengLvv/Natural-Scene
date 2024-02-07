using AnyTrail.DataAndTools;
using System.Collections.Generic;
using UnityEngine;

namespace AnyTrail.Effect.DrawWave {
	//新建一个_AnyTrailWave的全局变量，用来在shader中传递RT
	//x:这一帧的轨迹图,y:上一帧的轨迹图,z:下一帧的轨迹图
	public sealed class DrawWave : CanvasComputerAbstract {
		//kernel
		int Wave_Kernel;
		int Swap_Kernel;

		//buffer和RT
		//结构体继承自接口，可以在Canvas里面修改数值后传递给ComputeShader
		public ComputeShader WaveCompute;
		int WriterCount; //写入的数量
		WaveWriterStructure[] ForceStructArray;
		ComputeBuffer forcePosBuffer;
		int forceBufferStride;
		RenderTexture waveRT1;

		private WaveParam waveParam;


		//RT数据
		readonly static int Damping = Shader.PropertyToID( "damping" );
		readonly static int _WaveRT1 = Shader.PropertyToID( "_WaveRT" );
		readonly static int AnyTrailWave = Shader.PropertyToID( "_AnyTrailWave" );
		readonly static int ForcePosBuffer = Shader.PropertyToID( "forcePosBuffer" );
		readonly static int ForceCount = Shader.PropertyToID( "forceCount" );
		readonly static int DisappearStep = Shader.PropertyToID( "disappearStep" );

		public override void SetComputeParameter( ComputeParaInterface paramater ) {
			waveParam = (WaveParam)paramater;
			WaveCompute.SetFloat( Damping, waveParam.dampling );
			WaveCompute.SetFloat( DisappearStep, waveParam.disappearStep/10f );
		}

		void CreateRT() {
			//用来写入速度场的RT
			waveRT1 = RenderTexture.GetTemporary( rtPara.rtResolution, rtPara.rtResolution, 0, RenderTextureFormat.ARGBFloat );
			waveRT1.enableRandomWrite = true; //允许写入
			waveRT1.filterMode = FilterMode.Bilinear;
			waveRT1.wrapMode = TextureWrapMode.Clamp;
			waveRT1.useMipMap = false;
			waveRT1.Create();
		}

		private void InitShader() {
			Wave_Kernel = WaveCompute.FindKernel( "Wave" );
			Swap_Kernel = WaveCompute.FindKernel( "Swap" );

			//设置纹理
			WaveCompute.SetTexture( Wave_Kernel, _WaveRT1, waveRT1 );
			WaveCompute.SetTexture( Swap_Kernel, _WaveRT1, waveRT1 );
	

			//设置参数
			forceBufferStride = sizeof( float ) * 6;
			forcePosBuffer = new ComputeBuffer( WriterCount, forceBufferStride );
		}

		override protected void Init( int Count, RTPara RTParameter ) {
			//设置当无writer时是否停止计算
			base.Init( Count, RTParameter );
			WaveCompute = Resources.Load<ComputeShader>( "WavesCompute" );
			WriterCount = Count;

			//初始化结构体数组
			ForceStructArray = new WaveWriterStructure[WriterCount];

			CreateRT();
			InitShader();

			//x:这一帧的轨迹图,y:上一帧的轨迹图,z:下一帧的轨迹图
			Shader.SetGlobalTexture( AnyTrailWave, waveRT1 );
		}

		/// <summary>
		/// 构造函数
		/// </summary>
		/// <param name="Count"></param>
		/// <param name="RTParameter"></param>
		public DrawWave( int Count, RTPara RTParameter ) {
			//设置当无writer时是否停止计算
			StopWhenNoWriter = false;
			Init( Count, RTParameter );
		}

		public override void GetRTs( ref List<RenderTexture> rt ) {
			rt.Add( waveRT1 );
		}

		public override void AddWriterPara<T>( T writerPara ) {
			ForceStructArray[CurrentWriterCount] = (WaveWriterStructure)(object)writerPara;
			//CurrentWriterCount++
			base.AddWriterPara( writerPara );
		}

		override protected void Execute() {
			forcePosBuffer.SetData( ForceStructArray );
			WaveCompute.SetBuffer( Wave_Kernel, ForcePosBuffer, forcePosBuffer );
			WaveCompute.SetInt( ForceCount, forcePosBuffer.count );
			SimulateWave();
			//清空结构体数组
			ForceStructArray = new WaveWriterStructure[WriterCount];
		}
		void SimulateWave() {
			WaveCompute.Dispatch( Wave_Kernel, rtPara.rtResolution / 8, rtPara.rtResolution / 8, 1 );
			WaveCompute.Dispatch( Swap_Kernel, rtPara.rtResolution / 8, rtPara.rtResolution / 8, 1 );
		}

		public override void OnDestroy() {
			forcePosBuffer.Dispose();
			RenderTexture.ReleaseTemporary( waveRT1 );
		}
	}
}