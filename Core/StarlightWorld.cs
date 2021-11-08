﻿using Microsoft.Xna.Framework;
using StarlightRiver.Content.CustomHooks;
using StarlightRiver.Keys;
using StarlightRiver.NPCs.TownUpgrade;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria.ModLoader.IO;
using Terraria.World.Generation;
using static Terraria.ModLoader.ModContent;

namespace StarlightRiver.Core
{
	//Larger scale TODO: This is slowly becoming a godclass, we should really do something about that
	public partial class StarlightWorld : ModWorld
    {
        private static WorldFlags flags;

        public static Vector2 RiftLocation;

        public static float rottime;

        public static float Chungus;
        public static bool isDemoWorld;

        public static Cutaway cathedralOverlay;
        public static Cutaway templeCutaway;

        //Recipe """database""" TODO: More robust system for this? do we really need one?
        public static List<string> knownRecipies = new List<string>();

        public static int Timer; //I dont know why this is here and really dont want to risk removing it at this point.

        //Voidsmith
        public static Dictionary<string, bool> TownUpgrades = new Dictionary<string, bool>();

        public static List<Vector2> PureTiles = new List<Vector2>();

        public static Rectangle VitricBiome = new Rectangle();

        public static int SquidNPCProgress = 0;
        public static Rectangle SquidBossArena = new Rectangle();

        //Handling Keys
        public static List<Key> Keys = new List<Key>();

        public static List<Key> KeyInventory = new List<Key>();

        //temporary space event stuff
        public static bool spaceEventActive;
        public static float spaceEventFade;

        public static Rectangle VitricBossArena => new Rectangle(VitricBiome.X + VitricBiome.Width / 2 - 59, VitricBiome.Y - 1, 108, 74); //ceiros arena

        public static bool HasFlag(WorldFlags flag) => (flags & flag) != 0;

        public static void Flag(WorldFlags flag)
        {
            flags |= flag;
            NetMessage.SendData(MessageID.WorldData);
        }

        public static void FlipFlag(WorldFlags flag)
        {
            flags ^= flag;
            NetMessage.SendData(MessageID.WorldData);
        }

        public override void NetSend(BinaryWriter writer)
        {
            writer.Write((int)flags);

            WriteRectangle(writer, VitricBiome);
            WriteRectangle(writer, SquidBossArena);

            WriteNPCUpgrades(writer);
        }

        public override void NetReceive(BinaryReader reader)
        {
            flags = (WorldFlags)reader.ReadInt32();

            VitricBiome = ReadRectangle(reader);
            SquidBossArena = ReadRectangle(reader);

            if(CutawayHandler.cutaways.Count == 0)
                CreateCutaways();

            ReadNPCUpgrades(reader);
        }

        private void WriteRectangle(BinaryWriter writer, Rectangle rect)
        {
            writer.Write(rect.X);
            writer.Write(rect.Y);
            writer.Write(rect.Width);
            writer.Write(rect.Height);
        }

        private Rectangle ReadRectangle(BinaryReader reader) => new Rectangle(reader.ReadInt32(), reader.ReadInt32(), reader.ReadInt32(), reader.ReadInt32());

        private void WriteNPCUpgrades(BinaryWriter writer)
		{
            foreach (KeyValuePair<string, bool> upgrade in TownUpgrades)
            {
                writer.Write(upgrade.Key);
                writer.Write(upgrade.Value);
            }
		}

        private void ReadNPCUpgrades(BinaryReader reader)
		{
            foreach (KeyValuePair<string, bool> upgrade in TownUpgrades)
                TownUpgrades[reader.ReadString()] = reader.ReadBoolean();
        }

        private void EbonyGen(GenerationProgress progress)
        {
            progress.Message = "Making the World Impure...";

            for (int k = 0; k < (int)((Main.maxTilesX * Main.maxTilesY) * .0015); k++)
            {
                int x = WorldGen.genRand.Next(0, Main.maxTilesX);
                int y = WorldGen.genRand.Next(0, (int)WorldGen.worldSurfaceHigh);

                if (Main.tile[x, y].type == TileID.Dirt && Math.Abs(x - Main.maxTilesX / 2) >= Main.maxTilesX / 6)
                {
                    //WorldGen.TileRunner(x, y, WorldGen.genRand.Next(10, 11), 1, TileType<OreEbony>(), false, 0f, 0f, false, true);
                }
            }
        }

        public override void PreUpdate()
        {
            Timer++;
            rottime += (float)Math.PI / 60;
            if (rottime >= Math.PI * 2) rottime = 0;
        }

        public override void PostUpdate()
        {
            //Keys
            foreach (Key key in Keys) key.Update();

            if (spaceEventActive)
            {
                if (spaceEventFade <= 1)
                    spaceEventFade += 0.01f;
            }
            else if (spaceEventFade > 0)
                spaceEventFade -= 0.01f;

        }

        public override void Initialize()
        {
            VitricBiome.X = 0;
            VitricBiome.Y = 0;

            flags = default;

            TownUpgrades = new Dictionary<string, bool>();
            knownRecipies = new List<string>();

            //Autoload NPC upgrades
            Mod mod = StarlightRiver.Instance;
            if (mod.Code != null)
            {
                foreach (Type type in mod.Code.GetTypes().Where(t => t.IsSubclassOf(typeof(TownUpgrade))))
                {
                    TownUpgrades.Add(type.Name.Replace("Upgrade", ""), false);
                }
            }

            PureTiles = new List<Vector2>();
        }

        public override TagCompound Save()
        {
            TagCompound tag = new TagCompound();
            foreach (var pair in TownUpgrades)
                tag.Add(pair.Key, pair.Value);

            // TODO why the hell is this throwing Collection was modified?
            return new TagCompound
            {
                ["IsDemoWorld"] = isDemoWorld,
                ["VitricBiomePos"] = VitricBiome.TopLeft(),
                ["VitricBiomeSize"] = VitricBiome.Size(),

                ["SquidNPCProgress"] = SquidNPCProgress,
                ["SquidBossArenaPos"] = SquidBossArena.TopLeft(),
                ["SquidBossArenaSize"] = SquidBossArena.Size(),
                //["PermafrostCenter"] = permafrostCenter,

                [nameof(flags)] = (int)flags,

                [nameof(TownUpgrades)] = tag,

                [nameof(PureTiles)] = PureTiles,

                [nameof(RiftLocation)] = RiftLocation,

                ["Chungus"] = Chungus,

                ["Recipies"] = knownRecipies
            };
        }

        private static bool CheckForSquidArena(Player player)
		{
            return false;
        }

        public static void CreateCutaways()
		{
            templeCutaway = new Cutaway(GetTexture("StarlightRiver/Assets/Backgrounds/TempleCutaway"), new Vector2(VitricBiome.Center.X - 47, VitricBiome.Center.Y + 5) * 16);
            templeCutaway.inside = n => n.GetModPlayer<BiomeHandler>().ZoneGlassTemple;
            CutawayHandler.NewCutaway(templeCutaway);

            cathedralOverlay = new Cutaway(GetTexture("StarlightRiver/Assets/Bosses/SquidBoss/CathedralOver"), SquidBossArena.TopLeft() * 16);
            cathedralOverlay.inside = CheckForSquidArena;
            CutawayHandler.NewCutaway(cathedralOverlay);
        }

        public override void Load(TagCompound tag)
        {
            isDemoWorld = tag.GetBool("IsDemoWorld");
            VitricBiome.X = (int)tag.Get<Vector2>("VitricBiomePos").X;
            VitricBiome.Y = (int)tag.Get<Vector2>("VitricBiomePos").Y;
            VitricBiome.Width = (int)tag.Get<Vector2>("VitricBiomeSize").X;
            VitricBiome.Height = (int)tag.Get<Vector2>("VitricBiomeSize").Y;

            SquidNPCProgress = tag.GetInt("SquidNPCProgress");
            SquidBossArena.X = (int)tag.Get<Vector2>("SquidBossArenaPos").X;
            SquidBossArena.Y = (int)tag.Get<Vector2>("SquidBossArenaPos").Y;
            SquidBossArena.Width = (int)tag.Get<Vector2>("SquidBossArenaSize").X;
            SquidBossArena.Height = (int)tag.Get<Vector2>("SquidBossArenaSize").Y;
            //permafrostCenter = tag.GetInt("PermafrostCenter");

            flags = (WorldFlags)tag.GetInt(nameof(flags));

            TagCompound tag1 = tag.GetCompound(nameof(TownUpgrades));
            Dictionary<string, bool> targetDict = new Dictionary<string, bool>();

            foreach (KeyValuePair<string, object> pair in tag1)
                targetDict.Add(pair.Key, tag1.GetBool(pair.Key));

            TownUpgrades = targetDict;

            PureTiles = (List<Vector2>)tag.GetList<Vector2>(nameof(PureTiles));

            RiftLocation = tag.Get<Vector2>(nameof(RiftLocation));

            Chungus = tag.GetFloat("Chungus");

            Chungus += Main.rand.NextFloat(-0.005f, 0.01f);
            Chungus = MathHelper.Clamp(Chungus, 0, 1);

            knownRecipies = (List<string>)tag.GetList<string>("Recipies");

            foreach (Key key in KeyInventory)
            {
                Content.GUI.KeyInventory.keys.Add(new Content.GUI.KeyIcon(key, false));
            }

            //setup overlays
            if (Main.netMode == NetmodeID.SinglePlayer)
                CreateCutaways();

            Physics.VerletChain.toDraw.Clear();

            DummyTile.dummies.Clear();
        }

        public static void LearnRecipie(string key)
        {
            //this is set up in a way where the stored key should be the same as the display name, there is no real reason to differentiate as the entirety of the data stored is a string list.
            if (!knownRecipies.Contains(key))
            {
                knownRecipies.Add(key);
                CombatText.NewText(Main.LocalPlayer.Hitbox, Color.Tan, "Learned Recipie: " + key);
            }
        }
    }
}