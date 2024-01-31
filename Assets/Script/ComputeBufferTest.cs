using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Diagnostics;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.Serialization;

public class ComputeBufferTest : MonoBehaviour {
	// Start is called before the first frame update
	RTHandle _cameraColor;
	[FormerlySerializedAs( "_computeShader" )]
	public ComputeShader computeShader;
	int _kernelHandle;
	RenderTexture _tempTex;
	void Start() {
		_kernelHandle = computeShader.FindKernel ("CSMain");
		Camera mainCamera = Camera.main;
		RenderTexture tempTex = RenderTexture.GetTemporary( mainCamera.pixelWidth, mainCamera.pixelHeight, 0, RenderTextureFormat.ARGB32, RenderTextureReadWrite.Default, 1);
		tempTex.enableRandomWrite = true; //允许随机写入!!!
	}
	void OnRenderImage( RenderTexture source, RenderTexture destination ) {
		CommandBuffer cmd = CommandBufferPool.Get( "CustomRenderPass" );
		using( new ProfilingScope( cmd, new ProfilingSampler( "CustomRenderPass" ) ) ) {
			computeShader.SetTexture( _kernelHandle, "Result", _tempTex );//给compute shader传入纹理
			computeShader.Dispatch( _kernelHandle,1,1,1 );
		}
	}
}