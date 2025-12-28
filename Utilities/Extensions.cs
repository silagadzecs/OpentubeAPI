using System.Text;
using FFMpegCore;
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

    public static Guid ToGuid(this string? id) {
        return Guid.TryParse(id ?? "", out var guid) ? guid : Guid.Empty;
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

    private static FFMpegArgumentOptions WithCustomArgumentIf(this FFMpegArgumentOptions opt, bool condition,
        string argument) {
        if (condition) opt.WithCustomArgument(argument);
        return opt;
    }

    public static FFMpegArgumentOptions AddVAAPIArguments(this FFMpegArgumentOptions opt) {
        // opt.WithCustomArgument("-init_hw_device vaapi=vaapi0:/dev/dri/renderD128");
        // opt.WithCustomArgument("-filter_hw_device vaapi0");
        // opt.WithCustomArgument("-hwaccel vaapi");
        // opt.WithCustomArgument("-hwaccel_device /dev/dri/renderD128");
        // opt.WithCustomArgument("-hwaccel_output_format vaapi");
        opt.WithCustomArgument("-vaapi_device /dev/dri/renderD128");
        return opt;
    }

    public static FFMpegArgumentOptions AddBitrateArguments(this FFMpegArgumentOptions opt, int videoHeight) {
        var resolutions = new[] { 2160, 1440, 1080, 720, 480, 360, 240, 144 };
        resolutions = resolutions.Where(res => videoHeight >= res).ToArray();
        if (resolutions.Length == 0) return opt;
        var filterArgument =
            $"-filter_complex \"[0:v]split={resolutions.Length}{string.Join("", resolutions.Select(WrapResWithBrackets))};";
        foreach (var res in resolutions) {
            filterArgument +=
                $"{WrapResWithBrackets(res)}scale=w=-2:h={res}:flags=bicublin,format=nv12,hwupload{WrapStrWithBrackets(res + "out")}" +
                $"{(res == resolutions.Last() ? '"' : ';')}";
        }

        opt.WithCustomArgument(filterArgument);
        foreach (var res in resolutions) {
            opt.WithCustomArgument(
                $"-map {WrapStrWithBrackets(res + "out")} -c:v h264_vaapi -qp 20 -g 48 -keyint_min 48");
        }

        return opt;

        string WrapResWithBrackets(int res) {
            return $"[{res}]";
        }

        string WrapStrWithBrackets(string str) {
            return $"[{str}]";
        }
    }

    public static FFMpegArgumentOptions AddDASHArguments(this FFMpegArgumentOptions opt) {
        opt.WithCustomArgument("-f dash");
        opt.WithCustomArgument("-seg_duration 2");
        opt.WithCustomArgument("-use_timeline 1");
        opt.WithCustomArgument("-use_template 1");
        opt.WithCustomArgument("-adaptation_sets \"id=0,streams=v id=1,streams=a\"");
        return opt;
    }

    public static void DeleteNonEmptyDir(string dir) {
        var files = Directory.EnumerateFiles(dir);
        foreach (var file in files) {
            File.Delete(file);
        }

        Directory.Delete(dir);
    }

    public static async Task<bool> CatchCancellaton(this Func<Task> asyncFunc, Action callback = null!) {
        try {
            await asyncFunc();
            return false;
        }
        catch (OperationCanceledException) {
            callback?.Invoke();
            return true;
        }
    }

    public static async Task<bool> CatchCancellation(this Task task, Action? callback = null) {
        try {
            await task;
            return false;
        }
        catch (OperationCanceledException) {
            callback?.Invoke();
            return true;
        }
    }

    public static async Task<T?>
        CatchCancellation<T>(this Task<T> task, Action? callback = null) {
        try {
            await task;
            return task.Result;
        }
        catch (OperationCanceledException) {
            callback?.Invoke();
            return default(T);
        }
    }
}