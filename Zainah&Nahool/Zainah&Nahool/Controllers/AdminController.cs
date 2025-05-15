using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Zainah_Nahool.Models;
using System.Linq;
using System.Threading.Tasks;
using Zainah_Nahool.ViewModels;
using Microsoft.AspNetCore.Hosting;
using System.IO;
using System;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace Zainah_Nahool.Controllers
{
    public class AdminController : Controller
    {
        private readonly MyDbContext _context;
        private readonly IWebHostEnvironment _webHostEnvironment;

        public AdminController(MyDbContext context, IWebHostEnvironment webHostEnvironment)
        {
            _context = context;
            _webHostEnvironment = webHostEnvironment;
        }

        public async Task<IActionResult> Dashboard()
        {
            // إحصائيات سريعة
            var totalSales = await _context.Orders
                .Where(o => o.Status == "مكتمل")
                .SumAsync(o => o.TotalAmount);

            var totalOrders = await _context.Orders.CountAsync();
            var totalUsers = await _context.Users.CountAsync();
            var totalProducts = await _context.Products.CountAsync();

            // المبيعات الشهرية
            var monthlySales = await _context.Orders
                .Where(o => o.Status == "مكتمل" && o.OrderDate.HasValue && o.OrderDate >= System.DateTime.Now.AddMonths(-6))
                .GroupBy(o => new { Year = o.OrderDate.Value.Year, Month = o.OrderDate.Value.Month })
                .Select(g => new MonthlySalesData
                {
                    Month = g.Key.Month,
                    Year = g.Key.Year,
                    Total = g.Sum(o => o.TotalAmount ?? 0)  // إذا كانت القيمة null نستخدم 0
                })
                .OrderBy(x => x.Year)
                .ThenBy(x => x.Month)
                .ToListAsync();



            // توزيع المبيعات حسب المنتج
          var salesByProduct = await _context.OrderItems
    .Include(oi => oi.Product)
    .GroupBy(oi => oi.Product.Name)
    .Select(g => new ProductSalesData
    {
        ProductName = g.Key,
        TotalSales = g.Sum(oi => oi.Quantity * oi.Price)
    })
    .OrderByDescending(x => x.TotalSales)
    .Take(3)
    .ToListAsync();


            // الطلبات الأخيرة
            var recentOrders = await _context.Orders
        .Include(o => o.User)
        .OrderByDescending(o => o.OrderDate)
        .Take(5)
        .Select(o => new RecentOrderData
        {
            OrderId = o.Id,  // التأكد من أن الاسم هنا هو OrderId وليس Id
            CustomerName = o.User.FirstName,
            OrderDate = o.OrderDate,
            Status = o.Status,
            TotalAmount = o.TotalAmount
        })
        .ToListAsync();


            // المنتجات الأكثر مبيعاً
            var topProducts = await _context.OrderItems
          .Include(oi => oi.Product)
          .GroupBy(oi => new { oi.Product.Id, oi.Product.Name, oi.Product.Price, oi.Product.Image, oi.Product.StockQuantity })
          .Select(g => new TopProductData
          {
              Id = g.Key.Id,
              Name = g.Key.Name,
              Price = g.Key.Price,
              Image = g.Key.Image,
              StockQuantity = g.Key.StockQuantity,
              TotalSales = g.Sum(oi => oi.Quantity)
          })
          .OrderByDescending(x => x.TotalSales)
          .Take(5)
          .ToListAsync();


            // حساب نسب النمو للمبيعات
            var lastMonthSales = await _context.Orders
                .Where(o => o.Status == "مكتمل" && o.OrderDate >= System.DateTime.Now.AddMonths(-2) && o.OrderDate < System.DateTime.Now.AddMonths(-1))
                .SumAsync(o => o.TotalAmount);

            var currentMonthSales = await _context.Orders
                .Where(o => o.Status == "مكتمل" && o.OrderDate >= System.DateTime.Now.AddMonths(-1))
                .SumAsync(o => o.TotalAmount);

            // تحويل القيمة من decimal? إلى double باستخدام Convert.ToDouble
            var salesGrowth = lastMonthSales.HasValue && lastMonthSales.Value > 0
                ? ((Convert.ToDouble(currentMonthSales) - Convert.ToDouble(lastMonthSales.Value)) / Convert.ToDouble(lastMonthSales.Value)) * 100
                : 0;

            // حساب نسب النمو للطلبات
            var lastMonthOrders = await _context.Orders
                .Where(o => o.OrderDate >= System.DateTime.Now.AddMonths(-2) && o.OrderDate < System.DateTime.Now.AddMonths(-1))
                .CountAsync();

            var currentMonthOrders = await _context.Orders
                .Where(o => o.OrderDate >= System.DateTime.Now.AddMonths(-1))
                .CountAsync();

            // تحويل القيمة من int إلى double باستخدام (double)
            var ordersGrowth = lastMonthOrders > 0
                ? ((currentMonthOrders - lastMonthOrders) / (double)lastMonthOrders) * 100
                : 0;


            var viewModel = new DashboardViewModel
            {
                TotalSales = totalSales,
                TotalOrders = totalOrders,
                TotalUsers = totalUsers,
                TotalProducts = totalProducts,
                MonthlySales = monthlySales,
                SalesByProduct = salesByProduct,
                RecentOrders = recentOrders,
                TopProducts = topProducts,
                SalesGrowth = salesGrowth,
                OrdersGrowth = ordersGrowth
            };

            return View(viewModel);
        }

        // Products Management
        public async Task<IActionResult> Products()
        {
            var products = await _context.Products
                .Include(p => p.Category)
                .OrderByDescending(p => p.Id)
                .ToListAsync();

            return View(products);
        }

        public IActionResult CreateProduct()
        {
            ViewBag.Categories = new SelectList(_context.Categories, "Id", "Name");
            return View(new Product());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateProduct(Product product, IFormFile image)
        {
            if (ModelState.IsValid)
            {
                if (image != null && image.Length > 0)
                {
                    string uploadsFolder = Path.Combine(_webHostEnvironment.WebRootPath, "images/shop");
                    if (!Directory.Exists(uploadsFolder))
                    {
                        Directory.CreateDirectory(uploadsFolder);
                    }

                    string uniqueFileName = Guid.NewGuid().ToString() + "_" + image.FileName;
                    string filePath = Path.Combine(uploadsFolder, uniqueFileName);

                    using (var fileStream = new FileStream(filePath, FileMode.Create))
                    {
                        await image.CopyToAsync(fileStream);
                    }

                    product.Image = uniqueFileName;
                }
                else
                {
                    product.Image = "shop-1.jpg"; // صورة افتراضية
                }

                _context.Add(product);
                await _context.SaveChangesAsync();

                TempData["SuccessMessage"] = "تم إضافة المنتج بنجاح";
                return RedirectToAction(nameof(Products));
            }

            ViewBag.Categories = new SelectList(_context.Categories, "Id", "Name");
            return View(product);
        }

        public async Task<IActionResult> EditProduct(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var product = await _context.Products.FindAsync(id);
            if (product == null)
            {
                return NotFound();
            }

            ViewBag.Categories = new SelectList(_context.Categories, "Id", "Name");
            return View(product);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditProduct(int id, Product product, IFormFile image)
        {
            if (id != product.Id)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    var existingProduct = await _context.Products.FindAsync(id);
                    if (existingProduct == null)
                    {
                        return NotFound();
                    }

                    if (image != null && image.Length > 0)
                    {
                        // حذف الصورة القديمة إذا كانت موجودة
                        if (!string.IsNullOrEmpty(existingProduct.Image) && existingProduct.Image != "shop-1.jpg")
                        {
                            string oldImagePath = Path.Combine(_webHostEnvironment.WebRootPath, "images/shop", existingProduct.Image);
                            if (System.IO.File.Exists(oldImagePath))
                            {
                                System.IO.File.Delete(oldImagePath);
                            }
                        }

                        // حفظ الصورة الجديدة
                        string uploadsFolder = Path.Combine(_webHostEnvironment.WebRootPath, "images/shop");
                        string uniqueFileName = Guid.NewGuid().ToString() + "_" + image.FileName;
                        string filePath = Path.Combine(uploadsFolder, uniqueFileName);

                        using (var fileStream = new FileStream(filePath, FileMode.Create))
                        {
                            await image.CopyToAsync(fileStream);
                        }

                        existingProduct.Image = uniqueFileName;
                    }

                    // تحديث باقي البيانات
                    existingProduct.Name = product.Name;
                    existingProduct.Description = product.Description;
                    existingProduct.Price = product.Price;
                    existingProduct.StockQuantity = product.StockQuantity;
                    existingProduct.CategoryId = product.CategoryId;
                    existingProduct.IsActive = product.IsActive;

                    _context.Update(existingProduct);
                    await _context.SaveChangesAsync();
                    TempData["SuccessMessage"] = "تم تحديث المنتج بنجاح";
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!ProductExists(product.Id))
                    {
                        return NotFound();
                    }
                    else
                    {
                        throw;
                    }
                }
                return RedirectToAction(nameof(Products));
            }

            ViewBag.Categories = new SelectList(_context.Categories, "Id", "Name");
            return View(product);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteProduct(int id)
        {
            var product = await _context.Products.FindAsync(id);
            if (product == null)
            {
                return NotFound();
            }

            // حذف صورة المنتج إذا كانت موجودة
            if (!string.IsNullOrEmpty(product.Image) && product.Image != "shop-1.jpg")
            {
                string imagePath = Path.Combine(_webHostEnvironment.WebRootPath, "images/shop", product.Image);
                if (System.IO.File.Exists(imagePath))
                {
                    System.IO.File.Delete(imagePath);
                }
            }

            _context.Products.Remove(product);
            await _context.SaveChangesAsync();
            TempData["SuccessMessage"] = "تم حذف المنتج بنجاح";
            return RedirectToAction(nameof(Products));
        }

        private bool ProductExists(int id)
        {
            return _context.Products.Any(e => e.Id == id);
        }

        // Feedback Management
        public async Task<IActionResult> Feedbacks()
        {
            var feedbacks = await _context.Feedbacks
                .Include(f => f.User)
                .Include(f => f.Product)
                .OrderByDescending(f => f.CreatedAt)
                .ToListAsync();

            return View(feedbacks);
        }

        public async Task<IActionResult> ApproveFeedback(int id)
        {
            var feedback = await _context.Feedbacks.FindAsync(id);
            if (feedback == null)
            {
                return NotFound();
            }

            feedback.IsApproved = true;
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = "تمت الموافقة على المراجعة بنجاح";
            return RedirectToAction(nameof(Feedbacks));
        }

        public async Task<IActionResult> DisapproveFeedback(int id)
        {
            var feedback = await _context.Feedbacks.FindAsync(id);
            if (feedback == null)
            {
                return NotFound();
            }

            feedback.IsApproved = false;
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = "تم إلغاء الموافقة على المراجعة بنجاح";
            return RedirectToAction(nameof(Feedbacks));
        }

        public async Task<IActionResult> DeleteFeedback(int id)
        {
            var feedback = await _context.Feedbacks.FindAsync(id);
            if (feedback == null)
            {
                return NotFound();
            }

            _context.Feedbacks.Remove(feedback);
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = "تم حذف المراجعة بنجاح";
            return RedirectToAction(nameof(Feedbacks));
        }
    }
}
