using System.Collections.Generic;
using System.Collections.Specialized;
using System.Web;

namespace OpenDreamShared.Dream;

public static class ParamsHelpers {
    /// <summary>
    /// Enumerates every key and value in a list2params string
    /// </summary>
    /// <returns>Each key in the list, and a <see cref="string"/> for a single value or <see cref="IEnumerable{string}"/> for multiple values</returns>
    public static IEnumerable<KeyValuePair<string, object>> EnumerateParams2List(string queryString) {
        queryString = queryString.Replace(";", "&");
        NameValueCollection query = HttpUtility.ParseQueryString(queryString);

        foreach (string? queryKey in query.AllKeys) {
            string[]? queryValues = query.GetValues(queryKey);

            if (queryValues == null)
                continue;

            if (queryKey == null) { // queryValues contains every value without a key
                foreach (string value in queryValues.Distinct()) {
                    int count = queryValues.Count(item => item == value);

                    if (count > 1) { // "a;a;a" creates list(a=list("","",""))
                        yield return new(value, Enumerable.Repeat(string.Empty, count));
                    } else {
                        yield return new(value, string.Empty);
                    }
                }
            } else {
                yield return new(queryKey, queryValues.Length > 1 ? queryValues : queryValues[0]);
            }
        }
    }
}
