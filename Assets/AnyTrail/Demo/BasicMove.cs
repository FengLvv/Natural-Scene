using AnyTrail.Effect.DrawFluid;
using AnyTrail.Effect.DrawPattern;
using AnyTrail.Effect.DrawTrail;
using AnyTrail.Effect.DrawWave;
using UnityEngine;

namespace AnyTrail.Demo {
	public class BasicMove : MonoBehaviour {
 
		public float moveSpeed = 5f;

		void Update() {
			float horizontalInput = Input.GetAxis( "Horizontal" );
			float verticalInput = Input.GetAxis( "Vertical" );

			Vector3 movement = new Vector3( horizontalInput, 0f, verticalInput ) * (moveSpeed * Time.deltaTime);
			transform.Translate( movement );

			if( Input.GetKeyDown( KeyCode.Space ) ) {
				GetComponent<TrailWriter>().AutoDrawTrail=true;
				GetComponent<PatternWriter>().AutoDrawTrail=true;
				GetComponent<FluidWriter>().AutoDrawTrail=true;
				GetComponent<WaveWriter>().AutoDrawTrail=true;
			}
			if( Input.GetKeyUp(  KeyCode.Space ) ) {
				GetComponent<TrailWriter>().AutoDrawTrail=false;
				GetComponent<PatternWriter>().AutoDrawTrail=false;
				GetComponent<FluidWriter>().AutoDrawTrail=false;
				GetComponent<WaveWriter>().AutoDrawTrail=false;
			}
			if( Input.GetKeyDown( KeyCode.Alpha1 ) ) {
				GetComponent<TrailWriter>().WriteTrail=!GetComponent<TrailWriter>().WriteTrail;
			}
			if( Input.GetKeyDown( KeyCode.Alpha2 ) ) {
				GetComponent<PatternWriter>().WriteTrail=!GetComponent<PatternWriter>().WriteTrail;
			} 
			if( Input.GetKeyDown( KeyCode.Alpha3 ) ) {
				GetComponent<FluidWriter>().WriteTrail=!GetComponent<FluidWriter>().WriteTrail;
			}
			if( Input.GetKeyDown( KeyCode.Alpha4 ) ) {
				GetComponent<WaveWriter>().WriteTrail=!GetComponent<WaveWriter>().WriteTrail;
			}
		}
	
	}
}