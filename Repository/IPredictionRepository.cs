using Risk.Model;

namespace Risk.Repository
{
    public interface IPredictionRepository
    {
        Task<dynamic> GetPrediction(Param req);

        Task<dynamic> GetModel(Param req);
    }
}
