﻿using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using StarlightRiver.Content.Bosses.VitricBoss;
using StarlightRiver.Content.Dusts;
using StarlightRiver.Core;
using StarlightRiver.Helpers;
using StarlightRiver.Packets;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Terraria;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.ModLoader;
using static Terraria.ModLoader.ModContent;

namespace StarlightRiver.Content.Tiles.Vitric
{
	internal class VitricBossAltar : DummyTile
    {
        public override int DummyType => ProjectileType<VitricBossAltarDummy>();

        public override bool Autoload(ref string name, ref string texture)
        {
            texture = AssetDirectory.VitricTile + name;
            return base.Autoload(ref name, ref texture);
        }

        public override void SetDefaults()
        {
            (this).QuickSetFurniture(5, 7, DustType<Air>(), SoundID.Tink, false, new Color(200, 113, 113), false, false, "Ceiro's Altar");
            minPick = int.MaxValue;
        }

		public override bool CanExplode(int i, int j)
		{
            Tile tile = Framing.GetTileSafely(i, j);

            if (tile.type == TileType<VitricBossAltar>())
                return false;

            return base.CanExplode(i, j);
        }

		public override bool SpawnConditions(int i, int j)
        {
            Tile tile = Framing.GetTileSafely(i, j);
            return tile.frameX % 90 == 0 && tile.frameY == 0;
        }

        public override void SafeNearbyEffects(int i, int j, bool closer)
        {
            Tile tile = Framing.GetTileSafely(i, j);

            if (Main.rand.Next(200) == 0 && tile.frameX < 90 && tile.frameX > 16)
            {
                Vector2 pos = new Vector2(i * 16 + Main.rand.Next(16), j * 16 + Main.rand.Next(16));
                if (Main.rand.NextBool())
                    Dust.NewDustPerfect(pos, DustType<CrystalSparkle>(), Vector2.Zero);
                else
                    Dust.NewDustPerfect(pos, DustType<CrystalSparkle2>(), Vector2.Zero);
            }

            base.SafeNearbyEffects(i, j, closer);
        }

        public override void MouseOver(int i, int j)
        {
            Tile tile = Framing.GetTileSafely(i, j);

            if (tile.frameX >= 90)
            {
                Player player = Main.LocalPlayer;
                player.showItemIcon2 = ItemType<Items.Vitric.GlassIdol>();
                player.noThrow = 2;
                player.showItemIcon = true;
            }
        }

        public override bool NewRightClick(int i, int j)
        {
            Tile tile = (Tile)Framing.GetTileSafely(i, j).Clone();
            Player player = Main.LocalPlayer;

            if (StarlightWorld.HasFlag(WorldFlags.VitricBossOpen) && tile.frameX >= 90 && !NPC.AnyNPCs(NPCType<VitricBoss>()) && (player.ConsumeItem(ItemType<Items.Vitric.GlassIdol>()) || player.HasItem(ItemType<Items.Vitric.GlassIdolPremiumEdition>())))
            {
                int x = i - (tile.frameX - 90) / 18;
                int y = j - tile.frameY / 18;
                SpawnBoss(x, y);
                return true;
            }

            return false;
        }

        public void SpawnBoss(int i, int j)
        {
            if(Main.netMode == NetmodeID.MultiplayerClient)
			{
                var packet = new SpawnNPC(Main.myPlayer, i * 16 + 40, j * 16 + 556, NPCType<VitricBoss>());
                packet.Send(-1, -1, false);

                return;
			}

            int n = NPC.NewNPC(i * 16 + 40, j * 16 + 556, NPCType<VitricBoss>());
            var npc = Main.npc[n];

            if (npc.type == NPCType<VitricBoss>())
                (Dummy(i, j).modProjectile as VitricBossAltarDummy).boss = Main.npc[n];
        }
    }

    internal class VitricBossAltarDummy : Dummy
    {
        private NPC arenaLeft;
        private NPC arenaRight;

        public NPC boss;

        public ref float BarrierProgress => ref projectile.ai[0];
        public ref float CutsceneTimer => ref projectile.ai[1];

        public VitricBossAltarDummy() : base(TileType<VitricBossAltar>(), 80, 112) { }

        public override void SafeSetDefaults()
        {
            projectile.hide = true;
        }

        public override void DrawBehind(int index, List<int> drawCacheProjsBehindNPCsAndTiles, List<int> drawCacheProjsBehindNPCs, List<int> drawCacheProjsBehindProjectiles, List<int> drawCacheProjsOverWiresUI)
        {
            drawCacheProjsBehindNPCsAndTiles.Add(index);
        }

        public override void Collision(Player player)
        {
            Point16 parentPos = new Point16((int)projectile.position.X / 16, (int)projectile.position.Y / 16);
            Tile parent = Framing.GetTileSafely(parentPos.X, parentPos.Y);

            if (parent.frameX == 0 && Abilities.AbilityHelper.CheckDash(player, projectile.Hitbox))
            {
                Main.PlaySound(SoundID.Shatter);
                for (int k = 0; k < 100; k++) Dust.NewDust(projectile.position, projectile.width, projectile.height, DustType<Dusts.GlassGravity>(), 0, 0, 0, default, 1.2f);

                for (int x = parentPos.X; x < parentPos.X + 5; x++)
                    for (int y = parentPos.Y; y < parentPos.Y + 7; y++)
                        Framing.GetTileSafely(x, y).frameX += 90;

                NetMessage.SendTileRange(player.whoAmI, parentPos.X, parentPos.Y, 5, 7, TileChangeType.None);

                CutsceneTimer = 0;
            }
        }

        public override void Update()
        {
            Point16 parentPos = new Point16((int)projectile.position.X / 16, (int)projectile.position.Y / 16);
            Tile parent = Framing.GetTileSafely(parentPos.X, parentPos.Y);

            if (StarlightWorld.HasFlag(WorldFlags.VitricBossOpen) && CutsceneTimer < 660) //should prevent the cutscene from reoccuring?
                CutsceneTimer = 999;

            //This controls spawning the rest of the arena
            if (arenaLeft is null || arenaRight is null || !arenaLeft.active || !arenaRight.active)
            {
                foreach (NPC npc in Main.npc.Where(n => n.active && //reset the arena if one of the sides somehow dies
                 (
                 n.type == NPCType<VitricBackdropLeft>() ||
                 n.type == NPCType<VitricBackdropRight>() ||
                 n.type == NPCType<VitricBossPlatformDown>() ||
                 n.type == NPCType<VitricBossPlatformDownSmall>() ||
                 n.type == NPCType<VitricBossPlatformUp>() ||
                 n.type == NPCType<VitricBossPlatformUpSmall>()
                 )))
                {
                    npc.active = false;
                }

                Vector2 center = projectile.Center + new Vector2(0, 60);
                int timerset = StarlightWorld.HasFlag(WorldFlags.VitricBossOpen) && CutsceneTimer >= 660 ? 360 : 0; //the arena should already be up if it was opened before

                int index = NPC.NewNPC((int)center.X + 352, (int)center.Y, NPCType<VitricBackdropRight>(), 0, timerset);
                arenaRight = Main.npc[index];

                if (StarlightWorld.HasFlag(WorldFlags.VitricBossOpen) && Main.npc[index].modNPC is VitricBackdropRight)
                    (Main.npc[index].modNPC as VitricBackdropRight).SpawnPlatforms(false);

                index = NPC.NewNPC((int)center.X - 352, (int)center.Y, NPCType<VitricBackdropLeft>(), 0, timerset);
                arenaLeft = Main.npc[index];

                if (StarlightWorld.HasFlag(WorldFlags.VitricBossOpen) && Main.npc[index].modNPC is VitricBackdropLeft)
                    (Main.npc[index].modNPC as VitricBackdropLeft).SpawnPlatforms(false);
            }

            if (parent.frameX == 0)
                return;

            if(Main.netMode == NetmodeID.Server && (boss is null || !boss.active))
			{
                for(int k = 0; k < Main.maxNPCs; k++) //TODO: Find a better way to find the boss or the server on sync it from the spawning client
				{
                    var npc = Main.npc[k];
                    if(npc.active && npc.type == NPCType<VitricBoss>())
					{
                        boss = npc;
                        projectile.netUpdate = true;
                        return;
					}
				}
			}

            if (parent.frameX == 90 && !StarlightWorld.HasFlag(WorldFlags.VitricBossOpen))
            {
                Main.LocalPlayer.GetModPlayer<StarlightPlayer>().Shake += 1;
                Dust.NewDust(projectile.Center + new Vector2(-632, projectile.height / 2), 560, 1, DustType<Dusts.Sand>(), 0, Main.rand.NextFloat(-5f, -1f), Main.rand.Next(255), default, Main.rand.NextFloat(1.5f));
                Dust.NewDust(projectile.Center + new Vector2(72, projectile.height / 2), 560, 1, DustType<Dusts.Sand>(), 0, Main.rand.NextFloat(-5f, -1f), Main.rand.Next(255), default, Main.rand.NextFloat(1.5f));

                if (CutsceneTimer > 120 && CutsceneTimer <= 240)
                    Main.musicFade[Main.curMusic] = 1 - ((CutsceneTimer - 120) / 120f);

                if (CutsceneTimer == 180)
                {
                    Helper.PlayPitched("ArenaRise", 0.5f, -0.1f, projectile.Center);
                }

                CutsceneTimer++;
                if (CutsceneTimer > 180)
                {
                    StarlightWorld.Flag(WorldFlags.VitricBossOpen);

                    if (Main.LocalPlayer.GetModPlayer<BiomeHandler>().ZoneGlass)
                    {
                        Main.LocalPlayer.GetModPlayer<StarlightPlayer>().ScreenMovePan = projectile.Center + new Vector2(0, -400);
                        Main.LocalPlayer.GetModPlayer<StarlightPlayer>().ScreenMoveTarget = projectile.Center;
                        Main.LocalPlayer.GetModPlayer<StarlightPlayer>().ScreenMoveTime = VitricBackdropLeft.Risetime + 120;
                    }
                }
            }

            if(CutsceneTimer > 240 && CutsceneTimer < 660)
                Main.musicFade[Main.curMusic] = 0;

            CutsceneTimer++;

            //controls the drawing of the barriers
            if (BarrierProgress < 120 && boss != null && boss.active)
            {
                BarrierProgress++;
                if (BarrierProgress % 3 == 0) Main.LocalPlayer.GetModPlayer<StarlightPlayer>().Shake += 2; //screenshake
                if (BarrierProgress == 119) //hitting the top
                {
                    Main.LocalPlayer.GetModPlayer<StarlightPlayer>().Shake += 15;
                    //for (int k = 0; k < 5; k++) 
                    Helper.PlayPitched("VitricBoss/CeirosPillarImpact", 0.5f, 0, projectile.Center);
                }
            }
            else if (BarrierProgress > 0 && boss != null && !boss.active)
                BarrierProgress--;
        }

        public override void PostDraw(SpriteBatch spriteBatch, Color lightColor) //actually drawing the barriers and item indicator
        {
            Point16 parentPos = new Point16((int)projectile.position.X / 16, (int)projectile.position.Y / 16);
            Tile parent = Framing.GetTileSafely(parentPos.X, parentPos.Y);

            if (parent.frameX >= 90 && !NPC.AnyNPCs(NPCType<VitricBoss>()))
            {
                Texture2D texSkull = GetTexture("StarlightRiver/Assets/Symbol");
                spriteBatch.Draw(texSkull, projectile.Center - Main.screenPosition, null, new Color(255, 100, 100) * (1 - Vector2.Distance(Main.LocalPlayer.Center, projectile.Center) / 200f), 0, texSkull.Size() / 2, 1, 0, 0);
            }

            else if (parent.frameX < 90)
            {
                Texture2D glow = GetTexture(AssetDirectory.VitricTile + "VitricBossAltarGlow");
                spriteBatch.Draw(glow, projectile.position - Main.screenPosition + new Vector2(-1, 7), glow.Frame(), Helper.IndicatorColorProximity(300, 600, projectile.Center), 0, Vector2.Zero, 1, 0, 0);
            }

            //Barriers
            Vector2 center = projectile.Center + new Vector2(0, 56);
            Texture2D tex = GetTexture(AssetDirectory.VitricBoss + "VitricBossBarrier");
            Texture2D tex2 = GetTexture(AssetDirectory.VitricBoss + "VitricBossBarrier2");
            Texture2D texTop = GetTexture(AssetDirectory.VitricBoss + "VitricBossBarrierTop");
            //Color color = new Color(180, 225, 255);

            int off = (int)(BarrierProgress / 120f * tex.Height);
            int off2 = (int)(BarrierProgress / 120f * texTop.Width / 2);

            LightingBufferRenderer.DrawWithLighting(new Rectangle((int)center.X - 790 - (int)Main.screenPosition.X, (int)center.Y - off - 16 - (int)Main.screenPosition.Y, tex.Width, off), tex, new Rectangle(0, 0, tex.Width, off), default);
            LightingBufferRenderer.DrawWithLighting(new Rectangle((int)center.X + 606 - (int)Main.screenPosition.X, (int)center.Y - off - 16 - (int)Main.screenPosition.Y, tex.Width, off), tex2, new Rectangle(0, 0, tex.Width, off), default);

            //left
            LightingBufferRenderer.DrawWithLighting(new Rectangle((int)center.X - 592 - (int)Main.screenPosition.X, (int)center.Y - 1040 - (int)Main.screenPosition.Y, off2, texTop.Height), texTop, new Rectangle(texTop.Width / 2 - off2, 0, off2, texTop.Height), default);

            //right
            LightingBufferRenderer.DrawWithLighting(new Rectangle((int)center.X + 608 - off2 - (int)Main.screenPosition.X, (int)center.Y - 1040 - (int)Main.screenPosition.Y, off2, texTop.Height), texTop, new Rectangle(texTop.Width / 2, 0, off2, texTop.Height), default);

            //spriteBatch.Draw(tex, new Rectangle((int)center.X - 790 - (int)Main.screenPosition.X, (int)center.Y - off - 16 - (int)Main.screenPosition.Y, tex.Width, off),
            //new Rectangle(0, 0, tex.Width, off), color);

            //spriteBatch.Draw(tex, new Rectangle((int)center.X + 606 - (int)Main.screenPosition.X, (int)center.Y - off - 16 - (int)Main.screenPosition.Y, tex.Width, off),
            //new Rectangle(0, 0, tex.Width, off), color);
        }

		public override void SendExtraAI(BinaryWriter writer)
		{
            writer.Write(boss is null ? -1 : boss.whoAmI);
		}

		public override void ReceiveExtraAI(BinaryReader reader)
		{
            int id = reader.ReadInt32();

            if(id >= 0 && id < Main.npc.Length)
                boss = Main.npc[id];
		}
    }
}