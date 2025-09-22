using ProjectM.Network;
using Unity.Entities;

namespace CrowbaneCommands.Services;
internal class AdminService
{
	public static void AdminUser(Entity userEntity)
	{
		var user = userEntity.Read<User>();

		var archetype = Core.EntityManager.CreateArchetype(new ComponentType[]
		{
					ComponentType.ReadWrite<FromCharacter>(),
					ComponentType.ReadWrite<AdminAuthEvent>()
		});

		var entity = Core.EntityManager.CreateEntity(archetype);
		entity.Write(new FromCharacter()
		{
			Character = user.LocalCharacter.GetEntityOnServer(),
			User = userEntity
		});
	}
}

