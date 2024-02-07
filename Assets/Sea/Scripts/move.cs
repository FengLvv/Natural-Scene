using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent( typeof( CharacterController ) )]
public class move : MonoBehaviour {
	public float moveSpeed = 5.0f;
	public float rotationSpeed = 50.0f;
	private CharacterController characterController;
	
	float positionY;
	
	void Start() {
		characterController = GetComponent<CharacterController>();
		positionY = transform.position.y;
	}

	void Update() {
		// 移动控制
		float horizontalInput = Input.GetAxis( "Horizontal" );
		float verticalInput = Input.GetAxis( "Vertical" );
		Vector3 moveDirection = new Vector3( 0, verticalInput, transform.position.y>positionY? 1 :0  );
		moveDirection = transform.TransformDirection( moveDirection );
		characterController.Move( moveDirection * moveSpeed * Time.deltaTime );



		// 计算旋转方向
		transform.Rotate( -horizontalInput * Vector3.forward * rotationSpeed * Time.deltaTime );

	}



}