using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Driver;
using MongoDB.Driver.Builders;
using OneNorth.ExperienceAnalyticsTableControl.Api;
using Sitecore.Reflection;
using System;
using System.Configuration;
using System.Diagnostics;
using System.Linq;

namespace OneNorth.ExperienceAnalyticsTableControl.DataSources
{
    public class SampleBasicDataSource
    {
        public ExperienceAnalyticsTableControlResponse Get(DateTime dateFrom, DateTime dateTo, string siteName)
        {
            Stopwatch sw = new Stopwatch();

            var reportData = new ExperienceAnalyticsTableControlData<dynamic>();
            var count = 0;
            string cacheKey = "contactListCache";
            //var reportCache = new Sitecore.Caching.Cache("ReportCache", 1024);
            //reportCache.Add("test", 12)

            try
            {
                sw.Start();
                Sitecore.Diagnostics.Log.Info(" Custom Visitor Report : STOPWATCH - starts.", this);
                string connectionString = ConfigurationManager.ConnectionStrings["analytics"].ConnectionString;
                MongoUrl mongoUrl = new MongoUrl(connectionString);

                Sitecore.Diagnostics.Log.Info(" Custom Visitor Report : CONNECTION ESTABLISHED.", this);
                Sitecore.Diagnostics.Log.Info($" Custom Visitor Report : SiteName - {siteName}, DateFrom - {dateFrom.ToString("dd-MM-yy")}, DateTo - {dateTo.ToString("dd-MM-yy")}", this);

                var client = new MongoClient(connectionString);

                var database = client.GetServer().GetDatabase(mongoUrl.DatabaseName);

                Sitecore.Diagnostics.Log.Info($" Custom Visitor Report : STOPWATCH - database fetched in {sw.Elapsed.TotalSeconds} seconds", this);
                sw.Restart();

                IMongoQuery interactionQuery = null;

                BsonRegularExpression urlFilter1 = "[^/.]/industries/electricity/discover-the-active-grid";
                BsonRegularExpression urlFilter2 = "[^/.]/industries/electricity/discover-the-active-grid/";

                if (string.Equals(siteName, "All", StringComparison.OrdinalIgnoreCase))
                {
                    interactionQuery = Query.And(
                         Query.Or(
                             Query.Matches("Pages.Url.Path", urlFilter1), 
                             Query.Matches("Pages.Url.Path", urlFilter2)
                             ),
                         Query.GTE("StartDateTime", new BsonDateTime(dateFrom)),
                     Query.LTE("StartDateTime", new BsonDateTime(dateTo))
                             );
                }
                else
                {

                    interactionQuery = Query.And(
                     Query.Or(
                         Query.Matches("Pages.Url.Path", urlFilter1), 
                         Query.Matches("Pages.Url.Path", urlFilter2)
                         ),
                     Query.GTE("StartDateTime", new BsonDateTime(dateFrom)),
                     Query.LTE("StartDateTime", new BsonDateTime(dateTo)),
                     Query.EQ("SiteName", siteName)
                         );
                }

                Sitecore.Diagnostics.Log.Info($" Custom Visitor Report : STOPWATCH - search query built in {sw.Elapsed.TotalSeconds} seconds", this);
                sw.Restart();

                //Sitecore.Diagnostics.Log.Info($" Custom Visitor Report : BSON Dates = DateFrom - {new BsonDateTime(dateFrom).ToString()}, DateTo - {new BsonDateTime(dateTo).ToString()}", this);

                var interactions = database.GetCollection<Interaction>("Interactions").Find(interactionQuery);
                Sitecore.Diagnostics.Log.Info(" Custom Visitor Report : Interactions Query Executed.", this);

                Sitecore.Diagnostics.Log.Info($" Custom Visitor Report : STOPWATCH - Interactions fetched in {sw.Elapsed.TotalSeconds} seconds", this);
                sw.Restart();

                var customReportCache = Sitecore.Caching.CacheManager.FindCacheByName("CustomReportCache");
                if (customReportCache == null)
                {
                    customReportCache = new Sitecore.Caching.Cache("CustomReportCache", 1000000);
                    Sitecore.Diagnostics.Log.Info(" Custom Visitor Report : Pushing CustomReportCache into Cache.", this);
                }
                else
                {
                    Sitecore.Diagnostics.Log.Info(" Custom Visitor Report : Reading CustomReportCache from Cache.", this);
                }
                Sitecore.Diagnostics.Log.Info($" Custom Visitor Report : STOPWATCH - CustomReportCache fetched in {sw.Elapsed.TotalSeconds} seconds", this);
                sw.Restart();

                if (interactions != null && interactions.Any())
                {
                    //Sitecore.Diagnostics.Log.Info($" Custom Visitor Report : There are {interactions.Count().ToString()} filtered interactions ", this);
                    //Sitecore.Diagnostics.Log.Info($" Custom Visitor Report : Interaction String  - {interactions.ToJson()}", this);
                    Sitecore.Diagnostics.Log.Info($" Custom Visitor Report : STOPWATCH - Interactions found, working on getting contact ids.", this);

                    IMongoQuery contactQuery = null;

                    BsonArray contactidBsonArray = new BsonArray(interactions.Select(x => x.ContactId));
                    //Sitecore.Diagnostics.Log.Info($" Custom Visitor Report : Contact Id String  - {contactidBsonArray.ToJson()}", this);
                    Sitecore.Diagnostics.Log.Info($" Custom Visitor Report : STOPWATCH - Contacts Ids fetched in {sw.Elapsed.TotalSeconds} seconds", this);
                    sw.Restart();

                    contactQuery = Query.In("_id", contactidBsonArray);
                    MongoCollection<User> contactsCollection = null;

                    Sitecore.Diagnostics.Log.Info(" Custom Visitor Report : Started working on Contacts.", this);

                    var cachedContacts = customReportCache.GetValue(cacheKey);
                   

                    if (cachedContacts != null)
                    {
                        contactsCollection = (MongoCollection<User>)cachedContacts;
                        //Sitecore.Diagnostics.Log.Info($" Custom Visitor Report : {test}", this);

                        //contactsCollection = (MongoCollection<User>)customReportCache.GetValue(cacheKey);
                        Sitecore.Diagnostics.Log.Info(" Custom Visitor Report : Reading Contacts from Cache.", this);
                    }
                    else
                    {
                        contactsCollection = database.GetCollection<User>("Contacts");
                        customReportCache.Add(cacheKey, contactsCollection, TypeUtil.SizeOfObject(), DateTime.Now.AddHours(12));
                        Sitecore.Diagnostics.Log.Info($" Custom Visitor Report : Pushing Contacts into Cache. Size is {customReportCache.Size}", this);
                    }

                    Sitecore.Diagnostics.Log.Info($" Custom Visitor Report : STOPWATCH - Contacts fetched in {sw.Elapsed.TotalSeconds} seconds", this);
                    sw.Restart();

                    var contacts = contactsCollection.Find(contactQuery);
                    Sitecore.Diagnostics.Log.Info(" Custom Visitor Report : Contacts Query Executed.", this);

                    Sitecore.Diagnostics.Log.Info($" Custom Visitor Report : STOPWATCH - Contacts filtered in {sw.Elapsed.TotalSeconds} seconds", this);
                    sw.Stop();

                    if (contacts != null && contacts.Any())
                    {
                        Sitecore.Diagnostics.Log.Info($" Custom Visitor Report : There are {contacts.Count().ToString()} filtered contacts ", this);
                        int iteration = 1;
                        foreach (var contact in contacts)
                        {
                            if (!string.IsNullOrEmpty(contact.Personal.FirstName) && !string.IsNullOrEmpty(contact.Personal.Surname) && !string.IsNullOrEmpty(contact.Identifiers.Identifier))
                            {
                                var matchedInteraction = interactions.FirstOrDefault(x => x.ContactId.Equals(contact.id));

                                var item = new
                                {
                                    index = iteration - 1,
                                    srn = iteration,
                                    id = contact.id,
                                    emailid = contact.Identifiers.Identifier, 
                                    fullname = $"{contact.Personal.FirstName} {contact.Personal.Surname}", 
                                    sitename = matchedInteraction.SiteName,
                                    name = $"{contact.Personal.FirstName} {contact.Personal.Surname}",
                                    activitydate = matchedInteraction.EndDateTime.ToString("MM/dd/yyyy HH:mm:ss"),
                                    visitvalue = matchedInteraction.Value,
                                    filtersitename = siteName

                                };

                                reportData.AddItem(item);
                                iteration++;
                            }

                        }
                    }
                    else
                    {
                        Sitecore.Diagnostics.Log.Info($" Custom Visitor Report : There are NO filtered contacts ", this);
                    }
                }
                else
                {
                    Sitecore.Diagnostics.Log.Info($" Custom Visitor Report : There are NO filtered interactions ", this);
                }
                Sitecore.Diagnostics.Log.Info(" Custom Visitor Report : ACTIVITY COMPLETED", this);
            }
            catch (Exception e)
            {

                Sitecore.Diagnostics.Log.Error($" Custom Visitor Report : {e.Message}", this);
                Sitecore.Diagnostics.Log.Info(" Custom Visitor Report : ACTIVITY ENDE DUE TO ERROR", this);
            }

            var content = new ExperienceAnalyticsTableControlResponse()
            {
                Data = reportData,
                TotalRecordCount = count
            };

            return content;
        }

    }

    [BsonIgnoreExtraElements]
    public class User
    {
        public User()
        {
            Personal = new Personal();
            Identifiers = new Identifiers();
        }

        [BsonElementAttribute("_id")]
        public Guid id { get; set; }

        [BsonElementAttribute("Identifiers")]
        public Identifiers Identifiers { get; set; }

        [BsonElementAttribute("Personal")]
        public Personal Personal { get; set; }

        public string GetFullName()
        {
            return String.Format("{0} {1}", Personal.FirstName, Personal.Surname);
        }
    }


    [BsonIgnoreExtraElements]
    public class Identifiers
    {
        [BsonElementAttribute("Identifier")]
        public string Identifier;

    }


    [BsonIgnoreExtraElements]
    public class Personal
    {
        [BsonElementAttribute("FirstName")]
        public string FirstName;
        [BsonElementAttribute("Surname")]
        public string Surname;

    }

    [BsonIgnoreExtraElements]
    public class Interaction
    {
        public Interaction()
        {
        }

        [BsonElementAttribute("ContactId")]
        public Guid ContactId { get; set; }

        [BsonElementAttribute("Value")]
        public Int32 Value { get; set; }

        [BsonElementAttribute("EndDateTime")]
        public DateTime EndDateTime { get; set; }

        [BsonElementAttribute("SiteName")]
        public string SiteName { get; set; }

    }

    /* ---- Query to fire on Mongo DB to verify data ----
     db.getCollection('Interactions').find({
    $and : [
        { $or : [
                {"Pages.Url.Path" : "/na/industries/electricity/discover-the-active-grid"},
                {"Pages.Url.Path" : "/na/industries/electricity/discover-the-active-grid/"}
                ]
        },
        {"StartDateTime" : { $gte : new ISODate("2018-01-05") } },
        {"StartDateTime" : { $lte : new ISODate("2018-02-05") } }
           ]
        },
        {"ContactId":1,"_id":0, "Value":2, "EndDateTime":3, "SiteName":4 }) 



        db.getCollection('Contacts').find( {
                 $or : [
            {"_id" : LUUID("7c22b0f3-4289-9c4a-a238-3eba71449083")},
            {"_id" : LUUID("fb26b0c5-eee7-184a-9ecd-4808ca926410")},
            {"_id" : LUUID("fb26b0c5-eee7-184a-9ecd-4808ca926410")},
            {"_id" : LUUID("fb26b0c5-eee7-184a-9ecd-4808ca926410")},
            {"_id" : LUUID("fb26b0c5-eee7-184a-9ecd-4808ca926410")},
            {"_id" : LUUID("fb26b0c5-eee7-184a-9ecd-4808ca926410")},
            {"_id" : LUUID("fb26b0c5-eee7-184a-9ecd-4808ca926410")},
            {"_id" : LUUID("bb243e63-84e7-0f4c-bb80-c47138bbfbbe")},
            {"_id" : LUUID("7c22b0f3-4289-9c4a-a238-3eba71449083")},
            {"_id" : LUUID("7c22b0f3-4289-9c4a-a238-3eba71449083")},
            {"_id" : LUUID("fb26b0c5-eee7-184a-9ecd-4808ca926410")},
            {"_id" : LUUID("fb26b0c5-eee7-184a-9ecd-4808ca926410")},
            {"_id" : LUUID("1d713f93-0084-4043-b60a-9fc0fcdca050")},
            {"_id" : LUUID("8974b3de-9b40-6644-ab1b-45a7bcfe5032")},
            {"_id" : LUUID("8974b3de-9b40-6644-ab1b-45a7bcfe5032")},
            {"_id" : LUUID("8974b3de-9b40-6644-ab1b-45a7bcfe5032")},
            {"_id" : LUUID("2b8cb9bb-6163-2240-b93a-e89d9536fa28")},
            {"_id" : LUUID("2b8cb9bb-6163-2240-b93a-e89d9536fa28")},
            {"_id" : LUUID("e9bee238-a072-014b-a2a2-7a173e820022")},
            {"_id" : LUUID("1d713f93-0084-4043-b60a-9fc0fcdca050")},
            {"_id" : LUUID("1d713f93-0084-4043-b60a-9fc0fcdca050")},
            {"_id" : LUUID("bf9b69be-d5fb-6c48-ae23-45255c3f55eb")},
            {"_id" : LUUID("8530ae84-1c3b-bf4f-8d03-2bccbe229475")},
            {"_id" : LUUID("aede9939-f94d-6f4b-8677-3ce0f3a3b357")},
            {"_id" : LUUID("99693620-69df-664a-8acc-f0bdfe0a2ddd")},
            {"_id" : LUUID("bb243e63-84e7-0f4c-bb80-c47138bbfbbe")}
                       ]
             }
             ) 
    */
}
