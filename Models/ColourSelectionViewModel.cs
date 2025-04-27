using System.ComponentModel.DataAnnotations;
using System.Collections.Generic;
using EnumExample.Controllers;

namespace EnumExample.Models
{
    public class ColourSelectionViewModel
    {
        [Required(ErrorMessage = "1번 색상을 선택해주세요.")]
        [Display(Name = "1번 선택된 색상")]
        public UserColours BasicSelectedColour { get; set; }

        [Required(ErrorMessage = "2번 색상을 선택해주세요.")]
        [Display(Name = "2번 선택된 색상")]
        public UserColours CustomHelperSelectedColour { get; set; }

        [Required(ErrorMessage = "3번 색상을 선택해주세요.")]
        [Display(Name = "3번 선택된 색상")]
        public UserColours TagHelperSelectedColour { get; set; }

        [Display(Name = "다중 선택된 색상")]
        public IEnumerable<UserColours>? MultipleColours { get; set; }
    }
}