using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEngine;

public static class MinecraftTextureAutoAssign
{
    public static class MinecraftTextureNames
    {
        public class Faces
        {
            public readonly string top,bottom,side,front,back,left,right;
            public Faces(string side,string top,string bottom,string front,string back,string left,string right)
            {this.side=side;this.top=top;this.bottom=bottom;this.front=front;this.back=back;this.left=left;this.right=right;}
            public string Get(string face)
            {
                switch(face){case "top":return top;case "bottom":return bottom;case "front":return front;case "back":return back;case "left":return left;case "right":return right;default:return side;}
            }
        }
        private static readonly Dictionary<BlockType,Faces> blocks=new Dictionary<BlockType,Faces>{
            {BlockType.Nothing,new Faces("","","","","","","")},
            {BlockType.Air,new Faces("","","","","","","")},
            {BlockType.Dirt,new Faces("dirt","dirt","dirt","dirt","dirt","dirt","dirt")},
            {BlockType.Grass,new Faces("grass_block_side|grass_side","grass_block_top|grass_top","dirt","grass_block_side|grass_side","grass_block_side|grass_side","grass_block_side|grass_side","grass_block_side|grass_side")},
            {BlockType.Stone,new Faces("stone","stone","stone","stone","stone","stone","stone")},
            {BlockType.Log,new Faces("oak_log|log_oak","oak_log_top|log_oak_top","oak_log_top|log_oak_top","oak_log|log_oak","oak_log|log_oak","oak_log|log_oak","oak_log|log_oak")},
            {BlockType.Leaves,new Faces("oak_leaves|leaves_oak","oak_leaves|leaves_oak","oak_leaves|leaves_oak","oak_leaves|leaves_oak","oak_leaves|leaves_oak","oak_leaves|leaves_oak","oak_leaves|leaves_oak")},
            {BlockType.Sand,new Faces("sand","sand","sand","sand","sand","sand","sand")},
            {BlockType.Water,new Faces("water_flow","water_still","water_still","water_flow","water_flow","water_flow","water_flow")},
            {BlockType.Glass,new Faces("glass","glass","glass","glass","glass","glass","glass")},
            {BlockType.GlowStone,new Faces("glowstone","glowstone","glowstone","glowstone","glowstone","glowstone","glowstone")},
            {BlockType.WoodenSlab,new Faces("oak_planks|planks_oak","oak_planks|planks_oak","oak_planks|planks_oak","oak_planks|planks_oak","oak_planks|planks_oak","oak_planks|planks_oak","oak_planks|planks_oak")},
            {BlockType.Bedrock,new Faces("bedrock","bedrock","bedrock","bedrock","bedrock","bedrock","bedrock")},
            {BlockType.Gravel,new Faces("gravel","gravel","gravel","gravel","gravel","gravel","gravel")},
            {BlockType.Sandstone,new Faces("sandstone|sandstone_normal","sandstone_top","sandstone_bottom","sandstone|sandstone_normal","sandstone|sandstone_normal","sandstone|sandstone_normal","sandstone|sandstone_normal")},
            {BlockType.Lava,new Faces("lava_flow","lava_still","lava_still","lava_flow","lava_flow","lava_flow","lava_flow")},
            {BlockType.Ice,new Faces("ice","ice","ice","ice","ice","ice","ice")},
            {BlockType.SpruceLog,new Faces("spruce_log|log_spruce","spruce_log_top|log_spruce_top","spruce_log_top|log_spruce_top","spruce_log|log_spruce","spruce_log|log_spruce","spruce_log|log_spruce","spruce_log|log_spruce")},
            {BlockType.SpruceLeaves,new Faces("spruce_leaves|leaves_spruce","spruce_leaves|leaves_spruce","spruce_leaves|leaves_spruce","spruce_leaves|leaves_spruce","spruce_leaves|leaves_spruce","spruce_leaves|leaves_spruce","spruce_leaves|leaves_spruce")},
            {BlockType.BirchLog,new Faces("birch_log|log_birch","birch_log_top|log_birch_top","birch_log_top|log_birch_top","birch_log|log_birch","birch_log|log_birch","birch_log|log_birch","birch_log|log_birch")},
            {BlockType.BirchLeaves,new Faces("birch_leaves|leaves_birch","birch_leaves|leaves_birch","birch_leaves|leaves_birch","birch_leaves|leaves_birch","birch_leaves|leaves_birch","birch_leaves|leaves_birch","birch_leaves|leaves_birch")},
            {BlockType.Cobblestone,new Faces("cobblestone","cobblestone","cobblestone","cobblestone","cobblestone","cobblestone","cobblestone")},
            {BlockType.WoodenPlanks,new Faces("oak_planks|planks_oak","oak_planks|planks_oak","oak_planks|planks_oak","oak_planks|planks_oak","oak_planks|planks_oak","oak_planks|planks_oak","oak_planks|planks_oak")},
            {BlockType.OakSapling,new Faces("oak_sapling|sapling_oak","oak_sapling|sapling_oak","oak_sapling|sapling_oak","oak_sapling|sapling_oak","oak_sapling|sapling_oak","oak_sapling|sapling_oak","oak_sapling|sapling_oak")},
            {BlockType.GoldOre,new Faces("gold_ore","gold_ore","gold_ore","gold_ore","gold_ore","gold_ore","gold_ore")},
            {BlockType.IronOre,new Faces("iron_ore","iron_ore","iron_ore","iron_ore","iron_ore","iron_ore","iron_ore")},
            {BlockType.CoalOre,new Faces("coal_ore","coal_ore","coal_ore","coal_ore","coal_ore","coal_ore","coal_ore")},
            {BlockType.Sponge,new Faces("sponge","sponge","sponge","sponge","sponge","sponge","sponge")},
            {BlockType.LapisOre,new Faces("lapis_ore","lapis_ore","lapis_ore","lapis_ore","lapis_ore","lapis_ore","lapis_ore")},
            {BlockType.LapisBlock,new Faces("lapis_block","lapis_block","lapis_block","lapis_block","lapis_block","lapis_block","lapis_block")},
            {BlockType.Dispenser,new Faces("furnace_side","furnace_top","furnace_top","dispenser_front|dispenser_front_horizontal","furnace_side","furnace_side","furnace_side")},
            {BlockType.NoteBlock,new Faces("note_block|noteblock","note_block|noteblock","note_block|noteblock","note_block|noteblock","note_block|noteblock","note_block|noteblock","note_block|noteblock")},
            {BlockType.Bed,new Faces("bed_feet_side|bed_foot_side","bed_feet_top|bed_foot_top","oak_planks|planks_oak","bed_feet_end|bed_foot_end","bed_feet_side|bed_foot_side","bed_feet_side|bed_foot_side","bed_feet_side|bed_foot_side")},
            {BlockType.PoweredRail,new Faces("powered_rail|rail_golden","powered_rail|rail_golden","powered_rail|rail_golden","powered_rail|rail_golden","powered_rail|rail_golden","powered_rail|rail_golden","powered_rail|rail_golden")},
            {BlockType.DetectorRail,new Faces("detector_rail|rail_detector","detector_rail|rail_detector","detector_rail|rail_detector","detector_rail|rail_detector","detector_rail|rail_detector","detector_rail|rail_detector","detector_rail|rail_detector")},
            {BlockType.StickyPiston,new Faces("piston_side","sticky_piston_top|piston_top_sticky","piston_bottom","piston_side","piston_side","piston_side","piston_side")},
            {BlockType.Cobweb,new Faces("cobweb|web","cobweb|web","cobweb|web","cobweb|web","cobweb|web","cobweb|web","cobweb|web")},
            {BlockType.TallGrass,new Faces("short_grass|tallgrass|grass","short_grass|tallgrass|grass","short_grass|tallgrass|grass","short_grass|tallgrass|grass","short_grass|tallgrass|grass","short_grass|tallgrass|grass","short_grass|tallgrass|grass")},
            {BlockType.DeadBush,new Faces("dead_bush|deadbush","dead_bush|deadbush","dead_bush|deadbush","dead_bush|deadbush","dead_bush|deadbush","dead_bush|deadbush","dead_bush|deadbush")},
            {BlockType.Piston,new Faces("piston_side","piston_top|piston_top_normal","piston_bottom","piston_side","piston_side","piston_side","piston_side")},
            {BlockType.PistonHead,new Faces("piston_side","piston_top|piston_top_normal","piston_bottom","piston_side","piston_side","piston_side","piston_side")},
            {BlockType.WhiteWool,new Faces("white_wool|wool_colored_white","white_wool|wool_colored_white","white_wool|wool_colored_white","white_wool|wool_colored_white","white_wool|wool_colored_white","white_wool|wool_colored_white","white_wool|wool_colored_white")},
            {BlockType.MovingPiston,new Faces("","","","","","","")},
            {BlockType.Dandelion,new Faces("dandelion|flower_dandelion","dandelion|flower_dandelion","dandelion|flower_dandelion","dandelion|flower_dandelion","dandelion|flower_dandelion","dandelion|flower_dandelion","dandelion|flower_dandelion")},
            {BlockType.Rose,new Faces("rose|poppy|flower_rose","rose|poppy|flower_rose","rose|poppy|flower_rose","rose|poppy|flower_rose","rose|poppy|flower_rose","rose|poppy|flower_rose","rose|poppy|flower_rose")},
            {BlockType.BrownMushroom,new Faces("brown_mushroom|mushroom_brown","brown_mushroom|mushroom_brown","brown_mushroom|mushroom_brown","brown_mushroom|mushroom_brown","brown_mushroom|mushroom_brown","brown_mushroom|mushroom_brown","brown_mushroom|mushroom_brown")},
            {BlockType.RedMushroom,new Faces("red_mushroom|mushroom_red","red_mushroom|mushroom_red","red_mushroom|mushroom_red","red_mushroom|mushroom_red","red_mushroom|mushroom_red","red_mushroom|mushroom_red","red_mushroom|mushroom_red")},
            {BlockType.GoldBlock,new Faces("gold_block","gold_block","gold_block","gold_block","gold_block","gold_block","gold_block")},
            {BlockType.IronBlock,new Faces("iron_block","iron_block","iron_block","iron_block","iron_block","iron_block","iron_block")},
            {BlockType.DoubleStoneSlab,new Faces("smooth_stone_slab_side|stone_slab_side","smooth_stone|stone_slab_top","smooth_stone|stone_slab_top","smooth_stone_slab_side|stone_slab_side","smooth_stone_slab_side|stone_slab_side","smooth_stone_slab_side|stone_slab_side","smooth_stone_slab_side|stone_slab_side")},
            {BlockType.StoneSlab,new Faces("smooth_stone_slab_side|stone_slab_side","smooth_stone|stone_slab_top","smooth_stone|stone_slab_top","smooth_stone_slab_side|stone_slab_side","smooth_stone_slab_side|stone_slab_side","smooth_stone_slab_side|stone_slab_side","smooth_stone_slab_side|stone_slab_side")},
            {BlockType.Bricks,new Faces("bricks|brick","bricks|brick","bricks|brick","bricks|brick","bricks|brick","bricks|brick","bricks|brick")},
            {BlockType.TNT,new Faces("tnt_side","tnt_top","tnt_bottom","tnt_side","tnt_side","tnt_side","tnt_side")},
            {BlockType.Bookshelf,new Faces("bookshelf","oak_planks|planks_oak","oak_planks|planks_oak","bookshelf","bookshelf","bookshelf","bookshelf")},
            {BlockType.MossyCobblestone,new Faces("mossy_cobblestone|cobblestone_mossy","mossy_cobblestone|cobblestone_mossy","mossy_cobblestone|cobblestone_mossy","mossy_cobblestone|cobblestone_mossy","mossy_cobblestone|cobblestone_mossy","mossy_cobblestone|cobblestone_mossy","mossy_cobblestone|cobblestone_mossy")},
            {BlockType.Obsidian,new Faces("obsidian","obsidian","obsidian","obsidian","obsidian","obsidian","obsidian")},
            {BlockType.Torch,new Faces("torch","torch","torch","torch","torch","torch","torch")},
            {BlockType.Fire,new Faces("fire_0|fire_layer_0","fire_0|fire_layer_0","fire_0|fire_layer_0","fire_0|fire_layer_0","fire_0|fire_layer_0","fire_0|fire_layer_0","fire_0|fire_layer_0")},
            {BlockType.MonsterSpawner,new Faces("spawner|mob_spawner","spawner|mob_spawner","spawner|mob_spawner","spawner|mob_spawner","spawner|mob_spawner","spawner|mob_spawner","spawner|mob_spawner")},
            {BlockType.WoodenStairs,new Faces("oak_planks|planks_oak","oak_planks|planks_oak","oak_planks|planks_oak","oak_planks|planks_oak","oak_planks|planks_oak","oak_planks|planks_oak","oak_planks|planks_oak")},
            {BlockType.Chest,new Faces("chest_side","chest_top","chest_top","chest_front","chest_side","chest_side","chest_side")},
            {BlockType.RedstoneWire,new Faces("redstone_dust_line|redstone_dust_line0","redstone_dust_cross","redstone_dust_cross","redstone_dust_line|redstone_dust_line0","redstone_dust_line|redstone_dust_line0","redstone_dust_line|redstone_dust_line0","redstone_dust_line|redstone_dust_line0")},
            {BlockType.DiamondOre,new Faces("diamond_ore","diamond_ore","diamond_ore","diamond_ore","diamond_ore","diamond_ore","diamond_ore")},
            {BlockType.DiamondBlock,new Faces("diamond_block","diamond_block","diamond_block","diamond_block","diamond_block","diamond_block","diamond_block")},
            {BlockType.CraftingTable,new Faces("crafting_table_side","crafting_table_top","oak_planks|planks_oak","crafting_table_front","crafting_table_side","crafting_table_front","crafting_table_side")},
            {BlockType.WheatCrop0,new Faces("wheat_stage0|wheat_stage_0","wheat_stage0|wheat_stage_0","wheat_stage0|wheat_stage_0","wheat_stage0|wheat_stage_0","wheat_stage0|wheat_stage_0","wheat_stage0|wheat_stage_0","wheat_stage0|wheat_stage_0")},
            {BlockType.Farmland,new Faces("dirt","farmland|farmland_dry","dirt","dirt","dirt","dirt","dirt")},
            {BlockType.Furnace,new Faces("furnace_side","furnace_top","furnace_top","furnace_front|furnace_front_off","furnace_side","furnace_side","furnace_side")},
            {BlockType.LitFurnace,new Faces("furnace_side","furnace_top","furnace_top","furnace_front_on","furnace_side","furnace_side","furnace_side")},
            {BlockType.StandingSign,new Faces("oak_planks|planks_oak","oak_planks|planks_oak","oak_planks|planks_oak","oak_planks|planks_oak","oak_planks|planks_oak","oak_planks|planks_oak","oak_planks|planks_oak")},
            {BlockType.WoodenDoor,new Faces("oak_door_bottom|door_wood_lower","oak_door_top|door_wood_upper","oak_door_bottom|door_wood_lower","oak_door_bottom|door_wood_lower","oak_door_bottom|door_wood_lower","oak_door_bottom|door_wood_lower","oak_door_bottom|door_wood_lower")},
            {BlockType.Ladder,new Faces("ladder","ladder","ladder","ladder","ladder","ladder","ladder")},
            {BlockType.Rail,new Faces("rail|rail_normal","rail|rail_normal","rail|rail_normal","rail|rail_normal","rail|rail_normal","rail|rail_normal","rail|rail_normal")},
            {BlockType.CobblestoneStairs,new Faces("cobblestone","cobblestone","cobblestone","cobblestone","cobblestone","cobblestone","cobblestone")},
            {BlockType.WallSign,new Faces("oak_planks|planks_oak","oak_planks|planks_oak","oak_planks|planks_oak","oak_planks|planks_oak","oak_planks|planks_oak","oak_planks|planks_oak","oak_planks|planks_oak")},
            {BlockType.Lever,new Faces("cobblestone","cobblestone","cobblestone","cobblestone","cobblestone","cobblestone","cobblestone")},
            {BlockType.StonePressurePlate,new Faces("stone","stone","stone","stone","stone","stone","stone")},
            {BlockType.IronDoor,new Faces("iron_door_bottom|door_iron_lower","iron_door_top|door_iron_upper","iron_door_bottom|door_iron_lower","iron_door_bottom|door_iron_lower","iron_door_bottom|door_iron_lower","iron_door_bottom|door_iron_lower","iron_door_bottom|door_iron_lower")},
            {BlockType.WoodenPressurePlate,new Faces("oak_planks|planks_oak","oak_planks|planks_oak","oak_planks|planks_oak","oak_planks|planks_oak","oak_planks|planks_oak","oak_planks|planks_oak","oak_planks|planks_oak")},
            {BlockType.RedstoneOre,new Faces("redstone_ore","redstone_ore","redstone_ore","redstone_ore","redstone_ore","redstone_ore","redstone_ore")},
            {BlockType.LitRedstoneOre,new Faces("redstone_ore","redstone_ore","redstone_ore","redstone_ore","redstone_ore","redstone_ore","redstone_ore")},
            {BlockType.RedstoneTorchOff,new Faces("redstone_torch_off","redstone_torch_off","redstone_torch_off","redstone_torch_off","redstone_torch_off","redstone_torch_off","redstone_torch_off")},
            {BlockType.RedstoneTorch,new Faces("redstone_torch|redstone_torch_on","redstone_torch|redstone_torch_on","redstone_torch|redstone_torch_on","redstone_torch|redstone_torch_on","redstone_torch|redstone_torch_on","redstone_torch|redstone_torch_on","redstone_torch|redstone_torch_on")},
            {BlockType.StoneButton,new Faces("stone","stone","stone","stone","stone","stone","stone")},
            {BlockType.SnowLayer,new Faces("snow","snow","snow","snow","snow","snow","snow")},
            {BlockType.SnowBlock,new Faces("snow","snow","snow","snow","snow","snow","snow")},
            {BlockType.Cactus,new Faces("cactus_side","cactus_top","cactus_bottom","cactus_side","cactus_side","cactus_side","cactus_side")},
            {BlockType.Clay,new Faces("clay","clay","clay","clay","clay","clay","clay")},
            {BlockType.SugarCane,new Faces("sugar_cane|reeds","sugar_cane|reeds","sugar_cane|reeds","sugar_cane|reeds","sugar_cane|reeds","sugar_cane|reeds","sugar_cane|reeds")},
            {BlockType.Jukebox,new Faces("jukebox_side","jukebox_top","oak_planks|planks_oak","jukebox_side","jukebox_side","jukebox_side","jukebox_side")},
            {BlockType.Fence,new Faces("oak_planks|planks_oak","oak_planks|planks_oak","oak_planks|planks_oak","oak_planks|planks_oak","oak_planks|planks_oak","oak_planks|planks_oak","oak_planks|planks_oak")},
            {BlockType.Pumpkin,new Faces("pumpkin_side","pumpkin_top","pumpkin_top","carved_pumpkin|pumpkin_face_off","pumpkin_side","pumpkin_side","pumpkin_side")},
            {BlockType.Netherrack,new Faces("netherrack","netherrack","netherrack","netherrack","netherrack","netherrack","netherrack")},
            {BlockType.SoulSand,new Faces("soul_sand","soul_sand","soul_sand","soul_sand","soul_sand","soul_sand","soul_sand")},
            {BlockType.NetherPortal,new Faces("nether_portal|portal","nether_portal|portal","nether_portal|portal","nether_portal|portal","nether_portal|portal","nether_portal|portal","nether_portal|portal")},
            {BlockType.JackOLantern,new Faces("pumpkin_side","pumpkin_top","pumpkin_top","jack_o_lantern|pumpkin_face_on","pumpkin_side","pumpkin_side","pumpkin_side")},
            {BlockType.Cake,new Faces("cake_side","cake_top","cake_bottom","cake_side","cake_side","cake_side","cake_side")},
            {BlockType.RedstoneRepeater,new Faces("repeater|repeater_off","repeater|repeater_off","repeater|repeater_off","repeater|repeater_off","repeater|repeater_off","repeater|repeater_off","repeater|repeater_off")},
            {BlockType.PoweredRepeater,new Faces("repeater_on","repeater_on","repeater_on","repeater_on","repeater_on","repeater_on","repeater_on")},
            {BlockType.LockedChest,new Faces("chest_side","chest_top","chest_top","chest_front","chest_side","chest_side","chest_side")},
            {BlockType.Trapdoor,new Faces("oak_trapdoor|trapdoor","oak_trapdoor|trapdoor","oak_trapdoor|trapdoor","oak_trapdoor|trapdoor","oak_trapdoor|trapdoor","oak_trapdoor|trapdoor","oak_trapdoor|trapdoor")},
            {BlockType.SpruceSapling,new Faces("spruce_sapling|sapling_spruce","spruce_sapling|sapling_spruce","spruce_sapling|sapling_spruce","spruce_sapling|sapling_spruce","spruce_sapling|sapling_spruce","spruce_sapling|sapling_spruce","spruce_sapling|sapling_spruce")},
            {BlockType.BirchSapling,new Faces("birch_sapling|sapling_birch","birch_sapling|sapling_birch","birch_sapling|sapling_birch","birch_sapling|sapling_birch","birch_sapling|sapling_birch","birch_sapling|sapling_birch","birch_sapling|sapling_birch")},
            {BlockType.DryTallGrass,new Faces("dead_bush|deadbush","dead_bush|deadbush","dead_bush|deadbush","dead_bush|deadbush","dead_bush|deadbush","dead_bush|deadbush","dead_bush|deadbush")},
            {BlockType.Fern,new Faces("fern","fern","fern","fern","fern","fern","fern")},
            {BlockType.SandstoneSlab,new Faces("sandstone|sandstone_normal","sandstone_top","sandstone_bottom","sandstone|sandstone_normal","sandstone|sandstone_normal","sandstone|sandstone_normal","sandstone|sandstone_normal")},
            {BlockType.CobblestoneSlab,new Faces("cobblestone","cobblestone","cobblestone","cobblestone","cobblestone","cobblestone","cobblestone")},
            {BlockType.DoubleSandstoneSlab,new Faces("sandstone|sandstone_normal","sandstone_top","sandstone_bottom","sandstone|sandstone_normal","sandstone|sandstone_normal","sandstone|sandstone_normal","sandstone|sandstone_normal")},
            {BlockType.DoubleWoodenSlab,new Faces("oak_planks|planks_oak","oak_planks|planks_oak","oak_planks|planks_oak","oak_planks|planks_oak","oak_planks|planks_oak","oak_planks|planks_oak","oak_planks|planks_oak")},
            {BlockType.DoubleCobblestoneSlab,new Faces("cobblestone","cobblestone","cobblestone","cobblestone","cobblestone","cobblestone","cobblestone")},
            {BlockType.MoistFarmland,new Faces("dirt","farmland_moist|farmland_wet","dirt","dirt","dirt","dirt","dirt")},
            {BlockType.OrangeWool,new Faces("orange_wool|wool_colored_orange","orange_wool|wool_colored_orange","orange_wool|wool_colored_orange","orange_wool|wool_colored_orange","orange_wool|wool_colored_orange","orange_wool|wool_colored_orange","orange_wool|wool_colored_orange")},
            {BlockType.MagentaWool,new Faces("magenta_wool|wool_colored_magenta","magenta_wool|wool_colored_magenta","magenta_wool|wool_colored_magenta","magenta_wool|wool_colored_magenta","magenta_wool|wool_colored_magenta","magenta_wool|wool_colored_magenta","magenta_wool|wool_colored_magenta")},
            {BlockType.LightBlueWool,new Faces("light_blue_wool|wool_colored_light_blue","light_blue_wool|wool_colored_light_blue","light_blue_wool|wool_colored_light_blue","light_blue_wool|wool_colored_light_blue","light_blue_wool|wool_colored_light_blue","light_blue_wool|wool_colored_light_blue","light_blue_wool|wool_colored_light_blue")},
            {BlockType.YellowWool,new Faces("yellow_wool|wool_colored_yellow","yellow_wool|wool_colored_yellow","yellow_wool|wool_colored_yellow","yellow_wool|wool_colored_yellow","yellow_wool|wool_colored_yellow","yellow_wool|wool_colored_yellow","yellow_wool|wool_colored_yellow")},
            {BlockType.LimeWool,new Faces("lime_wool|wool_colored_lime","lime_wool|wool_colored_lime","lime_wool|wool_colored_lime","lime_wool|wool_colored_lime","lime_wool|wool_colored_lime","lime_wool|wool_colored_lime","lime_wool|wool_colored_lime")},
            {BlockType.PinkWool,new Faces("pink_wool|wool_colored_pink","pink_wool|wool_colored_pink","pink_wool|wool_colored_pink","pink_wool|wool_colored_pink","pink_wool|wool_colored_pink","pink_wool|wool_colored_pink","pink_wool|wool_colored_pink")},
            {BlockType.GrayWool,new Faces("gray_wool|wool_colored_gray","gray_wool|wool_colored_gray","gray_wool|wool_colored_gray","gray_wool|wool_colored_gray","gray_wool|wool_colored_gray","gray_wool|wool_colored_gray","gray_wool|wool_colored_gray")},
            {BlockType.LightGrayWool,new Faces("light_gray_wool|wool_colored_silver","light_gray_wool|wool_colored_silver","light_gray_wool|wool_colored_silver","light_gray_wool|wool_colored_silver","light_gray_wool|wool_colored_silver","light_gray_wool|wool_colored_silver","light_gray_wool|wool_colored_silver")},
            {BlockType.CyanWool,new Faces("cyan_wool|wool_colored_cyan","cyan_wool|wool_colored_cyan","cyan_wool|wool_colored_cyan","cyan_wool|wool_colored_cyan","cyan_wool|wool_colored_cyan","cyan_wool|wool_colored_cyan","cyan_wool|wool_colored_cyan")},
            {BlockType.PurpleWool,new Faces("purple_wool|wool_colored_purple","purple_wool|wool_colored_purple","purple_wool|wool_colored_purple","purple_wool|wool_colored_purple","purple_wool|wool_colored_purple","purple_wool|wool_colored_purple","purple_wool|wool_colored_purple")},
            {BlockType.BlueWool,new Faces("blue_wool|wool_colored_blue","blue_wool|wool_colored_blue","blue_wool|wool_colored_blue","blue_wool|wool_colored_blue","blue_wool|wool_colored_blue","blue_wool|wool_colored_blue","blue_wool|wool_colored_blue")},
            {BlockType.BrownWool,new Faces("brown_wool|wool_colored_brown","brown_wool|wool_colored_brown","brown_wool|wool_colored_brown","brown_wool|wool_colored_brown","brown_wool|wool_colored_brown","brown_wool|wool_colored_brown","brown_wool|wool_colored_brown")},
            {BlockType.GreenWool,new Faces("green_wool|wool_colored_green","green_wool|wool_colored_green","green_wool|wool_colored_green","green_wool|wool_colored_green","green_wool|wool_colored_green","green_wool|wool_colored_green","green_wool|wool_colored_green")},
            {BlockType.RedWool,new Faces("red_wool|wool_colored_red","red_wool|wool_colored_red","red_wool|wool_colored_red","red_wool|wool_colored_red","red_wool|wool_colored_red","red_wool|wool_colored_red","red_wool|wool_colored_red")},
            {BlockType.BlackWool,new Faces("black_wool|wool_colored_black","black_wool|wool_colored_black","black_wool|wool_colored_black","black_wool|wool_colored_black","black_wool|wool_colored_black","black_wool|wool_colored_black","black_wool|wool_colored_black")},
            {BlockType.WheatCrop1,new Faces("wheat_stage1|wheat_stage_1","wheat_stage1|wheat_stage_1","wheat_stage1|wheat_stage_1","wheat_stage1|wheat_stage_1","wheat_stage1|wheat_stage_1","wheat_stage1|wheat_stage_1","wheat_stage1|wheat_stage_1")},
            {BlockType.WheatCrop2,new Faces("wheat_stage2|wheat_stage_2","wheat_stage2|wheat_stage_2","wheat_stage2|wheat_stage_2","wheat_stage2|wheat_stage_2","wheat_stage2|wheat_stage_2","wheat_stage2|wheat_stage_2","wheat_stage2|wheat_stage_2")},
            {BlockType.WheatCrop3,new Faces("wheat_stage3|wheat_stage_3","wheat_stage3|wheat_stage_3","wheat_stage3|wheat_stage_3","wheat_stage3|wheat_stage_3","wheat_stage3|wheat_stage_3","wheat_stage3|wheat_stage_3","wheat_stage3|wheat_stage_3")},
            {BlockType.WheatCrop4,new Faces("wheat_stage4|wheat_stage_4","wheat_stage4|wheat_stage_4","wheat_stage4|wheat_stage_4","wheat_stage4|wheat_stage_4","wheat_stage4|wheat_stage_4","wheat_stage4|wheat_stage_4","wheat_stage4|wheat_stage_4")},
            {BlockType.WheatCrop5,new Faces("wheat_stage5|wheat_stage_5","wheat_stage5|wheat_stage_5","wheat_stage5|wheat_stage_5","wheat_stage5|wheat_stage_5","wheat_stage5|wheat_stage_5","wheat_stage5|wheat_stage_5","wheat_stage5|wheat_stage_5")},
            {BlockType.WheatCrop6,new Faces("wheat_stage6|wheat_stage_6","wheat_stage6|wheat_stage_6","wheat_stage6|wheat_stage_6","wheat_stage6|wheat_stage_6","wheat_stage6|wheat_stage_6","wheat_stage6|wheat_stage_6","wheat_stage6|wheat_stage_6")},
            {BlockType.WheatCrop7,new Faces("wheat_stage7|wheat_stage_7","wheat_stage7|wheat_stage_7","wheat_stage7|wheat_stage_7","wheat_stage7|wheat_stage_7","wheat_stage7|wheat_stage_7","wheat_stage7|wheat_stage_7","wheat_stage7|wheat_stage_7")},
        };
        public static Faces Block(BlockType type) {Faces result;return blocks.TryGetValue(type,out result)?result:null;}
        public static string Snake(string name) => Regex.Replace(name,"([a-z0-9])([A-Z])","$1_$2").ToLowerInvariant();

        public static string Part(BlockType type,BlockTexturePart part)
        {
            var f=Block(type);
            switch(part)
            {
                case BlockTexturePart.Surface:return type==BlockType.Lever?"lever":f==null?"":f.side;
                case BlockTexturePart.DoorLower:return f.side;
                case BlockTexturePart.DoorUpper:return f.top;
                case BlockTexturePart.BedHeadTop:return "bed_head_top";
                case BlockTexturePart.BedHeadSide:return "bed_head_side";
                case BlockTexturePart.BedHeadEnd:return "bed_head_end";
                case BlockTexturePart.BedFootTop:return "bed_feet_top|bed_foot_top";
                case BlockTexturePart.BedFootSide:return "bed_feet_side|bed_foot_side";
                case BlockTexturePart.BedFootEnd:return "bed_feet_end|bed_foot_end";
                case BlockTexturePart.BedBottom:return "oak_planks|planks_oak";
                case BlockTexturePart.RailStraight:return f.side;
                case BlockTexturePart.RailCorner:return "rail_corner|rail_normal_turned";
                case BlockTexturePart.RailPowered:return type==BlockType.DetectorRail?"detector_rail_on|rail_detector_powered":"powered_rail_on|rail_golden_powered";
                case BlockTexturePart.WireCross:return "redstone_dust_cross";
                case BlockTexturePart.WireLine:return "redstone_dust_line|redstone_dust_line0";
                case BlockTexturePart.WireCrossOverlay:return "redstone_dust_cross_overlay";
                case BlockTexturePart.WireLineOverlay:return "redstone_dust_line_overlay";
                case BlockTexturePart.LeverBase:return "cobblestone";
                case BlockTexturePart.LeverHandle:return "lever";
                case BlockTexturePart.RepeaterTop:return f.side;
                case BlockTexturePart.RepeaterSide:case BlockTexturePart.RepeaterBottom:return "smooth_stone|stone_slab_top";
                case BlockTexturePart.RepeaterTorch:return type==BlockType.PoweredRepeater?"redstone_torch|redstone_torch_on":"redstone_torch_off";
                case BlockTexturePart.PistonFace:return "piston_top|piston_top_normal";
                case BlockTexturePart.PistonStickyFace:return "sticky_piston_top|piston_top_sticky";
                case BlockTexturePart.PistonSide:return "piston_side";
                case BlockTexturePart.PistonBottom:return "piston_bottom";
                case BlockTexturePart.PistonInner:return "piston_inner";
                case BlockTexturePart.PistonRod:return "piston_side";
                case BlockTexturePart.SignFront:case BlockTexturePart.SignBack:case BlockTexturePart.SignEdge:case BlockTexturePart.SignPost:return "oak_planks|planks_oak";
                case BlockTexturePart.FireA:return "fire_0|fire_layer_0";
                case BlockTexturePart.FireB:return "fire_1|fire_layer_1";
                case BlockTexturePart.FluidStill:return type==BlockType.Lava?"lava_still":"water_still";
                case BlockTexturePart.FluidFlow:return type==BlockType.Lava?"lava_flow":"water_flow";
                case BlockTexturePart.CakeInside:return "cake_inner";
                case BlockTexturePart.ChestFrontLeft:return "chest_front_left";
                case BlockTexturePart.ChestFrontRight:return "chest_front_right";
                case BlockTexturePart.ChestBackLeft:return "chest_back_left";
                case BlockTexturePart.ChestBackRight:return "chest_back_right";
                case BlockTexturePart.ChestTopLeft:return "chest_top_left";
                case BlockTexturePart.ChestTopRight:return "chest_top_right";
                case BlockTexturePart.GrassSideOverlay:return "grass_block_side_overlay|grass_side_overlay";
                default:return "";
            }
        }
        public static string Item(ItemType type)
        {
            switch(type)
            {
                case ItemType.IronIngot:return "iron_ingot|ingot_iron";
                case ItemType.GoldIngot:return "gold_ingot|ingot_gold";
                case ItemType.Bow:return "bow|bow_standby";
                case ItemType.Book:return "book|book_normal";
                case ItemType.Bucket:return "bucket|bucket_empty";
                case ItemType.ChestMinecart:return "chest_minecart|minecart_chest";
                case ItemType.FurnaceMinecart:return "furnace_minecart|minecart_furnace";
                case ItemType.GoldenApple:return "golden_apple|apple_golden";
                case ItemType.Bed:return "red_bed|bed";
                case ItemType.WoodenDoor:return "oak_door|wooden_door|door_wood";
                case ItemType.IronDoor:return "iron_door|door_iron";
                case ItemType.StandingSign:case ItemType.WallSign:return "oak_sign|sign";
                case ItemType.SugarCane:return "sugar_cane|reeds";
                case ItemType.RedstoneRepeater:case ItemType.PoweredRepeater:return "repeater";
                case ItemType.RedstoneDust:return "redstone|redstone_dust";
                case ItemType.RawPorkchop:return "porkchop|porkchop_raw";
                case ItemType.CookedPorkchop:return "cooked_porkchop|porkchop_cooked";
                case ItemType.RawFish:return "cod|fish_cod_raw|fish|fish_raw";
                case ItemType.CookedFish:return "cooked_cod|fish_cod_cooked|cooked_fish|fish_cooked";
                case ItemType.WaterBucket:return "water_bucket|bucket_water";
                case ItemType.LavaBucket:return "lava_bucket|bucket_lava";
                case ItemType.MilkBucket:return "milk_bucket|bucket_milk";
                case ItemType.Boat:return "oak_boat|boat";
                case ItemType.MusicDisc13:return "music_disc_13|record_13";
                case ItemType.MusicDiscCat:return "music_disc_cat|record_cat";
                case ItemType.Map:return "filled_map|map_filled|map";
                case ItemType.Compass:return "compass|compass_00";
                case ItemType.Clock:return "clock|clock_00";
                case ItemType.FishingRod:return "fishing_rod|fishing_rod_uncast";
                case ItemType.GlowstoneDust:return "glowstone_dust";
                case ItemType.InkSac:return "ink_sac|dye_powder_black";
                case ItemType.RoseRed:return "red_dye|rose_red|dye_powder_red";
                case ItemType.CactusGreen:return "green_dye|cactus_green|dye_powder_green";
                case ItemType.CocoaBeans:return "cocoa_beans|dye_powder_brown";
                case ItemType.LapisLazuli:return "lapis_lazuli|dye_powder_blue";
                case ItemType.PurpleDye:return "purple_dye|dye_powder_purple";
                case ItemType.CyanDye:return "cyan_dye|dye_powder_cyan";
                case ItemType.LightGrayDye:return "light_gray_dye|silver_dye|dye_powder_silver";
                case ItemType.GrayDye:return "gray_dye|dye_powder_gray";
                case ItemType.PinkDye:return "pink_dye|dye_powder_pink";
                case ItemType.LimeDye:return "lime_dye|dye_powder_lime";
                case ItemType.DandelionYellow:return "yellow_dye|dandelion_yellow|dye_powder_yellow";
                case ItemType.LightBlueDye:return "light_blue_dye|dye_powder_light_blue";
                case ItemType.MagentaDye:return "magenta_dye|dye_powder_magenta";
                case ItemType.OrangeDye:return "orange_dye|dye_powder_orange";
                case ItemType.BoneMeal:return "bone_meal|dye_powder_white";
                case ItemType.Seeds:return "wheat_seeds|seeds_wheat|seeds";
                case ItemType.LeatherHelmet:return "leather_helmet|leather_helmet_overlay";
                default:
                    string name=type.ToString(),normal=Snake(name);
                    var tier=Regex.Match(name,"^(Wooden|Stone|Iron|Diamond|Golden|Leather|Chainmail)(Sword|Shovel|Pickaxe|Axe|Hoe|Helmet|Chestplate|Leggings|Boots)$");
                    if(!tier.Success)return normal;
                    string material=tier.Groups[1].Value.ToLowerInvariant();
                    if(material=="wooden")material="wood";
                    if(material=="golden")material="gold";
                    string part=tier.Groups[2].Value.ToLowerInvariant();
                    return normal+"|"+part+"_"+material+(material=="leather"?"|"+part+"_cloth":"");
            }
        }
        public static bool DedicatedItemIcon(ItemType type)
        {
            return (int)type>=1000||type==ItemType.Bed||type==ItemType.Cake||type==ItemType.WoodenDoor||
                type==ItemType.IronDoor||type==ItemType.StandingSign||type==ItemType.WallSign||
                type==ItemType.SugarCane||type==ItemType.RedstoneRepeater||type==ItemType.PoweredRepeater;
        }
        public static bool FlatBlockIcon(ItemType type)
        {
            string name=type.ToString();
            if(name.StartsWith("WheatCrop",StringComparison.Ordinal))return true;
            switch(type)
            {
                case ItemType.OakSapling:case ItemType.SpruceSapling:case ItemType.BirchSapling:
                case ItemType.Dandelion:case ItemType.Rose:case ItemType.BrownMushroom:case ItemType.RedMushroom:
                case ItemType.TallGrass:case ItemType.DryTallGrass:case ItemType.Fern:case ItemType.DeadBush:
                case ItemType.Cobweb:case ItemType.Torch:case ItemType.RedstoneTorch:case ItemType.RedstoneTorchOff:
                case ItemType.Rail:case ItemType.PoweredRail:case ItemType.DetectorRail:case ItemType.Ladder:
                case ItemType.RedstoneWire:case ItemType.Lever:return true;
                default:return false;
            }
        }
    }

    // Keep lookup types with their consumer so the editor tool has no separate
    // helper-type dependency during script imports.
    public enum MinecraftTextureKind { Block, Item, Gui, Entity, Colormap, Flat }

    // Never search the whole project or accept an arbitrary first match. GUI/item
    // names collide with block names, and packs can contain duplicate subfolders.
    public class MinecraftTextureLookup
    {
        private static string Key(MinecraftTextureKind kind,string name) => kind+":"+name.Replace("_","").Replace("-","");
        private readonly Dictionary<string,List<string>> files=new Dictionary<string,List<string>>(StringComparer.OrdinalIgnoreCase);
        public MinecraftTextureLookup(string root,IEnumerable<string> paths)
        {
            root=root.Replace('\\','/').TrimEnd('/')+"/";
            foreach(var raw in paths)
            {
                string path=raw.Replace('\\','/');
                if(!path.StartsWith(root,StringComparison.OrdinalIgnoreCase))continue;
                string relative=path.Substring(root.Length),lower="/"+relative.ToLowerInvariant();
                var kind=lower.Contains("/block/")||lower.Contains("/blocks/")?MinecraftTextureKind.Block:
                    lower.Contains("/item/")||lower.Contains("/items/")?MinecraftTextureKind.Item:
                    lower.Contains("/gui/")?MinecraftTextureKind.Gui:lower.Contains("/entity/")?MinecraftTextureKind.Entity:
                    lower.Contains("/colormap/")||lower=="/grass.png"||lower=="/foliage.png"||lower=="/grasscolor.png"||lower=="/foliagecolor.png"?MinecraftTextureKind.Colormap:MinecraftTextureKind.Flat;
                string key=Key(kind,Path.GetFileNameWithoutExtension(path));
                List<string> matches;
                if(!files.TryGetValue(key,out matches))files.Add(key,matches=new List<string>());
                if(!matches.Contains(path))matches.Add(path);
            }
        }
        public string Find(MinecraftTextureKind kind,string aliases,out bool ambiguous)
        {
            ambiguous=false;
            foreach(string alias in (aliases??"").Split('|'))
            {
                if(alias.Length==0)continue;
                List<string> matches;
                if(!files.TryGetValue(Key(kind,alias),out matches)&&!files.TryGetValue(Key(MinecraftTextureKind.Flat,alias),out matches))continue;
                if(matches.Count!=1){ambiguous=true;return null;}
                return matches[0];
            }
            return null;
        }
    }

    public const string Root="Assets/Textures/Minecraft";
    private static string ReportPath => Path.Combine(Path.GetDirectoryName(Application.dataPath),"Logs/MinecraftTextureAssignment.txt");
    private sealed class Run
    {
        public MinecraftTextureLookup lookup;
        public readonly MinecraftTextureImageTools images=new MinecraftTextureImageTools();
        public readonly StringBuilder report=new StringBuilder();
        public int assigned,missing,ambiguous;
        public readonly HashSet<UnityEngine.Object> dirty=new HashSet<UnityEngine.Object>();
        public Texture2D Find(MinecraftTextureKind kind,string names,string target,bool optional=false)
        {
            bool conflict;string path=lookup.Find(kind,names,out conflict);
            if(path==null)
            {
                if(conflict){ambiguous++;report.AppendLine("AMBIGUOUS "+target+" ["+names+"] -- existing link kept");}
                else if(!optional){missing++;report.AppendLine("MISSING "+target+" ["+names+"] -- existing link kept");}
                return null;
            }
            var image=AssetDatabase.LoadAssetAtPath<Texture2D>(path);
            if(image==null){missing++;report.AppendLine("UNREADABLE "+target+" "+path);return null;}
            report.AppendLine("MATCH "+target+" -> "+path);
            return image;
        }
    }

    [InitializeOnLoadMethod]
    private static void QueueFluidBinding() { EditorApplication.delayCall += BindMissingFluidTextures; }

    [MenuItem("Minecraft/Textures/Bind Missing Fluid Textures")]
    public static void BindMissingFluidTextures()
    {
        if(EditorApplication.isPlayingOrWillChangePlaymode)return;
        if(EditorApplication.isCompiling||EditorApplication.isUpdating)
        {EditorApplication.delayCall+=BindMissingFluidTextures;return;}
        if(!AssetDatabase.IsValidFolder(Root))return;
        var blocks=AssetDatabase.LoadAssetAtPath<BlockDataSO>("Assets/Block Data.asset");
        if(blocks==null||blocks.textureDataList==null)return;
        var paths=new List<string>();
        foreach(string guid in AssetDatabase.FindAssets("t:Texture2D",new[]{Root}))paths.Add(AssetDatabase.GUIDToAssetPath(guid));
        var run=new Run{lookup=new MinecraftTextureLookup(Root,paths)};
        bool changed=false;
        foreach(var block in blocks.textureDataList)
        {
            if(block==null||(block.blockType!=BlockType.Water&&block.blockType!=BlockType.Lava))continue;
            foreach(var part in new[]{BlockTexturePart.FluidStill,BlockTexturePart.FluidFlow})
            {
                var texture=block.textureData;
                var slot=texture==null||texture.specialTextures==null?null:texture.specialTextures.Find(p=>p!=null&&p.part==part);
                if(slot!=null&&slot.image!=null)continue;
                var image=run.Find(MinecraftTextureKind.Block,MinecraftTextureNames.Part(block.blockType,part),block.blockType+"."+part,true);
                if(image==null)continue;
                if(texture==null)block.textureData=texture=new TextureData();
                if(texture.specialTextures==null)texture.specialTextures=new List<SpecialBlockTexture>();
                if(slot==null){slot=new SpecialBlockTexture{part=part};texture.specialTextures.Add(slot);}
                slot.image=image;slot.overrideAnimation=true;slot.animate=Animated(image);
                slot.columns=1;slot.rows=0;slot.frameCount=0;slot.framesPerSecond=Fps(image);
                changed=true;
            }
        }
        if(changed)
        {
            EditorUtility.SetDirty(blocks);AssetDatabase.SaveAssetIfDirty(blocks);
            AnimatedBlockTextureImports.RepairAssignedSheets();
            Debug.Log("Bound missing water/lava sheets from "+Root+". Existing texture links were preserved.");
        }
    }

    [MenuItem("Minecraft/Textures/Assign From Minecraft Folder")]
    public static void Assign()
    {
        if(EditorApplication.isPlayingOrWillChangePlaymode)
        {Debug.LogWarning("Exit Play Mode before assigning game textures.");return;}
        if(!AssetDatabase.IsValidFolder(Root))
        {Debug.LogError("Texture folder not found: "+Root+". Put the texture pack there, let Unity import it, then run this menu again.");return;}
        var blocks=AssetDatabase.LoadAssetAtPath<BlockDataSO>("Assets/Block Data.asset");
        var items=AssetDatabase.LoadAssetAtPath<ItemDataSO>("Assets/Resources/Item Data.asset");
        if(blocks==null||items==null)
        {Debug.LogError("Block Data or Item Data is missing. Existing assets were not changed.");return;}
        var paths=new List<string>();
        foreach(string guid in AssetDatabase.FindAssets("t:Texture2D",new[]{Root}))paths.Add(AssetDatabase.GUIDToAssetPath(guid));
        var run=new Run{lookup=new MinecraftTextureLookup(Root,paths)};
        run.report.AppendLine("Source: "+Root+"\nMissing and ambiguous matches keep existing links. Block/item IDs, stack limits, recipes, tints and world data are unchanged.");
        Undo.IncrementCurrentGroup();
        int group=Undo.GetCurrentGroup();Undo.SetCurrentGroupName("Assign Minecraft textures");
        Undo.RecordObject(blocks,"Assign Minecraft block textures");
        Undo.RecordObject(items,"Assign Minecraft item textures");
        run.dirty.Add(blocks);run.dirty.Add(items);
        bool committed=false;
        try
        {
            EditorUtility.DisplayProgressBar("Minecraft textures","Assigning block and model images",.1f);
            if(blocks.textureDataList!=null)foreach(var block in blocks.textureDataList)
            {
                if(block==null)continue;
                var spec=MinecraftTextureNames.Block(block.blockType);
                if(spec==null||string.IsNullOrEmpty(spec.side))continue;
                TextureData texture=block.textureData;
                foreach(string field in new[]{"top","bottom","side","front","back","left","right"})
                {
                    var image=run.Find(MinecraftTextureKind.Block,spec.Get(field),block.blockType+"."+field);
                    if(image==null)continue;
                    if(texture==null)block.textureData=texture=new TextureData();
                    var property=typeof(TextureData).GetField(field);
                    if(property.GetValue(texture)!=image){property.SetValue(texture,image);run.assigned++;}
                }
                foreach(var part in SpecialTextureLayout.Parts(block.blockType))
                {
                    var image=run.Find(MinecraftTextureKind.Block,MinecraftTextureNames.Part(block.blockType,part),block.blockType+"."+part,true);
                    if(image==null)continue;
                    if(texture==null)block.textureData=texture=new TextureData();
                    if(texture.specialTextures==null)texture.specialTextures=new List<SpecialBlockTexture>();
                    var slot=texture.specialTextures.Find(p=>p!=null&&p.part==part);
                    if(slot==null){slot=new SpecialBlockTexture{part=part};texture.specialTextures.Add(slot);}
                    if(slot.image!=image){slot.image=image;run.assigned++;}
                    bool animated=Animated(image);
                    slot.overrideAnimation=true;slot.animate=animated;
                    slot.columns=1;slot.rows=0;slot.frameCount=0;slot.framesPerSecond=Fps(image);
                    if(animated&&(block.blockType==BlockType.Water||block.blockType==BlockType.Lava||block.blockType==BlockType.Fire))
                    {texture.animateFaces=true;texture.animationColumns=1;texture.animationRows=0;texture.animationFrameCount=0;texture.animationFramesPerSecond=Fps(image);}
                }
            }
            EditorUtility.DisplayProgressBar("Minecraft textures","Assigning item images",.55f);
            if(items.items!=null)foreach(var item in items.items)
            {
                if(item==null)continue;
                bool dedicated=MinecraftTextureNames.DedicatedItemIcon(item.id);
                bool flat=MinecraftTextureNames.FlatBlockIcon(item.id);
                if(!dedicated&&!flat)continue; // Keep native 3D icons for ordinary block items.
                var image=dedicated?run.Find(MinecraftTextureKind.Item,MinecraftTextureNames.Item(item.id),"Item."+item.id):null;
                if(image==null&&flat)
                {
                    var spec=MinecraftTextureNames.Block((BlockType)(int)item.id);
                    string names=item.id==ItemType.Lever?"lever":item.id==ItemType.RedstoneWire?spec.top:spec.side;
                    image=run.Find(MinecraftTextureKind.Block,names,"Item."+item.id,true);
                }
                if(image==null)continue; // Block meshes remain the normal icon fallback.
                image=run.images.Icon(image);
                if(image!=null&&item.icon!=image){item.icon=image;run.assigned++;}
            }
            EditorUtility.DisplayProgressBar("Minecraft textures","Assigning HUD and player images",.85f);
            AssignHudAndSkin(run);
            AnimatedBlockTextureImports.RepairAssignedSheets();
            Undo.FlushUndoRecordObjects();
            foreach(var data in run.dirty){EditorUtility.SetDirty(data);AssetDatabase.SaveAssetIfDirty(data);}
            committed=true;
            Undo.CollapseUndoOperations(group);
            run.report.AppendLine("\nEntity sheets are not face images: missing bed/chest face images require a matching skin-layout conversion. Missing old-version items stay linked to their existing images.");
            run.report.AppendLine("GUI layouts/fonts and biome tint behaviour are retained; this tool assigns the images used by the existing renderers.");
            run.report.AppendLine("\nAvailable source images (for reviewing unresolved layouts):");
            paths.Sort(StringComparer.OrdinalIgnoreCase);
            foreach(string path in paths)
            {
                var image=AssetDatabase.LoadAssetAtPath<Texture2D>(path);
                run.report.AppendLine(path+(image==null?"":" ["+image.width+"x"+image.height+"]"));
            }
            string summary=run.assigned+" image links updated; "+run.missing+" missing required matches; "+run.ambiguous+" ambiguous matches.";
            Directory.CreateDirectory(Path.GetDirectoryName(ReportPath));
            File.WriteAllText(ReportPath,summary+"\n\n"+run.report);
            Debug.Log("Minecraft textures: "+summary+" See Minecraft > Textures > Open Assignment Report. Restart Play Mode to rebuild texture arrays.");
        }
        catch(Exception error)
        {if(!committed){Undo.FlushUndoRecordObjects();Undo.RevertAllDownToGroup(group);}Debug.LogException(error);throw;}
        finally {EditorUtility.ClearProgressBar();}
    }

    private static void AssignHudAndSkin(Run run)
    {
        var hud=AssetDatabase.LoadAssetAtPath<PlayerHudAssets>("Assets/Resources/PlayerHudAssets.asset");
        if(hud!=null)
        {
            Undo.RecordObject(hud,"Assign Minecraft HUD textures");
            run.dirty.Add(hud);
            var icons=run.Find(MinecraftTextureKind.Gui,"icons","HUD icons",true);
            if(icons!=null&&hud.icons!=icons){hud.icons=icons;run.assigned++;}
            // Use the exact container sheet; other GUI atlases can also be named inventory.
            string inventoryPath=Root+"/gui/container/inventory.png";
            var inventory=AssetDatabase.LoadAssetAtPath<Texture2D>(inventoryPath);
            if(inventory!=null&&hud.inventory!=inventory)
            {hud.inventory=inventory;run.assigned++;run.report.AppendLine("MATCH Survival inventory -> "+inventoryPath);}
            var crafting=AssetDatabase.LoadAssetAtPath<Texture2D>(Root+"/gui/container/crafting_table.png");
            if(crafting!=null&&hud.craftingTable!=crafting){hud.craftingTable=crafting;run.assigned++;}
            var breaking=run.Find(MinecraftTextureKind.Block,"block_break","Block breaking sheet",true);
            if(breaking==null)
            {
                var frames=new Texture2D[10];bool all=true;
                for(int i=0;i<10;i++){frames[i]=run.Find(MinecraftTextureKind.Block,"destroy_stage_"+i,"Block break stage "+i,true);all&=frames[i]!=null;}
                if(all)breaking=run.images.Breaking(frames);
            }
            if(breaking!=null)
            {
                if(hud.blockBreak!=breaking){hud.blockBreak=breaking;run.assigned++;}
                hud.breakColumns=0;hud.breakRows=0;
            }
        }
        var skin=run.Find(MinecraftTextureKind.Entity,"steve","Player skin",true);
        if(skin==null)return;
        skin=run.images.Skin(skin);if(skin==null){run.report.AppendLine("UNSUPPORTED Steve skin aspect ratio; player materials preserved.");return;}
        if(hud!=null&&hud.inventoryPlayerSkin!=skin){hud.inventoryPlayerSkin=skin;run.assigned++;}
        foreach(string path in new[]{"Assets/Textures/Player/player.mat","Assets/Resources/InventoryPlayerMaterial.mat"})
        {
            var material=AssetDatabase.LoadAssetAtPath<Material>(path);
            if(material==null||material.mainTexture==skin)continue;
            Undo.RecordObject(material,"Assign Minecraft player skin");material.mainTexture=skin;run.assigned++;
            run.dirty.Add(material);
        }
    }

    private static bool Animated(Texture2D image)
    {return image.height>image.width&&image.height%image.width==0;}
    private static float Fps(Texture2D image)
    {
        string path=Path.Combine(Path.GetDirectoryName(Application.dataPath),AssetDatabase.GetAssetPath(image)+".mcmeta");
        if(!File.Exists(path))return ModernFluidTextures.FramesPerSecond(image.name);
        var match=Regex.Match(File.ReadAllText(path),"\"frametime\"\\s*:\\s*(\\d+)");
        int ticks;return match.Success&&int.TryParse(match.Groups[1].Value,out ticks)?20f/Math.Max(1,ticks):20;
    }
    [MenuItem("Minecraft/Textures/Open Assignment Report")]
    public static void OpenReport()
    {if(File.Exists(ReportPath))EditorUtility.RevealInFinder(ReportPath);else Debug.Log("Run Assign From Minecraft Folder first.");}
}
