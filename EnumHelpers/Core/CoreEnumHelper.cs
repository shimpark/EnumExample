using Microsoft.AspNetCore.Html;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Razor.TagHelpers;

namespace EnumExample.EnumHelpers.Core
{
    /// <summary>
    /// ASP.NET Core 8 전용 Enum 확장 기능
    /// </summary>
    public static class CoreEnumHelper
    {
        /// <summary>
        /// Enum을 ASP.NET Core의 SelectList로 변환합니다.
        /// </summary>
        /// <typeparam name="TEnum">Enum 타입</typeparam>
        /// <param name="selectedValue">선택된 값(옵션)</param>
        /// <returns>SelectList 객체</returns>
        public static SelectList ToCoreSelectList<TEnum>(object? selectedValue = null)
            where TEnum : struct, Enum
        {
            var items = EnumHelper.ToSelectList<TEnum>();
            return new SelectList(items, "Value", "Text", selectedValue);
        }

        /// <summary>
        /// Enum을 ASP.NET Core의 MultiSelectList로 변환합니다.
        /// </summary>
        /// <typeparam name="TEnum">Enum 타입</typeparam>
        /// <param name="selectedValues">선택된 값들(옵션)</param>
        /// <returns>MultiSelectList 객체</returns>
        public static MultiSelectList ToCoreMultiSelectList<TEnum>(IEnumerable<int>? selectedValues = null)
            where TEnum : struct, Enum
        {
            var items = EnumHelper.ToSelectList<TEnum>();
            return new MultiSelectList(items, "Value", "Text", selectedValues);
        }

        /// <summary>
        /// Enum을 ASP.NET Core의 SelectList로 변환합니다.
        /// </summary>
        /// <typeparam name="TEnum">Enum 타입</typeparam>
        /// <param name="emptyText">빈 항목의 텍스트</param>
        /// <param name="selectedValue">선택된 값(옵션)</param>
        /// <returns>SelectList 객체</returns>
        public static SelectList ToCoreSelectListWithEmpty<TEnum>(
            string emptyText = "-- 선택하세요 --",
            object? selectedValue = null)
            where TEnum : struct, Enum
        {
            var items = new List<SelectListItem>
            {
                new() { Text = emptyText, Value = "" }
            };
            items.AddRange(EnumHelper.ToSelectList<TEnum>());
            return new SelectList(items, "Value", "Text", selectedValue);
        }

        /// <summary>
        /// 현재 컨텍스트에서 Enum 값의 HTML 표현을 생성합니다.
        /// </summary>
        /// <param name="helper">HTML 헬퍼</param>
        /// <param name="enumValue">Enum 값</param>
        /// <returns>HTML 문자열</returns>
        public static IHtmlContent EnumDisplayFor(this IHtmlHelper helper, Enum enumValue)
        {
            ArgumentNullException.ThrowIfNull(enumValue);
            return new HtmlString(EnumHelper.GetDescription(enumValue));
        }

        /// <summary>
        /// EnumDropDownListFor 헬퍼 메서드 - Razor 뷰에서 사용
        /// </summary>
        public static IHtmlContent EnumDropDownListFor<TModel, TEnum>(
            this IHtmlHelper<TModel> htmlHelper,
            System.Linq.Expressions.Expression<Func<TModel, TEnum>> expression,
            object? htmlAttributes = null,
            string? emptyText = null)
            where TEnum : struct, Enum
        {
            var selectList = emptyText != null
                ? EnumHelper.ToSelectListWithEmpty<TEnum>(emptyText)
                : EnumHelper.ToSelectList<TEnum>();

            return htmlHelper.DropDownListFor(expression, selectList, htmlAttributes);
        }

        /// <summary>
        /// EnumCheckBoxListFor 헬퍼 메서드 - Razor 뷰에서 사용
        /// </summary>
        public static IHtmlContent EnumCheckBoxListFor<TModel, TEnum>(
            this IHtmlHelper<TModel> htmlHelper,
            System.Linq.Expressions.Expression<Func<TModel, IEnumerable<TEnum>?>> expression,
            object? htmlAttributes = null)
            where TEnum : struct, Enum
        {
            var modelExpressionProvider = htmlHelper.ViewContext.HttpContext.RequestServices
                .GetRequiredService<ModelExpressionProvider>();

            var modelExpression = modelExpressionProvider.CreateModelExpression(
                htmlHelper.ViewData, expression);

            var selectedValues = modelExpression.Model as IEnumerable<TEnum>;
            var items = EnumHelper.ToSelectList<TEnum>();

            var sb = new System.Text.StringBuilder();
            sb.AppendLine("<div class=\"enum-checkbox-list\">");

            foreach (var item in items)
            {
                var id = $"{modelExpression.Name}_{item.Value}";
                var isChecked = selectedValues?.Any(v => Convert.ToInt32(v).ToString() == item.Value) ?? false;
                var checkedAttr = isChecked ? " checked=\"checked\"" : "";

                sb.AppendLine($"<div class=\"form-check\">");
                sb.AppendLine($"    <input type=\"checkbox\" class=\"form-check-input\" id=\"{id}\" name=\"{modelExpression.Name}\" value=\"{item.Value}\"{checkedAttr} />");
                sb.AppendLine($"    <label class=\"form-check-label\" for=\"{id}\">{item.Text}</label>");
                sb.AppendLine("</div>");
            }

            sb.AppendLine("</div>");
            return new HtmlString(sb.ToString());
        }
    }

    /// <summary>
    /// ASP.NET Core에서 사용할 Enum 태그 헬퍼 클래스
    /// </summary>
    [HtmlTargetElement("enum-select", Attributes = ForAttributeName + "," + EnumTypeAttributeName, TagStructure = TagStructure.WithoutEndTag)]
    public class EnumSelectTagHelper : TagHelper
    {
        private const string ForAttributeName = "asp-for";
        private const string EnumTypeAttributeName = "asp-enum-type";
        private const string EmptyTextAttributeName = "asp-empty-text";
        private const string SearchableAttributeName = "asp-searchable";

        [HtmlAttributeName(ForAttributeName)]
        public ModelExpression For { get; set; } = null!;

        [HtmlAttributeName(EnumTypeAttributeName)]
        public Type EnumType { get; set; } = null!;

        [HtmlAttributeName(EmptyTextAttributeName)]
        public string? EmptyText { get; set; }

        [HtmlAttributeName(SearchableAttributeName)]
        public bool Searchable { get; set; }

        [ViewContext]
        public ViewContext ViewContext { get; set; } = null!;

        public override void Process(TagHelperContext context, TagHelperOutput output)
        {
            if (EnumType == null || !EnumType.IsEnum)
            {
                throw new ArgumentException("지정된 타입이 Enum이 아닙니다.", nameof(EnumType));
            }

            output.TagName = "select";
            output.TagMode = TagMode.StartTagAndEndTag;

            // class 속성 처리
            var cssClass = "form-select";
            if (context.AllAttributes.ContainsName("class"))
            {
                cssClass = $"{context.AllAttributes["class"].Value} {cssClass}";
            }
            output.Attributes.SetAttribute("class", cssClass);

            // id 속성 처리
            var idAttribute = context.AllAttributes["id"]?.Value;
            output.Attributes.SetAttribute("id", idAttribute?.ToString() ?? For.Name.Replace(".", "_"));

            // name 속성 설정
            output.Attributes.SetAttribute("name", For.Name);

            // 검색 가능 속성 추가
            if (Searchable)
            {
                output.Attributes.SetAttribute("data-searchable", "true");
            }

            var currentValue = For.Model?.ToString();

            // 빈 항목 추가
            if (!string.IsNullOrEmpty(EmptyText))
            {
                var emptyOption = new TagBuilder("option");
                emptyOption.Attributes["value"] = "";
                emptyOption.InnerHtml.Append(EmptyText);
                output.Content.AppendHtml(emptyOption);
            }

            // Enum 값들을 옵션으로 추가
            foreach (var enumValue in Enum.GetValues(EnumType))
            {
                var option = new TagBuilder("option");
                var value = Convert.ToInt32(enumValue).ToString();
                var text = EnumHelper.GetDescription((Enum)enumValue);

                option.Attributes["value"] = value;
                option.Attributes["data-description"] = text;

                if (currentValue != null &&
                    (currentValue == value || currentValue == enumValue.ToString()))
                {
                    option.Attributes["selected"] = "selected";
                }

                option.InnerHtml.Append(text);
                output.Content.AppendHtml(option);
            }
        }
    }

    /// <summary>
    /// ASP.NET Core에서 사용할 Enum 확장 메서드 제공자
    /// </summary>
    public static class EnumExtensionsForCore
    {
        /// <summary>
        /// Enum을 HttpContext.Items에 등록하여 View에서 사용할 수 있게 합니다.
        /// </summary>
        public static void RegisterEnumForPage<TEnum>(this Controller controller, string? key = null)
            where TEnum : struct, Enum
        {
            string itemKey = key ?? typeof(TEnum).Name;
            controller.HttpContext.Items[itemKey] = EnumHelper.ToSelectList<TEnum>();
        }

        /// <summary>
        /// Enum을 ViewBag에 등록하여 View에서 사용할 수 있게 합니다.
        /// </summary>
        public static void RegisterEnumForView<TEnum>(
            this Controller controller,
            string? propertyName = null,
            string? emptyText = null)
            where TEnum : struct, Enum
        {
            string name = propertyName ?? typeof(TEnum).Name;
            controller.ViewData[name] = emptyText != null
                ? CoreEnumHelper.ToCoreSelectListWithEmpty<TEnum>(emptyText)
                : CoreEnumHelper.ToCoreSelectList<TEnum>();
        }
    }
}
