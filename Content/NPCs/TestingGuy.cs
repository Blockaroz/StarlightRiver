﻿using StarlightRiver.Core;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace StarlightRiver.Content.NPCs
{
	internal class TestingGuy : ModNPC
    {
        public override string Texture => AssetDirectory.VitricNpc + "CrystalSlime";

        public override void SetStaticDefaults()
        {
            DisplayName.SetDefault("Test Subject");
            Main.npcFrameCount[npc.type] = 2;
        }

        public override void SetDefaults()
        {
            npc.width = 48;
            npc.height = 32;
            npc.damage = 10;
            npc.defense = 5;
            npc.lifeMax = 2500;
            npc.HitSound = SoundID.NPCHit42;
            npc.DeathSound = SoundID.NPCDeath1;
            npc.value = 99999999f;
            npc.knockBackResist = 0.6f;
            npc.aiStyle = 1;

            npc.GetGlobalNPC<ShieldNPC>().MaxShield = 100;
        }
    }
}