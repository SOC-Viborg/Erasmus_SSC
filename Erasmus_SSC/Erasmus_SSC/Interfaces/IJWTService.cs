
using Erasmus_SSC.Models;
using Erasmus_SSC.Services;

namespace Erasmus_SSC.Interfaces;

public interface IJWTService
{
   
    string CreateToken(User user);

    
}