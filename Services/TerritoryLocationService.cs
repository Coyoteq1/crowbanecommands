using System.Collections.Generic;
using ProjectM.CastleBuilding;
using Unity.Mathematics;

namespace CrowbaneCommands.Services;
internal class TerritoryLocationService
{
	Dictionary<int, float2> territoryCenters = [];

	public TerritoryLocationService()
	{
		var entities = Helper.GetEntitiesByComponentType<CastleTerritory>(true);
		foreach (var castleTerritory in entities)
		{
			var castleTerritoryIndex = castleTerritory.Read<CastleTerritory>().CastleTerritoryIndex;
			var ctb = Core.EntityManager.GetBuffer<CastleTerritoryBlocks>(castleTerritory);

			int2 blockSum = 0;
			for (int i = 0; i < ctb.Length; i++)
			{
				blockSum += ctb[i].BlockCoordinate;
			}

			territoryCenters[castleTerritoryIndex] = new float2((10f * blockSum.x / ctb.Length - 6400) / 2f,
																(10f * blockSum.y / ctb.Length - 6400) / 2f);
		}
		entities.Dispose();
		territoryCenters[86] = new float2(-1482, -700);
	}

	public float2 GetTerritoryCenter(int territoryIndex)
	{
		if (territoryCenters.TryGetValue(territoryIndex, out var center))
			return center;
		return float2.zero;
	}
}

