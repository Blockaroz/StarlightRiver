﻿using StarlightRiver.Core;
using Terraria;
using Terraria.ID;

namespace StarlightRiver.Content.Items.Food
{
	internal class EaterSteak : Ingredient
    {
        public EaterSteak() : base("+3% damage reduction", 900, IngredientType.Main) { }

        public override void SafeSetDefaults() => Item.rare = ItemRarityID.Blue;

        public override void Load()
        {
            StarlightNPC.OnKillEvent += LootEaterSteak;
        }

        public override void BuffEffects(Player Player, float multiplier)
        {
            Player.endurance += 0.03f;
        }

        private void LootEaterSteak(NPC NPC)
        {
            if (NPC.type == NPCID.EaterofSouls && Main.rand.Next(4) == 0)
                Item.NewItem(NPC.GetItemSource_Loot(), NPC.Center, Item.type);
        }
    }
}
