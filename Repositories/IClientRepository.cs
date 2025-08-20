using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Reckoner.Models;
namespace Reckoner.Repositories
{
    public interface IClientRepository
    {
        bool SaveClientInfo(Client client);
        Client? GetClient(int clientId);
            //    Client GetClient(string FirstName, string LastName);
        IList<Client> GetAllClients();
    }
}
