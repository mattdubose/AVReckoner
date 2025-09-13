using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Reckoner.Utilities;

namespace Reckoner.Repositories
{
  public class ClientJSONRepository : IClientRepository
  {
    List<Client>? _clients;
    public ClientJSONRepository(string jsonFile) 
    {
      Stream stream = FileUtils.OpenFile(jsonFile);

      _clients = JsonSerializer.Deserialize<List<Client>>(stream);
    }

        public IList<Client> GetAllClients()
        {
            if (_clients is null) return new List<Client>();
            return _clients;
        }

        public Client GetClient(int clientId)
        {
          if (_clients == null || _clients.Count == 0) 
          {
            throw new KeyNotFoundException();
          }
          foreach (var client in _clients)
          {
            if (client.ClientId == clientId)
              return client;
          }
          throw new KeyNotFoundException();
        }

        public bool SaveClientInfo(Client client)
        {
            throw new NotImplementedException();
        }
    }

}
