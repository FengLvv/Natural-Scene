using AnyTrail.DataAndTools;
using UnityEngine;

namespace AnyTrail.PostEffectOnTrail {
	public class PostEffectOnTrail  {
		public ComputeShader postEffectComputer;
		int gaussianBlurKernelVertical;
		int gaussianBlurKernelHorizontal;
		RTPara rtPara;
		
		[Header( "是否使用高斯模糊处理轨迹" )]
		public bool useBlur;
		public int blurRadius = 3;


		void InitShader() {
			gaussianBlurKernelHorizontal = postEffectComputer.FindKernel( "GaussianBlurHorizontal" );
			gaussianBlurKernelVertical = postEffectComputer.FindKernel( "GaussianBlurVertical" );
			//初始化computeShader的参数
			postEffectComputer.SetInt( "blurRadius", blurRadius );
		}

		// Start is called before the first frame update
		void Start() {
			//水平方向模糊
			postEffectComputer.Dispatch( gaussianBlurKernelHorizontal, rtPara.rtResolution / 8, rtPara.rtResolution / 8, 1 );
			//垂直方向模糊
			postEffectComputer.Dispatch( gaussianBlurKernelVertical, rtPara.rtResolution / 8, rtPara.rtResolution / 8, 1 );
		}

		// Update is called once per frame
		void Update() {

		}
	}
}