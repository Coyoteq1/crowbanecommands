using System;
using HarmonyLib;
using ProjectM;
using ProjectM.Network;
using Unity.Collections;

namespace CrowbaneCommands.Patches;

[HarmonyBefore("gg.deca.VampireCommandFramework")]
[HarmonyPatch(typeof(ChatMessageSystem), nameof(ChatMessageSystem.OnUpdate))]
internal class ChatMessageSystemPatch
{
    public static void Prefix(ChatMessageSystem __instance)
    {
        var entities = __instance.__query_661171423_0.ToEntityArray(Allocator.Temp);
        try
        {
            foreach (var entity in entities)
            {
                var fromData = entity.Read<FromCharacter>();
                var userData = fromData.User.Read<User>();
                var chatEventData = entity.Read<ChatMessageEvent>();
                var messageText = chatEventData.MessageText.ToString();

                if (!string.IsNullOrEmpty(messageText))
                {
                    var configuredPrefix = Core.CommandConfig?.Config?.General.CommandPrefix ?? ".";
                    if (!string.IsNullOrEmpty(configuredPrefix) && messageText.StartsWith(configuredPrefix, StringComparison.Ordinal))
                    {
                        var body = messageText.Substring(configuredPrefix.Length);
                        var normalizedBody = Core.CommandConfig?.NormalizeIncomingCommand(body) ?? body.TrimStart();
                        var rebuilt = string.Concat(configuredPrefix, normalizedBody);
                        chatEventData.MessageText = new FixedString512Bytes(rebuilt);
                        entity.Write(chatEventData);
                        messageText = rebuilt;
                    }
                }

                User toUser = default;
                if (Core.Players.TryFindUserFromNetworkId(chatEventData.ReceiverEntity, out var toUserEntity))
                {
                    toUser = toUserEntity.Read<User>();
                }

                Core.AuditService.LogChatMessage(userData, toUser, chatEventData.MessageType, messageText);
            }
        }
        finally
        {
            entities.Dispose();
        }
    }
}

