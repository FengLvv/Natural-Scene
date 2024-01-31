using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SetSeaCenter : MonoBehaviour
{
	readonly static int CartoonSeaCenter = Shader.PropertyToID( "_CartoonSeaCenter" );

	void Update()
    {
       Shader.SetGlobalVector( CartoonSeaCenter, transform.position ); 
    }
}
