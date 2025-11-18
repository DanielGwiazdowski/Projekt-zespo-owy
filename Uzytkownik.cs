using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Projekt_zespołowy
{
    public class Uzytkownik
    {
        public int Id { get; set; }
        public string Login { get; set; } = "";
        public string Email { get; set; } = "";
        public string Rola { get; set; } = "";
        public string DataRejestracji { get; set; } = "";
    }
}
