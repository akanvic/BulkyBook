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
    [Authorize(Roles = SD.Role_Admin + "," + SD.Role_Employee)]
    public class CompanyController : Controller
    {
        private readonly IUnitOfWork _unitOfWork;

        public CompanyController(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }
        public IActionResult Index()
        {
            return View();
        }

        public IActionResult Upsert(int? id)
        {
            var company = new Company();

            if(id == null)
            {
                //Create
                return View(company);
            }

            company = _unitOfWork.Company.Get(id.GetValueOrDefault());

            if (company == null)
            {
                return NotFound();
            }

            return View(company);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Upsert(Company company)
        {
            if (ModelState.IsValid)
            {
                if(company.Id == 0)
                {
                    //Create
                    _unitOfWork.Company.Add(company);
                }
                else
                {
                    //Edit
                    _unitOfWork.Company.Update(company);

                }

                _unitOfWork.Save();
                return RedirectToAction(nameof(Index));
            }

            return View(company);
        }
        #region APICALLS

        [HttpGet]
        public IActionResult GetAll()
        {
            var companies = _unitOfWork.Company.GetAll();

            return Json(new { data = companies });
        }

        [HttpDelete]
        public IActionResult Delete(int id)
        {
            var company = _unitOfWork.Company.Get(id);

            if(company == null)
            {
                return Json(new { data = company, success = false, message = "Error While Deleting" });
            }

            _unitOfWork.Company.Remove(id);
            _unitOfWork.Save();

            return Json(new { data = company, success = true, message = "Delete Successful" });
        }
        #endregion

    }
}