using JetBrains.Annotations;
using UnityEngine;

namespace AnyTrail.DataAndTools {
	public abstract class TrailWriterAbstract {
		//返回一个实现接口的结构体
		[CanBeNull] public abstract WriterInterface GetNewestStructure( Vector3 positionWS ,Vector3 directionWS, float radius, float intensity);
	}
}