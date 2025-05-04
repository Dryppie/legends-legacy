using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.UseCases.Users.Dtos;

public class UserInfoDto
{
    public string Email { get; set; } = string.Empty;
    public bool IsRegisteredUser { get; set; }
}
