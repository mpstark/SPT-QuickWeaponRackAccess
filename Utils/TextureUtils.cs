using System.IO;
using UnityEngine;

namespace QuickWeaponRackAccess.Utils
{
    public static class TextureUtils
    {
        public static Texture2D LoadTexture2DFromPath(string path)
        {
            if (!File.Exists(path))
            {
                return null;
            }

            Texture2D tex = new Texture2D(2, 2, TextureFormat.RGBA32, false);
            tex.LoadImage(File.ReadAllBytes(path));

            return tex;
        }
    }
}
