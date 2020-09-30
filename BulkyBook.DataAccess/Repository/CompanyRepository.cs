using BulkyBook.DataAccess.Data;
using BulkyBook.DataAccess.Repository.IRepository;
using BulkyBook.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace BulkyBook.DataAccess.Repository
{
    public class CompanyRepository : Repository<Company>, ICompanyRepository
    {
        private readonly ApplicationDbContext _db;
        public CompanyRepository(ApplicationDbContext db) : base(db)
        {
            _db = db;
        }
        public void Update(Company company)
        {
            var companyFromDb = _db.CompanyUsers.FirstOrDefault(c => c.Id == company.Id);
            if(companyFromDb != null)
            {
                companyFromDb.Name = company.Name;
                companyFromDb.City = company.City;
                companyFromDb.State = company.State;
                companyFromDb.StreetAddress = company.StreetAddress;
                companyFromDb.PostalCode = company.PostalCode;
                companyFromDb.PhoneNumber = company.PhoneNumber;
                companyFromDb.IsAuthorizedCompany = company.IsAuthorizedCompany;
            }
        }
    }
}
