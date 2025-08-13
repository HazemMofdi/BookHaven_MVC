


using Bulky.DataAccess.Data;
using Bulky.DataAccess.Repository.IRepository;
using Bulky.Models;
using Bulky.Models.ViewModels;
using Bulky.Utility;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.DotNet.Scaffolding.Shared.Messaging;
using Microsoft.EntityFrameworkCore;




#region ---------------------------Before Unit Of Work---------------------------------
//using Bulky.DataAccess.Data;
//using Bulky.DataAccess.Repository.IRepository;
//using Bulky.Models;
//using Bulky.Models.ViewModels;
//using Bulky.Utility;
//using Microsoft.AspNetCore.Authorization;
//using Microsoft.AspNetCore.Identity;
//using Microsoft.AspNetCore.Mvc;
//using Microsoft.AspNetCore.Mvc.Rendering;
//using Microsoft.DotNet.Scaffolding.Shared.Messaging;
//using Microsoft.EntityFrameworkCore;

//namespace BookWeb.Areas.Admin.Controllers
//{
//    [Area("Admin")]
//    [Authorize(Roles = SD.Role_Admin)]
//    public class UserController : Controller
//    {
//        private readonly ApplicationDbContext _db;
//        private readonly UserManager<IdentityUser> _userManager;

//        public UserController(ApplicationDbContext db, UserManager<IdentityUser> userManager)
//        {
//            _db = db;
//            _userManager = userManager
//        }
//        public IActionResult Index()
//        {
//            return View();
//        }

//        [HttpGet]
//        public IActionResult RoleManagement(string Id)
//        {

//            var roleId = _db.UserRoles.FirstOrDefault(ur => ur.UserId == Id).RoleId;

//            RoleManagementVM roleManagementVM = new RoleManagementVM()
//            {
//                 User = _db.ApplicationUsers.Include(u => u.Company).FirstOrDefault(u=> u.Id == Id),

//                 RoleList = _db.Roles.Select(r => new SelectListItem { Text = r.Name, Value = r.Name }),

//                 CompanyList = _db.Companies.Select(c => new SelectListItem { Text = c.Name, Value = c.Id.ToString() })
//            };

//            roleManagementVM.User.Role = _db.Roles.FirstOrDefault(r => r.Id == roleId).Name;

//            return View(roleManagementVM);
//        }


//        [HttpPost]
//        [ActionName("RoleManagement")]
//        public IActionResult RoleManagementPOST(RoleManagementVM roleManagementVM)
//        {
//            var roleId = _db.UserRoles.FirstOrDefault(ur => ur.UserId == roleManagementVM.User.Id).RoleId;
//            var oldRole = _db.Roles.FirstOrDefault(r => r.Id == roleId).Name;


//            if(!(roleManagementVM.User.Role == oldRole))
//            {
//                // role was updated
//                var dbUser = _db.ApplicationUsers.FirstOrDefault(u => u.Id == roleManagementVM.User.Id);

//                if (roleManagementVM.User.Role == SD.Role_Company)
//                {
//                    dbUser.CompanyId = roleManagementVM.User.CompanyId;
//                }
//                if(oldRole == SD.Role_Company)
//                {
//                    dbUser.CompanyId = null;
//                }
//                _db.SaveChanges();

//                _userManager.RemoveFromRoleAsync(dbUser, oldRole).GetAwaiter().GetResult();
//                _userManager.AddToRoleAsync(dbUser, roleManagementVM.User.Role).GetAwaiter().GetResult();
//            }

//            return RedirectToAction("Index");
//        }



//        #region APICalls
//        [HttpGet]
//        public IActionResult GetAll()
//        {
//            var usersList = _db.ApplicationUsers.Include(u=> u.Company).ToList();

//            var userRoles = _db.UserRoles.ToList();
//            var roles = _db.Roles.ToList();


//            foreach (var user in usersList)
//            {

//                var roleId = userRoles.FirstOrDefault(ur => ur.UserId == user.Id).RoleId;
//                user.Role = roles.FirstOrDefault(r => r.Id == roleId).Name;

//                if(user.Company == null)
//                {
//                    user.Company = new Company()
//                    {
//                        Name = ""
//                    };
//                }
//            }
//            return Json(new { data = usersList });
//        }

//        [HttpPost]
//        public IActionResult LockUnlock([FromBody] string Id)
//        {
//            var objFromDb = _db.ApplicationUsers.FirstOrDefault(u => u.Id == Id);
//            if(objFromDb == null)
//            {
//                return Json (new { success  = false , message = "Error while retrieving the user" });
//            }
//            if(objFromDb.LockoutEnd != null && objFromDb.LockoutEnd > DateTime.Now)
//            {
//                // user is locked
//                objFromDb.LockoutEnd = DateTime.Now;
//            }
//            else
//            {
//                objFromDb.LockoutEnd = DateTime.Now.AddDays(30);
//            }
//            _db.SaveChanges();
//                return Json(new { success = true, message = "Operation Successfully" });
//        }
//        #endregion
//    }
//}  
#endregion


namespace BookWeb.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = SD.Role_Admin)]
    public class UserController : Controller
    {
        private readonly ApplicationDbContext _db;
        private readonly UserManager<IdentityUser> _userManager;
        private readonly RoleManager<IdentityRole> _roleManager;
        private readonly IUnitOfWork _unitOfWork;

        public UserController(UserManager<IdentityUser> userManager, RoleManager<IdentityRole> roleManager, IUnitOfWork unitOfWork)
        {
            _userManager = userManager;
            _roleManager = roleManager;
            _unitOfWork = unitOfWork;
        }
        public IActionResult Index()
        {
            return View();
        }

        [HttpGet]
        public IActionResult RoleManagement(string Id)
        {
            RoleManagementVM roleManagementVM = new RoleManagementVM()
            {
                User = _unitOfWork.ApplicationUser.Get(u=> u.Id == Id, includeProperties:"Company"),

                RoleList = _roleManager.Roles.Select(r => new SelectListItem { Text = r.Name, Value = r.Name }),

                CompanyList = _unitOfWork.Company.GetAll().Select(c => new SelectListItem { Text = c.Name, Value = c.Id.ToString() })
            };

            roleManagementVM.User.Role = _userManager.GetRolesAsync
            (_unitOfWork.ApplicationUser.Get(u=> u.Id == Id)).GetAwaiter().GetResult().FirstOrDefault();

            return View(roleManagementVM);
        }


        [HttpPost]
        [ActionName("RoleManagement")]
        public IActionResult RoleManagementPOST(RoleManagementVM roleManagementVM)
        {
            var oldRole = _userManager.GetRolesAsync
            (_unitOfWork.ApplicationUser.Get(u => u.Id == roleManagementVM.User.Id)).GetAwaiter().GetResult().FirstOrDefault();

            var dbUser = _unitOfWork.ApplicationUser.Get(u => u.Id == roleManagementVM.User.Id);


            if (!(roleManagementVM.User.Role == oldRole))
            {
                // role was updated

                if (roleManagementVM.User.Role == SD.Role_Company)
                {
                    dbUser.CompanyId = roleManagementVM.User.CompanyId;
                }
                if (oldRole == SD.Role_Company)
                {
                    dbUser.CompanyId = null;
                }

                _unitOfWork.ApplicationUser.Update(dbUser);
                _unitOfWork.Save();

                _userManager.RemoveFromRoleAsync(dbUser, oldRole).GetAwaiter().GetResult();
                _userManager.AddToRoleAsync(dbUser, roleManagementVM.User.Role).GetAwaiter().GetResult();
            }
            else
            {
                if(oldRole == SD.Role_Company && dbUser.CompanyId != roleManagementVM.User.CompanyId)
                {
                    dbUser.CompanyId = roleManagementVM.User.CompanyId;
                    _unitOfWork.ApplicationUser.Update(dbUser);
                    _unitOfWork.Save();
                }
            }

                return RedirectToAction("Index");
        }



        #region APICalls
        [HttpGet]
        public IActionResult GetAll()
        {
            var usersList = _unitOfWork.ApplicationUser.GetAll(includeProperties: "Company");

            foreach (var user in usersList)
            {

                user.Role = _userManager.GetRolesAsync(user).GetAwaiter().GetResult().FirstOrDefault();

                if (user.Company == null)
                {
                    user.Company = new Company()
                    {
                        Name = ""
                    };
                }
            }
            return Json(new { data = usersList });
        }

        [HttpPost]
        public IActionResult LockUnlock([FromBody] string Id)
        {
            var objFromDb = _unitOfWork.ApplicationUser.Get(u => u.Id == Id);
            if (objFromDb == null)
            {
                return Json(new { success = false, message = "Error while retrieving the user" });
            }
            if (objFromDb.LockoutEnd != null && objFromDb.LockoutEnd > DateTime.Now)
            {
                // user is locked
                objFromDb.LockoutEnd = DateTime.Now;
            }
            else
            {
                objFromDb.LockoutEnd = DateTime.Now.AddDays(30);
            }
            _unitOfWork.ApplicationUser.Update(objFromDb);
            _unitOfWork.Save();
            return Json(new { success = true, message = "Operation Successfully" });
        }
        #endregion
    }
}
