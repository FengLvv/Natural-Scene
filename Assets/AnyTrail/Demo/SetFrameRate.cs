using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SetFrameRate : MonoBehaviour
{
	void Awake() {
		Application.targetFrameRate = 120;
	}
}
