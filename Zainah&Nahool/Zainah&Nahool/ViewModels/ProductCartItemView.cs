﻿namespace Zainah_Nahool.ViewModels
{
    public class ProductCartItemView
    {
        //product
        public int ProductId { get; set; }

        public string Name { get; set; } = null!;

        public string? Description { get; set; }

        public decimal Price { get; set; }

        public int StockQuantity { get; set; }

        public int? CategoryId { get; set; }

        public string? Image { get; set; }

        public DateTime? CreatedAt { get; set; }

        ///cart item 
        

        public int Id { get; set; }

        public int? CartId { get; set; }


        public int Quantity { get; set; }

        public decimal Total => Price * Quantity;




        public class CartViewModel
        {
            public List<ProductCartItemView> CartItems { get; set; } = new List<ProductCartItemView>();
        }
    }
}

