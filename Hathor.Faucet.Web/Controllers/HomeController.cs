using Hathor.Faucet.Services;
using Hathor.Faucet.Web.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;

namespace Hathor.Faucet.Web.Controllers
{
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;
        private readonly HathorService hathorService;
        private readonly WalletTransactionService walletTransactionService;

        public HomeController(ILogger<HomeController> logger, HathorService hathorService, WalletTransactionService walletTransactionService)
        {
            _logger = logger;
            this.hathorService = hathorService;
            this.walletTransactionService = walletTransactionService;
        }

        public async Task<IActionResult> Index()
        {
            HomepageViewModel vm = new HomepageViewModel();

            vm.Address = await hathorService.GetAddressAsync();
            vm.Amount = await hathorService.GetCurrentFundsAsync();
            vm.NumberOfTransactions = await walletTransactionService.GetNumberOfTransactionsAsync();

            return View(vm);
        }

        public IActionResult Privacy()
        {
            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}
