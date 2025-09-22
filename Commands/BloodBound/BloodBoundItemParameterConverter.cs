using CrowbaneCommands.Commands.Converters;
using VampireCommandFramework;

namespace CrowbaneCommands.Commands.BloodBound;

/// <summary>
/// Converts user inputs to <see cref="BloodBoundItemParameter"/>>.
/// </summary>
public class BloodBoundItemParameterConverter : CommandArgumentConverter<BloodBoundItemParameter>
{
	/// <summary>
	/// Base converter.
	/// </summary>
	private readonly ItemParameterConverter _itemParameterConverter;

	public BloodBoundItemParameterConverter()
	{
		_itemParameterConverter = new ItemParameterConverter();
	}

	public override BloodBoundItemParameter Parse(ICommandContext ctx, string input)
	{
		var itemParameter = _itemParameterConverter.Parse(ctx, input);

		// verify there is an entity behind specified prefab id.
		if (!Core.PrefabCollectionSystem._PrefabGuidToEntityMap.TryGetValue(itemParameter.Value, out var entity))
		{
			throw ctx.Error($"{input} not found.");
		}

		var name = Core.PrefabCollectionSystem._PrefabLookupMap.GetName(itemParameter.Value);

		return new BloodBoundItemParameter(itemParameter.Value, entity, name);
	}
}

