
package com.tutorial.tutorial.listeners;

import org.bukkit.ChatColor;
import org.bukkit.Sound;
import org.bukkit.entity.Player;
import org.bukkit.event.EventHandler;
import org.bukkit.event.Listener;
import org.bukkit.event.block.Action;
import org.bukkit.event.player.PlayerInteractEvent;
import org.bukkit.inventory.ItemStack;
import org.bukkit.inventory.meta.ItemMeta;
import org.bukkit.potion.PotionEffect;
import org.bukkit.potion.PotionEffectType;

public class CoinListener implements Listener {

    @EventHandler
    public void onPlayerInteract(PlayerInteractEvent event) {
        // Check if the player right-clicked the air or a block
        if (event.getAction() == Action.RIGHT_CLICK_AIR || event.getAction() == Action.RIGHT_CLICK_BLOCK) {
            Player player = event.getPlayer();
            ItemStack itemInHand = player.getInventory().getItemInMainHand();

            // Check if the player is holding an item and it has metadata
            if (itemInHand != null && itemInHand.hasItemMeta()) {
                ItemMeta meta = itemInHand.getItemMeta();

                // Check if the item has CustomModelData and if it matches our coin's ID
                if (meta.hasCustomModelData() && meta.getCustomModelData() == 916496) {

                    // The player is holding our Crowbane Coin! Let's add the effects.
                    player.sendMessage(ChatColor.GREEN + "You feel a surge of crow-like agility!");

                    // Give Speed I for 10 seconds (20 ticks per second)
                    player.addPotionEffect(new PotionEffect(PotionEffectType.SPEED, 200, 0));

                    // Give Jump Boost II for 10 seconds
                    player.addPotionEffect(new PotionEffect(PotionEffectType.JUMP, 200, 1));

                    // Play a sound effect for the player
                    player.playSound(player.getLocation(), Sound.ENTITY_EXPERIENCE_ORB_PICKUP, 1.0f, 1.0f);

                    // Optional: Consume the coin on use
                    // itemInHand.setAmount(itemInHand.getAmount() - 1);
                }
            }
        }
    }
}
