using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using BulkyBook.DataAccess.Repository.IRepository;
using BulkyBook.Models;
using BulkyBook.Utility;
using Dapper;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BulkyBook.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = SD.Role_Admin)]
    public class CoverTypeController : Controller
    {
        private readonly IUnitOfWork _unitOfWork;

        public CoverTypeController(IUnitOfWork unitWork)
        {
            _unitOfWork = unitWork;
        }
        public IActionResult Index()
        {
            return View();
        }

        public IActionResult Upsert(int? id)
        {
            var coverType = new CoverType();

            if (id == null)
            {
                //Create
                return View(coverType);
            }
            //Edit
            var parameter = new DynamicParameters();
            parameter.Add("@Id", id);
            coverType = _unitOfWork.SP_Call.OneRecord<CoverType>(SD.Proc_CoverType_Get,parameter);

            if(coverType == null)
            {
                return NotFound();
            }

            return View(coverType);
        }
        
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Upsert(CoverType coverType)
        {
            if (ModelState.IsValid)
            {
                var parameter = new DynamicParameters();
                parameter.Add("@name", coverType.Name);

                if(coverType.Id == 0)
                {
                    _unitOfWork.SP_Call.Execute(SD.Proc_CoverType_Create, parameter);
                }

                else
                {
                    parameter.Add("@Id", coverType.Id);
                    _unitOfWork.SP_Call.Execute(SD.Proc_CoverType_Update, parameter);  
                }

                _unitOfWork.Save();
                return RedirectToAction(nameof(Index));
            }

            return View(coverType);
        }


        #region Api Calls
        [HttpGet]
        public IActionResult GetAll()
        {
            var coverTypes = _unitOfWork.SP_Call.List<CoverType>(SD.Proc_CoverType_GetAll,null);

            return Json(new { data = coverTypes});
        }
        
        [HttpDelete]
        public IActionResult Delete(int id)
        {
            var parameter = new DynamicParameters();
            parameter.Add("@Id", id);
            var coverType = _unitOfWork.SP_Call.OneRecord<CoverType>(SD.Proc_CoverType_Delete, parameter);
            
            if(coverType == null)
            {
                return Json(new { success = false, messsage = "Error while Deleting" });
            }

            _unitOfWork.SP_Call.Execute(SD.Proc_CoverType_Delete, parameter);
            _unitOfWork.Save();

            return Json(new { success = true, message = "Delete Succesful" });
        }
        #endregion 
    }
}