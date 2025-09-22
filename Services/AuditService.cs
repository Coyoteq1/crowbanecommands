using System;
using System.Collections.Concurrent;
using System.IO;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using HarmonyLib;
using ProjectM.Network;
using Stunlock.Core;
using Unity.Mathematics;
using VampireCommandFramework;
using Unity.Entities;

namespace CrowbaneCommands.Services;

class AuditMiddleware : CommandMiddleware
{
	public override void BeforeExecute(ICommandContext ctx, CommandAttribute attribute, MethodInfo method)
	{
		var chatCommandContext = (ChatCommandContext)ctx;
		var commandName = method.DeclaringType.Assembly.GetName().Name;

		if (method.DeclaringType.IsDefined(typeof(CommandGroupAttribute)))
		{
			var groupAttribute = (CommandGroupAttribute)Attribute.GetCustomAttribute(method.DeclaringType, typeof(CommandGroupAttribute), false);
			commandName += "." + groupAttribute.Name;
		}
		commandName += "." + attribute.Name;

		// Assuming you have a Core class that holds service instances
		// Core.AuditService.LogCommandUsage(chatCommandContext.Event.User, commandName);
	}
}

internal class AuditService : IDisposable
{
	private bool _disposed = false;
	private readonly string _logFilePath;
	private readonly object _lockObject = new object();

	public AuditService()
	{
		_logFilePath = Path.Combine(BepInEx.Paths.ConfigPath, "audit.log");

		var canCommandExecuteMethod = AccessTools.Method(typeof(CommandRegistry), "CanCommandExecute");
		var postfix = new HarmonyMethod(typeof(AuditService), nameof(CanCommandExecutePostfix));

		// This line will now work correctly
		Plugin.Harmony.Patch(canCommandExecuteMethod, postfix: postfix);
	}

	public void Dispose()
	{
		Dispose(true);
		GC.SuppressFinalize(this);
	}

	protected virtual void Dispose(bool disposing)
	{
		if (!_disposed)
		{
			if (disposing)
			{
				// Dispose managed resources here
			}
			_disposed = true;
		}
	}

	private void WriteLog(string eventType, string details)
	{
		try
		{
			lock (_lockObject)
			{
				var logEntry = $"[{DateTime.UtcNow:yyyy-MM-dd HH:mm:ss UTC}] {eventType}: {details}{Environment.NewLine}";
				File.AppendAllText(_logFilePath, logEntry);
			}
		}
		catch (Exception ex)
		{
			Plugin.PluginLog.LogError($"Failed to write audit log: {ex.Message}");
		}
	}

	public static void CanCommandExecutePostfix()
	{
		// This postfix method is called after command execution permission checks
		// Additional auditing logic can be added here if needed
	}

	public void LogDestroy(object user, object what, object where, object prefabGuid, object position, object amount)
	{
		var details = $"User: {user}, What: {what}, Where: {where}, PrefabGuid: {prefabGuid}, Position: {position}, Amount: {amount}";
		WriteLog("DESTROY", details);
	}

	public void LogGive(object user, object prefabGuid, object amount)
	{
		var details = $"User: {user}, PrefabGuid: {prefabGuid}, Amount: {amount}";
		WriteLog("GIVE", details);
	}

	public void LogBecomeObserver(object user, object mode)
	{
		var details = $"User: {user}, Mode: {mode}";
		WriteLog("BECOME_OBSERVER", details);
	}

	public void LogCastleHeartAdmin(object user, object eventType, object castleHeart, object userIndex)
	{
		var details = $"User: {user}, EventType: {eventType}, CastleHeart: {castleHeart}, UserIndex: {userIndex}";
		WriteLog("CASTLE_HEART_ADMIN", details);
	}

	public void LogChatMessage(object sender, object message, object channel, object timestamp)
	{
		var details = $"Sender: {sender}, Message: {message}, Channel: {channel}, Timestamp: {timestamp}";
		WriteLog("CHAT_MESSAGE", details);
	}

	public void LogMapTeleport(object user, object fromPosition, object toPosition)
	{
		var details = $"User: {user}, From: {fromPosition}, To: {toPosition}";
		WriteLog("MAP_TELEPORT", details);
	}

	public void LogTeleport(object user, object fromPosition, object toPosition, object target, object method)
	{
		var details = $"User: {user}, From: {fromPosition}, To: {toPosition}, Target: {target}, Method: {method}";
		WriteLog("TELEPORT", details);
	}

	public void LogCommandUsage(object user, string commandName)
	{
		var details = $"User: {user}, Command: {commandName}";
		WriteLog("COMMAND_USAGE", details);
	}
}
