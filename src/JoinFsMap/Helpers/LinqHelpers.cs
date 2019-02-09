using System.Collections.Generic;

namespace JoinFsMap.Helpers {
    public static class LinqHelpers {
        public static T GetOrDefault<T>(this IList<T> arr, int index, T @default = default(T)) {
            if (arr.Count > index && index >= 0) {
                return arr[index];
            }

            return @default;
        }
    }
}