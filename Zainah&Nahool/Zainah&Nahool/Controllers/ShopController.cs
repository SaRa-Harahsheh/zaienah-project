using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Internal;
using Zainah_Nahool.Models;
using Zainah_Nahool.ViewModels;

namespace Zainah_Nahool.Controllers
{
    public class ShopController : Controller
    {
        private readonly MyDbContext _DB;

        public ShopController(MyDbContext db)
        {
            _DB = db;
        }
        public IActionResult AllProduct()
        {
            var products = _DB.Products.ToList();
            return View(products);
        }

        public IActionResult ProductDetail(int id)
        {
            //var product = _DB.Products.FirstOrDefault(p=> p.Id==id);

            var product = _DB.Products
        .Include(p => p.Feedbacks)
            .ThenInclude(f => f.User) // ضروري لو بدك تعرضي اسم المستخدم اللي كتب الفيدباك
        .FirstOrDefault(p => p.Id == id);


            if (product == null)
            { 
                return RedirectToAction("AllProduct");
            }

            var model = new ProductFeedbackViewModel
            {
                Id = product.Id,
                Name = product.Name,
                Description = product.Description,
                Image = product.Image,
                FeedbackCount = product.Feedbacks?.Count ?? 0, // حساب عدد المراجعات

                Feedbacks = product.Feedbacks?.Select(f => new FeedbackItemViewModel
                {
                    Name = f.User.FirstName,
                    Email = f.User.Email,
                    Comment = f.Comment,
                    Rating = f.Rating??0,
                    DatePosted = f.CreatedAt ?? DateTime.Now
                }).ToList() ?? new List<FeedbackItemViewModel>()
            };

            return View(model);
        }



        public IActionResult AddFeedback()
        {
            var model = new AddFeedbackViewModel
            {
                Products = _DB.Products.ToList(), // جلب المنتجات من قاعدة البيانات
                Feedback = new Feedback() // تهيئة نموذج الفيدباك (فارغ)
            };

            return View(model); // إرسال الـ ViewModel إلى الصفحة
        }


        [HttpPost]
        public IActionResult AddFeedback(ProductFeedbackViewModel model)
        {
            var userId = HttpContext.Session.GetInt32("UserId");

            if (userId == null)
            {
                // خزني رابط الصفحة الي كان بدو يروحها
                TempData["ReturnUrl"] = Url.Action("ProductDetail", "Shop", new { id = model.Id });

                return RedirectToAction("login", "User");
            }


            //if (ModelState.IsValid)  // إذا كان النموذج صالحًا
            //{
                var product = _DB.Products.FirstOrDefault(p => p.Id == model.Id);

                if (product != null)
                {
                    var feedback = new Feedback
                    {
                        UserId = userId.Value,  // استرجاع معرف المستخدم من السيشن
                        ProductId = model.Id,  // استرجاع معرف المنتج من الفيدباك
                        Rating = model.Feedbacks.Select(x=>x.Rating).FirstOrDefault(),  // استرجاع التقييم من الفيدباك
                        Comment = model.Feedbacks.Select(x => x.Comment).FirstOrDefault(),   // استرجاع التعليق من الفيدباك
                        CreatedAt = DateTime.Now  // وقت إنشاء الفيدباك
                    };

                    _DB.Feedbacks.Add(feedback);

                    _DB.SaveChanges();

                    TempData["SuccessMessage"] = "تم إضافة الفيدباك بنجاح!";
                    return RedirectToAction("ProductDetail", new { id = model.Id });
                }
                else
                {

                    ModelState.AddModelError("", "المنتج غير موجود.");
                }
            //}
                // إذا كان هناك خطأ في النموذج، نقوم بإعادة تحميل الصفحة مع المنتجات والفيدباك
                //model.Products = _DB.Products.ToList();
            return RedirectToAction("AllProduct");
        }

    }
}
