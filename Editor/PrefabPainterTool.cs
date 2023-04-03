using System.Collections.Generic;
using System.Reflection;
using UnityEditor;
using UnityEditor.EditorTools;
using UnityEditor.ShortcutManagement;
using UnityEngine;

namespace LMirman.PrefabPainter
{
	[EditorTool("Prefab Painter")]
	public class PrefabPainterTool : EditorTool
	{
		/// <summary>
		/// The minimum distance to consider actual movement from last paint step.
		/// </summary>
		private const float PaintStepSize = 0.1f;
		private const string ConfigPrefsKey = "prefab_painter_config";

		public static PrefabPainterConfig Config = new PrefabPainterConfig();

		[SerializeField]
		private Texture2D toolIcon;
		private GUIContent iconContent;

		private PaintState paintState = PaintState.Inactive;
		private Vector3 lastPaintCenter;
		private float distanceToNextPaint;
		private float paintBudget;
		private readonly List<GameObject> paintObjects = new List<GameObject>();
		private readonly List<GameObject> avoidObjects = new List<GameObject>();

		public override GUIContent toolbarIcon => iconContent;

		[Shortcut("Activate Platform Tool", KeyCode.Slash)]
		private static void SetToolActive()
		{
			ToolManager.SetActiveTool<PrefabPainterTool>();
		}

		private void OnEnable()
		{
			SaveConfigToPrefs();
			iconContent = new GUIContent()
			{
				image = toolIcon,
				text = "Prefab Painter",
				tooltip = "Paint prefabs into the scene view"
			};
		}

		private void OnDisable()
		{
			LoadConfigFromPrefs();
		}

		public override void OnActivated()
		{
			SceneView.beforeSceneGui += SceneViewOnBeforeSceneGui;
		}

		public override void OnWillBeDeactivated()
		{
			SceneView.beforeSceneGui -= SceneViewOnBeforeSceneGui;
		}

		private void SceneViewOnBeforeSceneGui(SceneView obj)
		{
			if (!ToolManager.IsActiveTool(this))
			{
				StopPainting();
				return;
			}

			Event evt = Event.current;
			if ((evt.type == EventType.MouseUp && evt.button == 0) || evt.type == EventType.MouseLeaveWindow)
			{
				StopPainting();
				evt.Use();
			}
			else if (evt.type == EventType.MouseDown && evt.button == 0)
			{
				StartPainting();
				evt.Use();
			}
		}

		public override void OnToolGUI(EditorWindow window)
		{
			if (!(window is SceneView sceneView) || !ToolManager.IsActiveTool(this))
			{
				return;
			}

			distanceToNextPaint = Config.DistancePerPaint;
			bool eraseMode = Event.current.shift;
			Handles.color = paintState == PaintState.Inactive ? Color.gray : eraseMode ? Color.red : Color.green;
			GetToolValues(sceneView, out Vector3 currentCenter, out Vector3 toolNormal);
			Handles.DrawWireDisc(currentCenter, toolNormal, Config.Radius, Config.Density);

			if (paintState == PaintState.Starting)
			{
				lastPaintCenter = currentCenter;
				paintState = PaintState.Painting;
				RequestPaint(currentCenter, toolNormal, eraseMode, GetIsIn2DMode(sceneView));
			}
			else if (paintState == PaintState.Painting)
			{
				Vector3 currentOffset = currentCenter - lastPaintCenter;
				int numberOfSteps = Mathf.FloorToInt(currentOffset.magnitude / PaintStepSize);
				for (int i = 0; i < numberOfSteps; i++)
				{
					lastPaintCenter += currentOffset.normalized * PaintStepSize;
					paintBudget += PaintStepSize;

					while (paintBudget >= distanceToNextPaint)
					{
						RequestPaint(lastPaintCenter, toolNormal, eraseMode, GetIsIn2DMode(sceneView));
						paintBudget -= distanceToNextPaint;
					}
				}
			}

			sceneView.Repaint();
		}

		private void RequestPaint(Vector3 toolPosition, Vector3 toolNormal, bool eraseMode, bool is2DMode)
		{
			if (!Config.CanPaint)
			{
				return;
			}

			if (eraseMode)
			{
				List<GameObject> objectsToErase = new List<GameObject>();
				foreach (GameObject paintObject in paintObjects)
				{
					if (Vector3.Distance(toolPosition, paintObject.transform.position) < Config.Radius + 0.5f)
					{
						objectsToErase.Add(paintObject);
					}
				}

				for (int i = objectsToErase.Count - 1; i >= 0; i--)
				{
					GameObject eraseObject = objectsToErase[i];
					paintObjects.Remove(eraseObject);
					Undo.DestroyObjectImmediate(eraseObject);
				}
			}
			else
			{
				Vector3 right = Vector3.Cross(toolNormal, Vector3.forward).normalized;
				Vector3 forward = Vector3.Cross(right, toolNormal).normalized;
				Quaternion baseRotation = forward.magnitude > 0 ? Quaternion.LookRotation(forward, toolNormal) : Quaternion.identity;
				baseRotation *= GetRandomizedAxis();
				Vector2 randOffset = Random.Range(-Config.Radius, Config.Radius) * new Vector2(Random.Range(-1f, 1f), Random.Range(-1f, 1f)).normalized;
				Vector3 position = toolPosition + (baseRotation * Vector3.right * randOffset.x) + (baseRotation * (is2DMode ? Vector3.up : Vector3.forward) * randOffset.y);
				Quaternion rotation = baseRotation;

				if (IsPositionDistant(paintObjects, position, Config.AvoidanceRange) && IsPositionDistant(avoidObjects, position, Config.AvoidanceRange))
				{
					GameObject paintObject = PaintObject(Config.PrefabToPaint, position, rotation);
					paintObjects.Add(paintObject);
					if (Config.InvokeOnPaint)
					{
						try
						{
							MonoBehaviour[] behaviours = paintObject.GetComponents<MonoBehaviour>();
							foreach (MonoBehaviour monoBehaviour in behaviours)
							{
								MethodInfo targetMethod = monoBehaviour.GetType().GetMethod(Config.InvokeOnPaintMessage);
								if (targetMethod != null)
								{
									targetMethod.Invoke(monoBehaviour, null);
								}
							}
						}
						catch
						{
							Debug.LogError("An internal error occurred while invoking on paint.");
						}
					}
				}
			}
		}

		public static void SaveConfigToPrefs()
		{
			try
			{
				if (EditorPrefs.HasKey(ConfigPrefsKey))
				{
					string configJson = EditorPrefs.GetString(ConfigPrefsKey);
					Config = PrefabPainterConfig.FromJson(configJson);
				}
				else
				{
					Config = new PrefabPainterConfig();
				}
			}
			catch
			{
				Debug.LogWarning("Unable to load prefab painter config. Reverting to default config");
				Config = new PrefabPainterConfig();
			}
		}

		public static void LoadConfigFromPrefs()
		{
			EditorPrefs.SetString(ConfigPrefsKey, PrefabPainterConfig.ToJson(Config));
		}

		private static bool GetIsIn2DMode(SceneView sceneView)
		{
			return sceneView.in2DMode || Config.Force2DMode;
		}

		private static Quaternion GetRandomizedAxis()
		{
			float x = Config.AxisToRandomize.HasFlag(PrefabPainterConfig.Axis.X) ? Random.Range(-180f, 180f) : 0;
			float y = Config.AxisToRandomize.HasFlag(PrefabPainterConfig.Axis.Y) ? Random.Range(-180f, 180f) : 0;
			float z = Config.AxisToRandomize.HasFlag(PrefabPainterConfig.Axis.Z) ? Random.Range(-180f, 180f) : 0;
			return Quaternion.Euler(x, y, z);
		}

		private static GameObject PaintObject(GameObject prefabToPaint, Vector3 position, Quaternion rotation)
		{
			GameObject gameObject;
			if (PrefabUtility.IsPartOfAnyPrefab(prefabToPaint))
			{
				string prefabPath = PrefabUtility.GetPrefabAssetPathOfNearestInstanceRoot(prefabToPaint);
				GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
				gameObject = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
			}
			else
			{
				gameObject = Instantiate(prefabToPaint);
			}

			gameObject.transform.position = position;
			gameObject.transform.rotation = rotation;
			Undo.RegisterCreatedObjectUndo(gameObject, "Paint Prefab Object");
			return gameObject;
		}

		private static bool IsPositionDistant(List<GameObject> gameObjects, Vector3 position, float tolerance)
		{
			if (tolerance <= 0)
			{
				return true;
			}

			foreach (GameObject gameObject in gameObjects)
			{
				if (Vector3.Distance(gameObject.transform.position, position) < tolerance)
				{
					return false;
				}
			}

			return true;
		}

		private void StartPainting()
		{
			GetObjectLists();
			paintState = PaintState.Starting;
			lastPaintCenter = Vector3.zero;
			distanceToNextPaint = 0;
		}

		private void StopPainting()
		{
			paintObjects.Clear();
			avoidObjects.Clear();
			paintState = PaintState.Inactive;
			lastPaintCenter = Vector3.zero;
			distanceToNextPaint = 0;
		}

		private void GetObjectLists()
		{
			if (!Config.CanPaint || !PrefabUtility.IsPartOfAnyPrefab(Config.PrefabToPaint))
			{
				return;
			}

			paintObjects.Clear();
			avoidObjects.Clear();
			// The following method is significantly better but is not available in 2020.3 :(
			// https://docs.unity3d.com/ScriptReference/PrefabUtility.FindAllInstancesOfPrefab.html
			GameObject[] gameObjects = FindObjectsOfType<GameObject>();
			foreach (GameObject gameObject in gameObjects)
			{
				bool isOutermostPrefabInstanceRoot = PrefabUtility.IsOutermostPrefabInstanceRoot(gameObject);
				bool isPartOfPrefab = PrefabUtility.IsPartOfAnyPrefab(gameObject);
				GameObject correspondingFromSource = PrefabUtility.GetCorrespondingObjectFromSource(gameObject);
				if (isOutermostPrefabInstanceRoot && Config.PrefabToPaint == correspondingFromSource && !paintObjects.Contains(gameObject))
				{
					paintObjects.Add(gameObject);
				}

				if (isOutermostPrefabInstanceRoot && Config.PrefabsToAvoid.Contains(correspondingFromSource) && !avoidObjects.Contains(gameObject))
				{
					avoidObjects.Add(gameObject);
				}
				else if (!isPartOfPrefab && Config.PrefabsToAvoid.Contains(gameObject) && !avoidObjects.Contains(gameObject))
				{
					avoidObjects.Add(gameObject);
				}
			}
		}

		private static void GetToolValues(SceneView sceneView, out Vector3 position, out Vector3 normal)
		{
			Vector3 mousePosition = Event.current.mousePosition;
			bool didRaycast = HandleUtility.PlaceObject(mousePosition, out Vector3 raycastPosition, out Vector3 raycastNormal);
			Vector3 fallbackPosition = HandleUtility.GUIPointToWorldRay(mousePosition).GetPoint(10);
			position = didRaycast ? raycastPosition : fallbackPosition;
			normal = didRaycast ? raycastNormal : Vector3.up;
			if (GetIsIn2DMode(sceneView))
			{
				position.z = 0;
				normal = Vector3.forward;
			}
		}

		private enum PaintState
		{
			Inactive, Starting, Painting
		}
	}
}