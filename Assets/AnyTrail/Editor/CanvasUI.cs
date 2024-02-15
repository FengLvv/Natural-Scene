using AnyTrail.Effect.DrawFluid;
using AnyTrail.Effect.DrawPattern;
using AnyTrail.Effect.DrawTrail;
using AnyTrail.Effect.DrawWave;
using System;
using UnityEditor;
using UnityEngine;

namespace AnyTrail.UIElement {
	[CustomEditor( typeof( AnyTrailCanvas ) )]
	public class CanvasUI : Editor {
		private SerializedProperty rtPara;
		private SerializedProperty RTRefreshPartial;
		private SerializedProperty rtCenter;
		private SerializedProperty MaxWriterCount;

		private SerializedProperty fluidCanvas;
		private SerializedProperty fluidParam;

		private SerializedProperty waveCanvas;
		private SerializedProperty waveParam;

		private SerializedProperty trailCanvas;
		private SerializedProperty trailParam;
		
		private SerializedProperty patternCanvas;
		private SerializedProperty patternParam;
	

		AnyTrailCanvas anyTrailCanvas;
		void OnEnable() {
			fluidCanvas = serializedObject.FindProperty( "fluidCanvas" );
			fluidParam = serializedObject.FindProperty( "fluidParam" );
			waveCanvas = serializedObject.FindProperty( "waveCanvas" );
			waveParam = serializedObject.FindProperty( "waveParam" );
			trailCanvas = serializedObject.FindProperty( "trailCanvas" );
			trailParam = serializedObject.FindProperty( "trailParam" );
			patternCanvas = serializedObject.FindProperty( "patternCanvas" );
			patternParam = serializedObject.FindProperty( "patternParam" );


			rtPara = serializedObject.FindProperty( "rtPara" );
			RTRefreshPartial = serializedObject.FindProperty( "RTRefreshPartial" );
			rtCenter = serializedObject.FindProperty( "rtCenter" );
			MaxWriterCount = serializedObject.FindProperty( "MaxWriterCount" );
		
		


			anyTrailCanvas = target as AnyTrailCanvas;
		}


		public override void OnInspectorGUI() {
			// 更新序列化对象
			serializedObject.Update();

			EditorGUILayout.LabelField( "画布的参数" );
			EditorGUILayout.PropertyField( rtPara, new GUIContent( "RT参数" ) );
			EditorGUILayout.PropertyField( RTRefreshPartial, new GUIContent( "RT刷新参数", "0.01f是移动到RT边缘时刷新；4.99是跟随玩家刷新;每次刷新会降采样，以及额外的计算，所以这个值不用太大" ) );
			EditorGUILayout.Space();

			EditorGUILayout.LabelField( "Writer的信息" );
			EditorGUILayout.PropertyField( rtCenter, new GUIContent( "RT的中心" ) );
			EditorGUILayout.PropertyField( MaxWriterCount, new GUIContent( "最大接受的轨迹数量" ) );
			EditorGUILayout.Space();


			//每个Computer单独设置
			DrawCanvasProperty( typeof( DrawFluid ), fluidCanvas, "流体", fluidParam );
			DrawCanvasProperty( typeof( DrawWave ), waveCanvas, "波浪", waveParam );
			DrawCanvasProperty( typeof( DrawTrail ), trailCanvas, "轨迹", trailParam );
 			DrawCanvasProperty( typeof( DrawPattern ), patternCanvas, "图案", patternParam );

		
			// 应用属性的修改
			serializedObject.ApplyModifiedProperties();
		}
		private void DrawCanvasProperty( Type type, SerializedProperty canvas, string label, SerializedProperty param ) {
			EditorGUILayout.Space();
			EditorGUILayout.LabelField( label, EditorStyles.boldLabel );
			EditorGUILayout.PropertyField( canvas, new GUIContent( "是否打开" + label + "画布" ) );
			if( Time.frameCount > 10 ) {
				anyTrailCanvas.SetCanvas( type, canvas.boolValue );
			}
			if( canvas.boolValue ) {
				EditorGUILayout.PropertyField( param, new GUIContent( label + "参数" ) );
				anyTrailCanvas.ReSetComputeParamater();
			}
		}
	}
}