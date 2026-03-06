using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using Microsoft.Xna.Framework;
using Terraria.Audio;
using Terraria.DataStructures;
using BrilliantStone.Content.NPCs.Boss;
using BrilliantStone.Content.Biomes;

namespace BrilliantStone.Content.Items
{
    public class ResentmentOfBrilliantCyst : ModItem
    {
        public override void SetStaticDefaults()
        {
            ItemID.Sets.SortingPriorityBossSpawns[Type] = 12; // 物品栏排序优先级
        }

        public override void SetDefaults()
        {
            Item.width = 20;
            Item.height = 20;
            Item.maxStack = 20;
            Item.value = 100;
            Item.rare = ItemRarityID.Blue;
            Item.useAnimation = 30;
            Item.useTime = 30;
            Item.useStyle = ItemUseStyleID.HoldUp;
            Item.consumable = true;
        }

        public override bool CanUseItem(Player player)
        {
            // 只能在辉石群系使用，且确保同一时间没有其他女皇存在
            return player.InModBiome<BrilliantBiome>() && !NPC.AnyNPCs(ModContent.NPCType<BrilliantCystQueen>());
        }

        public override bool? UseItem(Player player)
        {
            if (Main.netMode != NetmodeID.MultiplayerClient)
            {
                // 生成Boss，位置在玩家上方200像素
                int npcIndex = NPC.NewNPC(new EntitySource_SpawnNPC(),
                    (int)player.Center.X, (int)player.Center.Y - 200,
                    ModContent.NPCType<BrilliantCystQueen>());
                if (Main.netMode == NetmodeID.Server)
                {
                    NetMessage.SendData(MessageID.SyncNPC, -1, -1, null, npcIndex);
                }
            }

            // 播放使用音效
            SoundEngine.PlaySound(SoundID.Roar, player.position);

            return true;
        }

        public override void AddRecipes()
        {
            CreateRecipe()
                .AddIngredient<BrilliantCarapace>(5)
                .AddIngredient<BrilliantStone>(20)
                .AddTile<Content.Tiles.EXPressureFurnaceTile>()   // 确保该物块命名空间正确
                .Register();
        }
    }
}