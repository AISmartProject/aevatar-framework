using System.Security.Cryptography;
using System.Text;

namespace Aevatar.Core.Abstractions.Extensions;

public static class StringExtensions
{
    public static Guid ToGuid(this string str)
    {
        var hash = MD5.HashData(Encoding.UTF8.GetBytes(str));
        return new Guid(hash);
    }
}