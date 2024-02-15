using AnyTrail.DataAndTools;
using System.Collections.Generic;
using UnityEngine;

namespace AnyTrail.Effect.DrawPattern {
	//新建一个_AnyTrailWave的全局变量，用来在shader中传递RT
	public sealed class DrawPattern : CanvasComputerAbstract {
		//kernel
		int DrawTrail_Kernel;
		int DisappearGradually_Kernel;
		float disappearStep;

		//buffer和RT
		//结构体继承自接口，可以在Canvas里面修改数值后传递给ComputeShader
		ComputeShader PatternCompute;

		List<PatternWriterStructure> PatternWriterList;
		RenderTexture patternTrailRT;

		PatternParam waveParam;

		//RT数据
		readonly static int PatternRadius = Shader.PropertyToID( "patternRadius" );
		readonly static int TrailRT = Shader.PropertyToID( "_TrailRT" );
		readonly static int PatternTex = Shader.PropertyToID( "_PatternTex" );
		readonly static int Pos = Shader.PropertyToID( "pos" );
		readonly static int Radius = Shader.PropertyToID( "radius" );
		readonly static int Direction = Shader.PropertyToID( "direction" );
		readonly static int Intensity = Shader.PropertyToID( "intensity" );
		readonly static int SampleRadius = Shader.PropertyToID( "sampleRadius" );
		readonly static int AnyTrailPattern = Shader.PropertyToID( "_AnyTrailPattern" );
		readonly static int DisappearStep = Shader.PropertyToID( "disappearStep" );

		void CreateRT() {
			//用来写入速度场的RT
			patternTrailRT = RenderTexture.GetTemporary( rtPara.rtResolution, rtPara.rtResolution, 0, RenderTextureFormat.RFloat );
			patternTrailRT.enableRandomWrite = true; //允许写入
			patternTrailRT.filterMode = FilterMode.Bilinear;
			patternTrailRT.wrapMode = TextureWrapMode.Clamp;
			patternTrailRT.useMipMap = false;
			patternTrailRT.Create();

		}

		void InitShader() {
			DrawTrail_Kernel = PatternCompute.FindKernel( "DrawTrail" );
			DisappearGradually_Kernel = PatternCompute.FindKernel( "DisappearGradually" );

			PatternWriterList = new List<PatternWriterStructure>();

			//设置纹理
			PatternCompute.SetTexture( DrawTrail_Kernel, TrailRT, patternTrailRT );
			PatternCompute.SetTexture( DisappearGradually_Kernel, TrailRT, patternTrailRT );
		}

		override protected void Init( int Count, RTPara RTParameter ) {
			base.Init( Count, RTParameter );
			PatternCompute = Resources.Load<ComputeShader>( "PattenCompute" );
			CreateRT();
			InitShader();
			Shader.SetGlobalTexture( AnyTrailPattern, patternTrailRT );
		}

		public override void SetComputeParameter( ComputeParaInterface paramater ) {

			waveParam = (PatternParam)paramater;
			disappearStep = waveParam.disappearStep / 200f;
			PatternCompute.SetFloat( DisappearStep, disappearStep );
			if( disappearStep > 0 ) {
				StopWhenNoWriter = false;
			} else {
				StopWhenNoWriter = true;
			}
		}

		/// <summary>
		/// 构造函数
		/// </summary>
		/// <param name="Count"></param>
		/// <param name="RTParameter"></param>
		public DrawPattern( int Count, RTPara RTParameter ) {
			//设置当无writer时是否停止计算
			StopWhenNoWriter = true;
			Init( Count, RTParameter );
		}

		public override void GetRTs( ref List<RenderTexture> rt ) {
			rt.Add( patternTrailRT );
		}

		public override void AddWriterPara<T>( T writerPara ) {
			PatternWriterList.Add( (PatternWriterStructure)(object)writerPara );
			//CurrentWriterCount++
			base.AddWriterPara( writerPara );
		}

		override protected void Execute() {
			if( disappearStep > 0 ) {
				PatternCompute.Dispatch( DisappearGradually_Kernel, rtPara.rtResolution / 8, rtPara.rtResolution / 8, 1 );
			}
			foreach( var writer in PatternWriterList ) {
				var randomPatternNum = writer.randomNum;
				var randomPatternScale = writer.randomScale;
				for( int i = 0; i < randomPatternNum; i++ ) {
					float factor = (float)( i + 0.25 ) / randomPatternNum;
					if( randomPatternNum == 1 ) {
						factor = 0;
					}
					Vector2 direction = writer.direction + Random.insideUnitCircle.normalized * ( factor * ( writer.direction.magnitude * 10 ) );
					float radius = writer.radius - factor * writer.radius * ( 1 - Random.Range( 0.5f * randomPatternScale, randomPatternScale ) * 2 );
					Vector2 pos = writer.pos + direction.normalized * ( factor * Random.Range( 0.5f, 2f ) * radius * 3 );
					float intensity = writer.intensity - writer.intensity * ( factor ) * Random.Range( 0.1f, 2f );
					Texture2D patternTex = writer.patternTex;
		

					PatternCompute.SetVector( Pos, pos );
					PatternCompute.SetFloat( Radius, radius );
					PatternCompute.SetVector( Direction, direction );
					PatternCompute.SetFloat( Intensity, intensity );
					PatternCompute.SetFloat( PatternRadius, patternTex.width / 2f );

					int sampleRadius = Mathf.CeilToInt( Mathf.Sqrt( 2 ) * radius );
					PatternCompute.SetInt( SampleRadius, sampleRadius );
					PatternCompute.SetTexture( DrawTrail_Kernel, PatternTex, patternTex );
					if( 2 * sampleRadius > 8 ) {
						PatternCompute.Dispatch( DrawTrail_Kernel, 2 * sampleRadius / 8, 2 * sampleRadius / 8, 1 );
					}
				}
			}
			//清空结构体数组
			PatternWriterList.Clear();
		}

		public override void OnDestroy() {
			RenderTexture.ReleaseTemporary( patternTrailRT );
		}
	}
}