using System.Collections.Generic;

namespace Zainah_Nahool.ViewModels
{
    public class DashboardViewModel
    {
        public decimal? TotalSales { get; set; }
        public int TotalOrders { get; set; }
        public int TotalUsers { get; set; }
        public int TotalProducts { get; set; }
        public List<MonthlySalesData> MonthlySales { get; set; }
        public List<ProductSalesData> SalesByProduct { get; set; }
        public List<RecentOrderData> RecentOrders { get; set; }
        public List<TopProductData> TopProducts { get; set; }
        public double SalesGrowth { get; set; }
        public double OrdersGrowth { get; set; }
    }

    public class MonthlySalesData
    {
        public int Month { get; set; }
        public int Year { get; set; }
        public decimal Total { get; set; }
    }

    public class ProductSalesData
    {
        public string ProductName { get; set; }
        public decimal TotalSales { get; set; }
    }

    public class RecentOrderData
    {
        public int OrderId { get; set; }
        public string CustomerName { get; set; }
        public DateTime? OrderDate { get; set; }  // ????? nullable
        public string Status { get; set; }
        public decimal? TotalAmount { get; set; }  // ????? nullable
    }

    public class TopProductData
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public decimal Price { get; set; }
        public string Image { get; set; }
        public int StockQuantity { get; set; }
        public int TotalSales { get; set; }
    }
} 