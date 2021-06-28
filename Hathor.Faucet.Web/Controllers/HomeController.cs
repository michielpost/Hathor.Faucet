using Hathor.Faucet.Database.Models;
using Hathor.Faucet.Services;
using Hathor.Faucet.Services.Exceptions;
using Hathor.Faucet.Services.Models;
using Hathor.Faucet.Web.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Recaptcha.Web;
using Recaptcha.Web.Mvc;
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
        private readonly FaucetConfig faucetConfig;

        public HomeController(ILogger<HomeController> logger,
            HathorService hathorService,
            WalletTransactionService walletTransactionService,
            FaucetService faucetService,
            IOptions<FaucetConfig> faucetConfigOptions
            )
        {
            _logger = logger;
            this.hathorService = hathorService;
            this.walletTransactionService = walletTransactionService;
            this.faucetService = faucetService;
            this.faucetConfig = faucetConfigOptions.Value;
        }

        [HttpGet]
        public async Task<IActionResult> Index()
        {
            HomepageViewModel vm = await GetHomepageViewModel();

            return View(vm);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Index([FromForm] string? address)
        {
            ViewBag.address = address;
            if (string.IsNullOrEmpty(address))
                ModelState.AddModelError("address", "Please provide a valid Hathor address.");

            RecaptchaVerificationHelper recaptchaHelper = this.GetRecaptchaVerificationHelper();
            if (string.IsNullOrEmpty(recaptchaHelper.Response))
            {
                ModelState.AddModelError("captcha", "Captcha answer cannot be empty.");
            }
            else
            {
                RecaptchaVerificationResult recaptchaResult = recaptchaHelper.VerifyRecaptchaResponse();
                if (!recaptchaResult.Success)
                    ModelState.AddModelError("captcha", "Invalid captcha answer.");
            }

            string ip = this.HttpContext.Connection.RemoteIpAddress?.ToString() ?? "UNKNOWN";

            try
            {
                if (!string.IsNullOrEmpty(address))
                {
                    Guid? result = await faucetService.SendHathorAsync(address, ip);
                    return RedirectToAction(nameof(ThankYou), new { txId = result });
                }
            }
            catch (FaucetException fe)
            {
                ModelState.AddModelError("faucet", fe.Message);
            }
            catch (Exception ex)
            {
                ModelState.AddModelError("faucet", "Something went wrong. Please try again later.");
            }

            HomepageViewModel vm = await GetHomepageViewModel();
            return View(vm);
        }

        [Route("thanks")]
        [HttpGet]
        public async Task<IActionResult> ThankYou([FromQuery] Guid? txId)
        {
            ThankYouViewModel vm = new ThankYouViewModel();
            vm.Address = await hathorService.GetAddressAsync();
            vm.ExplorerUrl = faucetConfig.ExplorerUrl;

            if (txId.HasValue)
                vm.WalletTransaction = await walletTransactionService.GetTransactionAsync(txId.Value);

            return View(vm);
        }

        [Route("privacy")]
        [HttpGet]
        public IActionResult Privacy()
        {
            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }

        private async Task<HomepageViewModel> GetHomepageViewModel()
        {
            HomepageViewModel vm = new HomepageViewModel();

            try
            {
                vm.Address = await hathorService.GetAddressAsync();
                vm.Amount = await hathorService.GetCurrentFundsAsync();
                vm.CurrentPayout = await hathorService.GetCurrentPayoutAsync();
                vm.LastTransactions = await hathorService.GetLastTransactionsAsync();
                vm.ExplorerUrl = faucetConfig.ExplorerUrl;
            }
            catch
            {
                //No Wallet connection
            }

            try
            {
                var history = await walletTransactionService.GetStats();
                vm.NumberOfTransactions = history.count;
                vm.HistoricPayoutAmount = history.payoutAmount;
            }
            catch
            {
                //No DB connection
            }

            return vm;
        }
    }
}
