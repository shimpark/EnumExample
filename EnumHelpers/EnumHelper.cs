using System.Collections.Concurrent;
using System.ComponentModel;
using System.Reflection;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace EnumExample.EnumHelpers
{
    /// <summary>
    /// ASP.NET Core 8에 최적화된 Enum 유틸리티 클래스
    /// </summary>
    public static class EnumHelper
    {
        private static readonly ConcurrentDictionary<Enum, string> _descriptionCache = new();
        private static readonly ConcurrentDictionary<Tuple<Type, string>, Enum> _enumCache = new();

        /// <summary>
        /// Enum 값에서 Description 어트리뷰트의 값을 추출합니다.
        /// </summary>
        /// <param name="en">Enum 값</param>
        /// <returns>Description 값 또는 Enum 이름</returns>
        public static string GetDescription(Enum en)
        {
            ArgumentNullException.ThrowIfNull(en);

            return _descriptionCache.GetOrAdd(en, enumValue =>
            {
                Type type = enumValue.GetType();
                string? description = type.GetMember(enumValue.ToString())
                    .FirstOrDefault()
                    ?.GetCustomAttribute<DescriptionAttribute>()
                    ?.Description;

                return description ?? enumValue.ToString();
            });
        }

        /// <summary>
        /// Enum 값에서 Description을 추출하는 확장 메서드
        /// </summary>
        /// <param name="en">Enum 값</param>
        /// <returns>Description 값 또는 Enum 이름</returns>
        public static string ToDescription(this Enum en) => GetDescription(en);

        /// <summary>
        /// 특정 Enum 타입의 모든 값과 설명을 사전 형태로 반환합니다.
        /// </summary>
        /// <typeparam name="TEnum">Enum 타입</typeparam>
        /// <returns>Enum 값과 설명이 담긴 Dictionary</returns>
        public static Dictionary<TEnum, string> GetAllValuesWithDescriptions<TEnum>()
            where TEnum : struct, Enum
        {
            return Enum.GetValues<TEnum>()
                      .ToDictionary(e => e, e => GetDescription(e));
        }

        /// <summary>
        /// 특정 Enum 타입의 모든 설명을 반환합니다.
        /// </summary>
        /// <typeparam name="TEnum">Enum 타입</typeparam>
        /// <returns>설명 목록</returns>
        public static IEnumerable<string> GetAllDescriptions<TEnum>()
            where TEnum : struct, Enum
        {
            return Enum.GetValues<TEnum>()
                      .Select(e => GetDescription(e));
        }

        /// <summary>
        /// 설명(Description)으로부터 해당하는 Enum 값을 찾습니다.
        /// </summary>
        /// <typeparam name="TEnum">Enum 타입</typeparam>
        /// <param name="description">찾을 설명</param>
        /// <returns>해당 설명을 가진 Enum 값</returns>
        public static TEnum GetEnumFromDescription<TEnum>(string description)
            where TEnum : struct, Enum
        {
            ArgumentException.ThrowIfNullOrEmpty(description);

            var key = new Tuple<Type, string>(typeof(TEnum), description);

            return (TEnum)_enumCache.GetOrAdd(key, k =>
            {
                var fields = typeof(TEnum).GetFields()
                    .Where(f => f.FieldType == typeof(TEnum));

                foreach (var field in fields)
                {
                    var enumValue = (TEnum)field.GetValue(null)!;
                    if (GetDescription(enumValue) == description || field.Name == description)
                        return enumValue;
                }

                throw new ArgumentException($"'{description}' 설명에 해당하는 {typeof(TEnum).Name} 값을 찾을 수 없습니다.");
            });
        }

        /// <summary>
        /// 안전하게 설명(Description)으로부터 해당하는 Enum 값을 찾습니다.
        /// </summary>
        /// <typeparam name="TEnum">Enum 타입</typeparam>
        /// <param name="description">찾을 설명</param>
        /// <param name="defaultValue">설명을 찾지 못했을 때 반환할 기본값</param>
        /// <returns>해당 설명을 가진 Enum 값 또는 기본값</returns>
        public static TEnum TryGetEnumFromDescription<TEnum>(string description, TEnum defaultValue)
            where TEnum : struct, Enum
        {
            try
            {
                return GetEnumFromDescription<TEnum>(description);
            }
            catch
            {
                return defaultValue;
            }
        }

        /// <summary>
        /// 특정 Enum 타입의 모든 값을 SelectListItem 목록으로 변환합니다.
        /// 이 메서드는 MVC와 Core 모두에서 사용 가능합니다.
        /// </summary>
        /// <typeparam name="TEnum">Enum 타입</typeparam>
        /// <param name="selectedValue">선택된 값(옵션)</param>
        /// <returns>SelectListItem 목록</returns>
        public static List<SelectListItem> ToSelectList<TEnum>(object? selectedValue = null)
            where TEnum : struct, Enum
        {
            return Enum.GetValues<TEnum>()
                .Select(e => new SelectListItem
                {
                    Text = GetDescription(e),
                    Value = Convert.ToInt32(e).ToString(),
                    Selected = selectedValue != null &&
                             (selectedValue.ToString() == Convert.ToInt32(e).ToString() ||
                              selectedValue.ToString() == e.ToString())
                })
                .ToList();
        }

        /// <summary>
        /// 특정 Enum 타입의 모든 값을 SelectListItem 목록으로 변환합니다.
        /// 이 메서드는 MVC와 Core 모두에서 사용 가능합니다.
        /// </summary>
        /// <typeparam name="TEnum">Enum 타입</typeparam>
        /// <param name="emptyText">빈 선택 텍스트</param>
        /// <param name="selectedValue">선택된 값(옵션)</param>
        /// <returns>SelectListItem 목록</returns>
        public static List<SelectListItem> ToSelectListWithEmpty<TEnum>(
            string emptyText = "-- 선택하세요 --",
            object? selectedValue = null)
            where TEnum : struct, Enum
        {
            var items = new List<SelectListItem>
            {
                new() { Text = emptyText, Value = "" }
            };
            items.AddRange(ToSelectList<TEnum>(selectedValue));
            return items;
        }

        /// <summary>
        /// 캐시를 초기화합니다.
        /// </summary>
        public static void ClearCache()
        {
            _descriptionCache.Clear();
            _enumCache.Clear();
        }

        /// <summary>
        /// 캐시를 미리 로드합니다.
        /// </summary>
        /// <typeparam name="TEnum">Enum 타입</typeparam>
        public static async Task WarmupCacheAsync<TEnum>()
            where TEnum : struct, Enum
        {
            await Task.Run(() =>
            {
                foreach (TEnum value in Enum.GetValues<TEnum>())
                {
                    GetDescription(value);
                }
            });
        }
    }
}
