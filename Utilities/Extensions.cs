using System.Text;
using MimeDetective;

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

    public static string GetMimeType(this byte[] file) {
        var inspector = new ContentInspectorBuilder() {
            Definitions = new MimeDetective.Definitions.CondensedBuilder() {
                UsageType = MimeDetective.Definitions.Licensing.UsageType.PersonalNonCommercial
            }.Build()
        }.Build();

        var result = inspector.Inspect(file).OrderByDescending(res => res.Points).FirstOrDefault();
        return result?.Definition.File.MimeType?.ToLower() ?? "application/octet-stream";
    }
    public static string GetMimeType(this Stream file) {
        var inspector = new ContentInspectorBuilder() {
            Definitions = new MimeDetective.Definitions.CondensedBuilder() {
                UsageType = MimeDetective.Definitions.Licensing.UsageType.PersonalNonCommercial
            }.Build()
        }.Build();
        if (!file.CanSeek) throw new InvalidOperationException("Cannot seek stream.");
        file.Position = 0;
        var fileBytes = new byte[file.Length];
        file.ReadExactly(fileBytes, 0, fileBytes.Length);
        var result = inspector.Inspect(fileBytes).OrderByDescending(res => res.Points).FirstOrDefault();
        return result?.Definition.File.MimeType?.ToLower() ?? "application/octet-stream";
    }
}