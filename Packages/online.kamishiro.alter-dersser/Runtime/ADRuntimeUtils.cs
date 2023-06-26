using System.Linq;
using System.Security.Cryptography;
using System.Text;
using UnityEngine;
using VRC.SDK3.Avatars.Components;

namespace online.kamishiro.alterdresser
{
    internal class ADRuntimeUtils
    {
        internal static string GetRelativePath(Transform parent, Transform child)
        {
            string path = "";
            Transform temp = child;
            while (temp != parent)
            {
                path = temp.name + "/" + path;
                temp = temp.parent;
                if (temp == null)
                {
                    break;
                }
            }
            if (path.EndsWith("/")) path = path.Substring(0, path.Length - 1);
            return path;
        }

        internal static VRCAvatarDescriptor GetAvatar(Transform c)
        {
            if (c == null) return null;
            Transform p = c.transform;
            while (p)
            {
                if (p.TryGetComponent(out VRCAvatarDescriptor avatar)) return avatar;
                p = p.parent;
            }
            return null;
        }

        internal static string GenerateID(Object obj)
        {
            SHA256 sha256 = SHA256.Create();
            byte[] encoding = Encoding.UTF8.GetBytes(System.Convert.ToString(obj.GetInstanceID(), 16));
            byte[] hash = sha256.ComputeHash(encoding);
            string hashed = string.Concat(hash.Select(b => $"{b:x2}")).ToUpper();
            return hashed.Substring(0, 5);
        }
    }
}
