using AnyTrail.DataAndTools;
using UnityEngine;

namespace AnyTrail.Effect.DrawTrail {
	public class TrailWriter : DrawTrailGO {
		private TrailWriterStructure ForceStruct;

		public override WriterInterface GetNewestStructure( Vector3 posWS, Vector3 directionWS, float radius, float intensity ) {
			Vector2 pos2D = new Vector2( posWS.x, posWS.z );
			Vector2 dir2D = new Vector2( directionWS.x, directionWS.z );
			if( dir2D.magnitude > 0.01f ) {
				//2D位置
				ForceStruct.pos = pos2D;
				//2D半徑: 移动越慢，半径越大
				ForceStruct.radius = radius ;
				//归一化的2d方向
				ForceStruct.direction = dir2D.normalized;
				//设置的强度
				ForceStruct.intensity = intensity;
				return ForceStruct;
			}
			else {
				//如果位置没有变化，那么就不用添加了
				return null;
			}
		}
	}
}