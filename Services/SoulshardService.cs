using System.Collections.Generic;
using System.Linq;
using Il2CppInterop.Runtime;
using CrowbaneCommands.Data;
using ProjectM;
using ProjectM.Scripting;
using ProjectM.Shared;
using Stunlock.Core;
using Unity.Collections;
using Unity.Entities;

namespace CrowbaneCommands.Services;

internal class SoulshardService
{
    private readonly List<Entity> _droppedSoulshards = new();
    private readonly List<Entity> _spawnedSoulshards = new();

    private EntityQuery _relicDroppedQuery;
    private EntityQuery _soulshardAndPrefabsQuery;

    public bool IsPlentiful => Core.ServerGameSettingsSystem._Settings.RelicSpawnType == RelicSpawnType.Plentiful;

    public SoulshardService()
    {
        var relicDroppedQueryBuilder = new EntityQueryBuilder(Allocator.Temp)
            .AddAll(new(Il2CppType.Of<RelicDropped>(), ComponentType.AccessMode.ReadOnly))
            .WithOptions(EntityQueryOptions.IncludeSystems);

        _relicDroppedQuery = Core.EntityManager.CreateEntityQuery(ref relicDroppedQueryBuilder);
        relicDroppedQueryBuilder.Dispose();

        var soulshardAndPrefabsQueryBuilder = new EntityQueryBuilder(Allocator.Temp)
            .AddAll(new(Il2CppType.Of<ItemData>(), ComponentType.AccessMode.ReadOnly))
            .AddAll(new(Il2CppType.Of<Relic>(), ComponentType.AccessMode.ReadOnly))
            .WithOptions(EntityQueryOptions.IncludePrefab);
        _soulshardAndPrefabsQuery = Core.EntityManager.CreateEntityQuery(ref soulshardAndPrefabsQueryBuilder);
        soulshardAndPrefabsQueryBuilder.Dispose();

        foreach (var entity in Helper.GetEntitiesByComponentTypes<ItemData, Relic>())
        {
            if (entity.Has<ScriptSpawn>())
            {
                _spawnedSoulshards.Add(entity);
            }
            else
            {
                _droppedSoulshards.Add(entity);
            }
        }

        RefreshWillDrop();

        if (Core.ConfigSettings.ShardDurabilityTime.HasValue)
        {
            SetShardDurabilityInternal(Core.ConfigSettings.ShardDurabilityTime);
        }
    }

    private int GetShardDropLimit(RelicType relicType) => relicType switch
    {
        RelicType.TheMonster => Core.ConfigSettings.ShardMonsterDropLimit,
        RelicType.Solarus => Core.ConfigSettings.ShardSolarusDropLimit,
        RelicType.WingedHorror => Core.ConfigSettings.ShardWingedHorrorDropLimit,
        RelicType.Dracula => Core.ConfigSettings.ShardDraculaDropLimit,
        RelicType.Morgana => Core.ConfigSettings.ShardMorganaDropLimit,
        _ => 1
    };

    public void RefreshWillDrop()
    {
        if (IsPlentiful || !Core.ConfigSettings.ShardDropManagementEnabled)
        {
            return;
        }

        var relicDropped = GetRelicDroppedBuffer();
        for (var relicType = RelicType.TheMonster; relicType <= RelicType.Morgana; relicType++)
        {
            var droppedCount = _droppedSoulshards.Count(e => e.Read<Relic>().RelicType == relicType);
            var shouldDrop = droppedCount < GetShardDropLimit(relicType);
            var isCurrentlyBlocked = relicDropped[(int)relicType].Value;

            if (isCurrentlyBlocked == shouldDrop)
            {
                relicDropped[(int)relicType] = new RelicDropped { Value = !shouldDrop };
            }
        }
    }

    public void SetShardDropLimit(int limit, RelicType relicType)
    {
        switch (relicType)
        {
            case RelicType.TheMonster:
                Core.ConfigSettings.ShardMonsterDropLimit = limit;
                break;
            case RelicType.Solarus:
                Core.ConfigSettings.ShardSolarusDropLimit = limit;
                break;
            case RelicType.WingedHorror:
                Core.ConfigSettings.ShardWingedHorrorDropLimit = limit;
                break;
            case RelicType.Dracula:
                Core.ConfigSettings.ShardDraculaDropLimit = limit;
                break;
            case RelicType.Morgana:
                Core.ConfigSettings.ShardMorganaDropLimit = limit;
                break;
            case RelicType.None:
                Core.ConfigSettings.ShardMonsterDropLimit = limit;
                Core.ConfigSettings.ShardSolarusDropLimit = limit;
                Core.ConfigSettings.ShardWingedHorrorDropLimit = limit;
                Core.ConfigSettings.ShardDraculaDropLimit = limit;
                Core.ConfigSettings.ShardMorganaDropLimit = limit;
                break;
        }

        RefreshWillDrop();
    }

    public (bool willDrop, int droppedCount, int spawnedCount)[] GetSoulshardStatus()
    {
        var status = new (bool willDrop, int droppedCount, int spawnedCount)[6];
        var relicDropped = GetRelicDroppedBuffer();

        for (var relicType = RelicType.None; relicType <= RelicType.Morgana; relicType++)
        {
            var droppedCount = _droppedSoulshards.Count(e => e.Read<Relic>().RelicType == relicType);
            var spawnedCount = _spawnedSoulshards.Count(e => e.Read<Relic>().RelicType == relicType);
            var willDrop = !relicDropped[(int)relicType].Value;
            status[(int)relicType] = (willDrop, droppedCount, spawnedCount);
        }

        return status;
    }

    public void HandleSoulshardSpawn(Entity soulshardItemEntity)
    {
        if (!soulshardItemEntity.Has<InventoryItem>())
        {
            return;
        }

        var invItem = soulshardItemEntity.Read<InventoryItem>();
        var isSpawned = invItem.ContainerEntity == Entity.Null || invItem.ContainerEntity.Read<PrefabGUID>() == Prefabs.External_Inventory;

        if (isSpawned)
        {
            _spawnedSoulshards.Add(soulshardItemEntity);
            soulshardItemEntity.Add<ScriptSpawn>();
        }
        else
        {
            _droppedSoulshards.Add(soulshardItemEntity);
        }

        RefreshWillDrop();
    }

    public void HandleSoulshardDestroy(Entity soulshardItemEntity)
    {
        if (!_droppedSoulshards.Remove(soulshardItemEntity))
        {
            _spawnedSoulshards.Remove(soulshardItemEntity);
        }
    }

    public void SetShardDurabilityTime(int? durabilityTime)
    {
        Core.ConfigSettings.ShardDurabilityTime = durabilityTime;
        SetShardDurabilityInternal(durabilityTime);
    }

    public bool ToggleShardDropManagement()
    {
        Core.ConfigSettings.ShardDropManagementEnabled = !Core.ConfigSettings.ShardDropManagementEnabled;
        return Core.ConfigSettings.ShardDropManagementEnabled;
    }

    private DynamicBuffer<RelicDropped> GetRelicDroppedBuffer()
    {
        var entities = _relicDroppedQuery.ToEntityArray(Allocator.Temp);
        var buffer = Core.EntityManager.GetBuffer<RelicDropped>(entities[0]);
        entities.Dispose();
        return buffer;
    }

    private void SetShardDurabilityInternal(int? durabilityTime)
    {
        var entities = _soulshardAndPrefabsQuery.ToEntityArray(Allocator.Temp);
        foreach (var entity in entities)
        {
            if (!entity.Has<LoseDurabilityOverTime>())
            {
                continue;
            }

            var durabilityComponent = entity.Read<LoseDurabilityOverTime>();
            durabilityComponent.TimeUntilBroken = durabilityTime ?? 129600;
            entity.Write(durabilityComponent);
        }
        entities.Dispose();
    }
}
