using Dapper;
using Risk.Model;
using System.Data;

namespace Risk.Repository
{
    public class ClientTransactionRepository: IClientTransactionRepository
    {
        private readonly DapperContext _dapperContext;
        

        public ClientTransactionRepository(DapperContext dapperContext)
        {
            _dapperContext = dapperContext;

        }
        public async Task <dynamic> GetClientDetails(Param req)
        {
            var parameters = new DynamicParameters();
            parameters.Add("@client_id", req.ClientId, DbType.String, ParameterDirection.Input);
            parameters.Add("@flag", req.flag, DbType.Int32, ParameterDirection.Input);
            ParamResp resp = new ParamResp();

            using (var connection = _dapperContext.CreateConnection())
            {
                using (var multi = await connection.QueryMultipleAsync("usp_client_dashboard", parameters, commandType: CommandType.StoredProcedure))
                {
                    if (req.flag == 3)
                    {
                        
                        resp.Cashflow= (await multi.ReadAsync<dynamic>()).ToList();
                        resp.ClientDetails = (await multi.ReadAsync<dynamic>()).ToList();

                        return resp;

                    }
                    else
                    {
                        var result = await connection.QueryAsync<dynamic>("usp_client_dashboard", parameters, commandType: CommandType.StoredProcedure);
                        return result;

                    }

                }
                
            }
        }
    }
}
