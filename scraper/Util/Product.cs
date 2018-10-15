using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace scraper.Util
{
    public class Product
    {
        public String UPC { get; set; }
        public String Brand { get; set; }
        public String Name { get; set; }
        public String VariantName { get; set; }
        public String Description { get; set; }
        public String ProductID { get; set; }
        public String SkuID { get; set; }
        public String ItemNumber { get; set; }
        public String ModelNumber { get; set; }
        public String Price { get; set; }
        public String Categories { get; set; }

        public String Variance_1 { get; set; }
        public String VarianceValue_1 { get; set; }

        public String Variance_2 { get; set; }
        public String VarianceValue_2 { get; set; }

        public String ProductUrl { get; set; }
        public String ImageUrl_A { get; set; }
        public String ImageUrl_B { get; set; }
        public String ImageUrl_C { get; set; }
        public String ImageUrl_D { get; set; }
        public String ImageUrl_E { get; set; }

    }
}
