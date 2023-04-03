// https://bronsonzgeb.com/index.php/2021/08/08/unity-editor-tools-the-place-objects-tool/

using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace LMirman.PrefabPainter
{
	public class PrefabPainterEditorWindow : EditorWindow
	{
		private bool savedBrushFoldoutState;
		private Vector2 scrollPosition;
		private readonly List<PrefabPainterConfigAsset> configAssets = new List<PrefabPainterConfigAsset>();
		private static string lastSavePath;

		[MenuItem("Tools/Prefab Painter")]
		private static void ShowWindow()
		{
			PrefabPainterEditorWindow window = GetWindow<PrefabPainterEditorWindow>();
			window.titleContent = new GUIContent("Prefab Painter");
			window.Show();
		}

		private void OnEnable()
		{
			PrefabPainterTool.SaveConfigToPrefs();
		}

		private void OnFocus()
		{
			RefreshBrushList();
		}

		private void OnLostFocus()
		{
			PrefabPainterTool.LoadConfigFromPrefs();
		}

		private void OnGUI()
		{
			DrawSavedBrushes();
			EditorGUILayout.Space();

			DrawBrushProperties();
			EditorGUILayout.Space();

			DrawAvoidance();
			EditorGUILayout.Space();

			DrawInvoke();
			EditorGUILayout.Space();
		}

		private void DrawSavedBrushes()
		{
			savedBrushFoldoutState = EditorGUILayout.Foldout(savedBrushFoldoutState, "Saved Brushes", EditorStyles.foldoutHeader);
			if (!savedBrushFoldoutState)
			{
				return;
			}

			scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition, GUILayout.MaxHeight(160));
			foreach (PrefabPainterConfigAsset configAsset in configAssets)
			{
				if (GUILayout.Button(configAsset.name))
				{
					LoadBrush(configAsset.Config);
				}
			}

			if (GUILayout.Button("+"))
			{
				SaveCurrentBrush();
			}

			EditorGUILayout.EndScrollView();
		}

		private static void DrawBrushProperties()
		{
			EditorGUILayout.LabelField("Brush Properties", EditorStyles.boldLabel);
			PrefabPainterConfig config = PrefabPainterTool.Config;
			config.PrefabToPaint = EditorGUILayout.ObjectField("Prefab", config.PrefabToPaint, typeof(GameObject), true) as GameObject;
			config.Radius = EditorGUILayout.Slider("Radius", config.Radius, PrefabPainterConfig.RadiusMin, PrefabPainterConfig.RadiusMax);
			config.DistancePerPaint = EditorGUILayout.Slider("Distance per Paint", config.DistancePerPaint, 1 / PrefabPainterConfig.DensityMax, 1 / PrefabPainterConfig.DensityMin);
			config.AxisToRandomize = (PrefabPainterConfig.Axis)EditorGUILayout.EnumFlagsField("Randomize Rotation", config.AxisToRandomize);
			config.Force2DMode = EditorGUILayout.Toggle("Force 2D Mode", config.Force2DMode);
		}

		private static void DrawAvoidance()
		{
			PrefabPainterConfig config = PrefabPainterTool.Config;
			List<GameObject> list = config.PrefabsToAvoid;
			EditorGUILayout.LabelField("Instance Avoidance", EditorStyles.boldLabel);
			config.UseRadiusForAvoidanceRange = EditorGUILayout.Toggle("Use Radius for Avoidance", config.UseRadiusForAvoidanceRange);
			EditorGUI.BeginDisabledGroup(config.UseRadiusForAvoidanceRange);
			config.AvoidanceRange = EditorGUILayout.Slider("Avoidance Range", config.AvoidanceRange, PrefabPainterConfig.RadiusMin, PrefabPainterConfig.RadiusMax);
			EditorGUI.EndDisabledGroup();

			EditorGUILayout.BeginHorizontal();
			EditorGUILayout.LabelField("Prefabs to Avoid");
			if (GUILayout.Button("Clear", GUILayout.Width(100)))
			{
				list.Clear();
			}

			EditorGUILayout.EndHorizontal();

			int removeIndex = -1;
			for (int i = 0; i < list.Count; i++)
			{
				EditorGUILayout.BeginHorizontal();
				if (GUILayout.Button("X", GUILayout.Width(20)))
				{
					removeIndex = i;
				}

				list[i] = EditorGUILayout.ObjectField(list[i], typeof(GameObject), true) as GameObject;
				EditorGUILayout.EndHorizontal();
			}

			if (removeIndex >= 0 && removeIndex < list.Count)
			{
				list.RemoveAt(removeIndex);
			}

			GameObject objectToAdd = EditorGUILayout.ObjectField("Add Avoid Object", null, typeof(GameObject), true) as GameObject;
			if (objectToAdd != null)
			{
				list.Add(objectToAdd);
			}
		}

		private static void DrawInvoke()
		{
			PrefabPainterConfig config = PrefabPainterTool.Config;
			EditorGUILayout.LabelField("Invoke On Paint", EditorStyles.boldLabel);
			config.InvokeOnPaint = EditorGUILayout.Toggle("Use Invoke on Paint", config.InvokeOnPaint);
			EditorGUI.BeginDisabledGroup(!config.InvokeOnPaint);
			config.InvokeOnPaintMessage = EditorGUILayout.TextField("Invoke Message Name", config.InvokeOnPaintMessage);
			EditorGUI.EndDisabledGroup();
		}

		private static void SaveCurrentBrush()
		{
			string path = EditorUtility.SaveFilePanelInProject("Save Prefab Brush", "Prefab Brush.asset", "asset", "Select a location in the project to save your Prefab Painter Brush",
				lastSavePath);
			if (path.Length > 0)
			{
				PrefabPainterConfigAsset asset = CreateInstance<PrefabPainterConfigAsset>();
				asset.Config.LoadConfig(PrefabPainterTool.Config);
				AssetDatabase.CreateAsset(asset, path);
				AssetDatabase.SaveAssets();
				lastSavePath = path;
			}
		}

		private void LoadBrush(PrefabPainterConfig config)
		{
			PrefabPainterTool.Config.LoadConfig(config);
		}

		private void RefreshBrushList()
		{
			configAssets.Clear();
			string[] guids = AssetDatabase.FindAssets("t:PrefabPainterConfigAsset");
			foreach (string guid in guids)
			{
				try
				{
					string path = AssetDatabase.GUIDToAssetPath(guid);
					PrefabPainterConfigAsset asset = AssetDatabase.LoadAssetAtPath<PrefabPainterConfigAsset>(path);
					configAssets.Add(asset);
				}
				catch
				{
					// ignored
				}
			}
		}
	}
}