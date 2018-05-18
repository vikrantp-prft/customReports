using OneNorth.ExperienceAnalyticsTableControl.Api;
using System;
using Sitecore.Analytics.Reporting;
using System.Data;

namespace OneNorth.ExperienceAnalyticsTableControl.DataSources
{
    public class SampleParameterDataSource
    {
        public ExperienceAnalyticsTableControlResponse Get(DateTime dateFrom, DateTime dateTo, string siteName, Guid id)
        {
            //Sitecore.Analytics.Reports.ReportFactory.
            Sitecore.Analytics.Reporting.ReportDataQuery test = new Sitecore.Analytics.Reporting.ReportDataQuery();
           
            var reportData = new ExperienceAnalyticsTableControlData<dynamic>();

            //-------------------
            //Connecting to the Analytics DB
            //var driver = Sitecore.Analytics.Data.DataAccess.MongoDb.MongoDbDriver.FromConnectionString("analytics");

            ////Building our query
            //var builder = new QueryBuilder();
            //var filter = builder.And(builder.GTE(_ => _.StartDateTime, DateTime.Now.AddDays(-30)), builder.EQ(_ => _.SiteName, siteName.ToLower()));

            ////Retrieving data from the "Interactions" collection
            //var interactions = driver.Interactions.FindAs(filter)
            //-------------------

            //Sitecore.Analytics.Reporting.MongoDbReportDataSource mongoDBSource = new Sitecore.Analytics.Reporting.MongoDbReportDataSource("analytics");
            //string query = "{collection: \"Interactions\",query: {_t: \"VisitData\"},fields: [\"_id\",\"ContactId\",\"StartDateTime\",\"EndDateTime\",\"Value\",\"VisitPageCount\"]}";
            //ReportDataQuery reportQuery = new Sitecore.Analytics.Reporting.ReportDataQuery(query);
            //DataTable interactions = mongoDBSource.GetData(reportQuery);
            ////------------------------------------------

            //string connectionString = ConfigurationManager.ConnectionStrings["analytics"].ConnectionString;

            //var client = new MongoDB.Driver.MongoClient(connectionString);

            //var database = client.GetServer().GetDatabase("your_database_name");

            //var contacts = database.GetCollection("Contacts");
            //var results =
            //    contacts.Aggregate(
            //        new BsonDocument
            //        {
            //{
            //    "$lookup",
            //    new BsonDocument
            //    {
            //        {"from", "Interactions"},
            //        {"localField", "_id"},
            //        {"foreignField", "ContactId"},
            //        {"as", "Interactions"}
            //    }
            //}
            //        });

            //// This may be a very large string, depending on the amount of data you have
            //string json = results.ResultDocuments.ToJson();
            //-------------------------------------------------------------
            var random = new Random();

            var count = 10;
            for (var i = 0; i < count; i++)
            {
                var item = new
                {
                    index = i,
                    id = Guid.NewGuid().ToString(),
                    datefrom = dateFrom.ToShortDateString(),
                    dateto = dateTo.ToShortDateString(),
                    sitename = siteName,
                    random = random.Next(0, 1000),
                    parameter = id.ToString()
                };

                reportData.AddItem(item);
            }
            var content = new ExperienceAnalyticsTableControlResponse()
            {
                Data = reportData,
                TotalRecordCount = count
            };

            return content;
        }
    }
}
