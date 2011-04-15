﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace NewTake.model.tools
{
    public class BlockRemover : Tool
    {

        public BlockRemover(Player player) : base(player){}

        public override void Use() {

            if (player.currentSelection.HasValue)
            {
                player.world.setBlock(player.currentSelection.Value.position, new Block(BlockType.None));
            }
        
        }

    }
}
