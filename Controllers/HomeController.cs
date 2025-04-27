using System.ComponentModel;
using System.Diagnostics;
using EnumExample.EnumHelpers.Core;
using EnumExample.EnumHelpers;
using EnumExample.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace EnumExample.Controllers
{
    public enum UserColours
    {
        [Description("밝은 핑크")]
        BrightPink = 1,
        [Description("하늘색")]
        SkyBlue = 2,
        [Description("연두색")]
        LightGreen = 3,
        [Description("주황색")]
        Orange = 4,
        [Description("보라색")]
        Purple = 5,
        [Description("진한 핑크색")]
        DarkPink = 6
    }

    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;

        public HomeController(ILogger<HomeController> logger)
        {
            _logger = logger;
        }

        public IActionResult Index()
        {
            // ViewBag에 열거 목록 등록 (빈 항목 포함)
            this.RegisterEnumForView<UserColours>(emptyText: "-- 색상을 선택하세요 --");

            // SelectList로 변환하여 ViewBag에 저장
            ViewBag.ColoursList = CoreEnumHelper.ToCoreSelectListWithEmpty<UserColours>();

            // 모델에 기본값 설정
            var model = new ColourSelectionViewModel
            {
                BasicSelectedColour = UserColours.BrightPink,
                CustomHelperSelectedColour = UserColours.SkyBlue,
                TagHelperSelectedColour = UserColours.LightGreen,
                MultipleColours = new[] { UserColours.BrightPink, UserColours.SkyBlue }
            };

            return View(model);
        }

        [HttpPost]
        public IActionResult Index(ColourSelectionViewModel model)
        {
            if (ModelState.IsValid)
            {
                // 각각의 선택된 색상의 Description 표시
                ViewBag.BasicSelectedColourDescription = EnumHelper.GetDescription(model.BasicSelectedColour);
                ViewBag.CustomHelperSelectedColourDescription = EnumHelper.GetDescription(model.CustomHelperSelectedColour);
                ViewBag.TagHelperSelectedColourDescription = EnumHelper.GetDescription(model.TagHelperSelectedColour);

                // 다중 선택된 색상 처리
                if (model.MultipleColours?.Any() == true)
                {
                    var descriptions = model.MultipleColours.Select(colour => EnumHelper.GetDescription(colour));
                    ViewBag.MultipleColoursDescription = string.Join(", ", descriptions);
                }
            }

            // ViewBag에 다시 설정
            this.RegisterEnumForView<UserColours>(emptyText: "-- 색상을 선택하세요 --");
            ViewBag.ColoursList = CoreEnumHelper.ToCoreSelectListWithEmpty<UserColours>();

            return View(model);
        }

        // API 엔드포인트 - JSON으로 Enum 목록 반환
        [HttpGet("api/colours")]
        public IActionResult GetColours()
        {
            var coloursDict = EnumHelper.GetAllValuesWithDescriptions<UserColours>();

            var result = coloursDict.Select(pair => new
            {
                id = (int)pair.Key,
                name = pair.Key.ToString(),
                description = pair.Value
            });

            return Json(result);
        }
    }
}
