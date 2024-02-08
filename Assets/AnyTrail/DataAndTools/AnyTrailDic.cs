using AnyTrail.Effect.DrawFluid;
using AnyTrail.Effect.DrawPattern;
using AnyTrail.Effect.DrawTrail;
using AnyTrail.Effect.DrawWave;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace AnyTrail.DataAndTools {
	public class AnyTrailDic {
		//计算器，writer，computePara
		readonly public static Dictionary<Type, (Type, Type)> AllComputeCanvas = new Dictionary<Type, (Type, Type)>(){
			{
				typeof( DrawFluid ), ( typeof( FluidWriterStructure ), typeof( FluidParam ) )
			},{
				typeof( DrawWave ), ( typeof( WaveWriterStructure ), typeof( WaveParam ) )
			},
			{
				typeof( DrawTrail ), ( typeof( TrailWriterStructure ), typeof( TrailParam ) )
			},
			{
				typeof( DrawPattern ), ( typeof( PatternWriterStructure ), typeof( PatternParam ) )
			}
		};

		public T CreateInstance<T>() where T : struct {
			return new T();
		}

		//私有量，Canvas，WriterStruct，ComputeParaStruct的对应关系
		private Dictionary<CanvasComputerAbstract, (Type, Type )> _canvasStruct;

		//公有量
		public Dictionary<CanvasComputerAbstract, (Type, Type )> CanvasStruct {
			get {
				if( _canvasStruct == null ) {
					_canvasStruct = new Dictionary<CanvasComputerAbstract, (Type, Type )>();
				}
				return _canvasStruct;
			}
			set {
				_canvasStruct = value;
			}
		}

		/// <summary>
		/// 新建一个画布结构体
		/// </summary>
		/// <param name="canvasType"></param>
		/// <param name="writerCount"></param>
		/// <param name="rtPara"></param>
		public void AddCanvasStruct( Type canvasType, int writerCount, RTPara rtPara ) {
			foreach( var canvas in CanvasStruct ) {
				if( canvas.Key.GetType() == canvasType ) {
					return;
				}
			}
			foreach( var category in AllComputeCanvas ) {
				if( category.Key == canvasType ) {
					//设置参数
					object[] parameters ={
						writerCount,
						rtPara
					};
					CanvasStruct.Add( (CanvasComputerAbstract)Activator.CreateInstance( canvasType, parameters ), category.Value );
					return;
				}

			}
		}

		public void RemoveCanvasStruct( Type canvasType ) {
			foreach( var canvas in CanvasStruct ) {
				if( canvas.Key.GetType() == canvasType ) {
					canvas.Key.OnDestroy();
					CanvasStruct.Remove( canvas.Key );
					return;
				}
			}
		}

		/// <summary>
		/// 输入结构体，为对应的画布设置参数
		/// </summary>
		/// <param name="paramater"></param>
		/// <typeparam name="T"></typeparam>
		public void SetComputeParamater<T>( T paramater ) where T : ComputeParaInterface {
			foreach( var canvas in CanvasStruct ) {
				if( canvas.Value.Item2 == paramater.GetType() ) {
					// canvas.Key.ComputePara = paramater;
					canvas.Key.SetComputeParameter( paramater );
				}
			}
		}

		/// <summary>
		/// 输入结构体，为对应的画布画上新的轨迹
		/// </summary>
		/// <param name="writerPara"></param>
		/// <typeparam name="T"></typeparam>
		public void AddWriterPara<T>( T writerPara ) where T : WriterInterface {
			foreach( var canvas in CanvasStruct ) {
				if( canvas.Value.Item1 == writerPara.GetType() ) {
					canvas.Key.AddWriterPara( writerPara );
				}
			}
		}


		public void GetRTs( ref List<RenderTexture> rt ) {
			foreach( var canvas in CanvasStruct ) {
				canvas.Key.GetRTs( ref rt );
			}
		}

		public void OnDistroy() {
			foreach( var canvas in CanvasStruct ) {
				canvas.Key.OnDestroy();
			}
		}

		public void Update() {
			foreach( var canvas in CanvasStruct ) {
				canvas.Key.Update();
			}
		}
	}
}