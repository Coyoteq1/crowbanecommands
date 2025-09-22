using CrowbaneCommands.Attributes;
using CrowbaneCommands.Extensions;
using VampireCommandFramework;

namespace CrowbaneCommands.Commands
{
    [CommandGroup(name: "example", shortHand: "ex")]
    public static class ExampleConfigurableCommands
    {
        [ConfigurableCommand("example_hello", "hello", "hi", "Say hello with configurable colors", adminOnly: false)]
        [Command("hello", "hi", "Say hello with configurable colors", adminOnly: false)]
        public static void HelloCommand(ChatCommandContext ctx)
        {
            ctx.ReplySuccess("Hello there! This is a success message.");
            ctx.ReplyInfo("This is an info message.");
            ctx.ReplyWarning("This is a warning message.");
            ctx.ReplyError("This is an error message.");
            ctx.ReplyHighlight("This is a highlighted message.");
            ctx.ReplyAccent("This is an accent message.");
        }

        [ConfigurableCommand("example_multicolor", "rainbow", "rb", "Demonstrate multi-color messages", adminOnly: false)]
        [Command("rainbow", "rb", "Demonstrate multi-color messages", adminOnly: false)]
        public static void MultiColorCommand(ChatCommandContext ctx)
        {
            ctx.ReplyMultiColor(
                ("Welcome ", "success"),
                ("to the ", "info"),
                ("colorful ", "warning"),
                ("world ", "error"),
                ("of ", "highlight"),
                ("V Rising!", "accent")
            );
        }

        [ConfigurableCommand("example_custom", "custom", "c", "Use custom color", adminOnly: false)]
        [Command("custom", "c", "Use custom color", adminOnly: false)]
        public static void CustomColorCommand(ChatCommandContext ctx)
        {
            ctx.ReplyColored("This message uses a custom purple color!", "#9966ff");
        }

        [ConfigurableCommand("example_admin", "admintest", "at", "Admin only command example", adminOnly: true)]
        [Command("admintest", "at", "Admin only command example", adminOnly: true)]
        public static void AdminOnlyCommand(ChatCommandContext ctx)
        {
            ctx.ReplySuccess("You have admin privileges! This command worked.");
        }

        [ConfigurableCommand("example_status", "status", "st", "Show server status with colors", adminOnly: false)]
        [Command("status", "st", "Show server status with colors", adminOnly: false)]
        public static void StatusCommand(ChatCommandContext ctx)
        {
            var playerCount = 42; // Example data
            var maxPlayers = 100;
            var uptime = "2h 30m";

            ctx.ReplyMultiColor(
                ("Server Status:", "highlight"),
                ("\n• Players: ", "info"),
                ($"{playerCount}/{maxPlayers}", playerCount > maxPlayers * 0.8 ? "warning" : "success"),
                ("\n• Uptime: ", "info"),
                (uptime, "accent"),
                ("\n• Status: ", "info"),
                ("Online", "success")
            );
        }
    }
}
