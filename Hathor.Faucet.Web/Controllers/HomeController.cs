using Hathor.Faucet.Database.Models;
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
        private readonly FaucetService faucetService;

        public HomeController(ILogger<HomeController> logger, 
            HathorService hathorService, 
            WalletTransactionService walletTransactionService,
            FaucetService faucetService)
        {
            _logger = logger;
            this.hathorService = hathorService;
            this.walletTransactionService = walletTransactionService;
            this.faucetService = faucetService;
        }

        public async Task<IActionResult> Index()
        {
            HomepageViewModel vm = new HomepageViewModel();

            vm.Address = await hathorService.GetAddressAsync();
            vm.Amount = await hathorService.GetCurrentFundsAsync();
            vm.CurrentPayout = await hathorService.GetCurrentPayoutAsync();
           
            var history = await walletTransactionService.GetHistoryInfo();
            vm.NumberOfTransactions = history.count;
            vm.HistoricPayoutAmount = history.payoutAmount;

            return View(vm);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Route("submit")]
        public async Task<IActionResult> SubmitAddress([FromForm]string address)
        {
            string ip = this.HttpContext.Connection.RemoteIpAddress?.ToString() ?? "UNKNOWN";

            Guid? result = await faucetService.SendHathorAsync(address, ip);

            return RedirectToAction(nameof(ThankYou), new { txId = result });
        }

        [Route("thanks")]
        public async Task<IActionResult> ThankYou([FromQuery]Guid? txId)
        {
            ThankYouViewModel vm = new ThankYouViewModel();
            vm.Address = await hathorService.GetAddressAsync();

            if (txId.HasValue)
                vm.WalletTransaction = await walletTransactionService.GetTransactionAsync(txId.Value);

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
