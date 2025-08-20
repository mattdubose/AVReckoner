using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace Reckoner.Models
{
  public class Person
  {
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string MiddleName { get; set; } = string.Empty;
    public string Suffix { get; set; } = string.Empty;// jr, IV, etc.
    
    public DateTime DateOfBirth { get; set; }
    private string _ssn = string.Empty;
    public int PersonID { get; private set; }
    public string SSN
    {
      get => _ssn;
      set
      {
        _ssn = value;
        // Remove non-numeric characters and convert to integer if possible
        PersonID = int.TryParse(new string(_ssn.Where(char.IsDigit).ToArray()), out var id) ? id : 0;
      }
    }
  }
  public class Client
  {
    public int ClientId { get; set; } = 0;
    public string Address { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Phone { get; set; } = string.Empty;
    public List<int> AccountIds { get; set; } = new List<int>();
    public Person? Spouse { get; set; } = null;
    public Person Primary { get; set; } = new Person();
    public bool IsMarried { get; set; } = false;
     public string FullName => $"{Primary.FirstName} {Primary.LastName}";
    }
}
