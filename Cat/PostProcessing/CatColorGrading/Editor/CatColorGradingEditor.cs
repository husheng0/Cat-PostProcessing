using System;
using UnityEngine;
using UnityEditor;
using Cat.Common;
using Cat.PostProcessing;
using Cat.CommonEditor;

namespace Cat.PostProcessingEditor {

	[CustomEditor(typeof(CatColorGrading))]
	//[CanEditMultipleObjects]
	public class CatColorGradingEditor : Editor {
		public SerializedProperty settings;

		public void OnEnable() {
			settings = serializedObject.FindProperty("m_Settings");
		}

		public override void OnInspectorGUI() {
			SerializedProperty propertyIterator = serializedObject.FindProperty("m_Settings");
			var isFirst = true;
			var count = 1;

			serializedObject.Update();
			//
			// Tonemapping:
			//
			DrawPropertyField(propertyIterator, ref isFirst);
			var tonemapper = settings.FindPropertyRelative("tonemapper").enumValueIndex;
			if (tonemapper == 2) {
				DrawPropertyField(propertyIterator, ref isFirst);
				DrawPropertyField(propertyIterator, ref isFirst);
			} else {
				SkipPropertyField(propertyIterator, ref isFirst);
				SkipPropertyField(propertyIterator, ref isFirst);

			}
			if (tonemapper >= 2) {
				EditorGUILayout.Space();
				DrawToneMappingFunction(128, 96, tonemapper-2);
				EditorGUILayout.Space();
			}

			//
			// Color Grading:
			//
			DrawPropertyField(propertyIterator, ref isFirst);
			DrawPropertyField(propertyIterator, ref isFirst);
			DrawPropertyField(propertyIterator, ref isFirst);

			//
			// Color Correction:
			//
			DrawPropertyField(propertyIterator, ref isFirst);
			DrawPropertyField(propertyIterator, ref isFirst);

			//
			// Color Mixer:
			//
			DrawPropertyField(propertyIterator, ref isFirst);
			var colorMapper = settings.FindPropertyRelative("colorMixer").enumValueIndex;
			if (colorMapper == 4) {
				DrawPropertyField(propertyIterator, ref isFirst);
				DrawPropertyField(propertyIterator, ref isFirst);
				DrawPropertyField(propertyIterator, ref isFirst);
				DrawPropertyField(propertyIterator, ref isFirst);
			} else {
				SkipPropertyField(propertyIterator, ref isFirst);
				SkipPropertyField(propertyIterator, ref isFirst);
				SkipPropertyField(propertyIterator, ref isFirst);
				SkipPropertyField(propertyIterator, ref isFirst);

			}

			//
			// Curves:
			//
			DrawPropertyField(propertyIterator, ref isFirst);
			DrawPropertyField(propertyIterator, ref isFirst);
			DrawPropertyField(propertyIterator, ref isFirst);

			DrawPropertyField(propertyIterator, ref isFirst);
			DrawPropertyField(propertyIterator, ref isFirst);


			//while (DrawPropertyField(propertyIterator, ref isFirst)) { /* NOP; */ }
			serializedObject.ApplyModifiedProperties();

			// serializedObject.Update();
			// EditorGUILayout.PropertyField(settings);
			// serializedObject.ApplyModifiedProperties();


			//
			// Tonemapping:
			//
			EditorGUILayout.Space();
			DrawResponseFunction(128, 96);
			EditorGUILayout.Space();
		}

		bool DrawPropertyField(SerializedProperty prop, ref bool isFirst) {
			var hasNext = prop.Next(isFirst);
			isFirst = false;
			if (hasNext) {
				EditorGUILayout.PropertyField(prop);
			}
			return hasNext;
		}

		bool SkipPropertyField(SerializedProperty prop, ref bool isFirst) {
			var hasNext = prop.Next(isFirst);
			isFirst = false;
			return hasNext;
		}

		void DrawToneMappingFunction(float width, float height, int tonemappingFuncionIndex) {
			var blackPoint = settings.FindPropertyRelative("blackPoint").floatValue;
			var grayPoint  = settings.FindPropertyRelative("midPoint").floatValue;
			var whitePoint = settings.FindPropertyRelative("whitePoint").floatValue;

			var response = Mathf.Pow(2, settings.FindPropertyRelative("response").floatValue);
			var gain =     Mathf.Pow(2, settings.FindPropertyRelative("gain").floatValue);


			var black = 0.0f + blackPoint * 0.25f;
			var gray  = 0.5f + grayPoint  * 0.125f;
			var white = 1.0f + whitePoint * 0.25f;

			var range = new Rect(
				0f, 0f,
				2, 1f
			);

			var rect = GUILayoutUtility.GetRect(width, height);
			var graph = new Graph(rect, range);

			// Background
			graph.DrawRect(graph.m_Range, 0.1f, 0.4f);

			// Grid
			graph.DrawGrid(new Vector2(4, 4), 0.4f, 2);
			graph.DrawGrid(new Vector2(2, 2), 0.4f, 3);
			// Label
			//Handles.Label(
			//	PointInRect(0, m_RangeY) + Vector3.right,
			//	"Brightness Response (linear)", EditorStyles.miniLabel
			//);

			// Threshold Range
			// var thresholdRect = new Rect(black, graph.m_Range.y, white - black, graph.m_Range.height);
			// graph.DrawRect(thresholdRect, 0.25f, -1f);
			// Threshold line
			// graph.DrawLine(new Vector2(black, graph.m_Range.yMin), new Vector2(black, graph.m_Range.yMax), 0.85f);
			// graph.DrawLine(new Vector2(white, graph.m_Range.yMin), new Vector2(white, graph.m_Range.yMax), 0.85f);
			// var grayLerp = Mathf.Lerp(black, white, gray);
			// graph.DrawLine(new Vector2(grayLerp,  graph.m_Range.yMin), new Vector2(grayLerp,  graph.m_Range.yMax), 0.85f);


			// Precompute some values

			const float scaleFactor = 20f;
			const float scaleFactorHalf = scaleFactor * 0.5f;

			float inBlack = 0.02f * scaleFactor + 1f;
			float outBlack = 0f * scaleFactorHalf + 1f;
			float inWhite = 10f / scaleFactor;
			float outWhite = 1f - 10f / scaleFactor;
			float blackRatio = inBlack / outBlack;
			float whiteRatio = inWhite / outWhite;

			const float a = 0.2f;
			float b = Mathf.Max(0f, Mathf.LerpUnclamped(0.57f, 0.37f, blackRatio));
			float c = Mathf.LerpUnclamped(0.01f, 0.24f, whiteRatio);
			float d = Mathf.Max(0f, Mathf.LerpUnclamped(0.02f, 0.20f, blackRatio));
			const float e = 0.02f;
			const float f = 0.30f;
			float whiteLevel = 5.3f;
			float whiteClip = 10f / scaleFactorHalf;

			// Graph
			//graph.DrawFunction(x => x * Mathf.Pow(Mathf.Max(0, x - minLuminance) / (x + 1e-1f), kneeStrength + 1), 0.1f);
			var curveParams = (serializedObject.targetObject as CatColorGrading).settings.GetCurveParams();
			graph.DrawFunction(
				//x => NeutralTonemap(x, a, b, c, d, e, f, whiteLevel, whiteClip)
				x => NeutralTonemap(x, 1, 1), 
				0.90f
			);
			if (tonemappingFuncionIndex == 0) {
				graph.DrawFunction(
					x => NeutralTonemap(x, response, gain), 
					Color.red * 0.75f
				);
			} else if (tonemappingFuncionIndex == 1) {
				graph.DrawFunction(
					x => Uncharted2Tonemap(x, response, gain), 
					Color.blue * 0.75f
				);
			}

		}

		float NeutralTonemap(float x, float response, float gain) {
			const float k = 1.235f;
			var amplitude = x;
			var y = gain * k * x / ((k - x / (0.1f + x) * 0.3f) / response * gain + amplitude);
			return y;
		}

		float Uncharted2Tonemap(float x, float response, float gain) {
			const float k = 1.235f;
			const float a = 0.15f;
			const float b = 0.50f;
			const float c = 0.10f;
			const float d = 0.20f;
			const float e = 0.02f;
			const float f = 0.30f;
			const float W = 11.21f;
			const float exposureBias = 2.00f;

			// Tonemap
			float whiteScale = 1f / NeutralCurve(W, a, b, c, d, e, f);
			var y = NeutralCurve(x * exposureBias, a, b, c, d, e, f);
			y *= whiteScale;

			//var y = ((x * (a * x + c * b) + d * e) / (x * (a * x + b) + d * f)) - e / f;
			return y;
		}

		float NeutralCurve(float x, float a, float b, float c, float d, float e, float f)
		{
			return ((x * (a * x + c * b) + d * e) / (x * (a * x + b) + d * f)) - e / f;
		}

		float NeutralTonemap(float x, float a, float b, float c, float d, float e, float f, float whiteLevel, float whiteClip)
		{
			x = Mathf.Max(0f, x);

			// Tonemap
			float whiteScale = 1f / NeutralCurve(whiteLevel, a, b, c, d, e, f);
			x = NeutralCurve(x * whiteScale, a, b, c, d, e, f);
			x *= whiteScale;

			// Post-curve white point adjustment
			x /= whiteClip;

			return x;
		}


		void DrawResponseFunction(float width, float height) {
			var blackPoint = settings.FindPropertyRelative("blackPoint").floatValue;
			var grayPoint  = settings.FindPropertyRelative("midPoint").floatValue;
			var whitePoint = settings.FindPropertyRelative("whitePoint").floatValue;

			var black = 0.0f + blackPoint * 0.25f;
			var gray  = 0.5f + grayPoint  * 0.125f;
			var white = 1.0f + whitePoint * 0.25f;

			var minX = Mathf.Min(0f, black);
			var maxX = Mathf.Max(1f, white);
			var range = new Rect(
				minX, 0f,
				maxX - minX, 1f
			);

			var rect = GUILayoutUtility.GetRect(width, height);
			var graph = new Graph(rect, range);

			// Background
			graph.DrawRect(graph.m_Range, 0.1f, 0.4f);

			// Grid
			graph.DrawGrid(new Vector2(4, 4), 0.4f, 2);
			graph.DrawGrid(new Vector2(2, 2), 0.4f, 3);
			// Label
			//Handles.Label(
			//	PointInRect(0, m_RangeY) + Vector3.right,
			//	"Brightness Response (linear)", EditorStyles.miniLabel
			//);

			// Threshold Range
			var thresholdRect = new Rect(black, graph.m_Range.y, white - black, graph.m_Range.height);
			graph.DrawRect(thresholdRect, 0.25f, -1f);
			// Threshold line
			graph.DrawLine(new Vector2(black, graph.m_Range.yMin), new Vector2(black, graph.m_Range.yMax), 0.85f);
			graph.DrawLine(new Vector2(white, graph.m_Range.yMin), new Vector2(white, graph.m_Range.yMax), 0.85f);
			var grayLerp = Mathf.Lerp(black, white, gray);
			graph.DrawLine(new Vector2(grayLerp,  graph.m_Range.yMin), new Vector2(grayLerp,  graph.m_Range.yMax), 0.85f);

			// Graph
			//graph.DrawFunction(x => x * Mathf.Pow(Mathf.Max(0, x - minLuminance) / (x + 1e-1f), kneeStrength + 1), 0.1f);
			var curveParams = (serializedObject.targetObject as CatColorGrading).settings.GetCurveParams();
			graph.DrawFunction(
				x => {
					x = (x - black) / (white - black);
					x = Mathf.Clamp01(x);
					return (curveParams.w + (curveParams.z + (curveParams.y + curveParams.x*x)*x)*x)*x;
				}
				, 0.90f
			);
		}

	}
}
