
package com.tutorial.tutorial;

import com.tutorial.tutorial.commands.GiveStickCommand;
import com.tutorial.tutorial.listeners.CoinListener;
import org.bukkit.plugin.java.JavaPlugin;

public final class Tutorial extends JavaPlugin {

    @Override
    public void onEnable() {
        // Plugin startup logic
        System.out.println("Tutorial plugin has started.");

        // Register the command executor
        getCommand("givestick").setExecutor(new GiveStickCommand());

        // Register the event listener
        getServer().getPluginManager().registerEvents(new CoinListener(), this);
    }

    @Override
    public void onDisable() {
        // Plugin shutdown logic
        System.out.println("Tutorial plugin has stopped.");
    }
}
