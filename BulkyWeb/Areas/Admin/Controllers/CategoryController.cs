using Bulky.DataAccess.Data;
using Bulky.Models;
using Bulky.DataAccess.Repository.IRepository;
using Microsoft.AspNetCore.Mvc;
using Bulky.DataAccess.Repository;
using Bulky.DataAccess.Repository.IRepository;
using Microsoft.AspNetCore.Authorization;
using Bulky.Utility;

namespace BookWeb.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = SD.Role_Admin)]
    public class CategoryController : Controller
    {
        private readonly IUnitOfWork _unitOfWork;
        public CategoryController(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }
        public IActionResult Index()
        {
            var catList = _unitOfWork.Category.GetAll();
            return View(catList);
        }

        [HttpGet]
        public IActionResult AddCategory()
        {
            return View();
        }

        [HttpPost]
        public IActionResult AddCategory(Category catObj)
        {
            if(ModelState.IsValid)
            {
                _unitOfWork.Category.Add(catObj);
                _unitOfWork.Save();
                TempData["Success"] = "Category Created Successfully";
                return RedirectToAction("Index");
            }
            return View(catObj);
        }


        [HttpGet]
        public IActionResult Edit(int? id)
        {
            if (id is null || id == 0)
            {
                return NotFound();
            }
            //var cat = _db.Categories.FirstOrDefault(c => c.Id == id);
            // var cat = _db.Categories.Find(id);
            var cat = _unitOfWork.Category.Get(c=> c.Id == id);
            if (cat is null)
            {
                return NotFound();
            }
            return View(cat);
        }

        [HttpPost]
        public IActionResult Edit(Category catObj)
        {
            if (ModelState.IsValid)
            {
                _unitOfWork.Category.Update(catObj);
                _unitOfWork.Save();
                TempData["Success"] = "Category Updated Successfully";
                return RedirectToAction("Index");
            }
            return View(catObj);
        }



        [HttpGet]
        public IActionResult Delete(int? id)
        {
            if (id is null || id == 0)
            {
                return NotFound();
            }
            //var cat = _db.Categories.FirstOrDefault(c => c.Id == id);
            //var cat = _db.Categories.Find(id);
            var cat = _unitOfWork.Category.Get(c => c.Id == id);
            if (cat is null)
            {
                return NotFound();
            }
            return View(cat);
        }

        [HttpPost]
        public IActionResult Delete(Category catObj)
        {
            //var cat = _db.Categories.Find(catObj.Id);
            var cat = _unitOfWork.Category.Get(c => c.Id == catObj.Id);

            if (cat is null)
            {
                return NotFound();
            }

            _unitOfWork.Category.Remove(cat);
            _unitOfWork.Save();
            TempData["Success"] = "Category Deleted Successfully";
            return RedirectToAction("Index");
        }
    }
}
