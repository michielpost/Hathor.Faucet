using System;
using System.Threading.Tasks;

namespace Hathor.Faucet.Services
{
    public class FaucetService
    {
        private readonly HathorService hathorService;
        private readonly WalletTransactionService walletTransactionService;

        public FaucetService(HathorService hathorService, WalletTransactionService walletTransactionService)
        {
            this.hathorService = hathorService;
            this.walletTransactionService = walletTransactionService;
        }

        public async Task<Guid> SendHathorAsync(string address, string ip)
        {
            address = address.Trim();
            //TODO: Check if address is valid

            int amount = await hathorService.GetCurrentPayoutAsync();

            //Save transaction in database
            var dbTx = await walletTransactionService.SaveTransactionAsync(address, amount, ip);

            //Send hathor
            var result = await hathorService.SendHathorAsync(address, amount);

            await walletTransactionService.SetHathorTxFinishedAsync(dbTx.Id, result.TxId, result.Success, result.Error);

            return dbTx.Id;

        }
    }
}
