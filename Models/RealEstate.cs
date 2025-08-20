using Reckoner.Repositories;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace Reckoner.Models
{
  public class RentalProperty : Asset
  {
    public RentalProperty()
    {
      Address = string.Empty;
      Name = string.Empty;
    }
    public RentalProperty(string address, string name)
    {
      Address = address;
      Name = name;
    }
    public RentalProperty(string address)
    {
      Address = address;
      Name = string.Empty;
    }

    public string Address { get; set; } = string.Empty;

    // Implement ID property to return Address for RealEstate
    public override string ID => Address;
    public override AssetType AssetType => AssetType.RealEstate;
  }

}