using RageCoop.Core;
using Newtonsoft.Json;
using System.Data.HashFunction.Jenkins;
using System;

namespace RageCoop.Client.DataDumper
{
    public static class Program
    {
        
        static UInt32 Hash(string key)
        {
            int i = 0;
            uint hash = 0;
            while (i != key.Length)
            {
                hash += key[i++];
                hash += hash << 10;
                hash ^= hash >> 6;
            }
            hash += hash << 3;
            hash ^= hash >> 11;
            hash += hash << 15;
            return hash;
        }
    }
}