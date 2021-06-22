# Hathor.Faucet
Hathor Community Faucet

Testnet: https://testnet.gethathor.com  
Mainnet: https://www.gethathor.com

## Development
Install the .NET 5 SDK from https://dot.net

Optional: Edit the configuration in `appsettings.json`. The default settings should provice a basic out of the box experience. Edit the settings to actually talk to the wallet api and send transactions:
- `ConnectionStrings.DefaultConnection`: optional SQL connection string. Leave empty to use SqlLite
- `HathorConfig.BaseUrl`: URL to your [Hathor Headless Wallet API](https://github.com/HathorNetwork/hathor-wallet-headless)
- `HathorConfig.ApiKey`: Optional API key for your headless wallet API
- `HathorConfig.FullNodeBaseUrl`: URL to full node. Default configured for testnet [More info](https://hathor.network/testnet/)
- `RecaptchaSiteKey`: Your [reCaptcha](https://www.google.com/u/1/recaptcha/admin/) keys
- `RecaptchaSecretKey`: Your [reCaptcha](https://www.google.com/u/1/recaptcha/admin/) keys

Run the following commands:
```ps
dotnet restore
dotnet run -p Hathor.Faucet.Web
```

## Contribute
Contributions to this project are welcome! Especially frontend / css / text improvements are needed. Open a PR with your changes.
For high impact changes, open an issue to discuss the proposed change you want to make.

## Open source credits
- [Hathor.Client](https://github.com/michielpost/Hathor.Client)
- [RecaptchaNet](https://github.com/tanveery/recaptcha-net)
- [Bootstrap Template](https://startbootstrap.com/template/business-frontpage)

## Acknowledgements
Development has been made possible with a grant from [Hathor](https://hathor.network).