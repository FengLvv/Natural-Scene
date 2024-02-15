using AnyTrail.DataAndTools;
using JetBrains.Annotations;
using UnityEngine;

namespace AnyTrail.Effect {
	public class DrawTrailGO : MonoBehaviour {
		public AnyTrailCanvas anyTrailCanvas;
		public bool WriteTrail = true;
		[Range( 0f, 5f )]
		public float TrailRadius = 0.1f;
		[Range( 0f, 100f )]
		public float TrailIntensity = 100;
		[Space( 10 )]
		public bool AutoDrawTrail = true;
		[Range( 0, 1f )]
		public float AutoDrawTrailInterval = 0.1f;

		float trailTimer=0.01f;

		Vector3 lastPosWS;
		Vector3 dirWS;
		Vector3 positionWS;

		[CanBeNull] public virtual WriterInterface GetNewestStructure( Vector3 posWS, Vector3 directionWS, float radius, float intensity ) {
			return null;
		}


		void Start() {
			var position = transform.position;
			lastPosWS = position;
			positionWS = position;
			dirWS = Vector3.zero;
		}
	
		/// <summary>
		/// 检查如果开了自动绘制，那么就添加笔刷
		/// </summary>
		void CheckAndAddFluidStructure() {
			trailTimer += Time.deltaTime;
			if( WriteTrail && AutoDrawTrail && trailTimer > AutoDrawTrailInterval ) {
				AddTrail();
				trailTimer = 0.01f;
			}
		}

		public void AddTrail() {
			if( WriteTrail == false )
				return;
			var newWriterStructure = GetNewestStructure( positionWS, dirWS, TrailRadius, TrailIntensity );
			if( newWriterStructure != null ) {
				anyTrailCanvas.AddNewWriterStructure( newWriterStructure );
			}
		}

		public void AddTrail( Vector3 addPositionWS, Vector3 addDirectionWS, float addRadius, float addIntensity ) {
			if( WriteTrail == false )
				return;
			var newWriterStructure = GetNewestStructure( addPositionWS, addDirectionWS, addRadius, addIntensity );
			if( newWriterStructure != null ) {
				anyTrailCanvas.AddNewWriterStructure( newWriterStructure );
			}
		}

		void Update() {
			//每帧计算一次方向
			positionWS = transform.position;
			dirWS = positionWS - lastPosWS;
			lastPosWS = positionWS;
			CheckAndAddFluidStructure();
		}
	}
}