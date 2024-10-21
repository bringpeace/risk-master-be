using Dapper;
using ExcelDataReader;
using Microsoft.ML;
using OfficeOpenXml;
using Risk.Model;
using System.Data;
using System.Data.SqlClient;
using ExcelDataReader;
using System.Xml.Linq;
using System.Formats.Asn1;
using System.Globalization;
using CsvHelper.Configuration;
using CsvHelper;

namespace Risk.Repository
{
    public class PredictionRepository : IPredictionRepository
    {
        private readonly DapperContext _dapperContext;


        public PredictionRepository(DapperContext dapperContext)
        {
            _dapperContext = dapperContext;

        }

        public async Task<dynamic> GetModel(Param req)
        {
            var mlContext = new MLContext();
            var profiles = new List<OverallRiskProfiles>();

            var parameters = new DynamicParameters();
            parameters.Add("@client_id", req.ClientId, DbType.String, ParameterDirection.Input);
            parameters.Add("@flag", 3, DbType.Int32, ParameterDirection.Input);

            using (var connection = _dapperContext.CreateConnection())
            {
                var transactions = await connection.QueryAsync<TransactionData>("usp_client_dashboard", parameters, commandType: CommandType.StoredProcedure);

                foreach (var transaction in transactions)
                {
                    transaction.RiskProfile = DetermineRiskProfile(transaction);
                }

                SaveToCsv(transactions, "D:\\Project\\Risk\\Data\\transactions_with_risk_profile_model.csv");

                string dataPath = "D:\\Project\\Risk\\Data\\transactions_with_risk_profile_model.csv"; // Convert Excel to CSV or use a library to read Excel directly
                IDataView dataView = mlContext.Data.LoadFromTextFile<TransactionData>(dataPath, separatorChar: ',', hasHeader: true);



                var dataProcessPipeline = mlContext.Transforms.Conversion.MapValueToKey("Label", nameof(TransactionData.RiskProfile))
                .Append(mlContext.Transforms.Categorical.OneHotEncoding(nameof(TransactionData.FullName)))
                .Append(mlContext.Transforms.Categorical.OneHotEncoding(nameof(TransactionData.InstrumentFullName)))
                .Append(mlContext.Transforms.Categorical.OneHotEncoding(nameof(TransactionData.AssetName)))
                .Append(mlContext.Transforms.Categorical.OneHotEncoding(nameof(TransactionData.IsinCode)))
                .Append(mlContext.Transforms.Categorical.OneHotEncoding(nameof(TransactionData.ProductName)))
                .Append(mlContext.Transforms.Categorical.OneHotEncoding(nameof(TransactionData.CategoryName)))
                .Append(mlContext.Transforms.Categorical.OneHotEncoding(nameof(TransactionData.BuySell)))
                .Append(mlContext.Transforms.Categorical.OneHotEncoding(nameof(TransactionData.NewsHeadline)))
                .Append(mlContext.Transforms.Categorical.OneHotEncoding(nameof(TransactionData.NewsSentiment)))
                .Append(mlContext.Transforms.Concatenate("Features",
                    nameof(TransactionData.TransactionId),
                    nameof(TransactionData.ClientId),
                    nameof(TransactionData.FkIdInstrument),
                    nameof(TransactionData.FkIdAsset),
                    nameof(TransactionData.FkIdProduct),
                    nameof(TransactionData.FkCategoryId),
                  // nameof(TransactionData.UnixTimestamp),
                    nameof(TransactionData.TotalAmount),
                    nameof(TransactionData.TotalAvgAmount),
                    nameof(TransactionData.TotalTransaction),
                    nameof(TransactionData.Xirr),
                    nameof(TransactionData.Bmxirr)))
                .Append(mlContext.Transforms.NormalizeMinMax("Features"))
                .AppendCacheCheckpoint(mlContext);


                var trainer = mlContext.MulticlassClassification.Trainers.SdcaMaximumEntropy("Label", "Features");
                var trainingPipeline = dataProcessPipeline.Append(trainer)
                    .Append(mlContext.Transforms.Conversion.MapKeyToValue("PredictedLabel"));

                // Train the model
                var model = trainingPipeline.Fit(dataView);

                // Evaluate the model
                var predictions = model.Transform(dataView);
                var metrics = mlContext.MulticlassClassification.Evaluate(predictions, "Label", "Score");

              //  Console.WriteLine($"Log-loss: {metrics.LogLoss}");

                // Save the model
                mlContext.Model.Save(model, dataView.Schema, "D:\\Project\\Risk\\Data\\trainingmodel.zip");

                // Load the model for prediction
                var loadedModel = mlContext.Model.Load("D:\\Project\\Risk\\Data\\trainingmodel.zip", out var modelInputSchema);

                // Create prediction engine
                var predEngine = mlContext.Model.CreatePredictionEngine<TransactionData, TransactionPrediction>(loadedModel);

                // Predict risk profiles for all transactions
                var predictionsList = transactions.Select(t => new { Transaction = t, Prediction = predEngine.Predict(t) }).ToList();


                 
                // Calculate overall risk profile for each client
                var overallRiskProfiles = predictionsList
                    .GroupBy(p => p.Transaction.ClientId)
                    .Select(g => new OverallRiskProfiles
                    {
                        ClientId = g.Key,
                        OverallRiskProfile = CalculateOverallRisk(g.Select(p => p.Prediction.RiskProfile).ToList())
                    })
                    .ToList();


                foreach (var profile in overallRiskProfiles)
                {
                    profiles.Add(profile);
                    //return $"ClientId: {profile.ClientId}, Overall Risk Profile: {profile.OverallRiskProfile}";
                }
            }
            return profiles;
        }

        public async Task<dynamic> GetPrediction(Param req)
        {
            
            MLContext mlContext = new MLContext();
            var profiles = new List<OverallRiskProfiles>();

            //Load data from proc to CSV
            var parameters = new DynamicParameters();
            parameters.Add("@client_id", req.ClientId, DbType.String, ParameterDirection.Input);
            parameters.Add("@flag", 3, DbType.Int32, ParameterDirection.Input);

            using (var connection = _dapperContext.CreateConnection())
            {
                var transactions = await connection.QueryAsync<TransactionData>("usp_client_dashboard", parameters, commandType: CommandType.StoredProcedure);

                foreach (var transaction in transactions)
                {
                    transaction.RiskProfile = DetermineRiskProfile(transaction);
                }

                SaveToCsv(transactions, "D:\\Project\\Risk\\Data\\transactions_with_risk_profile_predict.csv");


            }
            // Load the new client data
            string newClientDataPath = "D:\\Project\\Risk\\Data\\transactions_with_risk_profile_predict.csv";
            IDataView newClientDataView = mlContext.Data.LoadFromTextFile<TransactionData>(newClientDataPath, separatorChar: ',', hasHeader: true);

            // Load the trained model
            var loadedModel = mlContext.Model.Load("D:\\Project\\Risk\\Data\\trainingmodel.zip", out var modelInputSchema);

            var predEngine = mlContext.Model.CreatePredictionEngine<TransactionData, TransactionPrediction>(loadedModel);

            var newClientPredictions = mlContext.Data.CreateEnumerable<TransactionData>(newClientDataView, reuseRowObject: false)
            .Select(t => new { Transaction = t, Prediction = predEngine.Predict(t) })
            .ToList();

            var newClientOverallRiskProfiles = newClientPredictions
           .GroupBy(p => p.Transaction.ClientId)
           .Select(g => new OverallRiskProfiles
           {
               ClientId = g.Key,
               OverallRiskProfile = CalculateOverallRisk(g.Select(p => p.Prediction.RiskProfile).ToList())
           })
           .ToList();

            foreach (var profile in newClientOverallRiskProfiles)
            {
               profiles.Add(profile);
                //result += $"ClientId: {profile.ClientId}, Overall Risk Profile: {profile.OverallRiskProfile}\n";

            }
            return profiles;
        }

        static string CalculateOverallRisk(List<string> riskProfiles)
        {
            int highRiskCount = riskProfiles.Count(r => r == "Aggressive");
            int moderateRiskCount = riskProfiles.Count(r => r == "Balanced");
            int lowRiskCount = riskProfiles.Count(r => r == "Conservative");

            int totalTransactions = riskProfiles.Count;

            if (highRiskCount / (float)totalTransactions > 0.5)
            {
                return "Aggressive";
            }
            else if (lowRiskCount / (float)totalTransactions > 0.5)
            {
                return "Conservative";
            }
            else
            {
                return "Balanced";
            }
        }

        public string DetermineRiskProfile(TransactionData transaction)
        {
            // Example logic for determining risk profile
            if (transaction.AssetName == "Equity")
            {
                if (transaction.Xirr > transaction.Bmxirr && transaction.NewsSentiment == "Negative")
                {
                    return "Aggressive";
                }
                else if (transaction.Xirr > transaction.Bmxirr && transaction.NewsSentiment == "Positive")
                {
                    return "Balanced";
                }
                else if (transaction.Xirr <= transaction.Bmxirr)
                {
                    return "Balanced";
                }
            }
            else if (transaction.AssetName == "Debt")
            {
                if (transaction.Xirr < transaction.Bmxirr && transaction.NewsSentiment == "Positive")
                {
                    return "Conservative";
                }
                else if (transaction.Xirr < transaction.Bmxirr && transaction.NewsSentiment == "Negative")
                {
                    return "Balanced";
                }
                else if (transaction.Xirr >= transaction.Bmxirr)
                {
                    return "Balanced";
                }
            }


            // Additional logic based on product, category, total amount, and buy/sell action
            if (transaction.ProductName.Contains("Mutual Funds") && transaction.TotalAmount > 1000000)
            {
                return "Aggressive";
            }
            else if (transaction.CategoryName.Contains("Small Cap") && transaction.BuySell == "B")
            {
                return "Aggressive";
            }
            else if (transaction.CategoryName.Contains("Large Cap") && transaction.BuySell == "S")
            {
                return "Conservative";
            }

            // Default risk profile
            return "Balanced";
        }

        public void SaveToCsv(IEnumerable<TransactionData> transactions, string filePath)
        {
            var config = new CsvConfiguration(CultureInfo.InvariantCulture)
            {
                HasHeaderRecord = true,
            };

            using (var writer = new StreamWriter(filePath))
            using (var csv = new CsvWriter(writer, config))
            {
                csv.WriteRecords(transactions);
            }

            Console.WriteLine("CSV file has been created successfully.");
        }


    }






}





   





    




    

