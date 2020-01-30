using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace WebMVC.Models
{
    public class Product
    {
        public int ProductId { get; set; }
        public string ProductName { get; set; }
        public string Detail { get; set; }
        public string CreateTime { get; set; }
        public string OrderId { get; set; }
    }
}