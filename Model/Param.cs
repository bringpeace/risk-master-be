namespace Risk.Model
{
    public class Param
    {
        public string? ClientId { get; set; }
        public int? flag { get; set; }
    }

    public class ParamResp
    {
        public List<dynamic?> Cashflow{ get; set; }

        public List<dynamic?> ClientDetails { get; set; }

    }
}
