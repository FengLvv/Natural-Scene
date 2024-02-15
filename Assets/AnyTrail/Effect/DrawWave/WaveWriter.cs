using AnyTrail.DataAndTools;
using UnityEngine;

namespace AnyTrail.Effect.DrawWave {
	public class WaveWriter : DrawTrailGO {
		private WaveWriterStructure ForceStruct;
	
		public override WriterInterface GetNewestStructure( Vector3 positionWS, Vector3 directionWS, float radius, float intensity ) {
			Vector2 pos2D = new Vector2( positionWS.x, positionWS.z );
			Vector2 dir2D = new Vector2( directionWS.x, directionWS.z );
			if( intensity > 0 && dir2D.magnitude > 0.02f ) {
				ForceStruct.pos = pos2D;
				ForceStruct.radius = radius;
				ForceStruct.direction = dir2D.normalized;
				ForceStruct.intensity = intensity;
				return ForceStruct;
			}
			return null;
		}
	}
}