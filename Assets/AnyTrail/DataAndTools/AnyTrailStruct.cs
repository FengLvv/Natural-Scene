using System;
using UnityEngine;

namespace AnyTrail.DataAndTools {

	[Serializable]
	public struct RTPara {
		public float rtSize; //10
		public int rtResolution; //512
		/// <summary>
		/// 10,512
		/// </summary>
		/// <param name="rtSize"></param>
		/// <param name="rtResolution"></param>
		public RTPara( float rtSize, int rtResolution ) {
			this.rtSize = rtSize;
			this.rtResolution = rtResolution;
		}
	}

	//确保每个轨迹数据至少包含三个内容
	public interface WriterInterface {
		//位置不可以手动设置
		Vector2 pos { get; set; }
		float radius { get; set; }
		public Vector2 direction { get; set; }
		public float intensity { get; set; }
	}

	public interface ComputeParaInterface {
	}

	/// <summary>
	/// 流体模拟的结构体
	/// </summary>
	[Serializable]
	public struct FluidWriterStructure : WriterInterface {
		public Vector2 pos { get; set; }
		public float radius { get; set; }
		public Vector2 direction { get; set; }
		public float intensity { get; set; }
	}


	[Serializable]
	public struct FluidParam : ComputeParaInterface {
		[Header( "流体模拟参数" )]
		[Tooltip( "平流速度" )]
		[Range( 10, 300 )]
		public float advectionSpeed; //1
		[Tooltip( "粘性项次数" )] [Range( 0, 20 )]
		public int diffusionTimes; //6
		[Tooltip( "粘度" )] [Range( 0, 0.01f )]
		public float vscosity; //0
		[Tooltip( "速度场迭代次数" )] [Range( 5, 30 )]
		public int pressureIterationTimes; //20
		[Tooltip( "固定速度衰减" )] [Range( 0f, 1f )]
		public float disappearStep; //0.98
		public float dt;

		/// <summary>
		/// 1,6,0,20
		/// </summary>
		/// <param name="advectionSpeed"></param>
		/// <param name="diffusionTimes"></param>
		/// <param name="vscosity"></param>
		/// <param name="pressureIterationTimes"></param>
		public FluidParam( float advectionSpeed, int diffusionTimes, float vscosity, int pressureIterationTimes, float disappearStep,float dt) {
			this.advectionSpeed = advectionSpeed;
			this.diffusionTimes = diffusionTimes;
			this.vscosity = vscosity;
			this.pressureIterationTimes = pressureIterationTimes;
			this.disappearStep = disappearStep;
			this.dt = dt;
		}
	}


	[Serializable]
	public struct WaveWriterStructure : WriterInterface {
		public Vector2 pos { get; set; }
		public float radius { get; set; }
		public Vector2 direction { get; set; }
		public float intensity { get; set; }
	}

	[Serializable]
	public struct WaveParam : ComputeParaInterface {
		[SerializeField]
		//向外暴露的流体参数
		[Range( 0.8f, 0.999f )] [Tooltip( "阻尼" )]
		public float dampling; //0.98
		[Tooltip( "固定速度衰减" )] [Range( 0f, 1f )]
		public float disappearStep; //0.98
		public WaveParam( float dampling, float disappearStep ) {
			this.dampling = dampling;
			this.disappearStep = disappearStep;
		}
	}

	[Serializable]
	public struct TrailWriterStructure : WriterInterface {
		public Vector2 pos { get; set; }
		public float radius { get; set; }
		public Vector2 direction { get; set; }
		public float intensity { get; set; }
	}

	[Serializable]
	public struct TrailParam : ComputeParaInterface {
		[SerializeField]
		//向外暴露的流体参数
		[Range( 0f, 1f )] [Tooltip( "每帧消失的大小，0是永久存在" )]
		public float disappearStep; //0.98
		public TrailParam( float disappearStep ) {
			this.disappearStep = disappearStep;
		}
	}



	[Serializable]
	public struct PatternWriterStructure : WriterInterface {
		//向外暴露的流体参数
		[Range( 0, 10 )] [Tooltip( "周围随机生成的图案数量" )]
		public int randomNum; //0.98
		[Range( 0f, 1f )] [Tooltip( "周围随机生成的图案缩放" )]
		public float randomScale; //0.98
		public Vector2 pos { get; set; }
		public float radius { get; set; }
		public Vector2 direction { get; set; }
		public float intensity { get; set; }
		public Texture2D patternTex;
	}

	[Serializable]
	public struct PatternParam : ComputeParaInterface {
		[SerializeField]
		[Tooltip( "固定速度衰减" )] [Range( 0f, 1f )]
		public float disappearStep; //0.98
		public PatternParam( int patternRadius, float disappearStep ) {
			this.disappearStep = disappearStep;

		}
	}
}