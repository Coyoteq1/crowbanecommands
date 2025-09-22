
package com.tutorial.tutorial.commands;

import org.bukkit.ChatColor;
import org.bukkit.Material;
import org.bukkit.command.Command;
import org.bukkit.command.CommandExecutor;
import org.bukkit.command.CommandSender;
import org.bukkit.entity.Player;
import org.bukkit.inventory.ItemStack;
import org.bukkit.inventory.meta.ItemMeta;

import java.util.ArrayList;
import java.util.List;

public class GiveStickCommand implements CommandExecutor {

    @Override
    public boolean onCommand(CommandSender sender, Command command, String label, String[] args) {
        if (sender instanceof Player) {
            Player player = (Player) sender;

            // Create a custom coin based on a gold nugget
            ItemStack crowbaneCoin = new ItemStack(Material.GOLD_NUGGET);
            ItemMeta meta = crowbaneCoin.getItemMeta();

            // Set the custom name
            meta.setDisplayName(ChatColor.DARK_GRAY + "Crowbane Coin");

            // Set custom lore
            List<String> lore = new ArrayList<>();
            lore.add(ChatColor.GRAY + "A strange coin with an eerie aura.");
            meta.setLore(lore);

            // Set the custom model data ID for the resource pack
            meta.setCustomModelData(916496);

            // Apply the custom metadata to the item
            crowbaneCoin.setItemMeta(meta);

            // Give the custom item to the player
            player.getInventory().addItem(crowbaneCoin);
            player.sendMessage(ChatColor.GREEN + "You have been given a Crowbane Coin!");
            return true;
        } else {
            sender.sendMessage("This command can only be run by a player.");
            return false;
        }
    }
}
