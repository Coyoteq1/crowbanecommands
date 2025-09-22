using HarmonyLib;
using ProjectM;
using ProjectM.Gameplay.Scripting;
using ProjectM.Scripting;
using Unity.Entities;

namespace CrowbaneCommands.Patches;

[HarmonyPatch(typeof(Script_SetFlyingHeightVision_Buff.Server), nameof(Script_SetFlyingHeightVision_Buff.Server.ApplyFlyHeightVision))]
internal static class BatVisionPatch
{
    private static void Postfix(ref ServerGameManager game, ref SelfServer self)
    {
        var entityManager = Core.EntityManager;
        var buffEntity = self.Id;

        if (!entityManager.Exists(buffEntity))
        {
            return;
        }

        if (!entityManager.HasComponent<EntityOwner>(buffEntity))
        {
            return;
        }

        var ownerEntity = entityManager.GetComponentData<EntityOwner>(buffEntity).Owner;
        if (!entityManager.Exists(ownerEntity) || !entityManager.HasComponent<PlayerCharacter>(ownerEntity))
        {
            return;
        }

        if (!Core.BoostedPlayerService.HasBatVision(ownerEntity))
        {
            return;
        }

        if (!entityManager.HasComponent<Script_SetFlyingHeightVision_Buff_DataShared>(buffEntity))
        {
            return;
        }

        var buffData = entityManager.GetComponentData<Script_SetFlyingHeightVision_Buff_DataShared>(buffEntity);
        buffData.Delay = float.MaxValue;
        entityManager.SetComponentData(buffEntity, buffData);
    }
}

