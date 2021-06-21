using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Hathor.Faucet.Services.Exceptions
{
    public class FaucetException : Exception
    {
        public FaucetException(string message) : base(message)
        {

        }
    }
}
