using AnyTrail.DataAndTools;
using UnityEngine;

namespace AnyTrail.Effect.DrawFluid {
	public class FluidWriter : DrawTrailGO {
		private FluidWriterStructure ForceStruct;

		public override WriterInterface GetNewestStructure( Vector3 positionWS, Vector3 directionWS, float radius, float intensity ) {
			Vector2 pos2D = new Vector2( positionWS.x, positionWS.z );
			Vector2 dir2D = new Vector2( directionWS.x, directionWS.z );
			if( dir2D.magnitude > 0.01f ) {
				//2D位置
				ForceStruct.pos = pos2D;
				//2D半徑: 移动越慢，半径越大
				ForceStruct.radius = radius*5 / ( dir2D.magnitude * 3f + 0.5f );
				//归一化的2d方向
				ForceStruct.direction = dir2D.normalized;
				//设置的强度:移动越慢，力越小
				ForceStruct.intensity = dir2D.magnitude * intensity*15f;
				return ForceStruct;
			}
			else {
				//如果位置没有变化，那么就不用计算了
				return null;
			}
		}
	}
}