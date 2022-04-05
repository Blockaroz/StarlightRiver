﻿using StarlightRiver.Core;
using Terraria;

namespace StarlightRiver.Content.Tiles.Vitric
{
	internal class VitricFountain : ModFountain
    {
        public VitricFountain() : base("VitricFountainItem", AssetDirectory.VitricTile) { }

        public override void FountainActive(int i, int j, bool closer) =>
            Main.LocalPlayer.GetModPlayer<BiomeHandler>().FountainVitric = true;
    }

    internal class VitricFountainItem : QuickTileItem
    {
        public VitricFountainItem() : base("VitricFountainItem", "Vitric Fountain", "Debug Item", "VitricFountain", texturePath: AssetDirectory.VitricTile) { }
    }
}