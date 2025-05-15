using Microsoft.AspNetCore.Mvc.Rendering;
using Zainah_Nahool.Models;

namespace Zainah_Nahool.ViewModels
{
    public class ProductViewModel
    {
        public Product Product { get; set; }
        public List<SelectListItem> Categories { get; set; }
    }
}
