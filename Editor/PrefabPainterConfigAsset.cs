using UnityEngine;

namespace LMirman.PrefabPainter
{
	public class PrefabPainterConfigAsset : ScriptableObject
	{
		[SerializeField, HideInInspector]
		private PrefabPainterConfig prefabPainterConfig = new PrefabPainterConfig();

		public PrefabPainterConfig Config => prefabPainterConfig;
	}
}