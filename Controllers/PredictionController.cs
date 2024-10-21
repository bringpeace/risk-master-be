using Microsoft.AspNetCore.Mvc;
using Risk.Model;
using Risk.Repository;

namespace Risk.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class PredictionController : ControllerBase
    {
        private readonly IPredictionRepository _repository;
        public PredictionController(IPredictionRepository repository)
        {
            _repository = repository;
        }

        [HttpPost("get-prediction")]
        public async Task<IActionResult> GetPrediction(Param req)
        {
            var result = await _repository.GetPrediction(req);
            return Ok(result);
        }

        [HttpPost("get-model")]
        public async Task<IActionResult> GetModel(Param req)
        {
            var result = await _repository.GetModel(req);
            return Ok(result);
        }
        //[HttpPost("get-forecast")]
        //public async Task<IActionResult> GetForecast(Param req)
        //{
        //    var result = await _repository.GetForecast(req);
        //    return Ok(result);
        //}

    }
}
