using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using sample1.Models;
using sample1.Data;
using Microsoft.Extensions.Configuration;
using Xakep.Migrate;

namespace sample1.Controllers
{
    public class HomeController : Controller
    {
        private IConfiguration Configuration { get; }


        private IMigrateTable MigrateTable { get; }

        public HomeController(IConfiguration configuration, IMigrateTable migrateTable)
        {
            Configuration = configuration;
            MigrateTable = migrateTable;

            
        }

        public IActionResult Index()
        {
            return View();
        }

        public IActionResult About()
        {
            

            ViewData["Message"] = "Your application description page.";

            return View();
        }

       

       


    

        public IActionResult Contact()
        {
            ViewData["Message"] = "Your contact page.";

            return View();
        }

        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }

       
    }
}
