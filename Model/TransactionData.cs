using Microsoft.ML.Data;

namespace Risk.Model
{

    public class TransactionData
    {
        // private long _transactionDateUnixTimestamp;
        [LoadColumn(0)]
        public float TransactionId { get; set; }

        [LoadColumn(1)]
        public float ClientId { get; set; }
        [LoadColumn(2)]
        public string FullName { get; set; }
        [LoadColumn(3)]
        public float FkIdInstrument { get; set; }
        [LoadColumn(4)]
        public string InstrumentFullName { get; set; }
        [LoadColumn(5)]
        public float FkIdAsset { get; set; }
        [LoadColumn(6)]
        public string AssetName { get; set; }
        [LoadColumn(7)]
        public string IsinCode { get; set; }
        [LoadColumn(8)]
        public float FkIdProduct { get; set; }
        [LoadColumn(9)]
        public string ProductName { get; set; }
        [LoadColumn(10)]
        public float FkCategoryId { get; set; }
        [LoadColumn(11)]
        public string CategoryName { get; set; }

        [LoadColumn(12)]
        public long UnixTimestamp { get; set; }

        [LoadColumn(13)]

        public float TotalAmount { get; set; }
        [LoadColumn(14)]
        public string BuySell { get; set; }
        [LoadColumn(15)]
        public float TotalAvgAmount { get; set; }
        [LoadColumn(16)]
        public float TotalTransaction { get; set; }
        [LoadColumn(17)]
        public float Xirr { get; set; }
        [LoadColumn(18)]
        public float Bmxirr { get; set; }
        [LoadColumn(19)]
        public string NewsHeadline { get; set; }
        [LoadColumn(20)]
        public string NewsSentiment { get; set; }
        //[LoadColumn(21)]
        //public DateTime NewsDate { get; set; }
        [LoadColumn(21)]
        public string RiskProfile { get; set; }
        // public float TransactionDateNumeric { get; set; }
    }

    public class TransactionPrediction
    {
        [ColumnName("PredictedLabel")]
        public string RiskProfile { get; set; }
    }

    public class OverallRiskProfiles
    {
        public float ClientId { get; set; }
        public string OverallRiskProfile { get; set; }
    }


    public class ForecastData
    {
        public DateTime Time { get; set; }
        public string AssetName { get; set; }
        public string IsinCode { get; set; }
        public float FkIdProduct { get; set; }
        public string ProductName { get; set; }
        public float FkCategoryId { get; set; }
        public string CategoryName { get; set; }
        public float TotalAmount { get; set; }
        public string BuySell { get; set; }
        public string MarketSentiment { get; set; }
    }

    public class TransactionForecast
    {
        public float[] ForecastedAmounts { get; set; }
    }

}
