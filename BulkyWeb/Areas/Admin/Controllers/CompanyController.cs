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
    public class CompanyController : Controller
    {
        private readonly IUnitOfWork _unitOfWork;
        public CompanyController(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }
        public IActionResult Index()
        {
            var compList = _unitOfWork.Company.GetAll();

            return View(compList);
        }


        [HttpGet]
        public IActionResult Upsert(int? Id) // Update Insert
        {

            if (Id is null || Id == 0)
            {
                //Create
                return View(new Company());
            }
            // Update
            var comp = _unitOfWork.Company.Get(c => c.Id == Id);
            return View(comp);
        }

        [HttpPost]
        public IActionResult Upsert(Company comp)
        {
           
            if (ModelState.IsValid)
            {
                if (comp.Id == 0 || comp.Id == null)
                {
                    _unitOfWork.Company.Add(comp);
                    _unitOfWork.Save();
                    TempData["Success"] = "Company Created Successfully";
                }
                else
                {
                    _unitOfWork.Company.Update(comp);
                    _unitOfWork.Save();
                    TempData["Success"] = "Company Updated Successfully";
                }
                return RedirectToAction("Index");
            }

            return View(comp);
        }



        #region APICalls
        [HttpGet]
        public IActionResult GetAll()
        {
            var compList = _unitOfWork.Company.GetAll();
            return Json(new { data = compList });
        }

        [HttpDelete]
        public IActionResult Delete(int? Id)
        {
            var comp = _unitOfWork.Company.Get(c => c.Id == Id);
            if (comp is null)
            {
                return Json(new { success = false, message = "Error while deleting" });
            }

            _unitOfWork.Company.Remove(comp);
            _unitOfWork.Save();
            return Json(new { success = true, message = "Company Deleted Successfully" });

        }
        #endregion
    }
}
