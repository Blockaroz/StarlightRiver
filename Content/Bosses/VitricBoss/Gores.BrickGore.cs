﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Terraria;
using Terraria.ModLoader;
using StarlightRiver.Core;

namespace StarlightRiver.Content.Bosses.VitricBoss
{
	class BrickGode : IOrderedLoadable
	{
		public float Priority => 1;

		public void Load()
		{
			for(int k = 1; k <= 19; k++)
				GoreLoader.AddGoreFromTexture<>(StarlightRiver.Instance, AssetDirectory.VitricBoss + "Gore/Cluster" + k);

			GoreLoader.AddGoreFromTexture<>(StarlightRiver.Instance, AssetDirectory.VitricBoss + "TempleHole");
		}

		public void Unload() { }
	}
}
