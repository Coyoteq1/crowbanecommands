using VampireCommandFramework;

namespace CrowbaneCommands.Extensions
{
    public static class ChatCommandExtensions
    {
        /// <summary>
        /// Reply with success color formatting
        /// </summary>
        public static void ReplySuccess(this ChatCommandContext ctx, string message)
        {
            var formattedMessage = Core.CommandConfig.FormatMessage(message, "success");
            ctx.Reply(formattedMessage);
        }

        /// <summary>
        /// Reply with error color formatting
        /// </summary>
        public static void ReplyError(this ChatCommandContext ctx, string message)
        {
            var formattedMessage = Core.CommandConfig.FormatMessage(message, "error");
            ctx.Reply(formattedMessage);
        }

        /// <summary>
        /// Reply with warning color formatting
        /// </summary>
        public static void ReplyWarning(this ChatCommandContext ctx, string message)
        {
            var formattedMessage = Core.CommandConfig.FormatMessage(message, "warning");
            ctx.Reply(formattedMessage);
        }

        /// <summary>
        /// Reply with info color formatting
        /// </summary>
        public static void ReplyInfo(this ChatCommandContext ctx, string message)
        {
            var formattedMessage = Core.CommandConfig.FormatMessage(message, "info");
            ctx.Reply(formattedMessage);
        }

        /// <summary>
        /// Reply with highlight color formatting
        /// </summary>
        public static void ReplyHighlight(this ChatCommandContext ctx, string message)
        {
            var formattedMessage = Core.CommandConfig.FormatMessage(message, "highlight");
            ctx.Reply(formattedMessage);
        }

        /// <summary>
        /// Reply with secondary color formatting
        /// </summary>
        public static void ReplySecondary(this ChatCommandContext ctx, string message)
        {
            var formattedMessage = Core.CommandConfig.FormatMessage(message, "secondary");
            ctx.Reply(formattedMessage);
        }

        /// <summary>
        /// Reply with accent color formatting
        /// </summary>
        public static void ReplyAccent(this ChatCommandContext ctx, string message)
        {
            var formattedMessage = Core.CommandConfig.FormatMessage(message, "accent");
            ctx.Reply(formattedMessage);
        }

        /// <summary>
        /// Reply with custom color formatting
        /// </summary>
        public static void ReplyColored(this ChatCommandContext ctx, string message, string color)
        {
            var formattedMessage = $"<color={color}>{message}</color>";
            ctx.Reply(formattedMessage);
        }

        /// <summary>
        /// Reply with multiple color segments
        /// </summary>
        public static void ReplyMultiColor(this ChatCommandContext ctx, params (string text, string colorType)[] segments)
        {
            var message = "";
            foreach (var (text, colorType) in segments)
            {
                message += Core.CommandConfig.FormatMessage(text, colorType);
            }
            ctx.Reply(message);
        }
    }
}