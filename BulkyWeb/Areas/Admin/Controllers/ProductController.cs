using Bulky.Models;
using Bulky.Models.ViewModels;
using Bulky.Utility;
using Bulky.DataAccess.Repository.IRepository;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.DotNet.Scaffolding.Shared.Messaging;

namespace BookWeb.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = SD.Role_Admin)]
    public class ProductController : Controller
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IWebHostEnvironment _webHostEnvironment;
        public ProductController(IUnitOfWork unitOfWork, IWebHostEnvironment webHostEnvironment)
        {
            _unitOfWork = unitOfWork;
            _webHostEnvironment = webHostEnvironment;
        }
        public IActionResult Index()
        {
            var prodList = _unitOfWork.Product.GetAll(includeProperties: "Category"); 

            return View(prodList);
        }


        [HttpGet]
        public IActionResult Upsert(int? Id) // Update Insert
        {

            ProductVM productVM = new()
            {
                catList = _unitOfWork.Category.GetAll()
                .Select(c => new SelectListItem
                {
                    Text = c.Name,
                    Value = c.Id.ToString()
                }),
                product = new Product()
            };



            if (Id is null || Id == 0)
            {
                //Create
                return View(productVM);
            }
            // Update
            //                                                           The Name OF Navigation Property
            productVM.product = _unitOfWork.Product.Get(p => p.Id == Id, includeProperties: "ProductImages");
            return View(productVM);
        }

        [HttpPost]
        public IActionResult Upsert(ProductVM productVM, List<IFormFile> files)
        {
            if ((productVM.product.Id == 0 || productVM.product.Id == null) && files == null)
            {
                ModelState.AddModelError("ImageUrl", "Please upload an image.");
            }

            if (ModelState.IsValid)
            {


                if (productVM.product.Id == 0 || productVM.product.Id == null)
                {
                    _unitOfWork.Product.Add(productVM.product);
                }
                else
                {
                    _unitOfWork.Product.Update(productVM.product);
                }

                _unitOfWork.Save();

                string wwwRootPath = _webHostEnvironment.WebRootPath;
                if (files != null)
                {

                    foreach (IFormFile file in files)
                    {
                        string fileName = Guid.NewGuid().ToString() + Path.GetExtension(file.FileName);
                        string productPath = @"Images\Products\Product-" + productVM.product.Id;
                        string finalPath = Path.Combine(wwwRootPath, productPath);



                        if (!Directory.Exists(finalPath))
                        { 
                            Directory.CreateDirectory(finalPath);
                        }
                        using (var fileStream = new FileStream(Path.Combine
                            (finalPath, fileName), FileMode.Create))
                        {
                            file.CopyTo(fileStream);
                        }

                        ProductImage productImage = new()
                        {
                            ImageUrl = @"\"+ productPath +@"\"+fileName,
                            ProductId = productVM.product.Id,
                        };

                        if(productVM.product.ProductImages == null)
                            productVM.product.ProductImages = new List<ProductImage>();

                        productVM.product.ProductImages.Add(productImage);
                    }

                    _unitOfWork.Product.Update(productVM.product);
                    _unitOfWork.Save();
                }

                TempData["Success"] = "Product Created/Updated Successfully";
                return RedirectToAction("Index");
            }


            productVM.catList = _unitOfWork.Category.GetAll()
                .Select(c => new SelectListItem
                {
                    Text = c.Name,
                    Value = c.Id.ToString()
                });
            return View(productVM);
        }


        //[HttpGet]
        //public IActionResult Delete(int? Id)
        //{
        //    if (Id is null || Id == 0)
        //    {
        //        return NotFound();
        //    }
        //    var prod = _unitOfWork.Product.Get(p => p.Id == Id);
        //    if (prod == null)
        //    {
        //        return NotFound();
        //    }
        //    return View(prod);
        //}

        //[HttpPost]
        //public IActionResult Delete(Product prodObj)
        //{
        //    var prod = _unitOfWork.Product.Get(p => p.Id == prodObj.Id);

        //    if(prodObj is null)
        //    {
        //        return NotFound();
        //    }

        //    _unitOfWork.Product.Remove(prod);
        //    _unitOfWork.Save();
        //    TempData["Success"] = "Product Deleted Successfully";
        //    return RedirectToAction("Index");
        //}


        public IActionResult DeleteImage(int Id)
        {
            var imageToBeDeleted = _unitOfWork.ProductImage.Get(i => i.Id == Id);
            int productId = imageToBeDeleted.ProductId;
            if(imageToBeDeleted != null)
            {
                if(!string.IsNullOrEmpty(imageToBeDeleted.ImageUrl))
                {
                    var oldImagePath = Path.Combine(_webHostEnvironment.WebRootPath,
                        imageToBeDeleted.ImageUrl.TrimStart('\\'));

                    if(System.IO.File.Exists(oldImagePath))
                    {
                        System.IO.File.Delete(oldImagePath);
                    }

                    _unitOfWork.ProductImage.Remove(imageToBeDeleted);
                    _unitOfWork.Save();

                    TempData["Success"] = "Image Deleted Successfully!";
                }
            }
            return RedirectToAction("Upsert", new {Id = productId});
        }



        #region APICalls
        [HttpGet]
        public IActionResult GetAll()
        {
            var prodList = _unitOfWork.Product.GetAll(includeProperties: "Category");
            return Json(new { data = prodList });
        }


        [HttpDelete]
        public IActionResult Delete(int? Id)
        {
            var prod = _unitOfWork.Product.Get(p => p.Id == Id);
            if (prod is null)
            {
                return Json(new { success = false, message = "Error while deleting" });
            }

            var productImagesPath = Path.Combine(_webHostEnvironment.WebRootPath,
                @"Images\Products\Product-" + Id);


            if (Directory.Exists(productImagesPath))
            {
                string[] filePaths = Directory.GetFiles(productImagesPath);
                foreach (string filePath in filePaths)
                {
                    System.IO.File.Delete(filePath);
                }
                Directory.Delete(productImagesPath);
            }

            _unitOfWork.Product.Remove(prod);
            _unitOfWork.Save();
            return Json(new { success = true, message = "Product Deleted Successfully" });

        }



        //[HttpDelete]
        //public IActionResult Delete(int? Id)
        //{
        //    var prod = _unitOfWork.Product.Get(p => p.Id == Id, includeProperties: "ProductImages");
        //    List<ProductImage> productImages = prod.ProductImages;
        //    if (prod is null)
        //    {
        //        return Json(new { success = false, message = "Error while deleting" });
        //    }

        //    var productImagesPath = Path.Combine(_webHostEnvironment.WebRootPath,
        //        @"Images\Products\Product-" + Id);


        //    if (Directory.Exists(productImagesPath))
        //    {
        //        _unitOfWork.ProductImage.RemoveRange(productImages);
        //        _unitOfWork.Save();
        //        Directory.Delete(productImagesPath, true);
        //    }

        //    _unitOfWork.Product.Remove(prod);
        //    _unitOfWork.Save();
        //    return Json(new { success = true, message = "Product Deleted Successfully" });

        //}
        #endregion
    }
}
