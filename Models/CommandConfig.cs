using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace CrowbaneCommands.Models
{
    public class CommandConfig
    {
        [JsonPropertyName("commands")]
        public Dictionary<string, CommandSettings> Commands { get; set; } = new();

        [JsonPropertyName("colors")]
        public ColorSettings Colors { get; set; } = new();

        [JsonPropertyName("general")]
        public GeneralSettings General { get; set; } = new();
    }

    public class CommandSettings
    {
        [JsonPropertyName("name")]
        public string Name { get; set; } = "";

        [JsonPropertyName("shorthand")]
        public string Shorthand { get; set; } = "";

        [JsonPropertyName("description")]
        public string Description { get; set; } = "";

        [JsonPropertyName("enabled")]
        public bool Enabled { get; set; } = true;

        [JsonPropertyName("adminOnly")]
        public bool AdminOnly { get; set; } = false;

        [JsonPropertyName("customColor")]
        public string CustomColor { get; set; } = "";

        [JsonPropertyName("category")]
        public string Category { get; set; } = "";

        [JsonPropertyName("aliases")]
        public List<string> Aliases { get; set; } = new();
    }

    public class ColorSettings
    {
        [JsonPropertyName("success")]
        public string Success { get; set; } = "#00ff00";

        [JsonPropertyName("error")]
        public string Error { get; set; } = "#ff0000";

        [JsonPropertyName("warning")]
        public string Warning { get; set; } = "#ffff00";

        [JsonPropertyName("info")]
        public string Info { get; set; } = "#0000ff";

        [JsonPropertyName("highlight")]
        public string Highlight { get; set; } = "#ffffff";

        [JsonPropertyName("secondary")]
        public string Secondary { get; set; } = "#cccccc";

        [JsonPropertyName("accent")]
        public string Accent { get; set; } = "#ff8800";
    }

    public class GeneralSettings
    {
        [JsonPropertyName("enableCustomColors")]
        public bool EnableCustomColors { get; set; } = true;

        [JsonPropertyName("enableCommandCustomization")]
        public bool EnableCommandCustomization { get; set; } = true;

        [JsonPropertyName("reloadConfigOnChange")]
        public bool ReloadConfigOnChange { get; set; } = true;

        [JsonPropertyName("commandPrefix")]
        public string CommandPrefix { get; set; } = ".";
    }
}




