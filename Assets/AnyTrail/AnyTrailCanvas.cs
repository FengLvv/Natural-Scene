using AnyTrail.DataAndTools;
using AnyTrail.Effect.DrawFluid;
using AnyTrail.Effect.DrawPattern;
using AnyTrail.Effect.DrawTrail;
using AnyTrail.Effect.DrawWave;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Unity.Mathematics;
using Unity.VisualScripting;
using UnityEngine;

namespace AnyTrail {
	public class AnyTrailCanvas : MonoBehaviour {
		AnyTrailCanvas _instance;
		static float dt = 0.0025f;

		#region 向外暴露的函数
		//设置流体参数
		//仅仅是为了在面板上显示，修改请用SetCanvas
		[SerializeField]
		private bool fluidCanvas;
		public FluidParam fluidParam = new FluidParam( 100, 6, 0, 20, 0,dt );

		[SerializeField]
		private bool waveCanvas;
		public WaveParam waveParam = new WaveParam( 0.98f, 0 );

		[SerializeField]
		private bool trailCanvas;
		public TrailParam trailParam = new TrailParam( 0f );


		[SerializeField]
		private bool patternCanvas;
		public PatternParam patternParam = new PatternParam( 25, 0 );

		/// <summary>
		/// 设置画布计算器的参数，在修改后调用一次
		/// </summary>
		public void ReSetComputeParamater() {
			anyTrailDic?.SetComputeParamater( fluidParam );
			anyTrailDic?.SetComputeParamater( waveParam );
			anyTrailDic?.SetComputeParamater( trailParam );
			anyTrailDic?.SetComputeParamater( patternParam );
		}

		/// <summary>
		/// 变量和画布的对应关系
		/// </summary>
		private void SetInspectorCanvasActive( Type type, bool value ) {
			if( type == typeof( DrawFluid ) ) {
				fluidCanvas = value;
			} else if( type == typeof( DrawWave ) ) {
				waveCanvas = value;
			} else if( type == typeof( DrawTrail ) ) {
				trailCanvas = value;
			} else if( type == typeof( DrawPattern ) ) {
				patternCanvas = value;
			}
		}

		/// <summary>
		/// 开启或关闭画布计算器
		/// </summary>
		public void SetCanvas( Type type, bool value ) {
			SetInspectorCanvasActive( type, value );
			if( value ) {
				//初始化画布计算器的画布
				anyTrailDic?.AddCanvasStruct( type, MaxWriterCount, rtPara );
				//初始化画布计算器的参数
				ReSetComputeParamater();
				ReGetRTs();
			} else {
				anyTrailDic?.RemoveCanvasStruct( type );
				ReGetRTs();
			}
		}

		//给画布添加一个轨迹，轨迹在每帧计算后被清空
		public void AddNewWriterStructure( WriterInterface writerStruct ) {
			writerStructs.Add( writerStruct );
		}

		//确保每种画布的writer数量不超过MaxWriterCount
		private void VerifyWriterCount() {
			//把list按照类型分组，每个类型最多MaxWriterCount个
			var processedList = writerStructs.GroupBy( item =>
				item.GetType() ).SelectMany( g => {
				if( g.Count() > MaxWriterCount ) {
					return g.Skip( g.Count() - MaxWriterCount );
				} else {
					return g;
				}
			} ).ToList();
			writerStructs = processedList;
		}
		#endregion

		#region MoveCanvas的参数
		private AnyTrailDic anyTrailDic;
		//ComputeShader的参数
		ComputeShader CanvasComputer;
		RenderTexture MoveTrailRT;
		public RTPara rtPara = new RTPara( 10, 512 );
		//0.01f是移动到RT边缘时刷新；4.99是跟随玩家刷新;每次刷新会降采样，以及额外的计算，所以这个值不用太大"
		[Range( 0.01f, 4.99f )]
		public float RTRefreshPartial = 0.25f;

		// writer的信息
		// RT的中心
		public Transform rtCenter;
		//写入位置的物体,物体需要 AnyTrailWriterGO 组件
		public int MaxWriterCount = 100;

		//计算用的参数
		//变化前的中心位置
		Vector3 playerPosLastFrame;
		//移动画布时，储存子类的RT
		List<RenderTexture> computeRTs;
		//储存所有writer的数据
		List<WriterInterface> writerStructs;

		// computeShader和shader的索引参数
		//kernel
		int MoveMapWithPeopleKernel;
		int WriteBackCanvasAfterMoveKernel;
		//AnyTrail的中心位置
		readonly static int PosCenterProperty = Shader.PropertyToID( "_AnyTrailPosCenter" );
		//AnyTrail的画布大小
		readonly static int CanvasSizeProperty = Shader.PropertyToID( "_AnyTrailCanvasSize" );
		//中心移动的像素距离
		readonly static int PositionCenterMoveProperty = Shader.PropertyToID( "positionCenterMove" );
		readonly static int MoveTrailRTProperty = Shader.PropertyToID( "MoveTrailRT" );
		readonly static int OriginalTrailRTProperty = Shader.PropertyToID( "OriginalTrailRT" );
		#endregion

		#region 刷新targetPlane的Shader屏幕参数
		private void RefreshTargetPlanePara( Vector2 posCenterVector2, float newRTSize ) {
			Shader.SetGlobalVector( PosCenterProperty, posCenterVector2 );
			Shader.SetGlobalFloat( CanvasSizeProperty, newRTSize );
		}
		private void RefreshTargetPlanePara( Vector2 posCenterVector2 ) {
			Shader.SetGlobalVector( PosCenterProperty, posCenterVector2 );
		}
		private void RefreshTargetPlanePara( float newRTSize ) {
			Shader.SetGlobalFloat( CanvasSizeProperty, newRTSize );
		}
		#endregion

		/// <summary>
		/// 初始化MoveCanvas的参数
		/// </summary>
		private void InitShader() {
			CanvasComputer = Resources.Load<ComputeShader>( "MoveCanvas" );

			computeRTs = new List<RenderTexture>();
			MoveMapWithPeopleKernel = CanvasComputer.FindKernel( "MoveMapWithPeople" );
			WriteBackCanvasAfterMoveKernel = CanvasComputer.FindKernel( "WriteBackCanvasAfterMove" );

			//用来添加新的点到轨迹
			MoveTrailRT = RenderTexture.GetTemporary( rtPara.rtResolution, rtPara.rtResolution, 0, RenderTextureFormat.ARGBFloat );
			MoveTrailRT.enableRandomWrite = true; //允许写入
			MoveTrailRT.filterMode = FilterMode.Bilinear;
			MoveTrailRT.wrapMode = TextureWrapMode.Clamp;
			MoveTrailRT.useMipMap = false;
			MoveTrailRT.Create();

			CanvasComputer.SetTexture( MoveMapWithPeopleKernel, MoveTrailRTProperty, MoveTrailRT );
			CanvasComputer.SetTexture( WriteBackCanvasAfterMoveKernel, MoveTrailRTProperty, MoveTrailRT );

			//初始化上一帧的中心位置
			playerPosLastFrame = rtCenter.position;
		}

		/// <summary>
		/// 当开关计算器时，重新获取RT
		/// </summary>
		private void ReGetRTs() {
			//初始化画布计算器的RT和writer
			computeRTs = new List<RenderTexture>();
			//获取要更新的所有RT
			anyTrailDic?.GetRTs( ref computeRTs );
		}

		/// <summary>
		/// 画布字典初始化，设置画布计算器的参数
		/// </summary>
		private void InitPara() {
			//初始化画布计算器
			anyTrailDic = new AnyTrailDic();
			ReGetRTs();

			writerStructs = new List<WriterInterface>();
			float3 posCenter = rtCenter.position;
			Vector2 posCenterVector2 = new Vector2( posCenter.x, posCenter.z );
			RefreshTargetPlanePara( posCenterVector2, rtPara.rtSize );
		}

		private void Awake() {
			_instance = this;
			InitShader();
			InitPara();
		}

		/// <summary>
		/// 计算新位置在RT中的像素位置
		/// </summary>
		/// <returns></returns>
		private void DrawNewTrail() {
			// VerifyWriterCount();

			//当player超出RT范围，刷新center位置
			Vector3 posCenter = rtCenter.position;
			Vector3 posLBLast = playerPosLastFrame - new Vector3( rtPara.rtSize, 0, rtPara.rtSize );
			Vector3 posRTLast = playerPosLastFrame + new Vector3( rtPara.rtSize, 0, rtPara.rtSize );

			/////////////////////////////s//////////////////////////
			//////////////传入角色移动，计算新位置的贴图////////////////
			//////////////////////////////////////////////////////
			//移动RT中心，更新纹理:当player超出RT范围，刷新center位置
			//本来是中心的随时跟着人物更新，但一方面要每帧多一个计算；另一方面，每次计算都是重采样，会导致轨迹模糊。
			//所以只有当人物超出RT范围时，才更新中心位置
			float rtSizeRef = rtPara.rtSize * RTRefreshPartial;
			if( posCenter.x < posLBLast.x + rtSizeRef || posCenter.x > posRTLast.x - rtSizeRef || posCenter.z < posLBLast.z + rtSizeRef || posCenter.z > posRTLast.z - rtSizeRef ) {
				//重新计算中心位置在RT中的像素位置
				Vector2 posCenterVector2 = new Vector2( posCenter.x, posCenter.z );
				//刷新targetPlane中的全局变量
				RefreshTargetPlanePara( posCenterVector2 );

				//中心的移动换算成RT中的像素移动距离
				Vector3 posCenterMove = posCenter - playerPosLastFrame;
				//注意这算出来的像素可能是小数，所以shader里面插值采样
				Vector2 posCenterMovePixel = new Vector2( posCenterMove.x / ( 2 * rtPara.rtSize ) * rtPara.rtResolution, posCenterMove.z / ( 2 * rtPara.rtSize ) * rtPara.rtResolution );

				//设置computeShader的移动距离
				CanvasComputer.SetVector( PositionCenterMoveProperty, posCenterMovePixel );

				//更新上一帧的中心位置
				playerPosLastFrame = posCenter;
				//更新上一帧的LB
				posLBLast = playerPosLastFrame - new Vector3( rtPara.rtSize, 0, rtPara.rtSize );

				//设置RT
				foreach( var rt in computeRTs ) {
					CanvasComputer.SetTexture( MoveMapWithPeopleKernel, OriginalTrailRTProperty, rt );
					CanvasComputer.SetTexture( WriteBackCanvasAfterMoveKernel, OriginalTrailRTProperty, rt );

					CanvasComputer.Dispatch( MoveMapWithPeopleKernel, rtPara.rtResolution / 8, rtPara.rtResolution / 8, 1 );
					//调用computeShader，把移动后的纹理写回RT
					CanvasComputer.Dispatch( WriteBackCanvasAfterMoveKernel, rtPara.rtResolution / 8, rtPara.rtResolution / 8, 1 );
				}
			}

			///////////////////////////////////////////////
			//////////////把新的位置加到RT里面//////////////// 
			///////////////////////////////////////////////
			foreach( var writer in writerStructs ) {
				// Debug.Log( writer.radius );
				//调整为RT中的像素位置
				Vector2 posInRT = new Vector2( ( writer.pos.x - posLBLast.x ) / ( 2 * rtPara.rtSize ) * rtPara.rtResolution, ( writer.pos.y - posLBLast.z ) / ( 2 * rtPara.rtSize ) * rtPara.rtResolution );
				writer.pos = posInRT;
				writer.radius = writer.radius / ( rtPara.rtSize * 2 ) * rtPara.rtResolution;

				//找到对应的画布计算器，把writer的数据传入
				anyTrailDic.AddWriterPara( writer );
			}
			anyTrailDic.Update();

			//清空当前帧的笔刷
			writerStructs.Clear();
		}

		void Start() {
			SetCanvas( typeof( DrawFluid ), fluidCanvas );
			SetCanvas( typeof( DrawWave ), waveCanvas );
			SetCanvas( typeof( DrawTrail ), trailCanvas );
			SetCanvas( typeof( DrawPattern ), patternCanvas );
			 StartCoroutine( StartDrawTrail() );
		}

        //开个协程，每隔dt秒执行一次  DrawNewTrail 
        private IEnumerator StartDrawTrail() {
			while( true ) {
				DrawNewTrail();
				yield return new WaitForSeconds( dt );
			}
		}
         
        
        
		
		float lastExecutionTime = 0f;
		// void Update() {
		// 	if( Time.time - lastExecutionTime >= dt ) {
		// 		lastExecutionTime = Time.time;
		// 		// 在这里执行需要重复的逻辑
		// 		DrawNewTrail();
		// 	}
		// }

		private void OnDestroy() {
			//清空RT
			foreach( var rt in computeRTs ) {
				RenderTexture.ReleaseTemporary( rt );
			}
			//清空画布计算器
			anyTrailDic.OnDistroy();
			//清空computeShader
			RenderTexture.ReleaseTemporary( MoveTrailRT );
		}
	}
}