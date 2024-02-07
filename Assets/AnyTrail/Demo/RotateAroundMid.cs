using UnityEngine;
namespace AnyTrail.Demo {
	public class RotateAroundMid : MonoBehaviour {
		public Transform rotationCenter; // 自定义的旋转中心点
		public float rotationSpeed = 50f;
		public float radius = 5f; // 自定义的旋转半径
public float initialOffset = 0f; 
		void Update()
		{
			// 根据自定义的旋转半径计算位置
			float angle = Time.time * rotationSpeed+initialOffset;
			Vector3 offset = new Vector3(Mathf.Sin(angle), 0, Mathf.Cos(angle)) * radius;
			transform.position = rotationCenter.position + offset;

			// 让物体绕着自定义的旋转中心点旋转
			transform.RotateAround(rotationCenter.position, Vector3.up, rotationSpeed * Time.deltaTime/20f);
		}
	}
}