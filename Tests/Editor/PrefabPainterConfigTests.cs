using NUnit.Framework;

namespace LMirman.PrefabPainter.Tests
{
	public class PrefabPainterConfigTests
	{
		[Test]
		public void PrefabPainterConfig_ToFromJson_IsIdenticalRadius()
		{
			float expected = 5;
			PrefabPainterConfig stub = new PrefabPainterConfig() { Radius = expected };

			string json = PrefabPainterConfig.ToJson(stub);
			PrefabPainterConfig actual = PrefabPainterConfig.FromJson(json);

			Assert.AreEqual(actual.Radius, stub.Radius);
		}
	}
}
