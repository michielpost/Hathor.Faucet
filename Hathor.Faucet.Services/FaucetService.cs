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

            //Check if IP is already in database

            //Check if IP is on blocklist (Azure / Amazon / TOR etc)

            //Check if Hathor address is empty
            bool isEmpty = await hathorService.CheckIsAddressEmptyAsync(address);

            int amount = await hathorService.GetCurrentPayoutAsync();
#if !DEBUG
            if (!isEmpty)
                amount = 0;
#endif

            //Save transaction in database
            var dbTx = await walletTransactionService.SaveTransactionAsync(address, amount, ip);

            //Send hathor
            if (amount > 0)
            {
                var result = await hathorService.SendHathorAsync(address, amount);

                await walletTransactionService.SetHathorTxFinishedAsync(dbTx.Id, result.TxId, result.Success, result.Error);
            }

            return dbTx.Id;

        }
    }
}
