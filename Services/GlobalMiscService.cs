using CrowbaneCommands.Data;
using ProjectM.Gameplay.Scripting;

namespace CrowbaneCommands.Services;
internal class GlobalMiscService
{
	float defaultBatVisionDelay = 0.3f;
	public GlobalMiscService()
	{
		if (Core.PrefabCollectionSystem._PrefabGuidToEntityMap.TryGetValue(Prefabs.AB_Shapeshift_Bat_TakeFlight_Buff, out var prefabEntity))
		{
			var data = prefabEntity.Read<Script_SetFlyingHeightVision_Buff_DataShared>();
			defaultBatVisionDelay = data.Delay;
		}
		
		if (Core.ConfigSettings.BatVision)
			SetBatVisionState(Core.ConfigSettings.BatVision);
	}

	public bool ToggleBatVision()
	{
		Core.ConfigSettings.BatVision = !Core.ConfigSettings.BatVision;
		SetBatVisionState(Core.ConfigSettings.BatVision);
		return Core.ConfigSettings.BatVision;
	}

    void SetBatVisionState(bool enabled)
    {
		if (!Core.PrefabCollectionSystem._PrefabGuidToEntityMap.TryGetValue(Prefabs.AB_Shapeshift_Bat_TakeFlight_Buff, out var prefabEntity))
			return;

		var data = prefabEntity.Read<Script_SetFlyingHeightVision_Buff_DataShared>();
		data.Delay = enabled ? float.MaxValue : defaultBatVisionDelay;
		prefabEntity.Write(data);
	}
}

