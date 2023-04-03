using System;
using System.Collections.Generic;
using UnityEngine;

namespace LMirman.PrefabPainter
{
	[Serializable]
	public class PrefabPainterConfig
	{
		public const float DensityMin = 0.1f;
		public const float DensityMax = 2f;
		public const float RadiusMin = 0f;
		public const float RadiusMax = 10f;

		[SerializeField]
		private GameObject prefabToPaint;
		[SerializeField]
		private float radius = 1;
		[SerializeField]
		private float density = 1;
		[SerializeField]
		private bool force2DMode;
		[SerializeField]
		private Axis axisToRandomize = Axis.None;
		[SerializeField]
		private bool useRadiusForAvoidanceRange = true;
		[SerializeField]
		private float avoidanceRange = 1;
		[SerializeField]
		private List<GameObject> prefabsToAvoid = new List<GameObject>();
		[SerializeField]
		private bool invokeOnPaint;
		[SerializeField]
		private string invokeOnPaintMessage = "OnPaint";

		public GameObject PrefabToPaint
		{
			get => prefabToPaint;
			set => prefabToPaint = value;
		}
		public float Radius
		{
			get => radius;
			set => radius = Mathf.Clamp(value, RadiusMin, RadiusMax);
		}
		public float Density
		{
			get => density;
			set => density = Mathf.Clamp(value, DensityMin, DensityMax);
		}
		public float DistancePerPaint
		{
			get => 1 / Density;
			set => Density = 1 / value;
		}
		public bool Force2DMode
		{
			get => force2DMode;
			set => force2DMode = value;
		}
		public Axis AxisToRandomize
		{
			get => axisToRandomize;
			set => axisToRandomize = value;
		}
		public bool UseRadiusForAvoidanceRange
		{
			get => useRadiusForAvoidanceRange;
			set => useRadiusForAvoidanceRange = value;
		}
		public float AvoidanceRange
		{
			get => useRadiusForAvoidanceRange ? radius : avoidanceRange;
			set => avoidanceRange = value;
		}
		public List<GameObject> PrefabsToAvoid
		{
			get => prefabsToAvoid;
			private set => prefabsToAvoid = value;
		}
		public bool InvokeOnPaint
		{
			get => invokeOnPaint;
			set => invokeOnPaint = value;
		}
		public string InvokeOnPaintMessage
		{
			get => invokeOnPaintMessage;
			set => invokeOnPaintMessage = value;
		}

		public bool CanPaint => prefabToPaint != null;

		public void LoadConfig(PrefabPainterConfig config)
		{
			PrefabToPaint = config.prefabToPaint;
			Radius = config.radius;
			Density = config.density;
			Force2DMode = config.force2DMode;
			AxisToRandomize = config.axisToRandomize;
			UseRadiusForAvoidanceRange = config.useRadiusForAvoidanceRange;
			AvoidanceRange = config.avoidanceRange;
			PrefabsToAvoid = new List<GameObject>(config.prefabsToAvoid);
			InvokeOnPaint = config.invokeOnPaint;
			InvokeOnPaintMessage = config.invokeOnPaintMessage;
		}

		public PrefabPainterConfig()
		{

		}

		public PrefabPainterConfig(PrefabPainterConfig config)
		{
			LoadConfig(config);
		}

		public PrefabPainterConfig DeepCopy()
		{
			return new PrefabPainterConfig(this);
		}

		public static string ToJson(PrefabPainterConfig config)
		{
			return JsonUtility.ToJson(config);
		}

		public static PrefabPainterConfig FromJson(string json)
		{
			return JsonUtility.FromJson<PrefabPainterConfig>(json);
		}

		[Flags]
		public enum Axis
		{
			X = 1,
			Y = 2,
			Z = 4,
			None = 0,
			All = X | Y | Z
		}
	}
}