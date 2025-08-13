using Bulky.DataAccess.Data;
using Bulky.DataAccess.Repository;
using Bulky.DataAccess.Repository.IRepository;
using Bulky.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;

namespace DataAccess.Repository
{
    internal class ProductRepository : Repository<Product>, IProductRepository
    {
        private readonly ApplicationDbContext _db;
        public ProductRepository(ApplicationDbContext db) : base(db)
        {
            _db = db;
        }

        public void Update(Product prod)
        {
            var objFromDb = _db.Products.FirstOrDefault(p => p.Id == prod.Id);
            if (objFromDb != null)
            {
                objFromDb.Title = prod.Title;
                objFromDb.Description = prod.Description;
                objFromDb.CategoryId = prod.CategoryId;
                objFromDb.ISBN = prod.ISBN;
                objFromDb.Price = prod.Price;
                objFromDb.ListPrice = prod.ListPrice;
                objFromDb.Price50 = prod.Price50;
                objFromDb.Price50 = prod.Price100;
                objFromDb.Author = prod.Author;
                objFromDb.ProductImages = prod.ProductImages;

                //if (objFromDb.ImageUrl != null)
                //{
                //    objFromDb.ImageUrl = prod.ImageUrl;
                //}


                //_db.Products.Update(prod);


            }
        }
    }
}
