using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Risk.Model;
using Risk.Repository;

namespace Risk.Controllers
{

    [Route("api/[controller]")]
    [ApiController]
    public class ClientTransactionController :ControllerBase
    { 
        private readonly IClientTransactionRepository _repository;
        public ClientTransactionController(IClientTransactionRepository repository) {
            _repository = repository;
        }

        
        [HttpPost("get-client-details")]
        public async Task<IActionResult> GetClientDetails(Param req)
        {
            var result = await _repository.GetClientDetails(req);
            return Ok(result);
        }

    }
}
