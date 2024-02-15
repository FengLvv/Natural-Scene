using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Rendering;

[ExecuteAlways]
public class SeaControl : MonoBehaviour {
	[Header( "Big Wave" )]
	public float baseSpeed = 0.5f;
	public float baseAmplitude = 0.5f;
	public float baseWaveLength = 1f;
	public float baseDirection = 0f;
	
	[Range( 1, 10 )]
	public int waveNum = 10;

	readonly static int VertWavePara = Shader.PropertyToID( "_VertWavePara" );

	void Start() {
		SetUpVertWaveParam();
	}

	void SetUpVertWaveParam() {
		Vector4[] bigWaveParam = Enumerable.Repeat( new Vector4( 0, 0, 0, 0 ), 10 ).ToArray();

		for( int i = 0; i < waveNum; i++ ) {
			Random.InitState( i * 1000 + 50 );
			var weight = Mathf.Lerp( 0.5f, 1.5f, (float)i / 10 );
			var amplitude = baseAmplitude * weight * Random.Range( 0.8f, 1.2f );
			var direction = baseDirection + Random.Range( -90f, 90f );
			var length = baseWaveLength * weight * Random.Range( 0.6f, 1.4f );
			bigWaveParam[i] = new Vector4( amplitude, direction, length, baseSpeed );
			// bigWaveParam[i] = new Vector4( 1000,1000,1000,1000 );
		}
		Shader.SetGlobalVectorArray( VertWavePara, bigWaveParam );
	}

	// Update is called once per frame
	void Update() {
		SetUpVertWaveParam();
	}

}