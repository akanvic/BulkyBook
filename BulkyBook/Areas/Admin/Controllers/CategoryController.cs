using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using BulkyBook.DataAccess.Repository.IRepository;
using BulkyBook.Models;
using BulkyBook.Utility;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BulkyBook.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = SD.Role_Admin)]
    public class CategoryController : Controller
    {
        private readonly IUnitOfWork _unitWork;

        public CategoryController(IUnitOfWork unitOfWork)
        {
            _unitWork = unitOfWork;
        }
        public IActionResult Index()
        {   
            return View();
        }

        public IActionResult Upsert(int? id)
        {
            var category = new Category();

            if (id == null)
            {
                //For Create
                return View(category);
            }

            //This is for edit
            category = _unitWork.Category.Get(id.GetValueOrDefault());//This Id could be null so we use the GetValueOrDefault method

            if (category == null)
            {
                return NotFound();
            }
            return View(category);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Upsert(Category category)
        {
            if (ModelState.IsValid)
            {
                if (category.Id == 0)
                {
                    _unitWork.Category.Add(category);
                }
                else
                {
                    _unitWork.Category.Update(category);
                }

                _unitWork.Save();
                return RedirectToAction(nameof(Index));
            }

            return View(category);
        }

        #region API CALLS

        [HttpGet]
        public IActionResult GetAll()
        {
            var allObj = _unitWork.Category.GetAll();
            return Json(new { data = allObj });
        }

        [HttpDelete]
        public IActionResult Delete(int id)
        {
            var objFromDb = _unitWork.Category.Get(id);

            if(objFromDb== null)
            {
                return Json(new { success = false, message = "Error while deleting" });
            }
            _unitWork.Category.Remove(objFromDb);
            _unitWork.Save();

            return Json(new { success = true, message = "Delete Successful" });
        }
        #endregion
    }
}