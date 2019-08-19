using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using InsuranceClient.Models;
using InsuranceClient.Models.ViewModels;
using System.IO;
using InsuranceClient.Helpers;
using Microsoft.Extensions.Configuration;

namespace InsuranceClient.Controllers
{
    public class HomeController : Controller
    {
        private IConfiguration configuration;
        public HomeController(IConfiguration configuration)
        {
            this.configuration = configuration;
        }
        private IEnumerable<string> customerId;

        public IActionResult Index()
        {
            return View();
        }

        public IActionResult Create()
        {
            return View();
        }
        [HttpPost]
        public async Task<IActionResult> Create(CustomerViewModel model)
        {
            if(ModelState.IsValid)
            {
                var customerId = Guid.NewGuid();
                StorageHelper storageHelper = new StorageHelper();
                storageHelper.ConnectionString = configuration.GetConnectionString("StorageConnection");

                //save customer image to azure blob
                var tempFile = Path.GetTempFileName();

                using (var fs = new FileStream(tempFile,FileMode.Create, FileAccess.Write))
                {
                    await model.ImageUrl.CopyToAsync(fs);
                }
                var fileName = Path.GetFileName(model.ImageUrl.FileName);
                var tempPath = Path.GetDirectoryName(tempFile);
                var imagePath = Path.Combine(tempPath, string.Concat(customerId, "_", fileName));
                System.IO.File.Move(tempFile, imagePath);//rename temp file
                await storageHelper.UploadCustomerImageAsync("image", imagePath);

                //save customer data to azure table

                Customer customer = new Customer(customerId.ToString(), model.InsuranceType);
                customer.FullName = model.FullName;
                customer.Email = model.Email;
                customer.Amount = model.Amount;
                customer.AppDate = model.AppDate;
                customer.EndDate = model.EndDate;
                await storageHelper.InsertCustomerAsync("customers", customer);


                //add a confirmation message to azure queue
              await  storageHelper.AddMessageAsync("insurance-requests", customer);
                return RedirectToAction("Index");
            }
            else
            {

            }
            return View();
        }




        //public IActionResult Privacy()
        //{
        //    return View();
        //}

        //[ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        //public IActionResult Error()
        //{
        //    return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        //}
    }
}
