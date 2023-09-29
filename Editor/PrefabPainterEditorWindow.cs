// https://bronsonzgeb.com/index.php/2021/08/08/unity-editor-tools-the-place-objects-tool/

using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace LMirman.PrefabPainter
{
	public class PrefabPainterEditorWindow : EditorWindow
	{
		private bool savedBrushFoldoutState;
		private bool brushPropertiesFoldoutState;
		private bool avoidanceFoldoutState;
		private bool invokeFoldoutState;
		private Vector2 savedBrushScrollPosition;
		private Vector2 globalScrollPosition;
		private readonly List<PrefabPainterConfigAsset> configAssets = new List<PrefabPainterConfigAsset>();
		private static string lastSavePath;

		private const string FoldoutSavedBrushesKey = "prefabPainter_foldoutSavedBrushes";
		private const string FoldoutPropertiesKey = "prefabPainter_foldoutProperties";
		private const string FoldoutAvoidanceKey = "prefabPainter_foldoutAvoidance";
		private const string FoldoutInvokeKey = "prefabPainter_foldoutInvoke";

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
			savedBrushFoldoutState = EditorPrefs.GetBool(FoldoutSavedBrushesKey, true);
			brushPropertiesFoldoutState = EditorPrefs.GetBool(FoldoutPropertiesKey, true);
			avoidanceFoldoutState = EditorPrefs.GetBool(FoldoutAvoidanceKey, true);
			invokeFoldoutState = EditorPrefs.GetBool(FoldoutInvokeKey, true);
			RefreshBrushList();
		}

		private void OnLostFocus()
		{
			EditorPrefs.SetBool(FoldoutSavedBrushesKey, savedBrushFoldoutState);
			EditorPrefs.SetBool(FoldoutPropertiesKey, brushPropertiesFoldoutState);
			EditorPrefs.SetBool(FoldoutAvoidanceKey, avoidanceFoldoutState);
			EditorPrefs.SetBool(FoldoutInvokeKey, invokeFoldoutState);
			PrefabPainterTool.LoadConfigFromPrefs();
		}

		private void OnGUI()
		{
			globalScrollPosition = EditorGUILayout.BeginScrollView(globalScrollPosition);
			DrawSavedBrushes();
			DrawBrushProperties();
			DrawAvoidance();
			DrawInvoke();
			EditorGUILayout.EndScrollView();
			return;

			void DrawSavedBrushes()
			{
				DrawGroup(ref savedBrushFoldoutState, "Saved Brushes", DrawContent);
				return;

				void DrawContent()
				{
					savedBrushScrollPosition = EditorGUILayout.BeginScrollView(savedBrushScrollPosition, GUILayout.Height(120));
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
			}

			void DrawBrushProperties()
			{
				DrawGroup(ref brushPropertiesFoldoutState, "Brush Properties", DrawContent);
				return;

				void DrawContent()
				{
					PrefabPainterConfig config = PrefabPainterTool.Config;
					config.PrefabToPaint = EditorGUILayout.ObjectField("Prefab", config.PrefabToPaint, typeof(GameObject), true) as GameObject;
					config.Radius = EditorGUILayout.Slider("Radius", config.Radius, PrefabPainterConfig.RadiusMin, PrefabPainterConfig.RadiusMax);
					config.DistancePerPaint = EditorGUILayout.Slider("Distance per Paint", config.DistancePerPaint, 1 / PrefabPainterConfig.DensityMax, 1 / PrefabPainterConfig.DensityMin);
					config.AxisToRandomize = (PrefabPainterConfig.Axis)EditorGUILayout.EnumFlagsField("Randomize Rotation", config.AxisToRandomize);
					config.Force2DMode = EditorGUILayout.Toggle("Force 2D Mode", config.Force2DMode);
				}
			}

			void DrawAvoidance()
			{
				DrawGroup(ref avoidanceFoldoutState, "Instance Avoidance", DrawContent);
				return;

				void DrawContent()
				{
					PrefabPainterConfig config = PrefabPainterTool.Config;
					List<GameObject> list = config.PrefabsToAvoid;
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
			}

			void DrawInvoke()
			{
				DrawGroup(ref invokeFoldoutState, "Invoke On Paint", DrawContent);
				return;

				void DrawContent()
				{
					PrefabPainterConfig config = PrefabPainterTool.Config;
					config.InvokeOnPaint = EditorGUILayout.Toggle("Use Invoke on Paint", config.InvokeOnPaint);
					EditorGUI.BeginDisabledGroup(!config.InvokeOnPaint);
					config.InvokeOnPaintMessage = EditorGUILayout.TextField("Invoke Message Name", config.InvokeOnPaintMessage);
					EditorGUI.EndDisabledGroup();
				}
			}

			void DrawGroup(ref bool foldoutValue, string foldoutName, Action drawContent)
			{
				EditorGUILayout.BeginHorizontal();
				foldoutValue = EditorGUILayout.BeginFoldoutHeaderGroup(foldoutValue, foldoutName);
				EditorGUILayout.EndHorizontal();

				if (foldoutValue)
				{
					drawContent.Invoke();
				}

				EditorGUILayout.EndFoldoutHeaderGroup();
				EditorGUILayout.Space();
			}
		}

		private void SaveCurrentBrush()
		{
			string path = EditorUtility.SaveFilePanelInProject("Save Prefab Brush", "Prefab Brush.asset", "asset", "Select a location to save your Prefab Painter Brush", lastSavePath);
			if (path.Length <= 0)
			{
				return;
			}

			PrefabPainterConfigAsset asset = CreateInstance<PrefabPainterConfigAsset>();
			asset.Config.LoadConfig(PrefabPainterTool.Config);
			AssetDatabase.CreateAsset(asset, path);
			AssetDatabase.SaveAssets();
			lastSavePath = path;
			RefreshBrushList();
		}

		private static void LoadBrush(PrefabPainterConfig config)
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