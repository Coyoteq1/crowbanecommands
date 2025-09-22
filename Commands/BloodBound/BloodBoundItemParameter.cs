using Stunlock.Core;
using Unity.Entities;

namespace CrowbaneCommands.Commands.BloodBound;

public class BloodBoundItemParameter
{
	public BloodBoundItemParameter(PrefabGUID prefab, Entity entity, string name)
	{
		Prefab = prefab;
		Entity = entity;
		Name = name;
	}

	public Entity Entity { get; }

	public string Name { get; }

	public PrefabGUID Prefab { get; }
}

