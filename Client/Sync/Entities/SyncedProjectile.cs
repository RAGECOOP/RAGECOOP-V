using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using GTA;

namespace RageCoop.Client.Sync.Entities
{
    internal class SyncedProjectile:SyncedEntity
    {
        public bool Exploded { get; set; } = false;
        public Projectile MainProjectile { get; set; }
        public WeaponHash Hash { get; set; }
        private WeaponAsset Asset;
        public override void Update()
        {
            // Check if all data avalible
            if (!IsReady) { return; }

            // Skip update if no new sync message has arrived.
            if (!NeedUpdate)
            {
                return;
            }

            if (Exploded)
            {
                if (Exploded)
                {
                    if(MainProjectile != null && MainProjectile.Exists())
                    {
                        MainProjectile.Explode();
                        return;
                    }
                }
            }
            else
            {
                if (MainProjectile != null && MainProjectile.Exists())
                {
                    MainProjectile.Position=Position+Velocity*Networking.Latency;
                    MainProjectile.Velocity=Velocity;
                }
                else
                {
                    CreateProjectile();
                }
            }
        }

        private void CreateProjectile()
        {
            throw new NotImplementedException();
        }
    }
}
