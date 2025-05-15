using Microsoft.AspNetCore.Mvc;
using Zainah_Nahool.Models;
using Zainah_Nahool.ViewModels;
using Microsoft.EntityFrameworkCore;


namespace Zainah_Nahool.Controllers
{
    public class CartController : Controller
    {
        public readonly MyDbContext _DB;

        public CartController(MyDbContext db)
        {
            _DB = db;

        }
        public IActionResult Cart()
        {

            var userId = HttpContext.Session.GetInt32("UserId");

            var cart = _DB.Carts
                .Include(c => c.CartItems)
                .ThenInclude(ci => ci.Product)
                .FirstOrDefault(c => c.UserId == userId);

            var viewModel = new List<ProductCartItemView>();

            if (cart != null)
            {
                viewModel = cart.CartItems.Select(ci => new ProductCartItemView
                {
                    ProductId = ci.Id,
                    Name = ci.Product.Name,
                    Image = ci.Product.Image,
                    Price = ci.Product.Price,
                    Quantity = ci.Quantity
                }).ToList();
            }

            return View(viewModel);
        }

        [HttpPost]
        public IActionResult AddToCart(int id)
        {
            var userId = HttpContext.Session.GetInt32("UserId");

            var cart = _DB.Carts
                .Include(c => c.CartItems)
                .FirstOrDefault(c => c.UserId == userId);

            if (cart == null)
            {
                cart = new Cart { UserId = userId };
                _DB.Carts.Add(cart);
                _DB.SaveChanges(); // بدون await
            }

            var cartItem = cart.CartItems.FirstOrDefault(ci => ci.ProductId == id);

            if (cartItem != null)
            {
                cartItem.Quantity += 1;
            }
            else
            {
                cartItem = new CartItem
                {
                    ProductId = id,
                    Quantity = 1,
                    CartId = cart.Id
                };
                _DB.CartItems.Add(cartItem);
            }

            _DB.SaveChanges(); // بدون await

            return RedirectToAction("Cart");


        }


        //public IActionResult RemoveFromCart(int itemId)
        //{
        //    var userId = HttpContext.Session.GetInt32("UserId");

        //    var cart = _DB.Carts
        //        .Include(c => c.CartItems)
        //        .FirstOrDefault(c => c.UserId == userId);

        //    if (cart != null)
        //    {
        //        var cartItem = cart.CartItems.FirstOrDefault(ci => ci.Id == itemId);
        //        if (cartItem != null)
        //        {
        //            _DB.CartItems.Remove(cartItem);
        //            _DB.SaveChanges();
        //        }
        //    }

        //    return RedirectToAction("Cart");
        //}


        [HttpPost]
        public IActionResult UpdateQuantity([FromBody] UpdateQuantityModel model)
        {
            var userId = HttpContext.Session.GetInt32("UserId");

            if (userId == null)
            {
                // خزني رابط الصفحة الي كان بدو يروحها
                //TempData["ReturnUrl"] = Url.Action("ProductDetail", "Shop", new { id = model.Id });

                // إذا المستخدم مش داخل، رجّع له عنوان تسجيل الدخول
                return Json(new { redirect = "/User/Login" });

            }

            // العثور على السلة بناءً على UserId
            var cart = _DB.Carts
                .Include(c => c.CartItems)
                .FirstOrDefault(c => c.UserId == userId);

            if (cart != null)
            {
                // العثور على العنصر في السلة
                var cartItem = cart.CartItems.FirstOrDefault(ci => ci.ProductId == model.ItemId);

                if (cartItem != null && model.Quantity > 0)
                {
                    cartItem.Quantity = model.Quantity;
                    _DB.SaveChanges();  // حفظ التعديلات في قاعدة البيانات
                    return Ok();  // إذا كانت العملية ناجحة
                }
            }

            return Json(new { success = true });  // استجابة عادية إذا كل شيء تمام
        }
        [HttpPost]
        public IActionResult RemoveFromCart(int itemId)
        {
            var userId = HttpContext.Session.GetInt32("UserId");

            var cart = _DB.Carts
                .Include(c => c.CartItems)
                .FirstOrDefault(c => c.UserId == userId);

            if (cart != null)
            {
                var cartItem = cart.CartItems.FirstOrDefault(ci => ci.Id == itemId);
                if (cartItem != null)
                {
                    cart.CartItems.Remove(cartItem);
                    _DB.SaveChanges();
                    return Ok(); // إذا كانت العملية ناجحة
                }
            }

            return BadRequest(); // إذا حدث خطأ
        }

        public IActionResult Payment()
        {
            return View();
        }




        /////////////////// cart nav bar

        //public IActionResult GetCartItems()
        //{
        //    var userId = HttpContext.Session.GetInt32("UserId");

        //    var cart = _DB.Carts
        //        .Include(c => c.CartItems)
        //        .ThenInclude(ci => ci.Product)
        //        .FirstOrDefault(c => c.UserId == userId);

        //    var viewModel = new List<ProductCartItemView>();

        //    if (cart != null)
        //    {
        //        viewModel = cart.CartItems.Select(ci => new ProductCartItemView
        //        {
        //            ProductId = ci.ProductId ?? 0,
        //            Name = ci.Product.Name,
        //            Image = ci.Product.Image,
        //            Price = ci.Product.Price,
        //            Quantity = ci.Quantity
        //        }).ToList();
        //    }

        //    return PartialView("CartPartial", viewModel);
        //}



    }
}
