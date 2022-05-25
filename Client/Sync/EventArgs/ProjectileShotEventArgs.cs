using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using GTA;

namespace RageCoop.Client
{
    internal class ProjectileShotEventArgs:EventArgs
    {
        public bool IsMine { get; set; }
        public Projectile Projectile { get; set; }

        public SyncedPed Owner { get; set; }
    }
}
