using System.Globalization;
using System.Text;

namespace ForumService.Repositories.Helpers;

/// <summary>
/// Provides FPT University campus alias resolution across short codes (e.g. FU-HCM, FU-HL)
/// and accented/unaccented Vietnamese display names for EF Core queries.
/// </summary>
public static class CampusNormalizer
{
    private sealed record CampusDefinition(string[] NormalizedKeys, string[] CandidateValues);

    private static readonly CampusDefinition[] Definitions =
    [
        new(
            NormalizedKeys:
            [
                "hcm", "fu hcm", "fptu hcm", "fpt hcm", "fpt university hcm",
                "ho chi minh", "ho chi minh city", "tp hcm", "tphcm",
                "tp ho chi minh", "thanh pho ho chi minh", "hcmc",
                "fu ho chi minh", "fptu ho chi minh", "sai gon", "saigon"
            ],
            CandidateValues:
            [
                "hcm", "fu-hcm", "fu hcm", "fptu-hcm", "fptu hcm", "fpt-hcm", "fpt hcm", "fpt university hcm",
                "ho chi minh", "hồ chí minh", "ho chi minh city",
                "tp.hcm", "tp hcm", "tp. hcm", "tphcm",
                "tp.hồ chí minh", "tp. hồ chí minh", "tp hồ chí minh",
                "tp. ho chi minh", "tp ho chi minh",
                "thành phố hồ chí minh", "thanh pho ho chi minh",
                "fu ho chi minh", "fu hồ chí minh", "fptu ho chi minh", "fptu hồ chí minh",
                "hcmc", "sài gòn", "sai gon", "saigon",
                "campus hcm", "campus hồ chí minh", "campus ho chi minh",
                "cơ sở hcm", "cơ sở hồ chí minh"
            ]),
        new(
            NormalizedKeys:
            [
                "hn", "hl", "fu hn", "fu hl", "fptu hn", "fptu hl", "fpt hn", "fpt hl",
                "hoa lac", "ha noi", "hanoi", "tp ha noi", "thanh pho ha noi",
                "hoa lac ha noi", "ha noi hoa lac",
                "fu hoa lac", "fptu hoa lac", "fu ha noi", "fptu ha noi",
                "fpt university ha noi", "fpt university hoa lac"
            ],
            CandidateValues:
            [
                "hn", "hl", "fu-hn", "fu hn", "fu-hl", "fu hl",
                "fptu-hn", "fptu hn", "fptu-hl", "fptu hl", "fpt-hn", "fpt hn", "fpt-hl", "fpt hl",
                "hoa lac", "hòa lạc", "hoà lạc",
                "ha noi", "hà nội", "hanoi",
                "tp.hà nội", "tp. hà nội", "tp hà nội", "tp.ha noi", "tp. ha noi", "tp ha noi",
                "thành phố hà nội", "thanh pho ha noi",
                "hoa lac - ha noi", "hòa lạc - hà nội", "hoà lạc - hà nội",
                "hoa lac ha noi", "hòa lạc hà nội", "hoà lạc hà nội",
                "ha noi - hoa lac", "hà nội - hòa lạc", "hà nội - hoà lạc",
                "fu hoa lac", "fu hòa lạc", "fu hoà lạc",
                "fptu hoa lac", "fptu hòa lạc", "fptu hoà lạc",
                "fu ha noi", "fu hà nội", "fptu ha noi", "fptu hà nội",
                "fpt university ha noi", "fpt university hoa lac",
                "campus hòa lạc", "campus hoà lạc", "campus hoa lac",
                "campus hà nội", "campus ha noi",
                "cơ sở hòa lạc", "cơ sở hoà lạc", "cơ sở hà nội"
            ]),
        new(
            NormalizedKeys:
            [
                "dn", "fu dn", "fptu dn", "fpt dn",
                "da nang", "danang", "tp da nang", "thanh pho da nang",
                "fu da nang", "fptu da nang", "fpt university da nang"
            ],
            CandidateValues:
            [
                "dn", "fu-dn", "fu dn", "fptu-dn", "fptu dn", "fpt-dn", "fpt dn",
                "da nang", "đà nẵng", "danang",
                "tp.đà nẵng", "tp. đà nẵng", "tp đà nẵng",
                "tp.da nang", "tp. da nang", "tp da nang",
                "thành phố đà nẵng", "thanh pho da nang",
                "fu da nang", "fu đà nẵng", "fptu da nang", "fptu đà nẵng",
                "fpt university da nang",
                "campus đà nẵng", "campus da nang", "cơ sở đà nẵng"
            ]),
        new(
            NormalizedKeys:
            [
                "ct", "fu ct", "fptu ct", "fpt ct",
                "can tho", "cantho", "tp can tho", "thanh pho can tho",
                "fu can tho", "fptu can tho", "fpt university can tho"
            ],
            CandidateValues:
            [
                "ct", "fu-ct", "fu ct", "fptu-ct", "fptu ct", "fpt-ct", "fpt ct",
                "can tho", "cần thơ", "cantho",
                "tp.cần thơ", "tp. cần thơ", "tp cần thơ",
                "tp.can tho", "tp. can tho", "tp can tho",
                "thành phố cần thơ", "thanh pho can tho",
                "fu can tho", "fu cần thơ", "fptu can tho", "fptu cần thơ",
                "fpt university can tho",
                "campus cần thơ", "campus can tho", "cơ sở cần thơ"
            ]),
        new(
            NormalizedKeys:
            [
                "qn", "fu qn", "fptu qn", "fpt qn",
                "quy nhon", "quynhon", "tp quy nhon", "thanh pho quy nhon",
                "fu quy nhon", "fptu quy nhon", "fpt university quy nhon"
            ],
            CandidateValues:
            [
                "qn", "fu-qn", "fu qn", "fptu-qn", "fptu qn", "fpt-qn", "fpt qn",
                "quy nhon", "quy nhơn", "quynhon",
                "tp.quy nhơn", "tp. quy nhơn", "tp quy nhơn",
                "tp.quy nhon", "tp. quy nhon", "tp quy nhon",
                "thành phố quy nhơn", "thanh pho quy nhon",
                "fu quy nhon", "fu quy nhơn", "fptu quy nhon", "fptu quy nhơn",
                "fpt university quy nhon",
                "campus quy nhơn", "campus quy nhon", "cơ sở quy nhơn"
            ])
    ];

    private static readonly Dictionary<string, string[]> AliasLookup = BuildAliasLookup();

    private static Dictionary<string, string[]> BuildAliasLookup()
    {
        var map = new Dictionary<string, string[]>(StringComparer.Ordinal);
        foreach (var def in Definitions)
        {
            var mergedCandidates = def.NormalizedKeys
                .Concat(def.CandidateValues)
                .Select(c => c.Normalize(NormalizationForm.FormC).ToLowerInvariant())
                .Distinct(StringComparer.Ordinal)
                .ToArray();

            foreach (var key in def.NormalizedKeys)
            {
                map[key] = mergedCandidates;
            }
        }
        return map;
    }

    public static IReadOnlyList<string> GetQueryCandidates(string? campus)
    {
        if (string.IsNullOrWhiteSpace(campus))
        {
            return Array.Empty<string>();
        }

        var key = NormalizeKey(campus);
        if (!string.IsNullOrEmpty(key) && AliasLookup.TryGetValue(key, out var groupCandidates))
        {
            return groupCandidates;
        }

        var rawLower = campus.Trim().Normalize(NormalizationForm.FormC).ToLowerInvariant();
        if (string.IsNullOrEmpty(key) || key == rawLower)
        {
            return [rawLower];
        }

        return [rawLower, key];
    }

    private static string NormalizeKey(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return string.Empty;
        }

        var decomposed = value.Trim().Normalize(NormalizationForm.FormD);
        var sb = new StringBuilder(decomposed.Length);
        var lastWasSpace = false;

        foreach (var ch in decomposed)
        {
            if (CharUnicodeInfo.GetUnicodeCategory(ch) == UnicodeCategory.NonSpacingMark)
            {
                continue;
            }

            var mapped = ch switch
            {
                'đ' or 'Đ' => 'd',
                '-' or '_' or '.' or ',' or '/' or '(' or ')' => ' ',
                _ => char.ToLowerInvariant(ch)
            };

            if (char.IsWhiteSpace(mapped))
            {
                if (sb.Length > 0 && !lastWasSpace)
                {
                    sb.Append(' ');
                    lastWasSpace = true;
                }
            }
            else
            {
                sb.Append(mapped);
                lastWasSpace = false;
            }
        }

        var normalized = sb.ToString().Trim();
        if (normalized.StartsWith("campus ", StringComparison.Ordinal))
        {
            normalized = normalized["campus ".Length..].TrimStart();
        }
        else if (normalized.StartsWith("co so ", StringComparison.Ordinal))
        {
            normalized = normalized["co so ".Length..].TrimStart();
        }

        return normalized;
    }
}
