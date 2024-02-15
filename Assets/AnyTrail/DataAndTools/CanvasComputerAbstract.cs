using System.Collections.Generic;
using UnityEngine;

namespace AnyTrail.DataAndTools {
	public abstract class CanvasComputerAbstract {
		//计算的属性，在子类里面设置
		protected bool StopWhenNoWriter = true;
		
		//传入writer的数量,子类可读取，不可修改
		protected int CurrentWriterCount {
			get {
				return _CurrentWriterCount;
			}
			private set {
				_CurrentWriterCount = value;
			}
		}
		private int _CurrentWriterCount;
		
		protected RTPara rtPara { get; set; }
	
		/// <summary>
		/// 修改非每帧更新的参数
		/// </summary>
		/// <param name="paramater"></param>
		public abstract void SetComputeParameter( ComputeParaInterface paramater );
		
		/// <summary>
		/// 初始化，创建时候调用
		/// </summary>
		/// <param name="WriterCount"></param>
		/// <param name="RTParameter"></param>
		protected virtual void Init( int WriterCount, RTPara RTParameter ) {
			rtPara = RTParameter;
		}

		/// <summary>
		/// 从canvas调用，子类不继承。
		/// 执行compute shader计算的代码写在子类的Execute()里面
		/// </summary>
		public virtual void Update() {
			if( CurrentWriterCount != 0 || !StopWhenNoWriter ) {
				Execute();
				//执行完后重置数量
				CurrentWriterCount = 0;
			}
		}

		/// <summary>
		/// 写compute shader计算的代码
		/// </summary>
		protected abstract void Execute();

		/// <summary>
		/// 将需要随玩家移动的贴图放到writerPara里面
		/// </summary>
		/// <param name="writerPara"></param>
		/// <typeparam name="T"></typeparam>
		public virtual void AddWriterPara<T>( T writerPara ) where T : WriterInterface {
			CurrentWriterCount++;
		}

		/// <summary>
		/// 释放RT，buffer
		/// </summary>
		public abstract void OnDestroy();

		/// <summary>
		/// 将需要随玩家移动的贴图放到rt里面
		/// </summary>
		/// <param name="rt">rt.add(xxx)</param>
		public abstract void GetRTs( ref List<RenderTexture> rt );

	}
}