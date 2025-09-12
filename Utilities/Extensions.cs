using System.Text;
using Microsoft.AspNetCore.Mvc;
using OpentubeAPI.DTOs;

namespace OpentubeAPI.Utilities;

public static class Extensions {
    public static string ToCSVColumn(this IEnumerable<string> strings) {
        var i = 0;
        var sb = new StringBuilder();
        var strs = strings.ToList();
        foreach (var str in strs) {
            if (i == strs.Count - 1) {
                sb.Append($"\n  {str}\n");
            }
            else {
                sb.Append($"\n  {str},");
            }
            i++;
        }
        return sb.ToString();
    }
    public static string ToCSV(this IEnumerable<string> strings) {
        var i = 0;
        var sb = new StringBuilder();
        foreach (var str in strings) {
            if (i == 0) {
                sb.Append(str);
            }
            else {
                sb.Append(", " + str);
            }
            i++;
        }
        return sb.ToString();
    }
    public static string Capitalize(this string toCapitalize) {
        return toCapitalize.ToUpper()[0] + toCapitalize.Substring(1).ToLower();
    }
    public static Guid ToGuid(this string id) {
        return Guid.TryParse(id, out var guid) ? guid : Guid.Empty;
    }
    public static IQueryable<T> Paginate<T>(this IQueryable<T> query, int page, int pageSize) {
        if (page < 1) page = 1;
        if (pageSize < 1) pageSize = 0;
        return query.Skip((page - 1) * pageSize).Take(pageSize);
    }
}