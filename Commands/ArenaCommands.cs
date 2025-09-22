using VampireCommandFramework;

namespace CrowbaneCommands.Commands;

[CommandGroup("arena", "arn")]
public static class ArenaCommands
{
    private const string ComingSoonMessage = "Arena feature coming soon.";

    private static void Notify(ChatCommandContext ctx) => ctx.Reply(ComingSoonMessage);

    [Command("set", "set", "Configure arena parameters.", adminOnly: true)]
    public static void Set(ChatCommandContext ctx) => Notify(ctx);

    [Command("start", "st", "Start an arena match.", adminOnly: true)]
    public static void Start(ChatCommandContext ctx) => Notify(ctx);

    [Command("stop", "sp", "Stop the current arena match.", adminOnly: true)]
    public static void Stop(ChatCommandContext ctx) => Notify(ctx);

    [Command("reset", "rs", "Reset arena state.", adminOnly: true)]
    public static void Reset(ChatCommandContext ctx) => Notify(ctx);

    [Command("status", "stat", "View arena status.", adminOnly: true)]
    public static void Status(ChatCommandContext ctx) => Notify(ctx);

    [Command("stats", "sts", "Show arena statistics.", adminOnly: true)]
    public static void Stats(ChatCommandContext ctx) => Notify(ctx);

    [Command("teamadd", "ta", "Add a player to an arena team.", adminOnly: true)]
    public static void TeamAdd(ChatCommandContext ctx) => Notify(ctx);

    [Command("teamremove", "tr", "Remove a player from an arena team.", adminOnly: true)]
    public static void TeamRemove(ChatCommandContext ctx) => Notify(ctx);

    [Command("teamset", "ts", "Assign arena team composition.", adminOnly: true)]
    public static void TeamSet(ChatCommandContext ctx) => Notify(ctx);

    [Command("invite", "inv", "Invite a player to the arena.", adminOnly: true)]
    public static void Invite(ChatCommandContext ctx) => Notify(ctx);

    [Command("accept", "acc", "Accept an arena invitation.", adminOnly: true)]
    public static void Accept(ChatCommandContext ctx) => Notify(ctx);

    [Command("decline", "dec", "Decline an arena invitation.", adminOnly: true)]
    public static void Decline(ChatCommandContext ctx) => Notify(ctx);

    [Command("join", "jn", "Force a player to join the arena.", adminOnly: true)]
    public static void Join(ChatCommandContext ctx) => Notify(ctx);

    [Command("leave", "lv", "Remove a player from the arena queue.", adminOnly: true)]
    public static void Leave(ChatCommandContext ctx) => Notify(ctx);

    [Command("ready", "rd", "Mark arena as ready.", adminOnly: true)]
    public static void Ready(ChatCommandContext ctx) => Notify(ctx);

    [Command("unready", "urd", "Mark arena as not ready.", adminOnly: true)]
    public static void Unready(ChatCommandContext ctx) => Notify(ctx);

    [Command("spectate", "spc", "Toggle arena spectate mode.", adminOnly: true)]
    public static void Spectate(ChatCommandContext ctx) => Notify(ctx);

    [Command("config", "cfg", "Open arena configuration menu.", adminOnly: true)]
    public static void Config(ChatCommandContext ctx) => Notify(ctx);

    [Command("mode", "md", "Change arena mode.", adminOnly: true)]
    public static void Mode(ChatCommandContext ctx) => Notify(ctx);

    [Command("reward", "rwd", "Set arena rewards.", adminOnly: true)]
    public static void Reward(ChatCommandContext ctx) => Notify(ctx);
}
