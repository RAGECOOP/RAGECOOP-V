using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using GTA.Math;
using GTA;

namespace RageCoop.Client
{
    internal static class Memory
    {
        #region OFFSET-CONST
        public const int PositionOffset = 144;
        #endregion
        public static Vector3 ReadPosition(this Entity e)
        {
            return ReadVector3(e.MemoryAddress+PositionOffset);
        }
        public unsafe static Vector3 ReadVector3(IntPtr address)
        {
            float* ptr = (float*)address.ToPointer();
            return new Vector3()
            {
                X=*ptr,
                Y=ptr[1],
                Z=ptr[2]
            };
        }
    }
}
