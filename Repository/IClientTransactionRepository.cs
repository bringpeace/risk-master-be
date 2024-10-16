using Risk.Model;

namespace Risk.Repository
{
    public interface IClientTransactionRepository
    {
        Task<dynamic> GetClientDetails(Param req);


    }
}
