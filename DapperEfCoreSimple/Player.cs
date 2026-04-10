using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text;

namespace DapperEfCoreSimple;

[Table("Players")]
public class Player
{
    public Guid Id { get; set; }
    public string Name { get; set; }
    public int Age { get; set; }
}
