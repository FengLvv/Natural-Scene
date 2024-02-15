using AnyTrail.DataAndTools;
using UnityEngine;

namespace AnyTrail.Effect.DrawPattern {
	public class PatternWriter : DrawTrailGO {
		private PatternWriterStructure ForceStruct;
		public Texture2D patternTex;
		[Range( 1, 10 )]
		public int randomNum=1;
		[Range( 0, 1f )]
		public float randomScale;


		public override WriterInterface GetNewestStructure( Vector3 positionWS, Vector3 directionWS, float radius, float intensity ) {
			Vector2 pos2D = new Vector2( positionWS.x, positionWS.z );
			Vector2 dir2D = new Vector2( directionWS.x, directionWS.z );
			if( intensity > 0 ) {
				ForceStruct.pos = pos2D;
				ForceStruct.radius = radius;
				ForceStruct.direction = dir2D.normalized;
				ForceStruct.intensity = intensity;
				ForceStruct.patternTex = patternTex;
				ForceStruct.randomNum = randomNum;
				ForceStruct.randomScale = randomScale;

				return ForceStruct;
			}
			return null;
		}
	}
}