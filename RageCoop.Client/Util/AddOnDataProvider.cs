using GTA;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RageCoop.Client
{
    /// <summary>
    /// Class providing support for addon mods
    /// </summary>
    internal class AddOnDataProvider
    {
        public static int GetMuzzleIndex(Model model)
        {
            switch (model.Hash)
            {
                // f14a2
                case -848721350:
                    return 48;

                // f15e
                case 881261972:
                    return 32;

                // f16c
                case -2051171080:
                    return 25;

                // F22A
                case 2061630439:
                    return 14;

                // f35c
                case -343547392:
                    return 44;

                // mig29a
                case 513887552:
                    return 18;

                // su30sm
                case -733985185:
                    return 34;

                // su33
                case -722216722:
                    return 34;

                // su35s
                case -268602544:
                    return 28;

                // su57
                case 1490050781:
                    return 21;

                default:
                    return -1;
            }
        }
    }
}
