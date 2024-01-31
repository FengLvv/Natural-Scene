using System.Collections.Generic;
using System.Linq;
using UnityEngine;


public class DrawTrailToRT : MonoBehaviour {
	[Header( "ComputeShader的信息" )]
	public ComputeShader trailComputer;
	//kernel
	int moveTrailWithPeopleKernel;
	int writeinNewPosKernel;
	int gaussianBlurKernelVertical;
	int gaussianBlurKernelHorizontal;
	//compute buffer,用来传入新的位置
	ComputeBuffer trailPosBuffer;
	struct TrailPosStruct {
		public Vector2 pos;
		public float radius;
	}
	TrailPosStruct[] TrailPosStructArray;
	int trailPosBufferStride;

	[Header( "RT的信息" )]
	RenderTexture TrailRT_Add_Move_BlurH_BlurV;
	public int rtResolution = 1024;
	public int rtSize = 3;

	[Tooltip( "是否使用高斯模糊处理轨迹" )]
	public bool useBlur = true;
	public int blurRadius = 3;

	[Header( "写入新像素的信息" )]
	[Tooltip( "写入位置的物体" )]
	public List<Transform> writeinPoses;
	//上一帧和这一帧的位置
	Vector3[] writeinPosesLastFrame;
	Vector3[] writeinPosesThisFrame;
	//新位置在RT中的像素位置
	Vector2[] trailPosResolution;

	[Tooltip( "RT的中心" )]
	public Transform player;
	//变化前的中心位置
	Vector3 playerPosLastFrame;

	[Tooltip( "轨迹消失的时间" )]
	public float trailDisappearTime = 5;

	[Tooltip( "轨迹的半径" )]
	public float trailRadius = 0.2f;
	float trailRadiusPixel;

	[Header( "雪平面的信息" )]
	public GameObject snowPlane;

	void InitShader() {
		//设置写入的纹理到shader
		writeinNewPosKernel = trailComputer.FindKernel( "WriteinNewPos" );
		trailComputer.SetTexture( writeinNewPosKernel, "TrailRT_Add_Move_BlurH_BlurV", TrailRT_Add_Move_BlurH_BlurV );

		moveTrailWithPeopleKernel = trailComputer.FindKernel( "MoveTrailWithPeople" );
		trailComputer.SetTexture( moveTrailWithPeopleKernel, "TrailRT_Add_Move_BlurH_BlurV", TrailRT_Add_Move_BlurH_BlurV );

		gaussianBlurKernelHorizontal = trailComputer.FindKernel( "GaussianBlurHorizontal" );
		trailComputer.SetTexture( gaussianBlurKernelHorizontal, "TrailRT_Add_Move_BlurH_BlurV", TrailRT_Add_Move_BlurH_BlurV );

		gaussianBlurKernelVertical = trailComputer.FindKernel( "GaussianBlurVertical" );
		trailComputer.SetTexture( gaussianBlurKernelVertical, "TrailRT_Add_Move_BlurH_BlurV", TrailRT_Add_Move_BlurH_BlurV );

		//初始化computeShader的参数
		trailComputer.SetInt( "blurRadius", blurRadius );
		if( trailDisappearTime > 0 ) {
			trailComputer.SetFloat( "deltaTime", Time.deltaTime );
			trailComputer.SetFloat( "trailDisappearTime", trailDisappearTime );
		} else {
			trailComputer.SetFloat( "deltaTime", 0 );
			trailComputer.SetFloat( "trailDisappearTime", 0 );
		}

		//设置computeBuffer和传入的数据
		trailPosBufferStride = sizeof( float ) * 3;
		trailPosBuffer = new ComputeBuffer( writeinPoses.Count, trailPosBufferStride );

		//初始化计算参数
		writeinPosesLastFrame = new Vector3[writeinPoses.Count];
		writeinPosesThisFrame = new Vector3[writeinPoses.Count];
		trailPosResolution = new Vector2[writeinPoses.Count];
		//计算轨迹半径对应的像素半径
		trailRadiusPixel = trailRadius / ( rtSize * 2 ) * rtResolution;
		//初始化上一帧的中心位置
		Vector3 posCenter = player.position;
		playerPosLastFrame = posCenter;

		//雪平面传入参数
		//更具是否使用模糊，传入不同的纹理
		snowPlane.GetComponent<Renderer>().sharedMaterial.SetTexture( "_TrailMap", TrailRT_Add_Move_BlurH_BlurV );

		//传入中心位置，用来计算世界uv
		Vector2 posCenterVector2 = new Vector2( posCenter.x, posCenter.z );
		snowPlane.GetComponent<Renderer>().sharedMaterial.SetVector( "_WorldPos", posCenterVector2 );
		//传入半径,用来计算世界uv
		snowPlane.GetComponent<Renderer>().sharedMaterial.SetFloat( "_Size", rtSize );
	}

	void Start() {
		//用来添加新的点到轨迹
		TrailRT_Add_Move_BlurH_BlurV = new RenderTexture( rtResolution, rtResolution, 0, RenderTextureFormat.ARGBFloat );
		TrailRT_Add_Move_BlurH_BlurV.enableRandomWrite = true; //允许写入
		TrailRT_Add_Move_BlurH_BlurV.filterMode = FilterMode.Bilinear;
		TrailRT_Add_Move_BlurH_BlurV.wrapMode = TextureWrapMode.Clamp;
		TrailRT_Add_Move_BlurH_BlurV.useMipMap = false;
		TrailRT_Add_Move_BlurH_BlurV.Create();

		InitShader();
	}

	/// <summary>
	/// 计算新位置在RT中的像素位置
	/// </summary>
	/// <returns></returns>
	void DrawNewTrail() {
		//提取这一帧的位置
		writeinPosesThisFrame = new Vector3[writeinPoses.Count];
		writeinPosesThisFrame = writeinPoses.Select( x => x.position ).ToArray();

		////////////////////////////////////////////////////
		///////判断是否写入纹理，所有点都不移动，则这一帧不计算/////
		///////////////////////////////////////////////////
		//就比较每个位置的变化，如果有变化，就写入
		for( int i = 0; i < writeinPoses.Count; i++ ) {
			if( writeinPosesThisFrame[i] != writeinPosesLastFrame[i] ) {
				Debug.Log( "位置移动" );
				writeinPosesLastFrame = writeinPosesThisFrame;
				break;
			}
			//如果到最后一个位置都没有变化，就不写入
			if( i == writeinPosesThisFrame.Length - 1 ) {
				Debug.Log( "位置没变" );
				return;
			}
		}

		//当player超出RT范围，刷新center位置
		Vector3 posCenter = player.position;
		Vector3 posLBLast = playerPosLastFrame - new Vector3( rtSize, 0, rtSize );
		Vector3 posRTLast = playerPosLastFrame + new Vector3( rtSize, 0, rtSize );
		///////////////////////////////////////////////////////
		//////////////传入角色移动，计算新位置的贴图////////////////
		//////////////////////////////////////////////////////
		//// 移动RT中心，更新纹理
		//当player超出RT范围，刷新center位置
		//本来是中心的随时跟着人物更新，但一方面要每帧多一个计算；另一方面，每次计算都是重采样，会导致轨迹模糊。所以只有当人物超出RT范围时，才更新中心位置
		if( posCenter.x < posLBLast.x + 0.5f || posCenter.x > posRTLast.x - 0.5f || posCenter.z < posLBLast.z + 0.5f || posCenter.z > posRTLast.z - 0.5f ) {
			//重新计算中心位置在RT中的像素位置
			Vector2 posCenterVector2 = new Vector2( posCenter.x, posCenter.z );
			snowPlane.GetComponent<Renderer>().sharedMaterial.SetVector( "_WorldPos", posCenterVector2 );

			//中心的移动换算成RT中的像素移动距离
			Vector3 posCenterMove = posCenter - playerPosLastFrame;
			//注意这算出来的像素可能是小数，所以shader里面插值采样
			Vector2 posCenterMovePixel = new Vector2( posCenterMove.x / ( 2 * rtSize ) * rtResolution, posCenterMove.z / ( 2 * rtSize ) * rtResolution );
			playerPosLastFrame = posCenter;

			trailComputer.SetVector( "positionCenterMove", posCenterMovePixel );
			trailComputer.Dispatch( moveTrailWithPeopleKernel, rtResolution / 8, rtResolution / 8, 1 );

			//更新上一帧的LB
			posLBLast = playerPosLastFrame - new Vector3( rtSize, 0, rtSize );
		} else {
			//如果没有移动，就把移动距离设为0（shader里面会判断，决定用那个图作为初始图）
			trailComputer.SetVector( "positionCenterMove", Vector4.zero );
		}

		///////////////////////////////////////////////
		//////////////把新的位置加到RT里面////////////////
		///////////////////////////////////////////////
		////依据新中心计算在RT中的像素位置
		trailPosResolution = new Vector2[writeinPoses.Count];
		//计算新位置在RT中的像素位置
		for( int index = 0; index < writeinPosesThisFrame.Length; index++ ) {
			Vector3 perPos = writeinPosesThisFrame[index];
			//计算像素位置,这里的z对应RT的y
			Vector2 posInRT = new Vector2( ( perPos.x - posLBLast.x ) / ( 2 * rtSize ) * rtResolution, ( perPos.z - posLBLast.z ) / ( 2 * rtSize ) * rtResolution );
			trailPosResolution[index] = posInRT;
		}
		////设置computeBuffer和传入的数据，写入新位置
		TrailPosStructArray = new TrailPosStruct[writeinPoses.Count];
		for( int i = 0; i < writeinPoses.Count; i++ ) {
			TrailPosStructArray[i].pos = trailPosResolution[i];
			TrailPosStructArray[i].radius = trailRadiusPixel;
		}
		trailPosBuffer.SetData( TrailPosStructArray );
		trailComputer.SetBuffer( writeinNewPosKernel, "trailPosBuffer", trailPosBuffer );
		trailComputer.SetInt( "trailPosCount", trailPosBuffer.count );
		trailComputer.Dispatch( writeinNewPosKernel, rtResolution / 8, rtResolution / 8, 1 );

		/////////////////////////////////////////////////////
		//////////////如果开启了模糊，就添加高斯模糊//////////////
		/////////////////////////////////////////////////////
		if( useBlur ) {
			//水平方向模糊
			trailComputer.Dispatch( gaussianBlurKernelHorizontal, rtResolution / 8, rtResolution / 8, 1 );
			//垂直方向模糊
			trailComputer.Dispatch( gaussianBlurKernelVertical, rtResolution / 8, rtResolution / 8, 1 );
		}
	}

	// Update is called once per frame
	void Update() {
		DrawNewTrail();
	}

	void OnDestroy() {
		trailPosBuffer.Dispose();
		TrailRT_Add_Move_BlurH_BlurV.Release();
	}
}